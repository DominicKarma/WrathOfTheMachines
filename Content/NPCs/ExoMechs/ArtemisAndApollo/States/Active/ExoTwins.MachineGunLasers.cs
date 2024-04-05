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
        public static Vector2[] LaserCannonOffsets => [new(-72f, 38f), new(72f, 38f), new(-92f, 44f), new(92f, 44f), new(0f, 84f)];

        /// <summary>
        /// The rate at which Artemis shoots lasers during the MachineGunLasers attack.
        /// </summary>
        public static int MachineGunLasers_LaserShootRate => Utilities.SecondsToFrames(0.04f);

        /// <summary>
        /// How long the MachineGunLasers attack goes on for.
        /// </summary>
        public static int MachineGunLasers_AttackDuration => Utilities.SecondsToFrames(7f);

        /// <summary>
        /// The speed at which lasers fired by Artemis during the MachineGunLasers attack are shot.
        /// </summary>
        public static float MachineGunLasers_LaserShootSpeed => 18.75f;

        /// <summary>
        /// The maximum random spread of lasers fired by Artemis during the MachineGunLasers attack.
        /// </summary>
        public static float MachineGunLasers_LaserShootSpread => MathHelper.ToRadians(12f);

        /// <summary>
        /// AI update loop method for the MachineGunLasers attack.
        /// </summary>
        /// <param name="npc">Artemis' NPC instance.</param>
        /// <param name="artemisAttributes">Artemis' designated generic attributes.</param>
        /// <param name="localAITimer">Artemis' local AI timer.</param>
        public static void DoBehavior_MachineGunLasers(NPC npc, IExoTwin artemisAttributes, ref int localAITimer)
        {
            // Slowly attempt to fly towards the target.
            npc.SmoothFlyNearWithSlowdownRadius(Target.Center, 0.03f, 0.95f, 300f);

            // Look at the target.
            npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(Target.Center), 0.05f);

            DoBehavior_MachineGunLasers_ManageSounds(npc, ref localAITimer);

            if (AITimer % MachineGunLasers_LaserShootRate == MachineGunLasers_LaserShootRate - 1 && localAITimer < MachineGunLasers_AttackDuration - 45)
            {
                int offsetIndex = Main.rand.Next(LaserCannonOffsets.Length - 1);
                if (Main.rand.NextBool(4))
                    offsetIndex = LaserCannonOffsets.Length - 1;

                Vector2 unrotatedOffset = LaserCannonOffsets[offsetIndex];
                Vector2 laserShootOffset = unrotatedOffset.RotatedBy(npc.rotation - MathHelper.PiOver2) * npc.scale;
                Vector2 laserShootDirection = (npc.rotation + Main.rand.NextFloatDirection() * MachineGunLasers_LaserShootSpread).ToRotationVector2();
                Vector2 laserShootVelocity = laserShootDirection * Utilities.InverseLerp(0f, 60f, localAITimer) * MachineGunLasers_LaserShootSpeed;
                DoBehavior_MachineGunLasers_ShootLaser(npc, npc.Center + laserShootOffset, laserShootVelocity, offsetIndex == LaserCannonOffsets.Length - 1);
            }

            artemisAttributes.Animation = ExoTwinAnimation.Attacking;
            artemisAttributes.Frame = artemisAttributes.Animation.CalculateFrame(AITimer / 30f % 1f, artemisAttributes.InPhase2);

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
            SoundEngine.PlaySound(CommonCalamitySounds.ExoLaserShootSound, laserSpawnPosition);

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
            if (localAITimer == 1)
                SoundEngine.PlaySound(GatlingLaser.FireSound, npc.Center);

            if (localAITimer == 32)
                GatlingLaserSoundLoop = LoopedSoundManager.CreateNew(GatlingLaser.FireLoopSound, () => !npc.active);
            if (localAITimer >= MachineGunLasers_AttackDuration - 45)
                GatlingLaserSoundLoop?.Stop();

            GatlingLaserSoundLoop?.Update(npc.Center);
        }
    }
}
