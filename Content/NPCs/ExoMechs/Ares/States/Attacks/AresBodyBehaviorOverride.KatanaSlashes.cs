using System;
using Luminance.Common.Easings;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using WoTM.Content.Particles;

namespace WoTM.Content.NPCs.ExoMechs
{
    public sealed partial class AresBodyBehaviorOverride : NPCBehaviorOverride
    {
        /// <summary>
        /// AI update loop method for the KatanaSlashes attack.
        /// </summary>
        public void DoBehavior_KatanaSlashes()
        {
            NPC.SmoothFlyNear(Target.Center - Vector2.UnitY * 350f, 0.08f, 0.95f);

            InstructionsForHands[0] = new(h => KatanaSlashesHandUpdate(h, new Vector2(-400f, 40f), 0));
            InstructionsForHands[1] = new(h => KatanaSlashesHandUpdate(h, new Vector2(-280f, 224f), 1));
            InstructionsForHands[2] = new(h => KatanaSlashesHandUpdate(h, new Vector2(280f, 224f), 2));
            InstructionsForHands[3] = new(h => KatanaSlashesHandUpdate(h, new Vector2(400f, 40f), 3));
        }

        public void KatanaSlashesHandUpdate(AresHand hand, Vector2 hoverOffset, int armIndex)
        {
            NPC handNPC = hand.NPC;
            handNPC.Opacity = Utilities.Saturate(handNPC.Opacity + 0.3f);
            hand.UsesBackArm = armIndex == 0 || armIndex == ArmCount - 1;
            hand.ArmSide = (armIndex >= ArmCount / 2).ToDirectionInt();
            hand.HandType = AresHandType.EnergyKatana;
            hand.ArmEndpoint = handNPC.Center + handNPC.velocity;
            hand.EnergyDrawer.chargeProgress = 0f;
            hand.GlowmaskDisabilityInterpolant = 0f;
            handNPC.rotation = handNPC.AngleFrom(NPC.Center);
            handNPC.spriteDirection = 1;
            handNPC.scale = MathHelper.Lerp(0.6f, 1f, Utilities.Cos01(Main.GlobalTimeWrappedHourly));

            Vector2 hoverDestination = NPC.Center + hoverOffset * NPC.scale;
            if (armIndex <= 5)
            {
                PiecewiseCurve curve = new PiecewiseCurve().
                    Add(EasingCurves.Quadratic, EasingType.InOut, -1.3f, 0.5f).
                    Add(EasingCurves.MakePoly(20f), EasingType.Out, hand.UsesBackArm ? 1.61f : 1.2f, 0.64f).
                    Add(EasingCurves.Cubic, EasingType.InOut, 0f, 1f);

                float animationCompletion = (AITimer + handNPC.whoAmI * 17) / 120f % 1f;
                float handOffsetAngle = curve.Evaluate(animationCompletion) * hand.ArmSide;
                hoverDestination = NPC.Center + hoverOffset.RotatedBy(handOffsetAngle) * new Vector2(1.5f - animationCompletion * 0.9f, 0.5f) * NPC.scale;
            }

            handNPC.SmoothFlyNear(hoverDestination, 0.5f, 0.6f);

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
