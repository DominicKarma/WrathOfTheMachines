﻿using System;
using CalamityMod.NPCs.ExoMechs.Artemis;
using Luminance.Assets;
using Luminance.Common.DataStructures;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WoTM.Content.NPCs.ExoMechs.Projectiles
{
    public class ApolloLineTelegraph : ModProjectile, IPixelatedPrimitiveRenderer, IProjOwnedByBoss<Artemis>, IExoMechProjectile
    {
        public PixelationPrimitiveLayer LayerToRenderTo => PixelationPrimitiveLayer.BeforeNPCs;

        /// <summary>
        /// How long this telegraph should exist for, in frames.
        /// </summary>
        public int Lifetime => (int)Projectile.ai[0];

        /// <summary>
        /// How long this telegraph has existed, in frames.
        /// </summary>
        public ref float Time => ref Projectile.ai[1];

        /// <summary>
        /// How far this telegraph extends.
        /// </summary>
        public static float TelegraphLength => 3700f;

        public override string Texture => MiscTexturesRegistry.InvisiblePixelPath;

        public ExoMechDamageSource DamageType => ExoMechDamageSource.Plasma;

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 900;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Time++;

            Projectile.Opacity = Utilities.InverseLerpBump(0f, 0.32f, 0.75f, 1f, Time / Lifetime);

            if (Time >= Lifetime)
                Projectile.Kill();
        }

        public float TelegraphWidthFunction(float completionRatio)
        {
            return (1f - Projectile.Opacity) * 100f + MathF.Cos(Main.GlobalTimeWrappedHourly * 20f) * 4f + 24f;
        }

        public Color TelegraphColorFunction(float completionRatio)
        {
            return new Color(0.49f, 0.9f, 0.04f) * Projectile.Opacity;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
            var telegraphPoints = Projectile.GetLaserControlPoints(12, TelegraphLength);

            ManagedShader telegraphShader = ShaderManager.GetShader("WoTM.ApolloLineTelegraphShader");
            telegraphShader.Apply();

            PrimitiveSettings settings = new(TelegraphWidthFunction, TelegraphColorFunction, Pixelate: true, Shader: telegraphShader);
            PrimitiveRenderer.RenderTrail(telegraphPoints, settings, 28);
        }
    }
}
