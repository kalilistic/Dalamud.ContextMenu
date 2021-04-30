using System;

namespace XivCommon.Functions.ContextMenu.Inventory {
    public class InventoryContextMenuItemSelectedArgs : BaseInventoryContextMenuArgs {
        internal InventoryContextMenuItemSelectedArgs(IntPtr addon, IntPtr agent, string? parentAddonName, uint itemId, uint itemAmount, bool itemHq) : base(addon, agent, parentAddonName, itemId, itemAmount, itemHq) {
        }
    }
}
