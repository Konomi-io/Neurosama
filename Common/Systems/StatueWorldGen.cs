using Neurosama.Content.Tiles;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using Neurosama.Common.Configs;

namespace Neurosama.Common.Systems
{
    // Add statue to the statue pool
    public class StatueWorldGen : ModSystem
    {
        public override void Load()
        {
            On_WorldGen.SetupStatueList += On_WorldGen_SetupStatueList;
        }

        private void On_WorldGen_SetupStatueList(On_WorldGen.orig_SetupStatueList orig)
        {
            orig();

            // config check
            if (!ModContent.GetInstance<NeurosamaServerConfig>().StatueGeneration)
            {
                return;
            }

            int startIndex = GenVars.statueList.Length;

            // statues array for worldgen.
            // set shouldBeWired to true to make traps.
            (int type, bool shouldBeWired, ushort placeStyle)[] statueTypesToAdd = [
                (ModContent.TileType<ErmFishStatue>(), false, 0),
                (ModContent.TileType<ErmSharkStatue>(), false, 0),
            ];

            Array.Resize(ref GenVars.statueList, GenVars.statueList.Length + statueTypesToAdd.Length);

            for (int i = 0; i < statueTypesToAdd.Length; i++)
            {
                int arrayIndex = startIndex + i;
                (int statueType, bool shouldBeWired, ushort placeStyle) = statueTypesToAdd[i];

                GenVars.statueList[arrayIndex] = new Point16(statueType, placeStyle);

                if (shouldBeWired)
                {
                    GenVars.StatuesWithTraps.Add(arrayIndex);
                }
            }
        }
    }
}
