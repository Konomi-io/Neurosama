using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace Neurosama.Common.Configs
{
    public enum LavaLampServerTypeOptions
    {
        TestServer,
        MainServer,
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

        [DefaultValue(LavaLampServerTypeOptions.MainServer)]
        public LavaLampServerTypeOptions SelectedServerType { get; set; }

        [DefaultValue("https://api.neurolavalamp.com")]
        public string CustomServerUrl { get; set; }

        [DefaultValue(false)]
        public bool UseDiscoColorWhenNoNeuroStream { get; set; }

        [DefaultValue(true)]
        public bool StreamSync { get; set; }

        [Range(100, 1000)]
        [Increment(50)]
        [DefaultValue(500)]
        public int LavaLampLiveLatency { get; set; }

        [Range(10000, 60000)]
        [Increment(1000)]
        [DefaultValue(10000)]
        public int LavaLampOfflineLatency { get; set; }

    }
}
