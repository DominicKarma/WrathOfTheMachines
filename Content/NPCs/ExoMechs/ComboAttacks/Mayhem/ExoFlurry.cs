using System;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.Sounds;
using Luminance.Common.Easings;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

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
        public static float NukeExplosionDiameter => 3700f;

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

            npc.velocity = Vector2.Zero;

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

            npc.SmoothFlyNear(Target.Center - Vector2.UnitY * 200f, 0.08f, 0.95f);

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
            float dashSpeed = 114f;
            float standardSpinRadius = 325f;
            float maxSpinRadiusExtension = 872f;

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

                float spinOffsetAngle = EasingCurves.Cubic.Evaluate(EasingType.Out, 0f, MathHelper.Pi * 3f, spinCompletion) + ExoTwinsStateManager.SharedState.Values[0];
                Vector2 hoverOffset = spinOffsetAngle.ToRotationVector2() * currentRadius;

                // Invert the hover offset if Apollo is spinning, such that it is opposite to Artemis.
                if (isApollo)
                    hoverOffset *= -1f;

                Vector2 hoverDestination = Target.Center + hoverOffset;

                // Fly around in accordance with the radius offset.
                npc.SmoothFlyNear(hoverDestination, MathF.Cbrt(spinCompletion) * 0.5f, 0.01f);
                npc.rotation = npc.AngleTo(Target.Center);
            }

            // Perform the dash.
            if (spinCycleTimer == spinTime + spinSlowdownAndRepositionTime)
            {
                SoundEngine.PlaySound(Artemis.ChargeSound, npc.Center);

                ScreenShakeSystem.StartShakeAtPoint(npc.Center, 9f);

                npc.velocity = npc.rotation.ToRotationVector2() * dashSpeed;
                npc.rotation = npc.velocity.ToRotation();
                npc.netUpdate = true;
            }

            float motionBlurInterpolant = Utilities.InverseLerp(90f, 150f, npc.velocity.Length());
            float thrusterBoost = Utilities.InverseLerp(dashSpeed * 0.85f, dashSpeed, npc.velocity.Length()) * 1.3f;
            if (npc.TryGetBehavior(out ArtemisBehaviorOverride artemis))
            {
                artemis.MotionBlurInterpolant = motionBlurInterpolant;
                artemis.ThrusterBoost = MathHelper.Max(artemis.ThrusterBoost, thrusterBoost);
            }
            if (npc.TryGetBehavior(out ApolloBehaviorOverride apollo))
            {
                apollo.MotionBlurInterpolant = motionBlurInterpolant;
                apollo.ThrusterBoost = MathHelper.Max(apollo.ThrusterBoost, thrusterBoost);
            }

            // Handle post-dash behaviors.
            if (spinCycleTimer >= spinTime + spinSlowdownAndRepositionTime && spinCycleTimer <= spinTime + spinSlowdownAndRepositionTime + dashTime)
            {
                NPC artemisOther = Main.npc[CalamityGlobalNPC.draedonExoMechTwinRed];
                if (isApollo && npc.Hitbox.Intersects(artemisOther.Hitbox))
                {
                    Vector2 impactForce = npc.velocity.RotatedBy(-MathHelper.PiOver2) * 0.15f;
                    npc.velocity -= impactForce;
                    npc.netUpdate = true;
                    artemisOther.velocity += impactForce;
                    artemisOther.netUpdate = true;

                    ScreenShakeSystem.StartShake(6f);
                    SoundEngine.PlaySound(CommonCalamitySounds.ExoHitSound with { Volume = 4f });

                    for (int i = 0; i < 15; i++)
                    {
                        npc.HitEffect();
                        artemisOther.HitEffect();
                    }
                }

                npc.damage = npc.defDamage;
                npc.rotation = npc.velocity.ToRotation();

                if (spinCycleTimer == spinCycleTime - 1 && isApollo)
                {
                    ExoTwinsStateManager.SharedState.Values[0] = npc.AngleTo(Target.Center) - MathHelper.PiOver2;
                    npc.netUpdate = true;
                }
            }
        }
    }
}
