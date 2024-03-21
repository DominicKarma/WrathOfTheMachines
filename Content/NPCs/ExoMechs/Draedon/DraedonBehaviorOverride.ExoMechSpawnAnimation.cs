using System;
using CalamityMod;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.World;
using Luminance.Common.Utilities;
using Luminance.Core.Sounds;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DifferentExoMechs.Content.NPCs.ExoMechs
{
    public sealed partial class DraedonBehaviorOverride : NPCBehaviorOverride
    {
        public LoopedSoundInstance SirenSoundInstance
        {
            get;
            set;
        }

        public static readonly SoundStyle SirenSound = new("DifferentExoMechs/Assets/Sounds/Custom/GeneralExoMechs/ExoMechSiren");

        /// <summary>
        /// The AI method that makes Draedon handle the Exo Mech spawning.
        /// </summary>
        public void DoBehavior_ExoMechSpawnAnimation()
        {
            if (AITimer == 1f)
                CalamityUtils.DisplayLocalizedText("Mods.DifferentExoMechs.NPCs.Draedon.ExoMechChoiceResponse1", Draedon.TextColor);
            if (AITimer == 90f)
                CalamityUtils.DisplayLocalizedText("Mods.DifferentExoMechs.NPCs.Draedon.ExoMechChoiceResponse2", Draedon.TextColorEdgy);

            PerformStandardFraming();

            if (AITimer >= 90f)
            {
                MaxSkyOpacity = Utilities.Saturate(MaxSkyOpacity + 0.002f);
                Main.numCloudsTemp = (int)Utils.Remap(MaxSkyOpacity, 0f, 1f, Main.numCloudsTemp, Main.maxClouds);

                Vector2 hoverDestination = PlayerToFollow.Center + PlayerToFollow.SafeDirectionTo(NPC.Center) * new Vector2(820f, 560f);
                NPC.SmoothFlyNearWithSlowdownRadius(hoverDestination, 0.06f, 0.9f, 60f);

                Main.windSpeedTarget = 1.1f;

                SirenSoundInstance ??= LoopedSoundManager.CreateNew(SirenSound, () =>
                {
                    return !NPC.active || AIState != DraedonAIState.ExoMechSpawnAnimation || AITimer >= 450f;
                });
                SirenSoundInstance.Update(Main.LocalPlayer.Center, sound =>
                {
                    sound.Volume = Utilities.InverseLerp(0f, 120f, AITimer - 90f).Squared() * Utilities.InverseLerp(450f, 420f, AITimer);
                });
            }

            if (Main.mouseRight)
                AITimer = 0;

            PlaneFlyForwardInterpolant = Utilities.InverseLerp(360f, 420f, AITimer);
            CustomExoMechsSky.RedSirensIntensity = MathF.Pow(Utilities.Sin01(MathHelper.TwoPi * (AITimer - 90f) / 240f), 0.7f) * (1f - PlaneFlyForwardInterpolant) * 0.7f;

            if (Main.netMode != NetmodeID.MultiplayerClient && AITimer == 418f)
            {
                Vector2 exoMechSpawnPosition = PlayerToFollow.Center - Vector2.UnitY * 1200f;
                switch (CalamityWorld.DraedonMechToSummon)
                {
                    case ExoMech.Destroyer:
                        CalamityUtils.SpawnBossBetter(exoMechSpawnPosition, ModContent.NPCType<ThanatosHead>());
                        break;
                    case ExoMech.Prime:
                        CalamityUtils.SpawnBossBetter(exoMechSpawnPosition, ModContent.NPCType<AresBody>());
                        break;
                    case ExoMech.Twins:
                        CalamityUtils.SpawnBossBetter(exoMechSpawnPosition - Vector2.UnitX * 150f, ModContent.NPCType<Artemis>());
                        CalamityUtils.SpawnBossBetter(exoMechSpawnPosition + Vector2.UnitX * 150f, ModContent.NPCType<Apollo>());
                        break;
                }
            }
        }
    }
}
