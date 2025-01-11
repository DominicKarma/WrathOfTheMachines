using System;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using WoTM;

namespace FargowiltasCrossmod.Content.Calamity.Bosses.ExoMechs.Hades
{
    public sealed partial class HadesHeadEternity : NPCBehaviorOverride
    {
        /// <summary>
        /// AI update loop method for the inactive state.
        /// </summary>
        public void DoBehavior_Inactive()
        {
            BodyBehaviorAction = new(AllSegments(), CloseSegment());
            DisableMapIcon = true;

            Vector2 hoverDestination = Target.Center + new Vector2(MathF.Cos(MathHelper.TwoPi * AITimer / 150f) * 600f, 4200f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, NPC.SafeDirectionTo(hoverDestination) * 50f, 0.05f);

            SegmentReorientationStrength = 0.05f;

            NPC.damage = 0;
            NPC.rotation = NPC.velocity.ToRotation() + MathHelper.PiOver2;
        }
    }
}
