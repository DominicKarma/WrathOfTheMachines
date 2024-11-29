using System;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace WoTM.Content.Particles
{
    public class MetalSparkParticle : Particle
    {
        /// <summary>
        /// The palette this spark interpolates between as it progresses through its life.
        /// </summary>
        public Color[] Palette;

        /// <summary>
        /// The bloom texture.
        /// </summary>
        public static AtlasTexture BloomTexture
        {
            get;
            private set;
        }

        public override string AtlasTextureName => "WoTM.Spark.png";

        public override BlendState BlendState => BlendState.Additive;

        public MetalSparkParticle(Vector2 position, Vector2 velocity, Color[] palette, int lifetime, Vector2 scale)
        {
            Position = position;
            Velocity = velocity;
            Palette = palette;
            Scale = scale;
            Lifetime = lifetime;
            Opacity = 1f;
            Rotation = Velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override void Update()
        {
            Opacity = MathF.Pow(1f - LifetimeRatio, 0.6f);
            Velocity.X *= 0.95f;
            Velocity.Y += 0.3f;
            Rotation = Velocity.ToRotation() + MathHelper.PiOver2;

            DrawColor = Utilities.MulticolorLerp(LifetimeRatio * 0.99f, Palette);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            BloomTexture ??= AtlasManager.GetTexture("WoTM.BasicMetaballCircle.png");
            spriteBatch.Draw(BloomTexture, Position - Main.screenPosition, null, DrawColor * Opacity, Rotation, null, Scale * new Vector2(3f, 5f), 0);

            base.Draw(spriteBatch);
        }
    }
}
