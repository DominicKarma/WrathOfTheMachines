﻿using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using WoTM.Content.NPCs.ExoMechs.Projectiles;

namespace WoTM.Content.NPCs.ExoMechs
{
    public class LaserbeamDashes : ExoMechComboHandler
    {
        public override int[] ExpectedManagingExoMechs => [ModContent.NPCType<ThanatosHead>(), ModContent.NPCType<AresBody>()];

        public override bool Perform(NPC npc)
        {
            if (npc.type == ModContent.NPCType<AresBody>())
                return Perform_Ares(npc);

            Perform_Hades(npc);
            return false;
        }

        /// <summary>
        /// Performs Ares' part in the LaserbeamDashes attack.
        /// </summary>
        /// <param name="npc">Ares' NPC instance.</param>
        public static bool Perform_Ares(NPC npc)
        {
            if (!npc.TryGetBehavior(out AresBodyBehaviorOverride ares))
            {
                npc.active = false;
                return false;
            }

            int cannonChargeUpTime = Utilities.SecondsToFrames(ares.AimedLaserBursts_SweepCounter >= 1f ? 1.5f : 2.35f);
            if (AITimer == 1)
                SoundEngine.PlaySound(AresLaserCannon.TelSound);

            Vector2 hoverDestination = Target.Center + new Vector2(npc.OnRightSideOf(Target).ToDirectionInt() * 300f, -350f);
            if (MathHelper.Distance(Target.Center.X, npc.Center.X) <= 120f)
                hoverDestination.X = Target.Center.X;

            npc.Center = Vector2.Lerp(npc.Center, hoverDestination, 0.01f);
            ares.StandardFlyTowards(hoverDestination);

            ares.InstructionsForHands[0] = new(h => ares.AimedLaserBurstsHandUpdate(h, new Vector2(-430f, 50f), 0, cannonChargeUpTime));
            ares.InstructionsForHands[1] = new(h => ares.AimedLaserBurstsHandUpdate(h, new Vector2(-280f, 224f), 1, cannonChargeUpTime));
            ares.InstructionsForHands[2] = new(h => ares.AimedLaserBurstsHandUpdate(h, new Vector2(280f, 224f), 2, cannonChargeUpTime));
            ares.InstructionsForHands[3] = new(h => ares.AimedLaserBurstsHandUpdate(h, new Vector2(430f, 50f), 3, cannonChargeUpTime));

            if (AITimer >= cannonChargeUpTime + CannonLaserbeam.Lifetime + 45)
            {
                AITimer = 0;
                ares.AimedLaserBursts_SweepCounter++;
                npc.netUpdate = true;
            }

            if (ares.AimedLaserBursts_SweepCounter >= 2f)
            {
                ares.AimedLaserBursts_SweepCounter = 0f;
                ares.AimedLaserBursts_AimOffset = Vector2.Zero;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Performs Hades' part in the LaserbeamDashes attack.
        /// </summary>
        /// <param name="npc">Hades' NPC instance.</param>
        public static void Perform_Hades(NPC npc)
        {
            if (!npc.TryGetBehavior(out HadesHeadBehaviorOverride hades))
                return;

            int dashDelay = 90;
            int dashTime = 60;
            int wrappedTimer = AITimer % (dashDelay + dashTime);
            Vector2 hoverDestination = Target.Center + Target.SafeDirectionTo(npc.Center) * new Vector2(550f, 400f);

            if (wrappedTimer <= dashDelay)
            {
                Vector2 idealVelocity = npc.SafeDirectionTo(Target.Center) * 40f;
                idealVelocity = Vector2.Lerp(idealVelocity, npc.SafeDirectionTo(Target.Center) * 4f, Utilities.InverseLerp(210f, 120f, npc.Distance(hoverDestination)));

                npc.Center = Vector2.Lerp(npc.Center, hoverDestination, 0.029f);
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), 0.03f);
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.03f);

                npc.damage = 0;
                hades.BodyBehaviorAction = new(HadesHeadBehaviorOverride.EveryNthSegment(3), HadesHeadBehaviorOverride.OpenSegment());

                if (wrappedTimer == dashDelay)
                    ScreenShakeSystem.StartShake(7.5f);
            }
            else if (npc.velocity.Length() <= 120f)
            {
                npc.velocity += npc.velocity.SafeNormalize(Vector2.UnitY) * 4f;
                npc.damage = npc.defDamage;
                hades.BodyBehaviorAction = new(HadesHeadBehaviorOverride.AllSegments(), HadesHeadBehaviorOverride.OpenSegment());

                if (wrappedTimer >= dashDelay + dashTime - 30)
                    hades.BodyBehaviorAction = new(HadesHeadBehaviorOverride.AllSegments(), HadesHeadBehaviorOverride.CloseSegment());
            }

            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
        }
    }
}
