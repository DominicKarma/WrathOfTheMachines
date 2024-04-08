using System.Collections.Generic;
using CalamityMod.NPCs.ExoMechs;
using Luminance.Common.Utilities;
using Terraria;
using Terraria.ModLoader;

namespace WoTM.Content.NPCs.ExoMechs
{
    public sealed class ExoMechFightStateManager : ModSystem
    {
        /// <summary>
        /// The set of all previously summoned Exo Mechs throughout the fight. Used to keep track of which Exo Mechs existed in the past, after they are killed.
        /// </summary>
        internal static List<int> PreviouslySummonedMechIDs = [];

        /// <summary>
        /// The set of all defined Exo Mech phases.
        /// </summary>
        internal static List<PhaseDefinition> ExoMechPhases = [];

        /// <summary>
        /// Whether any Exo Mechs are currently present in the world.
        /// </summary>
        public static bool AnyExoMechsPresent
        {
            get;
            private set;
        }

        /// <summary>
        /// The current phase of the Exo Mechs fight, as calculated via the <see cref="PreUpdateEntities"/> hook in this system every frame.
        /// </summary>
        public static PhaseDefinition CurrentPhase
        {
            get;
            private set;
        }

        /// <summary>
        /// The overall state of the Exo Mechs fight, as calculated via the <see cref="PreUpdateEntities"/> hook in this system every frame.
        /// </summary>
        public static ExoMechFightState FightState
        {
            get;
            private set;
        }

        /// <summary>
        /// A representation of an undefined Exo Mech phase transition condition that always evaluates false regardless of context.
        /// </summary>
        /// 
        /// <remarks>
        /// This is primarily used in the context of a fallback when the fight is not ongoing at all.
        /// </remarks>
        public static readonly PhaseTransitionCondition UndefinedPhaseTransitionCondition = new(_ => false);

        /// <summary>
        /// The definition of an Exo Mech phase, defined by its ordering in the fight and overall condition.
        /// </summary>
        /// <param name="PhaseOrdering">The ordering of the phase definition. This governs when this phase should be entered, relative to all other phases.</param>
        /// <param name="FightState">The state of the overall Exo Mechs fight.</param>
        /// <param name="FightIsHappening">Whether the fight is actually happening or not.</param>
        public record PhaseDefinition(int PhaseOrdering, bool FightIsHappening, PhaseTransitionCondition FightState);

        /// <summary>
        /// Represents a condition by which a phase transition should occur.
        /// </summary>
        /// <param name="fightState">The state of the overall Exo Mechs fight.</param>
        public delegate bool PhaseTransitionCondition(ExoMechFightState fightState);

        public override void PreUpdateEntities()
        {
            DetermineBattleState();
        }

        /// <summary>
        /// Evaluates the overall fight state, storing the result in the <see cref="FightState"/> member.
        /// </summary>
        /// 
        /// <remarks>
        /// This method should only be called when the fight is ongoing.
        /// </remarks>
        private static void CalculateFightState()
        {
            List<int> evaluatedMechs = [];
            foreach (int exoMechID in ExoMechNPCIDs.ExoMechIDs)
                evaluatedMechs.Add(exoMechID);

            if (TryFindPrimaryMech(out NPC? primaryMech))
                evaluatedMechs.Remove(primaryMech!.type);

            ExoMechState[] stateOfOtherExoMechs = new ExoMechState[evaluatedMechs.Count];
            for (int i = 0; i < evaluatedMechs.Count; i++)
            {
                int otherExoMechIndex = NPC.FindFirstNPC(evaluatedMechs[i]);
                bool exoMechWasSummonedAtOnePoint = PreviouslySummonedMechIDs.Contains(evaluatedMechs[i]);
                NPC? otherExoMech = Main.npc.IndexInRange(otherExoMechIndex) ? Main.npc[otherExoMechIndex] : null;

                stateOfOtherExoMechs[i] = ExoMechStateFromNPC(otherExoMech, exoMechWasSummonedAtOnePoint);
            }

            FightState = new(ExoMechStateFromNPC(primaryMech, true), stateOfOtherExoMechs);

            AnyExoMechsPresent = true;
        }

