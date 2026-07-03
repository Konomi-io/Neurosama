using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using Neurosama.Content.Tiles.Furniture;

namespace Neurosama.Content
{
    public class LavaLampColor : ModSystem
    {
        public static Color CurrentColor { get; private set; } = Color.White;

        public static bool IsLive
        {
            get { lock (_stateLock) return _isLive; }
        }

        public static long LastSetUnixMs
        {
            get { lock (_stateLock) return _lastSetUnixMs; }
        }

        private static Color _targetColor = Color.White;
        private static bool _isLive = false;
        private static long _lastSetUnixMs = 0;
        private static readonly object _stateLock = new();

        private const float LerpSpeed = 0.05f;
        private static CancellationTokenSource _cts;
        private static int _lampActiveFramesRemaining = 0; // Thread-safe atomic counter
        private static volatile bool _useDiscoFallback = false;

        public static bool IsLavaLampOnScreen => Volatile.Read(ref _lampActiveFramesRemaining) > 0;

        public static void MarkLampAsVisible()
        {
            // Thread-safe write: Forces the value to 5 atomically across rendering threads
            Interlocked.Exchange(ref _lampActiveFramesRemaining, 5);
        }

        private static string TrimBytesAndEndings(string source)
        {
            return source.Replace("\r", "").Trim();
        }

        private static string BaseUrl
        {
            get
            {
                var config = ModContent.GetInstance<Common.Configs.NeurosamaConfig>();
                return config != null && config.UseTestServer
                    ? "https://test.neurolavalamp.com"
                    : "https://api.neurolavalamp.com";
            }
        }

        private static bool IsHostReachable(string url)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return false;
            }
            try
            {
                var uri = new Uri(url);
                var addresses = System.Net.Dns.GetHostAddresses(uri.Host);
                return addresses != null && addresses.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsSteamConnected =>
            Terraria.Social.SocialAPI.Mode == Terraria.Social.SocialMode.Steam &&
            Steamworks.SteamAPI.IsSteamRunning() &&
            Steamworks.SteamUser.BLoggedOn();

        public override void OnModLoad() { }

        public override void OnWorldLoad()
        {
            if (Main.dedServ) return;

            lock (_stateLock)
            {
                _targetColor = Main.DiscoColor;
                CurrentColor = Main.DiscoColor;
            }

            _cts = new CancellationTokenSource();
            _useDiscoFallback = false;
            Interlocked.Exchange(ref _lampActiveFramesRemaining, 0);
            Task.Run(() => StreamLavaLampAsync(_cts.Token));
        }

        public override void OnWorldUnload() => CleanUpToken();
        public override void OnModUnload() => CleanUpToken();

        private static void CleanUpToken()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            Interlocked.Exchange(ref _lampActiveFramesRemaining, 0);
        }

        public override void PostUpdateEverything()
        {
            if (Main.dedServ) return;

            // Thread-safe decrement on the main game thread
            if (Volatile.Read(ref _lampActiveFramesRemaining) > 0)
            {
                Interlocked.Decrement(ref _lampActiveFramesRemaining);
            }

            Color workingTargetColor;
            bool workingIsLive;

            lock (_stateLock)
            {
                workingTargetColor = _targetColor;
                workingIsLive = _isLive;
            }

            var config = ModContent.GetInstance<Common.Configs.NeurosamaConfig>();
            bool configOverrideActive = config != null && config.UseDiscoColorWhenNoNeuroStream && !workingIsLive;

            if (_useDiscoFallback || configOverrideActive)
            {
                workingTargetColor = Main.DiscoColor;
            }

            if (CurrentColor != workingTargetColor)
            {
                CurrentColor = Color.Lerp(CurrentColor, workingTargetColor, LerpSpeed);

                if (Vector3.Distance(CurrentColor.ToVector3(), workingTargetColor.ToVector3()) < 0.01f)
                {
                    CurrentColor = workingTargetColor;
                }
            }
        }

        private static async Task StreamLavaLampAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                string currentActiveUrl = BaseUrl;

                if (!IsLavaLampOnScreen || !IsSteamConnected)
                {
                    if (!IsSteamConnected && IsLavaLampOnScreen)
                    {
                        HandleFallbackTransition();
                    }
                    await Task.Delay(1000, token);
                    continue;
                }

                if (!IsHostReachable(currentActiveUrl))
                {
                    HandleFallbackTransition();
                    await Task.Delay(2000, token);
                    continue;
                }

                await RunSilentSseLoopAsync(currentActiveUrl, token);

                bool liveSnapshot;
                lock (_stateLock)
                {
                    liveSnapshot = _isLive;
                }

