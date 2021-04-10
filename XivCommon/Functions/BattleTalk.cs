using System;
using System.Text;
using Dalamud.Game;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Plugin;

namespace XivCommon.Functions {
    public class BattleTalk : IDisposable {
        private GameFunctions Functions { get; }
        private SeStringManager SeStringManager { get; }

        public delegate void BattleTalkEventDelegate(ref SeString sender, ref SeString message, ref BattleTalkOptions options, ref bool isHandled);

        public event BattleTalkEventDelegate? OnBattleTalk;

        private delegate byte AddBattleTalkDelegate(IntPtr uiModule, IntPtr sender, IntPtr message, float duration, byte style);

        private Hook<AddBattleTalkDelegate> AddBattleTextHook { get; }

        internal BattleTalk(GameFunctions functions, SigScanner scanner, SeStringManager seStringManager) {
            this.Functions = functions;
            this.SeStringManager = seStringManager;

            var addBattleTextPtr = scanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 50 48 8B 01 49 8B D8 0F 29 74 24 ?? 48 8B FA 0F 28 F3 FF 50 40 C7 44 24 ?? ?? ?? ?? ??");
            this.AddBattleTextHook = new Hook<AddBattleTalkDelegate>(addBattleTextPtr, new AddBattleTalkDelegate(this.AddBattleTalkDetour));
            this.AddBattleTextHook.Enable();
        }

        public void Dispose() {
            this.AddBattleTextHook.Dispose();
        }

        private unsafe byte AddBattleTalkDetour(IntPtr uiModule, IntPtr senderPtr, IntPtr messagePtr, float duration, byte style) {
            var rawSender = Util.ReadTerminated(senderPtr);
            var rawMessage = Util.ReadTerminated(messagePtr);

            var sender = this.SeStringManager.Parse(rawSender);
            var message = this.SeStringManager.Parse(rawMessage);

            var options = new BattleTalkOptions {
                Duration = duration,
                Style = (BattleTalkStyle) style,
            };

            var handled = false;
            try {
                this.OnBattleTalk?.Invoke(ref sender, ref message, ref options, ref handled);
            } catch (Exception ex) {
                PluginLog.Log(ex, "Exception in BattleTalk detour");
            }

            if (handled) {
                return 0;
            }

            var finalSender = sender.Encode().Terminate();
            var finalMessage = message.Encode().Terminate();

            fixed (byte* fSenderPtr = finalSender, fMessagePtr = finalMessage) {
                return this.AddBattleTextHook.Original(uiModule, (IntPtr) fSenderPtr, (IntPtr) fMessagePtr, options.Duration, (byte) options.Style);
            }
        }

        public void Show(SeString sender, SeString message, BattleTalkOptions? options = null) {
            this.Show(sender.Encode(), message.Encode(), options);
        }

        public void Show(string sender, string message, BattleTalkOptions? options = null) {
            this.Show(Encoding.UTF8.GetBytes(sender), Encoding.UTF8.GetBytes(message), options);
        }

        public void Show(SeString sender, string message, BattleTalkOptions? options = null) {
            this.Show(sender.Encode(), Encoding.UTF8.GetBytes(message), options);
        }

        public void Show(string sender, SeString message, BattleTalkOptions? options = null) {
            this.Show(Encoding.UTF8.GetBytes(sender), message.Encode(), options);
        }

        private void Show(byte[] sender, byte[] message, BattleTalkOptions? options) {
            if (sender.Length == 0) {
                throw new ArgumentException("sender cannot be empty", nameof(sender));
            }

            if (message.Length == 0) {
                throw new ArgumentException("message cannot be empty", nameof(message));
            }

            options ??= new BattleTalkOptions();

            var uiModule = this.Functions.GetUiModule();

            unsafe {
                fixed (byte* senderPtr = sender.Terminate(), messagePtr = message.Terminate()) {
                    this.AddBattleTalkDetour(uiModule, (IntPtr) senderPtr, (IntPtr) messagePtr, options.Duration, (byte) options.Style);
                }
            }
        }
    }

    public class BattleTalkOptions {
        /// <summary>
        /// Duration of the window in seconds.
        /// </summary>
        public float Duration { get; set; } = 5f;

        public BattleTalkStyle Style { get; set; } = BattleTalkStyle.Normal;
    }

    public enum BattleTalkStyle : byte {
        Normal = 0,
        Aetherial = 6,
        System = 7,
        Blue = 9,
    }
}
