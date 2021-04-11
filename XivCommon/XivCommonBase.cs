using System;
using Dalamud.Plugin;

namespace XivCommon {
    public class XivCommonBase : IDisposable {
        public GameFunctions Functions { get; }

        public XivCommonBase(DalamudPluginInterface @interface, Hooks hooks = HooksExt.DefaultHooks) {
            this.Functions = new GameFunctions(hooks, @interface.TargetModuleScanner, @interface.SeStringManager);
        }

        public void Dispose() {
            this.Functions.Dispose();
        }
    }
}
