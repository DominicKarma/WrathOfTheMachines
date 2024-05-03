using System;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
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
            int dashTime = 8;
            int redirectTime = 60;
            int spinCycleTime = spinTime + spinSlowdownAndRepositionTime + dashTime + redirectTime;
            int spinCycleTimer = AITimer % 180;
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

                float spinOffsetAngle = EasingCurves.Quartic.Evaluate(EasingType.Out, 0f, MathHelper.Pi * 3f, spinCompletion) + ExoTwinsStateManager.SharedState.Values[0];
                Vector2 hoverOffset = spinOffsetAngle.ToRotationVector2() * currentRadius;

                // Invert the hover offset if Apollo is spinning, such that it is opposite to Artemis.
                if (isApollo)
                    hoverOffset *= -1f;

                Vector2 hoverDestination = Target.Center + hoverOffset;

                // Fly around in accordance with the radius offset.
                npc.SmoothFlyNear(hoverDestination, 0.25f, 0.8f);
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

            // Handle post-dash behaviors.
            float thrusterBoost = 0f;
            if (spinCycleTimer >= spinTime + spinSlowdownAndRepositionTime && spinCycleTimer <= spinTime + spinSlowdownAndRepositionTime + dashTime)
            {
                npc.damage = npc.defDamage;
                thrusterBoost = 1.3f;
            }

            // Reposition after the dash.
            if (spinCycleTimer >= spinTime + spinSlowdownAndRepositionTime + dashTime && spinCycleTimer <= spinTime + spinSlowdownAndRepositionTime + dashTime + redirectTime)
            {
                if (npc.velocity.Length() >= 60f)
                    npc.damage = npc.defDamage;

                // Specify the orientation for the upcoming spin.
                if (spinCycleTimer == spinTime + spinSlowdownAndRepositionTime + dashTime + redirectTime - 1 && isApollo)
                {
                    ExoTwinsStateManager.SharedState.Values[0] = npc.AngleTo(Target.Center);
                    npc.netUpdate = true;
                }

                float redirectInterpolant = Utilities.InverseLerp(0f, redirectTime, spinCycleTimer - spinTime - spinSlowdownAndRepositionTime - dashTime).Squared();
                Vector2 hoverDestination = Target.Center + ExoTwinsStateManager.SharedState.Values[0].ToRotationVector2() * isApollo.ToDirectionInt() * standardSpinRadius;
                npc.SmoothFlyNear(hoverDestination, redirectInterpolant * 0.19f, 1f - redirectInterpolant * 0.185f);
                npc.rotation = npc.velocity.ToRotation();

                if (redirectInterpolant <= 0.25f)
                    npc.velocity *= 0.86f;
                else
                    npc.velocity = npc.velocity.RotatedBy((1.8f - redirectInterpolant).Squared() * -0.4f);
            }

            float motionBlurInterpolant = Utilities.InverseLerp(90f, 150f, npc.velocity.Length());
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
        }
    }
}
