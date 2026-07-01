using System;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Terraria;
using Terraria.ModLoader;
using NLayer;

namespace Neurosama
{
    /// <summary>
    /// Global manager that holds multiple active streaming channels and shares a single HttpClient.
    /// </summary>
    public static class Mp3StreamManager
    {
        private static readonly HttpClient _sharedHttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };

        // Keeps track of separate audio streams by a unique key/ID string
        private static readonly Dictionary<string, Mp3StreamChannel> _channels = new Dictionary<string, Mp3StreamChannel>();

        public static void StartStream(string channelId, string url)
        {
            if (!_channels.TryGetValue(channelId, out var channel))
            {
                channel = new Mp3StreamChannel(_sharedHttpClient);
                _channels[channelId] = channel;
            }

            channel.Start(url);
        }

        public static void StopStream(string channelId)
        {
            if (_channels.TryGetValue(channelId, out var channel))
            {
                channel.Stop();
            }
        }

        public static void StopAll()
        {
            foreach (var channel in _channels.Values)
            {
                channel.Stop();
            }
        }

        public static void PumpAudioBuffers()
        {
            foreach (var channel in _channels.Values)
            {
                channel.PumpAudio();
            }
        }

        public static void UpdateMainThreadCleanup()
        {
            foreach (var channel in _channels.Values)
            {
                channel.UpdateCleanup();
            }
        }

