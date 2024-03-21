using System;
using CalamityMod.NPCs.ExoMechs;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace DifferentExoMechs.Content.NPCs.ExoMechs
{
    public class CustomExoMechsSky : CustomSky
    {
        private bool skyActive;

        /// <summary>
        /// The general opacity of this sky.
        /// </summary>
        public static new float Opacity
        {
            get;
            set;
        }

        /// <summary>
        /// The intensity of the red sirens effect.
        /// </summary>
        public static float RedSirensIntensity
        {
            get;
            set;
        }

        /// <summary>
        /// The identifier key for this sky.
        /// </summary>
        public const string SkyKey = "DifferentExoMechs:ExoMechsSky";

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            // Calculate the maximum sky opacity value.
            // If Draedon is not present it is assumed that the Exo Mechs were just spawned in via cheating, and as such they sky should immediately draw at its maximum intensity, rather than not at all.
            float maxSkyOpacity = 1f;
            float planeForwardInterpolant = 0f;
            int draedonIndex = NPC.FindFirstNPC(ModContent.NPCType<Draedon>());
            if (draedonIndex >= 0 && Main.npc[draedonIndex].TryGetBehavior(out DraedonBehaviorOverride behavior))
            {
                maxSkyOpacity = behavior.MaxSkyOpacity;
                planeForwardInterpolant = 1f - behavior.PlaneFlyForwardInterpolant;
            }

            // Increase or decrease the opacity of this sky based on whether it's active or not, stopping at 0-1 bounds.
            Opacity = MathHelper.Clamp(Opacity + skyActive.ToDirectionInt() * 0.02f, 0f, maxSkyOpacity);

            // Prevent drawing beyond the back layer.
            if (maxDepth >= float.MaxValue)
                DrawGreySky();

            if (minDepth <= float.MinValue)
            {
                PrimitivePixelationSystem.RenderToPrimsNextFrame(() =>
                {
                    DrawPlane(planeForwardInterpolant);
                }, PixelationPrimitiveLayer.AfterProjectiles);
            }
        }

        public static void DrawGreySky()
        {
            Vector2 scale = new(Main.screenWidth * 1.1f / TextureAssets.MagicPixel.Value.Width, Main.screenHeight * 1.1f / TextureAssets.MagicPixel.Value.Height);
            Vector2 screenArea = new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
            Color drawColor = new Color(0.05f, 0.05f, 0.05f) * Opacity;
            Vector2 origin = TextureAssets.MagicPixel.Value.Size() * 0.5f;

            // Draw a grey background as base.
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, screenArea, null, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);
        }

        public override void Update(GameTime gameTime)
        {
            // Disable ambient sky objects like wyverns and eyes appearing in front of the background.
            if (Opacity >= 0.5f)
                SkyManager.Instance["Ambience"].Deactivate();

            // Disable Calamity's vanilla Exo Mechs background.
            if (Opacity >= 0.01f)
                SkyManager.Instance["CalamityMod:ExoMechs"]?.Deactivate();

            if (!skyActive)
                ResetVariablesWhileInactivity();
        }

        public static void DrawPlane(float forwardInterpolant)
        {
            if (forwardInterpolant <= 0f || forwardInterpolant >= 1f)
                return;

            Vector2 screenSize = new(Main.instance.GraphicsDevice.Viewport.Width, Main.instance.GraphicsDevice.Viewport.Height);
            Vector3 planePosition = new(screenSize * new Vector2(0.5f, 0.45f), MathHelper.Lerp(100f, -0.95f, MathF.Pow(1f - forwardInterpolant, 0.67f)));
            float scale = 0.7f / (planePosition.Z + 1f);
            float opacity = Utilities.InverseLerp(100f, 54f, planePosition.Z);
            planePosition.Y -= scale * 1560f;

            Matrix rotation = Matrix.CreateRotationX((1f - forwardInterpolant) * 0.5f) * Matrix.CreateRotationZ(MathHelper.Pi);

            Matrix world = rotation * Matrix.CreateScale(scale) * Matrix.CreateWorld(planePosition, Vector3.Forward, Vector3.Up);
            Matrix projection = Matrix.CreateOrthographicOffCenter(0f, screenSize.X, screenSize.Y, 0f, -5000f, 5000f);
            Model plane = ModelRegistry.CargoPlane;

            // Prepare shaders.
            ManagedShader shader = ShaderManager.GetShader("ModelPrimitiveShader");
            Main.instance.GraphicsDevice.Textures[0] = ModContent.Request<Texture2D>("DifferentExoMechs/Content/NPCs/ExoMechs/CargoPlaneModelTexture").Value;
            Main.instance.GraphicsDevice.BlendState = BlendState.AlphaBlend;
            Main.instance.GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

            foreach (ModelMesh mesh in plane.Meshes)
            {
                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    if (part.PrimitiveCount > 0)
                    {
                        shader.TrySetParameter("opacity", opacity);
                        shader.TrySetParameter("uWorldViewProjection", mesh.ParentBone.Transform * world * projection);
                        shader.Apply();

                        Main.instance.GraphicsDevice.SetVertexBuffer(part.VertexBuffer);
                        Main.instance.GraphicsDevice.Indices = part.IndexBuffer;
                        Main.instance.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, part.VertexOffset, 0, part.NumVertices, part.StartIndex, part.PrimitiveCount);
                    }
                }
            }
        }

        public static void ResetVariablesWhileInactivity()
        {
            RedSirensIntensity = Utilities.Saturate(RedSirensIntensity - 0.1f);
        }

        #region Boilerplate
        public override void Deactivate(params object[] args) => skyActive = false;

        public override void Reset() => skyActive = false;

        public override bool IsActive() => skyActive || Opacity > 0f;

        public override void Activate(Vector2 position, params object[] args) => skyActive = true;

        public override float GetCloudAlpha() => 1f - Opacity;
        #endregion Boilerplate
    }
}
