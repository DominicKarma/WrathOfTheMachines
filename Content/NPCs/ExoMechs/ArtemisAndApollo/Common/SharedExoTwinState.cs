using System.IO;

namespace DifferentExoMechs.Content.NPCs.Bosses
{
    /// <summary>
    /// A representation of a shared collection of state variables, that both Artemis and Apollo access for their attacks.
    /// </summary>
    public class SharedExoTwinState
    {
        /// <summary>
        /// The current state that both Artemis and Apollo are performing.
        /// </summary>
        public ExoTwinsAIState AIState
        {
            get;
            private set;
        }

        public float[] StateNumbers
        {
            get;
            private set;
        }

        public SharedExoTwinState(ExoTwinsAIState state, float[] stateNumbers)
        {
            AIState = state;
            StateNumbers = stateNumbers;
        }

        /// <summary>
        /// Writes this state to a <see cref="BinaryWriter"/> for the purposes of being sent across the network.
        /// </summary>
        /// <param name="writer">The binary writer.</param>
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(StateNumbers.Length);
            for (int i = 0; i < StateNumbers.Length; i++)
                writer.Write(StateNumbers[i]);
        }

        /// <summary>
        /// Reads a from a <see cref="BinaryReader"/> for the purposes of being received from across the network.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        public void ReadFrom(BinaryReader reader)
        {
            AIState = (ExoTwinsAIState)reader.ReadInt32();

            StateNumbers = new float[reader.ReadInt32()];
            for (int i = 0; i < StateNumbers.Length; i++)
                StateNumbers[i] = reader.ReadSingle();
        }
    }
}
