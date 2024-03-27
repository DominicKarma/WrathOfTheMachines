﻿using Luminance.Core.Graphics;
using Terraria;
using Terraria.Audio;

namespace WoTM.Content.NPCs.ExoMechs
{
    public sealed partial class DraedonBehaviorOverride : NPCBehaviorOverride
    {
        /// <summary>
        /// The monologue that Draedon uses upon an Exo Mech being defeated.
        /// </summary>
        public static readonly DraedonDialogueChain FirstInterjection = new DraedonDialogueChain("Mods.WoTM.NPCs.Draedon.").
            Add("Interjection1").
            Add(SelectInterjectionText).
            Add("Interjection3").
            Add("Interjection4");

        /// <summary>
        /// How much damage needs to be incurred by a given <see cref="ExoMechDamageSource"/> in order for the damage to be considered major.
        /// </summary>
        public const int MajorDamageThreshold = 500;

        /// <summary>
        /// The AI method that makes Draedon speak to the player after an Exo Mech has been defeated.
        /// </summary>
        public void DoBehavior_FirstInterjection()
        {
            int speakTimer = (int)AITimer - 90;
            var monologue = StartingMonologueToUse;
            for (int i = 0; i < monologue.Count; i++)
            {
                if (speakTimer == monologue[i].SpeakDelay)
                    monologue[i].SayInChat();
            }

            bool monologueIsFinished = speakTimer >= monologue.OverallDuration;

            if (speakTimer == monologue.OverallDuration)
            {
                if (Main.LocalPlayer.statLife < Main.LocalPlayer.statLifeMax2)
                    Main.LocalPlayer.Heal(Main.LocalPlayer.statLifeMax2 - Main.LocalPlayer.statLife);
                Main.LocalPlayer.statMana = Main.LocalPlayer.statManaMax2;

                ScreenShakeSystem.StartShake(4.5f);
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/OrbHeal", 5) with { Volume = 0.9f });
            }
        }

        /// <summary>
        /// Selects interjection text based on whatever the player took the most damage from.
        /// </summary>
        public static string SelectInterjectionText()
        {
            Player closest = Main.player[Player.FindClosest(Main.LocalPlayer.Center, 1, 1)];

            if (closest.TryGetModPlayer(out ExoMechDamageRecorderPlayer recorderPlayer))
                return "Interjection2_Error";

            ExoMechDamageSource source = recorderPlayer.MostDamagingSource;
            int damageIncurred = recorderPlayer.GetDamageBySource(source);
            float playerLifeRatio = closest.statLife / (float)closest.statLifeMax2;

            string typePrefix = source.ToString();
            string damagePrefix = "Minor";

            bool majorDamage = damageIncurred >= MajorDamageThreshold;
            bool nearLethalDamage = majorDamage && playerLifeRatio <= 0.2f;

            if (majorDamage)
                damagePrefix = "Major";
            if (nearLethalDamage)
                damagePrefix = "NearLethal";

            if (damageIncurred <= 0)
                return "Interjection2_Undamaged";

            return $"Interjection2_{typePrefix}_{damagePrefix}";
        }
    }
}
