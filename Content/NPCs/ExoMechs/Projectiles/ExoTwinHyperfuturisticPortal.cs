using System;
using System.Collections.Generic;
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
using WoTM.Content.NPCs.ExoMechs.SpecificManagers;

namespace WoTM.Content.NPCs.ExoMechs.Projectiles
{
    public class ExoTwinHyperfuturisticPortal : ModProjectile, IProjOwnedByBoss<AresBody>, IExoMechProjectile
    {
        /// <summary>
        /// How long this portal has existed, in frames.
        /// </summary>
        public ref float Time => ref Projectile.ai[1];

        /// <summary>
        /// How long this portal should exist for, in frames.
        /// </summary>
        public static int Lifetime => LumUtils.SecondsToFrames(0.45f);

        public override string Texture => MiscTexturesRegistry.InvisiblePixelPath;

        public ExoMechDamageSource DamageType => ExoMechDamageSource.Thermal;

        public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 720;

        public override void SetDefaults()
        {
            Projectile.width = 432;
            Projectile.height = 1000;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.hide = true;
            Projectile.timeLeft = Lifetime;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Time++;
            Projectile.Opacity = LumUtils.InverseLerp(1f, 0.82f, Time / Lifetime);
            Projectile.rotation = Projectile.velocity.ToRotation();

            float fadeIn = LumUtils.InverseLerp(0f, 9f, Time);
            float fadeOut = MathF.Pow(LumUtils.InverseLerp(0f, 15f, Projectile.timeLeft), 1.6f);
            Projectile.scale = fadeIn * fadeOut;

            if (Time == 10f)
                ScreenShakeSystem.StartShakeAtPoint(Projectile.Center, 18f, shakeStrengthDissipationIncrement: 1.4f);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.PrepareForShaders();
            Vector2 size = Projectile.Size;
            ManagedShader portalShader = ShaderManager.GetShader("WoTM.HyperfuturisticPortalShader");
            portalShader.TrySetParameter("useTextureForDistanceField", false);
            portalShader.TrySetParameter("textureSize0", size);
            portalShader.TrySetParameter("scale", Projectile.scale);
            portalShader.TrySetParameter("biasToMainSwirlColorPower", 2.4f);
            portalShader.TrySetParameter("mainSwirlColor", new Vector3(0.65f, 1.56f, 0.95f));
            portalShader.TrySetParameter("secondarySwirlColor", new Vector3(0f, 2.1f, 4.2f));
            portalShader.SetTexture(MiscTexturesRegistry.DendriticNoiseZoomedOut.Value, 1, SamplerState.LinearWrap);
            portalShader.Apply();

            Texture2D pixel = MiscTexturesRegistry.Pixel.Value;
            Main.spriteBatch.Draw(pixel, drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, pixel.Size() * 0.5f, size / pixel.Size() * 1.2f, 0, 0f);

            Main.spriteBatch.ResetToDefault();

            return false;
        }

        public override bool? CanDamage() => Projectile.Opacity >= 0.6f;

        public override bool ShouldUpdatePosition() => false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            LumUtils.CircularHitboxCollision(Projectile.Center, MathF.Sqrt(Time / Lifetime) * Projectile.width * 0.52f, targetHitbox);
    }
}
