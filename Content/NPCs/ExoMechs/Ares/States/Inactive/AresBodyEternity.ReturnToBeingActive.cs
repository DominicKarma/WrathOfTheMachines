using Microsoft.Xna.Framework;
using Terraria;
using WoTM.Core.BehaviorOverrides;

namespace WoTM.Content.NPCs.ExoMechs.Ares
{
    public sealed partial class AresBodyEternity : NPCBehaviorOverride
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
