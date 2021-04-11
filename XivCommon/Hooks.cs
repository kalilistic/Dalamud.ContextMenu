using System;

namespace XivCommon {
    [Flags]
    public enum Hooks {
        None,
        BattleTalk,
        PartyFinder,
    }

    internal static class HooksExt {
        internal const Hooks DefaultHooks = Hooks.None;
    }
}
