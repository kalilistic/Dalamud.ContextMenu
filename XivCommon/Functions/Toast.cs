using System;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Game;
using Dalamud.Game.Text.SeStringHandling;

namespace XivCommon.Functions {
    public class Toast {
        private GameFunctions Functions { get; }

        private delegate IntPtr ShowToastDelegate(IntPtr manager, IntPtr text, int layer, byte bool1, byte bool2, int logMessageId);

        private ShowToastDelegate ShowToast { get; }

        internal Toast(GameFunctions functions, SigScanner scanner) {
            this.Functions = functions;

            var showToast = scanner.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 83 3D ?? ?? ?? ?? ??");
            this.ShowToast = Marshal.GetDelegateForFunctionPointer<ShowToastDelegate>(showToast);
        }

        public void Show(string message) {
            this.Show(Encoding.UTF8.GetBytes(message));
        }

        public void Show(SeString message) {
            this.Show(message.Encode());
        }

        private void Show(byte[] bytes) {
            var manager = this.Functions.GetUiModule();

            unsafe {
                fixed (byte* ptr = bytes) {
                    this.ShowToast(manager, (IntPtr) ptr, 5, 0, 1, 0);
                }
            }
        }
    }
}
