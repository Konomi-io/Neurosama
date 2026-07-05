using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace Neurosama.Common.Configs
{
    public enum LavaLampServerTypeOptions
    {
        TestServer,
        RegularServer,
        CustomServer
    }
    
    public class NeurosamaConfig : ModConfig
	{
		// ConfigScope.ClientSide should be used for client side, usually visual or audio tweaks.
		public override ConfigScope Mode => ConfigScope.ClientSide;

		[Header("Audio")]

        [Range(0f, 1f)]
		[DefaultValue(.5f)]
        public float EmoteSoundsVolume;

        [DefaultValue(true)]
        public bool EmoteSFXToggle;

        [DefaultValue(true)]
        public bool EmoteVoiceToggle;

        [Header("LiveRadio")]
        [DefaultValue(false)]
        public bool KeepStreamingUnfocused;

        [DefaultValue(true)]
        public bool DisplayNowPlaying;

        [Header("LavaLamp")]

        [DefaultValue(LavaLampServerTypeOptions.RegularServer)]
        public LavaLampServerTypeOptions SelectedServerType { get; set; }

        [DefaultValue("https://api.neurolavalamp.com")]
        public string CustomServerUrl { get; set; }

        [DefaultValue(false)]
        public bool UseDiscoColorWhenNoNeuroStream;

    }
}
