using Luminance.Assets;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using WoTM.Content.NPCs.ExoMechs.Projectiles;
using WoTM.Content.Particles;

namespace WoTM.Content.Items.ExoelectricFieldDestabilizer
{
    public class ExoelectricFieldDestabilizerOrb : ModProjectile
    {
        /// <summary>
        /// The owner of this orb.
        /// </summary>
        public Player Owner => Main.player[Projectile.owner];

        /// <summary>
        /// Whether this orb is currently disappearing.
        /// </summary>
        public bool Disappearing
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value.ToInt();
        }

        /// <summary>
        /// Whether this orb has been launched.
        /// </summary>
        public bool Launched
        {
            get => Projectile.ai[0] == 2f;
            set => Projectile.ai[0] = value ? 2f : 0f;
        }

        /// <summary>
        /// How long this orb has existed for, in frames.
        /// </summary>
        public ref float Time => ref Projectile.ai[2];

        public override string Texture => MiscTexturesRegistry.InvisiblePixelPath;

        public override void SetDefaults()
        {
            Projectile.width = 270;
            Projectile.height = 270;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 9000000;
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

            Time++;
            if (Launched)
            {
                int maxPenetrate = 15;
                if (Projectile.penetrate > maxPenetrate || Projectile.penetrate < 0)
                    Projectile.penetrate = maxPenetrate;

                Projectile.velocity = (Projectile.velocity * 1.06f + Projectile.velocity.SafeNormalize(Vector2.Zero) * 2f).ClampLength(0f, 50f);
                Projectile.scale = MathHelper.Lerp(Projectile.scale, Utilities.InverseLerp(240f, 180f, Time) + (maxPenetrate - Projectile.penetrate) * 0.12f, 0.067f);
                Projectile.Opacity = MathHelper.Lerp(Projectile.Opacity, Projectile.penetrate / (float)maxPenetrate, 0.54f);

                if (Time >= 240f)
                    Projectile.Kill();

                return;
            }

            if (Disappearing)
            {
                Projectile.velocity *= 0.94f;
                Projectile.scale *= 0.9f;
                if (Projectile.scale <= 0.01f)
                    Projectile.Kill();

                return;
            }

            if (!Owner.channel)
            {
                Disappearing = true;
                Projectile.netUpdate = true;
                return;
            }

            Projectile.Opacity = Utilities.InverseLerp(0f, 17f, Time);
            Projectile.scale = MathHelper.SmoothStep(0f, 1f, Projectile.Opacity.Squared());

            AimAheadOfMouse();
        }

        /// <summary>
        /// Makes this tesla field stay in front of the mouse.
        /// </summary>
        public void AimAheadOfMouse()
        {
            if (Main.myPlayer == Projectile.owner)
            {
                Vector2 hoverDestination = Owner.RotatedRelativePoint(Owner.MountedCenter) + Owner.SafeDirectionTo(Main.MouseWorld) * Projectile.width * Projectile.scale * 0.81f;
                if (!Projectile.WithinRange(hoverDestination, 8f))
                {
                    Projectile.SmoothFlyNear(hoverDestination, 0.14f, 0.8f);
                    Projectile.netUpdate = true;
                    Projectile.netSpam = 0;
                }
                else
                    Projectile.velocity *= 0.93f;
            }
        }

        public void ReleaseParticles()
        {
            for (int i = 0; i < 2; i++)
            {
                if (Main.myPlayer == Projectile.owner && Main.rand.NextBool(Projectile.scale.Cubed()))
                {
                    Vector2 endOfOrb = Projectile.Center + Main.rand.NextVector2CircularEdge(Projectile.width, Projectile.height) * Projectile.scale * 0.32f;
                    Vector2 arcLength = Main.rand.NextVector2Circular(200f, 200f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), endOfOrb, arcLength, ModContent.ProjectileType<SmallTeslaArc>(), 0, 0f, Projectile.owner, Main.rand.Next(8, 13));
                }
            }

            for (int i = 0; i < 2; i++)
            {
                if (Main.rand.NextBool(Projectile.scale.Squared()))
                {
                    Vector2 pixelSpawnPosition = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width, Projectile.height) * Projectile.scale * 0.7f;
                    Vector2 pixelVelocity = Projectile.SafeDirectionTo(pixelSpawnPosition).RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(24f) - Projectile.velocity * 0.25f;
                    Color pixelColor = Color.Lerp(Color.Aqua, Color.Blue, Main.rand.NextFloat(0.7f));
                    BloomPixelParticle pixel = new(pixelSpawnPosition, pixelVelocity, Color.White, pixelColor * 0.5f, 15, Vector2.One * Main.rand.NextFloat(0.6f, 2f));
                    pixel.Spawn();
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.velocity *= 0.17f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Time <= 2f)
                return false;

            Main.spriteBatch.PrepareForShaders();

            float maxSphereSize = Projectile.width * MathHelper.Lerp(0.9f, 1.1f, Utilities.Cos01(MathHelper.TwoPi * Main.GlobalTimeWrappedHourly * 4f));
            Vector2 sphereSize = Vector2.One * MathHelper.SmoothStep(0f, maxSphereSize, Projectile.scale);

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Texture2D bloom = MiscTexturesRegistry.BloomCircleSmall.Value;
            Main.spriteBatch.Draw(bloom, drawPosition, null, Projectile.GetAlpha(new(1f, 1f, 1f, 0f)) * 0.5f, 0f, bloom.Size() * 0.5f, sphereSize / bloom.Size() * 1.8f, 0, 0f);
            Main.spriteBatch.Draw(bloom, drawPosition, null, Projectile.GetAlpha(new(0.34f, 0.5f, 1f, 0f)) * 0.4f, 0f, bloom.Size() * 0.5f, sphereSize / bloom.Size() * 3f, 0, 0f);

            Main.spriteBatch.PrepareForShaders();

            ManagedShader shader = ShaderManager.GetShader("WoTM.LargeTeslaSphereShader");
            shader.TrySetParameter("pixelationFactor", Vector2.One * 2f / Projectile.Size);
            shader.TrySetParameter("posterizationPrecision", 14f);
            shader.SetTexture(MiscTexturesRegistry.DendriticNoiseZoomedOut.Value, 1, SamplerState.LinearWrap);
            shader.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 2, SamplerState.LinearWrap);
            shader.Apply();

            Texture2D pixel = MiscTexturesRegistry.Pixel.Value;
            Main.spriteBatch.Draw(pixel, drawPosition, null, Projectile.GetAlpha(new(0.5f, 1f, 1f)), 0f, pixel.Size() * 0.5f, sphereSize * Projectile.scale / pixel.Size() * 1.2f, 0, 0f);

            Main.spriteBatch.ResetToDefault();

            return false;
        }

        public override bool? CanDamage() => Launched;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            Utilities.CircularHitboxCollision(Projectile.Center, Projectile.width * Projectile.scale * 0.5f, targetHitbox);
    }
}
