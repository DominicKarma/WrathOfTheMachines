using CalamityMod.Items.Weapons.DraedonsArsenal;
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
        /// How long the MachineGunLasers attack goes on for.
        /// </summary>
        public static int MachineGunLasers_AttackDuration => Utilities.SecondsToFrames(9f);

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
        /// <param name="npc">Artemis' NPC instance.</param>
        /// <param name="artemisAttributes">Artemis' designated generic attributes.</param>
        /// <param name="localAITimer">Artemis' local AI timer.</param>
        public static void DoBehavior_MachineGunLasers(NPC npc, IExoTwin artemisAttributes, ref int localAITimer)
        {
            if (localAITimer <= 60 && !npc.WithinRange(Target.Center, 150f))
                npc.velocity += npc.SafeDirectionTo(Target.Center) * localAITimer / 24f;

            // Slowly attempt to fly towards the target.
            npc.SmoothFlyNearWithSlowdownRadius(Target.Center, 0.03f, 0.95f, 350f);

            // Look at the target.
            float idealAngle = npc.AngleTo(Target.Center);
            npc.rotation = npc.rotation.AngleTowards(idealAngle, 0.035f).AngleLerp(idealAngle, 0.005f);

            DoBehavior_MachineGunLasers_ManageSounds(npc, ref localAITimer);

            if (localAITimer % MachineGunLasers_LaserShootRate == MachineGunLasers_LaserShootRate - 1 && localAITimer < MachineGunLasers_AttackDuration - 45 && localAITimer >= 60)
            {
                int offsetIndex = Main.rand.Next(LaserCannonOffsets.Length - 1);
                if (Main.rand.NextBool(4))
                    offsetIndex = LaserCannonOffsets.Length - 1;

                Vector2 unrotatedOffset = LaserCannonOffsets[offsetIndex];
                Vector2 laserShootOffset = unrotatedOffset.RotatedBy(npc.rotation - MathHelper.PiOver2) * npc.scale;
                Vector2 laserShootDirection = (npc.rotation + Main.rand.NextFloatDirection() * MachineGunLasers_LaserShootSpread).ToRotationVector2();
                Vector2 laserShootVelocity = laserShootDirection * Utilities.InverseLerp(60f, 120f, localAITimer) * MachineGunLasers_LaserShootSpeed * Main.rand.NextFloat(1f, 1.15f);
                DoBehavior_MachineGunLasers_ShootLaser(npc, npc.Center + laserShootOffset, laserShootVelocity, offsetIndex == LaserCannonOffsets.Length - 1);
            }

            artemisAttributes.Animation = localAITimer >= 60 ? ExoTwinAnimation.Attacking : ExoTwinAnimation.ChargingUp;
            artemisAttributes.Frame = artemisAttributes.Animation.CalculateFrame(localAITimer / 30f % 1f, artemisAttributes.InPhase2);

            if (localAITimer >= MachineGunLasers_AttackDuration)
                ExoTwinsStateManager.TransitionToNextState();
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
            ScreenShakeSystem.StartShakeAtPoint(laserSpawnPosition, 1.67f);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int laserID = big ? ModContent.ProjectileType<ArtemisLaserImproved>() : ModContent.ProjectileType<ArtemisLaserSmall>();
            Utilities.NewProjectileBetter(npc.GetSource_FromAI(), laserSpawnPosition, laserShootVelocity, laserID, BasicShotDamage, 0f);
        }

        /// <summary>
        /// Handles the management of sounds by Artemis during the MachineGunLasers attack.
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="localAITimer"></param>
        public static void DoBehavior_MachineGunLasers_ManageSounds(NPC npc, ref int localAITimer)
        {
            if (localAITimer == 40)
                SoundEngine.PlaySound(GatlingLaser.FireSound with { Volume = 2f });

            if (localAITimer == 93)
                GatlingLaserSoundLoop = LoopedSoundManager.CreateNew(GatlingLaser.FireLoopSound, () => !npc.active);
            if (localAITimer >= MachineGunLasers_AttackDuration - 45 || localAITimer <= 60)
                GatlingLaserSoundLoop?.Stop();

            GatlingLaserSoundLoop?.Update(npc.Center);
        }
    }
}
