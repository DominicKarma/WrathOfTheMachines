using CalamityMod;
using CalamityMod.NPCs.ExoMechs.Artemis;
using Luminance.Common.DataStructures;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DifferentExoMechs.Content.NPCs.ExoMechs.Projectiles
{
    public class ArtemisLaserImproved : ModProjectile, IProjOwnedByBoss<Artemis>
    {
        /// <summary>
        /// How long this laser has existed, in frames.
        /// </summary>
        public ref float Time => ref Projectile.ai[0];

        /// <summary>
        /// The amount of max updates this laser has.
        /// </summary>
        public static int TotalUpdates => 4;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.MaxUpdates = TotalUpdates;
            Projectile.timeLeft = Projectile.MaxUpdates * 300;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.frame = (int)Time / TotalUpdates / 5 % Main.projFrames[Type];

            if (Time >= TotalUpdates * 35f)
                Projectile.velocity *= 1.0189f;

            Time++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            Utilities.CircularHitboxCollision(projHitbox.Center(), Projectile.width * 0.4f, targetHitbox);

        public override bool PreDraw(ref Color lightColor)
        {
            int trailingMode = ProjectileID.Sets.TrailingMode[Type];
            Utilities.DrawAfterimagesCentered(Projectile, trailingMode, Color.White, positionClumpInterpolant: 0.25f);
            return false;
        }
    }
}
