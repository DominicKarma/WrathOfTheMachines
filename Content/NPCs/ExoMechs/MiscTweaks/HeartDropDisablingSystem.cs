using FargowiltasCrossmod.Content.Calamity.Bosses.ExoMechs.FightManagers;
using Terraria;
using Terraria.ModLoader;

namespace FargowiltasCrossmod.Content.Calamity.Bosses.ExoMechs
{
    public class HeartDropDisablingSystem : ModSystem
    {
        public override void OnModLoad() =>
            On_NPC.DoDeathEvents_DropBossPotionsAndHearts += DisablePotionsAndHeartsForExoMechs;

        private void DisablePotionsAndHeartsForExoMechs(On_NPC.orig_DoDeathEvents_DropBossPotionsAndHearts orig, NPC npc, ref string typeName)
        {
            if (!ExoMechNPCIDs.ExoMechIDs.Contains(npc.type))
                orig(npc, ref typeName);
        }
    }
}
