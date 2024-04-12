using System.Collections.Generic;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using Luminance.Assets;
using Luminance.Common.DataStructures;
using Luminance.Common.Easings;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WoTM.Content.NPCs.ExoMechs.Projectiles
{
    public class ExoEnergyBlast : ModProjectile, IPixelatedPrimitiveRenderer, IProjOwnedByBoss<AresBody>, IExoMechProjectile
    {
        public PixelationPrimitiveLayer LayerToRenderTo => PixelationPrimitiveLayer.AfterProjectiles;

        /// <summary>
        /// How overheated the beam is. Brings colors from cool electric cyans and blues to terrifying reds.
        /// </summary>
        public ref float OverheatInterpolant => ref Projectile.localAI[0];

        /// <summary>
        /// How long this sphere has existed, in frames.
        /// </summary>
        public ref float Time => ref Projectile.ai[1];

        /// <summary>
        /// How long this laserbeam current is.
        /// </summary>
        public ref float LaserbeamLength => ref Projectile.ai[2];

        /// <summary>
        /// How long the explosion lasts.
        /// </summary>
        public static int Lifetime => Utilities.SecondsToFrames(3f);

        /// <summary>
        /// The maximum length of this laserbeam.
        /// </summary>
        public static float MaxLaserbeamLength => 4600f;

        /// <summary>
        /// The starting <see cref="Projectile.timeLeft"/> where overheating begins for the beam.
        /// </summary>
        public static int OverheatStartingTime => Utilities.SecondsToFrames(2.5f);

        /// <summary>
        /// The starting <see cref="Projectile.timeLeft"/> where overheating ends for the beam.
        /// </summary>
        public static int OverheatEndingTime => Utilities.SecondsToFrames(2f);

        /// <summary>
        /// How long the beam waits before beginning to expand.
        /// </summary>
        public static int ExpandDelay => Utilities.SecondsToFrames(0.0667f);

        /// <summary>
        /// How long the beam spends expanding.
        /// </summary>
        public static int ExpandTime => Utilities.SecondsToFrames(0.2f);

        public override string Texture => MiscTexturesRegistry.InvisiblePixelPath;

        public ExoMechDamageSource DamageType => ExoMechDamageSource.Electricity;

        public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 6000;

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = Lifetime;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            int hadesIndex = CalamityGlobalNPC.draedonExoMechWorm;
            if (hadesIndex < 0 || hadesIndex >= Main.maxNPCs || Main.npc[hadesIndex].type != ModContent.NPCType<ThanatosHead>())
            {
                Projectile.Kill();
                return;
            }

            NPC hades = Main.npc[hadesIndex];
            Vector2 beamStart = hades.Center + hades.velocity.SafeNormalize(Vector2.UnitY) * 14f;
            Projectile.Center = beamStart;
            Projectile.velocity = hades.SafeDirectionTo(beamStart);
            LaserbeamLength = MathHelper.Clamp(LaserbeamLength + 189f, 0f, MaxLaserbeamLength);

            float expandInterpolant = Utilities.InverseLerp(0f, ExpandTime, Time - ExpandDelay);
            float bigWidth = MathHelper.Lerp(220f, 380f, OverheatInterpolant);
            Projectile.width = (int)(MathHelper.Lerp(Time / 42f * 8f, bigWidth, expandInterpolant.Squared()) * Utilities.InverseLerp(0f, 10f, Projectile.timeLeft));

            CreateVisuals(expandInterpolant);

            Time++;
        }

        /// <summary>
        /// Handles various visuals for this blast, such as calculating the overheat, creating sparks, making the screen shake, etc.
        /// </summary>
        /// <param name="expandInterpolant"></param>
        public void CreateVisuals(float expandInterpolant)
        {
            OverheatInterpolant = Utilities.InverseLerp(134f, 90f, Projectile.timeLeft);

            // Darken the sky to increase general contrast with everything.
            CustomExoMechsSky.CloudExposure = MathHelper.Lerp(CustomExoMechsSky.DefaultCloudExposure, 0.085f, expandInterpolant);

            for (int i = 0; i < (1f - OverheatInterpolant) * Projectile.width / 21; i++)
            {
                float laserbeamLengthInterpolant = Main.rand.NextFloat(0.07f, 1f);
                Vector2 randomLinePosition = Projectile.Center + Projectile.velocity * laserbeamLengthInterpolant * LaserbeamLength + Main.rand.NextVector2CircularEdge(Projectile.width, Projectile.width) * 0.5f;
                CreateElectricSpark(randomLinePosition);
            }
            if (Main.rand.NextBool((1f - OverheatInterpolant) * Projectile.width / 300f))
                CreateElectricSpark(Projectile.Center + Projectile.velocity * 20f);

            ScreenShakeSystem.SetUniversalRumble(Projectile.width / 120f);

            if (Time % 14f == 13f)
                CustomExoMechsSky.CreateLightning(Projectile.Center.ToScreenPosition());
        }

        /// <summary>
        /// Creates a single electric spark around the sphere's edge.
        /// </summary>
        public void CreateElectricSpark(Vector2 arcSpawnPosition)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int arcLifetime = Main.rand.Next(10, 19);
            Vector2 arcLength = Projectile.SafeDirectionTo(arcSpawnPosition).RotatedByRandom(MathHelper.PiOver2) * Main.rand.NextFloat(120f, Projectile.width * 0.034f + 150f);

            if (Main.rand.NextBool(2))
                arcLength *= 1.35f;
            if (Main.rand.NextBool(2))
                arcLength *= 1.35f;
            if (Main.rand.NextBool(4))
                arcLength *= 1.6f;
            if (Main.rand.NextBool(4))
                arcLength *= 1.6f;

            Utilities.NewProjectileBetter(Projectile.GetSource_FromThis(), arcSpawnPosition, arcLength, ModContent.ProjectileType<SmallTeslaArc>(), 0, 0f, -1, arcLifetime, 0f);
        }

        public float LaserWidthFunction(float completionRatio)
        {
            float frontExpansionInterpolant = Utilities.InverseLerp(0.015f, 0.21f, completionRatio);
            float maxSize = Projectile.width + completionRatio * Projectile.width * 1.2f;
            return EasingCurves.Quadratic.Evaluate(EasingType.Out, 2f, maxSize, frontExpansionInterpolant);
        }

        public Color LaserColorFunction(float completionRatio)
        {
            Color electricColor = new(0.4f, 1f, 1f);
            Color overheatColor = new(1f, 0.11f, 0.17f);
            return Projectile.GetAlpha(Color.Lerp(electricColor, overheatColor, OverheatInterpolant));
        }

        public float BloomWidthFunction(float completionRatio) => LaserWidthFunction(completionRatio) * 1.5f;

        public Color BloomColorFunction(float completionRatio)
        {
            Color electricColor = new(0.67f, 0.7f, 1f, 0f);
            Color overheatColor = new(1f, 0.3f, 0f, 0f);

            float opacity = Utilities.InverseLerp(0.01f, 0.065f, completionRatio) * 0.32f;
            return Projectile.GetAlpha(Color.Lerp(electricColor, overheatColor, OverheatInterpolant)) * opacity;
        }

        public void DrawBackBloom()
        {
            Color outerBloomColor = Color.Lerp(new(0.34f, 0.75f, 1f, 0f), new(1f, 0f, 0f, 0f), OverheatInterpolant);

            float bloomScale = Projectile.width / 300f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + Projectile.velocity * Projectile.width * 0.21f;
            Texture2D bloom = MiscTexturesRegistry.BloomCircleSmall.Value;
            Main.spriteBatch.Draw(bloom, drawPosition, null, Projectile.GetAlpha(new(1f, 1f, 1f, 0f)) * 0.5f, 0f, bloom.Size() * 0.5f, bloomScale * 2f, 0, 0f);
            Main.spriteBatch.Draw(bloom, drawPosition, null, Projectile.GetAlpha(outerBloomColor) * 0.4f, 0f, bloom.Size() * 0.5f, bloomScale * 5f, 0, 0f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrawBackBloom();
            List<Vector2> laserPositions = Projectile.GetLaserControlPoints(12, LaserbeamLength);

            ManagedShader shader = ShaderManager.GetShader("WoTM.PrimitiveBloomShader");
            PrimitiveSettings bloomSettings = new(BloomWidthFunction, BloomColorFunction, Shader: shader);
            PrimitiveRenderer.RenderTrail(laserPositions, bloomSettings, 60);

            return false;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
            List<Vector2> laserPositions = Projectile.GetLaserControlPoints(12, LaserbeamLength);

            ManagedShader shader = ShaderManager.GetShader("WoTM.HadesExoEnergyBlastShader");
            shader.TrySetParameter("laserDirection", Projectile.velocity);
            shader.TrySetParameter("edgeColorSubtraction", Vector3.Lerp(new(0.7f, 0.4f, 0), new(0f, 0.5f, 1f), OverheatInterpolant));
            shader.TrySetParameter("edgeGlowIntensity", MathHelper.Lerp(0.2f, 1f, OverheatInterpolant));
            shader.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 1, SamplerState.LinearWrap);

            PrimitiveSettings laserSettings = new(LaserWidthFunction, LaserColorFunction, Pixelate: true, Shader: shader);
            PrimitiveRenderer.RenderTrail(laserPositions, laserSettings, 60);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (targetHitbox.Distance(Projectile.Center) <= 150f)
                return false;

            float _ = 0f;
            float laserWidth = Projectile.width * 0.8f;
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity.SafeNormalize(Vector2.Zero) * LaserbeamLength;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, laserWidth, ref _);
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