                int delayMs = _useDiscoFallback ? 10000 : (liveSnapshot ? 1000 : 30000);
                try
                {
                    await Task.Delay(delayMs, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private static async Task RunSilentSseLoopAsync(string url, CancellationToken token)
        {
            var uri = new Uri($"{url}/v1/events");
            using var client = new TcpClient();

            try
            {
                ModContent.GetInstance<Neurosama>().Logger.Debug($"SSE Stream Connecting: {uri.AbsoluteUri}");
                await client.ConnectAsync(uri.Host, 443, token);

                using var sslStream = new SslStream(client.GetStream(), false);
                await sslStream.AuthenticateAsClientAsync(uri.Host);

                string request = $"GET {uri.PathAndQuery} HTTP/1.1\r\n" +
                                 $"Host: {uri.Host}\r\n" +
                                 "Accept: text/event-stream\r\n" +
                                 $"User-Agent: NeurosamaTerrariaMod/{ModContent.GetInstance<Neurosama>().Version}\r\n" +
                                 "Connection: close\r\n\r\n";

                byte[] requestBytes = Encoding.UTF8.GetBytes(request);
                await sslStream.WriteAsync(requestBytes.AsMemory(0, requestBytes.Length), token);
                await sslStream.FlushAsync(token);

                byte[] buffer = new byte[4096];
                StringBuilder stringBuffer = new StringBuilder();
                int headerEndIndex = -1;

                while (headerEndIndex == -1 && !token.IsCancellationRequested)
                {
                    int read = await sslStream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (read == 0) return;

                    stringBuffer.Append(Encoding.UTF8.GetString(buffer, 0, read));
                    headerEndIndex = stringBuffer.ToString().IndexOf("\r\n\r\n");
                }

                string structuralRemainder = stringBuffer.ToString().Substring(headerEndIndex + 4);
                StringBuilder lineBuffer = new StringBuilder();
                StringBuilder dataBuffer = new StringBuilder();
                lineBuffer.Append(structuralRemainder);

                while (!token.IsCancellationRequested && IsLavaLampOnScreen && IsSteamConnected)
                {
                    if (url != BaseUrl) break;

                    if (client.Client == null || !client.Connected) break;

                    if (client.Client.Available == 0)
                    {
                        bool pollPassed = !(client.Client.Poll(1, SelectMode.SelectRead) && client.Client.Available == 0);
                        if (!pollPassed) break;
                    }

                    int bytesRead = 0;
                    try
                    {
                        bytesRead = await sslStream.ReadAsync(buffer, 0, buffer.Length, token);
                    }
                    catch
                    {
                        break;
                    }

                    if (bytesRead == 0) break;

                    string chunkText = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    lineBuffer.Append(chunkText);

                    string currentContent = lineBuffer.ToString();
                    int newlineIndex;

                    while ((newlineIndex = currentContent.IndexOf('\n')) != -1)
                    {
                        string rawLine = currentContent.Substring(0, newlineIndex);
                        string line = TrimBytesAndEndings(rawLine);

                        currentContent = currentContent.Substring(newlineIndex + 1);

                        lineBuffer.Clear();
                        lineBuffer.Append(currentContent);

                        if (string.IsNullOrEmpty(line))
                        {
                            if (dataBuffer.Length > 0)
                            {
                                ParseAndEmitState(dataBuffer.ToString());
                                dataBuffer.Clear();
                            }
                            continue;
                        }

                        if (line.StartsWith(":")) continue;

                        if (line.StartsWith("data:"))
                        {
                            dataBuffer.AppendLine(line.Substring(5).Trim());
                        }
                    }
                }
            }
            catch
            {
                // Suppress background drops silently
            }
            finally
            {
                HandleFallbackTransition();
            }
        }

        private static void HandleFallbackTransition()
        {
            _useDiscoFallback = true;
            lock (_stateLock)
            {
                _isLive = false;
                _lastSetUnixMs = 0;
            }
        }

        private static void ParseAndEmitState(string json)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                long newUnixMs = root.GetProperty("lastSetUnixMs").GetInt64();
                bool newLive = root.GetProperty("live").GetBoolean();
                JsonElement rgb = root.GetProperty("rgb");
                Color newColor = new Color(rgb[0].GetInt32(), rgb[1].GetInt32(), rgb[2].GetInt32());

                _useDiscoFallback = false;

                lock (_stateLock)
                {
                    if (newUnixMs <= _lastSetUnixMs && newColor == _targetColor && newLive == _isLive)
                        return;

                    _targetColor = newColor;
                    _isLive = newLive;
                    _lastSetUnixMs = newUnixMs;
                }
            }
            catch (Exception ex)
            {
                ModContent.GetInstance<Neurosama>().Logger.Debug($"LavaLamp JSON Parse Error: {ex.Message}");
                HandleFallbackTransition();
            }
        }
    }
}