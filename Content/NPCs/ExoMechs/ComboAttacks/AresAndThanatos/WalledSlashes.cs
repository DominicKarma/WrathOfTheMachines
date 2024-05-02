using System;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WoTM.Content.NPCs.ExoMechs.Projectiles;

namespace WoTM.Content.NPCs.ExoMechs
{
    public class WalledSlashes : ExoMechComboHandler
    {
        public override int[] ExpectedManagingExoMechs => [ModContent.NPCType<ThanatosHead>(), ModContent.NPCType<AresBody>()];

        public override bool Perform(NPC npc)
        {
            if (npc.type == ModContent.NPCType<AresBody>())
                return Perform_Ares(npc);

            Perform_Hades(npc);

            return false;
        }

        public static bool Perform_Ares(NPC npc)
        {
            if (!npc.TryGetBehavior(out AresBodyBehaviorOverride ares))
            {
                npc.active = false;
                return false;
            }

            int slashCycleTime = Utilities.SecondsToFrames(1.31f);
            ares.InstructionsForHands[0] = new(h => ares.KatanaSlashesHandUpdate(h, new Vector2(-400f, 40f), 0, slashCycleTime, 0));
            ares.InstructionsForHands[1] = new(h => ares.KatanaSlashesHandUpdate(h, new Vector2(-280f, 224f), 0, slashCycleTime, 1));
            ares.InstructionsForHands[2] = new(h => ares.KatanaSlashesHandUpdate(h, new Vector2(280f, 224f), 0, slashCycleTime, 2));
            ares.InstructionsForHands[3] = new(h => ares.KatanaSlashesHandUpdate(h, new Vector2(400f, 40f), 0, slashCycleTime, 3));

            Vector2 hoverDestination = Target.Center - Vector2.UnitY * 300f;
            float arcAwayInterpolant = Utilities.InverseLerp(206f, 155f, npc.Distance(hoverDestination));
            Vector2 idealDirection = npc.SafeDirectionTo(hoverDestination).RotatedBy(MathHelper.Pi * arcAwayInterpolant);
            npc.velocity += idealDirection * new Vector2(0.6f, 0.24f);
            if (npc.velocity.AngleBetween(idealDirection) >= 1.3f)
                npc.velocity *= 0.84f;

            return false;
        }

        public static void Perform_Hades(NPC npc)
        {
            if (!npc.TryGetBehavior(out HadesHeadBehaviorOverride hades))
            {
                npc.active = false;
                return;
            }

            if (npc.ai[0] == 0f)
            {
                npc.ai[0] = Target.Center.X;
                npc.ai[1] = Target.Center.Y;
                npc.netUpdate = true;
            }

            float spinAngle = MathHelper.TwoPi * AITimer / 90f;
            Vector2 circularOffset = spinAngle.ToRotationVector2() * 1000f;
            Vector2 rectangularOffset = new(MathF.Sign(circularOffset.X), MathF.Sign(circularOffset.Y));

            Vector2 hoverDestination = new Vector2(npc.ai[0], npc.ai[1]) + circularOffset;
            npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(hoverDestination) * 72f, 0.3f);
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            if (Main.netMode != NetmodeID.MultiplayerClient && AITimer % 20 == 19)
                Utilities.NewProjectileBetter(npc.GetSource_FromAI(), npc.Center, npc.SafeDirectionTo(Target.Center) * Main.rand.NextFloat(25f, 80f), ModContent.ProjectileType<HadesMine>(), HadesHeadBehaviorOverride.MineDamage, 0f);

            hades.BodyBehaviorAction = new(HadesHeadBehaviorOverride.EveryNthSegment(3), HadesHeadBehaviorOverride.OpenSegment());
        }
    }
}
