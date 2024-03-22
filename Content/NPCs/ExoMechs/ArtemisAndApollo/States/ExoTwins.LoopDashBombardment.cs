using System;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Artemis;
using DifferentExoMechs.Content.NPCs.ExoMechs.Projectiles;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DifferentExoMechs.Content.NPCs.ExoMechs
{
    public static partial class ExoTwinsStates
    {
        /// <summary>
        /// AI update loop method for the LoopDashBombardment attack.
        /// </summary>
        /// <param name="npc">The Exo Twin's NPC instance.</param>
        /// <param name="twinAttributes">The Exo Twin's designated generic attributes.</param>
        public static void DoBehavior_LoopDashBombardment(NPC npc, IExoTwin twinAttributes)
        {
            bool isApollo = npc.type == ExoMechNPCIDs.ApolloID;
            if (isApollo)
                DoBehavior_LoopDashBombardment_ApolloDashes(npc, twinAttributes);
            else
                DoBehavior_LoopDashBombardment_ArtemisLasers(npc, twinAttributes);
        }

        /// <summary>
        /// AI update loop method for Apollo during the LoopDashBombardment.
        /// </summary>
        /// <param name="npc">Apollo's NPC instance.</param>
        /// <param name="apolloAttributes">Apollo's designated generic attributes.</param>
        public static void DoBehavior_LoopDashBombardment_ApolloDashes(NPC npc, IExoTwin apolloAttributes)
        {
            int hoverTime = 75;
            int telegraphSoundBuffer = 28;
            int dashTime = 23;
            int spinTime = 48;

            if (AITimer <= hoverTime)
            {
                if (AITimer == hoverTime - telegraphSoundBuffer)
                    SoundEngine.PlaySound(Artemis.ChargeTelegraphSound, npc.Center);

                Vector2 reelBackOffset = Target.SafeDirectionTo(npc.Center) * MathF.Pow(AITimer / (float)hoverTime, 8f) * 350f;
                Vector2 hoverOffset = Target.SafeDirectionTo(npc.Center) * new Vector2(1f, 0.94f);
                Vector2 hoverDestination = Target.Center + hoverOffset * new Vector2(750f, 400f) + reelBackOffset;
                npc.SmoothFlyNear(hoverDestination, AITimer / (float)hoverTime * 0.6f, 0.71f);
                npc.rotation = npc.AngleTo(Target.Center);

                apolloAttributes.Animation = ExoTwinAnimation.Idle;
                if (AITimer >= hoverTime - telegraphSoundBuffer)
                    apolloAttributes.Animation = ExoTwinAnimation.ChargingUp;
            }
            else
                npc.damage = npc.defDamage;

            if (AITimer == hoverTime)
            {
                ScreenShakeSystem.StartShakeAtPoint(npc.Center, 7.5f);
                npc.velocity = npc.SafeDirectionTo(Target.Center) * 60f;
                npc.ai[2] = -npc.velocity.X.NonZeroSign();

                SoundEngine.PlaySound(Artemis.ChargeSound, npc.Center);
            }

            if (AITimer >= hoverTime + dashTime)
            {
                if (AITimer < hoverTime + dashTime + spinTime)
                {
                    npc.velocity = npc.velocity.RotatedBy(MathHelper.TwoPi / spinTime * npc.ai[2]);
                    if (AITimer >= hoverTime + dashTime + 8 && npc.velocity.AngleBetween(npc.SafeDirectionTo(Target.Center + Target.velocity * 14f)) < 0.16f)
                        AITimer = hoverTime + dashTime + spinTime;
                }

                apolloAttributes.Animation = ExoTwinAnimation.Attacking;
                npc.rotation = npc.velocity.ToRotation();
            }

            bool canFireMissiles = AITimer >= hoverTime + dashTime && npc.velocity.Length() <= 74f;
            bool tooCloseToFireMissiles = npc.WithinRange(Target.Center, 240f);
            if (AITimer % 3 == 2 && canFireMissiles && !tooCloseToFireMissiles)
            {
                SoundEngine.PlaySound(Apollo.MissileLaunchSound with { Volume = 0.4f, MaxInstances = 0 }, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 missileVelocity = Vector2.Lerp(npc.rotation.ToRotationVector2(), npc.SafeDirectionTo(Target.Center), 0.5f) * 16f;
                    Vector2 offset = npc.rotation.ToRotationVector2() * 70f;
                    Utilities.NewProjectileBetter(npc.GetSource_FromAI(), npc.Center + offset, missileVelocity, ModContent.ProjectileType<ApolloMissile>(), BasicShotDamage, 0f, Main.myPlayer, Target.Center.Y);
                }
            }

            if (AITimer >= hoverTime + dashTime + spinTime + 10 && npc.velocity.Length() <= 150f)
                npc.velocity += npc.velocity.SafeNormalize(Vector2.UnitY) * 3f;

            apolloAttributes.Frame = apolloAttributes.Animation.CalculateFrame(AITimer / 40f % 1f, apolloAttributes.InPhase2);

            if (Main.mouseRight)
                AITimer = 0;
        }

        /// <summary>
        /// AI update loop method for Artemis during the LoopDashBombardment.
        /// </summary>
        /// <param name="npc">Artemis' NPC instance.</param>
        /// <param name="artemisAttributes">Artemis' designated generic attributes.</param>
        public static void DoBehavior_LoopDashBombardment_ArtemisLasers(NPC npc, IExoTwin artemisAttributes)
        {
            npc.velocity *= 0.6f;
        }
    }
}
