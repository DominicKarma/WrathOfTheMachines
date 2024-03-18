using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace DifferentExoMechs.Content.NPCs.Bosses
{
    public sealed partial class ApolloBehaviorOverride : NPCBehaviorOverride, IExoTwin
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
        /// Whether Apollo has fully entered his second phase yet or not.
        /// </summary>
        public bool InPhase2
        {
            get => NPC.ai[0] == 1f;
            set => NPC.ai[0] = value.ToInt();
        }

        /// <summary>
        /// Whether Apollo has verified that Artemis is alive or not.
        /// </summary>
        public bool ArtemisSummonCheckPerformed
        {
            get;
            set;
        }

        /// <summary>
        /// Apollo's current animation.
        /// </summary>
        public ExoTwinAnimation Animation
        {
            get;
            set;
        } = ExoTwinAnimation.Idle;

        /// <summary>
        /// Apollo's optic nerve colors.
        /// </summary>
        public Color[] OpticNervePalette => [new(28, 58, 60), new(62, 105, 80), new(108, 167, 94), new(144, 246, 100), new(81, 126, 85)];

        public override int NPCOverrideID => ExoMechNPCIDs.ApolloID;

        public override void AI()
        {
            if (!ArtemisSummonCheckPerformed)
            {
                if (!NPC.AnyNPCs(ExoMechNPCIDs.ArtemisID))
                    NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ExoMechNPCIDs.ArtemisID, NPC.whoAmI);

                ArtemisSummonCheckPerformed = true;
                NPC.netUpdate = true;
            }

            // Use base Calamity's ChargeCombo AIState at all times, since Apollo needs that to be enabled for his CanHitPlayer hook to return true.
            NPC.As<Apollo>().AIState = (int)Apollo.Phase.ChargeCombo;

            CalamityGlobalNPC.draedonExoMechTwinGreen = NPC.whoAmI;
            NPC.Opacity = 1f;
            AITimer++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            Texture2D glowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Apollo/ApolloGlow").Value;
            CommonExoTwinFunctionalities.DrawBase(NPC, this, glowmask, lightColor, screenPos, Frame);
            return false;
        }
    }
}
