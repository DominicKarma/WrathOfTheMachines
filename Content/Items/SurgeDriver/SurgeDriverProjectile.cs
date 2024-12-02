using System;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace WoTM.Content.Items.SurgeDriver
{
    public class SurgeDriverProjectile : ModProjectile
    {
        /// <summary>
        /// The owner of gun cannon.
        /// </summary>
        public Player Owner => Main.player[Projectile.owner];

        /// <summary>
        /// The shoot timer for this cannon. Once it exceeds a certain threshold, the cannon fires.
        /// </summary>
        public ref float ShootTimer => ref Projectile.ai[0];

        public override string Texture => "CalamityMod/Items/Weapons/Ranged/SurgeDriver";

        public override void SetDefaults()
        {
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.MaxUpdates = 2;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Ranged;
        }

        public override void AI()
        {
            AimTowardsMouse();
            ManipulatePlayerValues();

            // Stay alive so long as the owner is using the item.
            if (Owner.channel)
                Projectile.timeLeft = 2;

            ShootTimer++;
            if (ShootTimer >= Owner.HeldMouseItem().useAnimation * Projectile.MaxUpdates)
            {
                Vector2 blastSpawnPosition = Projectile.Center + Projectile.velocity * Projectile.scale * 132f;
                if (Main.myPlayer == Projectile.owner)
                {
                    int lifetime = 23;
                    float hue = Main.rand.NextFloat(0.023f, 0.081f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), blastSpawnPosition, Projectile.velocity, ModContent.ProjectileType<SurgeDriverBlast>(), Projectile.damage, Projectile.knockBack, Projectile.whoAmI, lifetime, hue);
                }

                ShootTimer = 0f;
                Projectile.netUpdate = true;
            }
        }

        /// <summary>
        /// Manipulates player hold variables and ensures that this cannon stays attached to the owner.
        /// </summary>
        public void ManipulatePlayerValues()
        {
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Owner.heldProj = Projectile.whoAmI;
            Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.velocity.ToRotation() - MathHelper.PiOver2);

            Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter);
        }

        /// <summary>
        /// Makes this cannon aim towards the mouse.
        /// </summary>
        public void AimTowardsMouse()
        {
            if (Main.myPlayer == Projectile.owner)
            {
                Vector2 idealDirection = Projectile.SafeDirectionTo(Main.MouseWorld);
                Vector2 newDirection = Vector2.Lerp(Projectile.velocity, idealDirection, 0.12f).SafeNormalize(Vector2.Zero);
                if (Projectile.velocity != newDirection)
                {
                    Projectile.velocity = newDirection;
                    Projectile.netUpdate = true;
                    Projectile.netSpam = 0;
                }
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            float rotation = Projectile.rotation;
            SpriteEffects direction = SpriteEffects.None;
            Vector2 origin = new(0.57f, 0.75f);
            if (MathF.Cos(rotation) < 0f)
            {
                origin.X = 1f - origin.X;
                rotation += MathHelper.Pi;
                direction = SpriteEffects.FlipHorizontally;
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(Color.White), rotation, texture.Size() * origin, Projectile.scale, direction);

            return false;
        }

        public override bool? CanDamage() => false;
    }
}
