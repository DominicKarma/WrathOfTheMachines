using Microsoft.Xna.Framework.Graphics;

namespace DifferentExoMechs.Content.NPCs.Bosses
{
    public sealed class ThanatosHeadBehaviorOverride : NPCBehaviorOverride
    {
        public enum ThanatosAIState
        {

        }

        public override int NPCOverrideID => ExoMechNPCIDs.ThanatosHeadID;

        public override void AI()
        {

        }

        public override bool PreDraw(SpriteBatch spriteBatch)
        {
            return false;
        }
    }
}
