using Terraria;

namespace DifferentExoMechs.Content.NPCs.Bosses
{
    public static partial class ExoTwinsStates
    {
        /// <summary>
        /// The shared AI timer by both Artemis and Apollo.
        /// </summary>
        public static ref int AITimer => ref ExoTwinsStateManager.SharedState.AITimer;

        /// <summary>
        /// The target that Artemis and Apollo will attempt to attack.
        /// </summary>
        public static Player Target => ExoMechTargetSelector.Target;
    }
}
