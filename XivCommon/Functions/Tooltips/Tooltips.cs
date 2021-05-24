using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
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

        private GameGui GameGui { get; }
        private SeStringManager SeStringManager { get; }
        private ItemTooltip? ItemTooltip { get; set; }
        private ActionTooltip? ActionTooltip { get; set; }

        internal Tooltips(SigScanner scanner, GameGui gui, SeStringManager manager, bool enabled) {
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
        }

        private unsafe IntPtr ItemGenerateTooltipDetour(IntPtr addon, int** numberArrayData, byte*** stringArrayData) {
            try {
                return this.ItemGenerateTooltipDetourInner(addon, numberArrayData, stringArrayData);
            } catch (Exception ex) {
                Logger.LogError(ex, "Exception in item tooltip detour");
            }

            return this.ItemGenerateTooltipHook!.Original(addon, numberArrayData, stringArrayData);
        }

        private unsafe IntPtr ItemGenerateTooltipDetourInner(IntPtr addon, int** numberArrayData, byte*** stringArrayData) {
            // var v3 = *(numberArrayData + 4);
            // var v9 = *(v3 + 4);
            //
            // if ((v9 & 2) == 0) {
            //     goto Original;
            // }

            this.ItemTooltip = new ItemTooltip(this.SeStringManager, this.SadSetString!, stringArrayData, numberArrayData);

            this.OnItemTooltip?.Invoke(this.ItemTooltip, this.GameGui.HoveredItem);

            return this.ItemGenerateTooltipHook!.Original(addon, numberArrayData, stringArrayData);
        }

        private unsafe IntPtr ActionGenerateTooltipDetour(IntPtr addon, int** numberArrayData, byte*** stringArrayData) {
            try {
                return this.ActionGenerateTooltipDetourInner(addon, numberArrayData, stringArrayData);
            } catch (Exception ex) {
                Logger.LogError(ex, "Exception in action tooltip detour");
            }

            return this.ActionGenerateTooltipHook!.Original(addon, numberArrayData, stringArrayData);
        }

        private unsafe IntPtr ActionGenerateTooltipDetourInner(IntPtr addon, int** numberArrayData, byte*** stringArrayData) {
            this.ActionTooltip = new ActionTooltip(this.SeStringManager, this.SadSetString!, stringArrayData, numberArrayData);

            this.OnActionTooltip?.Invoke(this.ActionTooltip, this.GameGui.HoveredAction);

            return this.ActionGenerateTooltipHook!.Original(addon, numberArrayData, stringArrayData);
        }
    }
}
