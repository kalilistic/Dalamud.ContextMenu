using System;

namespace XivCommon.Functions.ContextMenu {
    public abstract class BaseContextMenuArgs {
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

        /// <summary>
        /// The actor ID for this context menu. May be invalid (0xE0000000).
        /// </summary>
        public uint ActorId { get; }

        /// <summary>
        /// The lower half of the content ID of the actor for this context menu. May be zero.
        /// </summary>
        public uint ContentIdLower { get; }

        /// <summary>
        /// The text related to this context menu, usually an actor name.
        /// </summary>
        public string? Text { get; }

        /// <summary>
        /// The world of the actor this context menu is for, if any.
        /// </summary>
        public ushort ActorWorld { get; }

        internal BaseContextMenuArgs(IntPtr addon, IntPtr agent, string? parentAddonName, uint actorId, uint contentIdLower, string? text, ushort actorWorld) {
            this.Addon = addon;
            this.Agent = agent;
            this.ParentAddonName = parentAddonName;
            this.ActorId = actorId;
            this.ContentIdLower = contentIdLower;
            this.Text = text;
            this.ActorWorld = actorWorld;
        }
    }
}
