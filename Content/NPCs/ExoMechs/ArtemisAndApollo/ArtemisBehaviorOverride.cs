using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Artemis;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace DifferentExoMechs.Content.NPCs.Bosses
{
    public sealed partial class ArtemisBehaviorOverride : NPCBehaviorOverride, IExoTwin
    {
        /// <summary>
        /// Apollo's current AI timer.
        /// </summary>
        public int AITimer
        {
            get => (int)NPC.ai[1];
            set => NPC.ai[1] = value;
        }

        /// <summary>
        /// Apollo's current frame.
        /// </summary>
        public int Frame
        {
            get;
            set;
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
        /// Artemis's current animation.
        /// </summary>
        public ExoTwinAnimation Animation
        {
            get;
            set;
        } = ExoTwinAnimation.Idle;

        /// <summary>
        /// Artemis' optic nerve colors.
        /// </summary>
        public Color[] OpticNervePalette => [new(75, 14, 6), new(145, 35, 4), new(204, 101, 24), new(254, 172, 84), new(224, 147, 40)];

        public override int NPCOverrideID => ExoMechNPCIDs.ArtemisID;

        public override void AI()
        {
            if (!ApolloSummonCheckPerformed)
            {
                if (!NPC.AnyNPCs(ExoMechNPCIDs.ApolloID))
                    NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ExoMechNPCIDs.ApolloID, NPC.whoAmI);

                ApolloSummonCheckPerformed = true;
                NPC.netUpdate = true;
            }

            // Use base Calamity's Charge AIState at all times, since Artemis needs that to be enabled for her CanHitPlayer hook to return true.
            NPC.As<Artemis>().AIState = (int)Artemis.Phase.Charge;

            CalamityGlobalNPC.draedonExoMechTwinRed = NPC.whoAmI;
            NPC.Opacity = 1f;
            AITimer++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            Texture2D glowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Artemis/ArtemisGlow").Value;
            CommonExoTwinFunctionalities.DrawBase(NPC, this, glowmask, lightColor, screenPos, Frame);
            return false;
        }
    }
}
