using CalamityMod;
using CalamityMod.World;
using Microsoft.Xna.Framework;

namespace WoTM.Content.NPCs.ExoMechs
{
    public sealed partial class DraedonBehaviorOverride : NPCBehaviorOverride
    {
        /// <summary>
        /// The monologue that Draedon should use at the start of the battle. Once he's been spoken to, his dialogue is a lot lighter.
        /// </summary>
        public static DraedonDialogueChain StartingMonologueToUse => CalamityWorld.TalkedToDraedon ? StartingMonologueBrief : StartingMonologue;

        /// <summary>
        /// The AI method that makes Draedon speak to the player before the battle.
        /// </summary>
        public void DoBehavior_StartingMonologue()
        {
            int speakTimer = (int)AITimer - 90;
            var monologue = StartingMonologueToUse;
            for (int i = 0; i < monologue.Count; i++)
            {
                if (speakTimer == monologue[i].SpeakDelay)
                    monologue[i].SayInChat();
            }

            bool monologueIsFinished = speakTimer >= monologue.OverallDuration;
            bool playerHasSelectedExoMech = CalamityWorld.DraedonMechToSummon != ExoMech.None;
            if (monologueIsFinished)
            {
                if (!playerHasSelectedExoMech)
                    PlayerToFollow.Calamity().AbleToSelectExoMech = true;
                else
                    ChangeAIState(DraedonAIState.ExoMechSpawnAnimation);

                // Mark Draedon as talked to.
                if (!CalamityWorld.TalkedToDraedon)
                {
                    CalamityWorld.TalkedToDraedon = true;
                    CalamityNetcode.SyncWorld();
                }
            }

            if (Frame <= 10f)
            {
                if (FrameTimer % 7f == 6f)
                {
                    Frame++;
                    FrameTimer = 0f;
                }
            }
            else
                Frame = (int)MathHelper.Lerp(11f, 15f, FrameTimer / 30f % 1f);
        }
    }
}
