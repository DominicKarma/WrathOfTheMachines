using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.Particles;
using CalamityMod.Sounds;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Luminance.Core.Sounds;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WoTM.Content.NPCs.ExoMechs.Projectiles;

namespace WoTM.Content.NPCs.ExoMechs
{
    public static partial class ExoTwinsStates
    {
        internal static LoopedSoundInstance GatlingLaserSoundLoop;

        /// <summary>
        /// The set of Artemis' laser cannon offsets, for usage during the MachineGunLasers attack.
        /// </summary>
        public static Vector2[] LaserCannonOffsets => [new(-72f, 34f), new(72f, 34f), new(-88f, 44f), new(88f, 44f), new(0f, 84f)];

        /// <summary>
        /// The rate at which Artemis shoots lasers during the MachineGunLasers attack.
        /// </summary>
        public static int MachineGunLasers_LaserShootRate => Utilities.SecondsToFrames(0.04f);

        /// <summary>
        /// How long Artemis waits before firing during the MachineGunLasers attack.
        /// </summary>
        public static int MachineGunLasers_AttackDelay => Utilities.SecondsToFrames(1f);

        /// <summary>
        /// How long the MachineGunLasers attack goes on for.
        /// </summary>
        public static int MachineGunLasers_AttackDuration => Utilities.SecondsToFrames(10f);

        /// <summary>
        /// The speed at which lasers fired by Artemis during the MachineGunLasers attack are shot.
        /// </summary>
        public static float MachineGunLasers_LaserShootSpeed => 18.75f;

        /// <summary>
        /// The maximum random spread of lasers fired by Artemis during the MachineGunLasers attack.
        /// </summary>
        public static float MachineGunLasers_LaserShootSpread => MathHelper.ToRadians(7f);

        /// <summary>
        /// AI update loop method for the MachineGunLasers attack.
        /// </summary>
        /// <param name="npc">The Twins' NPC instance.</param>
        /// <param name="twinAttributes">The Twins' designated generic attributes.</param>
        public static void DoBehavior_MachineGunLasers(NPC npc, IExoTwin twinAttributes)
        {
            if (npc.type == ExoMechNPCIDs.ArtemisID)
                DoBehavior_MachineGunLasers_ArtemisLasers(npc, twinAttributes);
            else
                DoBehavior_MachineGunLasers_ApolloPlasmaDashes(npc, twinAttributes);
        }

        /// <summary>
        /// AI update loop method for the MachineGunLasers attack for Artemis specifically.
        /// </summary>
        /// <param name="npc">Artemis' NPC instance.</param>
        /// <param name="artemisAttributes">Artemis' designated generic attributes.</param>
        public static void DoBehavior_MachineGunLasers_ArtemisLasers(NPC npc, IExoTwin artemisAttributes)
        {
            if (AITimer <= MachineGunLasers_AttackDelay && !npc.WithinRange(Target.Center, 150f))
                npc.velocity += npc.SafeDirectionTo(Target.Center) * AITimer / MachineGunLasers_AttackDelay * 2.5f;

            // Slowly attempt to fly towards the target.
            npc.SmoothFlyNearWithSlowdownRadius(Target.Center, 0.03f, 0.95f, 350f);

            // Look at the target.
            float idealAngle = npc.AngleTo(Target.Center);
            npc.rotation = npc.rotation.AngleTowards(idealAngle, 0.023f).AngleLerp(idealAngle, 0.001f);

            DoBehavior_MachineGunLasers_ManageSounds(npc);

            if (AITimer % MachineGunLasers_LaserShootRate == MachineGunLasers_LaserShootRate - 1 && AITimer < MachineGunLasers_AttackDuration - 45 && AITimer >= MachineGunLasers_AttackDelay)
            {
                int offsetIndex = Main.rand.Next(LaserCannonOffsets.Length - 1);
                if (Main.rand.NextBool(4))
                    offsetIndex = LaserCannonOffsets.Length - 1;

                Vector2 unrotatedOffset = LaserCannonOffsets[offsetIndex];
                Vector2 laserShootOffset = unrotatedOffset.RotatedBy(npc.rotation - MathHelper.PiOver2) * npc.scale;
                Vector2 laserShootDirection = (npc.rotation + Main.rand.NextFloatDirection() * MachineGunLasers_LaserShootSpread).ToRotationVector2();
                Vector2 laserShootVelocity = laserShootDirection * Utilities.InverseLerp(60f, 120f, AITimer) * MachineGunLasers_LaserShootSpeed * Main.rand.NextFloat(1f, 1.15f);
                DoBehavior_MachineGunLasers_ShootLaser(npc, npc.Center + laserShootOffset, laserShootVelocity, offsetIndex == LaserCannonOffsets.Length - 1);
            }

            artemisAttributes.Animation = AITimer >= MachineGunLasers_AttackDelay ? ExoTwinAnimation.Attacking : ExoTwinAnimation.ChargingUp;
            artemisAttributes.Frame = artemisAttributes.Animation.CalculateFrame(AITimer / 30f % 1f, artemisAttributes.InPhase2);

            if (AITimer >= MachineGunLasers_AttackDelay + MachineGunLasers_AttackDuration)
                ExoTwinsStateManager.TransitionToNextState();
        }

