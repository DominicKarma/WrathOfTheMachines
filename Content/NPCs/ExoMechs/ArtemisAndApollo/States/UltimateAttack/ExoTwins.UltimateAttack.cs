using Luminance.Common.Utilities;
using Terraria;

namespace WoTM.Content.NPCs.ExoMechs
{
    public static partial class ExoTwinsStates
    {
        /// <summary>
        /// How long Artemis spends entering the background during the ultimate attack.
        /// </summary>
        public static int UltimateAttack_EnterBackgroundTime => Utilities.SecondsToFrames(1.1f);

        /// <summary>
        /// AI update loop method for the ultimate attack.
        /// </summary>
        /// <param name="npc">The Exo Twin's NPC instance.</param>
        /// <param name="twinAttributes">The Exo Twin's designated generic attributes.</param>
        public static void DoBehavior_UltimateAttack(NPC npc, IExoTwin twinAttributes)
        {
            bool isApollo = npc.type == ExoMechNPCIDs.ApolloID;

            if (isApollo)
            {
                npc.velocity *= 0.9f;
                return;
            }
        }

        /// <summary>
        /// AI update loop method for Artemis during the ultimate attack.
        /// </summary>
        /// <param name="npc">Artemis' NPC instance.</param>
        /// <param name="twinAttributes">Artemis' designated generic attributes.</param>
        public static void DoBehavior_UltimateAttack_Artemis(NPC npc, IExoTwin artemisAttributes)
        {
            if (!npc.TryGetBehavior(out ArtemisBehaviorOverride artemis))
            {
                npc.active = false;
                return;
            }

            float enterBackgroundInterpolant = Utilities.InverseLerp(0f, UltimateAttack_EnterBackgroundTime, AITimer);
            artemis.ZPosition = enterBackgroundInterpolant;
        }
    }
}
