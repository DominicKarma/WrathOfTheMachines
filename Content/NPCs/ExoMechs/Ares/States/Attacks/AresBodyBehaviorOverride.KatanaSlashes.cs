using System;
using CalamityMod.Items.Weapons.Melee;
using Luminance.Common.Easings;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using WoTM.Content.Particles;

namespace WoTM.Content.NPCs.ExoMechs
{
    public sealed partial class AresBodyBehaviorOverride : NPCBehaviorOverride
    {
        /// <summary>
        /// How long Ares waits before slashing during his Katana Slashes attack.
        /// </summary>
        public static int KatanaSlashes_AttackDelay => Utilities.SecondsToFrames(0.5f);

        /// <summary>
        /// How long a single swipe cycle lasts during Ares' Katana Slashes attack.
        /// </summary>
        public static int KatanaSlashes_AttackCycleTime => Utilities.SecondsToFrames(1.25f);

        /// <summary>
        /// AI update loop method for the KatanaSlashes attack.
        /// </summary>
        public void DoBehavior_KatanaSlashes()
        {
            if (Main.mouseRight && Main.mouseRightRelease)
                AITimer = 0;

            if (AITimer == 1)
            {
                ScreenShakeSystem.StartShake(10f);
                SoundEngine.PlaySound(LaughSound with { Volume = 10f });
            }

            AnimationState = AresFrameAnimationState.Laugh;

            NPC.SmoothFlyNear(Target.Center - Vector2.UnitY * 350f, 0.07f, 0.95f);

            if (AITimer >= 60000000)
                SelectNewState();

            InstructionsForHands[0] = new(h => KatanaSlashesHandUpdate(h, new Vector2(-400f, 40f), 0));
            InstructionsForHands[1] = new(h => KatanaSlashesHandUpdate(h, new Vector2(-280f, 224f), 1));
            InstructionsForHands[2] = new(h => KatanaSlashesHandUpdate(h, new Vector2(280f, 224f), 2));
            InstructionsForHands[3] = new(h => KatanaSlashesHandUpdate(h, new Vector2(400f, 40f), 3));
        }

        /// <summary>
        /// Updates one of Ares' hands for the Katana Slashes attack.
        /// </summary>
        /// <param name="hand">The hand's ModNPC instance.</param>
        /// <param name="hoverOffset">The hover offset of the hand.</param>
        /// <param name="armIndex">The index of the hand.</param>
        public void KatanaSlashesHandUpdate(AresHand hand, Vector2 hoverOffset, int armIndex)
        {
            NPC handNPC = hand.NPC;

            handNPC.Opacity = Utilities.Saturate(handNPC.Opacity + 0.3f);
            hand.UsesBackArm = armIndex == 0 || armIndex == ArmCount - 1;
            hand.ArmSide = (armIndex >= ArmCount / 2).ToDirectionInt();
            hand.HandType = AresHandType.EnergyKatana;
            hand.ArmEndpoint = handNPC.Center + handNPC.velocity;
            hand.EnergyDrawer.chargeProgress = Utilities.InverseLerp(0f, 30f, AITimer);
            hand.GlowmaskDisabilityInterpolant = 0f;
            handNPC.spriteDirection = 1;

            KatanaSlashesHandUpdate_HandleSlashMotion(hand, handNPC, hoverOffset);
            KatanaSlashesHandUpdate_CreateParticles(hand, handNPC);
        }

