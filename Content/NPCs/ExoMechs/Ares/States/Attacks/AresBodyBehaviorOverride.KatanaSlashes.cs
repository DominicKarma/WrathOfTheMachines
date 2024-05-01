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

            if (AITimer >= 600)
                SelectNewState();

            InstructionsForHands[0] = new(h => KatanaSlashesHandUpdate(h, new Vector2(-400f, 40f), 0));
            InstructionsForHands[1] = new(h => KatanaSlashesHandUpdate(h, new Vector2(-280f, 224f), 1));
            InstructionsForHands[2] = new(h => KatanaSlashesHandUpdate(h, new Vector2(280f, 224f), 2));
            InstructionsForHands[3] = new(h => KatanaSlashesHandUpdate(h, new Vector2(400f, 40f), 3));
        }

        public void KatanaSlashesHandUpdate(AresHand hand, Vector2 hoverOffset, int armIndex)
        {
            NPC handNPC = hand.NPC;
            int attackDelay = 30;
            int attackCycleTime = 75;
            int animationTimer = (int)(AITimer + handNPC.whoAmI * 16f - attackDelay) % attackCycleTime;
            float animationCompletion = animationTimer / (float)attackCycleTime;

            handNPC.Opacity = Utilities.Saturate(handNPC.Opacity + 0.3f);
            hand.UsesBackArm = armIndex == 0 || armIndex == ArmCount - 1;
            hand.ArmSide = (armIndex >= ArmCount / 2).ToDirectionInt();
            hand.HandType = AresHandType.EnergyKatana;
            hand.ArmEndpoint = handNPC.Center + handNPC.velocity;
            hand.EnergyDrawer.chargeProgress = Utilities.InverseLerp(0f, 30f, AITimer);
            hand.GlowmaskDisabilityInterpolant = 0f;
            handNPC.spriteDirection = 1;
            handNPC.scale = MathHelper.Lerp(0.6f, 1f, Utilities.Cos01(Main.GlobalTimeWrappedHourly));

            Vector2 hoverDestination = NPC.Center + hoverOffset * NPC.scale;
            if (AITimer >= attackDelay)
            {
                PiecewiseCurve curve = new PiecewiseCurve().
                    Add(EasingCurves.Quadratic, EasingType.InOut, -1.12f, 0.45f).
                    Add(EasingCurves.MakePoly(20f), EasingType.Out, hand.UsesBackArm ? 1.68f : 1.1f, 0.59f).
                    Add(EasingCurves.Quintic, EasingType.InOut, 0f, 1f);

                float handOffsetAngle = curve.Evaluate(animationCompletion) * hand.ArmSide;
                hoverDestination = NPC.Center + hoverOffset.RotatedBy(handOffsetAngle) * new Vector2(1.2f - animationCompletion * 0.75f, 0.5f) * NPC.scale;

                if (animationTimer == (int)(attackCycleTime * 0.45f))
                {
                    NPC.oldPos = new Vector2[NPC.oldPos.Length];
                    NPC.oldRot = new float[NPC.oldRot.Length];
                    ScreenShakeSystem.StartShakeAtPoint(NPC.Center, 2.6f);
                    SoundEngine.PlaySound(Exoblade.BigSwingSound with { Volume = 0.5f, MaxInstances = 0 }, handNPC.Center);
                }
                if (animationCompletion >= 0.48f && animationCompletion <= 0.54f)
                    hand.KatanaAfterimageOpacity = 1f;

                handNPC.rotation = handNPC.AngleFrom(NPC.Center).AngleLerp(hand.ShoulderToHandDirection, Utilities.InverseLerpBump(0.1f, 0.4f, 0.9f, 1f, animationCompletion).Squared());
            }
            else
                handNPC.rotation = handNPC.AngleFrom(NPC.Center);

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
