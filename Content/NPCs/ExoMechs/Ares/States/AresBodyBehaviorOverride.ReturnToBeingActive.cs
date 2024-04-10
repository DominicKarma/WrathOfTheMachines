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

            InstructionsForHands[0] = new(h => InactiveHandUpdate(h, new Vector2(-430f, 50f), 0));
            InstructionsForHands[1] = new(h => InactiveHandUpdate(h, new Vector2(-280f, 224f), 1));
            InstructionsForHands[2] = new(h => InactiveHandUpdate(h, new Vector2(280f, 224f), 2));
            InstructionsForHands[3] = new(h => InactiveHandUpdate(h, new Vector2(430f, 50f), 3));

            if (!Inactive && ZPosition <= 0f)
            {
                CurrentState = AresAIState.DetachHands;
                AITimer = 0;
                NPC.netUpdate = true;
            }
        }
    }
}
