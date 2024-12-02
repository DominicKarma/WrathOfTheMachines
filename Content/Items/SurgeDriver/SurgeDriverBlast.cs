using System.Collections.Generic;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Ranged;
using Luminance.Assets;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using WoTM.Content.Particles;

namespace WoTM.Content.Items.SurgeDriver
{
    public class SurgeDriverBlast : ModProjectile, IPixelatedPrimitiveRenderer
    {
        /// <summary>
        /// The owner of this blast.
        /// </summary>
        public Player Owner => Main.player[Projectile.owner];

        /// <summary>
        /// How long this blast should exist for, in frames.
        /// </summary>
        public ref float Lifetime => ref Projectile.ai[0];

        /// <summary>
        /// The hue of this laser blast.
        /// </summary>
        public ref float LaserHue => ref Projectile.ai[1];

        /// <summary>
        /// How long this laser blast should be.
        /// </summary>
        public ref float LaserLength => ref Projectile.ai[2];

        /// <summary>
        /// How long this blast has existed for, in frames.
        /// </summary>
        public ref float Time => ref Projectile.localAI[0];

        public override string Texture => MiscTexturesRegistry.InvisiblePixelPath;

        public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 5000;

        public override void SetDefaults()
        {
            Projectile.width = 128;
            Projectile.height = 128;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 7200;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 4;
            Projectile.DamageType = DamageClass.Ranged;
        }

        public override void AI()
        {
            ReleaseParticles();

            LaserLength = MathHelper.Clamp(LaserLength + 150f, 0f, 3600f);

            Time++;
            if (Time >= Lifetime)
                Projectile.Kill();

            float lifetimeRatio = Time / Lifetime;
            Projectile.Opacity = Utilities.InverseLerp(1f, 0.55f, lifetimeRatio);
            Projectile.scale = Utilities.InverseLerpBump(0f, 0.2f, 0.45f, 1f, lifetimeRatio);
        }

        public void ReleaseParticles()
        {
            StrongBloom weakBloom = new(Projectile.Center, Vector2.Zero, LaserColorFunction(0.5f).HueShift(0.04f) * 0.4f, 1.25f, 9);
            GeneralParticleHandler.SpawnParticle(weakBloom);

            StrongBloom bloom = new(Projectile.Center, Vector2.Zero, LaserColorFunction(0.5f) * 0.7f, 0.7f, 7);
            GeneralParticleHandler.SpawnParticle(bloom);

            StrongBloom glow = new(Projectile.Center, Vector2.Zero, Color.White, 0.2f, 13);
            GeneralParticleHandler.SpawnParticle(glow);

            for (int i = 0; i < 2; i++)
            {
                Vector2 sparkSpawnPosition = Projectile.Center;
                ElectricSparkParticle spark = new(sparkSpawnPosition, Main.rand.NextVector2Circular(11f, 11f), Color.Wheat, LaserColorFunction(0.5f) * 0.8f, 14, Vector2.One * 0.2f);
                spark.Spawn();
            }

            Vector2 perpendicular = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i < 2; i++)
            {
                Vector2 pixelSpawnPosition = Projectile.Center + Projectile.velocity * Main.rand.NextFloat(LaserLength) + perpendicular * Main.rand.NextFloatDirection() * Projectile.width * 0.4f;
                BloomPixelParticle pixel = new(pixelSpawnPosition, Projectile.velocity * Main.rand.NextFloat(10f, 27f), Color.White, LaserColorFunction(0.5f) * 0.7f, 13, Vector2.One * Main.rand.NextFloat(0.8f, 1.6f));
                pixel.Spawn();
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer == Projectile.owner && Owner.ownedProjectileCounts[ModContent.ProjectileType<PrismExplosionLarge>()] <= 3)
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero, ModContent.ProjectileType<PrismExplosionLarge>(), 0, 0f, Projectile.owner).MaxUpdates = 6;
        }

        public float LaserWidthFunction(float completionRatio)
        {
            float maxWidth = Projectile.width * MathHelper.SmoothStep(0.7f, 1f, Utilities.Cos01(MathHelper.Pi * completionRatio * 3f - Main.GlobalTimeWrappedHourly * 20f));
            return Projectile.scale * MathHelper.SmoothStep(0f, maxWidth, Utilities.InverseLerp(0f, 0.1f, completionRatio));
        }

        public Color LaserColorFunction(float completionRatio) => Projectile.GetAlpha(Main.hslToRgb(LaserHue, 0.93f, 0.55f)) * Utilities.InverseLerp(1f, 0.67f, completionRatio);

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
            ManagedShader blastShader = ShaderManager.GetShader("WoTM.SurgeDriverBlastShader");
            blastShader.TrySetParameter("innerGlowColor", LaserColorFunction(0.5f).HueShift(-0.07f));
            blastShader.SetTexture(TextureAssets.Extra[ExtrasID.FlameLashTrailShape], 1, SamplerState.LinearWrap);
            blastShader.SetTexture(TextureAssets.Extra[ExtrasID.RainbowRodTrailShape], 2, SamplerState.LinearWrap);
            blastShader.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 3, SamplerState.LinearWrap);

            PrimitiveSettings settings = new(LaserWidthFunction, LaserColorFunction, null, Pixelate: true, Shader: blastShader);
            List<Vector2> laserControlPoints = Projectile.GetLaserControlPoints(12, LaserLength);

            PrimitiveRenderer.RenderTrail(laserControlPoints, settings, 48);
        }

        public override bool? CanDamage() => true;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            float laserWidth = LaserWidthFunction(0.25f) * 0.85f;
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity * LaserLength;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, laserWidth, ref _);
        }
    }
}
