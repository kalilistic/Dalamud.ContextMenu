using System;
using Dalamud.Game;

namespace XivCommon {
    public class XivCommonBase : IDisposable {
        public GameFunctions Functions { get; }

        public XivCommonBase(SigScanner scanner) {
            this.Functions = new GameFunctions(scanner);
        }

        public void Dispose() {
            this.Functions.Dispose();
        }
    }
}
