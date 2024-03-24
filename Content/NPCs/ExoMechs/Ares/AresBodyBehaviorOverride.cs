using System.IO;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader.IO;

namespace DifferentExoMechs.Content.NPCs.ExoMechs
{
    public sealed partial class AresBodyBehaviorOverride : NPCBehaviorOverride
    {
        public enum AresAIState
        {

        }

        /// <summary>
        /// Ares' current, non-combo state.
        /// </summary>
        public AresAIState CurrentState
        {
            get;
            set;
        }

        /// <summary>
        /// Ares' current AI timer.
        /// </summary>
        public int AITimer
        {
            get;
            set;
        }

        /// <summary>
        /// The target that Ares will attempt to attack.
        /// </summary>
        public static Player Target => ExoMechTargetSelector.Target;

        public override int NPCOverrideID => ExoMechNPCIDs.AresBodyID;

        public override void SendExtraAI(BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            binaryWriter.Write(AITimer);
            binaryWriter.Write((int)CurrentState);
        }

        public override void ReceiveExtraAI(BitReader bitReader, BinaryReader binaryReader)
        {
            AITimer = binaryReader.ReadInt32();
            CurrentState = (AresAIState)binaryReader.ReadInt32();
        }

        public override void AI()
        {
            PerformPreUpdateResets();
            ExecuteCurrentState();

            NPC.Opacity = 1f;

            AITimer++;
        }

        /// <summary>
        /// Resets various things pertaining to the fight state prior to behavior updates.
        /// </summary>
        /// <remarks>
        /// This serves as a means of ensuring that changes to the fight state are gracefully reset if something suddenly changes, while affording the ability to make changes during updates.<br></br>
        /// As a result, this alleviates behaviors AI states from the burden of having to assume that they may terminate at any time and must account for that to ensure that the state is reset.
        /// </remarks>
        public void PerformPreUpdateResets()
        {
            NPC.damage = NPC.defDamage;
            NPC.defense = NPC.defDefense;
            NPC.dontTakeDamage = false;
            NPC.ShowNameOnHover = true;

            CalamityGlobalNPC.draedonExoMechPrime = NPC.whoAmI;
        }

        /// <summary>
        /// Performs Ares' current state.
        /// </summary>
        public void ExecuteCurrentState()
        {
            switch (CurrentState)
            {
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            return true;
        }
    }
}
