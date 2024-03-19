using System;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.Sounds;
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
        /// The amount of damage basic lasers from Artemis do.
        /// </summary>
        public static int BasicLaserDamage => Main.expertMode ? 400 : 250;

        /// <summary>
        /// AI update loop method for the DashesAndLasers attack.
        /// </summary>
        /// <param name="npc">The Exo Twin's NPC instance.</param>
        /// <param name="twinAttributes">The Exo Twin's designated generic attributes.</param>
        public static void DoBehavior_DashesAndLasers(NPC npc, IExoTwin twinAttributes)
        {
            bool isApollo = npc.type == ExoMechNPCIDs.ApolloID;
            if (isApollo)
                DoBehavior_DashesAndLasers_ApolloDashes(npc, twinAttributes);
            else
                DoBehavior_DashesAndLasers_ArtemisLasers(npc, twinAttributes);
        }

        /// <summary>
        /// AI update loop method for Apollo during the DashesAndLasers.
        /// </summary>
        /// <param name="npc">Apollo's NPC instance.</param>
        /// <param name="apolloAttributes">Apollo's designated generic attributes.</param>
        public static void DoBehavior_DashesAndLasers_ApolloDashes(NPC npc, IExoTwin apolloAttributes)
        {
            int hoverTime = 40;
            int reelBackTime = 14;
            int dashTime = 8;
            int slowDownTime = 18;
            bool artemis = npc.type == ExoMechNPCIDs.ArtemisID;
            Vector2 hoverDestination = Target.Center + Target.SafeDirectionTo(npc.Center) * new Vector2(650f, 450f);

            if (npc.life < npc.lifeMax * 0.95f)
            {
                SharedState.Reset();
                SharedState.AIState = ExoTwinsAIState.EnterSecondPhase;
            }

            if (AITimer <= hoverTime)
            {
                npc.SmoothFlyNear(hoverDestination, 0.2f, 0.4f);
                npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(Target.Center), AITimer / (float)hoverTime);
                apolloAttributes.Animation = ExoTwinAnimation.Idle;
                apolloAttributes.Frame = apolloAttributes.Animation.CalculateFrame(AITimer / (float)hoverTime, apolloAttributes.InPhase2);

                return;
            }

            if (AITimer <= hoverTime + reelBackTime)
            {
                if (AITimer == hoverTime + 1 && artemis)
                    SoundEngine.PlaySound(Artemis.AttackSelectionSound);

                float lookAngularVelocity = Utils.Remap(AITimer - hoverTime, 0f, reelBackTime, 0.1f, 0.006f);
                npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(Target.Center), lookAngularVelocity);
                npc.velocity *= 0.9f;

                apolloAttributes.Animation = ExoTwinAnimation.ChargingUp;
                apolloAttributes.Frame = apolloAttributes.Animation.CalculateFrame(Utilities.InverseLerp(0f, reelBackTime, AITimer - hoverTime), apolloAttributes.InPhase2);

                return;
            }

            if (AITimer <= hoverTime + reelBackTime + dashTime)
            {
                if (AITimer == hoverTime + reelBackTime + 1)
                {
                    ScreenShakeSystem.StartShake(14f, shakeStrengthDissipationIncrement: 0.35f);
                    SoundEngine.PlaySound(Artemis.ChargeSound);
                    npc.velocity = npc.rotation.ToRotationVector2() * 150f;
                    npc.netUpdate = true;
                }

                npc.damage = npc.defDamage;

                apolloAttributes.Animation = ExoTwinAnimation.Attacking;
                apolloAttributes.Frame = apolloAttributes.Animation.CalculateFrame(Utilities.InverseLerp(0f, dashTime, AITimer - hoverTime - reelBackTime), apolloAttributes.InPhase2);

                return;
            }

            if (AITimer <= hoverTime + reelBackTime + dashTime + slowDownTime)
            {
                npc.velocity *= 0.64f;
                npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(Target.Center), 0.1f);

                apolloAttributes.Animation = ExoTwinAnimation.Idle;
                apolloAttributes.Frame = apolloAttributes.Animation.CalculateFrame(Utilities.InverseLerp(0f, slowDownTime, AITimer - hoverTime - reelBackTime - dashTime), apolloAttributes.InPhase2);

                return;
            }

            AITimer = 0;
        }

        /// <summary>
        /// AI update loop method for Artemis during the DashesAndLasers.
        /// </summary>
        /// <param name="npc">Artemis' NPC instance.</param>
        /// <param name="artemisAttributes">Artemis' designated generic attributes.</param>
        public static void DoBehavior_DashesAndLasers_ArtemisLasers(NPC npc, IExoTwin artemisAttributes)
        {
            artemisAttributes.Animation = ExoTwinAnimation.ChargingUp;

            int shootTime = 40;
            int shootRate = 6;
            float aimOffsetAngle = 0f;
            if (AITimer <= shootTime)
            {
                artemisAttributes.Animation = ExoTwinAnimation.Attacking;

                aimOffsetAngle = MathF.Sin(MathHelper.TwoPi * AITimer / shootTime) * 0.7f;

                if (AITimer % shootRate == shootRate - 1)
                {
                    SoundEngine.PlaySound(CommonCalamitySounds.ExoLaserShootSound, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 aimDirection = npc.rotation.ToRotationVector2();
                        Vector2 laserShootVelocity = aimDirection * 20f / ArtemisLaserImproved.TotalUpdates;
                        Utilities.NewProjectileBetter(npc.GetSource_FromAI(), npc.Center + aimDirection * 100f, laserShootVelocity, ModContent.ProjectileType<ArtemisLaserImproved>(), BasicLaserDamage, 0f);
                    }
                }
            }

            Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X - npc.Center.X).NonZeroSign() * -600f, -350f) - npc.velocity * 3f;
            npc.SmoothFlyNearWithSlowdownRadius(hoverDestination, 0.04f, 0.7f, 50f);
            npc.rotation = npc.AngleTo(Target.Center) + aimOffsetAngle;

            artemisAttributes.Frame = artemisAttributes.Animation.CalculateFrame(AITimer / 50f % 1f, artemisAttributes.InPhase2);
        }
    }
}
