using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace WoTM.Content.Particles
{
    public class BloomPixelParticle : Particle
    {
        public Color BloomColor;

        public static AtlasTexture BloomTexture
        {
            get;
            private set;
        }

        public override string AtlasTextureName => "WoTM.Pixel.png";

        public override BlendState BlendState => BlendState.Additive;

        public BloomPixelParticle(Vector2 position, Vector2 velocity, Color color, Color bloomColor, int lifetime, Vector2 scale)
        {
            Position = position;
            Velocity = velocity;
            DrawColor = color;
            BloomColor = bloomColor;
            Scale = scale;
            Lifetime = lifetime;
            Opacity = 1f;
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        public override void Update()
        {
            if (Time >= Lifetime * 0.6f)
            {
                Opacity *= 0.91f;
                Scale *= 0.96f;
                Velocity *= 0.94f;
            }

            Velocity *= 0.96f;
            Rotation += Velocity.X * 0.07f;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            BloomTexture ??= AtlasManager.GetTexture("WoTM.BasicMetaballCircle.png");
            spriteBatch.Draw(BloomTexture, Position - Main.screenPosition, null, BloomColor * Opacity, 0f, null, Scale * 0.19f, 0);
            base.Draw(spriteBatch);
        }
    }
}
