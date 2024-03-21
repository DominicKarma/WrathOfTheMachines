using System.IO;
using CalamityMod.NPCs;
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
                case ExoTwinsAIState.EnterSecondPhase:
                    ExoTwinsStates.DoBehavior_EnterSecondPhase(twin, twinAttributes);
                    break;
            }
        }
    }
}
