using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace XivCommon.Functions {
    /// <summary>
    /// Context menu functions
    /// </summary>
    public class ContextMenu : IDisposable {
        private static class Signatures {
            internal const string ContextMenuOpen = "48 8B C4 57 41 56 41 57 48 81 EC ?? ?? ?? ??";
            internal const string ContextMenuSelected = "48 89 5C 24 ?? 55 57 41 56 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 44 24 ?? 80 B9 ?? ?? ?? ?? ??";
            internal const string AtkValueChangeType = "E8 ?? ?? ?? ?? 45 84 F6 48 8D 4C 24 ??";
            internal const string AtkValueSetString = "E8 ?? ?? ?? ?? 41 03 ED";
        }

        private const int MenuTypeOffset = 0x1D2;
        private const int MenuActionsOffset = 0x42F;
        private const int NoopContextId = 0x67;

        private unsafe delegate byte ContextMenuOpenDelegate(IntPtr agent, int menuSize, AtkValue* atkValueArgs);

        private Hook<ContextMenuOpenDelegate>? ContextMenuOpenHook { get; }

        private delegate byte ContextMenuItemSelectedDelegate(IntPtr agent, int index, byte a3);

        private Hook<ContextMenuItemSelectedDelegate>? ContextMenuItemSelectedHook { get; }

        private unsafe delegate void AtkValueChangeTypeDelegate(AtkValue* thisPtr, ValueType type);

        private readonly AtkValueChangeTypeDelegate _atkValueChangeType;

        private unsafe delegate void AtkValueSetStringDelegate(AtkValue* thisPtr, byte* bytes);

        private readonly AtkValueSetStringDelegate _atkValueSetString;

        private ClientLanguage Language { get; }
        private Dictionary<ContextMenuType, List<ContextMenuItem>> Items { get; } = new();
        private int NormalSize { get; set; }

        internal ContextMenu(SigScanner scanner, ClientLanguage language) {
            this.Language = language;

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

            if (scanner.TryScanText(Signatures.ContextMenuOpen, out var openPtr, "Context Menu open")) {
                unsafe {
                    this.ContextMenuOpenHook = new Hook<ContextMenuOpenDelegate>(openPtr, new ContextMenuOpenDelegate(this.OpenMenuDetour));
                }

                this.ContextMenuOpenHook.Enable();
            } else {
                return;
            }

            if (scanner.TryScanText(Signatures.ContextMenuSelected, out var selectedPtr, "Context Menu selected")) {
                this.ContextMenuItemSelectedHook = new Hook<ContextMenuItemSelectedDelegate>(selectedPtr, new ContextMenuItemSelectedDelegate(this.ItemSelectedDetour));
                this.ContextMenuItemSelectedHook.Enable();
            }
        }

        /// <inheritdoc />
        public void Dispose() {
            this.ContextMenuOpenHook?.Dispose();
            this.ContextMenuItemSelectedHook?.Dispose();
        }

        private unsafe byte OpenMenuDetour(IntPtr agent, int menuSize, AtkValue* atkValueArgs) {
            this.NormalSize = menuSize - 7;

            var menuType = Marshal.ReadInt16(agent + MenuTypeOffset);

            if (!this.Items.TryGetValue((ContextMenuType) menuType, out var registered)) {
                goto Original;
            }

            foreach (var item in registered) {
                // increment the menu size
                menuSize += 1;
                (&atkValueArgs[0])->UInt += 1;

                // set up the agent to ignore this item
                Marshal.WriteByte(agent + MenuActionsOffset + menuSize - 7, NoopContextId);

                // set up the new menu item
                var newItem = &atkValueArgs[menuSize - 1];
                this._atkValueChangeType(newItem, ValueType.String);
                var name = this.Language switch {
                    ClientLanguage.Japanese => item.NameJapanese,
                    ClientLanguage.English => item.NameEnglish,
                    ClientLanguage.German => item.NameGerman,
                    ClientLanguage.French => item.NameFrench,
                    _ => throw new ArgumentOutOfRangeException(),
                };
                var nameBytes = Encoding.UTF8.GetBytes(name).Terminate();
                fixed (byte* nameBytesPtr = nameBytes) {
                    this._atkValueSetString(newItem, nameBytesPtr);
                }
            }

            Original:
            return this.ContextMenuOpenHook!.Original(agent, menuSize, atkValueArgs);
        }

        private byte ItemSelectedDetour(IntPtr agent, int index, byte a3) {
            var menuType = Marshal.ReadInt16(agent + MenuTypeOffset);

            // a custom item is being clicked
            if (index >= this.NormalSize) {
                if (!this.Items.TryGetValue((ContextMenuType) menuType, out var registered)) {
                    goto Original;
                }

                var idx = index - this.NormalSize;
                if (registered.Count <= idx) {
                    goto Original;
                }

                var item = registered[idx];
                try {
                    item.Action();
                } catch (Exception ex) {
                    PluginLog.LogError(ex, "Exception in custom context menu item");
                }
            }

            Original:
            return this.ContextMenuItemSelectedHook!.Original(agent, index, a3);
        }

        /// <summary>
        /// Register a menu item to appear in a context menu.
        /// </summary>
        /// <param name="type">the context menu type to show the item in</param>
        /// <param name="item">the item to be shown</param>
        public void RegisterAction(ContextMenuType type, ContextMenuItem item) {
            if (!this.Items.TryGetValue(type, out var registered)) {
                this.Items[type] = new List<ContextMenuItem>();
                registered = this.Items[type];
            }

            registered.Add(item);
        }

        /// <summary>
        /// Remove a previously-registered context menu item.
        /// </summary>
        /// <param name="type">the context menu type the item was registered under</param>
        /// <param name="item">the item to be removed</param>
        public void UnregisterAction(ContextMenuType type, ContextMenuItem item) {
            this.UnregisterAction(type, item.Id);
        }

        /// <summary>
        /// Remove a previously-registered context menu item.
        /// </summary>
        /// <param name="type">the context menu type the item was registered under</param>
        /// <param name="id">the id of the item to be removed</param>
        public void UnregisterAction(ContextMenuType type, Guid id) {
            if (!this.Items.TryGetValue(type, out var registered)) {
                return;
            }

            registered.RemoveAll(item => item.Id == id);
        }
    }

    /// <summary>
    /// Context menu types
    /// </summary>
    public enum ContextMenuType : ushort {
        /// <summary>
        /// Context menu shown when right-clicking a Party Finder listing.
        /// </summary>
        PartyFinder = 0x72,
    }

    /// <summary>
    /// A custom context menu item
    /// </summary>
    public class ContextMenuItem {
        /// <summary>
        /// A unique ID to identify this item.
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>
        /// The name of the context item to be shown for English clients.
        /// </summary>
        public string NameEnglish { get; }
        /// <summary>
        /// The name of the context item to be shown for Japanese clients.
        /// </summary>
        public string NameJapanese { get; }
        /// <summary>
        /// The name of the context item to be shown for French clients.
        /// </summary>
        public string NameFrench { get; }
        /// <summary>
        /// The name of the context item to be shown for German clients.
        /// </summary>
        public string NameGerman { get; }

        /// <summary>
        /// The action to perform when this item is clicked.
        /// </summary>
        public Action Action { get; }

        /// <summary>
        /// Create a new context menu item.
        /// </summary>
        /// <param name="name">the English name of the item, copied to other languages</param>
        /// <param name="action">the action to perform on click</param>
        public ContextMenuItem(string name, Action action) {
            this.NameEnglish = name;
            this.NameJapanese = name;
            this.NameFrench = name;
            this.NameGerman = name;

            this.Action = action;
        }
    }
}
