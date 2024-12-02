using CalamityMod.Items;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using SurgeDriverClass = CalamityMod.Items.Weapons.Ranged.SurgeDriver;

namespace WoTM.Content.Items.SurgeDriver
{
    public class SurgeDriverReworkManager : GlobalItem
    {
        /// <summary>
        /// The base damage done by the gun.
        /// </summary>
        public static int BaseDamage => 408;

        public override void SetDefaults(Item item)
        {
            if (item.type == ModContent.ItemType<SurgeDriverClass>())
            {
                item.width = 164;
                item.height = 58;
                item.damage = BaseDamage;
                item.DamageType = DamageClass.Ranged;
                item.useTime = item.useAnimation = 10;
                item.useStyle = ItemUseStyleID.Shoot;
                item.noMelee = true;
                item.channel = true;
                item.knockBack = 9f;
                item.value = CalamityGlobalItem.RarityVioletBuyPrice;
                item.rare = ModContent.RarityType<Violet>();
                item.autoReuse = true;
                item.shoot = ModContent.ProjectileType<SurgeDriverProjectile>();
                item.shootSpeed = 11f;
                item.useAmmo = AmmoID.Bullet;
            }
        }

        public override bool CanConsumeAmmo(Item weapon, Item ammo, Player player)
        {
            // Ensure that the gun doesn't consume ammo when creating the gun projectile.
            if (weapon.type == ModContent.ItemType<SurgeDriverClass>())
                return player.ownedProjectileCounts[weapon.shoot] >= 1;

            return base.CanConsumeAmmo(weapon, ammo, player);
        }

        public override bool CanRightClick(Item item)
        {
            // Remove the right click functionality.
            if (item.type == ModContent.ItemType<SurgeDriverClass>())
                return false;

            return base.CanRightClick(item);
        }

        public override bool AltFunctionUse(Item item, Player player)
        {
            // Remove the right click functionality.
            if (item.type == ModContent.ItemType<SurgeDriverClass>())
                return false;

            return base.AltFunctionUse(item, player);
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (item.type == ModContent.ItemType<SurgeDriverClass>())
            {
                Projectile.NewProjectile(source, position, velocity.SafeNormalize(Vector2.Zero), item.shoot, damage, knockback, player.whoAmI);
                return false;
            }

            return base.Shoot(item, player, source, position, velocity, type, damage, knockback);
        }
    }
}
