﻿using System;

namespace XivCommon {
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
        /// This hook is used in order to enable all BattleTalk functions.
        /// </summary>
        BattleTalk,

        /// <summary>
        /// The Party Finder hooks.
        ///
        /// This hook is used in order to enable all Party Finder functions.
        /// </summary>
        PartyFinder,
    }

    internal static class HooksExt {
        internal const Hooks DefaultHooks = Hooks.None;
    }
}