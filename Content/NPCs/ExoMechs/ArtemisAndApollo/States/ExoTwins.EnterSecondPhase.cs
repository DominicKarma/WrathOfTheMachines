using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.Projectiles.Boss;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DifferentExoMechs.Content.NPCs.Bosses
{
    public static partial class ExoTwinsStates
    {
        /// <summary>
        /// How long the Exo Twins spend slowing down in place before beginning their phase transition.
        /// </summary>
        public static int EnterSecondPhase_SlowDownTime => Utilities.SecondsToFrames(0.6f);

        /// <summary>
        /// How long the phase 2 transition animation lasts.
        /// </summary>
        public static int EnterSecondPhase_SecondPhaseAnimationTime => Utilities.SecondsToFrames(3f);

        /// <summary>
        /// How long Artemis waits before she enters her second phase. This should be at least <see cref="EnterSecondPhase_SecondPhaseAnimationTime"/>, since she's meant to transition after Apollo does.
        /// </summary>
        public static int EnterSecondPhase_ArtemisPhaseTransitionDelay => EnterSecondPhase_SecondPhaseAnimationTime + Utilities.SecondsToFrames(1f);

        /// <summary>
        /// The speed at which the Exo Twins shoot their lens upon releasing it.
        /// </summary>
        public static float EnterSecondPhase_LensPopOffSpeed => 30f;

        /// <summary>
        /// The offset at which the lens is spawned from the Exo Twins.
        /// </summary>
        public static float EnterSecondPhase_LensCenterOffset => 70f;

        /// <summary>
        /// What damage is multiplied by if Apollo is hit by a projectile while protecting Artemis.
        /// </summary>
        public static float EnterSecondPhase_ApolloDamageProtectionFactor => 0.2f;

        public static void ReleaseLens(NPC npc)
        {
            SoundEngine.PlaySound(Artemis.LensSound, npc.Center);
            ScreenShakeSystem.StartShakeAtPoint(npc.Center, 5f);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int lensProjectileID = ModContent.ProjectileType<BrokenArtemisLens>();
                if (npc.type == ModContent.NPCType<Apollo>())
                    lensProjectileID = ModContent.ProjectileType<BrokenApolloLens>();

                Vector2 lensDirection = npc.rotation.ToRotationVector2();
                Vector2 lensOffset = lensDirection * EnterSecondPhase_LensCenterOffset;
                Vector2 lensPopOffVelocity = lensDirection * EnterSecondPhase_LensPopOffSpeed;
                Utilities.NewProjectileBetter(npc.GetSource_FromAI(), npc.Center + lensOffset, lensPopOffVelocity, lensProjectileID, 0, 0f);
            }
        }

        /// <summary>
        /// Determines whether Apollo is currently protecting Artemis or not.
        /// </summary>
        /// <param name="apolloAttributes">Apollo's designated generic attributes.</param>
        public static bool DoBehavior_EnterSecondPhase_ApolloIsProtectingArtemis(IExoTwin apolloAttributes) => SharedState.AIState == ExoTwinsAIState.EnterSecondPhase;

        /// <summary>
        /// AI update loop method for the EnterSecondPhase state.
        /// </summary>
        /// <param name="npc">The Exo Twin's NPC instance.</param>
        /// <param name="twinAttributes">The Exo Twin's designated generic attributes.</param>
        public static void DoBehavior_EnterSecondPhase(NPC npc, IExoTwin twinAttributes)
        {
            // Slow down in place.
            if (AITimer < EnterSecondPhase_SlowDownTime)
            {
                npc.velocity *= 0.84f;
                npc.rotation = npc.AngleTo(Target.Center);
                return;
            }

            bool isArtemis = npc.type == ExoMechNPCIDs.ArtemisID;
            bool isApollo = !isArtemis;
            bool shouldProtectArtemis = isApollo && twinAttributes.InPhase2;

            if (isApollo)
            {
                if (shouldProtectArtemis)
                    ProtectArtemis(npc, Main.npc[CalamityGlobalNPC.draedonExoMechTwinRed], twinAttributes);
                else if (!twinAttributes.InPhase2)
                {
                    float animationCompletion = Utilities.InverseLerp(0f, EnterSecondPhase_SecondPhaseAnimationTime, AITimer - EnterSecondPhase_SlowDownTime);
                    PerformPhase2TransitionAnimations(npc, twinAttributes, animationCompletion);
                    npc.rotation = npc.AngleTo(Target.Center);
                }
            }

            else
            {
                float animationCompletion = Utilities.InverseLerp(0f, EnterSecondPhase_SecondPhaseAnimationTime, AITimer - EnterSecondPhase_SlowDownTime - EnterSecondPhase_ArtemisPhaseTransitionDelay);
                PerformPhase2TransitionAnimations(npc, twinAttributes, animationCompletion);

                // Twirl around if Artemis' animation completion is ongoing.
                if (animationCompletion > 0f)
                    npc.rotation += Utilities.Convert01To010(animationCompletion).Cubed() * 0.25f;

                // Otherwise, stick behind Apollo, waiting for his transition animation to finish.
                // The intent with this is that Artemis is trying to hide behind him.
                else
                {
                    NPC apollo = Main.npc[CalamityGlobalNPC.draedonExoMechTwinGreen];
                    Vector2 behindApollo = apollo.Center + Target.SafeDirectionTo(apollo.Center) * 360f;
                    npc.SmoothFlyNear(behindApollo, 0.11f, 0.8f);
                    npc.rotation = npc.AngleTo(Target.Center);
                }

                if (twinAttributes.InPhase2)
                {
                    SharedState.AIState = ExoTwinsAIState.TestDashes;
                    AITimer = 0;
                    twinAttributes.InPhase2 = false;
                }
            }
        }

        /// <summary>
        /// Performs phase 2 transition animations, popping off the lens when ready and setting the <see cref="IExoTwin.InPhase2"/> variable.
        /// </summary>
        /// <param name="npc">The Exo Twin's NPC instance.</param>
        /// <param name="twinAttributes">The Exo Twin's designated generic attributes.</param>
        /// <param name="animationCompletion">The animation completion.</param>
        public static void PerformPhase2TransitionAnimations(NPC npc, IExoTwin twinAttributes, float animationCompletion)
        {
            if (Collision.SolidCollision(npc.TopLeft - Vector2.One * 150f, npc.width + 300, npc.height + 300))
                npc.velocity.Y -= 1.5f;
            else
                npc.velocity *= 0.85f;

            int previousFrame = twinAttributes.Frame;
            twinAttributes.Animation = ExoTwinAnimation.EnteringSecondPhase;
            twinAttributes.Frame = twinAttributes.Animation.CalculateFrame(animationCompletion, false);

            if (previousFrame != twinAttributes.Frame && twinAttributes.Frame == ExoTwinAnimation.LensPopOffFrame)
                ReleaseLens(npc);

            if (animationCompletion >= 1f)
            {
                twinAttributes.InPhase2 = true;
                npc.netUpdate = true;
            }
        }

        /// <summary>
        /// Updates Apollo's movement in order to protect Artemis as she transitions to her second phase.
        /// </summary>
        /// <param name="apollo">Apollo's NPC instance.</param>
        /// <param name="artemis">Artemis' NPC instance.</param>
        /// <param name="artemis">Apollo's designated generic attributes.</param>
        public static void ProtectArtemis(NPC apollo, NPC artemis, IExoTwin apolloAttributes)
        {
            Vector2 guardDestination = artemis.Center + artemis.SafeDirectionTo(Target.Center) * 285f;
            apollo.SmoothFlyNear(guardDestination, 0.15f, 0.45f);
            apollo.rotation = apollo.AngleTo(Target.Center);

            apolloAttributes.Animation = ExoTwinAnimation.Idle;
            apolloAttributes.Frame = apolloAttributes.Animation.CalculateFrame(AITimer / 50f % 1f, apolloAttributes.InPhase2);
        }
    }
}
