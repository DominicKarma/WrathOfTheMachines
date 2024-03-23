using System;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Artemis;
using Luminance.Assets;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;

namespace DifferentExoMechs.Content.NPCs.ExoMechs
{
    public sealed partial class ArtemisBehaviorOverride : NPCBehaviorOverride, IExoTwin
    {
        /// <summary>
        /// Artemis' current frame.
        /// </summary>
        public int Frame
        {
            get;
            set;
        }

        /// <summary>
        /// Artemis' current AI timer.
        /// </summary>
        public int AITimer
        {
            get => (int)NPC.ai[1];
            set => NPC.ai[1] = value;
        }

        /// <summary>
        /// Whether Artemis has fully entered her second phase yet or not.
        /// </summary>
        public bool InPhase2
        {
            get => NPC.ai[0] == 1f;
            set => NPC.ai[0] = value.ToInt();
        }

        /// <summary>
        /// Whether Artemis has verified that Apollo is alive or not.
        /// </summary>
        public bool ApolloSummonCheckPerformed
        {
            get;
            set;
        }

        /// <summary>
        /// The opacity of wingtip vortices on Artemis.
        /// </summary>
        public float WingtipVorticesOpacity
        {
            get;
            set;
        }

        /// <summary>
        /// The intensity boost of thrusters for Artemis.
        /// </summary>
        public float ThrusterBoost
        {
            get;
            set;
        }

        /// <summary>
        /// Artemis's current animation.
        /// </summary>
        public ExoTwinAnimation Animation
        {
            get;
            set;
        } = ExoTwinAnimation.Idle;

        /// <summary>
        /// Artemis' specific draw action.
        /// </summary>
        public Action? SpecificDrawAction
        {
            get;
            set;
        }

        /// <summary>
        /// Artemis' optic nerve colors.
        /// </summary>
        public Color[] OpticNervePalette => [new(75, 14, 6), new(145, 35, 4), new(204, 101, 24), new(254, 172, 84), new(224, 147, 40)];

        /// <summary>
        /// Artemis' base texture.
        /// </summary>
        internal static LazyAsset<Texture2D> BaseTexture;

        /// <summary>
        /// Artemis' glowmask texture.
        /// </summary>
        internal static LazyAsset<Texture2D> Glowmask;

        public override int NPCOverrideID => ExoMechNPCIDs.ArtemisID;

        public override void SetStaticDefaults()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            BaseTexture = LazyAsset<Texture2D>.Request("DifferentExoMechs/Content/NPCs/ExoMechs/ArtemisAndApollo/Textures/Artemis");
            Glowmask = LazyAsset<Texture2D>.Request("DifferentExoMechs/Content/NPCs/ExoMechs/ArtemisAndApollo/Textures/ArtemisGlow");
        }

        public void ResetLocalStateData()
        {
            AITimer = 0;
            NPC.ai[2] = 0f;
            NPC.ai[3] = 0f;
            NPC.netUpdate = true;
        }

        public override void AI()
        {
            if (!ApolloSummonCheckPerformed)
            {
                if (!NPC.AnyNPCs(ExoMechNPCIDs.ApolloID))
                    NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ExoMechNPCIDs.ApolloID, NPC.whoAmI);

                ApolloSummonCheckPerformed = true;
                NPC.netUpdate = true;
            }
            else if (CalamityGlobalNPC.draedonExoMechTwinGreen != -1 && Main.npc[CalamityGlobalNPC.draedonExoMechTwinGreen].type == ExoMechNPCIDs.ApolloID)
            {
                NPC.realLife = CalamityGlobalNPC.draedonExoMechTwinGreen;
                NPC.life = Main.npc[CalamityGlobalNPC.draedonExoMechTwinGreen].life;
            }
            else
                NPC.active = false;

            // Use base Calamity's Charge AIState at all times, since Artemis needs that to be enabled for her CanHitPlayer hook to return true.
            NPC.As<Artemis>().AIState = (int)Artemis.Phase.Charge;

            CalamityGlobalNPC.draedonExoMechTwinRed = NPC.whoAmI;
            ThrusterBoost = MathHelper.Clamp(ThrusterBoost - 0.035f, 0f, 10f);
            SpecificDrawAction = null;
            NPC.Opacity = 1f;
            NPC.damage = 0;
            AITimer++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            CommonExoTwinFunctionalities.DrawBase(NPC, this, BaseTexture.Value, Glowmask.Value, lightColor, screenPos, Frame);
            return false;
        }
    }
}
