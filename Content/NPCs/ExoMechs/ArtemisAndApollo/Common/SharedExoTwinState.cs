﻿using System.IO;

namespace WoTM.Content.NPCs.ExoMechs
{
    /// <summary>
    /// A representation of a shared collection of state variables, that both Artemis and Apollo access for their attacks.
    /// </summary>
    public class SharedExoTwinState
    {
        /// <summary>
        /// The AI timer shared by Artemis and Apollo.
        /// </summary>
        public int AITimer;

        /// <summary>
        /// How many attacks Artemis and Apollo has performed.
        /// </summary>
        public int TotalFinishedAttacks;

        /// <summary>
        /// The current state that both Artemis and Apollo are performing.
        /// </summary>
        public ExoTwinsAIState AIState
        {
            get;
            set;
        }

        /// <summary>
        /// Arbitrary numbers that are state-specific and shared mutually by Artemis and Apollo.
        /// </summary>
        public float[] Values
        {
            get;
            private set;
        }

        public SharedExoTwinState(ExoTwinsAIState state, float[] stateNumbers)
        {
            AIState = state;
            Values = stateNumbers;
        }

        /// <summary>
        /// Updates this state.
        /// </summary>
        public void Update() => AITimer++;

        /// <summary>
        /// Resets all mutable data for this state in anticipation of a different one.
        /// </summary>
        public void ResetForNextState()
        {
            AITimer = 0;
            TotalFinishedAttacks++;
            for (int i = 0; i < Values.Length; i++)
                Values[i] = 0f;
        }

        /// <summary>
        /// Resets all mutable data for this state as a consequence of the battle finishing.
        /// </summary>
        public void ResetForEntireBattle()
        {
            TotalFinishedAttacks = 0;
            AIState = ExoTwinsAIState.SpawnAnimation;
            ResetForNextState();
        }

        /// <summary>
        /// Writes this state to a <see cref="BinaryWriter"/> for the purposes of being sent across the network.
        /// </summary>
        /// <param name="writer">The binary writer.</param>
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write((int)AIState);
            writer.Write(Values.Length);
            for (int i = 0; i < Values.Length; i++)
                writer.Write(Values[i]);
        }

        /// <summary>
        /// Reads a from a <see cref="BinaryReader"/> for the purposes of being received from across the network.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        public void ReadFrom(BinaryReader reader)
        {
            AIState = (ExoTwinsAIState)reader.ReadInt32();
            int totalStates = reader.ReadInt32();

            Values = new float[totalStates];
            for (int i = 0; i < Values.Length; i++)
                Values[i] = reader.ReadSingle();
        }
    }
}
