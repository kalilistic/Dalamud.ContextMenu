using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Dalamud.Plugin;
using XivCommon.Functions;

namespace XivCommon {
    /// <summary>
    /// A class containing game functions
    /// </summary>
    public class GameFunctions : IDisposable {
        private static class Signatures {
            internal const string GetAgentByInternalId = "E8 ?? ?? ?? ?? 83 FF 0D";
            internal const string GetAtkStageSingleton = "E8 ?? ?? ?? ?? 41 B8 01 00 00 00 48 8D 15 ?? ?? ?? ?? 48 8B 48 20 E8 ?? ?? ?? ?? 48 8B CF";
        }

        private delegate IntPtr GetAtkStageSingletonDelegate();

        private delegate IntPtr GetAgentModuleDelegate(IntPtr basePtr);

        private delegate IntPtr GetAgentByInternalIdDelegate(IntPtr agentModule, uint id);

        private DalamudPluginInterface Interface { get; }

        private GetAgentByInternalIdDelegate? GetAgentByInternalIdInternal { get; }

        private GetAtkStageSingletonDelegate? GetAtkStageSingletonInternal { get; }

        /// <summary>
        /// Chat functions
        /// </summary>
        public Chat Chat { get; }

        /// <summary>
        /// Party Finder functions and events
        /// </summary>
        public PartyFinder PartyFinder { get; }

        /// <summary>
        /// BattleTalk functions and events
        /// </summary>
        public BattleTalk BattleTalk { get; }

        /// <summary>
        /// Examine functions
        /// </summary>
        public Examine Examine { get; }

        /// <summary>
        /// Talk events
        /// </summary>
        public Talk Talk { get; }

        /// <summary>
        ///  Chat bubble functions and events
        /// </summary>
        public ChatBubbles ChatBubbles { get; }

        /// <summary>
        /// Context menu functions
        /// </summary>
        public ContextMenu ContextMenu { get; }

        internal GameFunctions(Hooks hooks, DalamudPluginInterface @interface) {
            this.Interface = @interface;

            var scanner = @interface.TargetModuleScanner;
            var seStringManager = @interface.SeStringManager;

            var dalamudField = @interface.GetType().GetField("dalamud", BindingFlags.Instance | BindingFlags.NonPublic);
            var dalamud = (Dalamud.Dalamud) dalamudField!.GetValue(@interface);

            this.Chat = new Chat(this, scanner);
            this.PartyFinder = new PartyFinder(scanner, @interface.Framework.Gui.PartyFinder, hooks);
            this.BattleTalk = new BattleTalk(this, scanner, seStringManager, hooks.HasFlag(Hooks.BattleTalk));
            this.Examine = new Examine(this, scanner);
            this.Talk = new Talk(scanner, seStringManager, hooks.HasFlag(Hooks.Talk));
            this.ChatBubbles = new ChatBubbles(dalamud, scanner, seStringManager, hooks.HasFlag(Hooks.ChatBubbles));
            this.ContextMenu = new ContextMenu(this, scanner, @interface.ClientState.ClientLanguage, hooks);

            if (scanner.TryScanText(Signatures.GetAgentByInternalId, out var byInternalIdPtr, "GetAgentByInternalId")) {
                this.GetAgentByInternalIdInternal = Marshal.GetDelegateForFunctionPointer<GetAgentByInternalIdDelegate>(byInternalIdPtr);
            }

            if (scanner.TryScanText(Signatures.GetAtkStageSingleton, out var getSingletonPtr, "GetAtkStageSingleton")) {
                this.GetAtkStageSingletonInternal = Marshal.GetDelegateForFunctionPointer<GetAtkStageSingletonDelegate>(getSingletonPtr);
            }
        }

        /// <inheritdoc />
        public void Dispose() {
            this.ContextMenu.Dispose();
            this.ChatBubbles.Dispose();
            this.Talk.Dispose();
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

        /// <summary>
        /// Gets the pointer to the agent module
        /// </summary>
        /// <returns>Pointer</returns>
        public IntPtr GetAgentModule() {
            var uiModule = this.GetUiModule();
            var getAgentModulePtr = FollowPtrChain(uiModule, new[] {0, 0x110});
            var getAgentModule = Marshal.GetDelegateForFunctionPointer<GetAgentModuleDelegate>(getAgentModulePtr);
            return getAgentModule(uiModule);
        }

        private static IntPtr FollowPtrChain(IntPtr start, IEnumerable<int> offsets) {
            foreach (var offset in offsets) {
                start = Marshal.ReadIntPtr(start, offset);
                if (start == IntPtr.Zero) {
                    break;
                }
            }

            return start;
        }

        /// <summary>
        /// Gets the pointer to an agent from its internal ID.
        /// </summary>
        /// <param name="id">internal id of agent</param>
        /// <returns>Pointer</returns>
        /// <exception cref="InvalidOperationException">if the signature for the function could not be found</exception>
        public IntPtr GetAgentByInternalId(uint id) {
            if (this.GetAgentByInternalIdInternal == null) {
                throw new InvalidOperationException("Could not find signature for GetAgentByInternalId");
            }

            var agent = this.GetAgentModule();
            return this.GetAgentByInternalIdInternal(agent, id);
        }

        /// <summary>
        /// Gets the pointer to the AtkStage singleton
        /// </summary>
        /// <returns>Pointer</returns>
        /// <exception cref="InvalidOperationException">if the signature for the function could not be found</exception>
        public IntPtr GetAtkStageSingleton() {
            if (this.GetAtkStageSingletonInternal == null) {
                throw new InvalidOperationException("Could not find signature for GetAtkStageSingleton");
            }

            return this.GetAtkStageSingletonInternal();
        }
    }
}
