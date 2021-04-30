namespace XivCommon.Functions.ContextMenu {
    /// <summary>
    /// A base context menu item
    /// </summary>
    public abstract class BaseContextMenuItem {
        /// <summary>
        /// If this item should be enabled in the menu.
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}
