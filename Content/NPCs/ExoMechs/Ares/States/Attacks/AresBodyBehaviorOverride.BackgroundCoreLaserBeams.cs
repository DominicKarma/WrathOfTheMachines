﻿using System;
using System.Linq;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.Particles;
using Luminance.Common.Utilities;
using Luminance.Core.Sounds;
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
        /// The sound responsible for the laser sound loop.
        /// </summary>
        public LoopedSoundInstance ExoOverloadLoopedSound;

        /// <summary>
        /// The center position of Ares' core.
        /// </summary>
        public Vector2 CorePosition => NPC.Center + Vector2.UnitY.RotatedBy(NPC.rotation) * NPC.scale * 22f;

        /// <summary>
        /// How much damage missiles from Ares' core do.
        /// </summary>
        public static int MissileDamage => Main.expertMode ? 350 : 250;

        /// <summary>
        /// How much damage laserbeams from Ares' core do.
        /// </summary>
        public static int CoreLaserbeamDamage => Main.expertMode ? 560 : 400;

        /// <summary>
        /// AI update loop method for the BackgroundCoreLaserBeams attack.
        /// </summary>
        public void DoBehavior_BackgroundCoreLaserBeams()
        {
            float enterBackgroundInterpolant = Utilities.InverseLerp(0f, 30f, AITimer);
            float slowDownInterpolant = Utilities.InverseLerp(54f, 60f, AITimer);
            ZPosition = enterBackgroundInterpolant * 3.7f;
            NPC.Center = Vector2.Lerp(NPC.Center, Target.Center - Vector2.UnitY * enterBackgroundInterpolant * 360f, enterBackgroundInterpolant * (1f - slowDownInterpolant) * 0.08f);
            NPC.Center = Vector2.Lerp(NPC.Center, new Vector2(Target.Center.X, NPC.Center.Y), slowDownInterpolant * 0.111f);
            NPC.Center = Vector2.Lerp(NPC.Center, new Vector2(NPC.Center.X, Target.Center.Y - 70f), slowDownInterpolant * 0.019f);
            NPC.Center = NPC.Center.MoveTowards(new Vector2(NPC.Center.X, Target.Center.Y - 70f), slowDownInterpolant * 1.7f);
            NPC.velocity *= 0.93f;

            NPC.dontTakeDamage = true;

            if (AITimer == 1 || !Utilities.AnyProjectiles(ModContent.ProjectileType<ExoOverloadDeathray>()))
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(NPC.GetSource_FromAI(), CorePosition, Vector2.Zero, ModContent.ProjectileType<ExoOverloadDeathray>(), CoreLaserbeamDamage, 0f, -1);
                AITimer = 2;
            }

            if (AITimer == 2)
                SoundEngine.PlaySound(AresBody.LaserStartSound);

            if (AITimer == 180)
                ExoOverloadLoopedSound = LoopedSoundManager.CreateNew(AresBody.LaserLoopSound, () => CurrentState != AresAIState.BackgroundCoreLaserBeams || !NPC.active);

            var deathrays = Utilities.AllProjectilesByID(ModContent.ProjectileType<ExoOverloadDeathray>());
            if (deathrays.Any())
            {
                Vector3 direction = Vector3.Transform(Vector3.UnitX, deathrays.First().As<ExoOverloadDeathray>().Rotation);
                ExoOverloadLoopedSound?.Update(Target.Center, sound =>
                {
                    sound.Volume = Utilities.InverseLerp(0.8f, 0.42f, MathF.Abs(direction.X)) * 1.1f + 0.56f;
                });
            }

            if (AITimer >= 90)
            {
                if (AITimer % 8 == 7)
                {
                    Vector2 sparkSpawnPosition = NPC.Center - Vector2.UnitY.RotatedByRandom(1.1f) * NPC.scale * 90f;
                    SparkleParticle sparkle = new(sparkSpawnPosition, Vector2.Zero, Color.White, Color.Yellow, 0.23f, 10);
                    GeneralParticleHandler.SpawnParticle(sparkle);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 missileSpawnPosition = Target.Center + new Vector2(Main.rand.NextFloatDirection() * 1100f, -740f);
                        if (Main.rand.NextBool(3))
                            missileSpawnPosition.X = Target.Center.X + Main.rand.NextFloatDirection() * 50f + Target.velocity.X * Main.rand.NextFloat(8f, 32f);

                        Utilities.NewProjectileBetter(NPC.GetSource_FromAI(), missileSpawnPosition, Vector2.UnitY * 3f, ModContent.ProjectileType<AresMissile>(), MissileDamage, 0f, -1, Target.Bottom.Y);

                        Vector2 backgroundMissileVelocity = NPC.SafeDirectionTo(sparkSpawnPosition).RotatedByRandom(0.2f) * Main.rand.NextFloat(16f, 27f);
                        backgroundMissileVelocity.X *= 0.25f;
                        Utilities.NewProjectileBetter(NPC.GetSource_FromAI(), sparkSpawnPosition, backgroundMissileVelocity, ModContent.ProjectileType<AresMissileBackground>(), 0, 0f);
                    }
                }
            }

            BasicHandUpdateWrapper();
        }
    }
}
