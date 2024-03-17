using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace DifferentExoMechs.Content.Particles
{
    public class ElectricSparkParticle : Particle
    {
        public override int FrameCount => 4;

        public override string AtlasTextureName => "DifferentExoMechs.ElectricSparkParticle.png";

        public override BlendState BlendState => BlendState.Additive;

        public ElectricSparkParticle(Vector2 position, Vector2 velocity, Color color, int lifetime, Vector2 scale)
        {
            Position = position;
            Velocity = velocity;
            DrawColor = color;
            Scale = scale;
            Lifetime = lifetime;
            Frame = new(0, Main.rand.Next(FrameCount) * 125, 125, 125);
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        public override void Update()
        {
            if (Time >= Lifetime - 5)
                Opacity *= 0.9f;

            Velocity *= 0.89f;
            Rotation += Velocity.X * 0.12f;
        }
    }
}
