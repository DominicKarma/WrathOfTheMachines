using System.Collections.Generic;
using Luminance.Assets;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

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
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 7200;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
            Projectile.scale = 1f;
            Projectile.Opacity = 0f;
            Projectile.DamageType = DamageClass.Ranged;
        }

        public override void AI()
        {
            ReleaseParticles();

            LaserLength = MathHelper.Clamp(LaserLength + 150f, 0f, 3600f);

            Time++;
            if (Time >= Lifetime)
                Projectile.Kill();
        }

        public void ReleaseParticles()
        {
        }

        public float LaserWidthFunction(float completionRatio) => Projectile.scale * 32f;

        public Color LaserColorFunction(float completionRatio) => Projectile.GetAlpha(Main.hslToRgb(LaserHue, 0.93f, 0.55f));

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
            PrimitiveSettings settings = new(LaserWidthFunction, LaserColorFunction, null, Pixelate: true);
            List<Vector2> laserControlPoints = Projectile.GetLaserControlPoints(12, LaserLength);

            PrimitiveRenderer.RenderTrail(laserControlPoints, settings, 48);
        }

        public override bool? CanDamage() => false;

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
