using System;

namespace XivCommon.Functions.ContextMenu.Inventory {
    public abstract class BaseInventoryContextMenuArgs {
        /// <summary>
        /// Pointer to the context menu addon.
        /// </summary>
        public IntPtr Addon { get; }

        /// <summary>
        /// Pointer to the context menu agent.
        /// </summary>
        public IntPtr Agent { get; }

        /// <summary>
        /// The name of the addon containing this context menu, if any.
        /// </summary>
        public string? ParentAddonName { get; }

        public uint ItemId { get; }

        public uint ItemAmount { get; }

        public bool ItemHq { get; }

        internal BaseInventoryContextMenuArgs(IntPtr addon, IntPtr agent, string? parentAddonName, uint itemId, uint itemAmount, bool itemHq) {
            this.Addon = addon;
            this.Agent = agent;
            this.ParentAddonName = parentAddonName;
            this.ItemId = itemId;
            this.ItemAmount = itemAmount;
            this.ItemHq = itemHq;
        }
    }
}
