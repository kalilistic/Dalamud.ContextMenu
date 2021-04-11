using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Game.Text.SeStringHandling;
using XivCommon.Functions;

namespace XivCommon {
    /// <summary>
    /// A class containing game functions
    /// </summary>
    public class GameFunctions : IDisposable {
        private IntPtr UiModulePtr { get; }

        private delegate IntPtr GetUiModuleDelegate(IntPtr basePtr);

        private GetUiModuleDelegate InternalGetUiModule { get; }

        /// <summary>
        /// Chat functions
        /// </summary>
        public Chat Chat { get; }

        /// <summary>
        /// Party Finder functions
        /// </summary>
        public PartyFinder PartyFinder { get; }

        /// <summary>
        /// BattleTalk functions and events
        /// </summary>
        public BattleTalk BattleTalk { get; }

        internal GameFunctions(Hooks hooks, SigScanner scanner, SeStringManager seStringManager) {
            this.UiModulePtr = scanner.GetStaticAddressFromSig("48 8B 0D ?? ?? ?? ?? 48 8D 54 24 ?? 48 83 C1 10 E8 ?? ?? ?? ??");

            var getUiModulePtr = scanner.ScanText("E8 ?? ?? ?? ?? 83 3B 01");
            this.InternalGetUiModule = Marshal.GetDelegateForFunctionPointer<GetUiModuleDelegate>(getUiModulePtr);

            this.Chat = new Chat(this, scanner);
            this.PartyFinder = new PartyFinder(scanner, hooks.HasFlag(Hooks.PartyFinder));
            this.BattleTalk = new BattleTalk(this, scanner, seStringManager, hooks.HasFlag(Hooks.BattleTalk));
        }

        /// <inheritdoc />
        public void Dispose() {
            this.BattleTalk.Dispose();
            this.PartyFinder.Dispose();
        }

        /// <summary>
        /// Gets the pointer to the UI module
        /// </summary>
        /// <returns>Pointer</returns>
        public IntPtr GetUiModule() {
            return this.InternalGetUiModule(Marshal.ReadIntPtr(this.UiModulePtr));
        }
    }
}
