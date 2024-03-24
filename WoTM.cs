using Terraria.ModLoader;

namespace WoTM
{
    public class WoTM : Mod
    {
        // Reminder for self: Don't create an Instance property in this class, use ModContent.GetInstance.
        public override void Load()
        {
            Luminance.Luminance.InitializeMod(this);
        }
    }
}
