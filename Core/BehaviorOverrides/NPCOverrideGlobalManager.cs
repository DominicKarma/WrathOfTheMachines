using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DifferentExoMechs
{
    public class NPCOverrideGlobalManager : GlobalNPC
    {
        /// <summary>
        /// The relationship of NPC ID to corresponding override.
        /// </summary>
        internal static readonly Dictionary<int, NPCBehaviorOverride> NPCOverrideRelationship = [];

        /// <summary>
        /// The behavior override that governs the behavior of a given NPC.
        /// </summary>
        internal NPCBehaviorOverride? BehaviorOverride;

        public override bool InstancePerEntity => true;

        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            if (NPCOverrideRelationship.TryGetValue(npc.type, out NPCBehaviorOverride? behaviorOverride))
                BehaviorOverride = behaviorOverride!.Clone(npc);
        }

        public override bool PreAI(NPC npc)
        {
            if (!InfernumModeCompatibility.InfernumModeIsActive && BehaviorOverride is not null)
            {
                BehaviorOverride.AI();
                return false;
            }

            return true;
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            if (InfernumModeCompatibility.InfernumModeIsActive)
                return;

            BehaviorOverride?.FindFrame(frameHeight);
        }

        public override void ModifyTypeName(NPC npc, ref string typeName) => BehaviorOverride?.ModifyTypeName(ref typeName);

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter) => BehaviorOverride?.SendExtraAI(bitWriter, binaryWriter);

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader) => BehaviorOverride?.ReceiveExtraAI(bitReader, binaryReader);

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (InfernumModeCompatibility.InfernumModeIsActive)
                return true;

            return BehaviorOverride?.PreDraw(spriteBatch, screenPos, drawColor) ?? true;
        }
    }
}
