using Terraria.ModLoader;

namespace DifferentExoMechs
{
    public class DifferentExoMechs : Mod
    {
        // Reminder for self: Don't create an Instance property in this class, use ModContent.GetInstance.
        public override void Load()
        {
            Luminance.Luminance.InitializeMod(this);
        }
    }
}
