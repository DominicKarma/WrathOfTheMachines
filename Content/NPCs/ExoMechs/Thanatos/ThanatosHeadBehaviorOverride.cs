using System.IO;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.Particles;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DifferentExoMechs.Content.NPCs.Bosses
{
    public sealed class ThanatosHeadBehaviorOverride : NPCBehaviorOverride, IThanatosSegment
    {
        public enum ThanatosAIState
        {

        }

        /// <summary>
        /// Whether Thanatos has created his body segments yet or not.
        /// </summary>
        public bool HasCreatedSegments
        {
            get => NPC.ai[0] == 1f;
            set => NPC.ai[0] = value.ToInt();
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
        public BodySegmentInstructions? BodyAction
        {
            get;
            set;
        }

        /// <summary>
        /// Unimplemented, since Thanatos' head doesn't have an ahead segment. Do not use.
        /// </summary>
        public int AheadSegmentIndex => -1;

        /// <summary>
        /// The amount of body segments Thanatos spawns with.
        /// </summary>
        public const int BodySegmentCount = 67;

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
            binaryWriter.Write(SegmentOpenInterpolant);
        }

        public override void ReceiveExtraAI(BitReader bitReader, BinaryReader binaryReader)
        {
            SegmentOpenInterpolant = binaryReader.ReadSingle();
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

            NPC.Opacity = 1f;
            NPC.velocity = Vector2.Lerp(NPC.velocity, NPC.DirectionTo(Main.MouseWorld) * 23f, 0.032f);
            NPC.rotation = NPC.velocity.ToRotation() + MathHelper.PiOver2;

            if (Main.mouseRight)
                BodyAction = new(EveryNthSegment(2), OpenSegmentAction(0.03f));
            else
                BodyAction = new(EveryNthSegment(2), CloseSegmentAction(0.067f));
        }

        /// <summary>
        ///     Resets various things pertaining to the fight state prior to behavior updates.
        /// </summary>
        /// <remarks>
        ///     This serves as a means of ensuring that changes to the fight state are gracefully reset if something suddenly changes, while affording the ability to make changes during updates.<br></br>
        ///     As a result, this alleviates behaviors AI states from the burden of having to assume that they may terminate at any time and must account for that to ensure that the state is reset.
        /// </remarks>
        public void PerformPreUpdateResets()
        {
            NPC.damage = NPC.defDamage;
            NPC.defense = NPC.defDefense;
            NPC.dontTakeDamage = false;
            NPC.ShowNameOnHover = true;
            BodyAction = null;

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
        /// Generates a <see cref="BodySegmentCondition"/> that corresponds to every Nth segment. Meant to be used in conjunction with <see cref="BodyAction"/>.
        /// </summary>
        /// <param name="n">The cycle repeat value.</param>
        /// <param name="cyclicOffset">The offset in the cycle. Defaults to 0.</param>
        public static BodySegmentCondition EveryNthSegment(int n, int cyclicOffset = 0) => new((segment, segmentIndex) => segmentIndex % n == cyclicOffset);

        /// <summary>
        /// An action that opens a segment's vents. Meant to be used in conjunction with <see cref="BodyAction"/>.
        /// </summary>
        /// <param name="segmentOpenRate">The amount by which the segment open interpolant changes every frame.</param>
        public static BodySegmentAction OpenSegmentAction(float segmentOpenRate)
        {
            return new(behaviorOverride =>
            {
                float oldInterpolant = behaviorOverride.SegmentOpenInterpolant;
                behaviorOverride.SegmentOpenInterpolant = Utilities.Saturate(behaviorOverride.SegmentOpenInterpolant + segmentOpenRate);

                if (behaviorOverride.SegmentOpenInterpolant > 0f && oldInterpolant <= 0f)
                    SoundEngine.PlaySound(ThanatosHead.VentSound with { MaxInstances = 8, Volume = 0.3f }, behaviorOverride.NPC.Center);

                ThanatosSmokeParticleSet smoke = behaviorOverride.NPC.As<ThanatosBody1>().SmokeDrawer;
                if (behaviorOverride.SegmentOpenInterpolant > 0f)
                    smoke.ParticleSpawnRate = ThanatosHead.ventCloudSpawnRate;
            });
        }

        /// <summary>
        /// An action that closes a segment's vents. Meant to be used in conjunction with <see cref="BodyAction"/>.
        /// </summary>
        /// <param name="segmentCloseRate">The amount by which the segment open interpolant changes every frame.</param>
        public static BodySegmentAction CloseSegmentAction(float segmentCloseRate) => OpenSegmentAction(-segmentCloseRate);

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            return true;
        }
    }
}
