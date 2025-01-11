using System.Collections.Generic;
using System.IO;
using System.Linq;
using Luminance.Assets;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using WoTM.Content.NPCs.ExoMechs.Projectiles;

namespace WoTM.Content.Items.TheAtomSplitter
{
    public class TheAtomSplitterProjectile_Custom : ModProjectile
    {
        /// <summary>
        /// The position of the portal that this spear will enter.
        /// </summary>
        public Vector2 PortalPosition
        {
            get;
            set;
        }

        /// <summary>
        /// Whether this spear has played the portal enter sound.
        /// </summary>
        public bool HasPlayedPortalEnterSound
        {
            get;
            set;
        }

        /// <summary>
        /// The scale of the portal.
        /// </summary>
        public float PortalScale => Utilities.InverseLerpBump(2f, 17f, Lifetime - 16f, Lifetime, Time) + Utilities.InverseLerp(260f, 100f, Projectile.Distance(PortalPosition)) * -0.07f;

        /// <summary>
        /// How long this spear has existed for, in frames.
        /// </summary>
        public ref float Time => ref Projectile.ai[1];

        /// <summary>
        /// How long this spear (and by extension its portal) should exist before dying.
        /// </summary>
        public static int Lifetime => 54;

        /// <summary>
        /// The sound this spear plays when warping.
        /// </summary>
        public static readonly SoundStyle WarpSound = new("WoTM/Assets/Sounds/Custom/ItemReworks/AtomSplitterWarp", 2);

        public override string Texture => "CalamityMod/Items/Weapons/Rogue/TheAtomSplitter";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 3700;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 42;
        }

