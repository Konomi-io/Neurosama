using Microsoft.Xna.Framework;
using Neurosama.Content.NPCs;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace Neurosama.Content.Tiles
{
    public class ErmSharkStatue : ModTile
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

            // Find the coordinates of top left tile
            int topLeftX = i - tile.TileFrameX / 18 % TileWidth;
            int topLeftY = j - tile.TileFrameY / 18 % TileHeight;

            // SkipWire all tile coordinates covered by this tile to make sure it doesnt activate multiple times
            for (int y = topLeftY; y < topLeftY + TileHeight; y++)
            {
                for (int x = topLeftX; x < topLeftX + TileWidth; x++)
                {
                    Wiring.SkipWire(x, y);
                }
            }

            int spawnX = (topLeftX + TileWidth / 2) * 16;
            int spawnY = (topLeftY + TileHeight) * 16;
            int npcIndex = -1;

            int spawnedNpcId = ModContent.NPCType<ErmShark>();

            // Check for statue cooldown and entity limit
            if (Wiring.CheckMech(topLeftX, topLeftY, 30) && NPC.MechSpawn(spawnX, spawnY, spawnedNpcId))
            {
                // Checks if a 6x3 area around the statue is free of solid tiles
                if (!Collision.SolidTiles(topLeftX - 2, topLeftX + TileWidth + 1, topLeftY, topLeftY + TileHeight - 1))
                {
                    npcIndex = NPC.NewNPC(Wiring.GetNPCSource(topLeftX, topLeftY), spawnX, spawnY - 8, spawnedNpcId); // -8 to adjust for ermshark height
                }
                else
                {
                    // Not enough space, spawn poof of smoke
                    Vector2 position = new Vector2(spawnX - 4, spawnY - 22) - new Vector2(10f);
                    Utils.PoofOfSmoke(position);
                    NetMessage.SendData(MessageID.PoofOfSmoke, -1, -1, null, (int)position.X, position.Y); // Not sure if this line is needed, copied from vanilla code
                }
            }

            // Adjust spawned NPC properties if spawn was successful
            if (npcIndex >= 0)
            {
                var npc = Main.npc[npcIndex];

                npc.value = 0f;
                npc.npcSlots = 0f;
                npc.SpawnedFromStatue = true; // Affects drops and catchability
                npc.CanBeReplacedByOtherNPCs = true;
            }
        }
    }
}
