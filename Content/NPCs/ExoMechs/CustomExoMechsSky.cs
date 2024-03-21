using CalamityMod.NPCs.ExoMechs;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace DifferentExoMechs.Content.NPCs.ExoMechs
{
    public class CustomExoMechsSky : CustomSky
    {
        private bool skyActive;

        /// <summary>
        /// The general opacity of this sky.
        /// </summary>
        public static new float Opacity;

        /// <summary>
        /// The intensity of the red sirens effect.
        /// </summary>
        public static float RedSirensIntensity
        {
            get;
            set;
        }

        /// <summary>
        /// The identifier key for this sky.
        /// </summary>
        public const string SkyKey = "DifferentExoMechs:ExoMechsSky";

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            // Prevent drawing beyond the back layer.
            if (maxDepth < float.MaxValue || minDepth >= float.MaxValue)
                return;
        }

        public override void Update(GameTime gameTime)
        {
            // Calculate the maximum sky opacity value.
            // If Draedon is not present it is assumed that the Exo Mechs were just spawned in via cheating, and as such they sky should immediately draw at its maximum intensity, rather than not at all.
            float maxSkyOpacity = 1f;
            int draedonIndex = NPC.FindFirstNPC(ModContent.NPCType<Draedon>());
            if (draedonIndex >= 0 && Main.npc[draedonIndex].TryGetBehavior(out DraedonBehaviorOverride behavior))
                maxSkyOpacity = behavior.MaxSkyOpacity;

            // Increase or decrease the opacity of this sky based on whether it's active or not, stopping at 0-1 bounds.
            Opacity = MathHelper.Clamp(Opacity + skyActive.ToDirectionInt() * 0.02f, 0f, maxSkyOpacity);

            // Disable ambient sky objects like wyverns and eyes appearing in front of the background.
            if (Opacity >= 0.5f)
                SkyManager.Instance["Ambience"].Deactivate();

            // Disable Calamity's vanilla Exo Mechs background.
            if (Opacity >= 0.01f)
                SkyManager.Instance["CalamityMod:ExoMechs"]?.Deactivate();

            if (!skyActive)
                ResetVariablesWhileInactivity();
        }

        public static void ResetVariablesWhileInactivity()
        {
            RedSirensIntensity = Utilities.Saturate(RedSirensIntensity - 0.1f);
        }

        #region Boilerplate
        public override void Deactivate(params object[] args) => skyActive = false;

        public override void Reset() => skyActive = false;

        public override bool IsActive() => skyActive || Opacity > 0f;

        public override void Activate(Vector2 position, params object[] args) => skyActive = true;

        public override float GetCloudAlpha() => 1f - Opacity;
        #endregion Boilerplate
    }
}
