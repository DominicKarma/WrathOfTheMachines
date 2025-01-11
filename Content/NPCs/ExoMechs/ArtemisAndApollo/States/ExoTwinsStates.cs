using Terraria;
using WoTM.Content.NPCs.ExoMechs.ArtemisAndApollo;
using WoTM.Content.NPCs.ExoMechs.ArtemisAndApollo.Common;
using WoTM.Content.NPCs.ExoMechs.FightManagers;

namespace WoTM.Content.NPCs.ExoMechs.ArtemisAndApollo
{
    public static partial class ExoTwinsStates
    {
        // Just a useful shorthand so that ExoTwinsStateManager doesn't have to be typed out each time when working with state data in this class.
        private static SharedExoTwinState SharedState => ExoTwinsStateManager.SharedState;

        /// <summary>
        /// The shared AI timer by both Artemis and Apollo.
        /// </summary>
        public static ref int AITimer => ref SharedState.AITimer;

        /// <summary>
        /// The target that Artemis and Apollo will attempt to attack.
        /// </summary>
        public static Player Target => ExoMechTargetSelector.Target;
    }
}
