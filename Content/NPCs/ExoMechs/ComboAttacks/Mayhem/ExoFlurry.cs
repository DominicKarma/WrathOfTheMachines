﻿using System;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.Particles;
using CalamityMod.Sounds;
using Luminance.Common.Easings;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WoTM.Content.NPCs.ExoMechs.Projectiles;
using WoTM.Content.Particles;

namespace WoTM.Content.NPCs.ExoMechs
{
    public class ExoFlurry : ExoMechComboHandler
    {
        /// <summary>
        /// How long Ares spends charging up before firing his nuke.
        /// </summary>
        public static int NukeChargeUpTime => Utilities.SecondsToFrames(2.3f);

        /// <summary>
        /// How long Ares waits after firing his nuke before being able to begin attempting to fire a new one.
        /// </summary>
        public static int NukeShotDelay => Utilities.SecondsToFrames(9f);

        /// <summary>
        /// The diameter of the explosion from Ares' nukes.
        /// </summary>
        public static float NukeExplosionDiameter => 2300f;

        public override int[] ExpectedManagingExoMechs => [ModContent.NPCType<ThanatosHead>(), ModContent.NPCType<AresBody>(), ModContent.NPCType<Apollo>()];

        public override bool Perform(NPC npc)
        {
            if (npc.type == ModContent.NPCType<Artemis>() || npc.type == ModContent.NPCType<Apollo>())
            {
                Perform_ExoTwins(npc);
                return false;
            }

            if (npc.type == ModContent.NPCType<AresBody>())
            {
                Perform_Ares(npc);
                return false;
            }

            if (npc.type == ModContent.NPCType<ThanatosHead>())
            {
                Perform_Hades(npc);
                return false;
            }

            return false;
        }

        /// <summary>
        /// Performs Ares' part in the ExoFlurry attack.
        /// </summary>
        /// <param name="npc">Ares' NPC instance.</param>
        public static void Perform_Ares(NPC npc)
        {
            if (!npc.TryGetBehavior(out AresBodyBehaviorOverride ares))
                return;

            Vector2 hoverDestination = Target.Center - Vector2.UnitY * 350f;
            Vector2 flyDirection = npc.SafeDirectionTo(hoverDestination);
            if (!npc.WithinRange(hoverDestination, 300f))
                npc.velocity += flyDirection * 1.1f;
            if (npc.velocity.AngleBetween(flyDirection) >= 1.4f)
                npc.velocity *= 0.9f;

            ares.InstructionsForHands[0] = new(h => Perform_Ares_GaussNuke(npc, h, new Vector2(-400f, 40f), 0));
            ares.InstructionsForHands[1] = new(h => ares.BasicHandUpdate(h, new Vector2(-280f, 224f), 1));
            ares.InstructionsForHands[2] = new(h => ares.BasicHandUpdate(h, new Vector2(280f, 224f), 2));
            ares.InstructionsForHands[3] = new(h => Perform_Ares_GaussNuke(npc, h, new Vector2(400f, 40f), 3));
        }

