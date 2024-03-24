using System.IO;
using System.Linq;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace DifferentExoMechs.Content.NPCs.ExoMechs
{
    public class ExoTwinsStateManager : ModSystem
    {
        /// <summary>
        /// A shared state that both Artemis and Apollo may access.
        /// </summary>
        public static SharedExoTwinState SharedState
        {
            get;
            set;
        } = new(ExoTwinsAIState.DashesAndLasers, new float[5]);

        /// <summary>
        /// The set of all passive individual AI states the Exo Twins can perform.
        /// </summary>
        public static ExoTwinsIndividualAIState[] PassiveIndividualStates => [ExoTwinsIndividualAIState.Artemis_Passive, ExoTwinsIndividualAIState.Artemis_Passive];

        /// <summary>
        /// The set of all active individual AI states the Exo Twins can perform.
        /// </summary>
        public static ExoTwinsIndividualAIState[] ActiveIndividualStates => [ExoTwinsIndividualAIState.Artemis_Active, ExoTwinsIndividualAIState.Apollo_LoopDashBombardment];

        /// <summary>
        /// The set of all active individual AI states that Artemis can perform.
        /// </summary>
        public static ExoTwinsIndividualAIState[] IndividualArtemisStates => [ExoTwinsIndividualAIState.Artemis_Passive, ExoTwinsIndividualAIState.Artemis_Active];

        /// <summary>
        /// The set of all active individual AI states that Apollo can perform.
        /// </summary>
        public static ExoTwinsIndividualAIState[] IndividualApolloStates => [ExoTwinsIndividualAIState.Apollo_Passive, ExoTwinsIndividualAIState.Apollo_LoopDashBombardment];

        public override void PostUpdateNPCs()
        {
            SharedState.Update();

            if (CalamityGlobalNPC.draedonExoMechTwinGreen != -1)
            {
                NPC apollo = Main.npc[CalamityGlobalNPC.draedonExoMechTwinGreen];
                if (apollo.active && apollo.TryGetBehavior(out ApolloBehaviorOverride apolloAI))
                    PerformUpdateLoop(apollo, apolloAI);
            }

            if (CalamityGlobalNPC.draedonExoMechTwinRed != -1)
            {
                NPC artemis = Main.npc[CalamityGlobalNPC.draedonExoMechTwinRed];
                if (artemis.active && artemis.TryGetBehavior(out ArtemisBehaviorOverride artemisAI))
                    PerformUpdateLoop(artemis, artemisAI);
            }
        }

        public override void NetSend(BinaryWriter writer) => SharedState.WriteTo(writer);

        public override void NetReceive(BinaryReader reader) => SharedState.ReadFrom(reader);

        /// <summary>
        /// Performs the central AI state update loop for a given Exo Twin.
        /// </summary>
        /// <param name="twin">The Exo Twin's NPC instance.</param>
        /// <param name="twinAttributes">The Exo Twin's designated generic attributes.</param>
        public static void PerformUpdateLoop(NPC twin, IExoTwin twinAttributes)
        {
            twin.damage = 0;
            twin.defense = twin.defDefense;
            twin.dontTakeDamage = false;

            switch (SharedState.AIState)
            {
                case ExoTwinsAIState.SpawnAnimation:
                    ExoTwinsStates.DoBehavior_SpawnAnimation(twin, twinAttributes);
                    break;
                case ExoTwinsAIState.DashesAndLasers:
                    ExoTwinsStates.DoBehavior_DashesAndLasers(twin, twinAttributes);
                    break;
                case ExoTwinsAIState.PerformIndividualAttacks:
                    PerformIndividualizedAttacks(twin, twinAttributes);
                    break;

                case ExoTwinsAIState.EnterSecondPhase:
                    ExoTwinsStates.DoBehavior_EnterSecondPhase(twin, twinAttributes);
                    break;
            }
        }

        /// <summary>
        /// Performs the central AI state update loop for a given Exo Twin.
        /// </summary>
        /// 
        /// <remarks>
        /// State transitions will be performed based on whichever Exo Twin is performing an active attack rather than a passive one.
        /// </remarks>
        /// 
        /// <param name="twin">The Exo Twin's NPC instance.</param>
        /// <param name="twinAttributes">The Exo Twin's designated generic attributes.</param>
        public static void PerformIndividualizedAttacks(NPC twin, IExoTwin twinAttributes)
        {
            switch (twinAttributes.IndividualState.AIState)
            {
                case ExoTwinsIndividualAIState.Apollo_LoopDashBombardment:
                    ExoTwinsStates.DoBehavior_LoopDashBombardment(twin, twinAttributes, ref twinAttributes.IndividualState.AITimer);
                    break;
            }
            twinAttributes.IndividualState.AITimer++;
        }

        /// <summary>
        /// Picks an attack that Artemis and Apollo should adhere to.
        /// </summary>
        public static ExoTwinsAIState MakeAIStateChoice()
        {
            Player target = ExoMechTargetSelector.Target;
            NPC artemis = Main.npc[CalamityGlobalNPC.draedonExoMechTwinRed];
            NPC apollo = Main.npc[CalamityGlobalNPC.draedonExoMechTwinGreen];
            Vector2 twinsCenterOfMass = (artemis.Center + apollo.Center) * 0.5f;
            if (target.WithinRange(twinsCenterOfMass, 600f) && Main.rand.NextBool())
                return ExoTwinsAIState.DashesAndLasers;

            return ExoTwinsAIState.PerformIndividualAttacks;
        }

        /// <summary>
        /// Picks a set of two individualized AI states for Artemis and Apollo, at random, with there being one passive attack and one active one.
        /// </summary>
        public static void PickIndividualAIStates()
        {
            bool apolloWillPerformActiveState = Main.rand.NextBool();
            ExoTwinsIndividualAIState apolloState;
            ExoTwinsIndividualAIState artemisState;

            if (apolloWillPerformActiveState)
            {
                var activeApolloStates = IndividualApolloStates.Where(ActiveIndividualStates.Contains);
                apolloState = Main.rand.Next(activeApolloStates.ToList());

                var passiveArtemisStates = IndividualArtemisStates.Where(PassiveIndividualStates.Contains);
                artemisState = Main.rand.Next(passiveArtemisStates.ToList());
            }
            else
            {
                var activeArtemisStates = IndividualArtemisStates.Where(ActiveIndividualStates.Contains);
                apolloState = Main.rand.Next(activeArtemisStates.ToList());

                var passiveApolloStates = IndividualApolloStates.Where(PassiveIndividualStates.Contains);
                artemisState = Main.rand.Next(passiveApolloStates.ToList());
            }

            if (CalamityGlobalNPC.draedonExoMechTwinRed != -1 && Main.npc[CalamityGlobalNPC.draedonExoMechTwinRed].TryGetBehavior(out ArtemisBehaviorOverride artemis))
            {
                artemis.IndividualState.AITimer = 0;
                artemis.IndividualState.AIState = artemisState;
                artemis.NPC.netUpdate = true;
            }
            if (CalamityGlobalNPC.draedonExoMechTwinGreen != -1 && Main.npc[CalamityGlobalNPC.draedonExoMechTwinGreen].TryGetBehavior(out ArtemisBehaviorOverride apollo))
            {
                apollo.IndividualState.AITimer = 0;
                apollo.IndividualState.AIState = apolloState;
                apollo.NPC.netUpdate = true;
            }
        }

        /// <summary>
        /// Selects and uses a new AI state for the Exo Twins.
        /// </summary>
        public static void TransitionToNextState(ExoTwinsAIState? stateToUse = null)
        {
            SharedState.Reset();
            SharedState.AIState = stateToUse ?? MakeAIStateChoice();

            if (CalamityGlobalNPC.draedonExoMechTwinRed != -1 && Main.npc[CalamityGlobalNPC.draedonExoMechTwinRed].TryGetBehavior(out ArtemisBehaviorOverride artemis))
                artemis.ResetLocalStateData();

            if (CalamityGlobalNPC.draedonExoMechTwinGreen != -1 && Main.npc[CalamityGlobalNPC.draedonExoMechTwinGreen].TryGetBehavior(out ArtemisBehaviorOverride apollo))
                apollo.ResetLocalStateData();

            if (SharedState.AIState == ExoTwinsAIState.PerformIndividualAttacks)
                PickIndividualAIStates();
        }
    }
}
