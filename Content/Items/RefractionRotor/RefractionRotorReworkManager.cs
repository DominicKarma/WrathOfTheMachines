using CalamityMod.Items;
using CalamityMod.Rarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using RefractionRotorItem = CalamityMod.Items.Weapons.Rogue.RefractionRotor;

namespace WoTM.Content.Items.RefractionRotor
{
    public class RefractionRotorReworkManager : GlobalItem
    {
        /// <summary>
        /// The base damage done by the refraction rotor.
        /// </summary>
        public static int BaseDamage => 170;

        /// <summary>
        /// The amount of updates that the refraction rotor performs each frame.
        /// </summary>
        public static int MaxUpdates => 20;

        /// <summary>
        /// The speed that the refraction rotor travels at after decelerating.
        /// </summary>
        public static float SlowedSpeed => 4f;

        /// <summary>
        /// The starting speed of fired refraction rotors.
        /// </summary>
        public static float StartingRotorSpeed => 69f;

        /// <summary>
        /// The amount by which fired refraction rotors should slow down by default when ready.
        /// </summary>
        public static float BaseDecelerationFactor => 0.987f;

        /// <summary>
        /// The amount by which fired refraction rotors should slow down when grinding a target.
        /// </summary>
        public static float GrindingDecelerationFactor => 0.93f;

        public override void SetDefaults(Item item)
        {
            if (item.type == ModContent.ItemType<RefractionRotorItem>())
            {
                item.width = item.height = 120;
                item.damage = BaseDamage;
                item.knockBack = 6f;
                item.useAnimation = item.useTime = 4;
                item.autoReuse = true;
                item.shootSpeed = StartingRotorSpeed / MaxUpdates;
                item.shoot = ModContent.ProjectileType<RefractionRotorProjectile_Custom>();

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
