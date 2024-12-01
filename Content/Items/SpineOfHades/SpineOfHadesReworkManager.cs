using CalamityMod.Items;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace WoTM.Content.Items.SpineOfHades
{
    public class SpineOfHadesReworkManager : GlobalItem, IItemRename
    {
        public int OverrideID => ModContent.ItemType<SpineOfThanatos>();

        public LocalizedText? DisplayName => Language.GetText("Mods.WoTM.Items.SpineOfHades.DisplayName");

        public LocalizedText? Tooltip => Language.GetText("Mods.WoTM.Items.SpineOfHades.Tooltip");

        /// <summary>
        /// The base damage done by the spine.
        /// </summary>
        public static int BaseDamage => 572;

        /// <summary>
        /// How far the spine reaches when it's being used like a whip.
        /// </summary>
        public static float WhipReach => 660f;

        /// <summary>
        /// How far the spine reaches when it's lunged forward.
        /// </summary>
        public static float LungeReach => 800f;

        /// <summary>
        /// The angular arc created by the spine when it's whipping.
        /// </summary>
        public static float SwingOffsetArc => 1.24f;

        /// <summary>
        /// The color that the spine approaches when supercharged.
        /// </summary>
        public static Vector4 SuperchargeColor => new Color(255, 14, 20).ToVector4() * 2f;

        public override void SetDefaults(Item item)
        {
            if (item.type == OverrideID)
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
            if (item.type == OverrideID)
            {
                Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, velocity.ToRotation(), 0f);
                return false;
            }

            return base.Shoot(item, player, source, position, velocity, type, damage, knockback);
        }
    }
}
