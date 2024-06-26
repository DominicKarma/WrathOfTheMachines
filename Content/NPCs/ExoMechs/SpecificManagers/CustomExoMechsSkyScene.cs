﻿using CalamityMod.NPCs.ExoMechs;
using CalamityMod.Skies;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace WoTM.Content.NPCs.ExoMechs
{
    public class CustomExoMechsSkyScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

        public override float GetWeight(Player player) => 0.85f;

        public override bool IsSceneEffectActive(Player player) => (NPC.AnyNPCs(ModContent.NPCType<Draedon>()) || ExoMechFightStateManager.FightOngoing) && !InfernumModeCompatibility.InfernumModeIsActive;

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
