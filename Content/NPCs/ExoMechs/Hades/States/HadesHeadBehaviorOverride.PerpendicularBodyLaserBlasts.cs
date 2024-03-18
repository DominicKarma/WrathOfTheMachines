using System;
using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Sounds;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace DifferentExoMechs.Content.NPCs.Bosses
{
    public sealed partial class HadesHeadBehaviorOverride : NPCBehaviorOverride
    {
        /// <summary>
        /// Whether Hades has successfully reached his destination or not during his PerpendicularBodyLaserBlasts attack.
        /// </summary>
        public bool PerpendicularBodyLaserBlasts_HasReachedDestination
        {
            get => NPC.ai[0] == 1f;
            set => NPC.ai[0] = value.ToInt();
        }

        /// <summary>
        /// The fly direction Hades attempts to move in during his PerpendicularBodyLaserBlasts attack.
        /// </summary>
        public Vector2 PerpendicularBodyLaserBlasts_StartingDirection
        {
            get => NPC.ai[1].ToRotationVector2();
            set => NPC.ai[1] = value.ToRotation();
        }

        /// <summary>
        /// How long Hades spends snaking around into position in anticipation of attacking during his PerpendicularBodyLaserBlasts attack.
        /// </summary>
        public static int PerpendicularBodyLaserBlasts_RedirectTime => Utilities.SecondsToFrames(3f);

        /// <summary>
        /// How long Hades spends telegraphing prior to firing lasers during his PerpendicularBodyLaserBlasts attack.
        /// </summary>
        public static int PerpendicularBodyLaserBlasts_BlastTelegraphTime => Utilities.SecondsToFrames(1.9f);

        /// <summary>
        /// The 'n' in 'every Nth segment should fire' for Hades' PerpendicularBodyLaserBlasts attack.
        /// </summary>
        public static int PerpendicularBodyLaserBlasts_SegmentUsageCycle => Main.expertMode ? 3 : 2;

        /// <summary>
        /// How far Hades tries to go ahead of the player before firing during his PerpendicularBodyLaserBlasts attack.
        /// </summary>
        public static float PerpendicularBodyLaserBlasts_ForwardDestinationOffset => 1600f;

        /// <summary>
        /// How far Hades tries to go to the side of the player before firing during his PerpendicularBodyLaserBlasts attack.
        /// </summary>
        public static float PerpendicularBodyLaserBlasts_SideDestinationOffset => 450f;

        /// <summary>
        /// How far along, as a 0-1 completion ratio, Hades needs to be during his PerpendicularBodyLaserBlasts slow down animation before lasers are fired all at once.
        /// </summary>
        public static float PerpendicularBodyLaserBlasts_BurstShootCompletionRatio => 0.79f;

        /// <summary>
        /// How fast lasers shot by Hades should be during his PerpendicularBodyLaserBlasts attack.
        /// </summary>
        public static float PerpendicularBodyLaserBlasts_LaserShootSpeed => 27f;

        /// <summary>
        /// The amount of damage basic lasers from Hades do.
        /// </summary>
        public static int BasicLaserDamage => Main.expertMode ? 400 : 250;

        public static readonly SoundStyle LaserChargeUpSound = new("DifferentExoMechs/Assets/Sounds/Custom/Hades/LaserChargeUp");

        /// <summary>
        /// AI update loop method for the PerpendicularBodyLaserBlasts attack.
        /// </summary>
        public void DoBehavior_PerpendicularBodyLaserBlasts()
        {
            BodyBehaviorAction = new(AllSegments(), CloseSegment());

            if (AITimer < PerpendicularBodyLaserBlasts_RedirectTime)
                DoBehavior_PerpendicularBodyLaserBlasts_MoveNearPlayer();
            else if (AITimer < PerpendicularBodyLaserBlasts_RedirectTime + PerpendicularBodyLaserBlasts_BlastTelegraphTime)
                DoBehavior_PerpendicularBodyLaserBlasts_CreateBlastTelegraphs();
            else
            {
                PerpendicularBodyLaserBlasts_HasReachedDestination = false;
                AITimer = 0;
            }

            NPC.rotation = NPC.velocity.ToRotation() + MathHelper.PiOver2;
        }

        /// <summary>
        /// Makes Hades move around during his PerpendicularBodyLaserBlasts attack.
        /// </summary>
        public void DoBehavior_PerpendicularBodyLaserBlasts_MoveNearPlayer()
        {
            if (AITimer <= 10)
            {
                PerpendicularBodyLaserBlasts_StartingDirection = NPC.SafeDirectionTo(Target.Center);
                if (AITimer == 10)
                    NPC.netUpdate = true;
            }

            float forwardOffset = PerpendicularBodyLaserBlasts_ForwardDestinationOffset;
            float sideOffset = PerpendicularBodyLaserBlasts_SideDestinationOffset;
            Vector2 idealOffsetDirection = PerpendicularBodyLaserBlasts_StartingDirection;
            Vector2 flyDestination = Target.Center + idealOffsetDirection * forwardOffset + idealOffsetDirection.RotatedBy(MathHelper.PiOver2) * sideOffset;

            float moveCompletion = AITimer / (float)PerpendicularBodyLaserBlasts_RedirectTime;
            float flySpeedInterpolant = Utilities.InverseLerp(0f, 0.35f, moveCompletion);
            float idealFlySpeed = MathHelper.SmoothStep(4f, 42f, flySpeedInterpolant);
            if (PerpendicularBodyLaserBlasts_HasReachedDestination)
                idealFlySpeed *= 0.1f;

            Vector2 idealFlyDirection = PerpendicularBodyLaserBlasts_HasReachedDestination ? NPC.velocity.SafeNormalize(Vector2.UnitY) : NPC.SafeDirectionTo(flyDestination);
            Vector2 idealVelocity = idealFlyDirection * idealFlySpeed;
            NPC.velocity = Vector2.Lerp(NPC.velocity, idealVelocity, flySpeedInterpolant * 0.195f);

            if (!PerpendicularBodyLaserBlasts_HasReachedDestination && NPC.WithinRange(flyDestination, 250f))
            {
                PerpendicularBodyLaserBlasts_HasReachedDestination = true;
                if (AITimer < PerpendicularBodyLaserBlasts_RedirectTime - 10)
                    AITimer = PerpendicularBodyLaserBlasts_RedirectTime - 10;

                NPC.netUpdate = true;
            }
        }

        /// <summary>
        /// Makes Hades slow down and cast telegraphs during his PerpendicularBodyLaserBlasts attack.
        /// </summary>
        public void DoBehavior_PerpendicularBodyLaserBlasts_CreateBlastTelegraphs()
        {
            int localAITimer = AITimer - PerpendicularBodyLaserBlasts_RedirectTime;
            if (localAITimer == 1)
                SoundEngine.PlaySound(LaserChargeUpSound);

            NPC.velocity *= 0.97f;

            var bodySegmentCondition = EveryNthSegment(PerpendicularBodyLaserBlasts_SegmentUsageCycle, PerpendicularBodyLaserBlasts_SegmentUsageCycle - 1);
            float telegraphCompletion = localAITimer / (float)PerpendicularBodyLaserBlasts_BlastTelegraphTime;
            BodyBehaviorAction = new(bodySegmentCondition, new(behaviorOverride =>
            {
                if (localAITimer == (int)(PerpendicularBodyLaserBlasts_BlastTelegraphTime * PerpendicularBodyLaserBlasts_BurstShootCompletionRatio) && PerpendicularBodyLaserBlasts_SegmentCanFire(behaviorOverride.NPC, NPC))
                {
                    ScreenShakeSystem.StartShake(3.2f);

                    NPC segment = behaviorOverride.NPC;
                    SoundEngine.PlaySound(CommonCalamitySounds.ExoLaserShootSound with { MaxInstances = 0, Volume = 0.15f }, segment.Center);

                    Vector2 perpendicular = segment.rotation.ToRotationVector2();
                    Vector2 laserSpawnPosition = behaviorOverride.TurretPosition;
                    for (int i = 0; i < 10; i++)
                    {
                        int laserLineLifetime = Main.rand.Next(10, 30);
                        float laserLineSpeed = Main.rand.NextFloat(9f, 20f);
                        LineParticle line = new(laserSpawnPosition, perpendicular.RotatedByRandom(0.5f) * laserLineSpeed, false, laserLineLifetime, 1f, Color.Red);
                        GeneralParticleHandler.SpawnParticle(line);

                        line = new(laserSpawnPosition, perpendicular.RotatedByRandom(0.5f) * -laserLineSpeed, false, laserLineLifetime, 1f, Color.Red);
                        GeneralParticleHandler.SpawnParticle(line);
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Utilities.NewProjectileBetter(segment.GetSource_FromAI(), laserSpawnPosition, perpendicular * PerpendicularBodyLaserBlasts_LaserShootSpeed, ModContent.ProjectileType<HadesLaserBurst>(), BasicLaserDamage, 0f, -1, 60f, -1f);
                        Utilities.NewProjectileBetter(segment.GetSource_FromAI(), laserSpawnPosition, perpendicular * -PerpendicularBodyLaserBlasts_LaserShootSpeed, ModContent.ProjectileType<HadesLaserBurst>(), BasicLaserDamage, 0f, -1, 60f, -1f);
                    }
                }

                behaviorOverride.ShouldReorientDirection = false;

                OpenSegment().Invoke(behaviorOverride);
            }));
            BodyRenderAction = new(bodySegmentCondition, new(behaviorOverride =>
            {
                if (!PerpendicularBodyLaserBlasts_SegmentCanFire(behaviorOverride.NPC, NPC))
                    return;

                PrimitivePixelationSystem.RenderToPrimsNextFrame(() =>
                {
                    RenderLaserTelegraph(behaviorOverride, telegraphCompletion, 1000f, -behaviorOverride.NPC.rotation.ToRotationVector2());
                    RenderLaserTelegraph(behaviorOverride, telegraphCompletion, 1000f, behaviorOverride.NPC.rotation.ToRotationVector2());
                }, PixelationPrimitiveLayer.AfterNPCs);
            }));

            float rumblePower = Utilities.InverseLerpBump(0f, PerpendicularBodyLaserBlasts_BurstShootCompletionRatio, PerpendicularBodyLaserBlasts_BurstShootCompletionRatio, PerpendicularBodyLaserBlasts_BurstShootCompletionRatio + 0.04f, telegraphCompletion);
            ScreenShakeSystem.SetUniversalRumble(rumblePower * 1.3f);
        }

        /// <summary>
        /// Determines whether a body segment on Hades can fire during the PerpendicularBodyLaserBlasts attack.
        /// </summary>
        /// <param name="segment">The segment NPC instance.</param>
        /// <param name="head">The head NPC instance.</param>
        public static bool PerpendicularBodyLaserBlasts_SegmentCanFire(NPC segment, NPC head)
        {
            bool roughlySameDirectionAsHead = Vector2.Dot(segment.rotation.ToRotationVector2(), head.rotation.ToRotationVector2()) >= 0.2f;
            bool closeEnoughToTarget = segment.WithinRange(Target.Center, 1120f);
            return roughlySameDirectionAsHead && closeEnoughToTarget;
        }

        /// <summary>
        /// Renders a laser telegraph for a given <see cref="HadesBodyBehaviorOverride"/> in a given direction.
        /// </summary>
        /// <param name="behaviorOverride">The behavior override responsible for the segment.</param>
        /// <param name="telegraphIntensityFactor">The intensity factor of the telegraph.</param>
        /// <param name="telegraphSize">How big the telegraph should be by default.</param>
        /// <param name="telegraphDirection">The direction of the telegraph</param>
        public static void RenderLaserTelegraph(HadesBodyBehaviorOverride behaviorOverride, float telegraphIntensityFactor, float telegraphSize, Vector2 telegraphDirection)
        {
            float opacity = behaviorOverride.SegmentOpenInterpolant.Cubed();
            Vector2 start = behaviorOverride.TurretPosition;
            Texture2D invisible = ModContent.Request<Texture2D>("CalamityMod/Projectiles/InvisibleProj").Value;

            // The multiplication by 0.5 is because this is being rendered to the pixelation target, wherein everything is downscaled by a factor of two, so that it can be upscaled later.
            Vector2 drawPosition = (start - Main.screenPosition) * 0.5f;

            float fadeOut = Utilities.InverseLerp(1f, PerpendicularBodyLaserBlasts_BurstShootCompletionRatio, telegraphIntensityFactor).Squared();
            Effect effect = Filters.Scene["CalamityMod:SpreadTelegraph"].GetShader().Shader;
            effect.Parameters["centerOpacity"].SetValue(0.4f);
            effect.Parameters["mainOpacity"].SetValue(opacity * 0.4f);
            effect.Parameters["halfSpreadAngle"].SetValue((1.1f - opacity) * fadeOut * 0.89f);
            effect.Parameters["edgeColor"].SetValue(Vector3.Lerp(new(1.3f, 0.1f, 0.67f), new(4f, 0f, 0f), telegraphIntensityFactor));
            effect.Parameters["centerColor"].SetValue(new Vector3(1f, 0.1f, 0.1f));
            effect.Parameters["edgeBlendLength"].SetValue(0.07f);
            effect.Parameters["edgeBlendStrength"].SetValue(32f);
            effect.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Draw(invisible, drawPosition, null, Color.White, telegraphDirection.ToRotation(), invisible.Size() * 0.5f, Vector2.One * fadeOut * telegraphSize, SpriteEffects.None, 0f);
        }

        public static float LaserTelegraphWidthFunction(float completionRatio, float telegraphIntensity) => MathF.Pow(telegraphIntensity, 2.5f) * 10f;

        public static Color LaserTelegraphColorFunction(float completionRatio, float telegraphIntensity) => Color.Lerp(Color.SkyBlue, Color.Green, completionRatio) * MathF.Pow(telegraphIntensity, 5f) * 0.5f;
    }
}
