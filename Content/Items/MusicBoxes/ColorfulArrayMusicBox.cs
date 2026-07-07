using Terraria.ID;
using Terraria.ModLoader;

namespace Neurosama.Content.Items.MusicBoxes
{
    public class ColorfulArrayMusicBox : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.CanGetPrefixes[Type] = false; // music boxes can't get prefixes in vanilla
            ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.MusicBox; // recorded music boxes transform into the basic form in shimmer

            MusicLoader.AddMusicBox(Mod, MusicLoader.GetMusicSlot(Mod, "Assets/Music/Colorful_Array"), ModContent.ItemType<ColorfulArrayMusicBox>(), ModContent.TileType<Tiles.MusicBoxes.ColorfulArrayMusicBox>());
        }

        public override void SetDefaults()
        {
            Item.DefaultToMusicBox(ModContent.TileType<Tiles.MusicBoxes.ColorfulArrayMusicBox>(), 0);
        }
    }
}