        /// <summary>
        /// Handles the motion of one of Ares' katanas during the Katana Slashes attack.
        /// </summary>
        /// <param name="hand">The hand's ModNPC instance.</param>
        /// <param name="handNPC">The hand's NPC instance.</param>
        /// <param name="hoverOffset">The hover offset of the hand.</param>
        public void KatanaSlashesHandUpdate_HandleSlashMotion(AresHand hand, NPC handNPC, Vector2 hoverOffset)
        {
            int animationTimer = (int)(AITimer + handNPC.whoAmI * 16f - KatanaSlashes_AttackDelay) % KatanaSlashes_AttackCycleTime;
            float animationCompletion = animationTimer / (float)KatanaSlashes_AttackCycleTime;
            Vector2 hoverDestination = NPC.Center + hoverOffset * NPC.scale;

            float anticipationCurveEnd = 0.45f;
            float slashCurveEnd = 0.59f;

            // Bear in mind that the motion resulting from the easing curve in this function is not followed exactly, it's only closely
            // followed via the SmoothFlyNear function below. This gives the motion a slightly jerky, mechanical feel to it, which is well in
            // line with Ares.
            if (AITimer >= KatanaSlashes_AttackDelay)
            {
                PiecewiseCurve curve = new PiecewiseCurve().
                    Add(EasingCurves.Quadratic, EasingType.InOut, -1.12f, anticipationCurveEnd).
                    Add(EasingCurves.MakePoly(20f), EasingType.Out, hand.UsesBackArm ? 1.68f : 1.1f, slashCurveEnd).
                    Add(EasingCurves.Quintic, EasingType.InOut, 0f, 1f);

                // This serves two main functions. It makes slashes more densely compacted as the attack goes on, as well as
                // giving a jerky rebound effect when the cycle goes from the end to the start again, serving as an indirect animation state
                // before Ares slashes again.
                Vector2 hoverOffsetSquishFactor = new(MathHelper.Lerp(1.2f, 0.45f, animationCompletion), 0.5f);

                float handOffsetAngle = curve.Evaluate(animationCompletion) * hand.ArmSide;
                hoverDestination = NPC.Center + hoverOffset.RotatedBy(handOffsetAngle) * hoverOffsetSquishFactor * NPC.scale;

                if (animationTimer == (int)(KatanaSlashes_AttackCycleTime * anticipationCurveEnd))
                    KatanaSlashesHandUpdate_DoSlashEffects(handNPC);

                if (animationCompletion >= anticipationCurveEnd && animationCompletion <= slashCurveEnd)
                    hand.KatanaAfterimageOpacity = 1f;
            }
            else
                handNPC.rotation = handNPC.AngleFrom(NPC.Center);

            handNPC.SmoothFlyNear(hoverDestination, 0.5f, 0.6f);

            float rotateForwardInterpolant = Utilities.InverseLerpBump(0.1f, anticipationCurveEnd * 0.89f, 0.9f, 1f, animationCompletion).Squared();
            handNPC.rotation = handNPC.AngleFrom(NPC.Center).AngleLerp(hand.ShoulderToHandDirection, rotateForwardInterpolant);
        }

        /// <summary>
        /// Performs slash effects for one of Ares' katanas during the Katana Slashes attack.
        /// </summary>
        /// <param name="handNPC">The hand's NPC instance.</param>
        public void KatanaSlashesHandUpdate_DoSlashEffects(NPC handNPC)
        {
            NPC.oldPos = new Vector2[NPC.oldPos.Length];
            NPC.oldRot = new float[NPC.oldRot.Length];
            ScreenShakeSystem.StartShakeAtPoint(NPC.Center, 2.6f);
            SoundEngine.PlaySound(Exoblade.BigSwingSound with { Volume = 0.5f, MaxInstances = 0 }, handNPC.Center);
        }

        /// <summary>
        /// Creates idle particles for one of Ares' katanas during the Katana Slashes attack.
        /// </summary>
        /// <param name="hand">The hand's ModNPC instance.</param>
        /// <param name="handNPC">The hand's NPC instance.</param>
        public void KatanaSlashesHandUpdate_CreateParticles(AresHand hand, NPC handNPC)
        {
            if (AITimer % 20 == 19 && hand.EnergyDrawer.chargeProgress >= 0.4f)
            {
                int pulseCounter = (int)MathF.Round(hand.EnergyDrawer.chargeProgress * 5f);
                hand.EnergyDrawer.AddPulse(pulseCounter);
            }

            float handSpeed = handNPC.position.Distance(handNPC.oldPosition);
            int particleCount = (int)Utils.Remap(handSpeed, 7f, 30f, 1f, 3f);

            for (int i = 0; i < particleCount; i++)
            {
                Vector2 forward = handNPC.rotation.ToRotationVector2() * handNPC.spriteDirection;
                Vector2 perpendicular = forward.RotatedBy(MathHelper.PiOver2);
                Vector2 pixelVelocity = Main.rand.NextVector2Circular(3f, 3f) + perpendicular * Main.rand.NextFromList(-1f, 1f) * 5f;
                pixelVelocity *= 1f + handSpeed * 0.03f;

                Vector2 pixelSpawnPosition = handNPC.Center + forward * Main.rand.NextFloat(30f, 280f) + pixelVelocity.SafeNormalize(Vector2.Zero) * 26f;
                float pixelScale = Main.rand.NextFloat(0.6f, 1.1f);
                BloomPixelParticle pixel = new(pixelSpawnPosition, pixelVelocity, Color.Wheat, Color.Red * 0.5f, Main.rand.Next(7, 15), Vector2.One * pixelScale);
                pixel.Spawn();
            }
        }
    }
}
