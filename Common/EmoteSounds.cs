using Terraria;
using Terraria.GameContent.UI;
using Terraria.ID;
using Terraria.Audio;
using Terraria.ModLoader;
using Neurosama.Content.EmoteBubbles;
using Neurosama.Content.NPCs.Town;
using Neurosama.Common.Configs;

namespace Neurosama.Common
{
    [System.Flags]
    internal enum EmoteOrigin
    {
        None = 0,
        Evil = 1 << 0,
        Neuro = 1 << 1,
        Player = 1 << 2,
        Twins = Evil | Neuro,
        TwinsAndPlayer = Twins | Player
    }

    public class EmoteSounds : GlobalEmoteBubble
    {
        public override void OnSpawn(EmoteBubble bubble)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            if (ModContent.GetInstance<NeurosamaConfig>().EmoteSoundsVolume == 0f)
                // Emote sound volume is 0, skip emote sound playback  
                return;

            if (ModContent.GetInstance<NeurosamaConfig>().EmoteSFXToggle)
            {
                PlayEmoteSound(ModContent.EmoteBubbleType<NeweroEmote>(), bubble, "SFX/groantube", EmoteOrigin.Twins);
                PlayEmoteSound(ModContent.EmoteBubbleType<PipesEmote>(), bubble, "SFX/metalpipe", EmoteOrigin.Evil);
                PlayEmoteSound(EmoteID.EmoteSadness, bubble, "SFX/wompwompwomp", EmoteOrigin.Twins);
                PlayEmoteSound(EmoteID.EmoteSilly, bubble, "SFX/clownhorn", EmoteOrigin.Twins);
                PlayEmoteSound(EmoteID.DebuffCurse, bubble, "SFX/incorrectbuzzer", EmoteOrigin.Twins);
                PlayEmoteSound(EmoteID.EmoteNote, bubble, "SFX/correctbuzzer", EmoteOrigin.Twins);
                PlayEmoteSound(EmoteID.ItemDynamite, bubble, "SFX/vineboom", EmoteOrigin.Twins);
                PlayEmoteSound(EmoteID.DebuffSilence, bubble, "SFX/awkwardcrickets", EmoteOrigin.Twins);
                PlayEmoteSound(ModContent.EmoteBubbleType<NeurizzEmote>(), bubble, "SFX/rizz", EmoteOrigin.Twins);
                PlayEmoteSound(EmoteID.EmoteFear, bubble, "SFX/scaryviolin", EmoteOrigin.Twins);
                //PlayEmoteSound(EmoteID.ItemSword, bubble, "SFX/poke", EmoteOrigin.Twins);   
                PlayEmoteSound(EmoteID.CritterSlime, bubble, "SFX/sus", EmoteOrigin.Twins); //(it looks like the visor of the crewmate trust)  
                PlayEmoteSound(EmoteID.ItemTombstone, bubble, "SFX/deathbell", EmoteOrigin.Twins); // extremely rare, only used by default when like 5% health which they will probably die or heal before using)  
                PlayEmoteSound(EmoteID.EmoteLaugh, bubble, "SFX/badumtss", EmoteOrigin.Twins);
            }

            if (ModContent.GetInstance<NeurosamaConfig>().EmoteVoiceToggle)
            {
                PlayEmoteSound(ModContent.EmoteBubbleType<ErmEmote>(), bubble, "evil_erm", EmoteOrigin.Evil);
                PlayEmoteSound(ModContent.EmoteBubbleType<ErmEmote>(), bubble, "neuro_erm", EmoteOrigin.Neuro);

                PlayEmoteSound(ModContent.EmoteBubbleType<HeartEmote>(), bubble, "evil_heart", EmoteOrigin.Evil);
                PlayEmoteSound(ModContent.EmoteBubbleType<HeartEmote>(), bubble, "neuro_heart!!", EmoteOrigin.Neuro);
                PlayEmoteSound(ModContent.EmoteBubbleType<FrickEmote>(), bubble, "evil_frick", EmoteOrigin.Evil);
                PlayEmoteSound(ModContent.EmoteBubbleType<FrickEmote>(), bubble, "neuro_frick", EmoteOrigin.Neuro);

                PlayEmoteSound(ModContent.EmoteBubbleType<SmileEmote>(), bubble, "evil_smile", EmoteOrigin.Evil);
                PlayEmoteSound(ModContent.EmoteBubbleType<SmileEmote>(), bubble, "neuro_smile", EmoteOrigin.Neuro);

                PlayEmoteSound(ModContent.EmoteBubbleType<FocusEmote>(), bubble, "evil_focus!!", EmoteOrigin.Evil);
                PlayEmoteSound(ModContent.EmoteBubbleType<FocusEmote>(), bubble, "neuro_hmm!", EmoteOrigin.Neuro);
            }
        }

        private static void PlayEmoteSound(
            int emoteID,
            EmoteBubble bubble,
            string soundName,
            EmoteOrigin speaker
        )
        {
            if (bubble.emote != emoteID)
                return;

            if (bubble.anchor.entity is not Player && bubble.anchor.entity is not NPC)
                return;

            if (bubble.anchor.entity is Player && !speaker.HasFlag(EmoteOrigin.Player))
                return;

            if (bubble.anchor.entity is NPC npc)
            {
                bool isNeuro = npc.type == ModContent.NPCType<Neuro>();
                bool isEvil = npc.type == ModContent.NPCType<Evil>();

                if (!isNeuro && !isEvil)
                    return;

                if (isNeuro && !speaker.HasFlag(EmoteOrigin.Neuro))
                    return;

                if (isEvil && !speaker.HasFlag(EmoteOrigin.Evil))
                    return;
            }

            float volume = ModContent.GetInstance<NeurosamaConfig>().EmoteSoundsVolume;
            SoundEngine.PlaySound(
                new SoundStyle($"{nameof(Neurosama)}/Assets/Sounds/{soundName}") { Volume = volume },
                bubble.anchor.entity.Center
            );
        }
    }
}