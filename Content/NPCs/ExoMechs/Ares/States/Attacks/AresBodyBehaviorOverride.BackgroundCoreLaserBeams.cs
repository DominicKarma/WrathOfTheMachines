using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WoTM.Content.NPCs.ExoMechs.Projectiles;

namespace WoTM.Content.NPCs.ExoMechs
{
    public sealed partial class AresBodyBehaviorOverride : NPCBehaviorOverride
    {
        /// <summary>
        /// The center position of Ares' core.
        /// </summary>
        public Vector2 CorePosition => NPC.Center + Vector2.UnitY.RotatedBy(NPC.rotation) * NPC.scale * 22f;

        /// <summary>
        /// How much damage laserbeams from Ares' core do.
        /// </summary>
        public static int CoreLaserbeamDamage => Main.expertMode ? 700 : 500;

        /// <summary>
        /// AI update loop method for the BackgroundCoreLaserBeams attack.
        /// </summary>
        public void DoBehavior_BackgroundCoreLaserBeams()
        {
            float enterBackgroundInterpolant = Utilities.InverseLerp(0f, 30f, AITimer);
            float slowDownInterpolant = Utilities.InverseLerp(54f, 60f, AITimer);
            ZPosition = enterBackgroundInterpolant * 3.7f;
            NPC.Center = Vector2.Lerp(NPC.Center, Target.Center - Vector2.UnitY * enterBackgroundInterpolant * 360f, enterBackgroundInterpolant * (1f - slowDownInterpolant) * 0.08f);
            NPC.velocity *= 0.93f;

            if (AITimer == 1 || !Utilities.AnyProjectiles(ModContent.ProjectileType<ExoOverloadDeathray>()))
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(NPC.GetSource_FromAI(), CorePosition, Vector2.Zero, ModContent.ProjectileType<ExoOverloadDeathray>(), CoreLaserbeamDamage, 0f);
            }

            BasicHandUpdateWrapper();
        }
    }
}
