using System;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.Particles;
using Luminance.Assets;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using WoTM.Content.NPCs.ExoMechs.Projectiles;

namespace WoTM.Content.NPCs.ExoMechs
{
    public sealed partial class AresBodyBehaviorOverride : NPCBehaviorOverride
    {
        public void DoBehavior_AimedLaserBursts()
        {
            if (Main.mouseRight && Main.mouseRightRelease)
                AITimer = 0;

            if (AITimer == 1)
                SoundEngine.PlaySound(AresLaserCannon.TelSound);

            StandardFlyTowards(Target.Center + new Vector2((Target.Center.X - NPC.Center.X).NonZeroSign() * -300f, -350f));

            InstructionsForHands[0] = new(h => AimedLaserBurstsHandUpdate(h, new Vector2(-430f, 50f), 0));
            InstructionsForHands[1] = new(h => AimedLaserBurstsHandUpdate(h, new Vector2(-280f, 224f), 1));
            InstructionsForHands[2] = new(h => AimedLaserBurstsHandUpdate(h, new Vector2(280f, 224f), 2));
            InstructionsForHands[3] = new(h => AimedLaserBurstsHandUpdate(h, new Vector2(430f, 50f), 3));
        }

        public void DoBehavior_AimedLaserBursts_ReleaseBurst(Projectile teslaSphere)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            float burstOffsetAngle = MathF.Cos(MathHelper.TwoPi * AITimer / 120f) * MathHelper.PiOver2;
            Vector2 burstShootDirection = teslaSphere.SafeDirectionTo(Target.Center).RotatedBy(burstOffsetAngle);
            Vector2 burstSpawnPosition = teslaSphere.Center + burstShootDirection * teslaSphere.width * Main.rand.NextFloat(0.1f);
            Utilities.NewProjectileBetter(NPC.GetSource_FromAI(), burstSpawnPosition, burstShootDirection * 42f, ModContent.ProjectileType<HomingTeslaBurst>(), TeslaBurstDamage, 0f);
        }

        public void AimedLaserBurstsHandUpdate(AresHand hand, Vector2 hoverOffset, int armIndex)
        {
            NPC handNPC = hand.NPC;
            handNPC.SmoothFlyNear(NPC.Center + hoverOffset * NPC.scale, 0.3f, 0.8f);
            handNPC.Opacity = Utilities.Saturate(handNPC.Opacity + 0.2f);
            hand.UsesBackArm = armIndex == 0 || armIndex == ArmCount - 1;
            hand.ArmSide = (armIndex >= ArmCount / 2).ToDirectionInt();
            hand.HandType = AresHandType.LaserCannon;
            hand.ArmEndpoint = handNPC.Center + handNPC.velocity;
            hand.GlowmaskDisabilityInterpolant = 0f;
            hand.Frame = AITimer / 5 % 12;
            hand.RotateToLookAt(handNPC.AngleTo(Target.Center), 0.125f);
            hand.OptionalDrawAction = () =>
            {
                float sizeFadeout = Utilities.InverseLerp(1f, 0.83f, hand.EnergyDrawer.chargeProgress).Cubed();
                float opacity = Utilities.InverseLerp(0f, 60f, AITimer);
                float telegraphSize = (MathF.Cos(hand.NPC.position.X / 390f + AITimer / 23f) * 132f + 500f) * MathF.Sqrt(hand.EnergyDrawer.chargeProgress) * sizeFadeout;
                RenderLaserTelegraph(hand, opacity, sizeFadeout, telegraphSize, handNPC.rotation.ToRotationVector2() * handNPC.spriteDirection);
            };
            hand.EnergyDrawer.chargeProgress = Utilities.InverseLerp(0f, 200f, AITimer + NPC.whoAmI * 101f % 30f);
            if (hand.EnergyDrawer.chargeProgress >= 1f)
                hand.EnergyDrawer.chargeProgress = 0f;

            // Jitter in place.
            handNPC.velocity += Main.rand.NextVector2CircularEdge(0.27f, 1.6f) * hand.EnergyDrawer.chargeProgress.Squared();

            if (AITimer % 15 == 14 && hand.EnergyDrawer.chargeProgress >= 0.4f)
            {
                int pulseCounter = (int)MathF.Round(hand.EnergyDrawer.chargeProgress * 5f);
                hand.EnergyDrawer.AddPulse(pulseCounter);
            }

            // Charge energy in anticipation of the laser shot.
            if (hand.EnergyDrawer.chargeProgress < 0.9f)
            {
                Vector2 cannonEnd = handNPC.Center + new Vector2(handNPC.spriteDirection * 74f, 16f).RotatedBy(handNPC.rotation);
                float chargeUpCompletion = hand.EnergyDrawer.chargeProgress;
                float particleSpawnChance = Utilities.InverseLerp(0f, 0.85f, chargeUpCompletion).Squared();
                Vector2 aimDirection = NPC.SafeDirectionTo(cannonEnd);
                for (int i = 0; i < 2; i++)
                {
                    if (Main.rand.NextBool(particleSpawnChance))
                    {
                        Color energyColor = Color.Lerp(Color.Red, Color.Orange, Main.rand.NextFloat(0.24f));
                        energyColor = Color.Lerp(energyColor, Color.Wheat, Main.rand.NextBool(chargeUpCompletion) ? 0.75f : 0.1f);

                        Vector2 energySpawnPosition = cannonEnd + Main.rand.NextVector2Unit() * Main.rand.NextFloat(36f, chargeUpCompletion.Squared() * 100f + 80f);
                        Vector2 energyVelocity = (cannonEnd - energySpawnPosition) * 0.16f;
                        LineParticle energy = new(energySpawnPosition, energyVelocity, false, Main.rand.Next(6, 12), chargeUpCompletion * 0.9f, energyColor);
                        GeneralParticleHandler.SpawnParticle(energy);
                    }
                }

                // Release bursts of bloom over the cannon.
                if (AITimer % 6 == 0)
                {
                    StrongBloom bloom = new(cannonEnd + handNPC.velocity, handNPC.velocity, Color.Wheat * MathHelper.Lerp(0.4f, 1.1f, chargeUpCompletion), chargeUpCompletion * 0.5f, 12);
                    GeneralParticleHandler.SpawnParticle(bloom);

                    bloom = new(cannonEnd + handNPC.velocity, handNPC.velocity, Color.OrangeRed * MathHelper.Lerp(0.2f, 0.6f, chargeUpCompletion), chargeUpCompletion * 0.84f, 9);
                    GeneralParticleHandler.SpawnParticle(bloom);
                }
            }

            // Make the screen shake slightly before firing.
            ScreenShakeSystem.SetUniversalRumble(hand.EnergyDrawer.chargeProgress.Cubed() * 3f);
        }

