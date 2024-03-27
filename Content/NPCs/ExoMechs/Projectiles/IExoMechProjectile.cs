namespace WoTM.Content.NPCs.ExoMechs.Projectiles
{
    public interface IExoMechProjectile
    {
        /// <summary>
        /// The damage of damage this Exo Mech projectile inflicts upon players.
        /// </summary>
        public ExoMechDamageSource DamageType
        {
            get;
        }
    }
}
