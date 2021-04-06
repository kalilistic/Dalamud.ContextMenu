using System;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Game;
using Dalamud.Game.Chat.SeStringHandling;

namespace XivCommon.Functions {
    public class Toast {
        private GameFunctions Functions { get; }

        private delegate IntPtr ShowToastDelegate(IntPtr manager, IntPtr text, int layer, byte isTop, byte isFast, int logMessageId);

        private delegate byte ShowQuestToastDelegate(IntPtr manager, int position, IntPtr text, uint iconOrCheck1, byte playSound, uint iconOrCheck2, byte alsoPlaySound);

        private delegate byte ShowErrorToastDelegate(IntPtr manager, IntPtr text, int layer, byte respectsHidingMaybe);


        private ShowToastDelegate ShowToast { get; }
        private ShowQuestToastDelegate ShowQuestToast { get; }
        private ShowErrorToastDelegate ShowErrorToast { get; }

        internal Toast(GameFunctions functions, SigScanner scanner) {
            this.Functions = functions;

            var showToast = scanner.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 83 3D ?? ?? ?? ?? ??");
            this.ShowToast = Marshal.GetDelegateForFunctionPointer<ShowToastDelegate>(showToast);

            var showQuest = scanner.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC 40 83 3D ?? ?? ?? ?? ??");
            this.ShowQuestToast = Marshal.GetDelegateForFunctionPointer<ShowQuestToastDelegate>(showQuest);

            var showError = scanner.ScanText("40 56 57 41 56 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 41 8B F0");
            this.ShowErrorToast = Marshal.GetDelegateForFunctionPointer<ShowErrorToastDelegate>(showError);
        }

        [Obsolete]
        public void ShowNormal(string message, ToastOptions? options = null) {
            this.ShowNormal(Encoding.UTF8.GetBytes(message), options);
        }

        [Obsolete]
        public void ShowNormal(SeString message, ToastOptions? options = null) {
            this.ShowNormal(message.Encode(), options);
        }

        [Obsolete]
        private void ShowNormal(byte[] bytes, ToastOptions? options = null) {
            options ??= new ToastOptions();

            var manager = this.Functions.GetUiModule();

            unsafe {
                fixed (byte* ptr = bytes) {
                    this.ShowToast(manager, (IntPtr) ptr, 5, (byte) options.Position, (byte) options.Speed, 0);
                }
            }
        }

        public void ShowQuest(string message, QuestToastOptions? options = null) {
            this.ShowQuest(Encoding.UTF8.GetBytes(message), options);
        }

        public void ShowQuest(SeString message, QuestToastOptions? options = null) {
            this.ShowQuest(message.Encode(), options);
        }

        private void ShowQuest(byte[] bytes, QuestToastOptions? options = null) {
            const int checkmarkMagic = 60081;

            options ??= new QuestToastOptions();

            var manager = this.Functions.GetUiModule();

            uint ioc1, ioc2;
            if (options.DisplayCheckmark) {
                ioc1 = checkmarkMagic;
                ioc2 = options.IconId;
            } else {
                ioc1 = options.IconId;
                ioc2 = 0;
            }

            unsafe {
                fixed (byte* ptr = bytes) {
                    this.ShowQuestToast(manager, (int) options.Position, (IntPtr) ptr, ioc1, options.PlaySound ? (byte) 1 : (byte) 0, ioc2, 0);
                }
            }
        }

        private delegate IntPtr GetAtkModuleDelegate(IntPtr uiModule);

        private IntPtr GetAtkModule() {
            var uiModule = this.Functions.GetUiModule();
            var vtbl = Marshal.ReadIntPtr(uiModule);
            var getAtkPtr = Marshal.ReadIntPtr(vtbl + 7 * 8);
            var getAtkModule = Marshal.GetDelegateForFunctionPointer<GetAtkModuleDelegate>(getAtkPtr);
            return getAtkModule(uiModule);
        }

        public void ShowError(string message) {
            this.ShowError(Encoding.UTF8.GetBytes(message));
        }

        public void ShowError(SeString message) {
            this.ShowError(message.Encode());
        }

        private void ShowError(byte[] bytes) {
            var manager = this.GetAtkModule();

            unsafe {
                fixed (byte* ptr = bytes) {
                    this.ShowErrorToast(manager, (IntPtr) ptr, 10, 0);
                }
            }
        }
    }

    public sealed class ToastOptions {
        public ToastPosition Position { get; set; } = ToastPosition.Bottom;

        public ToastSpeed Speed { get; set; } = ToastSpeed.Slow;
    }

    public enum ToastPosition : byte {
        Bottom = 0,
        Top = 1,
    }

    public enum ToastSpeed : byte {
        Slow = 0,
        Fast = 1,
    }

    public sealed class QuestToastOptions {
        public QuestToastPosition Position { get; set; } = QuestToastPosition.Centre;
        public uint IconId { get; set; } = 0;
        public bool DisplayCheckmark { get; set; } = false;
        public bool PlaySound { get; set; } = false;
    }

    public enum QuestToastPosition {
        Centre = 0,
        Right = 1,
        Left = 2,
    }
}
