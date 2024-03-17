using CalamityMod.Particles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DifferentExoMechs
{
    public class DifferentExoMechs : Mod
    {
        public override void Load()
        {
            if (Main.netMode != NetmodeID.Server)
                GeneralParticleHandler.LoadModParticleInstances(this);
        }
    }
}
