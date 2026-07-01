using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Net.Http;
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
        // Globally accessible properties for Lavalamp furniture
        public static Color CurrentColor { get; private set; } = Color.White;

        public static bool IsLive
        {
            get { lock (_stateLock) return _isLive; }
        }

        public static long LastSetUnixMs
        {
            get { lock (_stateLock) return _lastSetUnixMs; }
        }

        // Thread-protected backing fields
        private static Color _targetColor = Color.White;
        private static bool _isLive = false;
        private static long _lastSetUnixMs = 0;
        private static readonly object _stateLock = new();

        private const float LerpSpeed = 0.05f;
        private static readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
        private static CancellationTokenSource _cts;
        private static volatile bool _isLavaLampOnScreen = false;
        private static int _scanTimer = 0;
        private static volatile bool _useDiscoFallback = false;

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

        public override void OnModLoad() { }

        public override void OnWorldLoad()
        {
            if (Main.dedServ) return;
            
            //initialize as disco
            lock (_stateLock)
            {
                _targetColor = Main.DiscoColor;
                CurrentColor = Main.DiscoColor;
            }

            _cts = new CancellationTokenSource();
            _useDiscoFallback = false;
            Task.Run(() => StreamLavaLampAsync(_cts.Token));
        }

        public override void OnWorldUnload() => CleanUpToken();
        public override void OnModUnload() => CleanUpToken();

        private static void CleanUpToken()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _isLavaLampOnScreen = false;
        }

        public override void PostUpdateEverything()
        {
            if (Main.dedServ) return;

            Color workingTargetColor;
            bool workingIsLive;

            // Extract values safely away from background thread manipulation
            lock (_stateLock)
            {
                workingTargetColor = _targetColor;
                workingIsLive = _isLive;
            }

            // 1. Fetch config to check for the disco override feature
            var config = ModContent.GetInstance<Common.Configs.NeurosamaConfig>();
            bool configOverrideActive = config != null && config.UseDiscoColorWhenNoNeuroStream && !workingIsLive;

            // 2. Determine final target color destination for this frame
            if (_useDiscoFallback || configOverrideActive)
            {
                workingTargetColor = Main.DiscoColor;
            }

            // 3. Smoothly transition CurrentColor toward workingTargetColor frame-by-frame
            if (CurrentColor != workingTargetColor)
            {
                CurrentColor = Color.Lerp(CurrentColor, workingTargetColor, LerpSpeed);

                if (Vector3.Distance(CurrentColor.ToVector3(), workingTargetColor.ToVector3()) < 0.01f)
                {
                    CurrentColor = workingTargetColor;
                }
            }

            // 4. Update tile scan timing
            _scanTimer++;
            if (_scanTimer >= 30)
            {
                _scanTimer = 0;
                _isLavaLampOnScreen = CheckIfLampOnScreen();
            }
        }

        private bool CheckIfLampOnScreen()
        {
            // 1. Cache conversions and clamp bounds immediately to eliminate overhead inside the loops
            int startX = Math.Max(0, (int)(Main.screenPosition.X / 16f) - 2);
            int endX = Math.Min(Main.maxTilesX - 1, (int)((Main.screenPosition.X + Main.screenWidth) / 16f) + 2);
            int startY = Math.Max(0, (int)(Main.screenPosition.Y / 16f) - 2);
            int endY = Math.Min(Main.maxTilesY - 1, (int)((Main.screenPosition.Y + Main.screenHeight) / 16f) + 2);

            ushort targetTileType = (ushort)ModContent.TileType<LavaLamp>();

            // 2. Step by 2 on the Y axis since the lamp is 2 tiles high.
            // This allows you to skip 50% of the screen's Y-coordinates safely.
            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y += 2)
                {
                    Tile tile = Main.tile[x, y];
                    if (tile.HasTile && tile.TileType == targetTileType)
                    {
                        return true; // Found a piece of the lamp, stop checking immediately
                    }
                }
            }
            return false;
        }

        private static async Task StreamLavaLampAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                string currentActiveUrl = BaseUrl;

                if (!_isLavaLampOnScreen)
                {
                    await Task.Delay(1000, token);
                    continue;
                }

                try
                {
                    await ListenToSSEAsync(currentActiveUrl, token);
                }
                catch (Exception ex)
                {
                    ModContent.GetInstance<Neurosama>().Logger.Debug($"LavaLamp SSE disconnected: {ex.Message}");
                    HandleFallbackTransition();
                }

                if (!token.IsCancellationRequested && _isLavaLampOnScreen)
                {
                    await PollFallbackAsync(token);
                }
            }
        }

        private static async Task ListenToSSEAsync(string targetUrl, CancellationToken token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{targetUrl}/v1/events");
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(token);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            StringBuilder dataBuffer = new();

            while (!token.IsCancellationRequested && !reader.EndOfStream)
            {
                if (!_isLavaLampOnScreen) break;

                if (targetUrl != BaseUrl)
                {
                    ModContent.GetInstance<Neurosama>().Logger.Info("LavaLamp config changed server target. Reconnecting...");
                    lock (_stateLock) _lastSetUnixMs = 0;
                    break;
                }

                string line = await reader.ReadLineAsync();

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
                    string value = line.Substring(5).Trim();
                    dataBuffer.AppendLine(value);
                }
            }
        }

        private static async Task PollFallbackAsync(CancellationToken token)
        {
            ModContent.GetInstance<Neurosama>().Logger.Info("LavaLamp entering smart polling fallback mode.");
            DateTime sseRetryDeadline = DateTime.UtcNow.AddSeconds(60);

            while (DateTime.UtcNow < sseRetryDeadline && !token.IsCancellationRequested && _isLavaLampOnScreen)
            {
                string currentPollingUrl = BaseUrl;

                try
                {
                    if (!_isLavaLampOnScreen) return;

                    // Recommended pattern update to fully leverage cancellation tokens during standard requests
                    using var response = await httpClient.GetAsync($"{currentPollingUrl}/v1/rgb", token);
                    response.EnsureSuccessStatusCode();
                    string json = await response.Content.ReadAsStringAsync(token);

                    ParseAndEmitState(json);
                }
                catch (Exception ex)
                {
                    ModContent.GetInstance<Neurosama>().Logger.Debug($"LavaLamp Poll Error: {ex.Message}");
                    HandleFallbackTransition();
                }

                bool liveSnapshot;
                lock (_stateLock) liveSnapshot = _isLive;

                int delayMs = _useDiscoFallback ? 10000 : (liveSnapshot ? 1000 : 30000);
                int interval = 500;
                int elapsed = 0;
                while (elapsed < delayMs && !token.IsCancellationRequested)
                {
                    if (currentPollingUrl != BaseUrl || !_isLavaLampOnScreen) return;

                    await Task.Delay(interval, token);
                    elapsed += interval;
                }
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
                    // Stale / identical structural checks executed within thread safety limits
                    if (newUnixMs < _lastSetUnixMs && newColor == _targetColor && newLive == _isLive)
                        return;

                    if (newUnixMs == _lastSetUnixMs && newColor == _targetColor && newLive == _isLive)
                        return;

                    _targetColor = newColor;
                    _isLive = newLive;
                    _lastSetUnixMs = newUnixMs;
                }
            }
            catch (Exception ex)
            {
                ModContent.GetInstance<Neurosama>().Logger.Debug($"LavaLamp Parse Error: {ex.Message}");
                HandleFallbackTransition();
            }
        }
    }
}