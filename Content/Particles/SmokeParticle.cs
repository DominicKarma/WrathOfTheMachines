using System;
using CalamityMod.Particles;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace DifferentExoMechs.Content.Particles
{
    public class SmokeParticle : Particle
    {
        public float ScaleGrowRate;

        public override int FrameVariants => 8;

        public override bool UseHalfTransparency => true;

        public override bool UseCustomDraw => true;

        public override string Texture => "DifferentExoMechs/Content/Particles/SmokeParticle";

        public SmokeParticle(Vector2 position, Vector2 velocity, Color color, int lifetime, float scale, float scaleGrowRate)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            Scale = scale;
            Variant = Main.rand.Next(FrameVariants);
            Lifetime = lifetime;
            ScaleGrowRate = scaleGrowRate;
        }

        public override void Update()
        {
            Rotation = Velocity.ToRotation() - MathHelper.PiOver2;
            Velocity *= 0.89f;
            Scale += LifetimeCompletion * ScaleGrowRate;
            Color = Color.Lerp(Color, Color.White, 0.05f);

            if (LifetimeCompletion >= 1f)
                Kill();
        }

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

            float opacity = Utilities.InverseLerpBump(0f, 0.02f, 0.75f, 1f, LifetimeCompletion);
            int horizontalFrame = (int)MathF.Round(MathHelper.Lerp(0f, 7f, LifetimeCompletion));
            Rectangle frame = texture.Frame(8, FrameVariants, horizontalFrame, Variant);
            spriteBatch.Draw(texture, Position - Main.screenPosition, frame, Color * opacity, Rotation, frame.Size() * 0.5f, Scale, 0, 0f);
        }
    }
}