        /// <summary>
        /// Handles the updating of Ares' gauss nuke(s) in the ExoFlurry attack.
        /// </summary>
        /// <param name="ares">Ares' NPC instance.</param>
        /// <param name="hand">The hand's ModNPC instance.</param>
        /// <param name="hoverOffset">The hover offset of the hand relative to Ares.</param>
        /// <param name="armIndex">The index of the arm.</param>
        public static void Perform_Ares_GaussNuke(NPC ares, AresHand hand, Vector2 hoverOffset, int armIndex)
        {
            int wrappedTimer = (AITimer + armIndex * NukeShotDelay / 3) % (NukeChargeUpTime + NukeShotDelay);
            NPC handNPC = hand.NPC;
            handNPC.SmoothFlyNear(ares.Center + hoverOffset * ares.scale, 0.3f, 0.8f);
            handNPC.Opacity = Utilities.Saturate(handNPC.Opacity + 0.2f);
            hand.UsesBackArm = armIndex == 0 || armIndex == AresBodyBehaviorOverride.ArmCount - 1;
            hand.ArmSide = (armIndex >= AresBodyBehaviorOverride.ArmCount / 2).ToDirectionInt();
            hand.HandType = AresHandType.GaussNuke;
            hand.ArmEndpoint = handNPC.Center + handNPC.velocity;
            hand.EnergyDrawer.chargeProgress = Utilities.InverseLerp(0f, NukeChargeUpTime, wrappedTimer);
            if (hand.EnergyDrawer.chargeProgress >= 1f)
                hand.EnergyDrawer.chargeProgress = 0f;
            hand.GlowmaskDisabilityInterpolant = 0f;
            hand.RotateToLookAt(Target.Center);

            if (wrappedTimer % 20 == 19 && hand.EnergyDrawer.chargeProgress >= 0.4f)
            {
                int pulseCounter = (int)MathF.Round(hand.EnergyDrawer.chargeProgress * 5f);
                hand.EnergyDrawer.AddPulse(pulseCounter);
            }

            if (wrappedTimer == 1)
                SoundEngine.PlaySound(AresGaussNuke.TelSound with { Volume = 3f });

            AresBodyBehaviorOverride.HandleGaussNukeShots(hand, handNPC, wrappedTimer, NukeChargeUpTime, NukeExplosionDiameter);
        }

        /// <summary>
        /// Performs Artemis and Apollo's part in the ExoFlurry attack. This method processes behaviors for both mechs.
        /// </summary>
        /// <param name="npc">The Exo Twins' NPC instance.</param>
        public static void Perform_ExoTwins(NPC npc)
        {
            int spinTime = 60;
            int spinSlowdownAndRepositionTime = 40;
            int dashTime = 32;
            int spinCycleTime = spinTime + spinSlowdownAndRepositionTime + dashTime;
            int spinCycleTimer = AITimer % spinCycleTime;
            bool isApollo = npc.type == ModContent.NPCType<Apollo>();
            float startingDashSpeed = 10f;
            float maxDashSpeed = 150f;
            float standardSpinRadius = 325f;
            float maxSpinRadiusExtension = 872f;
            ExoTwinAnimation animation = ExoTwinAnimation.ChargingUp;

            // Initialize the random spin orientation of the twins at the very beginning of the attack.
            if (AITimer == 1 && isApollo)
            {
                ExoTwinsStateManager.SharedState.Values[0] = Main.rand.NextFloat(MathHelper.TwoPi);
                npc.netUpdate = true;
            }

            float spinCompletion = Utilities.InverseLerp(0f, spinTime + spinSlowdownAndRepositionTime, spinCycleTimer);
            if (spinCompletion < 1f)
            {
                // Determine the radius of the spin for the two Exo Twins.
                float radiusExtendInterpolant = Utilities.InverseLerp(0f, spinSlowdownAndRepositionTime, spinCycleTimer - spinTime);
                float radiusExtension = MathF.Pow(radiusExtendInterpolant, 3.13f) * maxSpinRadiusExtension;
                float currentRadius = standardSpinRadius + radiusExtension;

                float spinOffsetAngle = EasingCurves.Quintic.Evaluate(EasingType.Out, 0f, MathHelper.Pi * 3f, spinCompletion) + ExoTwinsStateManager.SharedState.Values[0];
                Vector2 hoverOffset = spinOffsetAngle.ToRotationVector2() * currentRadius;

                // Invert the hover offset if Apollo is spinning, such that it is opposite to Artemis.
                if (isApollo)
                    hoverOffset *= -1f;

                Vector2 hoverDestination = Target.Center + hoverOffset;

                // Fly around in accordance with the radius offset.
                npc.SmoothFlyNear(hoverDestination, MathF.Cbrt(spinCompletion) * 0.9f, 0.01f);
                npc.rotation = npc.AngleTo(Target.Center);
            }

            // Perform the dash.
            if (spinCycleTimer == spinTime + spinSlowdownAndRepositionTime)
            {
                SoundEngine.PlaySound(Artemis.ChargeSound, npc.Center);

                ScreenShakeSystem.StartShakeAtPoint(npc.Center, 5.3f);

                npc.velocity = npc.rotation.ToRotationVector2() * startingDashSpeed;
                npc.rotation = npc.velocity.ToRotation();
                npc.netUpdate = true;
            }

            IExoTwin? twinInfo = null;
            float motionBlurInterpolant = Utilities.InverseLerp(90f, 150f, npc.velocity.Length());
            float thrusterBoost = Utilities.InverseLerp(maxDashSpeed * 0.85f, maxDashSpeed, npc.velocity.Length()) * 1.3f;
            if (npc.TryGetBehavior(out ArtemisBehaviorOverride artemisBehavior))
            {
                artemisBehavior.MotionBlurInterpolant = motionBlurInterpolant;
                artemisBehavior.ThrusterBoost = MathHelper.Max(artemisBehavior.ThrusterBoost, thrusterBoost);
                twinInfo = artemisBehavior;
            }
            else if (npc.TryGetBehavior(out ApolloBehaviorOverride apolloBehavior))
            {
                apolloBehavior.MotionBlurInterpolant = motionBlurInterpolant;
                apolloBehavior.ThrusterBoost = MathHelper.Max(apolloBehavior.ThrusterBoost, thrusterBoost);
                twinInfo = apolloBehavior;
            }

            // Handle post-dash behaviors.
            if (spinCycleTimer >= spinTime + spinSlowdownAndRepositionTime && spinCycleTimer <= spinTime + spinSlowdownAndRepositionTime + dashTime)
            {
                // Apply hit forces when Artemis and Apollo collide, in accordance with Newton's third law.
                NPC artemis = Main.npc[CalamityGlobalNPC.draedonExoMechTwinRed];
                if (isApollo && npc.Hitbox.Intersects(artemis.Hitbox))
                {
                    Vector2 impactForce = npc.velocity.RotatedBy(-MathHelper.PiOver2) * 0.15f;
                    npc.velocity -= impactForce;
                    npc.netUpdate = true;
                    artemis.velocity += impactForce;
                    artemis.netUpdate = true;

                    SoundEngine.PlaySound(CommonCalamitySounds.ExoHitSound, npc.Center);

                    for (int i = 0; i < 35; i++)
                    {
                        npc.HitEffect();
                        artemis.HitEffect();
                    }
                }

                npc.velocity = (npc.velocity + npc.velocity.SafeNormalize(Vector2.UnitY) * 15f).ClampLength(0f, maxDashSpeed);
                npc.damage = npc.defDamage;
                npc.rotation = npc.velocity.ToRotation();

                animation = ExoTwinAnimation.Attacking;

                if (spinCycleTimer == spinCycleTime - 1 && isApollo)
                {
                    ExoTwinsStateManager.SharedState.Values[0] = npc.AngleTo(Target.Center) - MathHelper.PiOver2;
                    npc.netUpdate = true;
                }
            }

            // Update animations.
            if (twinInfo is not null)
            {
                twinInfo.Animation = animation;
                twinInfo.Frame = twinInfo.Animation.CalculateFrame(AITimer / 40f % 1f, twinInfo.InPhase2);
            }
        }