        public static Mp3StreamChannel GetChannel(string channelId)
        {
            _channels.TryGetValue(channelId, out var channel);
            return channel;
        }
    }

    /// <summary>
    /// Represents an independent, instance-based audio stream slot.
    /// </summary>
    public class Mp3StreamChannel
    {
        public float Volume { get; set; } = 1f;
        public string Url { get; private set; } = "";
        public string CurrentSongTitle { get; private set; } = "";
        public bool IsStreaming { get; private set; } = false;

        private readonly HttpClient _httpClient;
        private DynamicSoundEffectInstance _soundInstance;
        private CancellationTokenSource _cts;
        private readonly ConcurrentQueue<byte[]> _audioBufferQueue = new ConcurrentQueue<byte[]>();

        private const int PreBufferTargetCount = 15;
        private const int MaxQueueCapacity = PreBufferTargetCount + 15;

        private bool _wasUnfocused = false;
        private bool _stopRequested = false;
        private readonly object _instanceLock = new object();

        public Mp3StreamChannel(HttpClient sharedClient)
        {
            _httpClient = sharedClient;
        }

        public void Start(string url)
        {
            if (IsStreaming) return;

            Url = url;
            IsStreaming = true;
            _stopRequested = false;

            while (_audioBufferQueue.TryDequeue(out _)) ;
            _cts = new CancellationTokenSource();

            Task.Run(() => StreamLoop(url, _cts.Token));
        }

        private async Task StreamLoop(string url, CancellationToken token)
        {
            DynamicSoundEffectInstance localInstance = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Icy-MetaData", "1");
                request.Headers.UserAgent.ParseAdd("Tmodloader-NeuroMod-MusicBox");

                // Use a linked timeout specifically for the handshake phase
                using var handshakeCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                handshakeCts.CancelAfter(TimeSpan.FromSeconds(6));

                HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, handshakeCts.Token);
                response.EnsureSuccessStatusCode();

                int metaInt = 0;
                if (response.Headers.TryGetValues("icy-metaint", out var values))
                {
                    int.TryParse(System.Linq.Enumerable.FirstOrDefault(values), out metaInt);
                }

                Stream rawNetStream = await response.Content.ReadAsStreamAsync(token);

                if (metaInt > 0)
                {
                    rawNetStream = new IcyStreamWrapper(rawNetStream, metaInt, (title) =>
                    {
                        CurrentSongTitle = title;
                    });
                }

                using (rawNetStream)
                using (var trackableStream = new PositionTrackingStream(rawNetStream))
                using (var mpegStream = new MpegFile(trackableStream))
                {
                    lock (_instanceLock)
                    {
                        if (token.IsCancellationRequested) return;

                        _soundInstance = new DynamicSoundEffectInstance(mpegStream.SampleRate, (AudioChannels)mpegStream.Channels);
                        localInstance = _soundInstance;
                    }

                    float[] sampleBuffer = new float[4096];
                    byte[] pcmByteBuffer = new byte[sampleBuffer.Length * 2];
                    bool hasStartedPlayback = false;

                    while (!token.IsCancellationRequested)
                    {
                        bool keepAlive = ModContent.GetInstance<Common.Configs.NeurosamaConfig>().KeepStreamingUnfocused;
                        if (!Main.instance.IsActive && !keepAlive)
                        {
                            _stopRequested = true;
                            break;
                        }


                        if (_audioBufferQueue.Count >= MaxQueueCapacity)
                        {
                            await Task.Delay(40, token); // Slightly reduced backoff for better frame-pacing
                            continue;
                        }

                        // Wrap the synchronous network decoding loop securely to prevent main thread stutters
                        int samplesRead = await Task.Run(() => mpegStream.ReadSamples(sampleBuffer, 0, sampleBuffer.Length), token);
                        if (samplesRead == 0)
                        {
                            ModContent.GetInstance<Neurosama>().Logger.Warn("Stream reached EOF. Stream stopping.");
                            break;
                        }

                        for (int i = 0; i < samplesRead; i++)
                        {
                            short sample = (short)MathHelper.Clamp(sampleBuffer[i] * short.MaxValue, short.MinValue, short.MaxValue);
                            pcmByteBuffer[i * 2] = (byte)(sample & 0xFF);
                            pcmByteBuffer[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
                        }

                        byte[] cleanChunk = new byte[samplesRead * 2];
                        Buffer.BlockCopy(pcmByteBuffer, 0, cleanChunk, 0, cleanChunk.Length);

                        _audioBufferQueue.Enqueue(cleanChunk);

                        if (!hasStartedPlayback && _audioBufferQueue.Count >= PreBufferTargetCount)
                        {
                            lock (_instanceLock)
                            {
                                if (_soundInstance != null && !_soundInstance.IsDisposed)
                                {
                                    _soundInstance.Play();
                                    hasStartedPlayback = true;
                                }
                            }
                        }

                        await Task.Delay(5, token);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                ModContent.GetInstance<Neurosama>().Logger.Error("Audio Stream Error: ", ex);
            }
            finally
            {
                _stopRequested = true;
            }
        }

        public void PumpAudio()
        {
            lock (_instanceLock)
            {
                if (_soundInstance == null || _soundInstance.IsDisposed)
                    return;

                _soundInstance.Volume = MathHelper.Clamp(Volume, 0f, 1f);

                bool keepAlive = ModContent.GetInstance<Common.Configs.NeurosamaConfig>().KeepStreamingUnfocused;

                if (!keepAlive)
                {
                    if (Main.instance.IsActive)
                    {
                        if (_wasUnfocused)
                        {
                            _wasUnfocused = false;
                            string cachedUrl = Url;
                            Stop();
                            if (!string.IsNullOrEmpty(cachedUrl))
                            {
                                Start(cachedUrl);
                            }
                            return;
                        }
                    }
                    else
                    {
                        _wasUnfocused = true;
                        return;
                    }
                }
                else
                {
                    _wasUnfocused = false;
                }

                if (_soundInstance.State != SoundState.Playing && _audioBufferQueue.Count >= PreBufferTargetCount)
                {
                    try { _soundInstance.Play(); } catch { }
                }

                if (_soundInstance.State != SoundState.Playing)
                    return;

                while (_soundInstance.PendingBufferCount < 4)
                {
                    if (_audioBufferQueue.TryDequeue(out byte[] nextChunk))
                    {
                        _soundInstance.SubmitBuffer(nextChunk);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        public void UpdateCleanup()
        {
            if (_stopRequested || Main.gameMenu)
            {
                Stop();
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            IsStreaming = false;
            _stopRequested = false;

            while (_audioBufferQueue.TryDequeue(out _)) ;

            lock (_instanceLock)
            {
                if (_soundInstance != null)
                {
                    var instanceToDispose = _soundInstance;
                    _soundInstance = null;

                    Main.QueueMainThreadAction(() =>
                    {
                        try
                        {
                            if (!instanceToDispose.IsDisposed)
                            {
                                instanceToDispose.Stop();
                                instanceToDispose.Dispose();
                            }
                        }
                        catch { }
                    });
                }
            }
        }
    }

    /// <summary>
    /// Stream wrapper tracking current byte positions for the decoder.
    /// </summary>
    public class PositionTrackingStream : Stream
    {
        private readonly Stream _baseStream;
        private long _position;

        public PositionTrackingStream(Stream baseStream)
        {
            _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
        }

        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _baseStream.Length;

        public override long Position
        {
            get => _position;
            set { }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = _baseStream.Read(buffer, offset, count);
            _position += read;
            return read;
        }

        public override void Flush() => _baseStream.Flush();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    /// <summary>
    /// Parsers raw icy streams to read injected metadata text blocks dynamically.
    /// </summary>
    public class IcyStreamWrapper : Stream
    {
        private readonly Stream _baseStream;
        private readonly int _metaInt;
        private readonly Action<string> _onTitleFound;
        private int _bytesUntilMeta;

        public IcyStreamWrapper(Stream baseStream, int metaInt, Action<string> onTitleFound)
        {
            _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _metaInt = metaInt;
            _bytesUntilMeta = metaInt;
            _onTitleFound = onTitleFound;
        }

        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _baseStream.Length;
        public override long Position { get => _baseStream.Position; set { } }
        public override void Flush() => _baseStream.Flush();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                if (_bytesUntilMeta == 0)
                {
                    int metadataLengthByte = _baseStream.ReadByte();
                    if (metadataLengthByte == -1) break; // End of stream

                    if (metadataLengthByte > 0)
                    {
                        int metadataLength = metadataLengthByte * 16;
                        byte[] metaBuffer = new byte[metadataLength];
                        int totalMetaRead = 0;

                        while (totalMetaRead < metadataLength)
                        {
                            int read = _baseStream.Read(metaBuffer, totalMetaRead, metadataLength - totalMetaRead);
                            if (read <= 0) break;
                            totalMetaRead += read;
                        }

                        string metaString = System.Text.Encoding.UTF8.GetString(metaBuffer);
                        if (metaString.Contains("StreamTitle='"))
                        {
                            string startSearch = "StreamTitle='";
                            int start = metaString.IndexOf(startSearch) + startSearch.Length;
                            int end = metaString.IndexOf("';", start);
                            if (start >= 0 && end > start)
                            {
                                string songTitle = metaString.Substring(start, end - start);
                                _onTitleFound?.Invoke(songTitle);
                            }
                        }
                    }
                    _bytesUntilMeta = _metaInt;
                }

                int toRead = Math.Min(count - totalBytesRead, _bytesUntilMeta);
                if (toRead <= 0) break;

                int bytesRead = _baseStream.Read(buffer, offset + totalBytesRead, toRead);
                if (bytesRead <= 0) break; // No more data available right now

                totalBytesRead += bytesRead;
                _bytesUntilMeta -= bytesRead;
            }

            return totalBytesRead;
        }
    }

    /// Stop stream when exiting world so that it doesnt play in menu
    public class Mp3RadioUnloadSystem : ModSystem
    {
        public override void OnWorldUnload()
        {
            Mp3StreamManager.StopAll();
        }

        public override void UpdateUI(GameTime gameTime)
        {
            Mp3StreamManager.UpdateMainThreadCleanup();
            Mp3StreamManager.PumpAudioBuffers();
        }

        public override void Unload()
        {
            Mp3StreamManager.StopAll();
        }
    }
}