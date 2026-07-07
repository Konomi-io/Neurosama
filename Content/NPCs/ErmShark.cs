using Microsoft.Xna.Framework;
using Neurosama.Content.Items.Consumables;
using System;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Utilities;
using Terraria.Utilities.Terraria.Utilities;
using Neurosama.Common.Configs;

namespace Neurosama.Content.NPCs
{
    public class ErmShark : ModNPC
    {
        // would probably be better to modify the npc after spawning for depth and family
        public ref float Family => ref NPC.ai[2];

        public int GetDepth() { return (int)NPC.ai[3]; }

        // Splits 3 times normally, 4 times on expert, and FTW adds 2 extra splits
        public int GetMaxDepth() { return (Main.getGoodWorld ? 2 : 0) + (Main.expertMode ? 4 : 3); }

        public bool MaxDepthReached() { return GetDepth() >= GetMaxDepth(); }

        public bool IsLastDescendant()
        {
            // this considers the original parent (1 ermShark) as the end of the family (also 1 ermShark), so we need to check for depth as well
            int familySize = Main.npc.Count(x => x.active && x.type == NPC.type && x.ai[2] == Family);
            return familySize == 1 && MaxDepthReached();
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 6;

            NPCID.Sets.StatueSpawnedDropRarity[Type] = 0.2f; // 0.2x drops when spawned from statue (consistent with shark)

            // Centre ermshark's bestiary icon
            NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new()
            {
                Position = new Vector2(16, 8),
                PortraitPositionXOverride = 0,
                IsWet = true,
                Velocity = 1f,
                Direction = -1
            };

            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);
        }

        public override void SetDefaults()
        {
            NPC.width = 62;
            NPC.height = 32;
            NPC.damage = 20;
            NPC.defense = 4;
            NPC.lifeMax = 240;
            NPC.value = 500f;
            NPC.aiStyle = NPCAIStyleID.Piranha; // AI style used by sharks
            NPC.noGravity = true;
            NPC.HitSound = SoundID.NPCHit1; // TODO
            NPC.DeathSound = SoundID.NPCDeath1; // TODO, also maybe different sound for last ermshark?

            AIType = NPCID.Shark; // Moves the same as a shark. Could potentailly add ai that differs individually so splits feel better

            Banner = Type;
            BannerItem = ModContent.ItemType<Items.Furniture.Banners.ErmSharkBanner>();
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange([
                // Sets the spawning conditions and background for the bestiary entry
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Ocean,

				// Beastiary element from localisation key		
				new FlavorTextBestiaryInfoElement(Language.GetTextValue("Mods.Neurosama.Bestiary.ErmShark"))
            ]);
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            // disable spawns config
            if (!ModContent.GetInstance<NeurosamaServerConfig>().ErmphibianSpawns)
            {
                return 0f; // Completely disables the spawn
            }

            return SpawnCondition.OceanMonster.Chance * 0.02f;
        }

        public override void OnSpawn(IEntitySource source)
        {
            // Set health
            if (!Main.getGoodWorld)
            {
                // Halves max health for each split
                NPC.lifeMax >>= GetDepth();

            }
            else
            {
                // divides max health by 1.5 for each split on FTW
                NPC.lifeMax = (int)(NPC.lifeMax / Math.Pow(1.5, GetDepth()));
            }

            // Should never happen normally, but make sure the health is at least 1
            NPC.lifeMax = Math.Max(NPC.lifeMax, 1);

            NPC.life = NPC.lifeMax;

            // Give first ermshark a family id
            if (GetDepth() == 0)
            {
                Family = Main.rand.NextFloat();
                //CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Center.Y, 0, 0), new Color(191, 191, 255), family.ToString());
            }

            // Give immunity on spawn so it doesnt instantly die to piercing damage
            Array.Fill(NPC.immune, 10);
        }

        public override bool SpecialOnKill()
        {
            if (IsLastDescendant()) // TODO: Maybe check if no new ermsharks exist after split rather than depth for the case where entity cap is full
            {
                return false;
            }

            // Split into 2 ermsharks if not at max depth
            if (!MaxDepthReached())
            {
                var entitySource = NPC.GetSource_FromAI();
                int newDepth = GetDepth() + 1;

                // Some X variance is added so they arent in the same spot
                // This line randomises whether the higher one is on the left or right
                int spawnOffset = Main.rand.NextBool() ? -12 : 12;

                // Erm Erm
                NPC child1 = NPC.NewNPCDirect(entitySource, (int)NPC.Center.X + spawnOffset, (int)NPC.Center.Y, Type, NPC.whoAmI, ai2: Family, ai3: newDepth);
                NPC child2 = NPC.NewNPCDirect(entitySource, (int)NPC.Center.X - spawnOffset, (int)NPC.Center.Y + 16, Type, NPC.whoAmI, ai2: Family, ai3: newDepth);

                // Preserve statue-related properties from parent
                child1.SpawnedFromStatue = NPC.SpawnedFromStatue;
                child2.SpawnedFromStatue = NPC.SpawnedFromStatue;

                child1.CanBeReplacedByOtherNPCs = NPC.CanBeReplacedByOtherNPCs;
                child2.CanBeReplacedByOtherNPCs = NPC.CanBeReplacedByOtherNPCs;

                child1.npcSlots = NPC.npcSlots;
                child2.npcSlots = NPC.npcSlots;

                child1.value = NPC.value;
                child2.value = NPC.value;

                // Apply velocity (and therefore knockback) to children, with some variance
                child1.velocity = Vector2.Multiply(NPC.velocity, Main.rand.NextFloat(new FloatRange(0.91f, 1.1f)));
                child2.velocity = Vector2.Multiply(NPC.velocity, Main.rand.NextFloat(new FloatRange(0.91f, 1.1f)));

                Utils.PoofOfSmoke(NPC.position);
            }

            // Don't run onkill as it's not the last ermshark in the family
            return true;
        }

        public override void OnKill()
        {
            //CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Center.Y, 0, 0), new Color(255, 191, 191), family.ToString());
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            //npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<Items.Furniture.NeuroCatErm>(), 8, 8));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<MountainBountyPizza>(), 8));
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;
            if (NPC.frameCounter >= 30)
            {
                NPC.frameCounter = 0;
            }
            NPC.frame.Y = (int)NPC.frameCounter / 6 * frameHeight;

            NPC.spriteDirection = NPC.direction;
        }
    }
}
