using System;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.Projectiles.Boss;
using DifferentExoMechs.Content.Particles;
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
        public static float EnterSecondPhase_LensCenterOffset => 62f;

        /// <summary>
        /// What damage is multiplied by if Apollo is hit by a projectile while protecting Artemis.
        /// </summary>
        public static float EnterSecondPhase_ApolloDamageProtectionFactor => 0.2f;

        /// <summary>
        /// The sound the Exo Twins make when ejecting their lens.
        /// </summary>
        public static readonly SoundStyle LensEjectSound = new("DifferentExoMechs/Assets/Sounds/Custom/ExoTwins/LensEject");

        public static void ReleaseLens(NPC npc)
        {
            SoundEngine.PlaySound(LensEjectSound, npc.Center);
            ScreenShakeSystem.StartShakeAtPoint(npc.Center, 5f);

            Vector2 lensDirection = npc.rotation.ToRotationVector2();
            Vector2 lensOffset = lensDirection * EnterSecondPhase_LensCenterOffset;
            Vector2 lensPosition = npc.Center + lensOffset;
            for (int i = 0; i < 45; i++)
            {
                Color smokeColor = Color.Lerp(Color.White, Color.Gray, Main.rand.NextFloat(0.85f));
                Vector2 smokeVelocity = lensDirection.RotatedByRandom(0.5f) * Main.rand.NextFloat(4f, 60f);
                int smokeLifetime = (int)Utils.Remap(smokeVelocity.Length(), 5f, 60f, 50f, 18f) + Main.rand.Next(-5, 10);
                SmokeParticle smoke = new(lensPosition, smokeVelocity, smokeColor, smokeLifetime, Main.rand.NextFloat(0.7f, 1.3f), 0.15f);
                smoke.Spawn();
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int lensProjectileID = ModContent.ProjectileType<BrokenArtemisLens>();
                if (npc.type == ModContent.NPCType<Apollo>())
                    lensProjectileID = ModContent.ProjectileType<BrokenApolloLens>();

                Vector2 lensPopOffVelocity = lensDirection * EnterSecondPhase_LensPopOffSpeed;
                Utilities.NewProjectileBetter(npc.GetSource_FromAI(), lensPosition, lensPopOffVelocity, lensProjectileID, 0, 0f);

                npc.netUpdate = true;
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
                npc.chaseable = false;

                float animationCompletion = Utilities.InverseLerp(0f, EnterSecondPhase_SecondPhaseAnimationTime, AITimer - EnterSecondPhase_SlowDownTime - EnterSecondPhase_ArtemisPhaseTransitionDelay);
                PerformPhase2TransitionAnimations(npc, twinAttributes, animationCompletion);

                // Look to the side if Artemis' animation completion is ongoing.
                if (animationCompletion > 0f)
                {
                    npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(Target.Center) + 0.5f, 0.1f);
                    npc.velocity *= 0.9f;
                }

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
                npc.velocity.Y -= 2.54f;
            else
                npc.velocity *= 0.81f;

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
            float guardOffset = MathF.Min(artemis.Distance(Target.Center), 285f);
            Vector2 guardDestination = artemis.Center + artemis.SafeDirectionTo(Target.Center) * guardOffset;
            apollo.SmoothFlyNear(guardDestination, 0.15f, 0.45f);
            apollo.rotation = apollo.AngleTo(Target.Center);

            apolloAttributes.Animation = ExoTwinAnimation.Idle;
            float animationCompletion = AITimer / 50f % 1f;

            bool playerIsUncomfortablyCloseToArtemis = Target.WithinRange(artemis.Center, 600f);
            if (playerIsUncomfortablyCloseToArtemis)
            {
                apollo.damage = apollo.defDamage;
                apolloAttributes.Animation = ExoTwinAnimation.Attacking;
                animationCompletion = SharedState.StateNumbers[0] / 50f % 1f;
                SharedState.StateNumbers[0]++;
            }
            else
                SharedState.StateNumbers[0] = 0f;

            apolloAttributes.Frame = apolloAttributes.Animation.CalculateFrame(animationCompletion, apolloAttributes.InPhase2);
        }
    }
}
