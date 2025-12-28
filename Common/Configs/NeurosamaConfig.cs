using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace Neurosama.Common.Configs
{
	public class NeurosamaConfig : ModConfig
	{
		// ConfigScope.ClientSide should be used for client side, usually visual or audio tweaks.
		// ConfigScope.ServerSide should be used for basically everything else, including disabling items or changing NPC behaviors
		public override ConfigScope Mode => ConfigScope.ClientSide;

		// The things in brackets are known as "Attributes".

		[Header("Audio")] // Headers are like titles in a config. You only need to declare a header on the item it should appear over, not every item in the category. 
		//[Label("Emote Sounds")] // A label is the text displayed next to the option. This should usually be a short description of what it does. By default all ModConfig fields and properties have an automatic label translation key, but modders can specify a specific translation key.
		// [Tooltip("$Some.Key")] // A tooltip is a description showed when you hover your mouse over the option. It can be used as a more in-depth explanation of the option. Like with Label, a specific key can be provided.
		[DefaultValue(true)]
		//[ReloadRequired]
		public bool EmoteSoundsToggle;
	}
}
