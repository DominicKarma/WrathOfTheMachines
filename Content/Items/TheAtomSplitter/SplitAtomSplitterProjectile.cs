using System.IO;
using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Melee;
using Luminance.Assets;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace WoTM.Content.Items.TheAtomSplitter
{
    public class SplitAtomSplitterProjectile : ModProjectile
    {
        /// <summary>
        /// The position of the portal that this spear will exit from.
        /// </summary>
        public Vector2 StartingPortalPosition
        {
            get;
            set;
        }

        /// <summary>
        /// The position of the portal that this spear will enter into.
        /// </summary>
        public Vector2 EndingPortalPosition
        {
            get;
            set;
        }

        /// <summary>
        /// Whether this spear has played the portal enter sound.
        /// </summary>
        public bool HasPlayedPortalEnterSound
        {
            get;
            set;
        }

        /// <summary>
        /// How long this spear has existed for, in frames.
        /// </summary>
        public ref float Time => ref Projectile.ai[0];

        /// <summary>
        /// How long this spear should exist before dying.
        /// </summary>
        public static int Lifetime => 72;

        public override string Texture => "CalamityMod/Projectiles/Rogue/TheAtomSplitterDuplicate";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 3700;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 42;
        }

        public override void SetDefaults()
        {
            Projectile.width = 120;
            Projectile.height = 120;
            Projectile.scale = Main.rand?.NextFloat(0.5f, 0.9f) ?? 0.5f;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.MaxUpdates = 2;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 5;
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(StartingPortalPosition);
            writer.WriteVector2(EndingPortalPosition);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            StartingPortalPosition = reader.ReadVector2();
            EndingPortalPosition = reader.ReadVector2();
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Projectile.velocity *= 1.1f;

            if (StartingPortalPosition == Vector2.Zero)
            {
                StartingPortalPosition = Projectile.Center;
                EndingPortalPosition = StartingPortalPosition + Projectile.velocity.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(250f, 350f);
            }

            if (Projectile.WithinRange(EndingPortalPosition, 100f))
                Projectile.damage = 0;

            Time++;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Color impactColor = Utilities.MulticolorLerp(Main.rand.NextFloat(), Color.Aqua, Color.Lime, Color.Yellow);
            StrongBloom impact = new(target.Center, Vector2.Zero, impactColor, 0.96f, 9);
            GeneralParticleHandler.SpawnParticle(impact);

            StrongBloom heatCore = new(target.Center, Vector2.Zero, Color.Wheat, 0.6f, 9);
            GeneralParticleHandler.SpawnParticle(heatCore);

            if (Main.myPlayer == Projectile.owner)
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Projectile.velocity.SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<ExobeamSlash>(), 0, 0f, Projectile.owner);
        }

        /// <summary>
        /// Renders this spear.
        /// </summary>
        public void RenderSpear()
        {
            Vector2 closerPortalPosition = EndingPortalPosition;
            Vector2 portalDirection = -Projectile.velocity.SafeNormalize(Vector2.Zero);
            if (Projectile.Distance(StartingPortalPosition) < Projectile.Distance(EndingPortalPosition))
            {
                closerPortalPosition = StartingPortalPosition;
                portalDirection *= -1f;
            }

            ManagedShader shader = ShaderManager.GetShader("WoTM.AtomSplitterSpearShader");
            shader.TrySetParameter("blurInterpolant", 0f);
            shader.TrySetParameter("blurDirection", new Vector2(0.707f, -0.707f));
            shader.TrySetParameter("portalPosition", Vector2.Transform(closerPortalPosition - Main.screenPosition, Main.GameViewMatrix.TransformationMatrix));
            shader.TrySetParameter("portalDirection", portalDirection);
            shader.Apply();

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0);
        }

        /// <summary>
        /// Renders an instance of this spear's portals.
        /// </summary>
        public void RenderPortal(Vector2 portalPosition, float portalScale, float rotation)
        {
            Vector2 portalSize = new Vector2(150f, 420f) * MathHelper.Lerp(0.96f, 1.04f, Utilities.Cos01(Main.GlobalTimeWrappedHourly * 31f + Projectile.identity * 3f)) * Projectile.scale;
            Vector2 drawPosition = portalPosition - Main.screenPosition;

            if (portalScale <= 0f)
                return;

            Color color = Main.hslToRgb(Projectile.identity / 13f % 0.297f + 0.2f, 1f, 0.6f);
            ManagedShader portalShader = ShaderManager.GetShader("WoTM.AtomSplitterPortalShader");
            portalShader.TrySetParameter("useTextureForDistanceField", false);
            portalShader.TrySetParameter("textureSize0", portalSize * 2f);
            portalShader.TrySetParameter("scale", portalScale);
            portalShader.TrySetParameter("biasToMainSwirlColorPower", MathHelper.Lerp(1.5f, 4.2f, Projectile.identity / 7f % 1f));
            portalShader.TrySetParameter("mainSwirlColor", color.ToVector3() * 3.5f);
            portalShader.TrySetParameter("secondarySwirlColor", color.HueShift(-0.15f) * 4f);
            portalShader.SetTexture(MiscTexturesRegistry.DendriticNoiseZoomedOut.Value, 1, SamplerState.LinearWrap);
            portalShader.Apply();

            Texture2D pixel = MiscTexturesRegistry.Pixel.Value;
            Main.spriteBatch.Draw(pixel, drawPosition, null, Projectile.GetAlpha(Color.White), rotation, pixel.Size() * 0.5f, portalSize / pixel.Size() * 1.2f, 0, 0f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.PrepareForShaders();
            RenderPortal(StartingPortalPosition, Utilities.InverseLerpBump(0f, 0.2f, 0.25f, 0.5f, Time / Lifetime), Projectile.velocity.ToRotation());
            RenderPortal(EndingPortalPosition, Utilities.InverseLerpBump(0f, 0.2f, 0.3f, 1f, Time / Lifetime), Projectile.velocity.ToRotation() + MathHelper.Pi);
            RenderSpear();
            Main.spriteBatch.ResetToDefault();

            return false;
        }
    }
}
