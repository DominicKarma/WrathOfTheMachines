namespace WoTM.Content.NPCs.ExoMechs
{
    /// <summary>
    /// A representation of the state of the <i>overall</i> Exo Mechs fight, holding all of the Exo Mech NPC data.
    /// </summary>
    /// <param name="InitialMechState">The state of the initial Exo Mech, aka the one that the player chose to the start the fight with.</param>
    /// <param name="OtherMechsStates">The state of all Exo Mechs other than the initial one.</param>
    public record ExoMechFightState(ExoMechState InitialMechState, params ExoMechState[] OtherMechsStates)
    {
        /// <summary>
        /// A representation of an undefined fight state, where all marks are represented as <see cref="ExoMechState.UndefinedExoMechState"/>.
        /// </summary>
        public static readonly ExoMechFightState UndefinedFightState = new(ExoMechState.UndefinedExoMechState);
    }
}
