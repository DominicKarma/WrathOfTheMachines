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
        /// <summary>
        /// How long the walled slashes attack goes on for until a new combo is selected.
        /// </summary>
        public static int AttackDuration => Utilities.SecondsToFrames(8f);

        /// <summary>
        /// Ares' slash cycle time, which dictates how fast each set of slashes are.
        /// </summary>
        public static int AresSlashCycleTime => Utilities.SecondsToFrames(1.32f);

        /// <summary>
        /// How long Ares waits before slashing.
        /// </summary>
        public static int AresSlashDelay => Utilities.SecondsToFrames(1f);

        /// <summary>
        /// The max speed at which Ares can fly when trying to reach the player.
        /// </summary>
        public static float AresMaxFlySpeed => 56f;

        /// <summary>
        /// How far vertically Ares attempts to hover relative to the target's position.
        /// </summary>
        public static float AresVerticalHoverOffset => -300f;

        /// <summary>
        /// Ares' fly acceleration while he attempts to slash the player.
        /// </summary>
        public static Vector2 AresAcceleration => new(0.51f, 0.24f);

        /// <summary>
        /// The rate at which Hades releases mines.
        /// </summary>
        public static int HadesMineReleaseRate => Utilities.SecondsToFrames(0.42f);

        /// <summary>
        /// The radius at which Hades spins around his focal point.
        /// </summary>
        public static float HadesSpinRadius => 1000f;

        /// <summary>
        /// The speed at which Hades spins around his focal point.
        /// </summary>
        public static float HadesSpinSpeed => 72f;

        /// <summary>
        /// How slowly Hades rotates around his focal point.
        /// </summary>
        public static float HadesSpinPeriod => 90f;

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

            ares.InstructionsForHands[0] = new(h => ares.KatanaSlashesHandUpdate(h, new Vector2(-400f, 40f), AresSlashDelay, AresSlashCycleTime, 0));
            ares.InstructionsForHands[1] = new(h => ares.KatanaSlashesHandUpdate(h, new Vector2(-280f, 224f), AresSlashDelay, AresSlashCycleTime, 1));
            ares.InstructionsForHands[2] = new(h => ares.KatanaSlashesHandUpdate(h, new Vector2(280f, 224f), AresSlashDelay, AresSlashCycleTime, 2));
            ares.InstructionsForHands[3] = new(h => ares.KatanaSlashesHandUpdate(h, new Vector2(400f, 40f), AresSlashDelay, AresSlashCycleTime, 3));

            float movementWindUpInterpolant = Utilities.InverseLerp(0f, AresSlashDelay, AITimer).Squared();
            Vector2 hoverDestination = Target.Center + Vector2.UnitY * AresVerticalHoverOffset;
            Vector2 idealDirection = npc.SafeDirectionTo(hoverDestination);
            Vector2 acceleration = AresAcceleration * movementWindUpInterpolant;

            npc.velocity = (npc.velocity + idealDirection * acceleration).ClampLength(0f, AresMaxFlySpeed);
            if (npc.velocity.AngleBetween(idealDirection) >= 1.37f)
                npc.velocity *= 0.84f;

            bool attackHasCompleted = AITimer >= AttackDuration;
            return attackHasCompleted;
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

            float spinAngle = MathHelper.TwoPi * AITimer / HadesSpinPeriod;
            Vector2 spinOffset = spinAngle.ToRotationVector2() * HadesSpinRadius;
            Vector2 hoverDestination = new Vector2(npc.ai[0], npc.ai[1]) + spinOffset;
            npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(hoverDestination) * HadesSpinSpeed, 0.3f);
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            if (Main.netMode != NetmodeID.MultiplayerClient && AITimer % HadesMineReleaseRate == HadesMineReleaseRate - 1)
                Utilities.NewProjectileBetter(npc.GetSource_FromAI(), npc.Center, npc.SafeDirectionTo(Target.Center) * Main.rand.NextFloat(25f, 80f), ModContent.ProjectileType<HadesMine>(), HadesHeadBehaviorOverride.MineDamage, 0f);

            hades.BodyBehaviorAction = new(HadesHeadBehaviorOverride.EveryNthSegment(3), HadesHeadBehaviorOverride.OpenSegment());
        }
    }
}
