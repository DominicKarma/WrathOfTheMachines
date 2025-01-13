﻿using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WoTM.Common.Utilities;
using WoTM.Content.NPCs.ExoMechs.ArtemisAndApollo;
using WoTM.Content.NPCs.ExoMechs.FightManagers;
using WoTM.Content.NPCs.ExoMechs.Hades;
using WoTM.Content.NPCs.ExoMechs.Projectiles;
using WoTM.Core.BehaviorOverrides;

namespace WoTM.Content.NPCs.ExoMechs.ComboAttacks.HadesAndTwins
{
    public class ExothermalLaserDashes : ExoMechComboHandler
    {
        /// <summary>
        /// The spin angle at which the Exo Twins should orient themselves in accordance with.
        /// </summary>
        public static float ExoTwinSpinAngle
        {
            get
            {
                if (!Main.npc.IndexInRange(CalamityGlobalNPC.draedonExoMechWorm))
                    return 0f;

                NPC hades = Main.npc[CalamityGlobalNPC.draedonExoMechWorm];
                return hades.ai[0];
            }
            set
            {
                if (!Main.npc.IndexInRange(CalamityGlobalNPC.draedonExoMechWorm))
                    return;

                NPC hades = Main.npc[CalamityGlobalNPC.draedonExoMechWorm];
                hades.ai[0] = value;
            }
        }

        /// <summary>
        /// How long the Exo Twins spend redirecting before firing their laserbeams.
        /// </summary>
        public static int RedirectTime => Variables.GetAIInt("ExothermalLaserDashes_RedirectTime", ExoMechAIVariableType.Combo);

        /// <summary>
        /// How long it takes for the Exo Twins to spin at full angular velocity.
        /// </summary>
        public static int ExoTwinSpinWindUpTime => Variables.GetAIInt("ExothermalLaserDashes_ExoTwinSpinWindUpTime", ExoMechAIVariableType.Combo);

        /// <summary>
        /// How much damage blazing exo laserbeams from the Exo Twins do.
        /// </summary>
        public static int BlazingLaserbeamDamage => Variables.GetAIInt("BlazingLaserbeamDamage", ExoMechAIVariableType.Combo);

        /// <summary>
        /// How far the Exo Twins should be away from Hades' head when spinning.
        /// </summary>
        public static float ExoTwinSpinRadius => Variables.GetAIFloat("ExothermalLaserDashes_ExoTwinSpinRadius", ExoMechAIVariableType.Combo);

        /// <summary>
        /// The angular velocity of the Exo Twins.
        /// </summary>
        public static float ExoTwinSpinAngularVelocity => MathHelper.ToRadians(Variables.GetAIFloat("ExothermalLaserDashes_ExoTwinSpinAngularVelocityDegrees", ExoMechAIVariableType.Combo));

        public override int[] ExpectedManagingExoMechs => [ModContent.NPCType<ThanatosHead>(), ModContent.NPCType<Apollo>()];

        public override bool Perform(NPC npc)
        {
            if (npc.type == ExoMechNPCIDs.HadesHeadID)
            {
                Perform_Hades(npc);

                // This is executed by Hades since unlike the Exo Twins there is only one instance of him, and as such he can be counted on for
                // storing and executing attack data.
                // Furthermore, parts of the state rely on him specifically, particularly the part that specifies what value the spin angle should begin at, which is related to Hades' position relative to the target.
                HandleAttackState(npc);
            }
            if (npc.type == ExoMechNPCIDs.ArtemisID || npc.type == ExoMechNPCIDs.ApolloID)
                Perform_ExoTwin(npc);

            return AITimer >= RedirectTime + BlazingExoLaserbeam.Lifetime;
        }

