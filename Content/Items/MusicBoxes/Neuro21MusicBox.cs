using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Neurosama.Content.Items.MusicBoxes
{
    public class Neuro21MusicBox : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.CanGetPrefixes[Type] = false; // music boxes can't get prefixes in vanilla
            ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.MusicBox; // recorded music boxes transform into the basic form in shimmer

            MusicLoader.AddMusicBox(Mod, MusicLoader.GetMusicSlot(Mod, "Assets/Music/silenceNeuro21"), ModContent.ItemType<Neuro21MusicBox>(), ModContent.TileType<Tiles.MusicBoxes.Neuro21MusicBox>());
        }

        public override void SetDefaults()
        {
            Item.DefaultToMusicBox(ModContent.TileType<Tiles.MusicBoxes.Neuro21MusicBox>(), 0);
        }
    }
}
