using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.DataStructures;
using CalamityMod.Particles;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WoTM.Content.NPCs.ExoMechs.Projectiles;
using WoTM.Content.Particles;

namespace WoTM.Content.Items.RefractionRotor
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

        public ref float StartingDirection => ref Projectile.ai[0];

        /// <summary>
        /// How long this spine has existed for, in frames.
        /// </summary>
        public ref float Time => ref Projectile.ai[2];

        public override string Texture => "CalamityMod/Projectiles/Melee/SpineOfThanatosProjectile";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20;
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
            CalculateWhipPoints();
            ManipulatePlayerValues();

            int animationTime = Owner.HeldMouseItem().useAnimation;
            int animationCounter = (int)Time / animationTime;
            float animationCompletion = Time % animationTime / animationTime;
            float swingDirection = animationCounter % 2 == 0 ? 1f : -1f;

            float currentReachInterpolant = MathF.Pow(Utilities.Convert01To010(animationCompletion) + 0.001f, 4.75f);
            float currentReach = currentReachInterpolant * 680f;
            float swingOffsetArc = 1.1f;
            float swingOffsetAngle = MathHelper.SmoothStep(-swingOffsetArc, swingOffsetArc, animationCompletion) * swingDirection;

            Vector2 directionOffset = (StartingDirection + swingOffsetAngle).ToRotationVector2();
            Projectile.Center = Start + directionOffset * currentReach;
            Projectile.rotation = swingOffsetAngle;
            Projectile.scale = MathHelper.SmoothStep(0.1f, 1f, Utilities.InverseLerpBump(0f, 0.3f, 0.7f, 1f, animationCompletion));

            if (Owner.channel || animationCompletion < 0.9f)
                Projectile.timeLeft = 2;

            if (Main.myPlayer == Projectile.owner)
            {
                float idealDirection = Start.AngleTo(Main.MouseWorld);
                float redirectInterpolant = Utilities.InverseLerp(0.2f, 0f, animationCompletion) * 0.5f;
                StartingDirection = StartingDirection.AngleLerp(idealDirection, redirectInterpolant);
            }

            Time++;
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

        public void Render(float angularOffset, float opacity)
        {
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
                Color color = Projectile.GetAlpha(Lighting.GetColor((int)WhipPoints[i].X / 16, (int)WhipPoints[i].Y / 16));
                Main.EntitySpriteDraw(whipSegmentTexture, drawPosition, null, color * opacity, rotation, origin, Projectile.scale, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(whipSegmentGlowmaskTexture, drawPosition, null, Color.White * opacity, rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }
        }

        public override bool PreDraw(ref Color lightColor)
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
