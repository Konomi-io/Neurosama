using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Utilities;
using Neurosama.Common.Configs;

namespace Neurosama.Content.NPCs
{
    public class ErmFish : ModNPC
    {
        private const int ClonedNPCID = NPCID.Goldfish; // Life is so easy

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = Main.npcFrameCount[ClonedNPCID];
            Main.npcCatchable[Type] = true;

            NPCID.Sets.CountsAsCritter[Type] = true;
            NPCID.Sets.TakesDamageFromHostilesWithoutBeingFriendly[Type] = true;
            NPCID.Sets.TownCritter[Type] = true;

            // Shimmer into faeling, like other critters
            NPCID.Sets.ShimmerTransformToNPC[Type] = NPCID.Shimmerfly;

            // Immune to confused like goldfish
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;

            // Influences how the NPC looks in the Bestiary
            NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new()
            {
                Position = new Vector2(0, 4),
                IsWet = true,
                Velocity = 1f,
                Direction = -1
            };

            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);

            // Add to the end of critter list
            NPCID.Sets.NormalGoldCritterBestiaryPriority.Append(Type);
        }

        public override void SetDefaults()
        {
            NPC.CloneDefaults(ClonedNPCID);

            // width = 12;
            NPC.height = 20;
            // HitSound = SoundID.NPCHit1; // TODO
            // DeathSound = SoundID.NPCDeath1; // TODO

            NPC.catchItem = ModContent.ItemType<Items.Consumables.ErmFish>();
            AIType = ClonedNPCID;
            AnimationType = ClonedNPCID;

            Banner = Type;
            BannerItem = ModContent.ItemType<Items.Furniture.Banners.ErmFishBanner>();
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange([
                // Sets the spawning conditions and background for the bestiary entry
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Ocean,

				// Beastiary element from localisation key		
				new FlavorTextBestiaryInfoElement(Language.GetTextValue("Mods.Neurosama.Bestiary.ErmFish"))
            ]);
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            // disable spawns config
            if (!ModContent.GetInstance<NeurosamaServerConfig>().ErmphibianSpawns)
            {
                return 0f; // Completely disables the spawn
            }

            return SpawnCondition.Ocean.Chance * 0.09975f; // 399/400 * 0.1f
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 6; i++)
                {
                    Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Worm, 2 * hit.HitDirection, -2f);
                    if (Main.rand.NextBool(2))
                    {
                        dust.noGravity = true;
                        dust.scale = 1.2f * NPC.scale;
                    }
                    else
                    {
                        dust.scale = 0.7f * NPC.scale;
                    }
                }
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>($"{Name}_Gore_Head").Type, NPC.scale);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>($"{Name}_Gore_Leg").Type, NPC.scale);
            }
        }
    }
}
