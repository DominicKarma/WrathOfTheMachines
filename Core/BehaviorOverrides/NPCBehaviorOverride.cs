using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace WoTM
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

        public virtual void SetDefaults() { }

        /// <summary>
        /// Allows for the setting of bestiary information for the NPC.
        /// </summary>
        public virtual void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) { }

        /// <summary>
        /// Gets called when the NPC spawns in the world.
        /// </summary>
        /// <param name="source">The spawn context of the NPC.</param>
        public virtual void OnSpawn(IEntitySource source) { }

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
        /// Allows you to add and modify NPC loot tables to drop on death and to appear in the Bestiary.<br/>
        /// <br/> This hook only runs once during mod loading, any dynamic behavior must be contained in the rules themselves.
        /// </summary>
        /// <param name="npcLoot">A reference to the item drop database for this npc type</param>
        public virtual void ModifyNPCLoot(NPCLoot npcLoot) { }

        /// <summary>
        /// Allows you to make things happen when this NPC dies (for example, dropping items and setting ModSystem fields). This hook runs on the server/single player. For client-side effects, such as dust, gore, and sounds, see HitEffect.
        /// </summary>
        public virtual void OnKill() { }

        /// <summary>
        /// Allows you to determine the color and transparency in which an NPC is drawn. Return null to use the default color (normally light and buff color). Returns null by default.
        /// </summary>
        /// <param name="drawColor">The base color of the NPC.</param>
        public virtual Color? GetAlpha(Color drawColor) => null;

        /// <summary>
        /// Allows you to modify the frame from an NPC's texture that is drawn, which is necessary in order to animate NPCs.
        /// </summary>
        /// <param name="frameHeight">The height of a single frame from the overall texture.</param>
        public virtual void FindFrame(int frameHeight) { }

        /// <summary>
        /// Allows you to customize the boss head texture used by an NPC based on its state. Set index to -1 to stop the texture from being displayed.
        /// </summary>
        public virtual void BossHeadSlot(ref int index) { }

        /// <summary>
        /// Allows you to make things happen whenever this NPC is hit, such as creating dust or gores. <br/> 
        /// Called on local, server and remote clients. <br/> 
        /// Usually when something happens when an NPC dies such as item spawning, you use NPCLoot, but you can use HitEffect paired with a check for <c>if (NPC.life &lt;= 0)</c> to do client-side death effects, such as spawning dust, gore, or death sounds.
        /// </summary>
        public virtual void HitEffect(NPC.HitInfo hit) { }

        /// <summary>
        /// Allows you to prevent an NPC from doing anything on death (besides die). Return false to stop the NPC from doing anything special. Returns true by default.
        /// </summary>
        public virtual bool PreKill() => true;

        /// <summary>
        /// Whether or not an NPC should be killed when it reaches 0 health. You may program extra effects in this hook (for example, how Golem's head lifts up for the second phase of its fight). Return false to stop the NPC from being killed. Returns true by default.
        /// </summary>
        public virtual bool CheckDead() => true;

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
