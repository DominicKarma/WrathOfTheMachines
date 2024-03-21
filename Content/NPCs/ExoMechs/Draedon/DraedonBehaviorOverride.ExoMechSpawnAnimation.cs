using System;
using CalamityMod;
using CalamityMod.NPCs.ExoMechs;
using Luminance.Common.Utilities;
using Luminance.Core.Sounds;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;

namespace DifferentExoMechs.Content.NPCs.ExoMechs
{
    public sealed partial class DraedonBehaviorOverride : NPCBehaviorOverride
    {
        public static LoopedSoundInstance SirenSoundInstance
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

                CustomExoMechsSky.RedSirensIntensity = MathF.Sin(MathHelper.TwoPi * (AITimer - 90f) / 240f) * 0.7f;
                Main.windSpeedTarget = 1.1f;

                SirenSoundInstance ??= LoopedSoundManager.CreateNew(SirenSound, () =>
                {
                    return !NPC.active || AIState != DraedonAIState.ExoMechSpawnAnimation;
                });
                SirenSoundInstance.Update(Main.LocalPlayer.Center, sound =>
                {
                    sound.Volume = Utilities.InverseLerp(0f, 120f, AITimer - 90f).Squared();
                });
            }
        }
    }
}
