using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria;

namespace WoTM.Content.NPCs.ExoMechs
{
    public sealed partial class AresBodyBehaviorOverride : NPCBehaviorOverride
    {
        public void DoBehavior_DetachHands()
        {
            NPC.velocity *= new Vector2(0.5f, 0.93f);

            for (int i = 0; i < InstructionsForHands.Length; i++)
            {
                int copyForDelegate = i;
                InstructionsForHands[i] = new(h => DetachHandsUpdate(h, copyForDelegate));
            }

            if (AITimer >= 40)
            {
                CurrentState = Main.rand.NextBool() ? AresAIState.AimedLaserBursts : AresAIState.NukeAoEAndPlasmaBlasts;
                AITimer = 0;
                NPC.netUpdate = true;
            }
        }

        public void DetachHandsUpdate(AresHand hand, int armIndex)
        {
            NPC handNPC = hand.NPC;
            handNPC.Opacity = Utilities.Saturate(handNPC.Opacity - 0.025f);
            handNPC.velocity.X *= 0.92f;
            handNPC.velocity.Y += 0.36f;

            hand.UsesBackArm = armIndex == 0 || armIndex == ArmCount - 1;
            hand.ArmSide = (armIndex >= ArmCount / 2).ToDirectionInt();
            hand.Frame = AITimer / 3 % 12;

            hand.ArmEndpoint = Vector2.Lerp(hand.ArmEndpoint, handNPC.Center + handNPC.velocity, handNPC.Opacity * 0.3f);

            if (handNPC.Opacity <= 0f)
                hand.GlowmaskDisabilityInterpolant = 0f;
        }
    }
}
