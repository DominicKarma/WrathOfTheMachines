using CalamityMod.NPCs.ExoMechs.Artemis;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;

namespace DifferentExoMechs.Content.NPCs.Bosses
{
    public static partial class ExoTwinsStates
    {
        /// <summary>
        /// AI update loop method for the TestDashes attack.
        /// </summary>
        /// <param name="twin">The Exo Twin's NPC instance.</param>
        /// <param name="twinAttributes">The Exo Twin's designated generic attributes.</param>
        public static void DoBehavior_TestDashes(NPC npc, IExoTwin twinAttributes)
        {
            int hoverTime = 50;
            int reelBackTime = 20;
            int dashTime = 7;
            int slowDownTime = 10;
            bool artemis = npc.type == ExoMechNPCIDs.ArtemisID;
            float driftAngularVelocity = AITimer >= hoverTime / 2 ? (artemis.ToDirectionInt() * 0.21f) : 0f;

            Vector2 hoverDestination = Target.Center + Target.SafeDirectionTo(npc.Center).RotatedBy(driftAngularVelocity) * new Vector2(650f, 450f);
            if (AITimer <= hoverTime)
            {
                npc.SmoothFlyNear(hoverDestination, 0.2f, 0.4f);
                npc.rotation = npc.AngleTo(Target.Center);
                twinAttributes.Animation = ExoTwinAnimation.Idle;
                twinAttributes.Frame = twinAttributes.Animation.CalculateFrame(AITimer / (float)hoverTime, twinAttributes.InPhase2);

                return;
            }

            if (AITimer <= hoverTime + reelBackTime)
            {
                if (AITimer == hoverTime + 1 && artemis)
                    SoundEngine.PlaySound(Artemis.AttackSelectionSound);

                float lookAngularVelocity = Utils.Remap(AITimer - hoverTime, 0f, reelBackTime, 0.1f, 0.006f);
                npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(Target.Center), lookAngularVelocity);
                npc.velocity *= 0.9f;

                twinAttributes.Animation = ExoTwinAnimation.ChargingUp;
                twinAttributes.Frame = twinAttributes.Animation.CalculateFrame(Utilities.InverseLerp(0f, reelBackTime, AITimer - hoverTime), twinAttributes.InPhase2);

                return;
            }

            if (AITimer <= hoverTime + reelBackTime + dashTime)
            {
                if (AITimer == hoverTime + reelBackTime + 1)
                {
                    ScreenShakeSystem.StartShake(14f, shakeStrengthDissipationIncrement: 0.35f);
                    SoundEngine.PlaySound(Artemis.ChargeSound);
                    npc.velocity = npc.rotation.ToRotationVector2() * 180f;
                    npc.netUpdate = true;
                }

                npc.damage = npc.defDamage;

                twinAttributes.Animation = ExoTwinAnimation.Attacking;
                twinAttributes.Frame = twinAttributes.Animation.CalculateFrame(Utilities.InverseLerp(0f, dashTime, AITimer - hoverTime - reelBackTime), twinAttributes.InPhase2);

                return;
            }

            if (AITimer <= hoverTime + reelBackTime + dashTime + slowDownTime)
            {
                npc.velocity *= 0.64f;
                npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(Target.Center), 0.1f);

                return;
            }

            AITimer = 0;
        }
    }
}
