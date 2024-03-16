﻿using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.Particles;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace DifferentExoMechs.Content.NPCs.Bosses
{
    public sealed class ThanatosBodyBehaviorOverride : NPCBehaviorOverride, IThanatosSegment
    {
        /// <summary>
        /// How long this body segment has existed, in frames.
        /// </summary>
        public ref float ExistenceTimer => ref NPC.localAI[0];

        /// <summary>
        /// How open this body segment is.
        /// </summary>
        public float SegmentOpenInterpolant
        {
            get;
            set;
        }

        /// <summary>
        /// The index to the ahead segment in the NPC array.
        /// </summary>
        public int AheadSegmentIndex => (int)NPC.ai[1];

        /// <summary>
        /// Whether this segment should draw as a tail.
        /// </summary>
        public bool IsTailSegment => NPC.ai[2] == 1f;

        /// <summary>
        /// The index of this segment relative to the entire worm. A value of 0 corresponds to the first body segment, a value of 1 to the second, and so on.
        /// </summary>
        public int RelativeIndex => (int)NPC.ai[3];

        /// <summary>
        /// How long body segments wait before executing any actual AI code.
        /// </summary>
        /// 
        /// <remarks>
        /// This exists to give a short window of time in multiplayer to allow for body segments to all spawn, so that if latency occurs with any of the segments the worm doesn't become just a head due to not having a valid ahead segment yet.
        /// </remarks>
        public static readonly int ActivationDelay = Utilities.SecondsToFrames(0.4f);

        public override int NPCOverrideID => ModContent.NPCType<ThanatosBody1>();

        public override void AI()
        {
            ExistenceTimer++;
            if (ExistenceTimer <= ActivationDelay)
                return;

            if (!ValidateAheadSegment())
            {
                NPC.active = false;
                return;
            }

            NPC aheadSegment = Main.npc[AheadSegmentIndex];
            Vector2 directionToNextSegment = aheadSegment.Center - NPC.Center;
            if (aheadSegment.rotation != NPC.rotation)
                directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - NPC.rotation) * 0.08f);

            NPC.Opacity = aheadSegment.Opacity;

            NPC.Center = aheadSegment.Center - directionToNextSegment.SafeNormalize(Vector2.Zero) * NPC.width * NPC.scale * 0.9f;
            NPC.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
            NPC.spriteDirection = directionToNextSegment.X.NonZeroSign();

            NPC.As<ThanatosBody1>().SmokeDrawer.ParticleSpawnRate = int.MaxValue;
            ListenToHeadInstructions();
            UpdateVentSmoke();

            ModifyDRBasedOnOpenInterpolant();
        }

        /// <summary>
        /// Listens to incoming instructions from the head's <see cref="ThanatosHeadBehaviorOverride.BodyAction"/>.
        /// </summary>
        public void ListenToHeadInstructions()
        {
            if (CalamityGlobalNPC.draedonExoMechWorm == -1)
                return;

            NPC thanatos = Main.npc[CalamityGlobalNPC.draedonExoMechWorm];
            if (!thanatos.TryGetGlobalNPC(out NPCOverrideGlobalManager behaviorOverride) || behaviorOverride.BehaviorOverride is not ThanatosHeadBehaviorOverride thanatosAI)
                return;

            if (!thanatosAI.BodyAction?.Condition(NPC, RelativeIndex) ?? false)
                return;

            thanatosAI.BodyAction?.Action(this);
        }

        /// <summary>
        /// Modifies the DR of this segment in accordance with the <see cref="SegmentOpenInterpolant"/> value.
        /// </summary>
        public void ModifyDRBasedOnOpenInterpolant()
        {
            float damageReduction = MathHelper.SmoothStep(0.9999f, 0f, SegmentOpenInterpolant);
            CalamityGlobalNPC globalNPC = NPC.Calamity();
            globalNPC.unbreakableDR = damageReduction >= 0.999f;
            globalNPC.DR = damageReduction;
            NPC.chaseable = !globalNPC.unbreakableDR;
        }

        /// <summary>
        /// Updates the vent smoke that Thanatos emits.
        /// </summary>
        public void UpdateVentSmoke()
        {
            ThanatosSmokeParticleSet smoke = NPC.As<ThanatosBody1>().SmokeDrawer;
            smoke.BaseMoveRotation = NPC.rotation - MathHelper.PiOver2;
            smoke.Update();
        }

        /// <summary>
        /// Validates whether the ahead segment is valid.
        /// </summary>
        /// <returns>Whether this segment can still exist due to having a valid ahead segment.</returns>
        public bool ValidateAheadSegment()
        {
            if (!Main.npc.IndexInRange(AheadSegmentIndex))
                return false;

            NPC aheadSegment = Main.npc[AheadSegmentIndex];
            bool connectedToSameWorm = NPC.realLife == aheadSegment.realLife;
            bool aheadSegmentIsHead = aheadSegment.type == ExoMechNPCIDs.ThanatosHeadID;
            if (!connectedToSameWorm && !aheadSegmentIsHead)
                return false;

            if (!aheadSegment.active)
                return false;

            return true;
        }

        public override void FindFrame(int frameHeight)
        {
            int frame = Utils.Clamp((int)(SegmentOpenInterpolant * Main.npcFrameCount[NPC.type]), 0, Main.npcFrameCount[NPC.type] - 1);
            NPC.frame.Y = frame * frameHeight;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            Texture2D texture = TextureAssets.Npc[NPC.type].Value;
            Texture2D glowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Thanatos/ThanatosBody1Glow").Value;
            if (RelativeIndex % 2 == 1)
            {
                int body2ID = ModContent.NPCType<ThanatosBody2>();
                Main.instance.LoadNPC(body2ID);
                texture = TextureAssets.Npc[body2ID].Value;
                glowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Thanatos/ThanatosBody2Glow").Value;
            }
            if (IsTailSegment)
            {
                int tailID = ModContent.NPCType<ThanatosTail>();
                Main.instance.LoadNPC(tailID);
                texture = TextureAssets.Npc[tailID].Value;
                glowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Thanatos/ThanatosTailGlow").Value;
            }

            int frame = NPC.frame.Y / NPC.frame.Height;
            Vector2 drawPosition = NPC.Center - screenPos;
            Rectangle rectangleFrame = texture.Frame(1, Main.npcFrameCount[NPC.type], 0, frame);
            Main.spriteBatch.Draw(texture, drawPosition, rectangleFrame, NPC.GetAlpha(lightColor), NPC.rotation, rectangleFrame.Size() * 0.5f, NPC.scale, NPC.spriteDirection.ToSpriteDirection(), 0f);
            Main.spriteBatch.Draw(glowmask, drawPosition, rectangleFrame, NPC.GetAlpha(Color.White), NPC.rotation, rectangleFrame.Size() * 0.5f, NPC.scale, NPC.spriteDirection.ToSpriteDirection(), 0f);

            NPC.As<ThanatosBody1>().SmokeDrawer.DrawSet(NPC.Center);

            return false;
        }
    }
}
