using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace DifferentExoMechs.Content.NPCs.ExoMechs
{
    public class CustomExoMechsColorationSystem : ModSystem
    {
        public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
        {
            backgroundColor = Color.Lerp(backgroundColor, new(72, 72, 92), CustomExoMechsSky.Opacity);
            tileColor = Color.Lerp(backgroundColor, new(72, 72, 92), CustomExoMechsSky.Opacity * 0.85f);

            float redSirenInterpolant = CustomExoMechsSky.RedSirensIntensity;
            backgroundColor = Color.Lerp(backgroundColor, new(255, 79, 72), redSirenInterpolant);
            tileColor = Color.Lerp(tileColor, new(206, 97, 95), redSirenInterpolant);
        }
    }
}
