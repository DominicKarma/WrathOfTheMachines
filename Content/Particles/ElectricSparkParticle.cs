using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;

namespace DifferentExoMechs.Content.Particles
{
    public class ElectricSparkParticle : Particle
    {
        public override int FrameVariants => 4;

        public override bool UseAdditiveBlend => true;

        public override bool UseCustomDraw => false;

        public override string Texture => "DifferentExoMechs/Content/Particles/ElectricSparkParticle";

        public ElectricSparkParticle(Vector2 position, Vector2 velocity, Color color, int lifetime, float scale)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            Scale = scale;
            Variant = Main.rand.Next(FrameVariants);
            Lifetime = lifetime;
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        public override void Update()
        {
            if (Time >= Lifetime - 6)
                Color *= 0.9f;

            Velocity *= 0.94f;
            Rotation += Velocity.X * 0.12f;

            if (LifetimeCompletion >= 1f)
                Kill();
        }
    }
}
