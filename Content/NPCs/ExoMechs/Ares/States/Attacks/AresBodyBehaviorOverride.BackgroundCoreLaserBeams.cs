using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;

namespace WoTM.Content.NPCs.ExoMechs
{
    public sealed partial class AresBodyBehaviorOverride : NPCBehaviorOverride
    {
        /// <summary>
        /// AI update loop method for the BackgroundCoreLaserBeams attack.
        /// </summary>
        public void DoBehavior_BackgroundCoreLaserBeams()
        {
            float enterBackgroundInterpolant = Utilities.InverseLerp(0f, 30f, AITimer);
            float slowDownInterpolant = Utilities.InverseLerp(54f, 60f, AITimer);
            ZPosition = enterBackgroundInterpolant * 3.7f;
            NPC.Center = Vector2.Lerp(NPC.Center, Target.Center - Vector2.UnitY * enterBackgroundInterpolant * 360f, enterBackgroundInterpolant * (1f - slowDownInterpolant) * 0.08f);
            NPC.velocity *= 0.93f;

            BasicHandUpdateWrapper();
        }
    }
}
