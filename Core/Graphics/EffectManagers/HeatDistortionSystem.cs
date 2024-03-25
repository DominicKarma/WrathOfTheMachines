using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using WoTM.Content.Particles.Metaballs;

namespace WoTM.Core.Graphics.EffectManagers
{
    [Autoload(Side = ModSide.Client)]
    public class HeatDistortionSystem : ModSystem
    {
        public override void OnModLoad() => On_FilterManager.EndCapture += SupplyHeatDistortionTextures;

        private void SupplyHeatDistortionTextures(On_FilterManager.orig_EndCapture orig, FilterManager self, RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Color clearColor)
        {
            if (ShaderManager.TryGetFilter("WoTM.HeatDistortionFilter", out ManagedScreenFilter distortion) && distortion.IsActive)
            {
                distortion.WrappedEffect.Parameters["screenZoom"]?.SetValue(Main.GameViewMatrix.Zoom);
                Main.instance.GraphicsDevice.Textures[2] = ModContent.GetInstance<HeatDistortionMetaball>().LayerTargets[0];
                Main.instance.GraphicsDevice.SamplerStates[2] = SamplerState.LinearClamp;
                Main.instance.GraphicsDevice.Textures[3] = MiscTexturesRegistry.DendriticNoise.Value;
                Main.instance.GraphicsDevice.SamplerStates[3] = SamplerState.LinearWrap;
            }

            orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
        }

        public override void PostUpdateEverything()
        {
            ManagedScreenFilter distortion = ShaderManager.GetFilter("WoTM.HeatDistortionFilter");

            bool shouldBeActive = ModContent.GetInstance<HeatDistortionMetaball>().ActiveParticleCount >= 1;
            if (!distortion.IsActive && shouldBeActive)
                distortion.Activate();
            if (distortion.IsActive && !shouldBeActive)
                distortion.Deactivate();
        }
    }
}
