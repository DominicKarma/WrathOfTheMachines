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

namespace WoTM.Content.NPCs.ExoMechs.Projectiles
{
    public class TeslaArc : ModProjectile, IPixelatedPrimitiveRenderer, IProjOwnedByBoss<AresBody>, IExoMechProjectile
    {
        /// <summary>
        /// The width factor to be used in the <see cref="ArcWidthFunction(float)"/>.
        /// </summary>
        public float WidthFactor;

        /// <summary>
        /// The color to be used in the <see cref="ArcColorFunction(float)"/>.
        /// </summary>
        public Color ArcColor;

        /// <summary>
        /// The set of all points used to draw the composite arc.
        /// </summary>
        /// 
        /// <remarks>
        /// This array is not synced, but since it's a purely visual effect it shouldn't need to be.
        /// </remarks>
        public Vector2[] ArcPoints;

        /// <summary>
        /// How long this sphere should exist, in frames.
        /// </summary>
        public ref float Lifetime => ref Projectile.ai[0];

        /// <summary>
        /// The maximum angle by which this arc should rotate.
        /// </summary>
        public ref float ArcAngle => ref Projectile.ai[1];

        /// <summary>
        /// How long this sphere has existed, in frames.
        /// </summary>
        public ref float Time => ref Projectile.ai[2];

        public override string Texture => MiscTexturesRegistry.InvisiblePixelPath;

        public ExoMechDamageSource DamageType => ExoMechDamageSource.Electricity;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 7200;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public void GenerateArcPoints()
        {
            ArcPoints = new Vector2[25];

            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity * Main.rand.NextFloat(0.67f, 1.2f) + Main.rand.NextVector2Circular(30f, 30f);
            Vector2 farFront = start - Projectile.velocity.RotatedByRandom(3.1f) * Main.rand.NextFloat(0.6f, 2.54f);
            Vector2 farEnd = end + Projectile.velocity.RotatedByRandom(3.1f) * 4f;
            for (int i = 0; i < ArcPoints.Length; i++)
            {
                ArcPoints[i] = Vector2.CatmullRom(farFront, start, end, farEnd, i / (float)(ArcPoints.Length - 1f));

                if (Main.rand.NextBool(9))
                    ArcPoints[i] += Main.rand.NextVector2CircularEdge(10f, 10f);
            }
        }

        public override void AI()
        {
            if (ArcPoints is null)
                GenerateArcPoints();
            else
            {
                for (int i = 0; i < ArcPoints.Length; i += 2)
                {
                    float trailCompletionRatio = i / (float)(ArcPoints.Length - 1f);
                    float arcProtrudeAngleOffset = Main.rand.NextFloatDirection() * 0.99f + MathHelper.PiOver2;
                    float arcProtrudeDistance = Main.rand.NextFloatDirection() * 9f;
                    if (Main.rand.NextBool(100))
                        arcProtrudeDistance *= 7f;
                    Vector2 arcOffset = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(arcProtrudeAngleOffset) * arcProtrudeDistance;

                    ArcPoints[i] += arcOffset * Utilities.Convert01To010(trailCompletionRatio);
                }
            }

            Time++;
            if (Time >= Lifetime)
                Projectile.Kill();
        }

        public float ArcWidthFunction(float completionRatio)
        {
            float lifetimeRatio = Time / Lifetime;
            float lifetimeSquish = Utilities.InverseLerpBump(0.1f, 0.35f, 0.75f, 1f, lifetimeRatio);
            return MathHelper.Lerp(1f, 3f, Utilities.Convert01To010(completionRatio)) * lifetimeSquish * WidthFactor;
        }

        public Color ArcColorFunction(float completionRatio) => Projectile.GetAlpha(ArcColor);

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
            if (ArcPoints is null)
                return;

            float lifetimeRatio = Time / Lifetime;
            ManagedShader shader = ShaderManager.GetShader("WoTM.TeslaArcShader");
            shader.TrySetParameter("lifetimeRatio", lifetimeRatio);
            shader.TrySetParameter("erasureThreshold", 0.7f);
            shader.SetTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/Neurons2"), 1, SamplerState.LinearWrap);
            shader.Apply();

            PrimitiveSettings settings = new(ArcWidthFunction, ArcColorFunction, Smoothen: false, Pixelate: true, Shader: shader);

            ArcColor = Color.Lerp(new Color(0.3f, 0.86f, 1f), new Color(0.75f, 0.83f, 1f), Projectile.identity / 19f % 1f);
            WidthFactor = 1f;

            PrimitiveRenderer.RenderTrail(ArcPoints, settings, 39);

            ArcColor = new Color(1f, 1f, 1f, 0f);
            WidthFactor = 0.5f;

            PrimitiveRenderer.RenderTrail(ArcPoints, settings, 39);
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
