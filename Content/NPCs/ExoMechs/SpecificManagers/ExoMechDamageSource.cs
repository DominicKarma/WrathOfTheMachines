namespace WoTM.Content.NPCs.ExoMechs
{
    /// <summary>
    /// Represents a type of damage that the Exo Mechs and their projectiles can inflict upon the player.
    /// </summary>
    public enum ExoMechDamageSource
    {
        // These names correspond to a localization identifier. If you change one, you must change the other.
        Thermal,
        Plasma,
        Electricity,
        Magnetism,
        Internal,
        BluntForceTrauma,

        // Used solely for iteration.
        Count
    }
}