        public override void SetDefaults()
        {
            Projectile.width = 120;
            Projectile.height = 120;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.MaxUpdates = 1;
            Projectile.timeLeft = Projectile.MaxUpdates * Lifetime;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = Projectile.MaxUpdates * 3;
            Projectile.DamageType = ModContent.GetInstance<CalamityMod.RogueDamageClass>();
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.WriteVector2(PortalPosition);

        public override void ReceiveExtraAI(BinaryReader reader) => PortalPosition = reader.ReadVector2();

        public override void AI()
        {
            Projectile.scale = Utilities.InverseLerp(0f, 3f, Time);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            // Accelerate forward.
            float incrementalAcceleration = Utilities.InverseLerp(1f, 15f, Time).Squared() * 1.3f;
            Projectile.velocity = (Projectile.velocity * 1.1f + Projectile.velocity.SafeNormalize(Vector2.Zero) * incrementalAcceleration).ClampLength(0f, 128f);

            // Initialize the portal's position.
            if (PortalPosition == Vector2.Zero)
            {
                PortalPosition = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * 600f;
                Projectile.netUpdate = true;
            }

            int portalElectricityCount = 1;
            bool enteringPortal = Projectile.WithinRange(PortalPosition, 180f);
            if (enteringPortal)
            {
                portalElectricityCount += 5;
                if (!HasPlayedPortalEnterSound)
                {
                    SoundEngine.PlaySound(WarpSound with { MaxInstances = 0, Volume = 0.9f }, Projectile.Center);
                    ScreenShakeSystem.StartShakeAtPoint(Projectile.Center, 5f, shakeStrengthDissipationIncrement: 0.6f);
                    HasPlayedPortalEnterSound = true;
                }
            }

            if (Main.myPlayer == Projectile.owner && Main.rand.NextBool())
            {
                Vector2 electricityDirection = Main.rand.NextVector2Unit().RotateTowards(Projectile.velocity.ToRotation() + MathHelper.Pi, 0.8f);
                for (int i = 0; i < portalElectricityCount; i++)
                {
                    Vector2 arcLength = electricityDirection * Main.rand.NextFloat(80f, PortalScale * 250f + 250f) * MathHelper.Lerp(0.3f, 1f, PortalScale);
                    Utilities.NewProjectileBetter(Projectile.GetSource_FromThis(), PortalPosition, arcLength, ModContent.ProjectileType<SmallTeslaArc>(), 0, 0f, Projectile.owner, 11);
                }
            }

            Time++;
        }

        /// <summary>
        /// Renders this spear.
        /// </summary>
        public void RenderSpear(Color lightColor)
        {
            float[] blurWeights = new float[12];
            for (int i = 0; i < blurWeights.Length; i++)
                blurWeights[i] = Utilities.GaussianDistribution(i / (float)(blurWeights.Length - 1f) * 2f, 1.75f);

            ManagedShader shader = ShaderManager.GetShader("WoTM.AtomSplitterSpearShader");
            shader.TrySetParameter("blurInterpolant", Utilities.InverseLerp(10f, 70f, Projectile.velocity.Length()) * 2.5f);
            shader.TrySetParameter("blurWeights", blurWeights);
            shader.TrySetParameter("blurDirection", new Vector2(0.707f, -0.707f));
            shader.TrySetParameter("portalPosition", Vector2.Transform(PortalPosition - Main.screenPosition, Main.GameViewMatrix.TransformationMatrix));
            shader.TrySetParameter("portalDirection", -Projectile.velocity.SafeNormalize(Vector2.Zero));
            shader.Apply();

            Texture2D texture = TextureAssets.Projectile[Type].Value;

            for (int i = 8; i >= 0; i--)
            {
                Vector2 drawPosition = Projectile.Center - Main.screenPosition - Projectile.velocity * i * 0.19f;
                Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(lightColor) * (1f - i / 9f).Squared(), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0);
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.myPlayer == Projectile.owner)
            {
                List<NPC> potentialTargets = [];
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (npc.CanBeChasedBy())
                        potentialTargets.Add(npc);
                }

                potentialTargets = potentialTargets.OrderBy(n =>
                {
                    return -Projectile.SafeDirectionTo(n.Center).AngleBetween(Projectile.velocity) + PortalPosition.Distance(n.Center) * 0.015f;
                }).ToList();
                int preferredTargetIndex = potentialTargets.FirstOrDefault()?.whoAmI ?? -1;

                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Main.player[Projectile.owner].Center, Vector2.Zero, ModContent.ProjectileType<AtomSplitterSpamSource>(), Projectile.damage, 0f, Projectile.owner, preferredTargetIndex);
            }
        }

        /// <summary>
        /// Renders this spear's portal.
        /// </summary>
        public void RenderPortal()
        {
            Vector2 portalSize = new(200f, 600f);
            Vector2 drawPosition = PortalPosition - Main.screenPosition;

            if (PortalScale <= 0f)
                return;

            ManagedShader portalShader = ShaderManager.GetShader("WoTM.AtomSplitterPortalShader");
            portalShader.TrySetParameter("useTextureForDistanceField", false);
            portalShader.TrySetParameter("textureSize0", portalSize * 2f);
            portalShader.TrySetParameter("scale", PortalScale);
            portalShader.TrySetParameter("biasToMainSwirlColorPower", MathHelper.Lerp(1.5f, 4.2f, Projectile.identity / 7f % 1f));
            portalShader.TrySetParameter("mainSwirlColor", new Vector3(0.65f, 1.56f, 0.95f));
            portalShader.TrySetParameter("secondarySwirlColor", new Vector3(0f, 2.1f, 4.2f));
            portalShader.SetTexture(MiscTexturesRegistry.DendriticNoiseZoomedOut.Value, 1, SamplerState.LinearWrap);
            portalShader.Apply();

            Texture2D pixel = MiscTexturesRegistry.Pixel.Value;
            Main.spriteBatch.Draw(pixel, drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.AngleFrom(PortalPosition), pixel.Size() * 0.5f, portalSize / pixel.Size() * 1.2f, 0, 0f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.PrepareForShaders();
            RenderPortal();
            RenderSpear(lightColor);
            Main.spriteBatch.ResetToDefault();

            return false;
        }
    }
}
