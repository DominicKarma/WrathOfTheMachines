using static WoTM.Content.NPCs.ExoMechs.ExoMechFightStateManager;

namespace WoTM.Content.NPCs.ExoMechs
{
    public static class ExoMechFightDefinitions
    {
        /// <summary>
        /// The first phase definition.
        /// </summary>
        /// 
        /// <remarks>
        /// During this phase, the player fights the first chosen Exo Mech until it reaches a given HP percentage.
        /// </remarks>
        public static readonly PhaseDefinition StartingSoloPhaseDefinition = CreateNewPhase(1, state => true);

        /// <summary>
        /// The second phase definition.
        /// </summary>
        /// 
        /// <remarks>
        /// During this phase the player fights the two mechs that aren't the initial mech until one of them reaches 70%.
        /// </remarks>
        public static readonly PhaseDefinition StartingTwoAtOncePhaseDefinition = CreateNewPhase(2, state =>
        {
            return state.InitialMechState.LifeRatio <= SummonOtherMechsLifeRatio;
        });

        /// <summary>
        /// The third phase definition.
        /// </summary>
        /// 
        /// <remarks>
        /// During this phase the player fights all three mechs at once, until one of them reaches 40%.
        /// </remarks>
        public static readonly PhaseDefinition MechaMayhemPhaseDefinition = CreateNewPhase(3, state =>
        {
            for (int i = 0; i < state.OtherMechsStates.Length; i++)
            {
                var otherMechState = state.OtherMechsStates[i];
                if (otherMechState.HasBeenSummoned && otherMechState.LifeRatio <= SummonOtherMechsLifeRatio)
                    return true;
            }

            return false;
        });

        /// <summary>
        /// The fourth phase definition.
        /// </summary>
        /// 
        /// <remarks>
        /// During this phase the player fights the Exo Mech they brought down to 40% until it is killed.
        /// </remarks>
        public static readonly PhaseDefinition FirstSoloUntilDeadPhaseDefinition = CreateNewPhase(4, state =>
        {
            for (int i = 0; i < state.OtherMechsStates.Length; i++)
            {
                var otherMechState = state.OtherMechsStates[i];
                if (otherMechState.HasBeenSummoned && otherMechState.LifeRatio <= FightAloneLifeRatio)
                    return true;
            }

            return state.InitialMechState.LifeRatio <= FightAloneLifeRatio;
        });

        /// <summary>
        /// The fifth phase definition.
        /// </summary>
        /// 
        /// <remarks>
        /// During this phase the player fights the two remaining Exo Mechs until one of them reaches 40%.
        /// </remarks>
        public static readonly PhaseDefinition SecondTwoAtOncePhaseDefinition = CreateNewPhase(5, state =>
        {
            return state.TotalKilledMechs >= 1;
        });

        /// <summary>
        /// The sixth phase definition.
        /// </summary>
        /// 
        /// <remarks>
        /// During this phase the player fights the Exo Mech they brought down to 40% until it dies.
        /// </remarks>
        public static readonly PhaseDefinition SecondToLastSoloPhaseDefinition = CreateNewPhase(6, state =>
        {
            for (int i = 0; i < state.OtherMechsStates.Length; i++)
            {
                var otherMechState = state.OtherMechsStates[i];
                if (otherMechState.HasBeenSummoned && !otherMechState.Killed && otherMechState.LifeRatio <= FightAloneLifeRatio)
                    return true;
            }

            return !state.InitialMechState.Killed && state.InitialMechState.LifeRatio <= FightAloneLifeRatio;
        });

        /// <summary>
        /// The seventh and final phase definition.
        /// </summary>
        /// 
        /// <remarks>
        /// During this phase the player fights the final Exo Mech until it dies.
        /// </remarks>
        public static readonly PhaseDefinition BerserkSoloPhaseDefinition = CreateNewPhase(7, state =>
        {
            return state.TotalKilledMechs >= 2;
        });

        // NOTE -- Update XML comments if these are changed.
        public static float SummonOtherMechsLifeRatio => 0.7f;

        public static float FightAloneLifeRatio => 0.4f;
    }
}
