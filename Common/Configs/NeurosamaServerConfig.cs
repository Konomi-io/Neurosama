using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace Neurosama.Common.Configs
{
    public class NeurosamaServerConfig : ModConfig
    {
        // ConfigScope.ServerSide should be used for server side, usually gameplay-related tweaks.
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Header("Ermphibians")]

        [DefaultValue(true)]
        public bool ErmphibianSpawns;

        [DefaultValue(true)]
        public bool StatueGeneration;
    }
}