        /// <summary>
        /// AI update loop method for the MachineGunLasers attack for Apollo specifically.
        /// </summary>
        /// <param name="npc">Apollo's NPC instance.</param>
        /// <param name="apolloAttributes">Apollo's designated generic attributes.</param>
        public static void DoBehavior_MachineGunLasers_ApolloPlasmaDashes(NPC npc, IExoTwin apolloAttributes)
        {
            int hoverRedirectTime = 25;
            int telegraphTime = 45;
            int dashTime = 13;
            int dashSlowdownTime = 16;
            int wrappedTimer = AITimer % (hoverRedirectTime + telegraphTime + dashTime + dashSlowdownTime);
            float dashSpeed = 124f;

            if (wrappedTimer <= hoverRedirectTime)
            {
                float hoverFlySpeedInterpolant = Utilities.InverseLerpBump(0f, 0.8f, 0.9f, 1f, wrappedTimer / (float)hoverRedirectTime) * 0.11f;
                Vector2 hoverDestination = Target.Center + Target.SafeDirectionTo(Main.npc[CalamityGlobalNPC.draedonExoMechTwinRed].Center).RotatedBy(MathHelper.PiOver2) * 950f;
                npc.SmoothFlyNear(hoverDestination, hoverFlySpeedInterpolant, 0.81f);
                npc.rotation = npc.AngleTo(Target.Center + Target.velocity * 25f);

                apolloAttributes.Animation = ExoTwinAnimation.ChargingUp;
            }

            else if (wrappedTimer <= hoverRedirectTime + telegraphTime)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient && wrappedTimer == hoverRedirectTime + 1)
                {
                    npc.velocity = npc.rotation.ToRotationVector2() * -0.5f;
                    Utilities.NewProjectileBetter(npc.GetSource_FromAI(), npc.Center + npc.velocity * 30f, npc.rotation.ToRotationVector2() * 0.01f, ModContent.ProjectileType<ApolloLineTelegraph>(), 0, 0f, -1, telegraphTime);
                    npc.netUpdate = true;
                }

                npc.velocity *= 1.031f;
                apolloAttributes.Animation = ExoTwinAnimation.ChargingUp;
            }
            else
            {
                if (wrappedTimer == hoverRedirectTime + telegraphTime + 1)
                {
                    ScreenShakeSystem.StartShakeAtPoint(npc.Center, 6.7f);

                    SoundEngine.PlaySound(Artemis.ChargeSound);
                    npc.velocity = npc.rotation.ToRotationVector2() * dashSpeed;
                    npc.netUpdate = true;
                }

                if (wrappedTimer >= hoverRedirectTime + telegraphTime + dashTime)
                {
                    npc.velocity *= 0.7f;
                    npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(Target.Center), 0.16f);
                }
                else if (wrappedTimer % 4 == 3)
                {
                    SoundEngine.PlaySound(CommonCalamitySounds.ExoPlasmaShootSound, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 plasmaFireballVelocity = npc.SafeDirectionTo(Target.Center).RotatedBy(MathHelper.Pi / 3.5f) * 35f;
                        Utilities.NewProjectileBetter(npc.GetSource_FromAI(), npc.Center, plasmaFireballVelocity, ModContent.ProjectileType<LingeringPlasmaFireball>(), BasicShotDamage, 0f);
                    }
                }

                npc.damage = npc.defDamage;

                apolloAttributes.Animation = ExoTwinAnimation.Attacking;
            }

            apolloAttributes.Frame = apolloAttributes.Animation.CalculateFrame(AITimer / 30f % 1f, apolloAttributes.InPhase2);
        }

        /// <summary>
        /// Instructs Artemis to shoot a single laser.
        /// </summary>
        /// <param name="npc">Artemis' NPC instance.</param>
        /// <param name="laserSpawnPosition">The spawn position of the laser.</param>
        /// <param name="laserShootVelocity">The shoot velocity of the laser.</param>
        /// <param name="big">Whether the shot laser should be big.</param>
        public static void DoBehavior_MachineGunLasers_ShootLaser(NPC npc, Vector2 laserSpawnPosition, Vector2 laserShootVelocity, bool big = false)
        {
            SoundEngine.PlaySound(CommonCalamitySounds.ExoLaserShootSound with { Volume = 0.4f, MaxInstances = 0 }, laserSpawnPosition);

            Color lightBloomColor = Color.Lerp(Color.Orange, Color.Wheat, Main.rand.NextFloat(0.75f));
            StrongBloom lightBloom = new(laserSpawnPosition, npc.velocity, lightBloomColor, 0.25f, 8);
            GeneralParticleHandler.SpawnParticle(lightBloom);

            LineParticle energy = new(laserSpawnPosition, laserShootVelocity.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.9f, 1.6f) + npc.velocity, false, 10, 0.6f, Color.Yellow);
            GeneralParticleHandler.SpawnParticle(energy);

            // Shake the screen just a tiny bit.
            ScreenShakeSystem.StartShakeAtPoint(laserSpawnPosition, 1.2f);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int laserID = big ? ModContent.ProjectileType<ArtemisLaserImproved>() : ModContent.ProjectileType<ArtemisLaserSmall>();
            Utilities.NewProjectileBetter(npc.GetSource_FromAI(), laserSpawnPosition, laserShootVelocity, laserID, BasicShotDamage, 0f);
        }

        /// <summary>
        /// Handles the management of sounds by Artemis during the MachineGunLasers attack.
        /// </summary>
        /// <param name="npc"></param>
        public static void DoBehavior_MachineGunLasers_ManageSounds(NPC npc)
        {
            if (AITimer == MachineGunLasers_AttackDelay - 20)
                SoundEngine.PlaySound(GatlingLaser.FireSound with { Volume = 2f });

            if (AITimer == MachineGunLasers_AttackDelay + 33)
                GatlingLaserSoundLoop = LoopedSoundManager.CreateNew(GatlingLaser.FireLoopSound, () => !npc.active);
            if (AITimer >= MachineGunLasers_AttackDuration - 45 || AITimer <= MachineGunLasers_AttackDelay)
                GatlingLaserSoundLoop?.Stop();

            GatlingLaserSoundLoop?.Update(npc.Center);
        }
    }
}
