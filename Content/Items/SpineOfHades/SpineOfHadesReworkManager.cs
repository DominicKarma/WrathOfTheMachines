using CalamityMod.Items;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using WoTM.Content.Items.RefractionRotor;

namespace WoTM.Content.Items.SpinOfHades
{
    public class SpineOfHadesReworkManager : GlobalItem
    {
        /// <summary>
        /// The base damage done by the spin.
        /// </summary>
        public static int BaseDamage => 372;

        public override void SetDefaults(Item item)
        {
            if (item.type == ModContent.ItemType<SpineOfThanatos>())
            {
                item.width = item.height = 28;
                item.damage = BaseDamage;
                item.knockBack = 8f;
                item.useAnimation = item.useTime = 25;
                item.autoReuse = true;
                item.shootSpeed = 1f;
                item.shoot = ModContent.ProjectileType<SpineOfHadesProjectile_Custom>();

                item.useStyle = ItemUseStyleID.Swing;
                item.UseSound = SoundID.Item1;
                item.noMelee = true;
                item.noUseGraphic = true;
                item.value = CalamityGlobalItem.RarityVioletBuyPrice;
                item.rare = ModContent.RarityType<Violet>();
            }
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (item.type == ModContent.ItemType<SpineOfThanatos>())
            {
                Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, velocity.ToRotation(), 0f);
                return false;
            }

            return base.Shoot(item, player, source, position, velocity, type, damage, knockback);
        }
    }
}
