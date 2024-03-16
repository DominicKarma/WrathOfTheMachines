using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace DifferentExoMechs
{
    public abstract class NPCBehaviorOverride : ModType<NPC, NPCBehaviorOverride>
    {
        /// <summary>
        /// The NPC that this behavior override handles.
        /// </summary>
        public NPC NPC => Entity;

        /// <summary>
        /// The NPC ID to override.
        /// </summary>
        public abstract int NPCOverrideID
        {
            get;
        }

        protected sealed override void Register()
        {
            ModTypeLookup<NPCBehaviorOverride>.Register(this);
            NPC.type = NPCOverrideID;
        }

        public sealed override void SetupContent()
        {
            NPCOverrideGlobalManager.NPCOverrideRelationship[NPCOverrideID] = this;
            SetStaticDefaults();
        }

        protected override NPC CreateTemplateEntity() => new();

        /// <summary>
        /// The central AI loop for the NPC.
        /// </summary>
        public virtual void AI() { }

        /// <summary>
        /// The central rendering method.
        /// </summary>
        /// <param name="spriteBatch"></param>
        /// <returns><see langword="false"/> if base drawing should be ignored, <see langword="true"/> otherwise.</returns>
        public virtual bool PreDraw(SpriteBatch spriteBatch) => true;
    }
}
