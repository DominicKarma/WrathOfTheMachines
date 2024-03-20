using CalamityMod.NPCs.ExoMechs;
using Luminance.Common.Utilities;
using Terraria.Audio;

namespace DifferentExoMechs.Content.NPCs.ExoMechs
{
    public sealed partial class DraedonBehaviorOverride : NPCBehaviorOverride
    {
        /// <summary>
        /// How long Draedon spends appearing as a hologram.
        /// </summary>
        public static int HologramAppearTime => Utilities.SecondsToFrames(1f);

        /// <summary>
        /// The AI method that makes Draedon appear as a hologram in front of the player.
        /// </summary>
        public void DoBehavior_AppearAsHologram()
        {
            if (AITimer == 1f)
                SoundEngine.PlaySound(Draedon.TeleportSound, PlayerToFollow.Center);

            HologramInterpolant = Utilities.InverseLerp(HologramAppearTime, 0f, AITimer);

            if (AITimer >= HologramAppearTime)
                ChangeAIState(DraedonAIState.StartingMonologue);
        }
    }
}
