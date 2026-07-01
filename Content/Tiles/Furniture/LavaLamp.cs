using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace Neurosama.Content.Tiles.Furniture
{
    public class LavaLamp : ModTile
    {
        private const int ClonedTileID = TileID.LavaLamp;
        private static Asset<Texture2D> glowTexture;

        public override void SetStaticDefaults()
        {
            Main.tileLighted[Type] = true;
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = true;

            // Copy from lava lamp
            TileObjectData.newTile.CopyFrom(TileObjectData.GetTileData(ClonedTileID, 0));
            TileObjectData.newTile.StyleLineSkip = 7;

            TileObjectData.addTile(Type);

            AddMapEntry(new Color(182, 177, 178), ModContent.GetInstance<Items.Furniture.LavaLamp>().DisplayName);

            glowTexture = ModContent.Request<Texture2D>(Texture + "_Glow");
        }

        
        public override bool CreateDust(int i, int j, ref int type)
        {
            return false;
        }

        public override void AnimateTile(ref int frame, ref int frameCounter)
        {
            if (++frameCounter >= 8)
            {
                frameCounter = 0;
                frame = ++frame % 7;
            }
        }

        public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
        {   
            frameYOffset = Main.tileFrame[type] * 36;
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            var tile = Main.tile[i, j];

            if (!TileDrawing.IsVisible(tile))
            {
                return;
            }

            //Color color = Main.DiscoColor;
            Color color = LavaLampColor.CurrentColor;
            color.A = 0;

            Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);

            int width = 16;
            int offsetY = 0;
            int height = 16;
            short frameX = tile.TileFrameX;
            short frameY = tile.TileFrameY;
            int addFrX = 0;
            int addFrY = 0;

            TileLoader.SetDrawPositions(i, j, ref width, ref offsetY, ref height, ref frameX, ref frameY); // calculates the draw offsets
            TileLoader.SetAnimationFrame(Type, i, j, ref addFrX, ref addFrY); // calculates the animation offsets

            Rectangle drawRectangle = new(tile.TileFrameX, tile.TileFrameY + addFrY, 16, 16);

            spriteBatch.Draw(glowTexture.Value, new Vector2(i * 16 - (int)Main.screenPosition.X, j * 16 - (int)Main.screenPosition.Y + offsetY) + zero, drawRectangle, color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            const float factor = 100f / (255f * 255f);

            /*
            r = Main.DiscoColor.R * factor;
            g = Main.DiscoColor.G * factor;
            b = Main.DiscoColor.B * factor;
            */
            // Grab the color directly from the global system!
            Color color = LavaLampColor.CurrentColor;
            r = color.R * factor;
            g = color.G * factor;
            b = color.B * factor;
        }
    }
}
