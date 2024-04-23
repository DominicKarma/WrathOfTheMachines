using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.NPCs;
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
    public class ExoOverloadDeathray : ModProjectile, IPixelatedPrimitiveRenderer, IProjOwnedByBoss<AresBody>, IExoMechProjectile
    {
        public PixelationPrimitiveLayer LayerToRenderTo => PixelationPrimitiveLayer.AfterNPCs;

        /// <summary>
        /// The rotation of this deathray.
        /// </summary>
        public Quaternion Rotation
        {
            get;
            set;
        }

        /// <summary>
        /// How long this sphere has existed, in frames.
        /// </summary>
        public ref float Time => ref Projectile.ai[1];

        /// <summary>
        /// How long this laserbeam current is.
        /// </summary>
        public ref float LaserbeamLength => ref Projectile.ai[2];

        /// <summary>
        /// How long the explosion lasts.
        /// </summary>
        public static int Lifetime => Utilities.SecondsToFrames(9000f);

        /// <summary>
        /// The maximum length of this laserbeam.
        /// </summary>
        public static float MaxLaserbeamLength => 9000f;

        public override string Texture => MiscTexturesRegistry.InvisiblePixelPath;

        public ExoMechDamageSource DamageType => ExoMechDamageSource.Plasma;

        public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 6000;

        public override void SetDefaults()
        {
            Projectile.width = 180;
            Projectile.height = 180;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = Lifetime;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            if (CalamityGlobalNPC.draedonExoMechPrime == -1 || !Main.npc[CalamityGlobalNPC.draedonExoMechPrime].active || !Main.npc[CalamityGlobalNPC.draedonExoMechPrime].TryGetBehavior(out AresBodyBehaviorOverride ares))
            {
                Projectile.Kill();
                return;
            }

            float sine = MathF.Sin(MathHelper.TwoPi * Time / 150f);
            float cosine = MathF.Cos(MathHelper.TwoPi * Time / 150f);
            var quaternionRotation = Matrix.CreateRotationZ(cosine * 0.2f) * Matrix.CreateRotationY(sine * 0.99f - MathHelper.PiOver2);
            Rotation = Quaternion.CreateFromRotationMatrix(quaternionRotation);

            Projectile.Center = ares.CorePosition;
            LaserbeamLength = MathHelper.Clamp(LaserbeamLength + 167f, 0f, MaxLaserbeamLength);

            Time++;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
        }

        public static void CalculatePrimitiveMatrices(out Matrix viewMatrix, out Matrix projectionMatrix)
        {
            int width = Main.instance.GraphicsDevice.Viewport.Width;
            int height = Main.instance.GraphicsDevice.Viewport.Height;

            Vector2 zoom = Main.GameViewMatrix.Zoom;
            Matrix zoomScaleMatrix = Matrix.CreateScale(zoom.X, zoom.Y, 1f);

            // Get a matrix that aims towards the Z axis (these calculations are relative to a 2D world).
            viewMatrix = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up);

            // Offset the matrix to the appropriate position.
            viewMatrix *= Matrix.CreateTranslation(0f, -height, 0f);

            // Flip the matrix around 180 degrees.
            viewMatrix *= Matrix.CreateRotationZ(MathHelper.Pi);

            // Account for the inverted gravity effect.
            if (Main.LocalPlayer.gravDir == -1f)
                viewMatrix *= Matrix.CreateScale(1f, -1f, 1f) * Matrix.CreateTranslation(0f, height, 0f);

            // And account for the current zoom.
            viewMatrix *= zoomScaleMatrix;

            projectionMatrix = Matrix.CreateOrthographicOffCenter(0f, width * zoom.X, 0f, height * zoom.Y, -1f, 1f);
            projectionMatrix *= zoomScaleMatrix;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector3 start = new(Projectile.Center - Main.screenPosition, 0f);
            Vector3 end = start + Vector3.Transform(Vector3.UnitX, Rotation) * LaserbeamLength;
            end.Z /= LaserbeamLength;

            GraphicsDevice gd = Main.instance.GraphicsDevice;
            gd.RasterizerState = RasterizerState.CullNone;

            Vector3[] palette = new Vector3[CalamityUtils.ExoPalette.Length];
            for (int i = 0; i < palette.Length; i++)
                palette[i] = CalamityUtils.ExoPalette[i].ToVector3();

            CalculatePrimitiveMatrices(out Matrix view, out Matrix projection);
            ManagedShader overloadShader = ShaderManager.GetShader("WoTM.ExoOverloadDeathrayShader");
            overloadShader.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 1, SamplerState.LinearWrap);
            overloadShader.SetTexture(MiscTexturesRegistry.TurbulentNoise.Value, 2, SamplerState.LinearWrap);
            overloadShader.TrySetParameter("uWorldViewProjection", view * projection);
            overloadShader.TrySetParameter("scrollColorA", new Vector3(1f, 0.4f, 0.25f));
            overloadShader.TrySetParameter("scrollColorB", new Vector3(0.9f, 0f, 0.3f));
            overloadShader.Apply();

            GetVerticesAndIndices(start, end, MathHelper.Pi, out VertexPositionColorTexture[] rightVertices, out int[] indices);
            gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, rightVertices, 0, rightVertices.Length, indices, 0, indices.Length / 3);

            GetVerticesAndIndices(start, end, 0f, out VertexPositionColorTexture[] leftVertices, out _);
            gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, leftVertices, 0, leftVertices.Length, indices, 0, indices.Length / 3);

            return false;
        }

        public void GetVerticesAndIndices(Vector3 start, Vector3 end, float cylinderOffsetAngle, out VertexPositionColorTexture[] vertices, out int[] indices)
        {
            int widthSegments = 15;
            int heightSegments = 2;
            int numVertices = (widthSegments + 1) * (heightSegments + 1);
            int numIndices = widthSegments * heightSegments * 6;

            vertices = new VertexPositionColorTexture[numVertices];
            indices = new int[numIndices];

            float widthStep = 1.0f / widthSegments;
            float heightStep = 1.0f / heightSegments;

            // Create vertices
            for (int j = 0; j <= heightSegments; j++)
            {
                for (int i = 0; i <= widthSegments; i++)
                {
                    float width = Utils.Remap(j * heightStep, 0f, 0.5f, 6f, Projectile.width * Projectile.scale);
                    float angle = MathHelper.Pi * i * widthStep + cylinderOffsetAngle;
                    Vector3 orthogonalOffset = Vector3.Transform(new Vector3(0f, MathF.Sin(angle), MathF.Cos(angle)), Rotation) * width;
                    Vector3 cylinderPoint = Vector3.Lerp(start, end, j * heightStep) + orthogonalOffset;
                    cylinderPoint.Z /= LaserbeamLength;

                    vertices[j * (widthSegments + 1) + i] = new(cylinderPoint, Color.Black, new Vector2(j * heightStep, i * widthStep));
                }
            }

            // Create indices
            for (int j = 0; j < heightSegments; j++)
            {
                for (int i = 0; i < widthSegments; i++)
                {
                    int upperLeft = j * (widthSegments + 1) + i;
                    int upperRight = upperLeft + 1;
                    int lowerLeft = upperLeft + (widthSegments + 1);
                    int lowerRight = lowerLeft + 1;

                    indices[(j * widthSegments + i) * 6] = upperLeft;
                    indices[(j * widthSegments + i) * 6 + 1] = lowerRight;
                    indices[(j * widthSegments + i) * 6 + 2] = lowerLeft;

                    indices[(j * widthSegments + i) * 6 + 3] = upperLeft;
                    indices[(j * widthSegments + i) * 6 + 4] = upperRight;
                    indices[(j * widthSegments + i) * 6 + 5] = lowerRight;
                }
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // TODO -- Implement this.
            return false;
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
