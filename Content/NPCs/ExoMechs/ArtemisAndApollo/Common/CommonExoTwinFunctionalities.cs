using System;
using System.Collections.Generic;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace DifferentExoMechs.Content.NPCs.Bosses
{
    public static class CommonExoTwinFunctionalities
    {
        /// <summary>
        /// The target that Artemis and Apollo will attempt to attack.
        /// </summary>
        public static Player Target => ExoMechTargetSelector.Target;

        private static float NerveEndingWidthFunction(float completionRatio)
        {
            float baseWidth = Utilities.InverseLerp(1f, 0.54f, completionRatio) * 6f;
            float endTipWidth = Utilities.Convert01To010(Utilities.InverseLerp(0.96f, 0.83f, completionRatio)) * 6f;
            return baseWidth + endTipWidth;
        }

        /// <summary>
        /// Draws nerve endings for a given Exo Twin.
        /// </summary>
        /// <param name="twin">The Exo Twin's NPC instance.</param>
        /// <param name="nerveEndingPalette">The palette for the nerve endings.</param>
        public static void DrawNerveEndings(NPC twin, params Color[] nerveEndingPalette)
        {
            Color nerveEndingColorFunction(float completionRatio)
            {
                float blackInterpolant = Utilities.InverseLerp(0.17f, 0.34f, completionRatio);
                Color paletteColor = Utilities.MulticolorLerp(completionRatio.Squared(), nerveEndingPalette);
                return Color.Lerp(Color.Black, paletteColor, blackInterpolant);
            }

            ManagedShader nerveEndingShader = ShaderManager.GetShader("ExoTwinNerveEndingShader");
            nerveEndingShader.SetTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/Neurons"), 1, SamplerState.LinearWrap);

            // Draw nerve endings near the main thruster
            for (int direction = -1; direction <= 1; direction += 2)
            {
                Vector2 backwards = -twin.rotation.ToRotationVector2();
                List<Vector2> ribbonDrawPositions = new();
                for (int i = 0; i < 12; i++)
                {
                    float angularChange = MathHelper.WrapAngle(twin.rotation - twin.oldRot[i]);

                    float ribbonCompletionRatio = i / 11f;
                    float inwardBendInterpolant = MathF.Pow(Utilities.InverseLerp(0f, 0.38f, ribbonCompletionRatio) * ribbonCompletionRatio, 1.2f);
                    float outwardExtrusion = MathHelper.Lerp(32f, 6f, inwardBendInterpolant);
                    Vector2 ribbonSegmentOffset = backwards.RotatedBy(angularChange * -0.8f) * ribbonCompletionRatio * 540f;
                    Vector2 ribbonOffset = new Vector2(direction * outwardExtrusion, -30f).RotatedBy(twin.oldRot[i] + MathHelper.PiOver2);

                    ribbonDrawPositions.Add(twin.Center + ribbonSegmentOffset + ribbonOffset);
                }

                PrimitiveSettings settings = new(NerveEndingWidthFunction, nerveEndingColorFunction, Shader: nerveEndingShader);
                PrimitiveRenderer.RenderTrail(ribbonDrawPositions, settings, 41);
            }
        }

        /// <summary>
        /// Draws an Exo Twin's barest things.
        /// </summary>
        /// <param name="twin">The Exo Twin's NPC data.</param>
        /// <param name="glowmask">The glowmask texture.</param>
        /// <param name="lightColor">The color of light at the Exo Twin's position.</param>
        /// <param name="screenPos">The screen position offset.</param>
        /// <param name="frame">The frame of the Exo Twin.</param>
        public static void DrawBase(NPC twin, Texture2D glowmask, Color lightColor, Vector2 screenPos, int frame)
        {
            DrawNerveEndings(twin, [new(28, 58, 60), new(62, 105, 80), new(108, 167, 94), new(144, 246, 100), new(81, 126, 85)]);

            Texture2D texture = TextureAssets.Npc[twin.type].Value;
            Vector2 drawPosition = twin.Center - screenPos;
            Rectangle frameRectangle = texture.Frame(10, 9, frame / 9, frame % 9);

            // The sheet appears to be malformed? Not doing this causes single-frame jitters when updating the horizontal frame.
            frameRectangle.X += frame / 9;

            Main.spriteBatch.Draw(texture, drawPosition, frameRectangle, twin.GetAlpha(lightColor), twin.rotation + MathHelper.PiOver2, frameRectangle.Size() * 0.5f, twin.scale, 0, 0f);
            Main.spriteBatch.Draw(glowmask, drawPosition, frameRectangle, twin.GetAlpha(Color.White), twin.rotation + MathHelper.PiOver2, frameRectangle.Size() * 0.5f, twin.scale, 0, 0f);
        }
    }
}
