using System.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Neurosama.Common
{
    public class TownNPCRespawnSystem : ModSystem
    {
        private static bool unlockedNeuroSpawn = false;
        private static bool unlockedEvilSpawn = false;

        public static bool UnlockedNeuroSpawn
        {
            get => unlockedNeuroSpawn;
            set => unlockedNeuroSpawn = value;
        }

        public static bool UnlockedEvilSpawn
        {
            get => unlockedEvilSpawn;
            set => unlockedEvilSpawn = value;
        }

        public override void ClearWorld()
        {
            unlockedNeuroSpawn = false;
            unlockedEvilSpawn = false;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag[nameof(unlockedNeuroSpawn)] = unlockedNeuroSpawn;
            tag[nameof(unlockedEvilSpawn)] = unlockedEvilSpawn;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            unlockedNeuroSpawn = tag.GetBool(nameof(unlockedNeuroSpawn));
            unlockedEvilSpawn = tag.GetBool(nameof(unlockedEvilSpawn));
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.WriteFlags(unlockedNeuroSpawn);
            writer.WriteFlags(unlockedEvilSpawn);
        }

        public override void NetReceive(BinaryReader reader)
        {
            reader.ReadFlags(out unlockedNeuroSpawn);
            reader.ReadFlags(out unlockedEvilSpawn);
        }
    }
}
