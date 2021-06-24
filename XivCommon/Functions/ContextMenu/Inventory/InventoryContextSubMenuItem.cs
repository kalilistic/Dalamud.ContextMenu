using Dalamud.Game.Text.SeStringHandling;

namespace XivCommon.Functions.ContextMenu.Inventory {
    /// <summary>
    /// A custom inventory context menu item that will open a submenu
    /// </summary>
    public class InventoryContextSubMenuItem : CustomContextMenuItem<ContextMenu.InventoryContextMenuOpenEventDelegate> {
        /// <summary>
        /// Create a new context menu item for inventory items that will open a submenu.
        /// </summary>
        /// <param name="name">the English name of the item, copied to other languages</param>
        /// <param name="action">the action to perform on click</param>
        public InventoryContextSubMenuItem(SeString name, ContextMenu.InventoryContextMenuOpenEventDelegate action) : base(name, action) {
        }
    }
}
