using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WoTM.Content.Items.TheAtomSplitter
{
    public class TheAtomSplitterProjectile_Custom : ModProjectile
    {
        /// <summary>
        /// How long this spear has existed for, in frames.
        /// </summary>
        public ref float Time => ref Projectile.ai[0];

        public override string Texture => "CalamityMod/Items/Weapons/Rogue/TheAtomSplitter";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 42;
        }

        public override void SetDefaults()
        {
            Projectile.width = 120;
            Projectile.height = 120;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.MaxUpdates = 1;
            Projectile.timeLeft = Projectile.MaxUpdates * 90;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = Projectile.MaxUpdates * 4;
            Projectile.Opacity = 0f;
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
        }

        public override void AI()
        {
            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return true;
        }
    }
}
