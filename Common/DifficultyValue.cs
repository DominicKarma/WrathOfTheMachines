using CalamityMod.World;
using Newtonsoft.Json;

namespace WoTM.Common
{
    // I would ordinarily make this all readonly, but that causes problems with JSON deserialization.

    /// <summary>
    /// Represents a value that should vary based on the three main-line modes: E-Rev, E-Death, and Masomode.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public struct DifficultyValue<TValue> where TValue : struct
    {
        /// <summary>
        /// The value that should be selected as a base.
        /// </summary>
        public TValue BaseValue;

        /// <summary>
        /// The value that should be selected in Revengeance mode.
        /// </summary>
        public TValue RevValue;

        /// <summary>
        /// The value that should be selected in Death Mode.
        /// </summary>
        public TValue DeathValue;

        /// <summary>
        /// The value that should be selected
        /// </summary>
        [JsonIgnore]
        public readonly TValue Value
        {
            get
            {
                if (CalamityWorld.death)
                    return DeathValue;
                if (CalamityWorld.revenge)
                    return RevValue;

                return BaseValue;
            }
        }

        public static implicit operator TValue(DifficultyValue<TValue> difficultyValue) => difficultyValue.Value;
    }
}
