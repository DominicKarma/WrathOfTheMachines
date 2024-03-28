﻿using CalamityMod.NPCs.ExoMechs.Ares;
using Luminance.Assets;
using Luminance.Common.DataStructures;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WoTM.Assets;
using WoTM.Content.Particles;

namespace WoTM.Content.NPCs.ExoMechs.Projectiles
{
    public class LargeTeslaSphere : ModProjectile, IProjOwnedByBoss<AresBody>, IExoMechProjectile
    {
        /// <summary>
        /// How long this sphere has existed, in frames.
        /// </summary>
        public ref float Time => ref Projectile.ai[0];

        /// <summary>
        /// The effective amount of spin that has elapsed thus far. Used by the sphere shader.
        /// </summary>
        public ref float SphereSpinScrollOffset => ref Projectile.localAI[0];

        public override string Texture => MiscTexturesRegistry.InvisiblePixelPath;

        public ExoMechDamageSource DamageType => ExoMechDamageSource.Electricity;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 240000;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            for (int i = 0; i < 2; i++)
                CreateElectricSpark();

            CreateElectricPixel();

            if (Time % 13 == 12)
                CreateConvergingCircleParticle();

            Projectile.scale = MathHelper.Lerp(1f, 1.06f, Utilities.Cos01(MathHelper.TwoPi * Time / 6.3f));

            if (Projectile.width < 750)
                Projectile.Resize((int)(Projectile.width * 1.015f) + 1, (int)(Projectile.height * 1.015f) + 1);
            if (Main.mouseRight && Main.mouseRightRelease)
                Projectile.Resize(120, 120);

            SphereSpinScrollOffset += Projectile.width * 0.000023f;

            Time++;
        }

        /// <summary>
        /// Creates a single electric spark around the sphere's edge.
        /// </summary>
        public void CreateElectricSpark()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int arcLifetime = Main.rand.Next(9, 16);
            Vector2 arcSpawnPosition = Projectile.Center + Main.rand.NextVector2Unit() * (Projectile.width * 0.41f - Main.rand.NextFloat(20f, 85f));
            Vector2 arcLength = Main.rand.NextVector2Unit() * Main.rand.NextFloat(40f, 60f);

            if (Vector2.Dot(arcLength, Projectile.Center - arcSpawnPosition) > 0f)
                arcLength *= -1f;

            if (Main.rand.NextBool(3))
                arcLength *= 1.3f;
            if (Main.rand.NextBool(3))
                arcLength *= 1.3f;
            if (Main.rand.NextBool(5))
                arcLength *= 1.5f;
            if (Main.rand.NextBool(5))
                arcLength *= 1.5f;

            Utilities.NewProjectileBetter(Projectile.GetSource_FromThis(), arcSpawnPosition, arcLength, ModContent.ProjectileType<TeslaArc>(), 0, 0f, -1, arcLifetime, 0f);
        }

        /// <summary>
        /// Creates a single electric pixel around the sphere's edge.
        /// </summary>
        public void CreateElectricPixel()
        {
            Vector2? pixelHomeDestination = Main.rand.NextBool(6) ? Projectile.Center : null;
            float pixelOffsetAngle = Main.rand.NextBool(10) || pixelHomeDestination is not null ? Main.rand.NextFloat(0.9f, 1.4f) : Main.rand.NextFloat(-0.3f, 0.3f);

            Vector2 pixelSpawnPosition = Projectile.Center + Main.rand.NextVector2Unit() * (Projectile.width * 0.5f + Main.rand.NextFloat(50f, 175f));

            float pixelSpeedFactor = Main.rand.NextFloat(0.019f, 0.07f);
            Vector2 pixelVelocity = (Projectile.Center - pixelSpawnPosition).RotatedBy(pixelOffsetAngle) * pixelSpeedFactor;

            BloomPixelParticle pixel = new(pixelSpawnPosition, pixelVelocity, Color.White, Color.DeepSkyBlue * 0.4f, 23, Vector2.One * Main.rand.NextFloat(1f, 1.85f), pixelHomeDestination);
            pixel.Spawn();
        }

        /// <summary>
        /// Creates a single converging circle particle that pulses inward onto the origin of the sphere.
        /// </summary>
        public void CreateConvergingCircleParticle()
        {
            HollowCircleParticle circle = new(Projectile.Center, Vector2.Zero, Color.CadetBlue, 11, Projectile.width / 85f, 0.6f, 0.72f);
            circle.Spawn();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Texture2D bloom = MiscTexturesRegistry.BloomCircleSmall.Value;
            Main.spriteBatch.Draw(bloom, drawPosition, null, Projectile.GetAlpha(new(1f, 1f, 1f, 0f)) * 0.5f, 0f, bloom.Size() * 0.5f, Projectile.Size / bloom.Size() * 1.8f, 0, 0f);
            Main.spriteBatch.Draw(bloom, drawPosition, null, Projectile.GetAlpha(new(0.34f, 0.5f, 1f, 0f)) * 0.4f, 0f, bloom.Size() * 0.5f, Projectile.Size / bloom.Size() * 3f, 0, 0f);

            Main.spriteBatch.PrepareForShaders();

            ManagedShader shader = ShaderManager.GetShader("WoTM.LargeTeslaSphereShader");
            shader.SetTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/HarshNoise"), 1, SamplerState.LinearWrap);
            shader.SetTexture(NoiseTexturesRegistry.ElectricNoise.Value, 2, SamplerState.LinearWrap);
            shader.TrySetParameter("textureSize0", Projectile.Size);
            shader.TrySetParameter("posterizationPrecision", 14f);
            shader.TrySetParameter("sphereSpinScrollOffset", SphereSpinScrollOffset);
            shader.TrySetParameter("ridgeNoiseInterpolationStart", 0.23f);
            shader.TrySetParameter("ridgeNoiseInterpolationEnd", 0.09f);
            shader.Apply();

            Texture2D pixel = MiscTexturesRegistry.Pixel.Value;
            Main.spriteBatch.Draw(pixel, drawPosition, null, Projectile.GetAlpha(new(0.7f, 1f, 1f)), 0f, pixel.Size() * 0.5f, Projectile.Size * Projectile.scale / pixel.Size() * 1.2f, 0, 0f);

            Main.spriteBatch.ResetToDefault();

            return false;
        }
    }
}