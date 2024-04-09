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
using WoTM.Content.Particles;

namespace WoTM.Content.NPCs.ExoMechs
{
    public sealed partial class AresBodyBehaviorOverride : NPCBehaviorOverride
    {
        /// <summary>
        /// How long it takes for cannons to charge up energy during the AimedLaserBursts attack.
        /// </summary>
        public static int AimedLaserBursts_CannonChargeUpTime => Utilities.SecondsToFrames(2.35f);

        /// <summary>
        /// How much damage laserbeams from Ares' laser cannons do.
        /// </summary>
        public static int CannonLaserbeamDamage => Main.expertMode ? 550 : 400;

        public void DoBehavior_AimedLaserBursts()
        {
            if (Main.mouseRight && Main.mouseRightRelease)
                AITimer = 0;

            if (AITimer == 1)
                SoundEngine.PlaySound(AresLaserCannon.TelSound);

            Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X - NPC.Center.X).NonZeroSign() * -300f, -350f);
            if (MathHelper.Distance(Target.Center.X, NPC.Center.X) <= 120f)
                hoverDestination.X = Target.Center.X;

            NPC.Center = Vector2.Lerp(NPC.Center, hoverDestination, 0.005f);
            StandardFlyTowards(hoverDestination);

            InstructionsForHands[0] = new(h => AimedLaserBurstsHandUpdate(h, new Vector2(-430f, 50f), 0));
            InstructionsForHands[1] = new(h => AimedLaserBurstsHandUpdate(h, new Vector2(-280f, 224f), 1));
            InstructionsForHands[2] = new(h => AimedLaserBurstsHandUpdate(h, new Vector2(280f, 224f), 2));
            InstructionsForHands[3] = new(h => AimedLaserBurstsHandUpdate(h, new Vector2(430f, 50f), 3));

            if (AITimer >= AimedLaserBursts_CannonChargeUpTime + CannonLaserbeam.Lifetime + 45)
            {
                CurrentState = AresAIState.DetachHands;
                AITimer = 0;
                NPC.netUpdate = true;
            }
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
            hand.OptionalDrawAction = () =>
            {
                float sizeFadeout = Utilities.InverseLerp(1f, 0.83f, hand.EnergyDrawer.chargeProgress).Cubed();
                float opacity = Utilities.InverseLerp(0f, 60f, AITimer);
                float telegraphSize = (MathF.Cos(hand.NPC.position.X / 390f + AITimer / 23f) * 132f + 500f) * MathF.Sqrt(hand.EnergyDrawer.chargeProgress) * sizeFadeout;
                RenderLaserTelegraph(hand, opacity, sizeFadeout, telegraphSize, handNPC.rotation.ToRotationVector2() * handNPC.spriteDirection);
            };

            int relativeTimer = AITimer + handNPC.whoAmI * 101 % 30;
            hand.EnergyDrawer.chargeProgress = Utilities.InverseLerp(0f, AimedLaserBursts_CannonChargeUpTime, relativeTimer);
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
            Vector2 cannonEnd = handNPC.Center + new Vector2(handNPC.spriteDirection * 74f, 16f).RotatedBy(handNPC.rotation);
            Vector2 aimDirection = NPC.SafeDirectionTo(cannonEnd);
            if (hand.EnergyDrawer.chargeProgress < 0.9f)
            {
                float chargeUpCompletion = hand.EnergyDrawer.chargeProgress;
                float particleSpawnChance = Utilities.InverseLerp(0f, 0.85f, chargeUpCompletion).Squared();
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

            // Fire.
            if (relativeTimer == AimedLaserBursts_CannonChargeUpTime)
            {
                ScreenShakeSystem.StartShake(7.5f);

                SoundEngine.PlaySound(AresLaserCannon.LaserbeamShootSound, handNPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(NPC.GetSource_FromAI(), handNPC.Center, handNPC.rotation.ToRotationVector2() * handNPC.spriteDirection, ModContent.ProjectileType<CannonLaserbeam>(), CannonLaserbeamDamage, 0f, -1, handNPC.whoAmI);
            }

            if (relativeTimer < AimedLaserBursts_CannonChargeUpTime)
                hand.RotateToLookAt(handNPC.AngleTo(Target.Center), 0.125f);

            // Handle post-firing particles.
            else
            {
                // Release bursts of bloom over the cannon.
                float bloomScaleFactor = MathHelper.Lerp(0.9f, 1.1f, Utilities.Cos01(handNPC.whoAmI * 3f + relativeTimer));
                if (AITimer % 6 == 0)
                {
                    StrongBloom bloom = new(cannonEnd + handNPC.velocity, handNPC.velocity, Color.White, bloomScaleFactor * 0.5f, 12);
                    GeneralParticleHandler.SpawnParticle(bloom);

                    bloom = new(cannonEnd + handNPC.velocity, handNPC.velocity, new(1f, 0.11f, 0.05f), bloomScaleFactor * 0.84f, 11);
                    GeneralParticleHandler.SpawnParticle(bloom);
                }

                Vector2 energyDirection = (handNPC.rotation.ToRotationVector2() * handNPC.spriteDirection).RotatedByRandom(0.6f);
                BloomPixelParticle energy = new(cannonEnd, energyDirection * Main.rand.NextFloat(3f, 15f), Color.White, Color.Red * 0.45f, 20, Vector2.One);
                energy.Spawn();

                handNPC.velocity -= handNPC.rotation.ToRotationVector2() * handNPC.spriteDirection * 2f + Main.rand.NextVector2Circular(0.5f, 0.5f);

                // Attempt to aim at the target.
                // This doesn't use hand.RotateToLookAt because that changes the spriteDirection of the cannon.
                // For most cases this looks natural, but when a laser is being fired from the cannon it's super important that it never "jump" in terms of position.
                // By locking the spriteDirection in place and just rotating normally, this issue is avoided.
                float cannonTurnSpeed = Utilities.InverseLerp(0f, 45f, relativeTimer - AimedLaserBursts_CannonChargeUpTime) * 0.085f;
                float idealCannonRotation = handNPC.AngleTo(Target.Center);
                if (handNPC.spriteDirection == -1)
                    idealCannonRotation += MathHelper.Pi;

                handNPC.rotation = handNPC.rotation.AngleLerp(idealCannonRotation, cannonTurnSpeed);
            }
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
            spread.Parameters["halfSpreadAngle"].SetValue((1.0356f - Utilities.Saturate(opacity) + Utilities.Cos01(Main.GlobalTimeWrappedHourly * 2f + start.X / 99f) * 0.01f) * 1.6f);
            spread.Parameters["edgeColor"].SetValue(Vector3.Lerp(new(1.3f, 0.1f, 0.67f), new(4f, 0.6f, 0.08f), telegraphIntensityFactor));
            spread.Parameters["centerColor"].SetValue(new Vector3(1f, 0.1f, 0.1f));
            spread.Parameters["edgeBlendLength"].SetValue(0.07f);
            spread.Parameters["edgeBlendStrength"].SetValue(32f);
            spread.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Draw(invisible, drawPosition, null, Color.White, telegraphDirection.ToRotation(), invisible.Size() * 0.5f, Vector2.One * opacity * telegraphSize, SpriteEffects.None, 0f);
        }
    }
}