        /// <summary>
        /// Performs Hades' part in the ExoFlurry attack.
        /// </summary>
        /// <param name="npc">Hades' NPC instance.</param>
        public static void Perform_Hades(NPC npc)
        {
            ref float localAITimer = ref npc.ai[0];
            if (AITimer == 1)
            {
                localAITimer = 0f;
                npc.netUpdate = true;
            }

            if (!npc.TryGetBehavior(out HadesHeadBehaviorOverride hades))
            {
                npc.active = false;
                return;
            }

            int returnOnScreenTime = 420;
            int beamWindUpTime = 180;
            int beamShootDelay = 60;
            int beamFireTime = ExoelectricBlast.Lifetime;
            int postBeamFireLeaveTime = 240;
            int attackCycleTime = returnOnScreenTime + beamWindUpTime + beamFireTime + postBeamFireLeaveTime;
            int wrappedAttackTimer = (int)localAITimer % attackCycleTime;
            float jawExtendInterpolant = 0f;

            if (wrappedAttackTimer <= returnOnScreenTime)
            {
                Vector2 idealDirection = npc.SafeDirectionTo(Target.Center);
                npc.velocity = idealDirection * MathHelper.Lerp(npc.velocity.Length(), 120f, 0.06f);
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

                if (npc.WithinRange(Target.Center, 400f))
                {
                    localAITimer += returnOnScreenTime - localAITimer + 1f;
                    npc.velocity *= 0.1f;
                    npc.netUpdate = true;
                }
            }

            else if (wrappedAttackTimer <= returnOnScreenTime + beamWindUpTime)
            {
                if (npc.velocity.Length() >= 3f)
                    npc.velocity *= 0.9485f;
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(Target.Center), 0.01f);
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

                float chargeUpCompletion = Utilities.InverseLerp(0f, beamWindUpTime, wrappedAttackTimer - returnOnScreenTime);

                jawExtendInterpolant = MathF.Sqrt(chargeUpCompletion);
                hades.ReticleOpacity = MathF.Pow(Utilities.InverseLerp(0f, 0.25f, chargeUpCompletion), 0.6f);

                int particleCount = (int)MathHelper.Lerp(1f, 3f, chargeUpCompletion);
                Vector2 mouthPosition = npc.Center + npc.velocity.SafeNormalize(Vector2.Zero) * 40f;
                for (int i = 0; i < particleCount; i++)
                {
                    float particleScale = (chargeUpCompletion + 1.1f) * Main.rand.NextFloat(0.6f, 1f);
                    Vector2 energySpawnPosition = mouthPosition + Main.rand.NextVector2Unit() * Main.rand.NextFloat(200f, 250f);
                    BloomPixelParticle energy = new(energySpawnPosition, (mouthPosition - energySpawnPosition).RotatedBy(MathHelper.PiOver4) * 0.05f, Color.Wheat, Color.DeepSkyBlue * 0.4f, 30, Vector2.One * particleScale, mouthPosition);
                    energy.Spawn();
                }

                if (wrappedAttackTimer % 10 == 9)
                {
                    float scale = MathHelper.Lerp(0.5f, 3f, chargeUpCompletion);

                    StrongBloom bloom = new(mouthPosition, Vector2.Zero, Color.DeepSkyBlue, scale, 20);
                    GeneralParticleHandler.SpawnParticle(bloom);

                    bloom = new(mouthPosition, Vector2.Zero, Color.Wheat, scale * 0.45f, 20);
                    GeneralParticleHandler.SpawnParticle(bloom);
                }

                if (wrappedAttackTimer == returnOnScreenTime + 2)
                    SoundEngine.PlaySound(HadesHeadBehaviorOverride.DeathrayChargeUpSound);

                if (wrappedAttackTimer == returnOnScreenTime + beamWindUpTime)
                {
                    SoundEngine.PlaySound(HadesHeadBehaviorOverride.DeathrayFireSound);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(npc.GetSource_FromAI(), mouthPosition, npc.velocity.SafeNormalize(Vector2.Zero), ModContent.ProjectileType<ExoelectricBlast>(), HadesHeadBehaviorOverride.ExoEnergyBlastDamage, 0f);
                }
            }

            else if (wrappedAttackTimer <= returnOnScreenTime + beamWindUpTime + beamShootDelay)
            {
                jawExtendInterpolant = 1f;
                hades.ReticleOpacity = Utilities.Saturate(hades.ReticleOpacity - 0.08f);
                if (npc.velocity.Length() >= 3f)
                    npc.velocity *= 0.9485f;
            }

            else if (wrappedAttackTimer <= returnOnScreenTime + beamWindUpTime + beamShootDelay + beamFireTime)
            {
                jawExtendInterpolant = 1f;
                if (npc.velocity.Length() >= 3f)
                    npc.velocity *= 0.9485f;
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(Target.Center), 0.0056f);
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            }
            else
            {
                npc.velocity = (npc.velocity * 1.02f).ClampLength(0f, 32f);
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            }

            hades.BodyBehaviorAction = new(HadesHeadBehaviorOverride.EveryNthSegment(3), HadesHeadBehaviorOverride.OpenSegment());
            hades.JawRotation = MathHelper.Lerp(hades.JawRotation, jawExtendInterpolant * 0.93f, 0.15f);
            npc.damage = 0;

            localAITimer++;
        }
    }
}
