using Terraria;

namespace WoTM
{
    public static class NPCExtensionMethods
    {
        /// <summary>
        /// Safely attempts to retrieve a given <see cref="NPCBehaviorOverride"/> instance from an NPC.
        /// </summary>
        /// <typeparam name="T">The type of override to search for.</typeparam>
        /// <param name="npc">The NPC</param>
        /// <param name="behavior">The resulting behavior. Is null if retrieval failed.</param>
        /// <returns>Whether the behavior was successfully retrieved.</returns>
        public static bool TryGetBehavior<T>(this NPC npc, out T behavior) where T : NPCBehaviorOverride
        {
            // This is technically a violation of null safety design, but I'm not sure what a saner alternative is.
            // Since NPCOverrideGlobalManager is an abstract object it'd be quite strange setting up a Default object in accordance with the null object pattern, especially
            // when safety is already guaranteed in the form of the bool this method returns

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            behavior = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            if (!npc.TryGetGlobalNPC(out NPCOverrideGlobalManager manager))
                return false;

            if (manager.BehaviorOverride is not T b)
                return false;

            behavior = b;
            return true;
        }
    }
}
