using System;
using System.IO;
using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.Sounds;
using Luminance.Assets;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using WoTM.Content.Particles.Metaballs;

namespace WoTM.Content.NPCs.ExoMechs
{
    public class AresHand : ModNPC
    {
        /// <summary>
        /// The type of hand that this NPC is.
        /// </summary>
        public AresHandType HandType
        {
            get;
            set;
        } = AresHandType.PlasmaCannon;

        /// <summary>
        /// The local index of this arm. This is used as a means of ensuring that the arm which instructions from Ares' body should be follows.
        /// </summary>
        public int LocalIndex => (int)NPC.ai[0];

        /// <summary>
        /// Which side arms should be drawn on relative to Ares' body.
        /// </summary>
        public int ArmSide
        {
            get => (int)NPC.ai[3];
            set => NPC.ai[3] = value;
        }

        /// <summary>
        /// The frame of this arm.
        /// </summary>
        public int Frame
        {
            get;
            set;
        }

        /// <summary>
        /// Whether this hand uses a back arm or not.
        /// </summary>
        public bool UsesBackArm
        {
            get;
            set;
        }

        /// <summary>
        /// The endpoint of this arm when drawing Ares arm.
        /// </summary>
        /// 
        /// <remarks>
        /// For most cases, this is equivalent to the hand's center. After all, one would usually want arms and hands to be attached.
        /// However, there are some circumstances, such as when a hand is being attached, that it's desirable for the two to be incongruent.
        /// </remarks>
        public Vector2 ArmEndpoint
        {
            get => new(NPC.ai[1], NPC.ai[2]);
            set
            {
                NPC.ai[1] = value.X;
                NPC.ai[2] = value.Y;
            }
        }

        public override string Texture => MiscTexturesRegistry.InvisiblePixelPath;

