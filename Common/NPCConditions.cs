using Terraria;
using Terraria.ModLoader;
using Neurosama.Content.NPCs.Town;

namespace Neurosama.Common
{
    public static class NPCConditions
    {
        public static readonly Condition VedalPresent = new Condition(
            "Mods.Neurosama.NPCConditions.VedalPresent",
            () => NPC.AnyNPCs(ModContent.NPCType<Vedal>())
        );

        public static readonly Condition NeuroPresent = new Condition(
            "Mods.Neurosama.NPCConditions.NeuroPresent",
            () => NPC.AnyNPCs(ModContent.NPCType<Neuro>())
        );

        public static readonly Condition EvilPresent = new Condition(
            "Mods.Neurosama.NPCConditions.EvilPresent",
            () => NPC.AnyNPCs(ModContent.NPCType<Evil>())
        );
    }
}