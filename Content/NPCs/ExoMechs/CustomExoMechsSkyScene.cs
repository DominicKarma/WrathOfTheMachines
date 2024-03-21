using CalamityMod.NPCs.ExoMechs;
using CalamityMod.Skies;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace DifferentExoMechs.Content.NPCs.ExoMechs
{
    public class CustomExoMechsSkyScene : ModSceneEffect
    {
        public override bool IsSceneEffectActive(Player player) => (NPC.AnyNPCs(ModContent.NPCType<Draedon>()) || ExoMechBehaviorManager.AnyExoMechsPresent) && !InfernumModeCompatibility.InfernumModeIsActive;

        public override void Load()
        {
            if (Main.netMode != NetmodeID.Server)
            {
                Filters.Scene[CustomExoMechsSky.SkyKey] = new Filter(new ExoMechsScreenShaderData("FilterMiniTower").UseColor(ExoMechsSky.DrawColor).UseOpacity(0f), EffectPriority.VeryHigh);
                SkyManager.Instance[CustomExoMechsSky.SkyKey] = new CustomExoMechsSky();
                SkyManager.Instance[CustomExoMechsSky.SkyKey].Load();
            }
        }

        public override void SpecialVisuals(Player player, bool isActive)
        {
            player.ManageSpecialBiomeVisuals(CustomExoMechsSky.SkyKey, isActive);
        }
    }
}
