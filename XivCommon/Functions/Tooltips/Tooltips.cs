using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Game.Internal;
using Dalamud.Game.Internal.Gui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;

namespace XivCommon.Functions.Tooltips {
    /// <summary>
    /// The class containing tooltip functionality
    /// </summary>
    public class Tooltips : IDisposable {
        private static class Signatures {
            internal const string ItemGenerateTooltip = "48 89 5C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 83 EC 50 48 8B 42 ??";
            internal const string ActionGenerateTooltip = "E8 ?? ?? ?? ?? 48 8B D5 48 8B CF E8 ?? ?? ?? ?? 41 8D 45 FF 83 F8 01 77 6D";
            internal const string SadSetString = "E8 ?? ?? ?? ?? F6 47 14 08";
        }

        internal unsafe delegate void StringArrayDataSetStringDelegate(IntPtr self, int index, byte* str, byte updatePtr, byte copyToUi, byte dontSetModified);

        private unsafe delegate IntPtr ItemGenerateTooltipDelegate(IntPtr addon, int** numberArrayData, byte*** stringArrayData);

        private unsafe delegate IntPtr ActionGenerateTooltipDelegate(IntPtr addon, int** numberArrayData, byte*** stringArrayData);

        private StringArrayDataSetStringDelegate? SadSetString { get; }
        private Hook<ItemGenerateTooltipDelegate>? ItemGenerateTooltipHook { get; }
        private Hook<ActionGenerateTooltipDelegate>? ActionGenerateTooltipHook { get; }

        /// <summary>
        /// The delegate for item tooltip events.
        /// </summary>
        public delegate void ItemTooltipEventDelegate(ItemTooltip itemTooltip, ulong itemId);

        /// <summary>
        /// The tooltip for action tooltip events.
        /// </summary>
        public delegate void ActionTooltipEventDelegate(ActionTooltip actionTooltip, HoveredAction action);

        /// <summary>
        /// <para>
        /// The event that is fired when an item tooltip is being generated for display.
        /// </para>
        /// <para>
        /// Requires the <see cref="Hooks.Tooltips"/> hook to be enabled.
        /// </para>
        /// </summary>
        public event ItemTooltipEventDelegate? OnItemTooltip;

        /// <summary>
        /// <para>
        /// The event that is fired when an action tooltip is being generated for display.
        /// </para>
        /// <para>
        /// Requires the <see cref="Hooks.Tooltips"/> hook to be enabled.
        /// </para>
        /// </summary>
        public event ActionTooltipEventDelegate? OnActionTooltip;

        private Framework Framework { get; }
        private GameGui GameGui { get; }
        private SeStringManager SeStringManager { get; }
        private ItemTooltip? ItemTooltip { get; set; }
        private ActionTooltip? ActionTooltip { get; set; }

        private ulong LastItem { get; set; }
        private bool ItemVisible { get; set; }
        private bool ItemStateChanged { get; set; } = true;

        private HoveredAction? LastAction { get; set; }
        private bool ActionVisible { get; set; }
        private bool ActionStateChanged { get; set; } = true;

        internal Tooltips(SigScanner scanner, Framework framework, GameGui gui, SeStringManager manager, bool enabled) {
            this.Framework = framework;
            this.GameGui = gui;
            this.SeStringManager = manager;

            if (scanner.TryScanText(Signatures.SadSetString, out var setStringPtr, "Tooltips - StringArrayData::SetString")) {
                this.SadSetString = Marshal.GetDelegateForFunctionPointer<StringArrayDataSetStringDelegate>(setStringPtr);
            } else {
                return;
            }

            if (!enabled) {
                return;
            }

            this.Framework.OnUpdateEvent += this.OnFrameworkUpdate;

            if (scanner.TryScanText(Signatures.ItemGenerateTooltip, out var generateItemPtr, "Tooltips - Items")) {
                unsafe {
                    this.ItemGenerateTooltipHook = new Hook<ItemGenerateTooltipDelegate>(generateItemPtr, new ItemGenerateTooltipDelegate(this.ItemGenerateTooltipDetour));
                }

                this.ItemGenerateTooltipHook.Enable();
            }

            if (scanner.TryScanText(Signatures.ActionGenerateTooltip, out var actionItemPtr, "Tooltips - Actions")) {
                unsafe {
                    this.ActionGenerateTooltipHook = new Hook<ActionGenerateTooltipDelegate>(actionItemPtr, new ActionGenerateTooltipDelegate(this.ActionGenerateTooltipDetour));
                }

                this.ActionGenerateTooltipHook.Enable();
            }
        }

        /// <inheritdoc />
        public void Dispose() {
            this.ActionGenerateTooltipHook?.Dispose();
            this.ItemGenerateTooltipHook?.Dispose();
            this.Framework.OnUpdateEvent -= this.OnFrameworkUpdate;
        }

        private void OnFrameworkUpdate(Framework framework) {
            var itemVisible = framework.Gui.GetAddonByName("ItemDetail", 1)?.Visible ?? false;
            var actionVisible = framework.Gui.GetAddonByName("ActionDetail", 1)?.Visible ?? false;

            if (itemVisible != this.ItemVisible) {
                this.ItemStateChanged = true;
            }

            if (actionVisible != this.ActionVisible) {
                this.ActionStateChanged = true;
            }

            this.ItemVisible = itemVisible;
            this.ActionVisible = actionVisible;
        }

        private unsafe IntPtr ItemGenerateTooltipDetour(IntPtr addon, int** numberArrayData, byte*** stringArrayData) {
            try {
                this.ItemGenerateTooltipDetourInner(numberArrayData, stringArrayData);
            } catch (Exception ex) {
                Logger.LogError(ex, "Exception in item tooltip detour");
            }

            return this.ItemGenerateTooltipHook!.Original(addon, numberArrayData, stringArrayData);
        }

        private unsafe void ItemGenerateTooltipDetourInner(int** numberArrayData, byte*** stringArrayData) {
            var itemId = this.GameGui.HoveredItem;
            if (this.ItemStateChanged || this.LastItem != itemId) {
                this.ItemStateChanged = false;

                this.ItemTooltip = new ItemTooltip(this.SeStringManager, this.SadSetString!, stringArrayData, numberArrayData);

                try {
                    this.OnItemTooltip?.Invoke(this.ItemTooltip, itemId);
                } catch (Exception ex) {
                    Logger.LogError(ex, "Exception in OnItemTooltip event");
                }
            }

            this.LastItem = itemId;
        }

        private unsafe IntPtr ActionGenerateTooltipDetour(IntPtr addon, int** numberArrayData, byte*** stringArrayData) {
            try {
                this.ActionGenerateTooltipDetourInner(numberArrayData, stringArrayData);
            } catch (Exception ex) {
                Logger.LogError(ex, "Exception in action tooltip detour");
            }

            return this.ActionGenerateTooltipHook!.Original(addon, numberArrayData, stringArrayData);
        }

        private unsafe void ActionGenerateTooltipDetourInner(int** numberArrayData, byte*** stringArrayData) {
            var action = this.GameGui.HoveredAction;
            if (this.ActionStateChanged || this.LastAction != action) {
                this.ActionStateChanged = false;

                this.ActionTooltip = new ActionTooltip(this.SeStringManager, this.SadSetString!, stringArrayData, numberArrayData);

                try {
                    this.OnActionTooltip?.Invoke(this.ActionTooltip, action);
                } catch (Exception ex) {
                    Logger.LogError(ex, "Exception in OnActionTooltip event");
                }
            }

            this.LastAction = action;
        }
    }
}
