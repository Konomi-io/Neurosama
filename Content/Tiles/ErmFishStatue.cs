using Microsoft.Xna.Framework;
using Neurosama.Content.NPCs;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace Neurosama.Content.Tiles
{
    public class ErmFishStatue : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileObsidianKill[Type] = true;
            Main.tileSpelunker[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;
            TileID.Sets.IsAMechanism[Type] = true; // Ensures that this tile and connected pressure plate won't be removed during the "Remove Broken Traps" worldgen step

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2xX);
            TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;

            TileObjectData.newTile.StyleWrapLimit = 2;
            TileObjectData.newTile.StyleMultiplier = 2;
            TileObjectData.newTile.StyleHorizontal = true;

            // Examplemod forgot to add an offset for the statue
            TileObjectData.newTile.DrawYOffset = 2;

            // Examplemod also forgot to add a flipped version
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;

            // Add tiles
            TileObjectData.addAlternate(1);
            TileObjectData.addTile(Type);

            DustType = DustID.Stone;

            AddMapEntry(new Color(144, 148, 144), Language.GetText("MapObject.Statue"));
        }

        public override void HitWire(int i, int j)
        {
            Tile tile = Main.tile[i, j];

            const int TileWidth = 2;
            const int TileHeight = 3;

            // Find the coordinates of top left tile square through math
            int topLeftX = i - tile.TileFrameX / 18 % TileWidth;
            int topLeftY = j - tile.TileFrameY / 18;

            // SkipWire all tile coordinates covered by this tile to make sure it doesnt activate multiple times
            for (int y = topLeftY; y < topLeftY + TileHeight; y++)
            {
                for (int x = topLeftX; x < topLeftX + TileWidth; x++)
                {
                    Wiring.SkipWire(x, y);
                }
            }

            float spawnX = (topLeftX + TileWidth * 0.5f) * 16;
            float spawnY = (topLeftY + TileHeight) * 16;

            var entitySource = new EntitySource_TileUpdate(topLeftX, topLeftY, context: "ErmFishStatue");

            int spawnedNpcId = ModContent.NPCType<ErmFish>();

            int npcIndex = -1;
            if (Wiring.CheckMech(topLeftX, topLeftY, 30) && NPC.MechSpawn(spawnX, spawnY, spawnedNpcId))
            {
                npcIndex = NPC.NewNPC(entitySource, (int)spawnX, (int)spawnY - 12, spawnedNpcId);
            }

            if (npcIndex >= 0)
            {
                var npc = Main.npc[npcIndex];

                npc.value = 0f;
                npc.npcSlots = 0f; // Statue enemies don't take up npc slots
                npc.SpawnedFromStatue = true; // Makes drops and catchability consistent with statue enemies
            }
        }
    }
}