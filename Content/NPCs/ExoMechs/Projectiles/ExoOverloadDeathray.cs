using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
        public static float MaxLaserbeamLength => 8000f;

        public const int CylinderWidthSegments = 12;

        public const int CylinderHeightSegments = 2;

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

            float rotationTime = Time / 142f;
            float sine = MathF.Sin(MathHelper.TwoPi * rotationTime);
            var quaternionRotation = Matrix.CreateRotationZ(0.11f) * Matrix.CreateRotationY(sine * 1.29f + MathHelper.PiOver2);
            Rotation = Quaternion.CreateFromRotationMatrix(quaternionRotation);

            Projectile.Center = ares.CorePosition;
            LaserbeamLength = MathHelper.Clamp(LaserbeamLength + 167f, 0f, MaxLaserbeamLength);

            Time++;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
        }

        public void Render(Vector3 start, Vector3 end, Color baseColor, float widthFactor)
        {
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            GetVerticesAndIndices(widthFactor, baseColor, start, end, MathHelper.Pi, out VertexPosition2DColorTexture[] rightVertices, out int[] indices);
            gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, rightVertices, 0, rightVertices.Length, indices, 0, indices.Length / 3);

            GetVerticesAndIndices(widthFactor, baseColor, start, end, 0f, out VertexPosition2DColorTexture[] leftVertices, out _);
            gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, leftVertices, 0, leftVertices.Length, indices, 0, indices.Length / 3);
        }

        public void GetBloomVerticesAndIndices(Color baseColor, Vector3 start, Vector3 end, out VertexPosition2DColorTexture[] leftVertices, out VertexPosition2DColorTexture[] rightVertices, out int[] indices)
        {
            int bloomSubdivisions = 40;
            int numVertices = (CylinderWidthSegments + 1) * (CylinderHeightSegments + 1);
            int numIndices = CylinderWidthSegments * CylinderHeightSegments * 6;

            leftVertices = new VertexPosition2DColorTexture[numVertices * bloomSubdivisions];
            rightVertices = new VertexPosition2DColorTexture[leftVertices.Length];
            indices = new int[numIndices * bloomSubdivisions * 6];

            for (int i = 0; i < bloomSubdivisions; i++)
            {
                float subdivisionInterpolant = i / 39f;
                float bloomWidthFactor = subdivisionInterpolant * 1.3f + 1f;
                Color bloomColor = baseColor * MathHelper.SmoothStep(0.05f, 0.005f, MathF.Sqrt(subdivisionInterpolant));
                GetVerticesAndIndices(bloomWidthFactor, bloomColor, start, end, MathHelper.Pi, out VertexPosition2DColorTexture[] localRightVertices, out int[] localIndices);
                GetVerticesAndIndices(bloomWidthFactor, bloomColor, start, end, 0f, out VertexPosition2DColorTexture[] localLeftVertices, out _);

                int startingIndex = indices.Max();
                for (int j = 0; j < localIndices.Length; j++)
                    indices[j + i * numIndices] = localIndices[j] + startingIndex;
                for (int j = 0; j < localLeftVertices.Length; j++)
                {
                    leftVertices[j + i * numVertices] = localLeftVertices[j];
                    rightVertices[j + i * numVertices] = localRightVertices[j];
                }
            }
        }

        public void GetVerticesAndIndices(float widthFactor, Color baseColor, Vector3 start, Vector3 end, float cylinderOffsetAngle, out VertexPosition2DColorTexture[] vertices, out int[] indices)
        {
            int numVertices = (CylinderWidthSegments + 1) * (CylinderHeightSegments + 1);
            int numIndices = CylinderWidthSegments * CylinderHeightSegments * 6;

            vertices = new VertexPosition2DColorTexture[numVertices];
            indices = new int[numIndices];

            float widthStep = 1.0f / CylinderWidthSegments;
            float heightStep = 1.0f / CylinderHeightSegments;

            // Create vertices
            for (int j = 0; j <= CylinderHeightSegments; j++)
            {
                for (int i = 0; i <= CylinderWidthSegments; i++)
                {
                    float width = Utils.Remap(j * heightStep, 0f, 0.5f, 3f, Projectile.width * Projectile.scale) * widthFactor;
                    float angle = MathHelper.Pi * i * widthStep + cylinderOffsetAngle;
                    Vector3 orthogonalOffset = Vector3.Transform(new Vector3(0f, MathF.Sin(angle), MathF.Cos(angle)), Rotation) * width;
                    Vector3 cylinderPoint = Vector3.Lerp(start, end, j * heightStep) + orthogonalOffset;
                    vertices[j * (CylinderWidthSegments + 1) + i] = new(new(cylinderPoint.X, cylinderPoint.Y), baseColor, new Vector2(j * heightStep, i * widthStep), 1f);
                }
            }

            // Create indices
            for (int j = 0; j < CylinderHeightSegments; j++)
            {
                for (int i = 0; i < CylinderWidthSegments; i++)
                {
                    int upperLeft = j * (CylinderWidthSegments + 1) + i;
                    int upperRight = upperLeft + 1;
                    int lowerLeft = upperLeft + (CylinderWidthSegments + 1);
                    int lowerRight = lowerLeft + 1;

                    indices[(j * CylinderWidthSegments + i) * 6] = upperLeft;
                    indices[(j * CylinderWidthSegments + i) * 6 + 1] = lowerRight;
                    indices[(j * CylinderWidthSegments + i) * 6 + 2] = lowerLeft;

                    indices[(j * CylinderWidthSegments + i) * 6 + 3] = upperLeft;
                    indices[(j * CylinderWidthSegments + i) * 6 + 4] = upperRight;
                    indices[(j * CylinderWidthSegments + i) * 6 + 5] = lowerRight;
                }
            }
        }

        public void RenderBloom(Vector3 start, Vector3 end, Matrix projection)
        {
            Color bloomColor = Color.White with { A = 0 };

            ManagedShader bloomShader = ShaderManager.GetShader("WoTM.CylinderPrimitiveBloomShader");
            bloomShader.TrySetParameter("uWorldViewProjection", projection);
            bloomShader.Apply();

            GetBloomVerticesAndIndices(bloomColor, start, end, out VertexPosition2DColorTexture[] leftVertices, out VertexPosition2DColorTexture[] rightVertices, out int[] indices);

            GraphicsDevice gd = Main.instance.GraphicsDevice;
            gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, rightVertices, 0, rightVertices.Length, indices, 0, indices.Length / 3);
            gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, leftVertices, 0, leftVertices.Length, indices, 0, indices.Length / 3);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector3 start = new(Projectile.Center - Main.screenPosition, 0f);
            Vector3 end = start + Vector3.Transform(Vector3.UnitX, Rotation) * LaserbeamLength;
            end.Z /= LaserbeamLength;

            GraphicsDevice gd = Main.instance.GraphicsDevice;
            gd.RasterizerState = RasterizerState.CullNone;

            int width = Main.screenWidth;
            int height = Main.screenHeight;
            Utilities.CalculatePrimitiveMatrices(width, height, out Matrix view, out Matrix projection);
            Matrix overallProjection = view * projection;

            RenderBloom(start, end, overallProjection);

            ManagedShader overloadShader = ShaderManager.GetShader("WoTM.ExoOverloadDeathrayShader");
            overloadShader.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 1, SamplerState.LinearWrap);
            overloadShader.SetTexture(MiscTexturesRegistry.TurbulentNoise.Value, 2, SamplerState.LinearWrap);
            overloadShader.TrySetParameter("uWorldViewProjection", overallProjection);
            overloadShader.TrySetParameter("scrollColorA", Color.White);
            overloadShader.TrySetParameter("scrollColorB", Color.White);
            overloadShader.TrySetParameter("baseColor", Color.White);
            overloadShader.Apply();

            Render(start, end, Color.White, 1f);
            return false;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector3 start = new(Projectile.Center, 0f);
            Vector3 end = start + Vector3.Transform(Vector3.UnitX, Rotation) * LaserbeamLength;
            end.Z /= LaserbeamLength;

            Vector3 rayDirection = Vector3.Normalize(end - start);
            Vector3 boxMin = new(targetHitbox.TopLeft(), -1f);
            Vector3 boxMax = new(targetHitbox.BottomRight(), 1f);

            Vector3 tMin = (boxMin - start) / rayDirection;
            Vector3 tMax = (boxMax - start) / rayDirection;
            Vector3 t1 = Vector3.Min(tMin, tMax);

            float tNear = MathF.Max(MathF.Max(t1.X, t1.Y), t1.Z);
            Vector3 targetCenter = new(targetHitbox.Center(), 0f);
            Vector3 endPoint = start + rayDirection * tNear;
            Vector3 directionToTarget = Vector3.Normalize(targetCenter - start);
            float distanceToProjection = Vector3.Distance(endPoint, targetCenter);

            return distanceToProjection <= Projectile.width * Projectile.scale && Vector3.Dot(directionToTarget, rayDirection) >= 0.97f;
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
