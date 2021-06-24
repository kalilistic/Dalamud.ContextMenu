using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud;
using Dalamud.Game;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Component.GUI;
using XivCommon.Functions.ContextMenu.Inventory;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace XivCommon.Functions.ContextMenu {
    /// <summary>
    /// Context menu functions
    /// </summary>
    public class ContextMenu : IDisposable {
        private static class Signatures {
            internal const string SomeOpenAddonThing = "E8 ?? ?? ?? ?? 0F B7 C0 48 83 C4 60";
            internal const string ContextMenuOpen = "48 8B C4 57 41 56 41 57 48 81 EC ?? ?? ?? ??";
            internal const string ContextMenuSelected = "48 89 5C 24 ?? 55 57 41 56 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 44 24 ?? 80 B9 ?? ?? ?? ?? ??";
            internal const string ContextMenuEvent66 = "E8 ?? ?? ?? ?? 44 39 A3 ?? ?? ?? ?? 0F 84 ?? ?? ?? ??";
            internal const string SetUpContextSubMenu = "E8 ?? ?? ?? ?? 44 39 A3 ?? ?? ?? ?? 0F 86 ?? ?? ?? ??";
            internal const string TitleContextMenuOpen = "48 8B C4 57 41 55 41 56 48 81 EC ?? ?? ?? ??";
            internal const string AtkValueChangeType = "E8 ?? ?? ?? ?? 45 84 F6 48 8D 4C 24 ??";
            internal const string AtkValueSetString = "E8 ?? ?? ?? ?? 41 03 ED";
            internal const string GetAddonByInternalId = "E8 ?? ?? ?? ?? 8B 6B 20";
        }

        #region Offsets and other constants

        private const int MaxItems = 32;

        /// <summary>
        /// Offset from addon to menu type
        /// </summary>
        private const int ParentAddonIdOffset = 0x1D2;

        private const int AddonArraySizeOffset = 0x1CA;
        private const int AddonArrayOffset = 0x160;

        private const int ContextMenuItemOffset = 7;

        /// <summary>
        /// Offset from agent to actions byte array pointer (have to add the actions offset after)
        /// </summary>
        private const int MenuActionsPointerOffset = 0xD18;

        /// <summary>
        /// SetUpContextSubMenu checks this
        /// </summary>
        private const int BooleanOffsetCheck = 0x690;

        /// <summary>
        /// Offset from [MenuActionsPointer] to actions byte array
        /// </summary>
        private const int MenuActionsOffset = 0x428;

        /// <summary>
        /// Offset from inventory context agent to actions byte array
        /// </summary>
        private const int InventoryMenuActionsOffset = 0x558;

        private const int ActorIdOffset = 0xEF0;
        private const int ContentIdLowerOffset = 0xEE0;
        private const int TextPointerOffset = 0xE08;
        private const int WorldOffset = 0xF00;

        private const int ItemIdOffset = 0x5F8;
        private const int ItemAmountOffset = 0x5FC;
        private const int ItemHqOffset = 0x604;

        // Found in the first function in the agent's vtable
        private const byte NoopContextId = 0x67;
        private const byte InventoryNoopContextId = 0xFF;

        #endregion

        // 82C570 called when you click on a title menu context item

        /// <summary>
        /// The delegate for context menu events.
        /// </summary>
        public delegate void ContextMenuOpenEventDelegate(ContextMenuOpenArgs args);

        /// <summary>
        /// <para>
        /// The event that is fired when a context menu is being prepared for opening.
        /// </para>
        /// <para>
        /// Requires the <see cref="Hooks.ContextMenu"/> hook to be enabled.
        /// </para>
        /// </summary>
        public event ContextMenuOpenEventDelegate? OpenContextMenu;

        /// <summary>
        /// The delegate for inventory context menu events.
        /// </summary>
        public delegate void InventoryContextMenuOpenEventDelegate(InventoryContextMenuOpenArgs args);

        /// <summary>
        /// <para>
        /// The event that is fired when an inventory context menu is being prepared for opening.
        /// </para>
        /// <para>
        /// Requires the <see cref="Hooks.ContextMenu"/> hook to be enabled.
        /// </para>
        /// </summary>
        public event InventoryContextMenuOpenEventDelegate? OpenInventoryContextMenu;

        /// <summary>
        /// The delegate that is run when a context menu item is selected.
        /// </summary>
        public delegate void ContextMenuItemSelectedDelegate(ContextMenuItemSelectedArgs args);

        /// <summary>
        /// The delegate that is run when an inventory context menu item is selected.
        /// </summary>
        public delegate void InventoryContextMenuItemSelectedDelegate(InventoryContextMenuItemSelectedArgs args);

        private delegate IntPtr SomeOpenAddonThingDelegate(IntPtr a1, IntPtr a2, IntPtr a3, uint a4, IntPtr a5, IntPtr a6, IntPtr a7, ushort a8);

        private Hook<SomeOpenAddonThingDelegate>? SomeOpenAddonThingHook { get; }

        private unsafe delegate byte ContextMenuOpenDelegate(IntPtr addon, int menuSize, AtkValue* atkValueArgs);

        private delegate IntPtr GetAddonByInternalIdDelegate(IntPtr raptureAtkUnitManager, short id);

        private readonly GetAddonByInternalIdDelegate _getAddonByInternalId = null!;

        private Hook<ContextMenuOpenDelegate>? ContextMenuOpenHook { get; }
        private Hook<ContextMenuOpenDelegate>? TitleContextMenuOpenHook { get; }

        private delegate byte ContextMenuItemSelectedInternalDelegate(IntPtr addon, int index, byte a3);

        private Hook<ContextMenuItemSelectedInternalDelegate>? ContextMenuItemSelectedHook { get; }

        private delegate byte SetUpContextSubMenuDelegate(IntPtr agent);

        private readonly SetUpContextSubMenuDelegate _setUpContextSubMenu = null!;

        private delegate byte ContextMenuEvent66Delegate(IntPtr agent);

        private Hook<ContextMenuEvent66Delegate>? ContextMenuEvent66Hook { get; }

        private unsafe delegate void AtkValueChangeTypeDelegate(AtkValue* thisPtr, ValueType type);

        private readonly AtkValueChangeTypeDelegate _atkValueChangeType = null!;

        private unsafe delegate void AtkValueSetStringDelegate(AtkValue* thisPtr, byte* bytes);

        private readonly AtkValueSetStringDelegate _atkValueSetString = null!;

        private GameFunctions Functions { get; }
        private ClientLanguage Language { get; }
        private SeStringManager SeStringManager { get; }
        private IntPtr Agent { get; set; } = IntPtr.Zero;
        private List<BaseContextMenuItem> Items { get; } = new();
        private int NormalSize { get; set; }

        internal ContextMenu(GameFunctions functions, SigScanner scanner, SeStringManager manager, ClientLanguage language, Hooks hooks) {
            this.Functions = functions;
            this.Language = language;
            this.SeStringManager = manager;

            if (!hooks.HasFlag(Hooks.ContextMenu)) {
                return;
            }

            if (scanner.TryScanText(Signatures.AtkValueChangeType, out var changeTypePtr, "Context Menu (change type)")) {
                this._atkValueChangeType = Marshal.GetDelegateForFunctionPointer<AtkValueChangeTypeDelegate>(changeTypePtr);
            } else {
                return;
            }

            if (scanner.TryScanText(Signatures.AtkValueSetString, out var setStringPtr, "Context Menu (set string)")) {
                this._atkValueSetString = Marshal.GetDelegateForFunctionPointer<AtkValueSetStringDelegate>(setStringPtr);
            } else {
                return;
            }

            if (scanner.TryScanText(Signatures.GetAddonByInternalId, out var getAddonPtr, "Context Menu (get addon)")) {
                this._getAddonByInternalId = Marshal.GetDelegateForFunctionPointer<GetAddonByInternalIdDelegate>(getAddonPtr);
            } else {
                return;
            }

            if (scanner.TryScanText(Signatures.SetUpContextSubMenu, out var setUpSubPtr, "Context Menu (set up submenu)")) {
                this._setUpContextSubMenu = Marshal.GetDelegateForFunctionPointer<SetUpContextSubMenuDelegate>(setUpSubPtr);
            } else {
                return;
            }

            if (scanner.TryScanText(Signatures.SomeOpenAddonThing, out var thingPtr, "Context Menu (some OpenAddon thing)")) {
                this.SomeOpenAddonThingHook = new Hook<SomeOpenAddonThingDelegate>(thingPtr, new SomeOpenAddonThingDelegate(this.SomeOpenAddonThingDetour));
                this.SomeOpenAddonThingHook.Enable();
            } else {
                return;
            }

            if (scanner.TryScanText(Signatures.ContextMenuOpen, out var openPtr, "Context Menu open")) {
                unsafe {
                    this.ContextMenuOpenHook = new Hook<ContextMenuOpenDelegate>(openPtr, new ContextMenuOpenDelegate(this.OpenMenuDetour));
                }

                this.ContextMenuOpenHook.Enable();
            } else {
                return;
            }

            if (scanner.TryScanText(Signatures.ContextMenuSelected, out var selectedPtr, "Context Menu selected")) {
                this.ContextMenuItemSelectedHook = new Hook<ContextMenuItemSelectedInternalDelegate>(selectedPtr, new ContextMenuItemSelectedInternalDelegate(this.ItemSelectedDetour));
                this.ContextMenuItemSelectedHook.Enable();
            }

            if (scanner.TryScanText(Signatures.TitleContextMenuOpen, out var titleOpenPtr, "Context Menu (title menu open)")) {
                unsafe {
                    this.TitleContextMenuOpenHook = new Hook<ContextMenuOpenDelegate>(titleOpenPtr, new ContextMenuOpenDelegate(this.TitleContextMenuOpenDetour));
                }

                this.TitleContextMenuOpenHook.Enable();
            }

            if (scanner.TryScanText(Signatures.ContextMenuEvent66, out var event66Ptr, "Context Menu (event 66)")) {
                this.ContextMenuEvent66Hook = new Hook<ContextMenuEvent66Delegate>(event66Ptr, new ContextMenuEvent66Delegate(this.ContextMenuEvent66Detour));
                this.ContextMenuEvent66Hook.Enable();
            }
        }

        /// <inheritdoc />
        public void Dispose() {
            this.SomeOpenAddonThingHook?.Dispose();
            this.ContextMenuOpenHook?.Dispose();
            this.TitleContextMenuOpenHook?.Dispose();
            this.ContextMenuItemSelectedHook?.Dispose();
            this.ContextMenuEvent66Hook?.Dispose();
        }

        private IntPtr SomeOpenAddonThingDetour(IntPtr a1, IntPtr a2, IntPtr a3, uint a4, IntPtr a5, IntPtr a6, IntPtr a7, ushort a8) {
            this.Agent = a6;
            return this.SomeOpenAddonThingHook!.Original(a1, a2, a3, a4, a5, a6, a7, a8);
        }

        private unsafe byte TitleContextMenuOpenDetour(IntPtr addon, int menuSize, AtkValue* atkValueArgs) {
            if (this.SubMenuTitle == IntPtr.Zero) {
                this.Items.Clear();
            }

            return this.TitleContextMenuOpenHook!.Original(addon, menuSize, atkValueArgs);
        }

        private enum AgentType {
            Normal,
            Inventory,
            Unknown,
        }

        private (AgentType agentType, IntPtr agent) GetContextMenuAgent() {
            var agentType = AgentType.Unknown;
            if (this.Agent == this.Functions.GetAgentByInternalId(9u)) {
                agentType = AgentType.Normal;
            } else if (this.Agent == this.Functions.GetAgentByInternalId(10u)) {
                agentType = AgentType.Inventory;
            }

            return (agentType, this.Agent);
        }

        private unsafe string? GetParentAddonName(IntPtr addon) {
            var parentAddonId = Marshal.ReadInt16(addon + ParentAddonIdOffset);
            if (parentAddonId == 0) {
                return null;
            }

            var stage = (AtkStage*) this.Functions.GetAtkStageSingleton();
            var parentAddon = this._getAddonByInternalId((IntPtr) stage->RaptureAtkUnitManager, parentAddonId);
            return Encoding.UTF8.GetString(Util.ReadTerminated(parentAddon + 8));
        }

        private unsafe (uint actorId, uint contentIdLower, SeString? text, ushort actorWorld) GetAgentInfo(IntPtr agent) {
            var actorId = *(uint*) (agent + ActorIdOffset);
            var contentIdLower = *(uint*) (agent + ContentIdLowerOffset);
            var textBytes = Util.ReadTerminated(Marshal.ReadIntPtr(agent + TextPointerOffset));
            var text = textBytes.Length == 0 ? null : this.SeStringManager.Parse(textBytes);
            var actorWorld = *(ushort*) (agent + WorldOffset);
            return (actorId, contentIdLower, text, actorWorld);
        }

        private static unsafe (uint itemId, uint itemAmount, bool itemHq) GetInventoryAgentInfo(IntPtr agent) {
            var itemId = *(uint*) (agent + ItemIdOffset);
            var itemAmount = *(uint*) (agent + ItemAmountOffset);
            var itemHq = *(byte*) (agent + ItemHqOffset) == 1;
            return (itemId, itemAmount, itemHq);
        }

        [HandleProcessCorruptedStateExceptions]
        private unsafe byte OpenMenuDetour(IntPtr addon, int menuSize, AtkValue* atkValueArgs) {
            try {
                this.OpenMenuDetourInner(addon, ref menuSize, ref atkValueArgs);
            } catch (Exception ex) {
                Logger.LogError(ex, "Exception in OpenMenuDetour");
            }

            return this.ContextMenuOpenHook!.Original(addon, menuSize, atkValueArgs);
        }

        private unsafe AtkValue* ExpandContextMenuArray(IntPtr addon) {
            const ulong newItemCount = MaxItems * 2 + ContextMenuItemOffset;

            var oldArray = *(AtkValue**) (addon + AddonArrayOffset);
            var oldArrayItemCount = *(ushort*) (addon + AddonArraySizeOffset);

            // if the array has enough room, don't reallocate
            if (oldArrayItemCount >= newItemCount) {
                return oldArray;
            }

            // reallocate
            var size = (ulong) sizeof(AtkValue) * newItemCount + 8;
            var newArray = this.Functions.UiAlloc.Alloc(size);
            // zero new memory
            Marshal.Copy(new byte[size], 0, newArray, (int) size);
            // update size and pointer
            *(ulong*) newArray = newItemCount;
            *(void**) (addon + AddonArrayOffset) = (void*) (newArray + 8);
            *(ushort*) (addon + AddonArraySizeOffset) = (ushort) newItemCount;

            // copy old memory if existing
            if (oldArray != null) {
                Buffer.MemoryCopy(oldArray, (void*) (newArray + 8), size, (ulong) sizeof(AtkValue) * oldArrayItemCount);
                this.Functions.UiAlloc.Free((IntPtr) oldArray - 8);
            }

            return (AtkValue*) (newArray + 8);
        }

        private unsafe void OpenMenuDetourInner(IntPtr addon, ref int menuSize, ref AtkValue* atkValueArgs) {
            this.Items.Clear();
            this.FreeSubMenuTitle();

            var (agentType, agent) = this.GetContextMenuAgent();
            if (agent == IntPtr.Zero) {
                return;
            }

            if (agentType == AgentType.Unknown) {
                return;
            }

            atkValueArgs = this.ExpandContextMenuArray(addon);

            var inventory = agentType == AgentType.Inventory;
            var offset = ContextMenuItemOffset + (inventory ? 0 : *(long*) (agent + BooleanOffsetCheck) != 0 ? 1 : 0);

            this.NormalSize = (int) (&atkValueArgs[0])->UInt;

            // idx 3 is bitmask of indices that are submenus
            var submenuArg = &atkValueArgs[3];
            var submenus = (int) submenuArg->UInt;

            var hasGameDisabled = menuSize - offset - this.NormalSize > 0;

            var addonName = this.GetParentAddonName(addon);

            var menuActions = inventory
                ? (byte*) (agent + InventoryMenuActionsOffset)
                : (byte*) (Marshal.ReadIntPtr(agent + MenuActionsPointerOffset) + MenuActionsOffset);

            var nativeItems = new List<NativeContextMenuItem>();
            for (var i = 0; i < this.NormalSize; i++) {
                var atkItem = &atkValueArgs[offset + i];

                var name = Util.ReadSeString((IntPtr) atkItem->String, this.SeStringManager);

                var enabled = true;
                if (hasGameDisabled) {
                    var disabledItem = &atkValueArgs[offset + this.NormalSize + i];
                    enabled = disabledItem->Int == 0;
                }

                var action = *(menuActions + offset + i);

                var isSubMenu = (submenus & (1 << i)) > 0;

                nativeItems.Add(new NativeContextMenuItem(action, name, enabled, isSubMenu));
            }

            if (inventory) {
                var info = GetInventoryAgentInfo(agent);

                var args = new InventoryContextMenuOpenArgs(
                    addon,
                    agent,
                    addonName,
                    info.itemId,
                    info.itemAmount,
                    info.itemHq
                );
                args.Items.AddRange(nativeItems);

                try {
                    this.OpenInventoryContextMenu?.Invoke(args);
                } catch (Exception ex) {
                    Logger.LogError(ex, "Exception in OpenMenuDetour");
                    return;
                }

                // remove any NormalContextMenuItems that may have been added - these will crash the game
                args.Items.RemoveAll(item => item is NormalContextMenuItem);

                // set the agent of any remaining custom items
                foreach (var item in args.Items) {
                    if (item is InventoryContextMenuItem custom) {
                        custom.Agent = agent;
                    }
                }

                this.Items.AddRange(args.Items);
            } else {
                var info = this.GetAgentInfo(agent);

                var args = new ContextMenuOpenArgs(
                    addon,
                    agent,
                    addonName,
                    info.actorId,
                    info.contentIdLower,
                    info.text,
                    info.actorWorld
                );
                args.Items.AddRange(nativeItems);

                try {
                    this.OpenContextMenu?.Invoke(args);
                } catch (Exception ex) {
                    Logger.LogError(ex, "Exception in OpenMenuDetour");
                    return;
                }

                // remove any InventoryContextMenuItems that may have been added - these will crash the game
                args.Items.RemoveAll(item => item is InventoryContextMenuItem);

                // set the agent of any remaining custom items
                foreach (var item in args.Items) {
                    if (item is NormalContextMenuItem custom) {
                        custom.Agent = agent;
                    }
                }

                this.Items.AddRange(args.Items);
            }

            if (this.Items.Count > MaxItems) {
                var toRemove = this.Items.Count - MaxItems;
                this.Items.RemoveRange(MaxItems, toRemove);
                Logger.LogWarning($"Context menu item limit ({MaxItems}) exceeded. Removing {toRemove} item(s).");
            }

            var hasCustomDisabled = this.Items.Any(item => !item.Enabled);
            var hasAnyDisabled = hasGameDisabled || hasCustomDisabled;

            // clear all submenu flags
            submenuArg->UInt = 0;

            for (var i = 0; i < this.Items.Count; i++) {
                var item = this.Items[i];

                if (hasAnyDisabled) {
                    var disabledArg = &atkValueArgs[offset + this.Items.Count + i];
                    this._atkValueChangeType(disabledArg, ValueType.Int);
                    disabledArg->Int = item.Enabled ? 0 : 1;
                }

                // set up the agent to take the appropriate action for this item
                *(menuActions + offset + i) = item switch {
                    NativeContextMenuItem nativeItem => nativeItem.InternalAction,
                    ContextSubMenuItem => 0x66,
                    _ => inventory ? InventoryNoopContextId : NoopContextId,
                };

                // set submenu flag
                if (item.IsSubMenu) {
                    submenuArg->UInt |= (uint) (1 << i);
                }

                // set up the menu item
                var newItem = &atkValueArgs[offset + i];
                this._atkValueChangeType(newItem, ValueType.String);

                var name = this.GetItemName(item);
                fixed (byte* nameBytesPtr = name.Encode().Terminate()) {
                    this._atkValueSetString(newItem, nameBytesPtr);
                }
            }

            (&atkValueArgs[0])->UInt = (uint) this.Items.Count;

            menuSize = (int) (&atkValueArgs[0])->UInt;
            if (hasAnyDisabled) {
                menuSize *= 2;
            }

            menuSize += offset;
        }

        private SeString GetItemName(BaseContextMenuItem item) {
            return item switch {
                NormalContextMenuItem custom => this.Language switch {
                    ClientLanguage.Japanese => custom.NameJapanese,
                    ClientLanguage.English => custom.NameEnglish,
                    ClientLanguage.German => custom.NameGerman,
                    ClientLanguage.French => custom.NameFrench,
                    _ => custom.NameEnglish,
                },
                InventoryContextMenuItem custom => this.Language switch {
                    ClientLanguage.Japanese => custom.NameJapanese,
                    ClientLanguage.English => custom.NameEnglish,
                    ClientLanguage.German => custom.NameGerman,
                    ClientLanguage.French => custom.NameFrench,
                    _ => custom.NameEnglish,
                },
                ContextSubMenuItem custom => this.Language switch {
                    ClientLanguage.Japanese => custom.NameJapanese,
                    ClientLanguage.English => custom.NameEnglish,
                    ClientLanguage.German => custom.NameGerman,
                    ClientLanguage.French => custom.NameFrench,
                    _ => custom.NameEnglish,
                },
                NativeContextMenuItem native => native.Name,
                _ => "Invalid context menu item",
            };
        }

        private ContextSubMenuItem? SubMenuItem { get; set; }

        private byte ItemSelectedDetour(IntPtr addon, int index, byte a3) {
            this.FreeSubMenuTitle();

            if (index < 0 || index >= this.Items.Count) {
                goto Original;
            }

            var item = this.Items[index];
            switch (item) {
                case ContextSubMenuItem sub: {
                    this.SubMenuItem = sub;
                    break;
                }
                // a custom item is being clicked
                case NormalContextMenuItem custom: {
                    var addonName = this.GetParentAddonName(addon);
                    var info = this.GetAgentInfo(custom.Agent);

                    var args = new ContextMenuItemSelectedArgs(
                        addon,
                        custom.Agent,
                        addonName,
                        info.actorId,
                        info.contentIdLower,
                        info.text,
                        info.actorWorld
                    );

                    try {
                        custom.Action(args);
                    } catch (Exception ex) {
                        Logger.LogError(ex, "Exception in custom context menu item");
                    }

                    break;
                }
                case InventoryContextMenuItem custom: {
                    var addonName = this.GetParentAddonName(addon);
                    var info = GetInventoryAgentInfo(custom.Agent);

                    var args = new InventoryContextMenuItemSelectedArgs(
                        addon,
                        custom.Agent,
                        addonName,
                        info.itemId,
                        info.itemAmount,
                        info.itemHq
                    );

                    try {
                        custom.Action(args);
                    } catch (Exception ex) {
                        Logger.LogError(ex, "Exception in custom context menu item");
                    }

                    break;
                }
            }

            Original:
            return this.ContextMenuItemSelectedHook!.Original(addon, index, a3);
        }

        private IntPtr SubMenuTitle { get; set; } = IntPtr.Zero;

        private void FreeSubMenuTitle() {
            if (this.SubMenuTitle == IntPtr.Zero) {
                return;
            }

            this.Functions.UiAlloc.Free(this.SubMenuTitle);
            this.SubMenuTitle = IntPtr.Zero;
        }

        private unsafe byte ContextMenuEvent66Detour(IntPtr agent) {
            if (this.SubMenuItem == null) {
                return this.ContextMenuEvent66Hook!.Original(agent);
            }

            // free our workaround pointer
            this.FreeSubMenuTitle();

            this.Items.Clear();

            try {
                // this will attempt to read the header from the agent
                // we don't currently update the agent with our new items, so let's just work around it
                var name = this.Language switch {
                    ClientLanguage.Japanese => this.SubMenuItem.NameJapanese,
                    ClientLanguage.English => this.SubMenuItem.NameEnglish,
                    ClientLanguage.German => this.SubMenuItem.NameGerman,
                    ClientLanguage.French => this.SubMenuItem.NameFrench,
                    _ => this.SubMenuItem.NameEnglish,
                };

                // Since the game checks the agent's AtkValue array for the submenu title, and since we
                // don't update that array, we need to work around this check.
                // First, we will convince the game to make the submenu title pointer null by telling it
                // that an invalid index was selected.
                // Second, we will replace the null pointer with our own pointer.
                // Third, we will restore the original selected index.

                // step 1
                var selectedIdx = (byte*) (agent + 0x670);
                var wasSelected = *selectedIdx;
                *selectedIdx = 0xFF;
                this._setUpContextSubMenu(agent);

                // step 2 (see SetUpContextSubMenu)
                var nameBytes = name.Encode().Terminate();
                this.SubMenuTitle = this.Functions.UiAlloc.Alloc((ulong) nameBytes.Length);
                Marshal.Copy(nameBytes, 0, this.SubMenuTitle, nameBytes.Length);
                var v10 = agent + 0x678 * *(byte*) (agent + 0x1740) + 0x28;
                *(byte**) (v10 + 0x668) = (byte*) this.SubMenuTitle;

                // step 3
                *selectedIdx = wasSelected;

                var secondaryArgsPtr = Marshal.ReadIntPtr(agent + MenuActionsPointerOffset);
                var submenuArgs = (AtkValue*) (secondaryArgsPtr + 8);
                var size = *(ushort*) secondaryArgsPtr;

                var info = this.GetAgentInfo(agent);

                var args = new ContextMenuOpenArgs(
                    IntPtr.Zero,
                    agent,
                    string.Empty,
                    info.actorId,
                    info.contentIdLower,
                    info.text,
                    info.actorWorld
                );
                this.SubMenuItem.Action(args);
                // remove any InventoryContextMenuItems that may have been added - these will crash the game
                args.Items.RemoveAll(item => item is InventoryContextMenuItem);

                // set the agent of any remaining custom items
                foreach (var item in args.Items) {
                    if (item is NormalContextMenuItem custom) {
                        custom.Agent = agent;
                    }
                }

                this.Items.AddRange(args.Items);

                var booleanOffset = *(long*) (agent + *(byte*) (agent + 0x1740) * 0x678 + 0x690) != 0 ? 1 : 0;

                for (var i = 0; i < args.Items.Count; i++) {
                    var item = args.Items[i];

                    *(ushort*) secondaryArgsPtr += 1;
                    var arg = &submenuArgs[size + i];
                    this._atkValueChangeType(arg, ValueType.String);
                    var itemName = this.GetItemName(item);
                    fixed (byte* namePtr = itemName.Encode().Terminate()) {
                        this._atkValueSetString(arg, namePtr);
                    }

                    // set action to no-op
                    *(byte*) (secondaryArgsPtr + booleanOffset + i + ContextMenuItemOffset + 0x428) = NoopContextId;
                }
            } finally {
                this.SubMenuItem = null;
            }


            return 0;
        }
    }
}
