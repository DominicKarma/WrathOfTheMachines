using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Rarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AtomSplitterItem = CalamityMod.Items.Weapons.Rogue.TheAtomSplitter;

namespace WoTM.Content.Items.TheAtomSplitter
{
    public class TheAtomSplitterReworkManager : GlobalItem
    {
        /// <summary>
        /// The base damage done by the atom splitter.
        /// </summary>
        public static int BaseDamage => 400;

        /// <summary>
        /// The starting speed of fired atom splitters.
        /// </summary>
        public static float StartingSpeed => 0.2f;

        public override void SetDefaults(Item item)
        {
            if (item.type == ModContent.ItemType<AtomSplitterItem>())
            {
                item.width = item.height = 120;
                item.damage = BaseDamage;
                item.knockBack = 7f;
                item.useAnimation = item.useTime = 37;
                item.DamageType = ModContent.GetInstance<RogueDamageClass>();
                item.autoReuse = true;
                item.shootSpeed = StartingSpeed;
                item.shoot = ModContent.ProjectileType<TheAtomSplitterProjectile_Custom>();
                item.useStyle = ItemUseStyleID.Swing;
                item.UseSound = SoundID.Item1;
                item.noMelee = true;
                item.noUseGraphic = true;
                item.value = CalamityGlobalItem.RarityVioletBuyPrice;
                item.rare = ModContent.RarityType<Violet>();
            }
        }
    }
}
