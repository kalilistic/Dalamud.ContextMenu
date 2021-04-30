using XivCommon.Functions.ContextMenu.Inventory;

namespace XivCommon.Functions.ContextMenu {
    public class ContextMenuItemSelectedArgsContainer {
        public ContextMenuItemSelectedArgs? ItemSelectedArgs { get; }
        public InventoryContextMenuItemSelectedArgs? InventoryItemSelectedArgs { get; }

        internal ContextMenuItemSelectedArgsContainer(ContextMenuItemSelectedArgs args) {
            this.ItemSelectedArgs = args;
        }

        internal ContextMenuItemSelectedArgsContainer(InventoryContextMenuItemSelectedArgs args) {
            this.InventoryItemSelectedArgs = args;
        }
    }
}