        // The wasSummoned parameter is necessary because it's sometimes not possible to check PreviouslySummonedMechIDs in this method, due to the NPC itself being null.
        /// <summary>
        /// Converts an NPC into an <see cref="ExoMechState"/>.
        /// </summary>
        /// 
        /// <remarks>
        /// This method gracefully accepts null or inactive NPCs, counting them as having been killed and having 0 health.
        /// </remarks>
        /// <param name="exoMech">The Exo Mech NPC.</param>
        /// <param name="wasSummoned">Whether the Exo Mech being checked was summoned previously or not.</param>
        private static ExoMechState ExoMechStateFromNPC(NPC? exoMech, bool wasSummoned)
        {
            // Yes, I know. Null safety operators. I prefer it like this, due to the fact that the
            // expression that requires null safety uses a Not operation.
            // '!(exoMech?.active ?? false)' just feels unnecessarily dense.
            if (exoMech is null || !exoMech.active)
                return new(0f, wasSummoned, true);

            return new(Utilities.Saturate(exoMech.life / (float)exoMech.lifeMax), true, false);
        }

        /// <summary>
        /// Evaluates the overall battle state, keeping track of the current phase and list of Exo Mechs that have been summoned throughout the battle.
        /// </summary>
        private static void DetermineBattleState()
        {
            bool hadesIsPresent = NPC.AnyNPCs(ExoMechNPCIDs.HadesHeadID);
            bool aresIsPresent = NPC.AnyNPCs(ExoMechNPCIDs.AresBodyID);
            bool artemisAndApolloArePresent = NPC.AnyNPCs(ExoMechNPCIDs.ArtemisID) || NPC.AnyNPCs(ExoMechNPCIDs.ApolloID);
            bool fightIsOngoing = hadesIsPresent || aresIsPresent || artemisAndApolloArePresent;
            if (!fightIsOngoing)
            {
                ResetBattleState();
                return;
            }

            RecordPreviouslySummonedMechs();
            CalculateFightState();
        }

        /// <summary>
        /// Resets various battle state variables in this class due to the Exo Mechs battle not happening.
        /// </summary>
        private static void ResetBattleState()
        {
            if (PreviouslySummonedMechIDs.Count >= 1)
                PreviouslySummonedMechIDs.Clear();

            AnyExoMechsPresent = false;
            CurrentPhase = new(0, false, UndefinedPhaseTransitionCondition);
            FightState = ExoMechFightState.UndefinedFightState;
            ExoTwinsStateManager.SharedState.ResetForEntireBattle();

            if (Main.LocalPlayer.TryGetModPlayer(out ExoMechDamageRecorderPlayer recorderPlayer) && !NPC.AnyNPCs(ModContent.NPCType<Draedon>()))
                recorderPlayer.ResetIncurredDamage();
        }

        /// <summary>
        /// Records all Exo Mechs that were previously summoned in the <see cref="PreviouslySummonedMechIDs"/> registry.
        /// </summary>
        private static void RecordPreviouslySummonedMechs()
        {
            foreach (int exoMechID in ExoMechNPCIDs.ExoMechIDs)
            {
                if (!PreviouslySummonedMechIDs.Contains(exoMechID) && NPC.AnyNPCs(exoMechID))
                    PreviouslySummonedMechIDs.Add(exoMechID);
            }
        }

        /// <summary>
        /// Attempts to find the mech that the player initially chose at the start of the fight.
        /// </summary>
        /// <param name="primaryMech">The primary mech that was found. Is null if it was not found.</param>
        private static bool TryFindPrimaryMech(out NPC? primaryMech)
        {
            primaryMech = null;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || !IsPrimaryMech(npc))
                    continue;

                primaryMech = npc;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Evaluates whether an NPC is a primary Exo Mech or not, based on whether it's a managing Exo Mech and has the appropriate AI flag.
        /// </summary>
        /// <param name="npc">The NPC to evaluate.</param>
        public static bool IsPrimaryMech(NPC npc) => ExoMechNPCIDs.IsManagingExoMech(npc) && npc.ai[3] == 1f;

        /// <summary>
        /// Marks an NPC as the primary mech.
        /// </summary>
        /// 
        /// <remarks>
        /// As a sanity check, this method will not do anything if <paramref name="npc"/> is not a managing Exo Mech.
        /// </remarks>
        /// <param name="npc">The NPC to try to turn into a primary mech.</param>
        public static void TryToMakePrimaryMech(NPC npc)
        {
            if (!ExoMechNPCIDs.IsManagingExoMech(npc))
                return;

            npc.ai[3] = 1f;
            npc.netUpdate = true;
        }
    }
}
