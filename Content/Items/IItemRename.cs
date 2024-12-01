using Terraria.Localization;

namespace WoTM.Content.Items
{
    public interface IItemRename
    {
        /// <summary>
        /// The ID of the item that this rename system should affect.
        /// </summary>
        public int OverrideID
        {
            get;
        }

        /// <summary>
        /// The translations for the display name of this item.
        /// </summary>
        public LocalizedText? DisplayName
        {
            get;
        }

        /// <summary>
        /// The translations for the tooltip of this item.
        /// </summary>
        public LocalizedText? Tooltip
        {
            get;
        }
    }
}
