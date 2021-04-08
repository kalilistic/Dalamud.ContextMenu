using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using XivCommon.Functions;

namespace XivCommon {
    public class GameFunctions : IDisposable {
        private IntPtr UiModulePtr { get; }

        private delegate IntPtr GetUiModuleDelegate(IntPtr basePtr);

        private GetUiModuleDelegate InternalGetUiModule { get; }

        public Chat Chat { get; }
        public PartyFinder PartyFinder { get; }

        internal GameFunctions(SigScanner scanner) {
            this.UiModulePtr = scanner.GetStaticAddressFromSig("48 8B 0D ?? ?? ?? ?? 48 8D 54 24 ?? 48 83 C1 10 E8 ?? ?? ?? ??");

            var getUiModulePtr = scanner.ScanText("E8 ?? ?? ?? ?? 83 3B 01");
            this.InternalGetUiModule = Marshal.GetDelegateForFunctionPointer<GetUiModuleDelegate>(getUiModulePtr);

            this.Chat = new Chat(this, scanner);
            this.PartyFinder = new PartyFinder(scanner);
        }

        public void Dispose() {
            this.PartyFinder.Dispose();
        }

        public IntPtr GetUiModule() {
            return this.InternalGetUiModule(Marshal.ReadIntPtr(this.UiModulePtr));
        }
    }
}
