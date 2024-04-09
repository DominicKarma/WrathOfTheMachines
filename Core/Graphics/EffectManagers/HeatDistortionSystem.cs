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
            ManagedScreenFilter distortion = ShaderManager.GetFilter("WoTM.HeatDistortionFilter");

            bool shouldBeActive = ModContent.GetInstance<HeatDistortionMetaball>().ActiveParticleCount >= 1;
            if (!distortion.IsActive && shouldBeActive)
            {
                distortion.TrySetParameter("screenZoom", Main.GameViewMatrix.Zoom);
                distortion.SetTexture(ModContent.GetInstance<HeatDistortionMetaball>().LayerTargets[0], 2, SamplerState.LinearClamp);
                distortion.SetTexture(MiscTexturesRegistry.DendriticNoise.Value, 3, SamplerState.LinearWrap);
                distortion.Activate();
            }
        }
    }
}
