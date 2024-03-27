using CalamityMod.NPCs.ExoMechs.Ares;
using Luminance.Assets;
using Luminance.Common.DataStructures;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WoTM.Content.Particles;

namespace WoTM.Content.NPCs.ExoMechs.Projectiles
{
    public class LargeTeslaSphere : ModProjectile, IProjOwnedByBoss<AresBody>, IExoMechProjectile
    {
        /// <summary>
        /// How long this sphere has existed, in frames.
        /// </summary>
        public ref float Time => ref Projectile.ai[0];

        public override string Texture => MiscTexturesRegistry.InvisiblePixelPath;

        public ExoMechDamageSource DamageType => ExoMechDamageSource.Electricity;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
        }

        public override void SetDefaults()
        {
            Projectile.width = 320;
            Projectile.height = 320;
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
                ReleaseElectricSpark();

            Vector2? pixelHomeDestination = Main.rand.NextBool(6) ? Projectile.Center : null;
            float pixelOffsetAngle = Main.rand.NextBool(10) || pixelHomeDestination is not null ? Main.rand.NextFloat(0.9f, 1.4f) : Main.rand.NextFloat(-0.3f, 0.3f);
            Vector2 pixelSpawnPosition = Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(0.55f, 0.8f) * Projectile.Size;
            Vector2 pixelVelocity = (Projectile.Center - pixelSpawnPosition).RotatedBy(pixelOffsetAngle) * Main.rand.NextFloat(0.019f, 0.07f);
            BloomPixelParticle pixel = new(pixelSpawnPosition, pixelVelocity, Color.White, Color.DeepSkyBlue * 0.4f, 23, Vector2.One * Main.rand.NextFloat(1f, 1.85f), pixelHomeDestination);
            pixel.Spawn();

            if (Time % 18 == 17)
            {
                HollowCircleParticle circle = new(Projectile.Center, Vector2.Zero, Color.CadetBlue, 16, 3.1f, 0.6f, 0.72f);
                circle.Spawn();
            }

            Projectile.scale = MathHelper.Lerp(1f, 1.1f, Utilities.Cos01(MathHelper.TwoPi * Time / 6.3f));

            Time++;
        }

        /// <summary>
        /// Releases a single electric spark around the sphere.
        /// </summary>
        public void ReleaseElectricSpark()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int arcLifetime = Main.rand.Next(7, 14);
            Vector2 arcSpawnPosition = Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(0.05f, 0.32f) * Projectile.Size;
            Vector2 arcLength = Main.rand.NextVector2Unit() * Main.rand.NextFloat(30f, 210f);
            Utilities.NewProjectileBetter(Projectile.GetSource_FromThis(), arcSpawnPosition, arcLength, ModContent.ProjectileType<TeslaArc>(), 0, 0f, -1, arcLifetime, 0f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.PrepareForShaders();

            ManagedShader shader = ShaderManager.GetShader("WoTM.LargeTeslaSphereShader");
            shader.SetTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/HarshNoise"), 1, SamplerState.LinearWrap);
            shader.TrySetParameter("textureSize0", Projectile.Size);
            shader.Apply();

            Texture2D pixel = MiscTexturesRegistry.Pixel.Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Main.spriteBatch.Draw(pixel, drawPosition, null, Projectile.GetAlpha(new(0.7f, 1f, 1f)), 0f, pixel.Size() * 0.5f, Projectile.Size * Projectile.scale / pixel.Size(), 0, 0f);

            Main.spriteBatch.ResetToDefault();

            return false;
        }
    }
}
