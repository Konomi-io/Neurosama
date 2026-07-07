using Terraria;
using Terraria.ModLoader;
using Terraria.Localization;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Neurosama.Common
{
    public class RadioStationManager : ModSystem
    {
        private class RadioStations
        {
            public string ChannelId { get; set; }
            public string StreamUrl { get; set; }
            public string MusicAssetPath { get; set; }
            public float VolumeMultiplier { get; set; } = 1f;
            public string RawPrefixFormat { get; set; }
            public string SongNameColor { get; set; }
            public string DashAndParenthesesColor { get; set; }
            public bool ColorKaraokeSingerName { get; set; } = false;
            public string LastChattedSong { get; set; } = "";
        }

        private List<RadioStations> _stations;

        public override void Load()
        {
            if (Main.dedServ) return;
            
            _stations = new List<RadioStations>
            {
                new RadioStations
                {
                    ChannelId = "Neuro21",
                    StreamUrl = "https://radio.twinskaraoke.com/listen/neuro_21/radio.mp3",
                    MusicAssetPath = "Assets/Music/silenceNeuro21",
                    VolumeMultiplier = 1f,
                    ColorKaraokeSingerName = true,
                    SongNameColor = "C5B3E0",
                    DashAndParenthesesColor = "FFFFFF",
                    RawPrefixFormat = $"[c/9D5CFF:[]" +
                                      $"[c/CE64CE:N][c/D364C9:e][c/D965C3:u][c/DE66BE:r][c/E467B8:o] " +
                                      $"[c/EF69AD:2][c/F469A8:1] " +
                                      $"[c/FF6B9D:S][c/F473A4:t][c/E97BAA:a][c/DE83B1:t][c/D48AB7:i][c/C992BE:o][c/BE9AC4:n]" +
                                      $"[c/9D5CFF:]] " +
                                      "[c/FF6B9D:{0}] " // {0} will be replaced by now playing (localization is loaded after the station list, so it must be done this way)
                },
                new RadioStations
                {
                    ChannelId = "TruckersFM",
                    StreamUrl = "https://live.truckers.fm/",
                    MusicAssetPath = "Assets/Music/silenceTruckersFM",
                    VolumeMultiplier = 0.5f, //truckersfm is loud lol
                    ColorKaraokeSingerName = false,
                    SongNameColor = "D9D9D9",
                    DashAndParenthesesColor = "F316B0",
                    RawPrefixFormat = $"[c/F316B0:[]" +
                                      $"[c/D9D9D9:truckers]" +
                                      $"[c/F316B0:.]" +
                                      $"[c/D9D9D9:fm]" +
                                      $"[c/F316B0:]] " +
                                      "[c/A37ECE:{0}] " // {0} will be replaced by now playing (localization is loaded after the station list, so it must be done this way)
                }
            };
        }

        public override void PostUpdateEverything()
        {
            if (Main.dedServ) return;

            if (Main.gameMenu)
            {
                foreach (var station in _stations)
                {
                    Mp3StreamManager.StopStream(station.ChannelId);
                }
                return;
            }

            foreach (var station in _stations)
            {
                int musicSlot = MusicLoader.GetMusicSlot(Mod, station.MusicAssetPath);

                if (musicSlot <= 0)
                {
                    Mp3StreamManager.StopStream(station.ChannelId);
                    continue;
                }

                float currentTrackFade = Main.musicFade[musicSlot];

                if (Main.curMusic == musicSlot || currentTrackFade > 0.01f)
                {
                    Mp3StreamManager.StartStream(station.ChannelId, station.StreamUrl);

                    var channel = Mp3StreamManager.GetChannel(station.ChannelId);
                    if (channel != null && channel.IsStreaming)
                    {
                        channel.Volume = Main.musicVolume * currentTrackFade * station.VolumeMultiplier;

                        string incomingTitle = channel.CurrentSongTitle;

                        if (!string.IsNullOrEmpty(incomingTitle) && station.LastChattedSong != incomingTitle && ModContent.GetInstance<Common.Configs.NeurosamaConfig>().DisplayNowPlaying)
                        {
                            station.LastChattedSong = incomingTitle;

                            int firstDash = incomingTitle.IndexOf(" - ");
                            string title = "";
                            string originalArtist = "";

                            if (firstDash >= 0)
                            {
                                originalArtist = incomingTitle[..firstDash];
                                title = incomingTitle[(firstDash + 3)..];
                            }
                            else
                            {
                                title = incomingTitle;
                            }

                            string coloredOriginalArtists = ColorizeArtistString(originalArtist, station.SongNameColor);
                            int parenStart = title.LastIndexOf('(');
                            string songDetails = "";

                            // unofficial Karaoke/Covers (Neuroverse only)
                            if (parenStart >= 0 && title.EndsWith(")") && !Regex.IsMatch(originalArtist, @"neuro|vedal|evil", RegexOptions.IgnoreCase) && station.ColorKaraokeSingerName)
                            {
                                string titlePart = title[..parenStart].TrimEnd();
                                string karaokeArtists = title[(parenStart + 1)..^1];
                                string[] parts = karaokeArtists.Split(" & ");
                                string coloredArtists = "";

                                for (int i = 0; i < parts.Length; i++)
                                {
                                    if (i > 0)
                                        coloredArtists += $" [c/{station.SongNameColor}:&] ";

                                    string karaokeArtist = parts[i].Trim();
                                    coloredArtists += GetColoredArtist(karaokeArtist, station.SongNameColor);
                                }

                                songDetails = $"{coloredOriginalArtists} [c/{station.DashAndParenthesesColor}:-] [c/{station.SongNameColor}:{titlePart}] [c/{station.DashAndParenthesesColor}:(]{coloredArtists}[c/{station.DashAndParenthesesColor}:)]";
                            }
                            else
                            {
                                //official songs/covers
                                if (firstDash >= 0)
                                {
                                    songDetails = $"{coloredOriginalArtists} [c/{station.DashAndParenthesesColor}:-] [c/{station.SongNameColor}:{title}]";
                                }
                                //no dashes?
                                else
                                {
                                    songDetails = $"[c/{station.SongNameColor}:{incomingTitle}]";
                                }
                            }
                            string finalStationPrefix = string.Format(station.RawPrefixFormat, Language.GetTextValue("Mods.Neurosama.NowPlaying"));

                            Main.NewText(finalStationPrefix + songDetails);
                        }
                    }
                }
                else
                {
                    Mp3StreamManager.StopStream(station.ChannelId);
                }
            }
        }

        public string ColorizeArtistString(string rawArtistGroup, string SongNameColor)
        {
            if (string.IsNullOrWhiteSpace(rawArtistGroup)) return "";

            string pattern = @"(,\s*and\s+|,\s*|\s*&\s*|\s*/\s*|\s+x\s+|\s+and\s+)";
            string[] tokens = Regex.Split(rawArtistGroup, pattern, RegexOptions.IgnoreCase);
            string result = "";

            foreach (string token in tokens)
            {
                if (Regex.IsMatch(token, pattern, RegexOptions.IgnoreCase))
                {
                    result += $"[c/{SongNameColor}:{token}]";
                }
                else if (!string.IsNullOrWhiteSpace(token))
                {
                    result += GetColoredArtist(token.Trim(), SongNameColor);
                }
                else
                {
                    result += token;
                }
            }

            return result;
        }

        public string GetColoredArtist(string artist, string SongNameColor)
        {
            string colorHex = artist.Trim().ToLowerInvariant().Replace(" ", "") switch
            {
                //canonically collabed
                "neuro" or "neuro-sama" or "neurosama" => "FF8AB6",
                "neurov1" or "hiyori" => "777777",
                "evil" or "evilneuro" => "E83750",
                "vedal" or "vedal987" => "97D091",
                "minikomew" or "mini" or "miniko" => "F9FD8C",
                "cerber" or "cerbervt" => "A550DF",
                "annytf" or "anny" => "F6DBE9",
                "miyune" or "miyu" => "1A90B4",
                "camila" => "FB5AC4",
                "willstetson" => "4FCDFF",
                "numi" or "nihmune" or "akumanihmune" => "7E71A6",
                "shylily" => "89C0F8",
                "lucypyre" or "lucy" => "BB2B2C",
                "baothewhale" or "bao" or "baovtuber" => "77ADDC",
                "obkatiekat" or "katie" => "E4B9A8",
                "queenpb" or "pb" or "mixedbyqueenpb" => "995176",
                //futureproofing
                "ellieminibot" or "ellie" => "92FFF6",
                "chrchie" => "BC93F9",
                "shoomimi" => "FDCBE6",
                "fallenshadow" or "shondo" => "A56288",
                "moniibagel" or "monii" => "89C5C5",
                "magemimi" or "mimi" or "meotashi" => "8F83B1",
                "laynalazar" or "layna" => "7D1321",
                "toma" or "tomavtuber" or "tomaliketomato" => "FF98AD",
                "takanashikiara" or "kiara" or "kiaratakanashi" => "E96433",
                "hatsunemiku" or "miku" or "mikuhatsune" => "33BBAD",
                "kasaneteto" or "teto" or "tetokasane" => "D3455C",
                _ => SongNameColor //if artist isnt listed, just use regular song color
            };

            return $"[c/{colorHex}:{artist}]";
        }
    }
}
