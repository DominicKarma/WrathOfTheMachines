using System;
using System.Collections.Generic;
using System.Linq;
using CalamityMod;
using CalamityMod.NPCs.ExoMechs;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace WoTM.Content.NPCs.ExoMechs
{
    public sealed class ExoMechStylePlayer : ModPlayer
    {
        private static bool resetData => !ExoMechFightStateManager.AnyExoMechsPresent && !NPC.AnyNPCs(ModContent.NPCType<Draedon>());

        /// <summary>
        /// How many times the rippers were activated during the Exo Mech fight.
        /// </summary>
        public int RipperActivationCount
        {
            get;
            set;
        }

        /// <summary>
        /// How many hits the player took during the Exo Mech fight.
        /// </summary>
        public int HitCount
        {
            get;
            set;
        }

        /// <summary>
        /// How many buffs the player had during the Exo Mech fight.
        /// </summary>
        /// 
        /// <remarks>
        /// Once the battle begins, this value can only go up. This means that losing one buff does not decrement this value, but receiving a new one will increment it.
        /// </remarks>
        public int BuffCount
        {
            get;
            set;
        }

        /// <summary>
        /// A bonus provided during the Exo Mechs fight based on how aggressive and up close the player's play was overall.
        /// </summary>
        public float AggressivenessBonus
        {
            get;
            set;
        }

        /// <summary>
        /// How long each phase lasted, in frames, during the Exo Mech fight.
        /// </summary>
        public readonly Dictionary<int, int> PhaseDurations = [];

        /// <summary>
        /// How short the Exo Mechs fight has to be in order to receive minimum style points for it.
        /// </summary>
        public static float MinStyleBoostFightTime => Utilities.MinutesToFrames(0.5f);

        /// <summary>
        /// How long the Exo Mechs fight has to be in order to receive maximum style points for it.
        /// </summary>
        public static float MaxStyleBoostFightTime => Utilities.MinutesToFrames(3f);

        /// <summary>
        /// How much style the player received during the Exo Mechs fight.
        /// </summary>
        public float Style
        {
            get
            {
                int fightDuration = PhaseDurations.Sum(kv => kv.Value);
                float hitsWeight = Utilities.Saturate(1f - (HitCount - 2f) / 13f);
                float buffsWeight = Utilities.Saturate(1f - (BuffCount - 11f) / 9f);
                float fightTimeInterpolant = Utilities.InverseLerp(MinStyleBoostFightTime, MaxStyleBoostFightTime, fightDuration, false);
                float fightTimeWeight = SmoothClamp(fightTimeInterpolant, 1.14f);

                float unclampedStyle = hitsWeight * 0.26f + buffsWeight * 0.13f + fightTimeWeight * 0.31f + SmoothClamp(AggressivenessBonus, 1.27f) * 0.3f;

                return Utilities.Saturate(unclampedStyle);
            }
        }

        /// <summary>
        /// Applies a smooth clamp that permits values to exceed 1 with diminishing returns, up to an asymptotic <paramref name="maxValue"/>.
        /// </summary>
        /// <param name="x">The input value.</param>
        /// <param name="maxValue">The maximum output value for the clamp.</param>
        public static float SmoothClamp(float x, float maxValue)
        {
            if (x < 0f)
                return 0f;

            // This is necessary to ensure that plugging 1 into the equation returns exactly 1.
            float correctionCoefficient = MathF.Atanh(1f / maxValue);
            return maxValue * MathF.Tanh(correctionCoefficient * x);
        }

        public void Reset()
        {
            RipperActivationCount = 0;
            HitCount = 0;
            BuffCount = 0;
            AggressivenessBonus = 0f;
            PhaseDurations.Clear();
        }

        public override void PostUpdate()
        {
            if (resetData)
            {
                Reset();
                return;
            }

            if (!PhaseDurations.ContainsKey(ExoMechFightStateManager.CurrentPhase.PhaseOrdering))
                PhaseDurations[ExoMechFightStateManager.CurrentPhase.PhaseOrdering] = 0;
            if (ExoMechFightStateManager.AnyExoMechsPresent)
                PhaseDurations[ExoMechFightStateManager.CurrentPhase.PhaseOrdering]++;

            UpdateBuffCount();
            UpdateAggressivenessBonus();
        }

        /// <summary>
        /// Keeps track of how many buffs the player has received in the Exo Mechs fight.
        /// </summary>
        private void UpdateBuffCount()
        {
            int currentBuffCount = 0;
            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                int buffID = Player.buffType[i];
                bool buffActive = Player.buffTime[i] >= 1;
                bool isntDebuff = (!Main.debuff[buffID] || CalamityLists.alcoholList.Contains(buffID)) && !BuffID.Sets.IsATagBuff[buffID];

                if (buffActive && isntDebuff)
                    currentBuffCount++;
            }
            BuffCount = Math.Max(BuffCount, currentBuffCount);
        }

        /// <summary>
        /// Updates aggressiveness bonuses for the given frame.
        /// </summary>
        private void UpdateAggressivenessBonus()
        {
            float distanceToClosestNPC = 999999f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.type != ExoMechNPCIDs.ApolloID && npc.type != ExoMechNPCIDs.ArtemisID && npc.type != ExoMechNPCIDs.HadesHeadID)
                    continue;

                if (npc.damage <= 0)
                    continue;

                float distanceFromNPC = npc.Hitbox.Distance(Player.Center);
                if (distanceFromNPC < distanceToClosestNPC)
                    distanceToClosestNPC = distanceFromNPC;
            }

            AggressivenessBonus += Utilities.InverseLerp(275f, 100f, distanceToClosestNPC) / MaxStyleBoostFightTime * 20f;

            Rectangle extendedPlayerHitbox = Player.Hitbox;
            extendedPlayerHitbox.Inflate(30, 30);
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.hostile || projectile.damage <= 0)
                    continue;

                if (projectile.Colliding(projectile.Hitbox, extendedPlayerHitbox) && !projectile.Colliding(projectile.Hitbox, Player.Hitbox))
                    AggressivenessBonus += 33f / MaxStyleBoostFightTime;
            }
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if ((CalamityKeybinds.RageHotKey.JustPressed || CalamityKeybinds.AdrenalineHotKey.JustPressed) && !resetData)
                RipperActivationCount++;
        }

        public override void OnHitByNPC(NPC npc, Player.HurtInfo hurtInfo)
        {
            if (!resetData)
                HitCount++;
        }

        public override void OnHitByProjectile(Projectile proj, Player.HurtInfo hurtInfo)
        {
            if (!resetData)
                HitCount++;
        }
    }
}
