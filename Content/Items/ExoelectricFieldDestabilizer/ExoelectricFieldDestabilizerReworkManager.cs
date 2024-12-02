using CalamityMod.Items;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace WoTM.Content.Items.ExoelectricFieldDestabilizer
{
    public class ExoelectricFieldDestabilizerReworkManager : GlobalItem, IItemRename
    {
        public int OverrideID => ModContent.ItemType<TheJailor>();

        public LocalizedText? DisplayName => Language.GetText("Mods.WoTM.Items.ExoelectricFieldDestabilizer.DisplayName");

        public LocalizedText? Tooltip => Language.GetText("Mods.WoTM.Items.ExoelectricFieldDestabilizer.Tooltip");

        /// <summary>
        /// The base damage done by the cannon.
        /// </summary>
        public static int BaseDamage => 856;

        public override void SetDefaults(Item item)
        {
            if (item.type == OverrideID)
            {
                item.width = 102;
                item.height = 70;
                item.damage = BaseDamage;
                item.DamageType = DamageClass.Ranged;
                item.useTime = item.useAnimation = 28;
                item.useStyle = ItemUseStyleID.Shoot;
                item.noMelee = true;
                item.knockBack = 8f;
                item.value = CalamityGlobalItem.RarityVioletBuyPrice;
                item.rare = ModContent.RarityType<Violet>();
                item.autoReuse = true;
                item.noUseGraphic = true;
                item.channel = true;
                item.shoot = ModContent.ProjectileType<ExoelectricFieldDestabilizerProjectile>();
                item.shootSpeed = 20f;
                item.UseSound = null;
                item.useAmmo = AmmoID.Bullet;
            }
        }

        public override bool CanConsumeAmmo(Item weapon, Item ammo, Player player)
        {
            // Ensure that the cannon doesn't consume ammo when creating the cannon projectile.
            if (weapon.type == OverrideID)
                return player.ownedProjectileCounts[weapon.shoot] >= 1;

            return base.CanConsumeAmmo(weapon, ammo, player);
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (item.type == OverrideID)
            {
                Projectile.NewProjectile(source, position, velocity.SafeNormalize(Vector2.Zero), item.shoot, damage, knockback, player.whoAmI);
                return false;
            }

            return base.Shoot(item, player, source, position, velocity, type, damage, knockback);
        }
    }
}
