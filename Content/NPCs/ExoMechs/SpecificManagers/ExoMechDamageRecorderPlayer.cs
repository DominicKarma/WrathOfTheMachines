﻿using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;

namespace WoTM.Content.NPCs.ExoMechs
{
    public class ExoMechDamageRecorderPlayer : ModPlayer
    {
        private readonly Dictionary<ExoMechDamageSource, int> damageDonePerSource = [];

        /// <summary>
        /// Calculates how much damage has been done in the Exo Mechs fight to this player by a given damage source.
        /// </summary>
        /// <param name="source">The damage source to evaluate.</param>
        public int GetDamageBySource(ExoMechDamageSource source)
        {
            if (damageDonePerSource.TryGetValue(source, out int damage))
                return damage;

            return damageDonePerSource[source] = 0;
        }

        /// <summary>
        /// The damage source that has thus far done the most damage to the player.
        /// </summary>
        public ExoMechDamageSource MostDamagingSource
        {
            get
            {
                // Fallback case.
                // TODO -- Should this be a separate enumeration value?
                if (damageDonePerSource.Count <= 0)
                    return ExoMechDamageSource.Thermal;

                return damageDonePerSource.MaxBy(kv => kv.Value).Key;
            }
        }

        /// <summary>
        /// Resets all incurred damage by Exo Mech damage sources.
        /// </summary>
        public void ResetIncurredDamage()
        {
            for (int i = 0; i < (int)ExoMechDamageSource.Count; i++)
                damageDonePerSource[(ExoMechDamageSource)i] = 0;
        }
    }
}
