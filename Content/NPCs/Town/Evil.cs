using Microsoft.Xna.Framework;
using Neurosama.Common;
using Neurosama.Content.EmoteBubbles;
using Neurosama.Content.Items.MusicBoxes;
using Neurosama.Content.Items.Weapons;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.Personalities;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.GameContent.UI;

namespace Neurosama.Content.NPCs.Town
{
    [AutoloadHead]
    public class Evil : ModNPC
    {
        public const string ShopName = "Shop";

        private static string[] Textures;
        private static int[] HeadIndexes;

        private static ITownNPCProfile NPCProfile;

        private static SoundStyle deathSound = new($"{nameof(Neurosama)}/Assets/Sounds/evil_aaaa");
        private static SoundStyle hitSound = new($"{nameof(Neurosama)}/Assets/Sounds/evil_oh");

        public override void Load()
        {
            // Define the Variant Textures.
            Textures = [
               Texture,
               Texture + "_Shimmer",
               Texture + "_Neko",
               Texture + "_Neko_Shimmer",
               Texture + "_Frog",
               Texture + "_Frog_Shimmer",
               Texture + "_Duck",
               Texture + "_Duck_Shimmer",
            ];

            // Assert that textures array is of even length, so x % Textures.Length doesn't mess with shimmer state
            if (Textures.Length % 2 != 0)
            {
                throw new System.Exception($"{GetType().Name} Textures array length is not even! Each variant must have a respective shimmer variant.");
            }

            // Adds the Variant Heads to the NPCHeadLoader
            // The default head texture is added to the array later in SetStaticDefaults
            HeadIndexes = new int[Textures.Length];

            for (int i = 1; i < Textures.Length; i++)
            {
                HeadIndexes[i] = Mod.AddNPCHeadTexture(Type, Textures[i] + "_Head");
            }
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 23; // The total amount of frames the NPC has
            NPCID.Sets.NPCFramingGroup[Type] = 1; // Uses same party hat offset as party girl

            NPCID.Sets.IsTownChild[Type] = true; // Makes NPC death work like the Angler & Princess

            NPCID.Sets.ExtraFramesCount[Type] = 9;
            NPCID.Sets.AttackFrameCount[Type] = 4;
            NPCID.Sets.DangerDetectRange[Type] = 700; // The amount of pixels away from the center of the NPC that it tries to attack enemies
            NPCID.Sets.AttackType[Type] = 1; // Ranged
            NPCID.Sets.AttackTime[Type] = 60;
            NPCID.Sets.AttackAverageChance[Type] = 30; // The denominator for the chance for a Town NPC to attack

            NPCID.Sets.ShimmerTownTransform[Type] = true; // NPC has a shimmered form

            NPCID.Sets.FaceEmote[Type] = ModContent.EmoteBubbleType<EvilEmote>();

            // Influences how the NPC looks in the Bestiary
            NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new()
            {
                Velocity = 1f,
                Direction = -1
            };

            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);

            // TODO: better happiness thingies
            NPC.Happiness
                .SetBiomeAffection<SnowBiome>(AffectionLevel.Dislike)
                .SetNPCAffection<Neuro>(AffectionLevel.Love) // cute sisters
            ;

            HeadIndexes[0] = NPCHeadLoader.GetHeadSlot(HeadTexture); // Head texture is now loaded, add index to array
            NPCProfile = new TwinTownNPCProfile(Textures, HeadIndexes);
        }

        public override void SetDefaults()
        {
            NPC.townNPC = true; // Sets NPC to be a Town NPC
            NPC.friendly = true; // NPC Will not attack player
            NPC.width = 18;
            NPC.height = 40;
            NPC.aiStyle = NPCAIStyleID.Passive;
            NPC.damage = 10;
            NPC.defense = 15;
            NPC.lifeMax = 250;
            NPC.HitSound = hitSound;
            NPC.DeathSound = deathSound;
            NPC.knockBackResist = 0.5f;

            // Uses same animation frames as party girl
            AnimationType = NPCID.PartyGirl;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange([
				// Preferred biome
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,

				// Beastiary element from localisation key		
				new FlavorTextBestiaryInfoElement(Language.GetTextValue("Mods.Neurosama.Bestiary.Evil"))
            ]);
        }

