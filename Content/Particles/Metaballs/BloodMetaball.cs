using System;
using System.Linq;
using Luminance.Assets;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using WoTM.Assets;

namespace WoTM.Content.Particles.Metaballs
{
    public class BloodMetaball : MetaballType
    {
        public override string MetaballAtlasTextureToUse => "WoTM.DistancedMetaballCircle.png";

        public override Color EdgeColor => Color.Transparent;

        public override bool ShouldRender => ActiveParticleCount >= 1;

        public override bool DrawnManually => false;

        public override Func<Texture2D>[] LayerTextures => [() => Main.gameMenu ? MiscTexturesRegistry.InvisiblePixel.Value : NoiseTexturesRegistry.BubblyNoise.Value];

        public override void UpdateParticle(MetaballInstance particle)
        {
            particle.Size *= 0.96f;
            particle.Velocity.X *= 0.97f;
            particle.Velocity.Y += 0.56f;
            if (particle.Velocity.Y > 16f)
                particle.Velocity.Y = MathHelper.Lerp(particle.Velocity.Y, 16f, 0.06f);

            particle.Velocity = Collision.TileCollision(particle.Center, particle.Velocity, 1, 16);
        }

        public override bool ShouldKillParticle(MetaballInstance particle) => particle.Size <= 3f;

        public override void PrepareShaderForTarget(int layerIndex)
        {
            // Store the shader in an easy to use local variable.
            ManagedShader metaballShader = ShaderManager.GetShader("WoTM.BloodMetaballShader");

            // Fetch the layer texture. This is the texture that will be overlaid over the greyscale contents on the screen.
            Texture2D layerTexture = LayerTextures[layerIndex]();

            // Calculate the layer scroll offset. This is used to ensure that the texture contents of the given metaball have parallax, rather than being static over the screen
            // regardless of world position.
            // This may be toggled off optionally by the metaball.
            Vector2 screenSize = Main.ScreenSize.ToVector2();
            Vector2 layerScrollOffset = Main.screenPosition / screenSize + CalculateManualOffsetForLayer(layerIndex);
            if (LayerIsFixedToScreen(layerIndex))
                layerScrollOffset = Vector2.Zero;

            Vector3[] palette = new Vector3[]
            {
                new Color(189, 8, 2).ToVector3(),
                new Color(99, 7, 8).ToVector3(),
                new Color(56, 5, 19).ToVector3()
            };

            metaballShader.TrySetParameter("layerSize", layerTexture.Size());
            metaballShader.TrySetParameter("screenSize", screenSize);
            metaballShader.TrySetParameter("layerOffset", layerScrollOffset);
            metaballShader.TrySetParameter("singleFrameScreenOffset", (Main.screenLastPosition - Main.screenPosition) / screenSize);
            metaballShader.TrySetParameter("gradient", palette);
            metaballShader.TrySetParameter("gradientCount", palette.Length);
            metaballShader.TrySetParameter("dissolvePersistence", 1.65f);
            metaballShader.TrySetParameter("maxDistortionOffset", new Vector2(20f, 8f));
            metaballShader.SetTexture(NoiseTexturesRegistry.PerlinNoise.Value, 1, SamplerState.LinearWrap);
            metaballShader.SetTexture(NoiseTexturesRegistry.WavyBlotchNoiseDetailed.Value, 2, SamplerState.LinearWrap);
            metaballShader.SetTexture(NoiseTexturesRegistry.DendriticNoise.Value, 3, SamplerState.LinearWrap);

            // Apply the metaball shader.
            metaballShader.Apply();
        }

        public override void DrawInstances()
        {
            AtlasTexture texture = AtlasManager.GetTexture(MetaballAtlasTextureToUse);
            var darknessOrderedParticles = Particles.OrderByDescending(p => p.ExtraInfo[0]);

            foreach (var particle in darknessOrderedParticles)
            {
                float darknessInterpolant = particle.ExtraInfo[0];
                float dissolveInterpolant = Utilities.InverseLerp(2f, 78f, particle.Size);
                float squish = particle.Velocity.Length() * 0.035f;
                if (squish > 0.67f)
                    squish = 0.67f;

                float rotation = particle.Velocity.ToRotation();
                Color color = new(1f, darknessInterpolant, dissolveInterpolant);
                Vector2 particleSize = new Vector2(1f + squish, 1f - squish) * particle.Size;

                Main.spriteBatch.Draw(texture, particle.Center - Main.screenPosition, null, color, rotation, null, particleSize / texture.Frame.Size(), SpriteEffects.None);
            }
        }
    }
}
