using System;
using System.Collections.Generic;
using Dalamud.Game.Text.SeStringHandling;

namespace XivCommon.Functions.ContextMenu {
    /// <summary>
    /// Arguments for the context menu event.
    /// </summary>
    public class ContextMenuOpenArgs : BaseContextMenuArgs {
        /// <summary>
        /// Context menu items in this menu.
        /// </summary>
        public List<BaseContextMenuItem> Items { get; } = new();

        internal ContextMenuOpenArgs(IntPtr addon, IntPtr agent, string? parentAddonName, uint actorId, uint contentIdLower, SeString? text, ushort actorWorld) : base(addon, agent, parentAddonName, actorId, contentIdLower, text, actorWorld) {
        }
    }
}
