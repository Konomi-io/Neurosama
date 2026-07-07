using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Neurosama.Content.Items.Furniture
{
    public class LavaLamp : ModItem
    {
        private static Asset<Texture2D> glowTexture;

        public override void SetStaticDefaults()
        {
            glowTexture = ModContent.Request<Texture2D>(Texture + "_Glow");
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<Tiles.Furniture.LavaLamp>());

            Item.width = 32;
            Item.height = 32;
            Item.rare = ItemRarityID.Cyan;
            // only 64.99!
            Item.value = Item.buyPrice(0, 0, 64, 99);
        }

        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Color color = Main.DiscoColor;
            color.A = 0;

            spriteBatch.Draw(glowTexture.Value, position, frame, color, 0, origin, scale, SpriteEffects.None, 0);
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Main.GetItemDrawFrame(Item.type, out var itemTexture, out var itemFrame);
            Vector2 origin = itemFrame.Size() / 2f;
            Vector2 drawPosition = Item.Bottom - Main.screenPosition - new Vector2(0, origin.Y);

            Color color = Main.DiscoColor;
            color.A = 0;

            spriteBatch.Draw(glowTexture.Value, drawPosition, itemFrame, color, rotation, origin, scale, SpriteEffects.None, 0);
        }
    }
}
