namespace XivCommon.Functions.ContextMenu.Inventory {
    public class InventoryContextMenuItem : CustomContextMenuItem<ContextMenu.InventoryContextMenuItemSelectedDelegate> {
        public InventoryContextMenuItem(string name, ContextMenu.InventoryContextMenuItemSelectedDelegate action) : base(name, action) {
        }
    }
}