        public static void RenderLaserTelegraph(AresHand hand, float opacity, float telegraphIntensityFactor, float telegraphSize, Vector2 telegraphDirection)
        {
            PrimitivePixelationSystem.RenderToPrimsNextFrame(() =>
            {
                RenderLaserTelegraphWrapper(hand, opacity, telegraphIntensityFactor, telegraphSize, telegraphDirection);
            }, PixelationPrimitiveLayer.AfterNPCs);
        }

        public static void RenderLaserTelegraphWrapper(AresHand hand, float opacity, float telegraphIntensityFactor, float telegraphSize, Vector2 telegraphDirection)
        {
            NPC handNPC = hand.NPC;
            Vector2 start = handNPC.Center + new Vector2(handNPC.spriteDirection * 74f, 16f).RotatedBy(handNPC.rotation);
            Texture2D invisible = MiscTexturesRegistry.InvisiblePixel.Value;

            // The multiplication by 0.5 is because this is being rendered to the pixelation target, wherein everything is downscaled by a factor of two, so that it can be upscaled later.
            Vector2 drawPosition = (start - Main.screenPosition) * 0.5f;

            Effect spread = Filters.Scene["CalamityMod:SpreadTelegraph"].GetShader().Shader;
            spread.Parameters["centerOpacity"].SetValue(0.4f);
            spread.Parameters["mainOpacity"].SetValue(opacity * 0.7f);
            spread.Parameters["halfSpreadAngle"].SetValue((1.011f - Utilities.Saturate(opacity) + Utilities.Cos01(Main.GlobalTimeWrappedHourly * 2f + start.X / 99f) * 0.02f) * 1.6f);
            spread.Parameters["edgeColor"].SetValue(Vector3.Lerp(new(1.3f, 0.1f, 0.67f), new(4f, 0.6f, 0.08f), telegraphIntensityFactor));
            spread.Parameters["centerColor"].SetValue(new Vector3(1f, 0.1f, 0.1f));
            spread.Parameters["edgeBlendLength"].SetValue(0.07f);
            spread.Parameters["edgeBlendStrength"].SetValue(32f);
            spread.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Draw(invisible, drawPosition, null, Color.White, telegraphDirection.ToRotation(), invisible.Size() * 0.5f, Vector2.One * opacity * telegraphSize, SpriteEffects.None, 0f);
        }
    }
}
