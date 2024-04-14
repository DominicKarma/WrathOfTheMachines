using CalamityMod;
using CalamityMod.UI.DraedonSummoning;
using CalamityMod.World;
using Luminance.Assets;
using Luminance.Common.Utilities;
using Luminance.Core.Hooking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace WoTM.Core.Graphics.EffectManagers
{
    public class ExoMechSelectionUIReplacer : ILEditProvider
    {
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

        public override void SetStaticDefaults()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            var hadesIconTexture = LazyAsset<Texture2D>.Request("CalamityMod/UI/DraedonSummoning/HeadIcon_THanos");
            HadesIcon = new(ExoMech.Destroyer, "Mods.WoTM.UI.HadesIconMessage", new(-64f, -46f), hadesIconTexture, ExoMechSelectionUI.ThanatosHoverSound);

            var aresIconTexture = LazyAsset<Texture2D>.Request("CalamityMod/UI/DraedonSummoning/HeadIcon_Ares");
            AresIcon = new(ExoMech.Prime, "Mods.WoTM.UI.AresIconMessage", Vector2.UnitY * -60f, aresIconTexture, ExoMechSelectionUI.AresHoverSound);

            var artemisApolloIconTexture = LazyAsset<Texture2D>.Request("CalamityMod/UI/DraedonSummoning/HeadIcon_ArtemisApollo");
            var artemisApolloHoverSound = new SoundStyle("WoTM/Assets/Sounds/Custom/GeneralExoMechs/ExoTwinsIconHover");
            ArtemisAndApolloIcon = new(ExoMech.Twins, "Mods.WoTM.UI.ArtemisAndApolloIconMessage", new(64f, -46f), artemisApolloIconTexture, artemisApolloHoverSound);
        }

        public override void PerformEdit(ILContext il, ManagedILEdit edit)
        {
            ILCursor cursor = new(il);

            cursor.EmitDelegate(DrawCustomUI);
            cursor.Emit(OpCodes.Ret);
        }

        public override void Subscribe(ManagedILEdit edit)
        {
            HookHelper.ModifyMethodWithIL(typeof(ExoMechSelectionUI).GetMethod("Draw", Utilities.UniversalBindingFlags), edit.SubscriptionWrapper);
        }

        public override void Unsubscribe(ManagedILEdit edit) { }

        /// <summary>
        /// Renders the custom Exo Mech selection UI.
        /// </summary>
        /// 
        /// <remarks>
        /// If Infernum is enabled, this method does nothing, letting it do what it needs instead.
        /// </remarks>
        public static void DrawCustomUI()
        {
            if (InfernumModeCompatibility.InfernumModeIsActive)
                return;

            HadesIcon.Update();
            HadesIcon.Render();

            AresIcon.Update();
            AresIcon.Render();

            ArtemisAndApolloIcon.Update();
            ArtemisAndApolloIcon.Render();
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
