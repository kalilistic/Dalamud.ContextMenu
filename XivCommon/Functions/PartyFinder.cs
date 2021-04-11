using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Hooking;

namespace XivCommon.Functions {
    /// <summary>
    /// A class containing Party Finder functionality
    /// </summary>
    public class PartyFinder : IDisposable {
        private delegate byte RequestPartyFinderListingsDelegate(IntPtr agent, byte categoryIdx);

        private RequestPartyFinderListingsDelegate RequestPartyFinderListings { get; }
        private Hook<RequestPartyFinderListingsDelegate>? RequestPfListingsHook { get; }

        private bool Enabled { get; }
        private IntPtr PartyFinderAgent { get; set; } = IntPtr.Zero;

        internal PartyFinder(SigScanner scanner, bool hook) {
            var requestPfPtr = scanner.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 40 0F 10 81 ?? ?? ?? ??");

            this.RequestPartyFinderListings = Marshal.GetDelegateForFunctionPointer<RequestPartyFinderListingsDelegate>(requestPfPtr);

            this.Enabled = hook;

            if (!hook) {
                return;
            }

            this.RequestPfListingsHook = new Hook<RequestPartyFinderListingsDelegate>(requestPfPtr, new RequestPartyFinderListingsDelegate(this.OnRequestPartyFinderListings));
            this.RequestPfListingsHook.Enable();
        }

        /// <inheritdoc />
        public void Dispose() {
            this.RequestPfListingsHook?.Dispose();
        }

        private byte OnRequestPartyFinderListings(IntPtr agent, byte categoryIdx) {
            this.PartyFinderAgent = agent;
            return this.RequestPfListingsHook!.Original(agent, categoryIdx);
        }

        /// <summary>
        /// <para>
        /// Refresh the Party Finder listings. This does not open the Party Finder.
        /// </para>
        /// <para>
        /// This maintains the currently selected category.
        /// </para>
        /// </summary>
        /// <exception cref="InvalidOperationException">If the <see cref="Hooks.PartyFinder"/> hook is not enabled</exception>
        public void RefreshListings() {
            if (!this.Enabled) {
                throw new InvalidOperationException("PartyFinder hooks are not enabled");
            }

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
