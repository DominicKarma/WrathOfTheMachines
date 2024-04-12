using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WoTM.Content.NPCs.ExoMechs.Projectiles;

namespace WoTM.Content.NPCs.ExoMechs
{
    public sealed partial class HadesHeadBehaviorOverride : NPCBehaviorOverride
    {
        /// <summary>
        /// The amount of damage the Exo Energy Blast from Hades does.
        /// </summary>
        public static int ExoEnergyBlastDamage => Main.expertMode ? 700 : 450;

        /// <summary>
        /// AI update loop method for the ExoEnergyBlast attack.
        /// </summary>
        public void DoBehavior_ExoEnergyBlast()
        {
            int initialRedirectTime = 360;
            int blastDelay = 240;
            float pointAtTargetSpeed = 1f;
            bool beamIsOverheating = AITimer >= initialRedirectTime + blastDelay + ExoEnergyBlast.Lifetime - ExoEnergyBlast.OverheatStartingTime;
            Vector2 outerHoverDestination = Target.Center + new Vector2((Target.Center.X - NPC.Center.X).NonZeroSign() * -960f, -300f);

            BodyBehaviorAction = new(AllSegments(), beamIsOverheating ? OpenSegment() : CloseSegment());
            SegmentOpenInterpolant = Utilities.Saturate(SegmentOpenInterpolant + (beamIsOverheating ? 2f : -1f) * StandardSegmentOpenRate);

            // Attempt to get into position for the light attack.
            if (AITimer < initialRedirectTime)
            {
                float idealHoverSpeed = MathHelper.Lerp(43.5f, 72.5f, AITimer / (float)initialRedirectTime);
                idealHoverSpeed *= Utils.GetLerpValue(35f, 300f, NPC.Distance(outerHoverDestination), true);

                Vector2 idealVelocity = NPC.SafeDirectionTo(outerHoverDestination) * MathHelper.Lerp(NPC.velocity.Length(), idealHoverSpeed, 0.135f);
                NPC.velocity = NPC.velocity.RotateTowards(idealVelocity.ToRotation(), 0.045f);
                NPC.velocity = NPC.velocity.MoveTowards(idealVelocity, 3f);

                // Stop hovering if close to the hover destination and prepare to move towards the target.
                if (NPC.WithinRange(outerHoverDestination, 105f) && AITimer >= 30)
                {
                    AITimer = initialRedirectTime;
                    NPC.velocity = Vector2.Lerp(NPC.velocity, NPC.SafeDirectionTo(Target.Center) * NPC.velocity.Length(), 0.92f);
                    NPC.velocity = NPC.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(NPC.velocity.Length(), pointAtTargetSpeed, 0.4f);
                    NPC.netUpdate = true;
                }
            }

            // Slow down, move towards the target (while maintaining the current direction) and create the telegraph.
            if (AITimer >= initialRedirectTime && AITimer < initialRedirectTime + blastDelay)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient && AITimer == initialRedirectTime + 1)
                    Utilities.NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<HadesExoEnergyOrb>(), 0, 0f, -1, blastDelay);

                // Approach the ideal position.
                NPC.velocity = NPC.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(NPC.velocity.Length(), pointAtTargetSpeed, 0.061f);
            }

            // Fire the Biden Blast.
            if (AITimer == initialRedirectTime + blastDelay)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<ExoEnergyBlast>(), ExoEnergyBlastDamage, 0f);
            }
            if (AITimer >= initialRedirectTime + blastDelay)
                NPC.velocity = NPC.velocity.RotateTowards(NPC.AngleTo(Target.Center), 0.0175f);

            if (AITimer >= initialRedirectTime + blastDelay + ExoEnergyBlast.Lifetime)
                AITimer = initialRedirectTime - 120;

            NPC.rotation = NPC.velocity.ToRotation() + MathHelper.PiOver2;
        }
    }
}
