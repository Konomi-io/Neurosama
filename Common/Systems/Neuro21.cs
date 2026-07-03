using Terraria;
using Terraria.ModLoader;
using Terraria.Localization;
using System.Text.RegularExpressions;

namespace Neurosama.Common
{
    public class Neuro21 : ModSystem
    {
        private string _lastChattedSong = "";

        // Tracks the unique music slot dynamically looked up from tModLoader's registry
        public static int NeuroRadioMusicSlot
        {
            get
            {
                // This checks the asset registry directly using the exact same path 
                // you used to register the music box item!
                return MusicLoader.GetMusicSlot(ModContent.GetInstance<Neuro21>().Mod, "Assets/Music/silenceNeuro21");
            }
        }

        public override void PostUpdateEverything()
        {
            const string ChannelId = "Neuro21";
            const string StreamUrl = "https://radio.twinskaraoke.com/listen/neuro_21/radio.mp3";

            if (Main.gameMenu || NeuroRadioMusicSlot <= 0)
            {
                Mp3StreamManager.StopStream(ChannelId);
                return;
            }

            float currentTrackFade = Main.musicFade[NeuroRadioMusicSlot];

            if (Main.curMusic == NeuroRadioMusicSlot || currentTrackFade > 0.01f)
            {
                // Start using our specific Channel key ID
                Mp3StreamManager.StartStream(ChannelId, StreamUrl);

                var channel = Mp3StreamManager.GetChannel(ChannelId);
                if (channel != null && channel.IsStreaming)
                {
                    // This passes the fade multiplier straight down to the instance runner
                    channel.Volume = Main.musicVolume * currentTrackFade;

                    // CLIENT CHAT METADATA INTERCEPT:
                    string incomingTitle = channel.CurrentSongTitle;

                    if (!string.IsNullOrEmpty(incomingTitle) && _lastChattedSong != incomingTitle)
                    {
                        // Update the memory immediately so it sticks across stream disconnects/reconnects
                        _lastChattedSong = incomingTitle;

                        // Print to chat
                        // find original artist
                        int firstDash = incomingTitle.IndexOf(" - ");
                        string title = "";
                        string originalArtist = "";
                        if (firstDash >= 0)
                        {
                            originalArtist = incomingTitle[..firstDash];
                            title = incomingTitle[(firstDash + 3)..];
                        } 
                        // scuffed song with no credited artist
                        else
                        {
                            title = incomingTitle;
                        }
                        
                        // find karaoke artist
                        int parenStart = title.LastIndexOf('(');

                        string coloredOriginalArtists = ColorizeArtistString(originalArtist);

                        // Color karaoke singer names
                        if (parenStart >= 0 && title.EndsWith(")") && !Regex.IsMatch(originalArtist, @"neuro|vedal|evil", RegexOptions.IgnoreCase))
                        {
                            string titlePart = title[..parenStart].TrimEnd();
                            string karaokeArtists = title[(parenStart + 1)..^1];

                            string[] parts = karaokeArtists.Split(" & ");

                            string coloredArtists = "";

                            for (int i = 0; i < parts.Length; i++)
                            {
                                if (i > 0)
                                    coloredArtists += " [c/C5B3E0:&] ";

                                string karaokeArtist = parts[i].Trim();

                                coloredArtists += GetColoredArtist(karaokeArtist);
                            }

                            Main.NewText(
                                $"[c/9D5CFF:[]"+
                                $"[c/CE64CE:N][c/D364C9:e][c/D965C3:u][c/DE66BE:r][c/E467B8:o] " +
                                $"[c/EF69AD:2][c/F469A8:1] " +
                                $"[c/FF6B9D:S][c/F473A4:t][c/E97BAA:a][c/DE83B1:t][c/D48AB7:i][c/C992BE:o][c/BE9AC4:n]" +
                                $"[c/9D5CFF:]] " +
                                $"[c/FF6B9D:{Language.GetTextValue("Mods.Neurosama.NowPlaying")}] " +
                                $"{coloredOriginalArtists} " +
                                $"[c/FFFFFF:-] " +
                                $"[c/C5B3E0:{titlePart}] " +
                                $"[c/FFFFFF:(]" +
                                coloredArtists +
                                $"[c/FFFFFF:)]"
                            );
                        }
                        // Official songs/covers
                        else
                        {
                            if (firstDash >= 0)
                            {
                                Main.NewText(
                                    $"[c/9D5CFF:[]" +
                                    $"[c/CE64CE:N][c/D364C9:e][c/D965C3:u][c/DE66BE:r][c/E467B8:o] " +
                                    $"[c/EF69AD:2][c/F469A8:1] " +
                                    $"[c/FF6B9D:S][c/F473A4:t][c/E97BAA:a][c/DE83B1:t][c/D48AB7:i][c/C992BE:o][c/BE9AC4:n]" +
                                    $"[c/9D5CFF:]] " +
                                    $"[c/FF6B9D:{Language.GetTextValue("Mods.Neurosama.NowPlaying")}] " +
                                    $"{coloredOriginalArtists} [c/FFFFFF:-] [c/C5B3E0:{title}]"
                                );
                            }
                            //no dashes?
                            else
                            {
                                Main.NewText(
                                    $"[c/9D5CFF:[]" +
                                    $"[c/CE64CE:N][c/D364C9:e][c/D965C3:u][c/DE66BE:r][c/E467B8:o] " +
                                    $"[c/EF69AD:2][c/F469A8:1] " +
                                    $"[c/FF6B9D:S][c/F473A4:t][c/E97BAA:a][c/DE83B1:t][c/D48AB7:i][c/C992BE:o][c/BE9AC4:n]" +
                                    $"[c/9D5CFF:]] " +
                                    $"[c/FF6B9D:{Language.GetTextValue("Mods.Neurosama.NowPlaying")}] " +
                                    $"[c/C5B3E0:{incomingTitle}]"
                                );
                            }
                        }
                    }
                }
            }
            else
            {
                Mp3StreamManager.StopStream(ChannelId);
                //_lastChattedSong = ""; // Reset when the stream stops completely
            }
        }

        public string ColorizeArtistString(string rawArtistGroup)
        {
            if (string.IsNullOrWhiteSpace(rawArtistGroup)) return "";

            // Regex pattern captures delimiters as independent array items using matching parentheses.
            // It searches for: , and / ,and / & / \bx\b / \band\b
            string pattern = @"(,\s*and\s+|,\s*|\s*&\s*|\s*/\s*|\s+x\s+|\s+and\s+)";

            string[] tokens = Regex.Split(rawArtistGroup, pattern, RegexOptions.IgnoreCase);
            string result = "";

            foreach (string token in tokens)
            {
                // If it matches our delimiters, print it as a standard uncolored/soft text element
                if (Regex.IsMatch(token, pattern, RegexOptions.IgnoreCase))
                {
                    result += $"[c/C5B3E0:{token}]";
                }
                else if (!string.IsNullOrWhiteSpace(token))
                {
                    // Clean trailing/leading spaces on the name for formatting safety, but preserve it for GetColoredArtist
                    result += GetColoredArtist(token.Trim());
                }
                else
                {
                    result += token; // Carry over basic spacing tokens if any
                }
            }

            return result;
        }

        public string GetColoredArtist(string artist)
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
                _ => "C5B3E0" //default
            };

            return $"[c/{colorHex}:{artist}]";
        }

    }
}