using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Game.Internal.Gui;
using Dalamud.Hooking;

namespace XivCommon.Functions {
    public class PartyFinder : IDisposable {
        private delegate byte RequestPartyFinderListingsDelegate(IntPtr agent, byte categoryIdx);

        private RequestPartyFinderListingsDelegate RequestPartyFinderListings { get; }
        private Hook<RequestPartyFinderListingsDelegate> RequestPfListingsHook { get; }

        private IntPtr PartyFinderAgent { get; set; } = IntPtr.Zero;

        internal PartyFinder(SigScanner scanner) {
            var requestPfPtr = scanner.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 40 0F 10 81 ?? ?? ?? ??");

            this.RequestPartyFinderListings = Marshal.GetDelegateForFunctionPointer<RequestPartyFinderListingsDelegate>(requestPfPtr);
            this.RequestPfListingsHook = new Hook<RequestPartyFinderListingsDelegate>(requestPfPtr, new RequestPartyFinderListingsDelegate(this.OnRequestPartyFinderListings));
            this.RequestPfListingsHook.Enable();
        }

        public void Dispose() {
            this.RequestPfListingsHook.Dispose();
        }

        private byte OnRequestPartyFinderListings(IntPtr agent, byte categoryIdx) {
            this.PartyFinderAgent = agent;
            return this.RequestPfListingsHook.Original(agent, categoryIdx);
        }

        public void RefreshListings() {
            // Updated 5.41
            const int categoryOffset = 10_655;

            if (this.PartyFinderAgent == IntPtr.Zero) {
                return;
            }

            var categoryIdx = Marshal.ReadByte(this.PartyFinderAgent + categoryOffset);
            this.RequestPartyFinderListings(this.PartyFinderAgent, categoryIdx);
        }
    }
}
