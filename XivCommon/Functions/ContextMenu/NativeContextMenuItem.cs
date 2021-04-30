namespace XivCommon.Functions.ContextMenu {
    /// <summary>
    /// A native context menu item
    /// </summary>
    public sealed class NativeContextMenuItem : BaseContextMenuItem {
        /// <summary>
        /// The action code to be used in the context menu agent for this item.
        /// </summary>
        public byte InternalAction { get; }

        /// <summary>
        /// The name of the context item.
        /// </summary>
        public string Name { get; set; }

        internal NativeContextMenuItem(byte action, string name, bool enabled) {
            this.Name = name;
            this.InternalAction = action;
            this.Enabled = enabled;
        }
    }
}