        public override bool PreAI()
        {
            if (NPC.ai[3] > 0)
            {
                NPC.ai[3]--;
            }

            return true;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                // Create 4 random smoke coulds to mimic angler and princess gores.
                List<int> gores = [
                    GoreID.Smoke1,
                    GoreID.Smoke2,
                    GoreID.Smoke3,
                ];

                for (int k = 0; k < 4; k++)
                {
                    int randomGore = gores[Main.rand.Next(gores.Count)];
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, Vector2.Zero, randomGore, 1f);
                }
            }
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (source is EntitySource_SpawnNPC)
            {
                // Unlock eliv as she has spawned
                TownNPCRespawnSystem.UnlockedEvilSpawn = true;
            }
        }

        public override bool CanTownNPCSpawn(int numTownNPCs)
        {
            if (TownNPCRespawnSystem.UnlockedEvilSpawn)
            {
                // Evil has spawned in the world before, don't need to check conditions
                return true;
            }

            if (numTownNPCs >= 3)
            {
                return true;
            }

            return false;
        }

        public override ITownNPCProfile TownNPCProfile()
        {
            return NPCProfile;
        }

        public override string GetChat()
        {
            WeightedRandom<string> chat = new();

            double standardChatWeight = 1;

            // Add dialogue for if Neuro is in world.
            int neuroNPC = NPC.FindFirstNPC(ModContent.NPCType<Neuro>());

            if (neuroNPC != -1)
            {
                string neuroNPCName = Main.npc[neuroNPC].FullName;

                // Dialogue for if Neuro is in the world
                chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Evil.NeuroDialogue1", neuroNPCName));
                chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Evil.NeuroDialogue2", neuroNPCName));
            }

            if (Main.bloodMoon)
            {
                // Dialogue for if it's a blood moon
                chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Evil.BloodMoonDialogue1"));
                chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Evil.BloodMoonDialogue2"));
                chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Evil.BloodMoonDialogue3"));
                chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Evil.BloodMoonDialogue4"));

                standardChatWeight = 0.25;
            }

            // Regular dialogue
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Evil.StandardDialogue1"), standardChatWeight);
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Evil.StandardDialogue2"), standardChatWeight);
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Evil.StandardDialogue3"), standardChatWeight);
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Evil.StandardDialogue4"), standardChatWeight);
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Evil.StandardDialogue5"), standardChatWeight);
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Evil.StandardDialogue6"), standardChatWeight);
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Evil.StandardDialogue7"), standardChatWeight);
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Evil.StandardDialogue8"), standardChatWeight);
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Evil.StandardDialogue9"), standardChatWeight);
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Evil.StandardDialogue10"), standardChatWeight);

            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Evil.RareDialogue"), standardChatWeight * 0.25);

            return chat;
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = Language.GetTextValue("LegacyInterface.28");
            button2 = Language.GetTextValue("Mods.Neurosama.UI.ToggleOutfit");
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shop)
        {
            if (firstButton)
            {
                shop = ShopName;
                return;
            }

            ToggleVariant();

            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                // Send a packet to the server to sync the toggle
                ModPacket packet = Mod.GetPacket();
                packet.Write((byte)MessageType.ToggleTwinVariant);
                packet.Write(NPC.whoAmI);
                packet.Send();
            }
        }

        public void ToggleVariant()
        {
            // Switch to next variant
            NPC.townNpcVariationIndex = (NPC.townNpcVariationIndex + 2) % Textures.Length;
            Utils.PoofOfSmoke(NPC.position);
        }

        public override void AddShops()
        {
            var npcShop = new NPCShop(Type, ShopName)
                .Add<Items.Furniture.EvilFumo>()
                .Add<Items.Furniture.VedalFumo>()
                .Add<Items.Furniture.AbandonedArchive>()
                .Add<Items.Furniture.Donoplank>()
                .Add<Items.Donobrick>()
                .Add<BoomMusicBox>()
                .Add<NeverMusicBox>()
                .Add<SwarmDrone>(Condition.DownedEyeOfCthulhu)
                .Add<Items.Armor.EvilVanityStockings>()
                .Add<Items.Armor.EvilVanityUniform>()
                .Add<Items.Armor.EvilVanityWig>()
            ;

            npcShop.Register();
        }

        // Queen statue only
        public override bool CanGoToStatue(bool toKingStatue) => !toKingStatue;

        public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
        {
            cooldown = 30;
            randExtraCooldown = 30;
        }

        /*public override void DrawTownAttackGun(ref Texture2D item, ref Rectangle itemFrame, ref float scale, ref int horizontalHoldoutOffset)
        {
            // TODO: harpoon is positioned wrong
            int itemType = ItemID.Harpoon;
            Main.GetItemDrawFrame(itemType, out item, out itemFrame);
            horizontalHoldoutOffset = (int)Main.DrawPlayerItemPos(1f, itemType).X - 12;
        }*/

        public override void TownNPCAttackShoot(ref bool inBetweenShots)
        {
            NPC target = null;
            float lowestDistance = float.MaxValue;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC potentialTarget = Main.npc[i];
                float distance = NPC.Distance(potentialTarget.Center);
                if (potentialTarget.active && potentialTarget.CanBeChasedBy() && distance < NPCID.Sets.DangerDetectRange[NPC.type] && distance < lowestDistance && Collision.CanHitLine(NPC.Center, 0, 0, potentialTarget.Center, 0, 0) && NPC.localAI[3] % NPCID.Sets.AttackTime[NPC.type] == 0)
                {
                    target = potentialTarget;
                    lowestDistance = distance;
                }
            }

            if (target != null && Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Add large cooldown because it is reset when the harpoon dies
                NPC.ai[3] = 450f;

                var handPosition = NPC.Center + new Vector2(NPC.direction * 12f, 0f);
                var unitVectorToTarget = (target.Top - handPosition).SafeNormalize(Vector2.Zero); // Maybe add height to target based on distance for better aiming?
                var projectile = Projectile.NewProjectileDirect(NPC.GetSource_FromAI(), handPosition, unitVectorToTarget * 11f, ModContent.ProjectileType<Projectiles.EvilHarpoon>(), 20, 4f, ai2: NPC.whoAmI);
                projectile.npcProj = true;
                projectile.noDropItem = true;
            }
        }
        public override int? PickEmote(Player closestPlayer, List<int> emoteList, WorldUIAnchor otherAnchor) {
			emoteList.Add(ModContent.EmoteBubbleType<NeurizzEmote>());

            emoteList.Add(ModContent.EmoteBubbleType<PipesEmote>());

            emoteList.Add(ModContent.EmoteBubbleType<FrickEmote>());

            emoteList.Add(ModContent.EmoteBubbleType<FocusEmote>());

            emoteList.Add(ModContent.EmoteBubbleType<NewlivEmote>());

            emoteList.Add(ModContent.EmoteBubbleType<TutelEmote>());

            emoteList.Add(ModContent.EmoteBubbleType<ErmEmote>());
            emoteList.Add(ModContent.EmoteBubbleType<HeartEmote>());
            emoteList.Add(ModContent.EmoteBubbleType<NeweroEmote>());
            //emoteList.Add(ModContent.EmoteBubbleType<SmileEmote>());
            emoteList.Add(EmoteID.DebuffCurse);
            emoteList.Add(EmoteID.ItemDynamite);
            return null;
		}
        
    }
}