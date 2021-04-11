using System;
using Dalamud.Plugin;

namespace XivCommon {
    /// <summary>
    /// A base class for accessing XivCommon functionality.
    /// </summary>
    public class XivCommonBase : IDisposable {
        /// <summary>
        /// Game functions and events
        /// </summary>
        public GameFunctions Functions { get; }

        /// <summary>
        /// <para>
        /// Construct a new XivCommon base.
        /// </para>
        /// <para>
        /// This will automatically enable hooks based on the hooks parameter.
        /// </para>
        /// </summary>
        /// <param name="interface">The <see cref="DalamudPluginInterface"/> of the plugin constructing this base</param>
        /// <param name="hooks">Flags indicating which hooks to enable</param>
        public XivCommonBase(DalamudPluginInterface @interface, Hooks hooks = HooksExt.DefaultHooks) {
            this.Functions = new GameFunctions(hooks, @interface.TargetModuleScanner, @interface.SeStringManager);
        }

        /// <inheritdoc />
        public void Dispose() {
            this.Functions.Dispose();
        }
    }
}
