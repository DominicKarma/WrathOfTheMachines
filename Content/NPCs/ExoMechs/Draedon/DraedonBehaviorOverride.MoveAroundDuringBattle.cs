using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;

namespace WoTM.Content.NPCs.ExoMechs
{
    public sealed partial class DraedonBehaviorOverride : NPCBehaviorOverride
    {
        /// <summary>
        /// The AI method that makes Draedon move around idly during the Exo Mechs battle.
        /// </summary>
        public void DoBehavior_MoveAroundDuringBattle()
        {
            Vector2 hoverDestination = PlayerToFollow.Center + PlayerToFollow.SafeDirectionTo(NPC.Center) * new Vector2(820f, 560f);
            NPC.SmoothFlyNearWithSlowdownRadius(hoverDestination, 0.06f, 0.9f, 60f);

            PerformStandardFraming();
        }
    }
}
