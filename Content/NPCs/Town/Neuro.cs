using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    public class Neuro : ModNPC
    {
        public const string ShopName = "Shop";

        private static string[] Textures;
        private static int[] HeadIndexes;

        private static ITownNPCProfile NPCProfile;

        private static SoundStyle deathSound = new($"{nameof(Neurosama)}/Assets/Sounds/neuro_ooo");
        private static SoundStyle hitSound = new($"{nameof(Neurosama)}/Assets/Sounds/neuro_erf");

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
            Main.npcFrameCount[Type] = 23;
            NPCID.Sets.NPCFramingGroup[Type] = 1; // Uses same party hat offset as party girl

            NPCID.Sets.IsTownChild[Type] = true; // Makes NPC death work like the Angler & Princess

            NPCID.Sets.ExtraFramesCount[Type] = 9;
            NPCID.Sets.AttackFrameCount[Type] = 4;
            NPCID.Sets.DangerDetectRange[Type] = 80; // Neuro should be close to the enemy when she tries to attack
            NPCID.Sets.AttackType[Type] = 3; // Melee
            NPCID.Sets.AttackTime[Type] = 30;
            NPCID.Sets.AttackAverageChance[Type] = 15;

            NPCID.Sets.ShimmerTownTransform[Type] = true; // NPC has a shimmered form

            NPCID.Sets.FaceEmote[Type] = ModContent.EmoteBubbleType<NeuroEmote>();

            // Influences how the NPC looks in the Bestiary
            NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new()
            {
                Velocity = 1f,
                Direction = -1
            };

            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);

            // TODO: better happiness thingies
            NPC.Happiness
                .SetNPCAffection<Evil>(AffectionLevel.Love) // cute sisters
                .SetNPCAffection(NPCID.Merchant, AffectionLevel.Dislike)
            ;

            HeadIndexes[0] = NPCHeadLoader.GetHeadSlot(HeadTexture); // Head texture is now loaded, add index to array
            ModContent.GetInstance<Neurosama>().Logger.Info(HeadIndexes[0]);

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

            AnimationType = NPCID.PartyGirl;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange([
				// Preferred biome
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,

				// Beastiary element from localisation key		
				new FlavorTextBestiaryInfoElement(Language.GetTextValue("Mods.Neurosama.Bestiary.Neuro"))
            ]);
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
                // Unlock neuro as she has spawned
                TownNPCRespawnSystem.UnlockedNeuroSpawn = true;
            }
        }

        public override bool CanTownNPCSpawn(int numTownNPCs)
        {
            if (TownNPCRespawnSystem.UnlockedNeuroSpawn)
            {
                // Neuro has spawned in the world before, don't need to check conditions
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

            int evilNPC = NPC.FindFirstNPC(ModContent.NPCType<Evil>());

            if (evilNPC != -1)
            {
                string evilNPCName = Main.npc[evilNPC].FullName;

                // Dialogue for if Evil is in world
                chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Neuro.EvilDialogue1", evilNPCName));
                //chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Neuro.EvilDialogue2", evilNPCName));
            }

            int nurseNPC = NPC.FindFirstNPC(NPCID.Nurse);

            if (nurseNPC != -1)
            {
                string nurseNPCName = Main.npc[nurseNPC].FullName;

                // Dialogue for if The Nurse is in world
                chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Neuro.NurseDialogue", nurseNPCName));
            }

            if (Main.bloodMoon)
            {
                // Dialogue for if it's a blood moon
                chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Neuro.BloodMoonDialogue1"));
                //chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Neuro.BloodMoonDialogue2"));
            }

            if (Main.IsItRaining)
            {
                // Dialogue for if it's raining
                chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Neuro.RainDialogue1"));
                chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Neuro.RainDialogue2"));
            }

            // Regular dialogue
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Neuro.StandardDialogue1"));
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Neuro.StandardDialogue2"));
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Neuro.StandardDialogue3"));
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Neuro.StandardDialogue4"));
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Neuro.StandardDialogue5"));
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Neuro.StandardDialogue6"));
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Neuro.StandardDialogue7"));
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Neuro.StandardDialogue8"));
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Neuro.StandardDialogue9"));

            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Neuro.RareDialogue"), 0.25);

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
                .Add<Items.Furniture.NeuroCatErm>()
                .Add<Items.Furniture.NeuroFumo>()
                .Add<Items.Furniture.VedalFumo>(NPCConditions.VedalPresent)
                .Add<Items.Furniture.AbandonedArchive>(NPCConditions.VedalPresent)
                .Add<Items.Furniture.LavaLamp>()
                .Add<Items.Donowall>()
                .Add<Items.Donobrick>()
                .Add<NeuroMusicBox>()
                .Add<NeverMusicBox>(NPCConditions.EvilPresent)
                .Add<ColorfulArrayMusicBox>()
                .Add<Neuro21MusicBox>(NPCConditions.EvilPresent)
                .Add<TruckersFMMusicBox>(NPCConditions.VedalPresent)
                //.Add<Items.SwarmPet>()
                .Add<Items.Armor.NeuroVanityStockings>()
                .Add<Items.Armor.NeuroVanityUniform>()
                .Add<Items.Armor.NeuroVanityWig>()
            ;

            npcShop.Register();
        }

        // Queen statue only
        public override bool CanGoToStatue(bool toKingStatue) => !toKingStatue;

        public override void TownNPCAttackStrength(ref int damage, ref float knockback)
        {
            damage = 8;
            knockback = 16f;
        }

        public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
        {
            cooldown = 30;
            randExtraCooldown = 30;
        }

        public override void TownNPCAttackSwing(ref int itemWidth, ref int itemHeight)
        {
            // The hitbox of the melee swing
            itemWidth = 50;
            itemHeight = 50;
        }

        public override void DrawTownAttackSwing(ref Texture2D item, ref Rectangle itemFrame, ref int itemSize, ref float scale, ref Vector2 offset)
        {
            // Load the item texture
            Main.GetItemDrawFrame(ModContent.ItemType<BanHammer>(), out Texture2D itemTexture, out Rectangle itemRectangle);

            // Set the item texture to the item texture
            item = itemTexture;

            itemFrame = itemRectangle;

            itemSize = itemRectangle.Width;

            // How far the arc of the swing is from nuero
            scale = 0.15f;
        }

        /*public override void PostAI()
        {
            CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Center.Y, 0, 0), new Color(255, 191, 191), NPC.IsShimmerVariant.ToString());
        }*/

        public override int? PickEmote(Player closestPlayer, List<int> emoteList, WorldUIAnchor otherAnchor) {
            emoteList.Add(ModContent.EmoteBubbleType<NeurizzEmote>());

            emoteList.Add(ModContent.EmoteBubbleType<ErmEmote>());

            emoteList.Add(ModContent.EmoteBubbleType<HeartEmote>());

            emoteList.Add(ModContent.EmoteBubbleType<NeweroEmote>());

            emoteList.Add(ModContent.EmoteBubbleType<SmileEmote>());

            emoteList.Add(ModContent.EmoteBubbleType<TutelEmote>());

            emoteList.Add(ModContent.EmoteBubbleType<FrickEmote>());
            //emoteList.Add(ModContent.EmoteBubbleType<FocusEmote>());
            emoteList.Add(ModContent.EmoteBubbleType<NewlivEmote>());
            emoteList.Add(EmoteID.DebuffCurse);
            emoteList.Add(EmoteID.ItemDynamite);
            return null;
		}
        }
}