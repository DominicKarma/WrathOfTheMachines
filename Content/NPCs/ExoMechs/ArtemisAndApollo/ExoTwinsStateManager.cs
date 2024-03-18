using System.IO;
using CalamityMod.NPCs;
using Terraria;
using Terraria.ModLoader;

namespace DifferentExoMechs.Content.NPCs.Bosses
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
        }

        public override void PostUpdateNPCs()
        {
            if (CalamityGlobalNPC.draedonExoMechTwinGreen != -1)
            {
                NPC apollo = Main.npc[CalamityGlobalNPC.draedonExoMechTwinGreen];
                if (apollo.active && apollo.type == ExoMechNPCIDs.ApolloID)
                    PerformUpdateLoop(apollo);
            }

            if (CalamityGlobalNPC.draedonExoMechTwinRed != -1)
            {
                NPC artemis = Main.npc[CalamityGlobalNPC.draedonExoMechTwinRed];
                if (artemis.active && artemis.type == ExoMechNPCIDs.ArtemisID)
                    PerformUpdateLoop(artemis);
            }
        }

        public override void NetSend(BinaryWriter writer) => SharedState.WriteTo(writer);

        public override void NetReceive(BinaryReader reader) => SharedState.ReadFrom(reader);

        /// <summary>
        /// Performs the central AI state update loop for a given Exo Twin.
        /// </summary>
        /// <param name="twin">The Exo Twin's NPC instance.</param>
        public static void PerformUpdateLoop(NPC twin)
        {
            twin.damage = twin.defDamage;
            twin.defense = twin.defDefense;
            twin.dontTakeDamage = false;

            switch (SharedState.AIState)
            {
                case ExoTwinsAIState.TestDashes:
                    ExoTwinsStates.DoBehavior_TestDashes(twin);
                    break;
            }
        }
    }
}
