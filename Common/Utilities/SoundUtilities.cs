using ReLogic.Utilities;
using Terraria.Audio;

namespace WoTM.Common.Utilities
{
    public static class SoundUtilities
    {
        // Because apparently SoundStyle.Volume doesn't allow for going past a certain threshold...
        /// <summary>
        /// Modifies the volume of a played sound slot instance.
        /// </summary>
        /// <param name="soundSlot">The sound slot to affect.</param>
        /// <param name="volumeFactor">The volume modifier factor.</param>
        public static SlotId WithVolumeBoost(this SlotId soundSlot, float volumeFactor)
        {
            if (SoundEngine.TryGetActiveSound(soundSlot, out ActiveSound? sound) && sound is not null)
                sound.Volume *= volumeFactor;

            return soundSlot;
        }
    }
}
