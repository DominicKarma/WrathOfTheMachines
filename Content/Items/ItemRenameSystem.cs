using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace WoTM.Content.Items
{
    public class ItemRenameSystem : ModSystem
    {
        // This is a jank choice of hook to use for this, but it seems to be the only one that gets called after ItemLoader.FinishSetup (which stores item display names and tooltip).
        public override void ModifyGameTipVisibility(IReadOnlyList<GameTipData> gameTips)
        {
            [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "_itemNameCache")]
            static extern ref LocalizedText[] GetItemName(Lang? instance);

            [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "_itemTooltipCache")]
            static extern ref ItemTooltip[] GetItemTooltip(Lang? instance);

            foreach (GlobalItem item in GlobalList<GlobalItem>.Globals)
            {
                if (item is not IItemRename rename)
                    continue;

                int itemID = rename.OverrideID;

                if (rename.DisplayName is not null)
                    GetItemName(null)[itemID] = rename.DisplayName;

                if (rename.Tooltip is not null)
                    GetItemTooltip(null)[itemID] = ItemTooltip.FromLocalization(rename.Tooltip);

                ContentSamples.ItemsByType[itemID].RebuildTooltip();
            }
        }
    }
}
