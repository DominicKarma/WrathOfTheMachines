using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using WoTM.Content.Particles.Metaballs;

namespace WoTM.Core.Graphics.EffectManagers
{
    [Autoload(Side = ModSide.Client)]
    public class HeatDistortionSystem : ModSystem
    {
        public override void PostUpdateEverything()
        {
            ManagedScreenFilter distortionShader = ShaderManager.GetFilter("WoTM.HeatDistortionFilter");

            bool shouldBeActive = ModContent.GetInstance<HeatDistortionMetaball>().ActiveParticleCount >= 1;
            if (!distortionShader.IsActive && shouldBeActive)
                ApplyDistortionParameters(distortionShader);
        }

        private static void ApplyDistortionParameters(ManagedScreenFilter distortionShader)
        {
            distortionShader.TrySetParameter("screenZoom", Main.GameViewMatrix.Zoom);
            distortionShader.SetTexture(ModContent.GetInstance<HeatDistortionMetaball>().LayerTargets[0], 2, SamplerState.LinearClamp);
            distortionShader.SetTexture(MiscTexturesRegistry.DendriticNoise.Value, 3, SamplerState.LinearWrap);
            distortionShader.Activate();
        }
    }
}
