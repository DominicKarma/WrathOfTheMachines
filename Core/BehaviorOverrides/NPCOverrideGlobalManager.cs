using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

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

        public override bool InstancePerEntity => false;

        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            if (NPCOverrideRelationship.TryGetValue(npc.type, out NPCBehaviorOverride? behaviorOverride))
                BehaviorOverride = behaviorOverride!.Clone(npc);
        }
    }
}
