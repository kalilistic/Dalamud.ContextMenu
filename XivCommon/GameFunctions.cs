using System;
using Dalamud.Plugin;
using XivCommon.Functions;

namespace XivCommon {
    /// <summary>
    /// A class containing game functions
    /// </summary>
    public class GameFunctions : IDisposable {
        private DalamudPluginInterface Interface { get; }

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

        internal GameFunctions(Hooks hooks, DalamudPluginInterface @interface) {
            this.Interface = @interface;
            this.Chat = new Chat(this, @interface.TargetModuleScanner);
            this.PartyFinder = new PartyFinder(@interface.TargetModuleScanner, hooks.HasFlag(Hooks.PartyFinder));
            this.BattleTalk = new BattleTalk(this, @interface.TargetModuleScanner, @interface.SeStringManager, hooks.HasFlag(Hooks.BattleTalk));
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
            return this.Interface.Framework.Gui.GetUIModule();
        }
    }
}
