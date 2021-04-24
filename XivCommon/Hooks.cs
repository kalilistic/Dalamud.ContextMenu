using System;

namespace XivCommon {
    /// <summary>
    /// Flags for which hooks to use
    /// </summary>
    [Flags]
    public enum Hooks {
        /// <summary>
        /// No hook.
        ///
        /// This flag is used to disable all hooking.
        /// </summary>
        None,

        /// <summary>
        /// The BattleTalk hook.
        ///
        /// This hook is used in order to enable the BattleTalk events.
        /// </summary>
        BattleTalk,

        /// <summary>
        /// Hooks used for refreshing Party Finder listings.
        /// </summary>
        PartyFinderListings,

        /// <summary>
        /// Hooks used for Party Finder join events.
        /// </summary>
        PartyFinderJoins,

        /// <summary>
        /// All Party Finder hooks.
        ///
        /// This hook is used in order to enable all Party Finder functions.
        /// </summary>
        PartyFinder = PartyFinderListings | PartyFinderJoins,

        /// <summary>
        /// The Talk hooks.
        ///
        /// This hook is used in order to enable the Talk events.
        /// </summary>
        Talk,

        /// <summary>
        /// The chat bubbles hooks.
        ///
        /// This hook is used in order to enable the chat bubbles events.
        /// </summary>
        ChatBubbles,
    }

    internal static class HooksExt {
        internal const Hooks DefaultHooks = Hooks.None;
    }
}
