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
        /// The maximum time Hades spends redirecting during the ExoEnergyBlast attack.
        /// </summary>
        public static int ExoEnergyBlast_InitialRedirectTime => Utilities.SecondsToFrames(6f);

        /// <summary>
        /// The delay of the blast during the ExoEnergyBlast attack.
        /// </summary>
        public static int ExoEnergyBlast_BlastDelay => Utilities.SecondsToFrames(3.5f);

        /// <summary>
        /// AI update loop method for the ExoEnergyBlast attack.
        /// </summary>
        public void DoBehavior_ExoEnergyBlast()
        {
            bool beamIsOverheating = AITimer >= ExoEnergyBlast_InitialRedirectTime + ExoEnergyBlast_BlastDelay + ExoEnergyBlast.Lifetime - ExoEnergyBlast.OverheatStartingTime;
            float pointAtTargetSpeed = 4.4f;
            Vector2 outerHoverDestination = Target.Center + new Vector2((Target.Center.X - NPC.Center.X).NonZeroSign() * -1050f, -400f);

            BodyBehaviorAction = new(AllSegments(), beamIsOverheating ? OpenSegment() : CloseSegment());
            SegmentOpenInterpolant = Utilities.Saturate(SegmentOpenInterpolant + (beamIsOverheating ? 2f : -1f) * StandardSegmentOpenRate);

            // Attempt to get into position for the light attack.
            if (AITimer < ExoEnergyBlast_InitialRedirectTime)
            {
                float idealHoverSpeed = MathHelper.Lerp(43.5f, 72.5f, AITimer / (float)ExoEnergyBlast_InitialRedirectTime);
                idealHoverSpeed *= Utils.GetLerpValue(35f, 300f, NPC.Distance(outerHoverDestination), true);

                Vector2 idealVelocity = NPC.SafeDirectionTo(outerHoverDestination) * MathHelper.Lerp(NPC.velocity.Length(), idealHoverSpeed, 0.135f);
                NPC.velocity = NPC.velocity.RotateTowards(idealVelocity.ToRotation(), 0.045f);
                NPC.velocity = NPC.velocity.MoveTowards(idealVelocity, AITimer / (float)ExoEnergyBlast_InitialRedirectTime * 8f);

                // Stop hovering if close to the hover destination and prepare to move towards the target.
                if (NPC.WithinRange(outerHoverDestination, 105f) && AITimer >= 30)
                {
                    AITimer = ExoEnergyBlast_InitialRedirectTime;
                    NPC.velocity = Vector2.Lerp(NPC.velocity, NPC.SafeDirectionTo(Target.Center) * NPC.velocity.Length(), 0.92f);
                    NPC.velocity = NPC.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(NPC.velocity.Length(), pointAtTargetSpeed, 0.4f);
                    NPC.netUpdate = true;
                }
            }

            // Slow down, move towards the target (while maintaining the current direction) and create the telegraph.
            if (AITimer >= ExoEnergyBlast_InitialRedirectTime && AITimer < ExoEnergyBlast_InitialRedirectTime + ExoEnergyBlast_BlastDelay)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient && AITimer == ExoEnergyBlast_InitialRedirectTime + 1)
                    Utilities.NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<HadesExoEnergyOrb>(), 0, 0f, -1, ExoEnergyBlast_BlastDelay);

                if (AITimer % 45 == 44 && AITimer < ExoEnergyBlast_InitialRedirectTime + ExoEnergyBlast_BlastDelay - 40)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 7; i++)
                        {
                            Vector2 burstShootDirection = NPC.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.Lerp(-1.4f, 1.4f, i / 6f));
                            Vector2 burstSpawnPosition = NPC.Center + NPC.velocity.SafeNormalize(Vector2.UnitY) * 250f;
                            Utilities.NewProjectileBetter(NPC.GetSource_FromAI(), burstSpawnPosition, burstShootDirection * 0.4f, ModContent.ProjectileType<HomingTeslaBurst>(), BasicLaserDamage, 0f, -1, HomingTeslaBurst.HomeInTime);
                        }
                    }
                }

                // Approach the ideal position.
                NPC.velocity = NPC.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(NPC.velocity.Length(), pointAtTargetSpeed, 0.061f);
            }

            // Fire the Biden Blast.
            if (AITimer == ExoEnergyBlast_InitialRedirectTime + ExoEnergyBlast_BlastDelay)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<ExoEnergyBlast>(), ExoEnergyBlastDamage, 0f);
            }
            if (AITimer >= ExoEnergyBlast_InitialRedirectTime + ExoEnergyBlast_BlastDelay)
                NPC.velocity = NPC.velocity.RotateTowards(NPC.AngleTo(Target.Center), 0.0135f) * 0.986f;

            if (AITimer >= ExoEnergyBlast_InitialRedirectTime + ExoEnergyBlast_BlastDelay + ExoEnergyBlast.Lifetime)
                SelectNextAttack();

            NPC.rotation = NPC.velocity.ToRotation() + MathHelper.PiOver2;
        }
    }
}
