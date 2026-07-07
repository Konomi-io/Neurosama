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
    internal class ErmFishGold : ModNPC
    {
        private const int ClonedNPCID = NPCID.GoldGoldfish;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = Main.npcFrameCount[ClonedNPCID];
            Main.npcCatchable[Type] = true;

            NPCID.Sets.CountsAsCritter[Type] = true;
            NPCID.Sets.TakesDamageFromHostilesWithoutBeingFriendly[Type] = true;
            NPCID.Sets.TownCritter[Type] = true;
            NPCID.Sets.GoldCrittersCollection.Add(Type);

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

            // Goes after ermfish in bestiary?
            NPCID.Sets.NormalGoldCritterBestiaryPriority.Append(Type);
            //NPCID.Sets.NormalGoldCritterBestiaryPriority.Insert(NPCID.Sets.NormalGoldCritterBestiaryPriority.IndexOf(ModContent.NPCType<ErmFish>()) + 1, Type);
        }

        public override void SetDefaults()
        {
            NPC.CloneDefaults(ClonedNPCID);

            // width = 12;
            NPC.height = 20;
            // HitSound = SoundID.NPCHit1; // TODO
            // DeathSound = SoundID.NPCDeath1; // TODO

            NPC.catchItem = ModContent.ItemType<Items.Consumables.ErmFishGold>();
            NPC.lavaImmune = false;
            AIType = ClonedNPCID;
            AnimationType = ClonedNPCID;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange([
                // Sets the spawning conditions and background for the bestiary entry
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Ocean,

				// Beastiary element from localisation key		
				new FlavorTextBestiaryInfoElement(Language.GetTextValue("Mods.Neurosama.Bestiary.ErmFishGold"))
            ]);
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            // disable spawns config
            if (!ModContent.GetInstance<NeurosamaServerConfig>().ErmphibianSpawns)
            {
                return 0f; // Completely disables the spawn
            }

            return SpawnCondition.Ocean.Chance * 0.00025f; // 1/400 * 0.1f
        }

        public override void AI()
        {
            // Taken from vanilla source
            if (Main.netMode != NetmodeID.Server)
            {
                NPC.position += NPC.netOffset;
                Color color = Lighting.GetColor((int)NPC.Center.X / 16, (int)NPC.Center.Y / 16);
                if (color.R > 20 || color.B > 20 || color.G > 20)
                {
                    int num = color.R;
                    if (color.G > num)
                    {
                        num = color.G;
                    }
                    if (color.B > num)
                    {
                        num = color.B;
                    }
                    num /= 30;
                    if (Main.rand.Next(300) < num)
                    {
                        int num2 = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.TintableDustLighted, 0f, 0f, 254, new Color(255, 255, 0), 0.5f);
                        Main.dust[num2].velocity *= 0f;
                    }
                }
                NPC.position -= NPC.netOffset;
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            // Taken from vanilla source
            if (NPC.life > 0)
            {
                for (int i = 0; i < 10; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, Main.rand.Next(232, 234), hit.HitDirection, -1f);
                }
            }
            else
            {
                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, Main.rand.Next(232, 234), 2 * hit.HitDirection, -2f);
                }
            }
        }
    }
}
