namespace XivCommon.Functions.ContextMenu.Inventory {
    /// <summary>
    /// A custom context menu item for inventory items.
    /// </summary>
    public class InventoryContextMenuItem : CustomContextMenuItem<ContextMenu.InventoryContextMenuItemSelectedDelegate> {
        /// <summary>
        /// Create a new context menu item for inventory items.
        /// </summary>
        /// <param name="name">the English name of the item, copied to other languages</param>
        /// <param name="action">the action to perform on click</param>
        public InventoryContextMenuItem(string name, ContextMenu.InventoryContextMenuItemSelectedDelegate action) : base(name, action) {
        }
    }
}
