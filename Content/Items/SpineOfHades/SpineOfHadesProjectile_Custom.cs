using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.DataStructures;
using CalamityMod.Particles;
using Luminance.Assets;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WoTM.Content.NPCs.ExoMechs.Projectiles;
using WoTM.Content.Particles;
using static WoTM.Content.Items.SpineOfHades.SpineOfHadesReworkManager;

namespace WoTM.Content.Items.SpineOfHades
{
    public class SpineOfHadesProjectile_Custom : ModProjectile
    {
        /// <summary>
        /// The set of all points upon which this whip's segments exist.
        /// </summary>
        public List<Vector2> WhipPoints = new(64);

        /// <summary>
        /// The starting position of this spine.
        /// </summary>
        public Vector2 Start => Owner.RotatedRelativePoint(Owner.MountedCenter + StartingDirection.ToRotationVector2() * 20f);

        /// <summary>
        /// The owner of this spine.
        /// </summary>
        public Player Owner => Main.player[Projectile.owner];

        /// <summary>
        /// The base direction of this spine.
        /// </summary>
        public ref float StartingDirection => ref Projectile.ai[0];

        /// <summary>
        /// How many animations have been performed so far.
        /// </summary>
        public ref float AnimationCounter => ref Projectile.ai[1];

        /// <summary>
        /// How long this spine has existed for, in frames.
        /// </summary>
        public ref float Time => ref Projectile.ai[2];

        /// <summary>
        /// The 0-1 interpolant which dictates how electrified this spin is.
        /// </summary>
        public ref float SuperchargeInterpolant => ref Projectile.localAI[0];

        /// <summary>
        /// The amount by which this spine should be horizontally stretched.
        /// </summary>
        public ref float HorizontalStretch => ref Projectile.localAI[1];

        public override string Texture => "CalamityMod/Projectiles/Melee/SpineOfThanatosProjectile";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 9;
        }

        public override void SetDefaults()
        {
            Projectile.width = 62;
            Projectile.height = 62;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.MaxUpdates = 1;
            Projectile.timeLeft = 900000;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = Projectile.MaxUpdates * 1;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.ownerHitCheck = true;
        }

        public override void AI()
        {
            // TODO -- Add sounds.

            CalculateWhipPoints();
            ManipulatePlayerValues();

            int animationTime = Owner.HeldMouseItem().useAnimation;
            int animationCycle = (int)AnimationCounter % 3;
            if (animationCycle == 2)
                animationTime = (int)(animationTime * 1.4f);

            float animationCompletion = Time / animationTime;
            float currentReachInterpolant = MathF.Pow(Utilities.Convert01To010(animationCompletion) + 0.001f, 4.75f);
            float currentReach = currentReachInterpolant * WhipReach;

            SuperchargeInterpolant = 0f;
            HorizontalStretch *= 0.9f;
            Projectile.scale = MathHelper.SmoothStep(0.1f, 1f, Utilities.InverseLerpBump(0f, 0.3f, 0.7f, 1f, animationCompletion));

            float swingDirection = 0;
            switch (animationCycle)
            {
                // Right swing.
                case 0:
                    swingDirection = 1;
                    break;

                // Left swing.
                case 1:
                    swingDirection = -1;
                    break;

                // Forward lunge.
                case 2:
                    currentReachInterpolant = MathF.Pow(Utilities.Convert01To010(animationCompletion) + 0.001f, 0.7f);
                    currentReach = currentReachInterpolant * LungeReach;
                    SuperchargeInterpolant = Utilities.InverseLerpBump(0.2f, 0.4f, 0.6f, 0.8f, animationCompletion);
                    HorizontalStretch = SuperchargeInterpolant * 0.2f;

                    if (MathHelper.Distance(animationCompletion, 0.5f) <= 0.07f)
                    {
                        ScreenShakeSystem.StartShakeAtPoint(Owner.Center, 2f);
                        for (int i = 0; i < 5; i++)
                            CreateArc(StartingDirection.ToRotationVector2().RotatedByRandom(0.7f), Main.rand.Next(WhipPoints), 3f);
                    }

                    break;
            }

            float swingOffsetAngle = MathHelper.SmoothStep(-SwingOffsetArc, SwingOffsetArc, animationCompletion) * swingDirection;
            Vector2 directionOffset = (StartingDirection + swingOffsetAngle).ToRotationVector2();
            Projectile.Center = Start + directionOffset * currentReach;
            Projectile.rotation = swingOffsetAngle;

            // Stay alive so long as the owner is using the item.
            // If they aren't, wait until the animation is almost done to disappear.
            if (Owner.channel || animationCompletion < 0.9f)
                Projectile.timeLeft = 2;

            // Update the starting direction at the beginning of the animation, to allow the player to use the weapon without having to click again.
            if (Main.myPlayer == Projectile.owner)
            {
                float idealDirection = Start.AngleTo(Main.MouseWorld);
                float redirectInterpolant = Utilities.InverseLerp(0.2f, 0f, animationCompletion) * 0.5f;
                StartingDirection = StartingDirection.AngleLerp(idealDirection, redirectInterpolant);

                if (redirectInterpolant > 0f)
                {
                    Projectile.netUpdate = true;
                    Projectile.netSpam = 0;
                }
            }

            // Create electricity at the end of the spine.
            if (currentReach >= 75f)
                CreateArc(Main.rand.NextVector2Unit(), WhipPoints[^1], 1f);

            Time++;
            if (Time >= animationTime)
            {
                Time = 0f;
                AnimationCounter++;
                Projectile.netUpdate = true;
            }
        }

