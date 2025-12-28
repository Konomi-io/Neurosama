using Terraria;
using Terraria.GameContent.UI;
using Terraria.ID;
using Terraria.Audio;
using Terraria.ModLoader;
using Neurosama.Content.EmoteBubbles;
using Neurosama.Content.NPCs.Town;
using Neurosama.Common.Configs;

public class EmoteSounds : GlobalEmoteBubble
{
    public override void OnSpawn(EmoteBubble bubble)
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        if (ModContent.GetInstance<NeurosamaConfig>().EmoteSoundsToggle == false)
            return;
        // add modded emote sounds here
        // Options: "Neuro", "Evil", "Twins", "TwinsAndPlayer"
        PlayModEmoteSound<NeweroEmote>(bubble, "fx_groantube", "Twins");
        PlayModEmoteSound<NewlivEmote>(bubble, "fx_metalpipe", "Evil"); //temporary until pipes emote is drawn
            // voicelines
        PlayModEmoteSound<ErmEmote>(bubble, "evil_erm", "Evil");
        PlayModEmoteSound<ErmEmote>(bubble, "neuro_erm", "Neuro");
        
        PlayModEmoteSound<HeartEmote>(bubble, "evil_heart", "Evil");
        PlayModEmoteSound<HeartEmote>(bubble, "neuro_heart!!", "Neuro");

        PlayModEmoteSound<FrickEmote>(bubble, "evil_frick", "Evil");
        PlayModEmoteSound<FrickEmote>(bubble, "neuro_frick", "Neuro");

        PlayModEmoteSound<SmileEmote>(bubble, "evil_smile", "Evil");
        PlayModEmoteSound<SmileEmote>(bubble, "neuro_smile", "Neuro");

        PlayModEmoteSound<FocusEmote>(bubble, "evil_focus!!", "Evil");
        PlayModEmoteSound<FocusEmote>(bubble, "neuro_hmm!", "Neuro");

        // add vanilla emote sounds here
        PlayVanillaEmoteSound(134, bubble, "fx_wompwompwomp", "Twins"); //sad
        PlayVanillaEmoteSound(139, bubble, "fx_clownhorn", "Twins"); //silly
        PlayVanillaEmoteSound(11, bubble, "fx_incorrectbuzzer", "Twins"); //curse 
        PlayVanillaEmoteSound(17, bubble, "fx_correctbuzzer", "Twins"); //music
        PlayVanillaEmoteSound(81, bubble, "fx_vineboom", "Twins"); //dynamite
        PlayVanillaEmoteSound(10, bubble, "fx_awkwardcrickets", "Twins"); //silent (the emote is called silent but the sound is not)
        PlayVanillaEmoteSound(0, bubble, "fx_rizz", "Twins"); //heart (red)
        PlayVanillaEmoteSound(138, bubble, "fx_scaryviolin", "Twins"); //scowl
        PlayVanillaEmoteSound(78, bubble, "fx_poke", "Twins"); //sword
        PlayVanillaEmoteSound(13, bubble, "fx_sus", "Twins"); //slime (it looks like the visor of the crewmate trust)
        PlayVanillaEmoteSound(84, bubble, "fx_deathbell", "Twins"); //tombstone
        PlayVanillaEmoteSound(15, bubble, "fx_badumtss", "Twins"); //laugh
    }

    private void PlayModEmoteSound<T>(
        EmoteBubble bubble,
        string soundName,
        string speaker
    ) where T : ModEmoteBubble
    {
        int emoteType = ModContent.EmoteBubbleType<T>();

        if (bubble.emote != emoteType)
            return;

        PlayEmoteSound(bubble, soundName, speaker);
    }

     private void PlayVanillaEmoteSound(
        int emoteID,
        EmoteBubble bubble,
        string soundName,
        string speaker
    )
    {
        if (bubble.emote != emoteID)
            return;

        PlayEmoteSound(bubble, soundName, speaker);
    }

    private void PlayEmoteSound(
        EmoteBubble bubble,
        string soundName,
        string speaker
    )
    {

        if (bubble.anchor.entity is not Player && bubble.anchor.entity is not NPC)
            return;

        if (speaker != "TwinsAndPlayer" && bubble.anchor.entity is Player)
            return;
        
        if (bubble.anchor.entity is NPC npc){

        bool isNeuro = npc.type == ModContent.NPCType<Neuro>();
        bool isEvil  = npc.type == ModContent.NPCType<Evil>();

        if (speaker == "Neuro" && !isNeuro)
            return;

        if (speaker == "Evil" && !isEvil)
            return;

        if (speaker == "Twins" && !isEvil && !isNeuro)
            return;

        if (speaker == "TwinsAndPlayer" && !isEvil && !isNeuro)
            return;

        }

        SoundEngine.PlaySound(
            new SoundStyle($"Neurosama/Assets/Sounds/{soundName}"),
            bubble.anchor.entity.Center
        );

    }
}
