using System;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ModLoader;
using WoTM.Content.NPCs.ExoMechs.Projectiles;

namespace WoTM.Content.Items.ExoelectricFieldDestabilizer
{
    public class ExoelectricFieldDestabilizerProjectile : ModProjectile
    {
        /// <summary>
        /// The squish scaling factor of this cannon.
        /// </summary>
        public Vector2 Squish
        {
            get;
            set;
        } = Vector2.One;

        /// <summary>
        /// The owner of this cannon.
        /// </summary>
        public Player Owner => Main.player[Projectile.owner];

        /// <summary>
        /// The shoot timer for this cannon. Once it exceeds a certain threshold, the cannon fires.
        /// </summary>
        public ref float ShootTimer => ref Projectile.ai[0];

        public override string Texture => "CalamityMod/Items/Weapons/Ranged/TheJailor";

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

            // Summon a tesla sphere for the player on the first frame.
            if (Main.myPlayer == Projectile.owner && ShootTimer == 1f)
            {
                Vector2 orbVelocity = Projectile.velocity * 30f;
                if (Vector2.Dot(Owner.velocity, orbVelocity) > 0f)
                    orbVelocity += Owner.velocity * 5f;

                if (Owner.PickAmmo(Owner.HeldMouseItem(), out _, out _, out int damage, out float knockback, out _))
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, orbVelocity, ModContent.ProjectileType<ExoelectricFieldDestabilizerOrb>(), damage, knockback, Projectile.owner);
                    ScreenShakeSystem.StartShakeAtPoint(Owner.Center, 4f);

                    // Apply recoil to the cannon.
                    Projectile.velocity = Projectile.velocity.RotatedBy(Projectile.velocity.X.NonZeroSign() * -1.04f);
                    Projectile.netUpdate = true;
                }
                else
                    Projectile.Kill();
            }

            if (ShootTimer >= Owner.HeldMouseItem().useAnimation * Projectile.MaxUpdates)
            {
                SoundEngine.PlaySound(TeslaCannon.FireSound with { MaxInstances = 0, Volume = 0.7f }, Projectile.Center);

                ShootTimer = 0f;
                Projectile.netUpdate = true;

                int orbID = ModContent.ProjectileType<ExoelectricFieldDestabilizerOrb>();
                foreach (Projectile orb in Main.ActiveProjectiles)
                {
                    if (orb.owner == Projectile.owner && orb.ai[0] == 0f && orb.type == orbID)
                    {
                        orb.As<ExoelectricFieldDestabilizerOrb>().Time = 5f;
                        orb.As<ExoelectricFieldDestabilizerOrb>().Launched = true;
                        orb.velocity = Projectile.velocity * 10f;
                        orb.netUpdate = true;
                    }
                }
            }

            float squishTime = MathHelper.TwoPi * ShootTimer / 13.5f;
            float squishX = Utilities.Cos01(squishTime) * 0.15f;
            float squishY = Utilities.Cos01(squishTime * 1.2f) * 0.1f;
            Squish = new Vector2(1f + squishX, 1f + squishY);
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

        /// <summary>
        /// Creates electric arcs at the end of the cannon.
        /// </summary>
        public void CreateElectricity()
        {
            float electricityCreationChance = Utilities.InverseLerp(0f, 30f, ShootTimer).Squared();
            if (Main.myPlayer == Projectile.owner && Main.rand.NextBool(electricityCreationChance))
            {
                Vector2 endOfCannon = Projectile.Center + Projectile.velocity * Projectile.scale * Squish * 50f;
                Vector2 arcLength = Projectile.velocity.RotatedByRandom(1.04f) * Main.rand.NextFloat(300f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), endOfCannon, arcLength, ModContent.ProjectileType<SmallTeslaArc>(), 0, 0f, Projectile.owner, Main.rand.Next(8, 13));
            }
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

            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(Color.White), rotation, texture.Size() * origin, Projectile.scale * Squish, direction);

            return false;
        }

        public override bool? CanDamage() => false;
    }
}
