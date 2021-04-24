using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Game.Internal.Gui;
using Dalamud.Game.Internal.Gui.Structs;
using Dalamud.Hooking;
using Dalamud.Plugin;

namespace XivCommon.Functions {
    /// <summary>
    /// A class containing Party Finder functionality
    /// </summary>
    public class PartyFinder : IDisposable {
        private delegate byte RequestPartyFinderListingsDelegate(IntPtr agent, byte categoryIdx);

        private delegate IntPtr JoinPfDelegate(IntPtr manager, IntPtr a2, int type, IntPtr packetData, uint a5);

        private RequestPartyFinderListingsDelegate RequestPartyFinderListings { get; }
        private Hook<RequestPartyFinderListingsDelegate>? RequestPfListingsHook { get; }
        private Hook<JoinPfDelegate>? JoinPfHook { get; }

        /// <summary>
        /// The delegate for party join events.
        /// </summary>
        public delegate void JoinPfEventDelegate(PartyFinderListing listing);

        /// <summary>
        /// <para>
        /// The event that is fired when the player joins a <b>cross-world</b> party via Party Finder.
        /// </para>
        /// <para>
        /// Requires the <see cref="Hooks.PartyFinder"/> hook to be enabled.
        /// </para>
        /// </summary>
        public event JoinPfEventDelegate? JoinParty;

        private PartyFinderGui PartyFinderGui { get; }
        private bool Enabled { get; }
        private IntPtr PartyFinderAgent { get; set; } = IntPtr.Zero;
        private Dictionary<uint, PartyFinderListing> Listings { get; } = new();
        private int LastBatch { get; set; } = -1;

        internal PartyFinder(SigScanner scanner, PartyFinderGui partyFinderGui, bool hook) {
            this.PartyFinderGui = partyFinderGui;

            var requestPfPtr = scanner.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 40 0F 10 81 ?? ?? ?? ??");

            this.RequestPartyFinderListings = Marshal.GetDelegateForFunctionPointer<RequestPartyFinderListingsDelegate>(requestPfPtr);

            this.Enabled = hook;

            if (!hook) {
                return;
            }

            this.RequestPfListingsHook = new Hook<RequestPartyFinderListingsDelegate>(requestPfPtr, new RequestPartyFinderListingsDelegate(this.OnRequestPartyFinderListings));
            this.RequestPfListingsHook.Enable();

            var joinPtr = scanner.ScanText("E8 ?? ?? ?? ?? 0F B7 47 28");
            this.JoinPfHook = new Hook<JoinPfDelegate>(joinPtr, new JoinPfDelegate(this.JoinPfDetour));
            this.JoinPfHook.Enable();

            this.PartyFinderGui.ReceiveListing += this.ReceiveListing;
        }

        /// <inheritdoc />
        public void Dispose() {
            this.PartyFinderGui.ReceiveListing -= this.ReceiveListing;
            this.JoinPfHook?.Dispose();
            this.RequestPfListingsHook?.Dispose();
        }

        private void ReceiveListing(PartyFinderListing listing, PartyFinderListingEventArgs args) {
            if (args.BatchNumber != this.LastBatch) {
                this.Listings.Clear();
            }

            this.LastBatch = args.BatchNumber;

            this.Listings[listing.Id] = listing;
        }

        private byte OnRequestPartyFinderListings(IntPtr agent, byte categoryIdx) {
            this.PartyFinderAgent = agent;
            return this.RequestPfListingsHook!.Original(agent, categoryIdx);
        }

        private IntPtr JoinPfDetour(IntPtr manager, IntPtr a2, int type, IntPtr packetData, uint a5) {
            // Updated: 5.5
            const int idOffset = -0x20;

            var ret = this.JoinPfHook!.Original(manager, a2, type, packetData, a5);

            if (this.JoinParty == null || (JoinType) type != JoinType.PartyFinder || packetData == IntPtr.Zero) {
                return ret;
            }

            try {
                var id = (uint) Marshal.ReadInt32(packetData + idOffset);
                if (this.Listings.TryGetValue(id, out var listing)) {
                    this.JoinParty?.Invoke(listing);
                }
            } catch (Exception ex) {
                PluginLog.LogError(ex, "Exception in PF join detour");
            }

            return ret;
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

            // Updated 5.5
            const int categoryOffset = 10_655;

            if (this.PartyFinderAgent == IntPtr.Zero) {
                return;
            }

            var categoryIdx = Marshal.ReadByte(this.PartyFinderAgent + categoryOffset);
            this.RequestPartyFinderListings(this.PartyFinderAgent, categoryIdx);
        }
    }

    internal enum JoinType : byte {
        /// <summary>
        /// Join via invite or party conversion.
        /// </summary>
        Normal = 0,

        /// <summary>
        /// Join via Party Finder.
        /// </summary>
        PartyFinder = 1,

        Unknown2 = 2,

        /// <summary>
        /// Remain in cross-world party after leaving a duty.
        /// </summary>
        LeaveDuty = 3,

        Unknown4 = 4,
    }
}
