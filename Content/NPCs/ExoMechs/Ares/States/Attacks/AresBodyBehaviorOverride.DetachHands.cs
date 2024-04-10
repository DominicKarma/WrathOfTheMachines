using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria;

namespace WoTM.Content.NPCs.ExoMechs
{
    public sealed partial class AresBodyBehaviorOverride : NPCBehaviorOverride
    {
        /// <summary>
        /// AI update loop method for the DetachHands state.
        /// </summary>
        public void DoBehavior_DetachHands()
        {
            NPC.velocity *= new Vector2(0.5f, 0.93f);

            for (int i = 0; i < InstructionsForHands.Length; i++)
            {
                int copyForDelegate = i;
                InstructionsForHands[i] = new(h => DetachHandsUpdate(h, copyForDelegate));
            }

            if (AITimer >= 45)
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
            handNPC.velocity.X *= 0.84f;
            handNPC.velocity.Y += 0.36f;
            if (handNPC.velocity.Y < 0f)
                handNPC.velocity.Y *= 0.9f;

            hand.UsesBackArm = armIndex == 0 || armIndex == ArmCount - 1;
            hand.ArmSide = (armIndex >= ArmCount / 2).ToDirectionInt();
            hand.Frame = AITimer / 3 % 12;

            hand.ArmEndpoint = Vector2.Lerp(hand.ArmEndpoint, handNPC.Center + handNPC.velocity, handNPC.Opacity);
            hand.EnergyDrawer.chargeProgress *= 0.7f;

            if (handNPC.Opacity <= 0f)
                hand.GlowmaskDisabilityInterpolant = 0f;
        }
    }
}
