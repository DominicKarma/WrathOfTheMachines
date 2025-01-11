using Terraria;
using Terraria.ModLoader;
using WoTM.Content.NPCs.ExoMechs.FightManagers;

namespace WoTM.Content.NPCs.ExoMechs.MiscTweaks
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
