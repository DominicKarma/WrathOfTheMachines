using CalamityMod;
using Luminance.Assets;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using WoTM.Content.NPCs.ExoMechs.Projectiles;

namespace WoTM.Content.Items.TheAtomSplitter
{
    public class AtomSplitterSpamSource : ModProjectile
    {
        /// <summary>
        /// The preferredt target index for this source.
        /// </summary>
        public int PreferredTargetIndex => (int)Projectile.ai[0];

        /// <summary>
        /// How long this source for exist for, in frames.
        /// </summary>
        public static int Lifetime => 36;

        /// <summary>
        /// The rate at which split spears can be summoned by this source.
        /// </summary>
        public static int SplitSummonRate => 6;

        /// <summary>
        /// How far this source can search for a target.
        /// </summary>
        public static float TargetingRange => 3500f;

        public override string Texture => MiscTexturesRegistry.InvisiblePixelPath;

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
        }

        public override void AI()
        {
            if (Projectile.timeLeft % SplitSummonRate == 0)
            {
                NPC? potentialTarget = Projectile.FindTargetWithinRange(TargetingRange);
                if (PreferredTargetIndex != -1 && Main.npc[PreferredTargetIndex].CanBeChasedBy())
                    potentialTarget = Main.npc[PreferredTargetIndex];

                if (potentialTarget is not null)
                {
                    Vector2 spearSpawnPosition = potentialTarget.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(140f, 265f);

                    SoundEngine.PlaySound(TheAtomSplitterProjectile_Custom.WarpSound with { MaxInstances = 0, Volume = 0.1f }, spearSpawnPosition);
                    if (Main.myPlayer == Projectile.owner)
                    {
                        Vector2 spearVelocity = spearSpawnPosition.SafeDirectionTo(potentialTarget.Center) * 20f;
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), spearSpawnPosition, spearVelocity, ModContent.ProjectileType<SplitAtomSplitterProjectile>(), Projectile.damage / 2, 0f, Projectile.owner);

                        for (int i = 0; i < 2; i++)
                        {
                            Vector2 arcLength = spearVelocity.SafeNormalize(Vector2.Zero).RotatedByRandom(0.6f) * Main.rand.NextFloat(60f, 220f);
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), spearSpawnPosition, arcLength, ModContent.ProjectileType<SmallTeslaArc>(), Projectile.damage / 2, 0f, Projectile.owner, 10);
                        }
                    }
                }
            }
        }

        public override bool? CanDamage() => false;
    }
}
