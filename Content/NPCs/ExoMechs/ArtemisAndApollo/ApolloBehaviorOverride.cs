using System;
using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using Luminance.Assets;
using Luminance.Common.Utilities;
using Luminance.Core.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;

namespace WoTM.Content.NPCs.ExoMechs
{
    public sealed partial class ApolloBehaviorOverride : NPCBehaviorOverride, IExoTwin
    {
        /// <summary>
        /// Apollo's current frame.
        /// </summary>
        public int Frame
        {
            get;
            set;
        }

        /// <summary>
        /// Apollo's current AI timer.
        /// </summary>
        public int AITimer
        {
            get => (int)NPC.ai[1];
            set => NPC.ai[1] = value;
        }

        /// <summary>
        /// Whether Apollo has fully entered his second phase yet or not.
        /// </summary>
        public bool InPhase2
        {
            get => NPC.ai[0] == 1f;
            set => NPC.ai[0] = value.ToInt();
        }

        /// <summary>
        /// Whether Apollo has verified that Artemis is alive or not.
        /// </summary>
        public bool ArtemisSummonCheckPerformed
        {
            get;
            set;
        }

        /// <summary>
        /// The opacity of wingtip vortices on Apollo.
        /// </summary>
        public float WingtipVorticesOpacity
        {
            get;
            set;
        }

        /// <summary>
        /// The intensity boost of thrusters for Apollo.
        /// </summary>
        public float ThrusterBoost
        {
            get;
            set;
        }

        /// <summary>
        /// Apollo's current animation.
        /// </summary>
        public ExoTwinAnimation Animation
        {
            get;
            set;
        } = ExoTwinAnimation.Idle;

        /// <summary>
        /// The engine sound Apollo plays.
        /// </summary>
        public LoopedSoundInstance EngineLoopSound
        {
            get;
            set;
        }

        /// <summary>
        /// The individual AI state of Apollo. Only used if the shared AI state is <see cref="ExoTwinsAIState.PerformIndividualAttacks"/>.
        /// </summary>
        public IndividualExoTwinStateHandler IndividualState
        {
            get;
            set;
        } = new(0);

        /// <summary>
        /// Apollo's specific draw action.
        /// </summary>
        public Action? SpecificDrawAction
        {
            get;
            set;
        }

        /// <summary>
        /// Apollo's optic nerve colors.
        /// </summary>
        public Color[] OpticNervePalette => [new(28, 58, 60), new(62, 105, 80), new(108, 167, 94), new(144, 246, 100), new(81, 126, 85)];

        /// <summary>
        /// Apollo's base texture.
        /// </summary>
        internal static LazyAsset<Texture2D> BaseTexture;

        /// <summary>
        /// Apollo's glowmask texture.
        /// </summary>
        internal static LazyAsset<Texture2D> Glowmask;

        public override int NPCOverrideID => ExoMechNPCIDs.ApolloID;

        public void ResetLocalStateData()
        {
            NPC.ai[2] = 0f;
            NPC.ai[3] = 0f;
        }

        public override void SetStaticDefaults()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            BaseTexture = LazyAsset<Texture2D>.Request("WoTM/Content/NPCs/ExoMechs/ArtemisAndApollo/Textures/Apollo");
            Glowmask = LazyAsset<Texture2D>.Request("WoTM/Content/NPCs/ExoMechs/ArtemisAndApollo/Textures/ApolloGlow");
        }

        public override void AI()
        {
            if (!ArtemisSummonCheckPerformed)
            {
                if (!NPC.AnyNPCs(ExoMechNPCIDs.ArtemisID))
                    NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ExoMechNPCIDs.ArtemisID, NPC.whoAmI);

                ArtemisSummonCheckPerformed = true;
                NPC.netUpdate = true;
            }
            else if (CalamityGlobalNPC.draedonExoMechTwinRed == -1)
                NPC.active = false;

            Vector2 actualHitboxSize = new(164f, 164f);
            if (NPC.Size != actualHitboxSize)
                NPC.Size = actualHitboxSize;

            // Use base Calamity's ChargeCombo AIState at all times, since Apollo needs that to be enabled for his CanHitPlayer hook to return true.
            NPC.As<Apollo>().AIState = (int)Apollo.Phase.ChargeCombo;

            UpdateEngineSound();

            CalamityGlobalNPC.draedonExoMechTwinGreen = NPC.whoAmI;
            ThrusterBoost = MathHelper.Clamp(ThrusterBoost - 0.035f, 0f, 10f);
            SpecificDrawAction = null;
            NPC.Opacity = 1f;
            NPC.damage = 0;
            AITimer++;
        }

        public void UpdateEngineSound()
        {
            EngineLoopSound ??= LoopedSoundManager.CreateNew(ArtemisBehaviorOverride.EngineSound, () =>
            {
                return !NPC.active;
            });
            EngineLoopSound.Update(NPC.Center, s =>
            {
                if (s.Sound is null)
                    return;

                s.Volume = Utilities.InverseLerp(12f, 60f, NPC.velocity.Length()) * 1.5f + 0.45f;
                s.Pitch = Utilities.InverseLerp(9f, 50f, NPC.velocity.Length()) * 0.5f;
            });
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            CommonExoTwinFunctionalities.DrawBase(NPC, this, BaseTexture.Value, Glowmask.Value, lightColor, screenPos, Frame);
            return false;
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (ExoTwinsStates.DoBehavior_EnterSecondPhase_ApolloIsProtectingArtemis(this))
            {
                modifiers.FinalDamage *= ExoTwinsStates.EnterSecondPhase_ApolloDamageProtectionFactor;
                if (!CalamityLists.projectileDestroyExceptionList.Contains(projectile.type))
                    projectile.Kill();
            }
        }
    }
}
