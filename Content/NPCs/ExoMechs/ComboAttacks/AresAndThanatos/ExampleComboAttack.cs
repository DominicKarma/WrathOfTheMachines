﻿using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace WoTM.Content.NPCs.ExoMechs
{
    public class ExampleComboAttack : ExoMechComboHandler
    {
        public override int[] ExpectedManagingExoMechs => [ModContent.NPCType<Apollo>(), ModContent.NPCType<AresBody>()];

        public override bool Perform(NPC npc)
        {
            npc.Center = Vector2.Lerp(npc.Center, Target.Center, 0.09f);
            npc.velocity = Vector2.Zero;

            return false;
        }
    }
}
