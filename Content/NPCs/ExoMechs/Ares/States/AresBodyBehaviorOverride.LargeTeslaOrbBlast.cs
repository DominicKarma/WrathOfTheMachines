﻿using System;
using System.Linq;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WoTM.Content.NPCs.ExoMechs.Projectiles;

namespace WoTM.Content.NPCs.ExoMechs
{
    public sealed partial class AresBodyBehaviorOverride : NPCBehaviorOverride
    {
        public float LargeTeslaOrbBlast_ExplodeAnticipationInterpolant =>
            Utilities.InverseLerp(0f, LargeTeslaOrbBlast_ExplodeAnticipationTime, AITimer - LargeTeslaOrbBlast_OrbChargeUpTime - LargeTeslaOrbBlast_HomingBurstReleaseDelay - LargeTeslaOrbBlast_HomingBurstReleaseTime);

        public static int TeslaBurstDamage => Main.expertMode ? 350 : 225;

        public static int LargeTeslaOrbBlast_OrbChargeUpTime => Utilities.SecondsToFrames(2.5f);

        public static int LargeTeslaOrbBlast_HomingBurstReleaseDelay => Utilities.SecondsToFrames(1f);

        public static int LargeTeslaOrbBlast_HomingBurstReleaseTime => Utilities.SecondsToFrames(5f);

        public static int LargeTeslaOrbBlast_ExplodeAnticipationTime => Utilities.SecondsToFrames(2f);

        public static Vector2 LargeTeslaOrbBlast_BaseOrbOffset => Vector2.UnitY * 360f;

        public void DoBehavior_LargeTeslaOrbBlast()
        {
            var teslaSpheres = Utilities.AllProjectilesByID(ModContent.ProjectileType<LargeTeslaSphere>());
            float reelBackInterpolant = Utilities.InverseLerp(0f, 90f, AITimer - LargeTeslaOrbBlast_OrbChargeUpTime).Squared();
            if (!teslaSpheres.Any())
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center + LargeTeslaOrbBlast_BaseOrbOffset, Vector2.Zero, ModContent.ProjectileType<LargeTeslaSphere>(), 500, 0f);

                return;
            }

            Projectile teslaSphere = teslaSpheres.First();
            DoBehavior_LargeTeslaOrbBlast_ManageSphere(teslaSphere, reelBackInterpolant);

            Vector2 flyDestination = Target.Center + new Vector2(reelBackInterpolant * NPC.HorizontalDirectionTo(Target.Center) * -300f, -275f);
            StandardFlyTowards(flyDestination);

            bool readyToShootBursts = AITimer >= LargeTeslaOrbBlast_OrbChargeUpTime + LargeTeslaOrbBlast_HomingBurstReleaseDelay;
            bool doneShootingBursts = AITimer >= LargeTeslaOrbBlast_OrbChargeUpTime + LargeTeslaOrbBlast_HomingBurstReleaseDelay + LargeTeslaOrbBlast_HomingBurstReleaseTime;
            bool shootingBursts = readyToShootBursts && !doneShootingBursts;
            if (AITimer % 21 == 20 && shootingBursts)
                DoBehavior_LargeTeslaOrbBlast_ReleaseBurst(teslaSphere);

            // TODO -- Debug behavior, remove when attack is finished.
            if (Main.mouseRight && Main.mouseRightRelease)
            {
                AITimer = 75;

                teslaSphere.Kill();
            }

