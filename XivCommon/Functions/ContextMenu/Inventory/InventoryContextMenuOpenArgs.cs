using System;
using System.Collections.Generic;

namespace XivCommon.Functions.ContextMenu.Inventory {
    public class InventoryContextMenuOpenArgs : BaseInventoryContextMenuArgs {
        /// <summary>
        /// Context menu items in this menu.
        /// </summary>
        public List<BaseContextMenuItem> Items { get; } = new();

        internal InventoryContextMenuOpenArgs(IntPtr addon, IntPtr agent, string? parentAddonName, uint itemId, uint itemAmount, bool itemHq) : base(addon, agent, parentAddonName, itemId, itemAmount, itemHq) {
        }
    }
}
