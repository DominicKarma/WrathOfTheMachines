using CalamityMod.NPCs.ExoMechs.Ares;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria;

namespace WoTM.Content.NPCs.ExoMechs
{
    public sealed partial class AresBodyBehaviorOverride : NPCBehaviorOverride
    {
        public void DoBehavior_Inactive()
        {
            ZPosition = MathHelper.Clamp(ZPosition + 0.1f, -0.99f, 5f);
            NPC.SmoothFlyNear(Target.Center - Vector2.UnitY * (ZPosition * 120f + 100f), ZPosition * 0.03f, 0.87f);
            NPC.dontTakeDamage = true;
            NPC.damage = 0;

            // This is necessary to ensure that the map icon goes away.
            NPC.As<AresBody>().SecondaryAIState = (int)AresBody.SecondaryPhase.PassiveAndImmune;

            InstructionsForHands[0] = new(h => InactiveHandUpdate(h, new Vector2(-430f, 50f), 0));
            InstructionsForHands[1] = new(h => InactiveHandUpdate(h, new Vector2(-280f, 224f), 1));
            InstructionsForHands[2] = new(h => InactiveHandUpdate(h, new Vector2(280f, 224f), 2));
            InstructionsForHands[3] = new(h => InactiveHandUpdate(h, new Vector2(430f, 50f), 3));

            if (!Inactive)
            {
                CurrentState = AresAIState.ReturnToBeingActive;
                AITimer = 0;
                NPC.netUpdate = true;
            }
        }

        public void InactiveHandUpdate(AresHand hand, Vector2 hoverOffset, int armIndex)
        {
            NPC handNPC = hand.NPC;
            handNPC.Opacity = Utilities.Saturate(handNPC.Opacity + 0.025f);
            handNPC.SmoothFlyNear(NPC.Center + hoverOffset * NPC.scale, 0.7f, 0.5f);

            hand.UsesBackArm = armIndex == 0 || armIndex == ArmCount - 1;
            hand.ArmSide = (armIndex >= ArmCount / 2).ToDirectionInt();
            hand.Frame = AITimer / 3 % 12;

            hand.ArmEndpoint = Vector2.Lerp(hand.ArmEndpoint, handNPC.Center + handNPC.velocity, handNPC.Opacity * 0.3f);

            if (handNPC.Opacity <= 0f)
                hand.GlowmaskDisabilityInterpolant = 0f;
        }
    }
}
