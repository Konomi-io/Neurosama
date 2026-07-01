using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace Neurosama.Common.Configs
{
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

        [Header("LavaLamp")]
        [DefaultValue(false)]
        public bool UseTestServer;
        [DefaultValue(false)]
        public bool UseDiscoColorWhenNoNeuroStream;

    }
}
