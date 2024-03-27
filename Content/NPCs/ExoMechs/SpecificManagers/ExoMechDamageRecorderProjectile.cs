using Terraria;
using Terraria.ModLoader;
using WoTM.Content.NPCs.ExoMechs.Projectiles;

namespace WoTM.Content.NPCs.ExoMechs
{
    public class ExoMechDamageRecorderProjectile : GlobalProjectile
    {
        public override void OnHitPlayer(Projectile projectile, Player target, Player.HurtInfo info)
        {
            if (projectile.ModProjectile is not null and IExoMechProjectile mechProj && target.TryGetModPlayer(out ExoMechDamageRecorderPlayer recorderPlayer))
                recorderPlayer.AddDamageFromSource(mechProj.DamageType, info.Damage);
        }
    }
}
