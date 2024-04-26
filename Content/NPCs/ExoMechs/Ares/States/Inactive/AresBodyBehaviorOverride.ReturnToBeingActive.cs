using Microsoft.Xna.Framework;
using Terraria;

namespace WoTM.Content.NPCs.ExoMechs
{
    public sealed partial class AresBodyBehaviorOverride : NPCBehaviorOverride
    {
        public void DoBehavior_ReturnToBeingActive()
        {
            ZPosition = MathHelper.Clamp(ZPosition - 0.07f, 0f, 10f);
            NPC.velocity *= 0.85f;
            NPC.dontTakeDamage = true;
            NPC.damage = 0;

            BasicHandUpdateWrapper();

            if (!Inactive && ZPosition <= 0f)
                SelectNewState();
        }
    }
}
