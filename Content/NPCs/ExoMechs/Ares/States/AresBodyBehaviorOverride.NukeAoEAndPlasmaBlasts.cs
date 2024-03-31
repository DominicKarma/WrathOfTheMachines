using System;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.Sounds;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WoTM.Content.NPCs.ExoMechs.Projectiles;

namespace WoTM.Content.NPCs.ExoMechs
{
    public sealed partial class AresBodyBehaviorOverride : NPCBehaviorOverride
    {
        /// <summary>
        /// How long Ares spends charging up before firing his nuke during the NukeAoEAndPlasmaBlasts attack.
        /// </summary>
        public static int NukeAoEAndPlasmaBlasts_NukeChargeUpTime => Utilities.SecondsToFrames(2.54f);

        /// <summary>
        /// How long Ares' nuke waits before detonating during the NukeAoEAndPlasmaBlasts attack.
        /// </summary>
        public static int NukeAoEAndPlasmaBlasts_NukeExplosionDelay => Utilities.SecondsToFrames(5.6f);

        /// <summary>
        /// How big the nuke explosion should be during the NukeAoEAndPlasmaBlasts attack.
        /// </summary>
        public static float NukeAoEAndPlasmaBlasts_NukeExplosionRadius => 2900f;

        /// <summary>
        /// How much Ares' nuke (not its resulting explosion!) does.
        /// </summary>
        public static int NukeWeaponDamage => 500;

        public void DoBehavior_NukeAoEAndPlasmaBlasts()
        {
            if (AITimer == 1)
            {
                SoundEngine.PlaySound(AresPlasmaFlamethrower.TelSound);
                SoundEngine.PlaySound(AresGaussNuke.TelSound);
            }

            if (Main.mouseRight)
                AITimer = 0;

            Vector2 flyDestination = Target.Center - Vector2.UnitY * 350f;
            StandardFlyTowards(flyDestination);

            InstructionsForHands[0] = new(h => NukeAoEAndPlasmaBlastsHandUpdate(h, new Vector2(-400f, 40f), 0));
            InstructionsForHands[1] = new(h => NukeAoEAndPlasmaBlastsHandUpdate(h, new Vector2(-280f, 224f), 1));
            InstructionsForHands[2] = new(h => NukeAoEAndPlasmaBlastsHandUpdate(h, new Vector2(280f, 224f), 2));
            InstructionsForHands[3] = new(h => NukeAoEAndPlasmaBlastsHandUpdate(h, new Vector2(400f, 40f), 3));
        }

        public void DoBehavior_NukeAoEAndPlasmaBlasts_ReleaseBurst(Projectile teslaSphere)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            float burstOffsetAngle = MathF.Cos(MathHelper.TwoPi * AITimer / 120f) * MathHelper.PiOver2;
            Vector2 burstShootDirection = teslaSphere.SafeDirectionTo(Target.Center).RotatedBy(burstOffsetAngle);
            Vector2 burstSpawnPosition = teslaSphere.Center + burstShootDirection * teslaSphere.width * Main.rand.NextFloat(0.1f);
            Utilities.NewProjectileBetter(NPC.GetSource_FromAI(), burstSpawnPosition, burstShootDirection * 42f, ModContent.ProjectileType<HomingTeslaBurst>(), TeslaBurstDamage, 0f);
        }

        public void NukeAoEAndPlasmaBlastsHandUpdate(AresHand hand, Vector2 hoverOffset, int armIndex)
        {
            NPC handNPC = hand.NPC;
            handNPC.Center = NPC.Center + hoverOffset * NPC.scale;
            handNPC.Opacity = Utilities.Saturate(handNPC.Opacity + 0.2f);
            hand.UsesBackArm = armIndex == 0 || armIndex == ArmCount - 1;
            hand.ArmSide = (armIndex >= ArmCount / 2).ToDirectionInt();
            hand.HandType = armIndex == ArmCount - 1 ? AresHandType.GaussNuke : AresHandType.PlasmaCannon;
            hand.ArmEndpoint = handNPC.Center + handNPC.velocity;
            hand.EnergyDrawer.chargeProgress = Utilities.InverseLerp(0f, NukeAoEAndPlasmaBlasts_NukeChargeUpTime, AITimer);
            if (hand.EnergyDrawer.chargeProgress >= 1f)
                hand.EnergyDrawer.chargeProgress = 0f;

            hand.GlowmaskDisabilityInterpolant = 0f;
            hand.RotateToLookAt(Target.Center);

            if (AITimer % 20 == 19 && hand.EnergyDrawer.chargeProgress >= 0.4f)
            {
                int pulseCounter = (int)MathF.Round(hand.EnergyDrawer.chargeProgress * 5f);
                hand.EnergyDrawer.AddPulse(pulseCounter);
            }

            if (hand.HandType == AresHandType.GaussNuke)
            {
                if (hand.EnergyDrawer.chargeProgress > 0f)
                    hand.Frame = (int)(hand.EnergyDrawer.chargeProgress * 34f);
                else
                {
                    if (AITimer % 5 == 2)
                        hand.Frame++;
                    if (hand.Frame >= 24)
                    {
                        if (hand.Frame >= 80)
                            hand.Frame = 0;
                    }
                    else
                        hand.Frame %= 23;
                }

                if (AITimer == NukeAoEAndPlasmaBlasts_NukeChargeUpTime - 12)
                {
                    SoundEngine.PlaySound(CommonCalamitySounds.LargeWeaponFireSound, handNPC.Center);
                    ScreenShakeSystem.StartShakeAtPoint(handNPC.Center, 6f);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 handDirection = handNPC.rotation.ToRotationVector2() * handNPC.spriteDirection;
                        Vector2 nukeSpawnPosition = handNPC.Center + handDirection * 40f;
                        Vector2 nukeVelocity = handDirection * 80f;
                        Utilities.NewProjectileBetter(handNPC.GetSource_FromAI(), nukeSpawnPosition, nukeVelocity, ModContent.ProjectileType<GaussNuke>(), NukeWeaponDamage, 0f);
                        handNPC.netUpdate = true;
                    }
                }
            }
            else
                hand.Frame = AITimer / 3 % 12;
        }
    }
}
