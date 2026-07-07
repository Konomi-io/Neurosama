using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Neurosama.Content.EmoteBubbles;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace Neurosama.Content.NPCs.Town
{
    [AutoloadHead]
    public class Vedal : ModNPC
    {
        //private static int ShimmerHeadIndex;
        private static Profiles.StackedNPCProfile NPCProfile;

        private static SoundStyle deathSound = new($"{nameof(Neurosama)}/Assets/Sounds/vedal_cry");
        private static SoundStyle hitSound = new($"{nameof(Neurosama)}/Assets/Sounds/vedal_noise");

        public override void Load()
        {
            // Adds the Shimmer Head to the NPCHeadLoader
            //ShimmerHeadIndex = Mod.AddNPCHeadTexture(Type, Texture + "_Shimmer_Head");
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 28;
            NPCID.Sets.ExtraFramesCount[Type] = 21; // The number of frames after the walking frames.
            NPCID.Sets.AttackFrameCount[Type] = 0; // Town Pets don't have any attacking frames.
            NPCID.Sets.ExtraTextureCount[Type] = 0;

            NPCID.Sets.DangerDetectRange[Type] = 250;
            NPCID.Sets.AttackType[Type] = -1;
            NPCID.Sets.AttackTime[Type] = -1;
            NPCID.Sets.AttackAverageChance[Type] = 1;

            NPCID.Sets.ShimmerTownTransform[Type] = false; // No shimmer variant atm

            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Shimmer] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;

            NPCID.Sets.NPCFramingGroup[Type] = 4; // Base party hat walking animation is taken from Town Cat

            NPCID.Sets.IsTownPet[Type] = true;
            NPCID.Sets.CannotSitOnFurniture[Type] = true;

            NPCID.Sets.TownNPCBestiaryPriority.Add(Type);

            NPCID.Sets.FaceEmote[Type] = ModContent.EmoteBubbleType<VedalEmote>();

            // Influences how the NPC looks in the Bestiary
            NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new()
            {
                Velocity = 1f,
                Direction = -1
            };

            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);

            NPCProfile = new Profiles.StackedNPCProfile(
                new Profiles.DefaultNPCProfile(Texture, NPCHeadLoader.GetHeadSlot(HeadTexture))/*,
                new Profiles.DefaultNPCProfile(Texture + "_Shimmer", ShimmerHeadIndex)*/
            );
        }

        public override void SetDefaults()
        {
            NPC.townNPC = true;
            NPC.friendly = true;
            NPC.width = 18;
            NPC.height = 20;
            NPC.aiStyle = NPCAIStyleID.Passive;
            NPC.damage = 10;
            NPC.defense = 15;
            NPC.lifeMax = 250;
            NPC.HitSound = hitSound;
            NPC.DeathSound = deathSound;
            NPC.knockBackResist = 0.5f;
            NPC.housingCategory = 1; // Can share a house with a normal Town NPC. Could be cool to make it only neuro or evil
            AnimationType = NPCID.TownBunny;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange([
				// Preferred biome
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,

				// Beastiary element from localisation key		
				new FlavorTextBestiaryInfoElement(Language.GetTextValue("Mods.Neurosama.Bestiary.Vedal"))
            ]);
        }

        public override bool CanTownNPCSpawn(int numTownNPCs)
        {
            // Check if Neuro or Evil is present in the world
            int evilNPC = NPC.FindFirstNPC(ModContent.NPCType<Evil>());
            int neuroNPC = NPC.FindFirstNPC(ModContent.NPCType<Neuro>());

            if (evilNPC != -1 || neuroNPC != -1)
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

            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Vedal.StandardDialogue1"));
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Vedal.StandardDialogue2"));
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Vedal.StandardDialogue3"));
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Vedal.StandardDialogue4"));
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Vedal.StandardDialogue5"));
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Vedal.StandardDialogue6"));
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Vedal.StandardDialogue7"));
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Vedal.StandardDialogue8"));
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Vedal.StandardDialogue9"));
            chat.Add(Language.GetTextValue("Mods.Neurosama.Dialogue.Vedal.StandardDialogue10"));

            return chat;
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            // Remove the pet button, which is there by default for town pets
            button = "";
        }

        public override void PartyHatPosition(ref Vector2 position, ref SpriteEffects spriteEffects)
        {
            int frameOffsetX;
            int frameOffsetY;

            // Calculate offset for specfic animation frames
            // A case for sitting on a chair is not needed as it is handled in PreDraw()
            var frame = NPC.frame;
            switch (frame.Y / frame.Height)
            {
                case 11: // Hiding frame 2
                case 15: // Hiding frame 6
                    frameOffsetX = 4;
                    frameOffsetY = 8;
                    break;

                case 12: // Hiding frame 3
                case 13: // Hiding frame 4
                case 14: // Hiding frame 5
                    frameOffsetX = 6;
                    frameOffsetY = 8;
                    break;

                case 17: // Drinking frame 1
                case 25: // Drinking frame 9
                    frameOffsetX = 4;
                    frameOffsetY = -4;
                    break;
                case 18: // Drinking frame 2
                    frameOffsetX = 4;
                    frameOffsetY = -6;
                    break;
                case 19: // Drinking frame 3
                case 24: // Drinking frame 8
                    frameOffsetX = 0;
                    frameOffsetY = -8;
                    break;
                case 20: // Drinking frame 4
                case 22: // Drinking frame 6
                case 23: // Drinking frame 7
                    frameOffsetX = 0;
                    frameOffsetY = -10;
                    break;
                case 21: // Drinking frame 5
                    frameOffsetX = 0;
                    frameOffsetY = -12;
                    break;
                case 26: // Drinking frame 10
                    frameOffsetX = 4;
                    frameOffsetY = 0;
                    break;

                // Default position
                default:
                    frameOffsetX = 4;
                    frameOffsetY = 6;
                    break;
            }

            position.X += frameOffsetX * NPC.direction;
            position.Y += frameOffsetY;
        }

        public override void FindFrame(int frameHeight)
        {
            // Use extra frame when sitting on a chair
            if (NPC.ai[0] == 5f)
            {
                NPC.frame.Y = 27 * frameHeight;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (NPC.ai[0] == 5f)
            {
                // Draw vedal manually for custom sit position
                Texture2D vedalTexture = TextureAssets.Npc[Type].Value;

                SpriteEffects spriteEffect = NPC.direction == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                Vector2 vedalDrawPosition = NPC.Center - screenPos + new Vector2(7f * NPC.direction, -22f);

                spriteBatch.Draw(vedalTexture, vedalDrawPosition, NPC.frame, drawColor, NPC.rotation, NPC.frame.Size() / 2, NPC.scale, spriteEffect, 0f);

                // Draw the party hat if applicable
                var partyHatColor = (int)NPC.GetPartyHatColor();
                if (partyHatColor != 0)
                {
                    var hatIndex = partyHatColor == 1 ? 0 : partyHatColor + 14; // Convert to correct index of the hats texture

                    var hatTexture = TextureAssets.Extra[ExtrasID.TownNPCHats].Value;
                    var hatframe = hatTexture.Frame(20, 1, hatIndex); // Get the correct frame for the party hat color

                    Vector2 hatDrawPosition = vedalDrawPosition + new Vector2(-1f, -20f);

                    spriteBatch.Draw(hatTexture, hatDrawPosition, hatframe, drawColor, NPC.rotation, hatframe.Size() / 2, NPC.scale, spriteEffect, 0f);
                }

                return false;
            }

            return true;
        }

        public override void ChatBubblePosition(ref Vector2 position, ref SpriteEffects spriteEffects)
        {
            if (NPC.ai[0] == 5f)
            {
                position.X += 7 * NPC.direction;
                position.Y += -30;
            }
            else
            {
                position.X += 4 * NPC.direction;
                position.Y += 4;
            }
        }
    }
}