        /// <summary>
        /// Creates a single red tesla arc.
        /// </summary>
        public void CreateArc(Vector2 direction, Vector2 spawnPosition, float arcLengthFactor)
        {
            int arcLifetime = Main.rand.Next(12, 24);
            Vector2 arcLength = direction * Main.rand.NextFloat(50f, 120f) * arcLengthFactor;
            if (Main.myPlayer == Projectile.owner)
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnPosition, arcLength, ModContent.ProjectileType<SmallTeslaArc>(), 0, 0f, -1, arcLifetime, 1f);

            for (int i = 0; i < 7; i++)
            {
                Vector2 electricityVelocity = arcLength.SafeNormalize(Vector2.Zero).RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloatDirection() * 5f + Main.rand.NextVector2Circular(0.6f, 0.6f);
                Dust electricity = Dust.NewDustPerfect(spawnPosition, 182, electricityVelocity);
                electricity.noGravity = true;
            }
        }

        /// <summary>
        /// Manipulates player hold variables, ensuring that this whip stays attached to the player.
        /// </summary>
        public void ManipulatePlayerValues()
        {
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Owner.heldProj = Projectile.whoAmI;
            Owner.ChangeDir(MathF.Cos(StartingDirection).NonZeroSign());

            float armDirection = Start.AngleTo(Projectile.Center).AngleLerp(StartingDirection, 0.75f);
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armDirection - MathHelper.PiOver2);
        }

        /// <summary>
        /// Calculates the set of whip points for this spine.
        /// </summary>
        public void CalculateWhipPoints()
        {
            Vector2[] controls = new Vector2[16];
            Vector2 end = Projectile.Center;

            float startDistanceToEnd = Start.Distance(end);
            Vector2 bezierMidpoint = Start + StartingDirection.ToRotationVector2() * startDistanceToEnd * 0.6f;

            for (int i = 0; i < 16; i++)
                controls[i] = Utilities.QuadraticBezier(Start, bezierMidpoint, end, i / 15f);

            int totalChains = (int)Utils.Clamp(startDistanceToEnd / Projectile.scale / 20f, 12, 400);
            BezierCurve bezierCurve = new(controls);

            WhipPoints = bezierCurve.GetPoints(totalChains);
        }

        /// <summary>
        /// Renders a given instance of the spin with a given angular offset and opacity.
        /// </summary>
        public void Render(float angularOffset, float opacity)
        {
            Color glowmaskColor = Color.Lerp(Color.White, Color.Crimson, SuperchargeInterpolant);
            Vector2 scale = new Vector2(1f + HorizontalStretch, 1f - HorizontalStretch) * Projectile.scale;

            for (int i = 0; i < WhipPoints.Count - 1; i++)
            {
                string whipTexturePath;
                if (i == WhipPoints.Count - 2)
                    whipTexturePath = Texture;
                else if (i == 0)
                    whipTexturePath = "CalamityMod/Projectiles/Melee/SpineOfThanatosTail";
                else
                    whipTexturePath = $"CalamityMod/Projectiles/Melee/SpineOfThanatosBody{i % 2 + 1}";
                Texture2D whipSegmentTexture = ModContent.Request<Texture2D>(whipTexturePath).Value;
                Texture2D whipSegmentGlowmaskTexture = ModContent.Request<Texture2D>($"{whipTexturePath}Glowmask").Value;

                Vector2 origin = whipSegmentTexture.Size() * 0.5f;
                float rotation = (WhipPoints[i + 1] - WhipPoints[i]).ToRotation() + MathHelper.PiOver2;
                Vector2 drawPosition = WhipPoints[i].RotatedBy(angularOffset, Start) - Main.screenPosition;
                Color color = Projectile.GetAlpha(Lighting.GetColor(WhipPoints[i].ToTileCoordinates()));
                Main.EntitySpriteDraw(whipSegmentTexture, drawPosition, null, color * opacity, rotation, origin, scale, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(whipSegmentGlowmaskTexture, drawPosition, null, glowmaskColor * opacity, rotation, origin, scale, SpriteEffects.None, 0);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            bool usingShader = SuperchargeInterpolant > 0f;
            if (usingShader)
            {
                Main.spriteBatch.PrepareForShaders();

                ManagedShader electricityShader = ShaderManager.GetShader("WoTM.SpineOfHadesSuperchargeShader");
                electricityShader.TrySetParameter("electricityColor", SuperchargeColor * SuperchargeInterpolant);
                electricityShader.TrySetParameter("electrifyInterpolant", SuperchargeInterpolant);
                electricityShader.SetTexture(MiscTexturesRegistry.DendriticNoise.Value, 1, SamplerState.LinearWrap);
                electricityShader.Apply();
            }

            try
            {
                int afterimageCount = Projectile.oldRot.Length;
                float angularVelocity = Projectile.oldRot[1].AngleBetween(Projectile.rotation);
                for (int i = afterimageCount - 1; i >= 0; i -= 2)
                {
                    float opacity = MathF.Pow(1f - i / (float)afterimageCount, 3.2f) * 0.3f;

                    float angularOffset = angularVelocity * i * -0.16f;
                    Render(angularOffset, opacity);
                }

                Render(0f, 1f);
            }
            finally
            {
                if (usingShader)
                    Main.spriteBatch.ResetToDefault();
            }

            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            ScreenShakeSystem.StartShakeAtPoint(target.Center, 2.5f, shakeStrengthDissipationIncrement: 0.45f);

            for (int i = 0; i < 2; i++)
            {
                float bloomScale = MathHelper.Lerp(0.6f, 1.4f, Utilities.Cos01(Main.GlobalTimeWrappedHourly * 90f));
                StrongBloom energy = new(target.Center + Main.rand.NextVector2Circular(10f, 10f), Vector2.Zero, Main.rand.NextBool() ? Color.OrangeRed : Color.Crimson, bloomScale, 16);
                GeneralParticleHandler.SpawnParticle(energy);

                energy = new(target.Center, Vector2.Zero, Color.Wheat, bloomScale * 0.6f, 24);
                GeneralParticleHandler.SpawnParticle(energy);

                Color sparkColor = Color.Lerp(Color.Crimson, Color.OrangeRed, Main.rand.NextFloat());
                ElectricSparkParticle spark = new(target.Center, Main.rand.NextVector2Circular(15f, 15f), Color.White, sparkColor * 0.7f, 25, Vector2.One * Main.rand.NextFloat(0.6f));
                spark.Spawn();
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 3; i++)
                {
                    int arcLifetime = Main.rand.Next(11, 19);
                    Vector2 arcLength = Main.rand.NextVector2Circular(60f, 192f).RotatedBy(Start.AngleTo(target.Center));

                    if (Main.rand.NextBool(2))
                        arcLength *= 1.35f;
                    if (Main.rand.NextBool(2))
                        arcLength *= 1.35f;
                    if (Main.rand.NextBool(4))
                        arcLength *= 1.6f;
                    if (Main.rand.NextBool(4))
                        arcLength *= 1.6f;

                    Utilities.NewProjectileBetter(Projectile.GetSource_FromThis(), target.Center, arcLength, ModContent.ProjectileType<SmallTeslaArc>(), 0, 0f, -1, arcLifetime, 1f);
                }
            }

            for (int i = 0; i < 8; i++)
            {
                int pixelLifetime = Main.rand.Next(18, 48);
                Vector2 pixelSpawnOffset = Main.rand.NextVector2Circular(120f, 120f);
                Vector2 pixelSpawnPosition = target.Center + pixelSpawnOffset;
                Vector2 pixelVelocity = pixelSpawnOffset.SafeNormalize(Vector2.Zero).RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(17f);
                BloomPixelParticle pixel = new(pixelSpawnPosition, pixelVelocity, Color.White, Color.Red * 0.5f, pixelLifetime, Vector2.One * Main.rand.NextFloat(0.8f, 1.7f));
                pixel.Spawn();
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (WhipPoints.Count <= 1)
                return false;

            float width = Projectile.scale * 38f;
            for (int i = 0; i < WhipPoints.Count - 1; i++)
            {
                float _ = 0f;
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), WhipPoints[i], WhipPoints[i + 1], width, ref _))
                    return true;
            }
            return false;
        }
    }
}
