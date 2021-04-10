using System;
using Dalamud.Game;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin;

namespace XivCommon {
    public class XivCommonBase : IDisposable {
        public GameFunctions Functions { get; }

        public XivCommonBase(DalamudPluginInterface @interface) {
            this.Functions = new GameFunctions(@interface.TargetModuleScanner, @interface.SeStringManager);
        }

        public void Dispose() {
            this.Functions.Dispose();
        }
    }
}
