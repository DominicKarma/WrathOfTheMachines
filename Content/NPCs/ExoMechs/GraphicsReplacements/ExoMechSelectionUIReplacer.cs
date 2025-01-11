using System;
using System.Reflection;
using CalamityMod;
using CalamityMod.UI.DraedonSummoning;
using CalamityMod.World;
using Luminance.Assets;
using Luminance.Core.Hooking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargowiltasCrossmod.Content.Calamity.Bosses.ExoMechs.GraphicsReplacements
{
    public class ExoMechSelectionUIReplacer : ModSystem
    {
        private static readonly MethodInfo? selectionUIDrawMethod = typeof(ExoMechSelectionUI).GetMethod("Draw", LumUtils.UniversalBindingFlags);

        /// <summary>
        /// The general scale of icons.
        /// </summary>
        public static float GeneralScaleInterpolant
        {
            get;
            set;
        }

        /// <summary>
        /// The exo mech icon used for summoning Hades.
        /// </summary>
        public static ExoMechSelectionIcon HadesIcon
        {
            get;
            set;
        }

        /// <summary>
        /// The exo mech icon used for summoning Ares.
        /// </summary>
        public static ExoMechSelectionIcon AresIcon
        {
            get;
            set;
        }

        /// <summary>
        /// The exo mech icon used for summoning Artemis and Apollo.
        /// </summary>
        public static ExoMechSelectionIcon ArtemisAndApolloIcon
        {
            get;
            set;
        }

        /// <summary>
        /// How much the <see cref="GeneralScaleInterpolant"/> increments each frame.
        /// </summary>
        public static float ScaleIncrement => 0.03f;

        public delegate void Orig_ExoMechSelectionUIDraw();

        public override void SetStaticDefaults()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            var hadesIconTexture = LazyAsset<Texture2D>.Request("CalamityMod/UI/DraedonSummoning/HeadIcon_THanos");
            HadesIcon = new(ExoMech.Destroyer, "Mods.WoTM.UI.HadesIconMessage", new(-64f, -46f), hadesIconTexture, ExoMechSelectionUI.ThanatosHoverSound);

            var aresIconTexture = LazyAsset<Texture2D>.Request("CalamityMod/UI/DraedonSummoning/HeadIcon_Ares");
            AresIcon = new(ExoMech.Prime, "Mods.WoTM.UI.AresIconMessage", Vector2.UnitY * -60f, aresIconTexture, ExoMechSelectionUI.AresHoverSound);

            var artemisApolloIconTexture = LazyAsset<Texture2D>.Request("CalamityMod/UI/DraedonSummoning/HeadIcon_ArtemisApollo");
            var artemisApolloHoverSound = new SoundStyle("FargowiltasCrossmod/Assets/Sounds/ExoMechs/GeneralExoMechs/ExoTwinsIconHover");
            ArtemisAndApolloIcon = new(ExoMech.Twins, "Mods.WoTM.UI.ArtemisAndApolloIconMessage", new(64f, -46f), artemisApolloIconTexture, artemisApolloHoverSound);
        }

        public override void OnModLoad()
        {
            if (selectionUIDrawMethod is null)
                return;

            HookHelper.ModifyMethodWithDetour(selectionUIDrawMethod, DrawCustomUI);
        }

        /// <summary>
        /// Renders the custom Exo Mech selection UI.
        /// </summary>
        public static void DrawCustomUI(Orig_ExoMechSelectionUIDraw orig)
        {
            HadesIcon.Update();
            HadesIcon.Render(0f);

            AresIcon.Update();
            AresIcon.Render(-0.15f);

            ArtemisAndApolloIcon.Update();
            ArtemisAndApolloIcon.Render(-0.3f);
        }

        public override void UpdateUI(GameTime gameTime)
        {
            bool iconsActive = Main.LocalPlayer.Calamity().AbleToSelectExoMech;
            GeneralScaleInterpolant = MathF.Max(0f, GeneralScaleInterpolant + iconsActive.ToDirectionInt() * ScaleIncrement);
            if (!iconsActive && GeneralScaleInterpolant > 1f)
                GeneralScaleInterpolant = 1f;
        }

        /// <summary>
        /// Prepares an Exo Mech for summoning by Draedon.
        /// </summary>
        /// <param name="mechToSummon">The type of Exo Mech to summon.</param>
        public static void SummonExoMech(ExoMech mechToSummon)
        {
            CalamityWorld.DraedonMechToSummon = mechToSummon;
            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                var packet = ModContent.GetInstance<CalamityMod.CalamityMod>().GetPacket();
                packet.Write((byte)CalamityModMessageType.ExoMechSelection);
                packet.Write((int)CalamityWorld.DraedonMechToSummon);
                packet.Send();
            }
        }
    }
}