            InstructionsForHands[0] = new(h => LargeTeslaOrbBlastHandUpdate(h, teslaSphere, new Vector2(-430f, 40f), 0));
            InstructionsForHands[1] = new(h => LargeTeslaOrbBlastHandUpdate(h, teslaSphere, new Vector2(-300f, 224f), 1));
            InstructionsForHands[2] = new(h => LargeTeslaOrbBlastHandUpdate(h, teslaSphere, new Vector2(300f, 224f), 2));
            InstructionsForHands[3] = new(h => LargeTeslaOrbBlastHandUpdate(h, teslaSphere, new Vector2(430f, 40f), 3));
        }

        public void DoBehavior_LargeTeslaOrbBlast_ManageSphere(Projectile teslaSphere, float reelBackInterpolant)
        {
            Vector2 sphereHoverDestination = NPC.Center + LargeTeslaOrbBlast_BaseOrbOffset;
            sphereHoverDestination -= NPC.SafeDirectionTo(Target.Center) * reelBackInterpolant * new Vector2(200f, 50f);
            sphereHoverDestination.Y += reelBackInterpolant * 100f;

            teslaSphere.Center = Vector2.Lerp(teslaSphere.Center, sphereHoverDestination, 0.06f);
            teslaSphere.velocity += (sphereHoverDestination - teslaSphere.Center) * 0.0051f;

            float chargeUpInterpolant = Utilities.InverseLerp(0f, LargeTeslaOrbBlast_OrbChargeUpTime, AITimer);
            Vector2 teslaSphereSize = Vector2.Lerp(Vector2.One * 2f, Vector2.One * 750f, chargeUpInterpolant.Cubed());
            teslaSphereSize *= MathHelper.SmoothStep(1f, MathF.Cos(AITimer) * 0.07f + 0.4f, LargeTeslaOrbBlast_ExplodeAnticipationInterpolant);

            ScreenShakeSystem.SetUniversalRumble(LargeTeslaOrbBlast_ExplodeAnticipationInterpolant.Squared() * 3f);

            if (AITimer >= LargeTeslaOrbBlast_OrbChargeUpTime + LargeTeslaOrbBlast_HomingBurstReleaseDelay + LargeTeslaOrbBlast_HomingBurstReleaseTime + LargeTeslaOrbBlast_ExplodeAnticipationTime + 60)
            {
                teslaSphere.Kill();
                AITimer = 0;
            }

            teslaSphere.Resize((int)teslaSphereSize.X, (int)teslaSphereSize.Y);
        }

        public void DoBehavior_LargeTeslaOrbBlast_ReleaseBurst(Projectile teslaSphere)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            float burstOffsetAngle = MathF.Cos(MathHelper.TwoPi * AITimer / 120f) * MathHelper.PiOver2;
            Vector2 burstShootDirection = teslaSphere.SafeDirectionTo(Target.Center).RotatedBy(burstOffsetAngle);
            Vector2 burstSpawnPosition = teslaSphere.Center + burstShootDirection * teslaSphere.width * Main.rand.NextFloat(0.1f);
            Utilities.NewProjectileBetter(NPC.GetSource_FromAI(), burstSpawnPosition, burstShootDirection * 42f, ModContent.ProjectileType<HomingTeslaBurst>(), TeslaBurstDamage, 0f);
        }

        public void LargeTeslaOrbBlastHandUpdate(AresHand hand, Projectile teslaSphere, Vector2 hoverOffset, int armIndex)
        {
            hand.NPC.SmoothFlyNear(NPC.Center + hoverOffset * NPC.scale, 0.2f, 0.84f);
            hand.NPC.Center = NPC.Center + hoverOffset * NPC.scale;
            hand.RotateToLookAt(teslaSphere.Center);
            hand.NPC.Opacity = Utilities.Saturate(hand.NPC.Opacity + 0.2f);
            hand.UsesBackArm = armIndex == 0 || armIndex == ArmCount - 1;
            hand.ArmSide = (armIndex >= ArmCount / 2).ToDirectionInt();
            hand.HandType = AresHandType.TeslaCannon;

            hand.EnergyDrawer.chargeProgress = Utilities.InverseLerp(150f, 700f, teslaSphere.width) * 0.9999f;
            hand.EnergyDrawer.SpawnAreaCompactness = LargeTeslaOrbBlast_ExplodeAnticipationInterpolant * 100f;

            if (AITimer % 20 == 19 && hand.EnergyDrawer.chargeProgress >= 0.4f)
            {
                int pulseCounter = (int)MathF.Round(hand.EnergyDrawer.chargeProgress * 5f);
                hand.EnergyDrawer.AddPulse(pulseCounter);
            }

            int animateRate = 3;
            hand.Frame = AITimer / animateRate % 11;

            hand.NPC.velocity += Main.rand.NextVector2Circular(3f, 3f) * hand.EnergyDrawer.chargeProgress;

            float arcCreationChance = Utils.Remap(teslaSphere.width, 175f, 700f, 0.05f, 1f) * (1f - LargeTeslaOrbBlast_ExplodeAnticipationInterpolant);
            if (AITimer >= LargeTeslaOrbBlast_OrbChargeUpTime)
                arcCreationChance *= 0.4f;

            for (int i = 0; i < 2; i++)
            {
                Vector2 arcSpawnPosition = hand.NPC.Center + new Vector2(hand.NPC.spriteDirection * 54f, 8f).RotatedBy(hand.NPC.rotation);
                if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(arcCreationChance))
                {
                    Vector2 arcLength = (teslaSphere.Center - arcSpawnPosition).RotatedByRandom(0.02f) * Main.rand.NextFloat(0.97f, 1.03f);
                    Utilities.NewProjectileBetter(hand.NPC.GetSource_FromAI(), arcSpawnPosition, arcLength, ModContent.ProjectileType<SmallTeslaArc>(), 0, 0f, -1, Main.rand.Next(6, 9));
                }
            }
        }
    }
}
