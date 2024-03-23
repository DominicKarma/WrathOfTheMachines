using System;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Artemis;
using DifferentExoMechs.Content.NPCs.ExoMechs.Projectiles;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DifferentExoMechs.Content.NPCs.ExoMechs
{
    public static partial class ExoTwinsStates
    {
        /// <summary>
        /// How long Apollo spends hovering/reeling back before dashing during the LoopDashBombardment attack.
        /// </summary>
        public static int LoopDashBombardment_HoverTime => Utilities.SecondsToFrames(1.25f);

        /// <summary>
        /// The amount of time before the dash's happening that Apollo uses to determine when he should play a telegraph beep sound during the LoopDashBombardment attack.
        /// </summary>
        public static int LoopDashBombardment_TelegraphSoundBuffer => Utilities.SecondsToFrames(0.467f);

        /// <summary>
        /// How long Apollo spends performing his initial straight dash during the LoopDashBombardment attack.
        /// </summary>
        public static int LoopDashBombardment_StraightDashTime => Utilities.SecondsToFrames(0.333f);

        /// <summary>
        /// How long Apollo spends spinning after dashing during the LoopDashBombardment attack.
        /// </summary>
        public static int LoopDashBombardment_SpinTime => Utilities.SecondsToFrames(0.8f);

        /// <summary>
        /// The speed Apollo starts his straight dash at during the LoopDashBombardment attack.
        /// </summary>
        public static float LoopDashBombardment_InitialApolloDashSpeed => 60f;

        /// <summary>
        /// The maximum speed that Apollo flies at while spinning during the LoopDashBombardment attack. When above this speed he will slow down.
        /// </summary>
        public static float LoopDashBombardment_MaxApolloSpinSpeed => 40f;

        /// <summary>
        /// The speed of missiles shot by Apollo during the LoopDashBombardment attack.
        /// </summary>
        public static float LoopDashBombardment_ApolloMissileShootSpeed => 16f;

        // This serves two purposes:
        // 1. Anti-telefrag prevention. Wouldn't want missiles to just immediately fly at the player.
        // 2. It encourages risky play in the attack. By getting close to Apollo you can induce him to not fire rockets, and make it easier to weave through them as they fall after the fact.
        /// <summary>
        /// The closest distance Apollo can be to his targets before he ceases to fire rockets during the LoopDashBombardment attack.
        /// </summary>
        public static float LoopDashBombardment_ApolloMissileSpawnDistanceThreshold => 256f;

        /// <summary>
        /// The acceleration of Apollo when performing his final straight dash during the LoopDashBombardment attack.
        /// </summary>
        public static float LoopDashBombardment_ApolloFinalDashAcceleration => 5f;

        /// <summary>
        /// The max speed of Apollo when performing his final straight dash during the LoopDashBombardment attack.
        /// </summary>
        public static float LoopDashBombardment_MaxApolloFinalDashSpeed => 250f;

        /// <summary>
        /// AI update loop method for the LoopDashBombardment attack.
        /// </summary>
        /// <param name="npc">The Exo Twin's NPC instance.</param>
        /// <param name="twinAttributes">The Exo Twin's designated generic attributes.</param>
        public static void DoBehavior_LoopDashBombardment(NPC npc, IExoTwin twinAttributes)
        {
            bool isApollo = npc.type == ExoMechNPCIDs.ApolloID;
            if (isApollo)
                DoBehavior_LoopDashBombardment_ApolloDashes(npc, twinAttributes);
            else
                DoBehavior_LoopDashBombardment_ArtemisLasers(npc, twinAttributes);
        }

        /// <summary>
        /// AI update loop method for Apollo during the LoopDashBombardment.
        /// </summary>
        /// <param name="npc">Apollo's NPC instance.</param>
        /// <param name="apolloAttributes">Apollo's designated generic attributes.</param>
        public static void DoBehavior_LoopDashBombardment_ApolloDashes(NPC npc, IExoTwin apolloAttributes)
        {
            int hoverTime = LoopDashBombardment_HoverTime;
            int telegraphSoundBuffer = LoopDashBombardment_TelegraphSoundBuffer;
            int straightDashTime = LoopDashBombardment_StraightDashTime;
            int spinTime = LoopDashBombardment_SpinTime;
            bool doneHovering = AITimer >= hoverTime;
            bool performingStraightDash = AITimer >= hoverTime && AITimer <= hoverTime + straightDashTime;
            bool pastSpinTime = AITimer >= hoverTime + straightDashTime;
            bool performingSpinDash = pastSpinTime && AITimer <= hoverTime + straightDashTime + spinTime;
            bool acceleratingAfterSpin = AITimer >= hoverTime + straightDashTime + spinTime + 10;
            ref float spinDirection = ref npc.ai[2];

            if (!doneHovering)
            {
                if (AITimer == hoverTime - telegraphSoundBuffer)
                    SoundEngine.PlaySound(Artemis.ChargeTelegraphSound, npc.Center);

                // Hover to the side of the target at first. The hover offset calculation is the unit direction from the target to Apollo.
                // By default, using this would make Apollo attempt to stay a direction from the target, merely adjusting his radius.
                // However, since the Y position is multiplied every frame, this causes him to gradually level out and hover to the side of the target as time passes.
                Vector2 reelBackOffset = Target.SafeDirectionTo(npc.Center) * MathF.Pow(AITimer / (float)hoverTime, 8f) * 350f;
                Vector2 hoverOffset = Target.SafeDirectionTo(npc.Center) * new Vector2(1f, 0.94f);
                Vector2 hoverDestination = Target.Center + hoverOffset * new Vector2(750f, 400f) + reelBackOffset;
                npc.SmoothFlyNear(hoverDestination, AITimer / (float)hoverTime * 0.6f, 0.71f);
                npc.rotation = npc.AngleTo(Target.Center);

                apolloAttributes.Animation = ExoTwinAnimation.Idle;
                if (AITimer >= hoverTime - telegraphSoundBuffer)
                    apolloAttributes.Animation = ExoTwinAnimation.ChargingUp;
            }

            if (doneHovering)
                npc.damage = npc.defDamage;

            if (AITimer == hoverTime)
            {
                ScreenShakeSystem.StartShakeAtPoint(npc.Center, 7.5f);
                npc.velocity = npc.SafeDirectionTo(Target.Center) * LoopDashBombardment_InitialApolloDashSpeed;
                spinDirection = -npc.velocity.X.NonZeroSign();
                npc.netUpdate = true;

                SoundEngine.PlaySound(Artemis.ChargeSound, npc.Center);
            }

            if (performingStraightDash)
                npc.velocity *= 1.014f;

            if (pastSpinTime)
            {
                if (performingSpinDash)
                {
                    npc.velocity = npc.velocity.RotatedBy(MathHelper.TwoPi / spinTime * spinDirection);
                    if (AITimer >= hoverTime + straightDashTime + 8 && npc.velocity.AngleBetween(npc.SafeDirectionTo(Target.Center + Target.velocity * 18f)) < 0.16f)
                        AITimer = hoverTime + straightDashTime + spinTime;
                }

                if (npc.velocity.Length() > LoopDashBombardment_MaxApolloSpinSpeed && !acceleratingAfterSpin)
                    npc.velocity *= 0.94f;

                apolloAttributes.Animation = ExoTwinAnimation.Attacking;
                npc.rotation = npc.velocity.ToRotation();
            }

            // Release missiles.
            bool canFireMissiles = AITimer >= hoverTime + straightDashTime && npc.velocity.Length() <= 150f;
            bool tooCloseToFireMissiles = npc.WithinRange(Target.Center, LoopDashBombardment_ApolloMissileSpawnDistanceThreshold);
            if (AITimer % 4 == 3 && canFireMissiles && !tooCloseToFireMissiles)
                DoBehavior_LoopDashBombardment_ReleasePlasmaMissile(npc);

            if (acceleratingAfterSpin && npc.velocity.Length() <= LoopDashBombardment_MaxApolloFinalDashSpeed)
                npc.velocity += npc.velocity.SafeNormalize(Vector2.UnitY) * LoopDashBombardment_ApolloFinalDashAcceleration;

            apolloAttributes.Frame = apolloAttributes.Animation.CalculateFrame(AITimer / 40f % 1f, apolloAttributes.InPhase2);
            apolloAttributes.WingtipVorticesOpacity = Utilities.InverseLerp(30f, 45f, npc.velocity.Length());
        }

        /// <summary>
        /// Releases a single missing during the LoopDashBombardment attack.
        /// </summary>
        /// <param name="apollo">Apollo's NPC instance.</param>
        public static void DoBehavior_LoopDashBombardment_ReleasePlasmaMissile(NPC apollo)
        {
            SoundEngine.PlaySound(Apollo.MissileLaunchSound with { Volume = 0.4f, MaxInstances = 0 }, apollo.Center);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 missileVelocity = Vector2.Lerp(apollo.rotation.ToRotationVector2(), apollo.SafeDirectionTo(Target.Center), 0.5f) * LoopDashBombardment_ApolloMissileShootSpeed;
                Vector2 missileSpawnPosition = apollo.Center + apollo.rotation.ToRotationVector2() * 70f;
                Utilities.NewProjectileBetter(apollo.GetSource_FromAI(), missileSpawnPosition, missileVelocity, ModContent.ProjectileType<ApolloMissile>(), BasicShotDamage, 0f, Main.myPlayer, Target.Center.Y);
            }
        }

        /// <summary>
        /// AI update loop method for Artemis during the LoopDashBombardment.
        /// </summary>
        /// <param name="npc">Artemis' NPC instance.</param>
        /// <param name="artemisAttributes">Artemis' designated generic attributes.</param>
        public static void DoBehavior_LoopDashBombardment_ArtemisLasers(NPC npc, IExoTwin artemisAttributes)
        {
            npc.velocity *= 0.6f;
        }
    }
}
