using System;
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
    public class GaussNukeBoom : ModProjectile, IProjOwnedByBoss<AresBody>, IExoMechProjectile
    {
        /// <summary>
        /// How long this sphere has existed, in frames.
        /// </summary>
        public ref float Time => ref Projectile.ai[0];

        /// <summary>
        /// How long the explosion lasts.
        /// </summary>
        public static int Lifetime => Utilities.SecondsToFrames(2.1f);

        public override string Texture => MiscTexturesRegistry.InvisiblePixelPath;

        public ExoMechDamageSource DamageType => ExoMechDamageSource.Thermal;

        public override void SetDefaults()
        {
            Projectile.width = (int)AresBodyBehaviorOverride.NukeAoEAndPlasmaBlasts_NukeExplosionDiameter;
            Projectile.height = (int)AresBodyBehaviorOverride.NukeAoEAndPlasmaBlasts_NukeExplosionDiameter;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = Lifetime;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Time++;
            Projectile.Opacity = Utilities.InverseLerp(1f, 0.78f, Time / Lifetime);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.PrepareForShaders();

            float lifetimeRatio = Time / Lifetime;
            ManagedShader shader = ShaderManager.GetShader("WoTM.GaussNukeExplosionShader");
            shader.SetTexture(MiscTexturesRegistry.DendriticNoiseZoomedOut.Value, 1, SamplerState.LinearWrap);
            shader.TrySetParameter("lifetimeRatio", lifetimeRatio);
            shader.TrySetParameter("textureSize0", Projectile.Size);
            shader.Apply();

            Color color = Projectile.GetAlpha(new Color(1f, 0.54f, 0.09f));
            Texture2D pixel = MiscTexturesRegistry.Pixel.Value;
            Main.spriteBatch.Draw(pixel, drawPosition, null, color, 0f, pixel.Size() * 0.5f, Projectile.Size * Projectile.scale / pixel.Size() * 1.2f, 0, 0f);

            Main.spriteBatch.ResetToDefault();

            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            Utilities.CircularHitboxCollision(Projectile.Center, MathF.Sqrt(Time / Lifetime) * Projectile.width * 0.47f, targetHitbox) && Projectile.Opacity >= 0.6f;
    }
}
