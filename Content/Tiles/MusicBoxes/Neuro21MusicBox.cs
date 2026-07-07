using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Terraria.ID;
using Neurosama.Common;
using Terraria.GameContent.ObjectInteractions;


namespace Neurosama.Content.Tiles.MusicBoxes
{
    public class Neuro21MusicBox : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileObsidianKill[Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.Origin = new Point16(0, 1);
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.newTile.StyleLineSkip = 2;
            TileObjectData.addTile(Type);

            // 1. Tell Terraria this tile has 2 distinct animation frames
            Main.tileFrameCounter[Type] = 0;

            // Use vanilla "Music Box" text for map entry
            AddMapEntry(new Color(191, 142, 111), Language.GetText("ItemName.MusicBox"));
        }

        // 2. Control the global animation timer
        public override void AnimateTile(ref int frame, ref int frameCounter)
        {
            // How many ticks a single frame lasts (e.g., 10 ticks = 6 frames per second)
            const int ticksPerFrame = 10; 
            
            frameCounter++;
            if (frameCounter >= ticksPerFrame)
            {
                frameCounter = 0;
                frame++;
                if (frame >= 2) // Reset back to frame 0 after frame 1
                {
                    frame = 0;
                }
            }
        }

        // 3. Apply the animation frame to this specific tile instance
        public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
        {
            // Your texture should have the second frame directly below the first frame.
            // Since it's a 2x2 tile, each frame is 36 pixels high (2 blocks * 16px + padding/borders).
            // Main.tileFrame[type] holds the current frame number (0 or 1) calculated in AnimateTile.
            frameYOffset = Main.tileFrame[type] * 36; 
        }

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<Items.MusicBoxes.Neuro21MusicBox>();
        }
        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings)
        {
            return true;
        }

        public override void EmitParticles(int i, int j, Tile tileCache, short tileFrameX, short tileFrameY, Color tileLight, bool visible)
        {
            // This code spawns the music notes when the music box is open.
            Tile tile = Main.tile[i, j];

            if (!visible || tile.TileFrameX != 36 || tile.TileFrameY % 36 != 0 || (int)Main.timeForVisualEffects % 7 != 0 || !Main.rand.NextBool(3))
            {
                return;
            }

            int MusicNote = Main.rand.Next(570, 573);
            Vector2 SpawnPosition = new Vector2(i * 16 + 8, j * 16 - 8);
            Vector2 NoteMovement = new Vector2(Main.WindForVisuals * 2f, -0.5f);
            NoteMovement.X *= Main.rand.NextFloat(0.5f, 1.5f);
            NoteMovement.Y *= Main.rand.NextFloat(0.5f, 1.5f);
            switch (MusicNote)
            {
                case 572:
                    SpawnPosition.X -= 8f;
                    break;
                case 571:
                    SpawnPosition.X -= 4f;
                    break;
            }

            Gore.NewGore(new EntitySource_TileUpdate(i, j), SpawnPosition, NoteMovement, MusicNote, 0.8f);
        }
    }
}