        /// <summary>
        /// Performs Hades' part in the ExothermalLaserDashes attack.
        /// </summary>
        /// <param name="npc">Hades' NPC instance.</param>
        public static void Perform_Hades(NPC npc)
        {
            if (!npc.TryGetBehavior(out HadesHeadBehavior hades))
                return;

            hades.BodyBehaviorAction = new(HadesHeadBehavior.EveryNthSegment(3), segment =>
            {
                HadesHeadBehavior.OpenSegment(HadesHeadBehavior.StandardSegmentOpenRate, 0f).Invoke(segment);
                segment.NPC.damage = segment.NPC.defDamage;
            });
            hades.SegmentReorientationStrength = 0.07f;

            // Get to the player in the first few frames.
            float approachPlayerInterpolant = LumUtils.InverseLerp(0f, 15f, AITimer) * LumUtils.InverseLerp(400f, 600f, npc.Distance(Target.Center));
            npc.Center = Vector2.Lerp(npc.Center, Target.Center, approachPlayerInterpolant * 0.075f);

            npc.damage = 0;
            if (AITimer % 120 >= 95 && npc.velocity.AngleBetween(npc.SafeDirectionTo(Target.Center)) <= MathHelper.Pi * 0.41667f)
            {
                npc.velocity = (npc.velocity * 1.065f + npc.velocity.SafeNormalize(Vector2.UnitY) * 4f).ClampLength(0f, 50f);
                npc.damage = npc.defDamage;
            }
            else if (AITimer % 120 >= 65)
                npc.velocity *= 0.93f;
            else
            {
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(Target.Center) * 10f, 0.04f);
                npc.velocity += npc.SafeDirectionTo(Target.Center) * 0.7f;
            }

            npc.rotation = (npc.position - npc.oldPosition).ToRotation() + MathHelper.PiOver2;
        }

        /// <summary>
        /// Performs The Exo Twins' part in the ExothermalLaserDashes attack.
        /// </summary>
        /// <param name="npc">The Exo Twins' NPC instance.</param>
        public static void Perform_ExoTwin(NPC npc)
        {
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.draedonExoMechWorm))
                return;

            NPC hades = Main.npc[CalamityGlobalNPC.draedonExoMechWorm];

            float hoverOffsetAngle = ExoTwinSpinAngle;
            float flyAwayInterpolant = LumUtils.InverseLerp(RedirectTime + BlazingExoLaserbeam.Lifetime - 90f, RedirectTime + BlazingExoLaserbeam.Lifetime, AITimer);
            float hoverFlySpeedInterpolant = LumUtils.InverseLerp(0f, RedirectTime * 0.9f, AITimer);
            if (npc.type == ExoMechNPCIDs.ApolloID)
                hoverOffsetAngle += MathHelper.Pi;

            Vector2 hoverDestination = hades.Center + hoverOffsetAngle.ToRotationVector2() * (ExoTwinSpinRadius + flyAwayInterpolant * 400f);
            if (hoverDestination.Y < 300f)
                hoverDestination.Y = 300f;

            npc.SmoothFlyNear(hoverDestination, hoverFlySpeedInterpolant * 0.21f, 1f - hoverFlySpeedInterpolant * 0.175f);

            npc.rotation = npc.AngleFrom(hades.Center).AngleLerp(hoverOffsetAngle, hoverFlySpeedInterpolant);

            if (AITimer == RedirectTime + 1)
            {
                ScreenShakeSystem.StartShake(9.5f);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    LumUtils.NewProjectileBetter(npc.GetSource_FromAI(), npc.Center, npc.rotation.ToRotationVector2(), ModContent.ProjectileType<BlazingExoLaserbeam>(), BlazingLaserbeamDamage, 0f, -1, npc.whoAmI);
            }

            if (npc.TryGetBehavior(out NPCBehaviorOverride behavior) && behavior is IExoTwin twin)
            {
                twin.Animation = ExoTwinAnimation.ChargingUp;
                twin.Frame = twin.Animation.CalculateFrame(AITimer / 40f % 1f, twin.InPhase2);
                twin.OpticNerveAngleSensitivity = MathHelper.Lerp(-1.6f, -4f, LumUtils.Cos01(MathHelper.TwoPi * AITimer / 54f + npc.whoAmI * 4f));
            }
        }

        /// <summary>
        /// Handles general purpose state variables for the ExothermalLaserDashes attack.
        /// </summary>
        /// <param name="hades">Hades' NPC instance.</param>
        public static void HandleAttackState(NPC hades)
        {
            // Make the spin angle perpendicular to the target, to ensure that they don't get telefragged by the lasers.
            if (AITimer <= RedirectTime)
                ExoTwinSpinAngle = hades.AngleTo(Target.Center) + MathHelper.PiOver2;

            float spinWindUpInterpolant = LumUtils.InverseLerp(0f, ExoTwinSpinWindUpTime, AITimer - RedirectTime);
            ExoTwinSpinAngle += MathHelper.SmoothStep(0f, ExoTwinSpinAngularVelocity, spinWindUpInterpolant);
        }
    }
}
