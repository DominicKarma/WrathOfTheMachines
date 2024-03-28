using System;
using System.Linq;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WoTM.Content.NPCs.ExoMechs.Projectiles;

namespace WoTM.Content.NPCs.ExoMechs
{
    public sealed partial class AresBodyBehaviorOverride : NPCBehaviorOverride
    {
        public static int LargeTeslaOrbBlast_OrbChargeUpTime => Utilities.SecondsToFrames(2.5f);

        public void DoBehavior_LargeTeslaOrbBlast()
        {
            var teslaSpheres = Utilities.AllProjectilesByID(ModContent.ProjectileType<LargeTeslaSphere>());
            Vector2 sphereHoverDestination = NPC.Center + Vector2.UnitY * 360f;
            if (!teslaSpheres.Any())
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(NPC.GetSource_FromAI(), sphereHoverDestination, Vector2.Zero, ModContent.ProjectileType<LargeTeslaSphere>(), 500, 0f);

                return;
            }

            float reelBackInterpolant = Utilities.InverseLerp(0f, 90f, AITimer - LargeTeslaOrbBlast_OrbChargeUpTime).Squared();
            sphereHoverDestination -= NPC.SafeDirectionTo(Target.Center) * reelBackInterpolant * new Vector2(300f, 50f);
            sphereHoverDestination.Y += reelBackInterpolant * 100f;

            Projectile teslaSphere = teslaSpheres.First();
            teslaSphere.Center = Vector2.Lerp(teslaSphere.Center, sphereHoverDestination, 0.04f);
            teslaSphere.velocity += (sphereHoverDestination - teslaSphere.Center) * 0.0051f;

            float chargeUpInterpolant = Utilities.InverseLerp(0f, LargeTeslaOrbBlast_OrbChargeUpTime, AITimer);
            Vector2 teslaSphereSize = Vector2.Lerp(Vector2.One * 2f, Vector2.One * 750f, chargeUpInterpolant.Cubed());
            teslaSphere.Resize((int)teslaSphereSize.X, (int)teslaSphereSize.Y);

            Vector2 flyDestination = Target.Center + new Vector2(reelBackInterpolant * NPC.HorizontalDirectionTo(Target.Center) * -300f, -275f);
            StandardFlyTowards(flyDestination);

            if (Main.mouseRight && Main.mouseRightRelease)
            {
                teslaSphere.Resize(120, 120);
                AITimer = 0;
            }

            InstructionsForHands[0] = new(h => LargeTeslaOrbBlastHandUpdate(h, teslaSphere, new Vector2(-430f, 40f), 0));
            InstructionsForHands[1] = new(h => LargeTeslaOrbBlastHandUpdate(h, teslaSphere, new Vector2(-300f, 224f), 1));
            InstructionsForHands[2] = new(h => LargeTeslaOrbBlastHandUpdate(h, teslaSphere, new Vector2(300f, 224f), 2));
            InstructionsForHands[3] = new(h => LargeTeslaOrbBlastHandUpdate(h, teslaSphere, new Vector2(430f, 40f), 3));
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
            hand.EnergyDrawer.SpawnAreaCompactness = 100f;

            if (AITimer % 20 == 19 && hand.EnergyDrawer.chargeProgress >= 0.4f)
            {
                int pulseCounter = (int)MathF.Round(hand.EnergyDrawer.chargeProgress * 5f);
                hand.EnergyDrawer.AddPulse(pulseCounter);
            }

            int animateRate = 3;
            hand.Frame = AITimer / animateRate % 11;

            hand.NPC.velocity += Main.rand.NextVector2Circular(3f, 3f) * hand.EnergyDrawer.chargeProgress;

            float arcCreationChance = Utils.Remap(teslaSphere.width, 175f, 700f, 0.05f, 1f);
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
