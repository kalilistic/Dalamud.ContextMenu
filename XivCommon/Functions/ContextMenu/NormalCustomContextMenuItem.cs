namespace XivCommon.Functions.ContextMenu {
    public class NormalContextMenuItem : CustomContextMenuItem<ContextMenu.ContextMenuItemSelectedDelegate> {
        public NormalContextMenuItem(string name, ContextMenu.ContextMenuItemSelectedDelegate action) : base(name, action) {
        }
    }
}
