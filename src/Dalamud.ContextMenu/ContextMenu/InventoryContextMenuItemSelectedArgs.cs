﻿//------------------------------------------------------------------------------
// <auto-generated>
// Suppress stylecop rules to avoid fixing all these warnings right now.
// </auto-generated>
//------------------------------------------------------------------------------
namespace Dalamud.ContextMenu;

using System;

/// <summary>
/// The arguments for when an inventory context menu item is selected
/// </summary>
public class InventoryContextMenuItemSelectedArgs : BaseInventoryContextMenuArgs {
    internal InventoryContextMenuItemSelectedArgs(IntPtr addon, IntPtr agent, string? parentAddonName, uint itemId, uint itemAmount, bool itemHq) : base(addon, agent, parentAddonName, itemId, itemAmount, itemHq) {
    }
}