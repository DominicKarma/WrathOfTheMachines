using CalamityMod.Skies;
using FargowiltasCrossmod.Content.Calamity.Bosses.ExoMechs.FightManagers;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargowiltasCrossmod.Content.Calamity.Bosses.ExoMechs.SpecificManagers
{
    public class CustomExoMechsSkyScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

        public override float GetWeight(Player player) => 0.85f;

        public override bool IsSceneEffectActive(Player player) => NPC.AnyNPCs(ModContent.NPCType<CalamityMod.NPCs.ExoMechs.Draedon>()) || ExoMechFightStateManager.FightOngoing;

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
