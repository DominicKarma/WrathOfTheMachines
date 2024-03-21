using CalamityMod.NPCs.ExoMechs.Artemis;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;

namespace DifferentExoMechs.Content.NPCs.ExoMechs
{
    public static partial class ExoTwinsStates
    {
        /// <summary>
        /// AI update loop method for the spawn animation of the Exo Twins.
        /// </summary>
        /// <param name="npc">The Exo Twin's NPC instance.</param>
        /// <param name="twinAttributes">The Exo Twin's designated generic attributes.</param>
        public static void DoBehavior_SpawnAnimation(NPC npc, IExoTwin twinAttributes)
        {
            npc.dontTakeDamage = true;

            if (AITimer == 90f)
                SoundEngine.PlaySound(Artemis.AttackSelectionSound);

            if (AITimer >= 150f)
            {
                SharedState.Reset();
                SharedState.AIState = ExoTwinsAIState.DashesAndLasers;
            }

            if (AITimer == 1f)
            {
                npc.velocity = Vector2.UnitY.RotatedByRandom(0.3f) * 120f;
                npc.netUpdate = true;

                ScreenShakeSystem.StartShake(20f);
                SoundEngine.PlaySound(Artemis.ChargeSound);
            }

            if (AITimer >= 12f)
                npc.velocity *= 0.9f;
            if (AITimer <= 10f)
                npc.rotation = npc.velocity.ToRotation();
            if (AITimer >= 60f)
                npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(Target.Center), 0.18f);

            twinAttributes.Animation = ExoTwinAnimation.Idle;
            twinAttributes.Frame = twinAttributes.Animation.CalculateFrame(AITimer / 50f % 1f, twinAttributes.InPhase2);
        }
    }
}
