using System;

namespace XivCommon.Functions.ContextMenu {
    /// <summary>
    /// Arguments for the context menu item selected delegate.
    /// </summary>
    public class ContextMenuItemSelectedArgs : BaseContextMenuArgs {
        internal ContextMenuItemSelectedArgs(IntPtr addon, IntPtr agent, string? parentAddonName, uint actorId, uint contentIdLower, string? text, ushort actorWorld) : base(addon, agent, parentAddonName, actorId, contentIdLower, text, actorWorld) {
        }
    }
}
