﻿using System;
using System.IO;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using DifferentExoMechs.Content.Particles;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DifferentExoMechs.Content.NPCs.Bosses
{
    public sealed partial class ThanatosHeadBehaviorOverride : NPCBehaviorOverride, IThanatosSegment
    {
        public enum ThanatosAIState
        {
            PerpendicularBodyLaserBlasts
        }

        /// <summary>
        /// Thanatos' current, non-combo state.
        /// </summary>
        public ThanatosAIState CurrentState
        {
            get;
            set;
        }

        /// <summary>
        /// Thanatos' current AI timer.
        /// </summary>
        public int AITimer
        {
            get;
            set;
        }

        /// <summary>
        /// Whether Thanatos has created his body segments yet or not.
        /// </summary>
        public bool HasCreatedSegments
        {
            get;
            set;
        }

        /// <summary>
        /// How open this head segment is.
        /// </summary>
        public float SegmentOpenInterpolant
        {
            get;
            set;
        }

        /// <summary>
        /// The action that should be taken by body segments.
        /// </summary>
        /// 
        /// <remarks>
        /// This value can be (and usually is) null. When it is, nothing special is performed by the body segments.
        /// </remarks>
        public BodySegmentInstructions? BodyBehaviorAction
        {
            get;
            set;
        }

        /// <summary>
        /// The render action that should be taken by body segments.
        /// </summary>
        /// 
        /// <remarks>
        /// This value can be (and usually is) null. When it is, nothing special is performed by the body segments.
        /// </remarks>
        public BodySegmentInstructions? BodyRenderAction
        {
            get;
            set;
        }

        /// <summary>
        /// Unimplemented, since Thanatos' head doesn't have an ahead segment. Do not use.
        /// </summary>
        public int AheadSegmentIndex => throw new NotImplementedException();

        /// <summary>
        /// The target that Thanatos will attempt to attack.
        /// </summary>
        public static Player Target => ExoMechTargetSelector.Target;

        /// <summary>
        /// The amount of body segments Thanatos spawns with.
        /// </summary>
        public const int BodySegmentCount = 67;

        /// <summary>
        /// The standard segment opening rate from <see cref="OpenSegment(float)"/>.
        /// </summary>
        public const float StandardSegmentOpenRate = 0.0285f;

        /// <summary>
        /// The standard segment closing rate from <see cref="CloseSegment(float)"/>.
        /// </summary>
        public const float StandardSegmentCloseRate = 0.067f;

        /// <summary>
        /// Represents an action that should be performed by segments on Thanatos' body.
        /// </summary>
        /// <param name="behaviorOverride">The segment's overriding instance.</param>
        public delegate void BodySegmentAction(ThanatosBodyBehaviorOverride behaviorOverride);

        /// <summary>
        /// Represents a condition that should be applied to Thanatos' body segments.
        /// </summary>
        /// <param name="segment">The segment's NPC instance.</param>
        /// <param name="segmentIndex">The index of the segment being evaluated.</param>
        public delegate bool BodySegmentCondition(NPC segment, int segmentIndex);

        /// <summary>
        /// Represents an action that should be taken conditionally across Thanatos' body segments.
        /// </summary>
        /// <param name="Condition">The condition that dictates whether the <paramref name="Action"/> should occur.</param>
        /// <param name="Action">The action that the body segments should perform.</param>
        public record BodySegmentInstructions(BodySegmentCondition Condition, BodySegmentAction Action);

        public override int NPCOverrideID => ExoMechNPCIDs.ThanatosHeadID;

        public override void SendExtraAI(BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            bitWriter.WriteBit(HasCreatedSegments);

            binaryWriter.Write(SegmentOpenInterpolant);
            binaryWriter.Write(AITimer);
            binaryWriter.Write((int)CurrentState);
        }

        public override void ReceiveExtraAI(BitReader bitReader, BinaryReader binaryReader)
        {
            HasCreatedSegments = bitReader.ReadBit();

            SegmentOpenInterpolant = binaryReader.ReadSingle();
            AITimer = binaryReader.ReadInt32();
            CurrentState = (ThanatosAIState)binaryReader.ReadInt32();
        }

        public override void AI()
        {
            PerformPreUpdateResets();

            if (!HasCreatedSegments)
            {
                CreateSegments();
                HasCreatedSegments = true;
                NPC.netUpdate = true;
            }

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
            BodyBehaviorAction = null;
            BodyRenderAction = null;

            CalamityGlobalNPC.draedonExoMechWorm = NPC.whoAmI;
        }

        /// <summary>
        /// Creates a total of <see cref="BodySegmentCount"/> segments that attach to each other similar to a linked list.
        /// </summary>
        public void CreateSegments()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // The tail and body2 NPC variants are unused in favor of varied drawing so as to minimize the amount of code generalizations/copy-pasting necessary to get Thanatos working.
            int segmentID = ModContent.NPCType<ThanatosBody1>();
            int previousSegmentIndex = NPC.whoAmI;
            for (int i = 0; i < BodySegmentCount; i++)
            {
                bool tailSegment = i == BodySegmentCount - 1;
                int nextSegmentIndex = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, segmentID, NPC.whoAmI + 1, NPC.whoAmI, previousSegmentIndex, tailSegment.ToInt(), i);

                NPC nextSegment = Main.npc[nextSegmentIndex];
                nextSegment.realLife = NPC.whoAmI;

                // Immediately inform all clients of the spawning of the body segment so that there's a little latency as possible.
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, nextSegmentIndex);

                previousSegmentIndex = nextSegmentIndex;
            }
        }

        /// <summary>
        /// Performs Thanatos' current state.
        /// </summary>
        public void ExecuteCurrentState()
        {
            switch (CurrentState)
            {
                case ThanatosAIState.PerpendicularBodyLaserBlasts:
                    DoBehavior_PerpendicularBodyLaserBlasts();
                    break;
            }
        }

        /// <summary>
        /// Generates a <see cref="BodySegmentCondition"/> that corresponds to every Nth segment. Meant to be used in conjunction with <see cref="BodyBehaviorAction"/>.
        /// </summary>
        /// <param name="n">The cycle repeat value.</param>
        /// <param name="cyclicOffset">The offset in the cycle. Defaults to 0.</param>
        public static BodySegmentCondition EveryNthSegment(int n, int cyclicOffset = 0) => new((segment, segmentIndex) => segmentIndex % n == cyclicOffset);

        /// <summary>
        /// Generates a <see cref="BodySegmentCondition"/> that corresponds to every single segment. Meant to be used in conjunction with <see cref="BodyBehaviorAction"/>.
        /// </summary>
        public static BodySegmentCondition AllSegments() => new((segment, segmentIndex) => true);

        /// <summary>
        /// An action that opens a segment's vents. Meant to be used in conjunction with <see cref="BodyBehaviorAction"/>.
        /// </summary>
        /// <param name="segmentOpenRate">The amount by which the segment open interpolant changes every frame.</param>
        public static BodySegmentAction OpenSegment(float segmentOpenRate = StandardSegmentOpenRate)
        {
            return new(behaviorOverride =>
            {
                float oldInterpolant = behaviorOverride.SegmentOpenInterpolant;
                behaviorOverride.SegmentOpenInterpolant = Utilities.Saturate(behaviorOverride.SegmentOpenInterpolant + segmentOpenRate);

                bool segmentJustOpened = behaviorOverride.SegmentOpenInterpolant > 0f && oldInterpolant <= 0f;
                if (segmentJustOpened)
                    SoundEngine.PlaySound(ThanatosHead.VentSound with { MaxInstances = 8, Volume = 0.3f }, behaviorOverride.NPC.Center);

                float bigInterpolant = Utilities.InverseLerp(1f, 0.91f, behaviorOverride.SegmentOpenInterpolant);
                if (behaviorOverride.SegmentOpenInterpolant >= 0.91f)
                    CreateSmoke(behaviorOverride.NPC, bigInterpolant);
            });
        }

        /// <summary>
        /// Creates smoke particles perpendicular to a segment NPC.
        /// </summary>
        /// <param name="npc">The segment NPC instance.</param>
        /// <param name="bigInterpolant">How big the smoke should be.</param>
        public static void CreateSmoke(NPC npc, float bigInterpolant)
        {
            if (!npc.WithinRange(Main.LocalPlayer.Center, 1500f))
                return;

            int smokeCount = (int)MathHelper.Lerp(2f, 40f, bigInterpolant);
            for (int i = 0; i < smokeCount; i++)
            {
                int smokeLifetime = Main.rand.Next(24, 36);
                float smokeSpeed = Main.rand.NextFloat(15f, 29f);
                Color smokeColor = Color.Lerp(Color.Red, Color.Gray, 0.6f);
                if (Main.rand.NextBool(4))
                    smokeColor = Color.DarkRed;
                smokeColor.A = 97;

                if (Main.rand.NextBool(bigInterpolant))
                {
                    smokeSpeed *= 1f + bigInterpolant;
                    smokeLifetime += (int)(bigInterpolant * 30f);
                }

                Vector2 perpendicular = npc.rotation.ToRotationVector2();
                Vector2 smokeVelocity = perpendicular.RotatedByRandom(0.2f) * Main.rand.NextFromList(-1f, 1f) * smokeSpeed;
                SmokeParticle smoke = new(npc.Center, smokeVelocity, smokeColor, smokeLifetime, 0.6f, 0.18f);
                smoke.Spawn();
            }
        }

        /// <summary>
        /// An action that closes a segment's vents. Meant to be used in conjunction with <see cref="BodyBehaviorAction"/>.
        /// </summary>
        /// <param name="segmentCloseRate">The amount by which the segment open interpolant changes every frame.</param>
        public static BodySegmentAction CloseSegment(float segmentCloseRate = StandardSegmentCloseRate) => OpenSegment(-segmentCloseRate);

        public override void ModifyTypeName(ref string typeName)
        {
            typeName = Language.GetTextValue("Mods.DifferentExoMechs.NPCs.ThanatosRename");
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            // TODO -- Implement segment opening.
            return true;
        }
    }
}
