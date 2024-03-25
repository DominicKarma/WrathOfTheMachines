using System.Collections.Generic;
using System.IO;
using System.Linq;
using CalamityMod.NPCs;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace WoTM.Content.NPCs.ExoMechs
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
        /// Whether Ares has created his arms yet or not.
        /// </summary>
        public bool HasCreatedArms
        {
            get;
            set;
        }

        /// <summary>
        /// The set of instructions that should be performed by each of Ares' arms.
        /// </summary>
        public HandInstructions[] InstructionsForHand
        {
            get;
            set;
        } = new HandInstructions[ArmCount];

        /// <summary>
        /// The target that Ares will attempt to attack.
        /// </summary>
        public static Player Target => ExoMechTargetSelector.Target;

        /// <summary>
        /// The amount of arms that Ares should have.
        /// </summary>
        public const int ArmCount = 4;

        /// <summary>
        /// Represents an action that should be taken for one of Ares' hands.
        /// </summary>
        /// <param name="Action">The action that the hand should perform.</param>
        public record HandInstructions(AresHandAction Action);

        /// <summary>
        /// Represents an action that should be performed by hands attached to Ares.
        /// </summary>
        /// <param name="hand">The hand's ModNPC instance..</param>
        public delegate void AresHandAction(AresHand hand);

        public override int NPCOverrideID => ExoMechNPCIDs.AresBodyID;

        public override void SendExtraAI(BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            bitWriter.WriteBit(HasCreatedArms);
            binaryWriter.Write(AITimer);
            binaryWriter.Write((int)CurrentState);
        }

        public override void ReceiveExtraAI(BitReader bitReader, BinaryReader binaryReader)
        {
            HasCreatedArms = bitReader.ReadBit();
            AITimer = binaryReader.ReadInt32();
            CurrentState = (AresAIState)binaryReader.ReadInt32();
        }

        public override void AI()
        {
            InstructionsForHand ??= new HandInstructions[ArmCount];
            if (Main.netMode != NetmodeID.MultiplayerClient && !HasCreatedArms)
            {
                CreateArms();
                HasCreatedArms = true;
                NPC.netUpdate = true;
            }

            PerformPreUpdateResets();
            ExecuteCurrentState();

            NPC.SimpleFlyMovement(NPC.SafeDirectionTo(Target.Center) * 14f, 0.3f);

            InstructionsForHand[0] = new(hand =>
            {
                hand.NPC.Opacity = 1f;
                hand.UsesBackArm = true;
                hand.NPC.SmoothFlyNear(NPC.Center + new Vector2(-420f, 80f), 0.2f, 0.84f);
                hand.RotateToLookAt(Target.Center);
                hand.ArmSide = -1;
                hand.Frame = (AITimer / 5 + 9) % 11;
            });
            InstructionsForHand[1] = new(hand =>
            {
                hand.NPC.Opacity = 1f;
                hand.UsesBackArm = false;
                hand.NPC.SmoothFlyNear(NPC.Center + new Vector2(-300f, 224f), 0.2f, 0.84f);
                hand.RotateToLookAt(Target.Center);
                hand.ArmSide = -1;
                hand.Frame = (AITimer / 5 + 7) % 11;
            });
            InstructionsForHand[2] = new(hand =>
            {
                hand.NPC.Opacity = 1f;
                hand.UsesBackArm = false;
                hand.NPC.SmoothFlyNear(NPC.Center + new Vector2(300f, 224f), 0.2f, 0.84f);
                hand.RotateToLookAt(Target.Center);
                hand.ArmSide = 1;
                hand.Frame = (AITimer / 5 + 2) % 11;
            });
            InstructionsForHand[3] = new(hand =>
            {
                hand.NPC.Opacity = 1f;
                hand.UsesBackArm = true;
                hand.NPC.SmoothFlyNear(NPC.Center + new Vector2(420f, 80f), 0.2f, 0.84f);
                hand.RotateToLookAt(Target.Center);
                hand.ArmSide = 1;
                hand.Frame = (AITimer / 5 + 12) % 11;
            });

            NPC.Opacity = 1f;

            AITimer++;
        }

        /// <summary>
        /// Creates Ares' arms.
        /// </summary>
        public void CreateArms()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < ArmCount; i++)
                NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<AresHand>(), NPC.whoAmI, i);
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
            int handID = ModContent.NPCType<AresHand>();
            List<AresHand> handsToDraw = [];
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n.type != handID || !n.active)
                    continue;

                handsToDraw.Add(n.As<AresHand>());
            }

            foreach (AresHand hand in handsToDraw.OrderBy(h => h.LocalIndex - h.UsesBackArm.ToInt() * 10))
            {
                hand.DrawArm(spriteBatch, screenPos);
            }

            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresBody").Value;
            Texture2D glowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresBodyGlow").Value;
            Vector2 drawPosition = NPC.Center - screenPos;
            Main.spriteBatch.Draw(texture, drawPosition, NPC.frame, NPC.GetAlpha(lightColor), NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, NPC.spriteDirection.ToSpriteDirection(), 0f);
            Main.spriteBatch.Draw(glowmask, drawPosition, NPC.frame, NPC.GetAlpha(Color.White), NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, NPC.spriteDirection.ToSpriteDirection(), 0f);

            return false;
        }
    }
}