        public override void SetStaticDefaults()
        {
            this.ExcludeFromBestiary();
            NPCID.Sets.TrailingMode[NPC.type] = 3;
            NPCID.Sets.TrailCacheLength[NPC.type] = 10;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 5f;
            NPC.damage = 100;
            NPC.width = 172;
            NPC.height = 108;
            NPC.defense = 100;
            NPC.DR_NERD(0.35f);
            NPC.LifeMaxNERB(1250000, 1495000, 650000);
            NPC.lifeMax += (int)(NPC.lifeMax * CalamityConfig.Instance.BossHealthBoost * 0.01);
            NPC.aiStyle = -1;
            AIType = -1;
            NPC.knockBackResist = 0f;
            NPC.canGhostHeal = false;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.DeathSound = CommonCalamitySounds.ExoDeathSound;
            NPC.netAlways = true;
            NPC.boss = true;
            NPC.hide = true;
            NPC.Calamity().VulnerableToSickness = false;
            NPC.Calamity().VulnerableToElectricity = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(NPC.dontTakeDamage);
            HandType.WriteTo(writer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            NPC.dontTakeDamage = reader.ReadBoolean();
            HandType = AresHandType.ReadFrom(reader);
        }

        public override void AI()
        {
            if (CalamityGlobalNPC.draedonExoMechPrime <= -1 || !Main.npc[CalamityGlobalNPC.draedonExoMechPrime].active || !Main.npc[CalamityGlobalNPC.draedonExoMechPrime].TryGetBehavior(out AresBodyBehaviorOverride body))
            {
                NPC.active = false;
                return;
            }

            NPC.noTileCollide = true;
            if (Main.mouseRight)
            {
                NPC.noTileCollide = false;
                NPC.velocity.X *= 0.95f;
                NPC.velocity.Y += 0.7f;
                NPC.Opacity = Utilities.Saturate(NPC.Opacity - 0.04f);
                NPC.rotation = NPC.rotation.AngleLerp(0f, 0.15f);
                ArmEndpoint += Main.rand.NextVector2Circular(1f, 2f) * NPC.Opacity;

                if (NPC.Opacity <= 0f)
                    NPC.Center = ArmEndpoint + Vector2.UnitY * 240f;
            }
            else
                ArmEndpoint = NPC.Center;

            NPC.Calamity().ShouldCloseHPBar = true;
            body.InstructionsForHand[LocalIndex]?.Action?.Invoke(this);

            NPC.dontTakeDamage = NPC.Opacity < 0.95f;
            NPC.realLife = CalamityGlobalNPC.draedonExoMechPrime;
        }

        public void RotateToLookAt(Vector2 lookDestination)
        {
            float idealRotation = NPC.AngleTo(lookDestination);
            NPC.spriteDirection = MathF.Cos(idealRotation).NonZeroSign();
            if (NPC.spriteDirection == -1)
                idealRotation += MathHelper.Pi;

            NPC.rotation = idealRotation;
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;

        public override void FindFrame(int frameHeight)
        {
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            if (HandType is null || CalamityGlobalNPC.draedonExoMechPrime == -1)
                return false;

            Texture2D texture = ModContent.Request<Texture2D>(HandType.TexturePath).Value;
            Texture2D glowmask = ModContent.Request<Texture2D>(HandType.GlowmaskPath).Value;
            Vector2 drawPosition = NPC.Center - screenPos;

            int frameX = Frame / HandType.TotalHorizontalFrames;
            int frameY = Frame % HandType.TotalHorizontalFrames;
            NPC.frame = texture.Frame(HandType.TotalHorizontalFrames, HandType.TotalVerticalFrames, frameX, frameY);

            Main.spriteBatch.Draw(texture, drawPosition, NPC.frame, NPC.GetAlpha(lightColor), NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, NPC.spriteDirection.ToSpriteDirection(), 0f);
            Main.spriteBatch.Draw(glowmask, drawPosition, NPC.frame, NPC.GetAlpha(Color.White), NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, NPC.spriteDirection.ToSpriteDirection(), 0f);

            return false;
        }

        public void DrawMagneticLine(NPC aresBody, Vector2 start, Vector2 end, float opacity = 1f)
        {
            Vector2[] controlPoints = new Vector2[8];
            for (int i = 0; i < controlPoints.Length; i++)
                controlPoints[i] = Vector2.Lerp(start, end, i / 7f);

            Vector2 distortionVelocity = (end - start).RotatedByRandom(0.4f) * 0.01f;
            ModContent.GetInstance<HeatDistortionMetaball>().CreateParticle(Vector2.Lerp(start, end, Main.rand.NextFloat(0.45f, 0.7f)), distortionVelocity, opacity * 18f);

            float magnetismWidthFunction(float completionRatio) => aresBody.Opacity * 12f;
            Color magnetismColorFunction(float completionRatio) => aresBody.GetAlpha(Color.Cyan) * opacity * 0.45f;

            PrimitivePixelationSystem.RenderToPrimsNextFrame(() =>
            {
                ManagedShader magnetismShader = ShaderManager.GetShader("WoTM.AresMagneticConnectionShader");
                magnetismShader.SetTexture(MiscTexturesRegistry.TurbulentNoise.Value, 1, SamplerState.PointWrap);

                PrimitiveSettings magnetismLineSettings = new(magnetismWidthFunction, magnetismColorFunction, Pixelate: true, Shader: magnetismShader);
                PrimitiveRenderer.RenderTrail(controlPoints, magnetismLineSettings, 24);

            }, PixelationPrimitiveLayer.BeforeNPCs);
        }

        /// <summary>
        /// Draws this hand's arm.
        /// </summary>
        /// <param name="spriteBatch"></param>
        /// <param name="screenPosition"></param>
        public void DrawArm(SpriteBatch spriteBatch, Vector2 screenPosition)
        {
            NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];

            if (UsesBackArm)
            {
                Vector2 shoulderDrawPosition = DrawBackArmShoulderAndArm(aresBody, spriteBatch, screenPosition);
                DrawBackArmForearm(aresBody, shoulderDrawPosition, spriteBatch, screenPosition);
            }
            else
            {
                Vector2 connectorDrawPosition = DrawFrontArmShoulder(aresBody, spriteBatch, screenPosition);
                Vector2 elbowDrawPosition = DrawFrontArmArm(aresBody, connectorDrawPosition, spriteBatch, screenPosition);
                DrawFrontArmForearm(aresBody, elbowDrawPosition, spriteBatch, screenPosition);
            }
        }

        /// <summary>
        /// Draws the shoulder and arm of this hand's back arm.
        /// </summary>
        /// <param name="aresBody">Ares' body NPC instance.</param>
        /// <param name="spriteBatch">The sprite batch.</param>
        /// <param name="screenPosition">The position of the screen. Used for draw offsets.</param>
        /// <returns>The end position of the arm in screen space.</returns>
        public Vector2 DrawBackArmShoulderAndArm(NPC aresBody, SpriteBatch spriteBatch, Vector2 screenPosition)
        {
            Texture2D shoulderTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresArmTopShoulder").Value;
            Texture2D shoulderTextureGlowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresArmTopShoulderGlow").Value;
            Texture2D shoulderPaddingTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresBottomArmShoulder").Value;
            Texture2D shoulderPaddingTextureGlowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresBottomArmShoulderGlow").Value;
            Vector2 shoulderDrawPosition = aresBody.Center + aresBody.scale * new Vector2(ArmSide * 164f, -54f) - screenPosition;
            Vector2 shoulderPaddingDrawPosition = aresBody.Center + aresBody.scale * new Vector2(ArmSide * 100f, -72f) - screenPosition;

            Color shoulderColor = aresBody.GetAlpha(Lighting.GetColor((shoulderDrawPosition + screenPosition).ToTileCoordinates()));
            Color shoulderPaddingColor = aresBody.GetAlpha(Lighting.GetColor((shoulderPaddingDrawPosition + screenPosition).ToTileCoordinates()));
            Rectangle shoulderFrame = shoulderTexture.Frame(1, 9, 0, (int)(Main.GlobalTimeWrappedHourly * 12f) % 9);
            Rectangle shoulderPadFrame = shoulderPaddingTexture.Frame(1, 9, 0, (int)(Main.GlobalTimeWrappedHourly * 12f) % 9);

            Vector2 armStart = shoulderDrawPosition + aresBody.scale * new Vector2(ArmSide * 22f, 2f);

            Texture2D armTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresArmTopPart1").Value;
            Texture2D forearmTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresBottomArmPart2").Value;
            Vector2 elbowDrawPosition = Utilities.CalculateElbowPosition(armStart, ArmEndpoint - screenPosition, armTexture.Width * aresBody.scale, forearmTexture.Width * aresBody.scale * 1.2f, ArmSide == -1);
            Vector2 armOrigin = armTexture.Size() * new Vector2(0.81f, 0.66f);
            float armRotation = (elbowDrawPosition - armStart).ToRotation() + MathHelper.Pi;

            if (ArmSide == 1)
            {
                armRotation += MathHelper.Pi;
                armOrigin.X = armTexture.Width - armOrigin.X;
            }

            Color armColor = aresBody.GetAlpha(Lighting.GetColor((armStart + screenPosition).ToTileCoordinates()));
            Color glowmaskColor = aresBody.GetAlpha(Color.White);
            spriteBatch.Draw(armTexture, armStart, null, armColor, armRotation, armOrigin, NPC.scale, ArmSide.ToSpriteDirection() ^ SpriteEffects.FlipHorizontally, 0f);

            spriteBatch.Draw(shoulderPaddingTexture, shoulderPaddingDrawPosition, shoulderPadFrame, shoulderPaddingColor, 0f, shoulderPadFrame.Size() * 0.5f, NPC.scale, ArmSide.ToSpriteDirection() ^ SpriteEffects.FlipHorizontally, 0f);
            spriteBatch.Draw(shoulderPaddingTextureGlowmask, shoulderPaddingDrawPosition, shoulderPadFrame, glowmaskColor, 0f, shoulderPadFrame.Size() * 0.5f, NPC.scale, ArmSide.ToSpriteDirection() ^ SpriteEffects.FlipHorizontally, 0f);

            spriteBatch.Draw(shoulderTexture, shoulderDrawPosition, shoulderFrame, shoulderColor, 0f, shoulderFrame.Size() * 0.5f, NPC.scale, ArmSide.ToSpriteDirection(), 0f);
            spriteBatch.Draw(shoulderTextureGlowmask, shoulderDrawPosition, shoulderFrame, glowmaskColor, 0f, shoulderFrame.Size() * 0.5f, NPC.scale, ArmSide.ToSpriteDirection(), 0f);

            Vector2 armEnd = armStart + armRotation.ToRotationVector2() * aresBody.scale * ArmSide * 92f;

            return armEnd;
        }

        /// <summary>
        /// Draws the forearm of this hand's front arm.
        /// </summary>
        /// <param name="aresBody">Ares' body NPC instance.</param>
        /// <param name="shoulderDrawPosition">The position of the shoulder in screen space.</param>
        /// <param name="spriteBatch">The sprite batch.</param>
        /// <param name="screenPosition">The position of the screen. Used for draw offsets.</param>
        public void DrawBackArmForearm(NPC aresBody, Vector2 shoulderDrawPosition, SpriteBatch spriteBatch, Vector2 screenPosition)
        {
            Texture2D armSegmentTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresArmTopSegment").Value;
            Texture2D armSegmentTextureGlowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresArmTopSegmentGlow").Value;
            Texture2D forearmTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresArmTopPart2").Value;
            Texture2D forearmTextureGlowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresArmTopPart2Glow").Value;
            Rectangle shoulderFrame = armSegmentTexture.Frame(1, 9, 0, (int)(Main.GlobalTimeWrappedHourly * 12f) % 9);
            Rectangle forearmFrame = forearmTexture.Frame(1, 9, 0, (int)(Main.GlobalTimeWrappedHourly * 12f) % 9);
            Vector2 forearmOrigin = forearmFrame.Size();

            Vector2 segmentDrawPosition = shoulderDrawPosition;
            Vector2 forearmDrawPosition = segmentDrawPosition;
            Color segmentColor = aresBody.GetAlpha(Lighting.GetColor((segmentDrawPosition + screenPosition).ToTileCoordinates()));
            Color glowmaskColor = aresBody.GetAlpha(Color.White);

            float segmentRotation = (ArmEndpoint - screenPosition - segmentDrawPosition).ToRotation();
            float forearmRotation = (ArmEndpoint - screenPosition - segmentDrawPosition).ToRotation() + MathHelper.Pi;
            if (ArmSide == 1)
            {
                forearmOrigin.X = forearmTexture.Width - forearmOrigin.X;
                segmentRotation += MathHelper.Pi;
                forearmRotation += MathHelper.Pi;
            }

            forearmDrawPosition += new Vector2(ArmSide * 20f, 16f).RotatedBy(forearmRotation) * aresBody.scale;

            spriteBatch.Draw(armSegmentTexture, segmentDrawPosition, shoulderFrame, segmentColor, segmentRotation, shoulderFrame.Size() * 0.5f, NPC.scale, ArmSide.ToSpriteDirection() ^ SpriteEffects.FlipHorizontally, 0f);
            spriteBatch.Draw(armSegmentTextureGlowmask, segmentDrawPosition, shoulderFrame, glowmaskColor, segmentRotation, shoulderFrame.Size() * 0.5f, NPC.scale, ArmSide.ToSpriteDirection() ^ SpriteEffects.FlipHorizontally, 0f);

            spriteBatch.Draw(forearmTexture, forearmDrawPosition, forearmFrame, segmentColor, forearmRotation, forearmOrigin, NPC.scale, ArmSide.ToSpriteDirection() ^ SpriteEffects.FlipHorizontally, 0f);
            spriteBatch.Draw(forearmTextureGlowmask, forearmDrawPosition, forearmFrame, glowmaskColor, forearmRotation, forearmOrigin, NPC.scale, ArmSide.ToSpriteDirection() ^ SpriteEffects.FlipHorizontally, 0f);

            Vector2 magnetismStart = forearmDrawPosition + Main.screenPosition - Vector2.UnitY.RotatedBy(forearmRotation) * aresBody.scale * 20f;
            Vector2 magnetismEnd = ArmEndpoint - new Vector2(ArmSide * -60f, 8f).RotatedBy(forearmRotation) * aresBody.scale;
            DrawMagneticLine(aresBody, magnetismStart, magnetismEnd, NPC.Opacity.Cubed());
        }

        /// <summary>
        /// Draws the shoulder and connector of this hand's front arm.
        /// </summary>
        /// <param name="aresBody">Ares' body NPC instance.</param>
        /// <param name="spriteBatch">The sprite batch.</param>
        /// <param name="screenPosition">The position of the screen. Used for draw offsets.</param>
        /// <returns>The position of the connector in screen space.</returns>
        public Vector2 DrawFrontArmShoulder(NPC aresBody, SpriteBatch spriteBatch, Vector2 screenPosition)
        {
            Texture2D connectorTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresBottomArmConnector").Value;
            Vector2 shoulderDrawPosition = aresBody.Center + aresBody.scale * new Vector2(ArmSide * 110f, -54f) - screenPosition;
            Vector2 connectorDrawPosition = shoulderDrawPosition + aresBody.scale * new Vector2(ArmSide * 4f, 32f);

            Color connecterColor = aresBody.GetAlpha(Lighting.GetColor((connectorDrawPosition + screenPosition).ToTileCoordinates()));
            spriteBatch.Draw(connectorTexture, connectorDrawPosition, null, connecterColor, 0f, connectorTexture.Size() * 0.5f, NPC.scale, ArmSide.ToSpriteDirection() ^ SpriteEffects.FlipHorizontally, 0f);

            return connectorDrawPosition;
        }

        /// <summary>
        /// Draws the arm of this hand's front arm.
        /// </summary>
        /// <param name="aresBody">Ares' body NPC instance.</param>
        /// <param name="connectorDrawPosition">The position of the connector in screen space.</param>
        /// <param name="spriteBatch">The sprite batch.</param>
        /// <param name="screenPosition">The position of the screen. Used for draw offsets.</param>
        /// <returns>The position of the connector in screen space.</returns>
        public Vector2 DrawFrontArmArm(NPC aresBody, Vector2 connectorDrawPosition, SpriteBatch spriteBatch, Vector2 screenPosition)
        {
            Vector2 armStart = connectorDrawPosition + aresBody.scale * new Vector2(ArmSide * 32f, -6f);

            Texture2D armTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresBottomArmPart1").Value;
            Texture2D armTextureGlowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresBottomArmPart1Glow").Value;
            Texture2D forearmTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresBottomArmPart2").Value;
            Vector2 elbowDrawPosition = Utilities.CalculateElbowPosition(armStart, ArmEndpoint - screenPosition, armTexture.Width * aresBody.scale, forearmTexture.Width * aresBody.scale * 1.2f, ArmSide == -1);
            Rectangle armFrame = armTexture.Frame(1, 9, 0, (int)(Main.GlobalTimeWrappedHourly * 12f) % 9);
            Vector2 armOrigin = armFrame.Size() * new Vector2(0.81f, 0.66f);
            float armRotation = (elbowDrawPosition - armStart).ToRotation() + MathHelper.Pi;

            if (ArmSide == 1)
            {
                armRotation += MathHelper.Pi;
                armOrigin.X = armTexture.Width - armOrigin.X;
            }

            Color armColor = aresBody.GetAlpha(Lighting.GetColor((elbowDrawPosition + screenPosition).ToTileCoordinates()));
            Color glowmaskColor = aresBody.GetAlpha(Color.White);
            spriteBatch.Draw(armTexture, armStart, armFrame, armColor, armRotation, armOrigin, NPC.scale, ArmSide.ToSpriteDirection() ^ SpriteEffects.FlipHorizontally, 0f);
            spriteBatch.Draw(armTextureGlowmask, armStart, armFrame, glowmaskColor, armRotation, armOrigin, NPC.scale, ArmSide.ToSpriteDirection() ^ SpriteEffects.FlipHorizontally, 0f);

            Vector2 magnetLineOffset = new Vector2(ArmSide * 50f, -10f).RotatedBy(armRotation) + Main.screenPosition;
            DrawMagneticLine(aresBody, armStart + magnetLineOffset, elbowDrawPosition + magnetLineOffset);

            return elbowDrawPosition;
        }

        /// <summary>
        /// Draws the forearm of this hand's front arm.
        /// </summary>
        /// <param name="aresBody">Ares' body NPC instance.</param>
        /// <param name="elbowDrawPosition">The position of the elbow in screen space.</param>
        /// <param name="spriteBatch">The sprite batch.</param>
        /// <param name="screenPosition">The position of the screen. Used for draw offsets.</param>
        public void DrawFrontArmForearm(NPC aresBody, Vector2 elbowDrawPosition, SpriteBatch spriteBatch, Vector2 screenPosition)
        {
            Vector2 armStart = elbowDrawPosition + aresBody.scale * new Vector2(ArmSide * 32f, -6f);

            Texture2D forearmTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresBottomArmPart2").Value;
            Texture2D forearmTextureGlowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresBottomArmPart2Glow").Value;
            Rectangle forearmFrame = forearmTexture.Frame(1, 9, 0, (int)(Main.GlobalTimeWrappedHourly * 12f) % 9);
            Vector2 forearmOrigin = forearmFrame.Size() * new Vector2(0.81f, 0.5f);
            float forearmRotation = (ArmEndpoint - screenPosition - armStart).ToRotation() + MathHelper.Pi;

            if (ArmSide == 1)
            {
                forearmRotation += MathHelper.Pi;
                forearmOrigin.X = forearmTexture.Width - forearmOrigin.X;
            }

            Color forearmColor = aresBody.GetAlpha(Lighting.GetColor((armStart + screenPosition).ToTileCoordinates()));
            Color glowmaskColor = aresBody.GetAlpha(Color.Wheat);
            spriteBatch.Draw(forearmTexture, armStart, forearmFrame, forearmColor, forearmRotation, forearmOrigin, NPC.scale, ArmSide.ToSpriteDirection() ^ SpriteEffects.FlipHorizontally, 0f);
            spriteBatch.Draw(forearmTextureGlowmask, armStart, forearmFrame, glowmaskColor, forearmRotation, forearmOrigin, NPC.scale, ArmSide.ToSpriteDirection() ^ SpriteEffects.FlipHorizontally, 0f);

            DrawMagneticLine(aresBody, armStart + Main.screenPosition, ArmEndpoint, NPC.Opacity.Cubed());
        }

        public override void DrawBehind(int index)
        {
            Main.instance.DrawCacheNPCProjectiles.Add(index);
        }

        public override void ModifyTypeName(ref string typeName)
        {
            if (HandType is not null)
                typeName = Language.GetTextValue(HandType.NameLocalizationKey);
        }

        // TODO -- Add on-death gores.
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.soundDelay == 0)
            {
                NPC.soundDelay = 3;
                SoundEngine.PlaySound(CommonCalamitySounds.ExoHitSound, NPC.Center);
            }
        }

        public override bool CheckActive() => false;

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
            NPC.lifeMax = (int)(NPC.lifeMax * balance * 0.8f);
            NPC.damage = (int)(NPC.damage * 0.8f);
        }
    }
}
