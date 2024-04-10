using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WoTM.Content.NPCs.ExoMechs.Projectiles;

namespace WoTM.Content.NPCs.ExoMechs
{
    public sealed partial class DraedonBehaviorOverride : NPCBehaviorOverride
    {
        /// <summary>
        /// The monologue that Draedon uses upon the Exo Mechs battle concluding.
        /// </summary>
        public static readonly DraedonDialogueChain PostBattleInterjectionInterjection = new DraedonDialogueChain("Mods.WoTM.NPCs.Draedon.").
            Add("EndOfBattle_FirstDefeat1").
            Add("EndOfBattle_FirstDefeat2").
            Add("EndOfBattle_FirstDefeat3").
            Add("EndOfBattle_FirstDefeat4").
            Add("EndOfBattle_FirstDefeat5").
            Add("EndOfBattle_FirstDefeat6");

        /// <summary>
        /// The AI method that makes Draedon speak to the player after an Exo Mech has been defeated.
        /// </summary>
        public void DoBehavior_PostBattleInterjection()
        {
            int speakTimer = (int)AITimer - 90;
            var monologue = PostBattleInterjectionInterjection;
            for (int i = 0; i < monologue.Count; i++)
            {
                if (speakTimer == monologue[i].SpeakDelay)
                    monologue[i].SayInChat();
            }

            Vector2 hoverDestination = PlayerToFollow.Center + new Vector2((PlayerToFollow.Center.X - NPC.Center.X).NonZeroSign() * -450f, -5f);
            NPC.SmoothFlyNear(hoverDestination, 0.05f, 0.94f);

            bool monologueIsFinished = speakTimer >= monologue.OverallDuration;

            // Reset the variables to their controls by healing the player.
            if (Main.netMode != NetmodeID.MultiplayerClient && speakTimer == monologue[3].SpeakDelay - 60)
            {
                foreach (Player player in Main.ActivePlayers)
                {
                    if (player.WithinRange(NPC.Center, 6700f))
                        Utilities.NewProjectileBetter(NPC.GetSource_FromAI(), player.Center - Vector2.UnitY * 800f, Vector2.Zero, ModContent.ProjectileType<DraedonLootCrate>(), 0, 0f, player.whoAmI);
                }
            }

            if (monologueIsFinished)
            {
                HologramInterpolant = Utilities.Saturate(HologramInterpolant + 0.04f);
                MaxSkyOpacity = 1f - HologramInterpolant;
                if (HologramInterpolant >= 1f)
                    NPC.active = false;
            }

            PerformStandardFraming();
        }
    }
}
