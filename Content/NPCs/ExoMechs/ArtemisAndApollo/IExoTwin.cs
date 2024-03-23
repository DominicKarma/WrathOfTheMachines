using System;
using Microsoft.Xna.Framework;

namespace DifferentExoMechs.Content.NPCs.ExoMechs
{
    public interface IExoTwin
    {
        /// <summary>
        /// The current frame for this Exo Twin.
        /// </summary>
        public int Frame
        {
            get;
            set;
        }

        /// <summary>
        /// Whether this Exo Twin has fully entered its second phase yet or not.
        /// </summary>
        /// 
        /// <remarks>
        /// In this context, "second phase" does not refer to the phases of the overall battle, instead referring to whether the Exo Twin has removed its lens and revealed its full mechanical form.
        /// </remarks>
        public bool InPhase2
        {
            get;
            set;
        }

        /// <summary>
        /// The opacity of wingtip vortices on this Exo Twin.
        /// </summary>
        public float WingtipVorticesOpacity
        {
            get;
            set;
        }

        /// <summary>
        /// The current animation of this Exo Twin.
        /// </summary>
        public ExoTwinAnimation Animation
        {
            get;
            set;
        }

        /// <summary>
        /// The palette of the optic nerve of this Exo Twin.
        /// </summary>
        public Color[] OpticNervePalette
        {
            get;
        }

        /// <summary>
        /// A specific, optionally definable draw action that may be used for specific, circumstantial effects.
        /// </summary>
        public Action? SpecificDrawAction
        {
            get;
            set;
        }

        /// <summary>
        /// Resets local data for this Exo Twin due to an AI state transition.
        /// </summary>
        public void ResetLocalStateData();
    }
}
