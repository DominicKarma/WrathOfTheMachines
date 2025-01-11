global using LumUtils = Luminance.Common.Utilities.Utilities;
using CalamityMod.Events;
using CalamityMod.NPCs.ExoMechs;
using Terraria.ModLoader;
using WoTM.Content.NPCs.ExoMechs.Ares;

namespace WoTM
{
    public class WoTM : Mod
    {
        public override void PostSetupContent()
        {
            BossRushEvent.Bosses.ForEach(b =>
            {
                if (b.EntityID == ModContent.NPCType<Draedon>())
                    b.HostileNPCsToNotDelete.Add(ModContent.NPCType<AresHand>());
            });
        }
    }
}
