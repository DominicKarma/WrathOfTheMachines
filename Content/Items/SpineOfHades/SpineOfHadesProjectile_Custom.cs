using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WoTM.Content.Items.RefractionRotor
{
    public class SpineOfHadesProjectile_Custom : ModProjectile
    {
        /// <summary>
        /// The set of all points upon which this whip's segments exist.
        /// </summary>
        public List<Vector2> WhipPoints = new(64);

        public ref float StartingDirection => ref Projectile.ai[0];

        /// <summary>
        /// How long this spine has existed for, in frames.
        /// </summary>
        public ref float Time => ref Projectile.ai[2];

        public override string Texture => "CalamityMod/Projectiles/Melee/SpineOfThanatosProjectile";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20;
        }

        public override void SetDefaults()
        {
            Projectile.width = 62;
            Projectile.height = 62;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Projectile.MaxUpdates * 45;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = Projectile.MaxUpdates * 10;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.ownerHitCheck = true;
        }

        public override void AI()
        {
            Time++;
        }

        /// <summary>
        /// Calculates the set of whip points for this spine.
        /// </summary>
        public void CalculateWhipPoints()
        {
            WhipPoints.Clear();

            Vector2[] controls = new Vector2[16];
        }

        public override bool PreDraw(ref Color lightColor)
        {
            for (int i = 0; i < WhipPoints.Count - 1; i++)
            {
                string whipTexturePath;
                if (i == WhipPoints.Count - 2)
                    whipTexturePath = Texture;
                else if (i == 0)
                    whipTexturePath = "CalamityMod/Projectiles/Melee/SpineOfThanatosTail";
                else
                    whipTexturePath = $"CalamityMod/Projectiles/Melee/SpineOfThanatosBody{i % 2 + 1}";
                Texture2D whipSegmentTexture = ModContent.Request<Texture2D>(whipTexturePath).Value;
                Texture2D whipSegmentGlowmaskTexture = ModContent.Request<Texture2D>($"{whipTexturePath}Glowmask").Value;

                Vector2 origin = whipSegmentTexture.Size() * 0.5f;
                float rotation = (WhipPoints[i + 1] - WhipPoints[i]).ToRotation() + MathHelper.PiOver2;
                Vector2 drawPosition = WhipPoints[i] - Main.screenPosition;
                Color color = Projectile.GetAlpha(Lighting.GetColor((int)WhipPoints[i].X / 16, (int)WhipPoints[i].Y / 16));
                Main.EntitySpriteDraw(whipSegmentTexture, drawPosition, null, color, rotation, origin, Projectile.scale, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(whipSegmentGlowmaskTexture, drawPosition, null, Color.White, rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }
            return false;
        }
    }
}
