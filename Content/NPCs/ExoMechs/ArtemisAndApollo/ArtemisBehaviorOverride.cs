using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace DifferentExoMechs.Content.NPCs.Bosses
{
    public sealed partial class ArtemisBehaviorOverride : NPCBehaviorOverride, IExoTwin
    {
        /// <summary>
        /// Apollo's current AI timer.
        /// </summary>
        public int AITimer
        {
            get;
            set;
        }

        /// <summary>
        /// Apollo's current frame.
        /// </summary>
        public int Frame
        {
            get;
            set;
        }

        /// <summary>
        /// Whether Artemis has fully entered her second phase yet or not.
        /// </summary>
        public bool InPhase2
        {
            get;
            set;
        }

        /// <summary>
        /// Artemis's current animation.
        /// </summary>
        public ExoTwinAnimation Animation
        {
            get;
            set;
        } = ExoTwinAnimation.Idle;

        public override int NPCOverrideID => ExoMechNPCIDs.ArtemisID;

        public override void AI()
        {
            CalamityGlobalNPC.draedonExoMechTwinRed = NPC.whoAmI;
            NPC.Opacity = 1f;
            AITimer++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            Texture2D glowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Artemis/ArtemisGlow").Value;
            CommonExoTwinFunctionalities.DrawBase(NPC, glowmask, lightColor, screenPos, Frame);
            return false;
        }
    }
}
