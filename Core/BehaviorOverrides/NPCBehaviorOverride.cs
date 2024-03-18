using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

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
        /// Modifies the type name of the NPC.
        /// </summary>
        /// <param name="typeName">The NPC's <see cref="NPC.TypeName"/.></param>
        public virtual void ModifyTypeName(ref string typeName) { }

        /// <summary>
        /// Sends arbitrary NPC state data across the network when an NPC sync occurs.
        /// </summary>
        /// <param name="bitWriter">The compressible bit writer. Booleans written via this are compressed across all mods to improve multiplayer performance.</param>
        /// <param name="binaryWriter">The writer.</param>
        public virtual void SendExtraAI(BitWriter bitWriter, BinaryWriter binaryWriter) { }

        /// <summary>
        /// Receives arbitrary NPC state data across the network after an NPC sync occurs.
        /// </summary>
        /// <param name="bitReader">The compressible bit reader.</param>
        /// <param name="binaryReader">The reader.</param>
        public virtual void ReceiveExtraAI(BitReader bitReader, BinaryReader binaryReader) { }

        /// <summary>
        /// The central AI loop for the NPC.
        /// </summary>
        public virtual void AI() { }

        /// <summary>
        /// Allows you to modify the frame from an NPC's texture that is drawn, which is necessary in order to animate NPCs.
        /// </summary>
        /// <param name="frameHeight">The height of a single frame from the overall texture.</param>
        public virtual void FindFrame(int frameHeight) { }

        /// <summary>
        /// Allows you to modify the damage, knockback, etc., that an NPC takes from a projectile.
        /// </summary>
        /// <param name="projectile">The harming projectile.</param>
        /// <param name="modifiers">The hit modifier information.</param>
        public virtual void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers) { }

        /// <summary>
        /// The central rendering method.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch to draw with.</param>
        /// <param name="screenPos">The screen position.</param>
        /// <param name="lightColor">The color of light at the NPC's center.</param>
        /// <returns><see langword="false"/> if base drawing should be ignored, <see langword="true"/> otherwise.</returns>
        public virtual bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor) => true;
    }
}
