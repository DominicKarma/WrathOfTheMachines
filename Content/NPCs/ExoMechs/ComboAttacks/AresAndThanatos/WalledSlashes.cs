using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace WoTM.Content.NPCs.ExoMechs
{
    public class WalledSlashes : ExoMechComboHandler
    {
        public override int[] ExpectedManagingExoMechs => [ModContent.NPCType<ThanatosHead>(), ModContent.NPCType<AresBody>()];

        public override bool Perform(NPC npc)
        {
            if (npc.type == ModContent.NPCType<AresBody>())
                return Perform_Ares(npc);

            Perform_Hades(npc);

            return false;
        }

        public static bool Perform_Ares(NPC npc)
        {
            if (!npc.TryGetBehavior(out AresBodyBehaviorOverride ares))
            {
                npc.active = false;
                return false;
            }

            int slashCycleTime = Utilities.SecondsToFrames(2.3f);
            ares.InstructionsForHands[0] = new(h => ares.KatanaSlashesHandUpdate(h, new Vector2(-400f, 40f), 0, slashCycleTime, 0));
            ares.InstructionsForHands[1] = new(h => ares.BasicHandUpdate(h, new Vector2(-280f, 224f), 1));
            ares.InstructionsForHands[2] = new(h => ares.BasicHandUpdate(h, new Vector2(280f, 224f), 2));
            ares.InstructionsForHands[3] = new(h => ares.KatanaSlashesHandUpdate(h, new Vector2(400f, 40f), 0, slashCycleTime, 3));

            return false;
        }

        public static void Perform_Hades(NPC npc)
        {
            float spinAngle =
            Vector2 hoverDestination = Target.Center;
        }
    }
}
