using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Plugin;

namespace XivCommon.Functions {
    /// <summary>
    /// Class containing Talk events
    /// </summary>
    public class Talk : IDisposable {
        // Updated: 5.5
        private const int TextOffset = 0;
        private const int NameOffset = 0x10;
        private const int StyleOffset = 0x38;

        private SeStringManager SeStringManager { get; }

        private delegate void AddonTalkV45Delegate(IntPtr addon, IntPtr a2, IntPtr data);

        private Hook<AddonTalkV45Delegate>? AddonTalkV45Hook { get; }

        private delegate IntPtr SetAtkValueStringDelegate(IntPtr atkValue, IntPtr text);

        private SetAtkValueStringDelegate SetAtkValueString { get; }

        /// <summary>
        /// The delegate for Talk events.
        /// </summary>
        public delegate void TalkEventDelegate(ref SeString name, ref SeString text, ref TalkStyle style);

        /// <summary>
        /// <para>
        /// The event that is fired when NPCs talk.
        /// </para>
        /// <para>
        /// Requires the <see cref="Hooks.Talk"/> hook to be enabled.
        /// </para>
        /// </summary>
        public event TalkEventDelegate? OnTalk;

        internal Talk(SigScanner scanner, SeStringManager manager, bool hooksEnabled) {
            this.SeStringManager = manager;

            var setAtkPtr = scanner.ScanText("E8 ?? ?? ?? ?? 41 03 ED");
            this.SetAtkValueString = Marshal.GetDelegateForFunctionPointer<SetAtkValueStringDelegate>(setAtkPtr);

            if (!hooksEnabled) {
                return;
            }

            var showMessageBoxPtr = scanner.ScanText("4C 8B DC 55 57 41 55 49 8D 6B 98");
            this.AddonTalkV45Hook = new Hook<AddonTalkV45Delegate>(showMessageBoxPtr, new AddonTalkV45Delegate(this.AddonTalkV45Detour));
            this.AddonTalkV45Hook.Enable();
        }

        /// <inheritdoc />
        public void Dispose() {
            this.AddonTalkV45Hook?.Dispose();
        }

        private void AddonTalkV45Detour(IntPtr addon, IntPtr a2, IntPtr data) {
            if (this.OnTalk == null) {
                this.AddonTalkV45Hook!.Original(addon, a2, data);
                return;
            }

            var rawName = Util.ReadTerminated(Marshal.ReadIntPtr(data + NameOffset + 8));
            var rawText = Util.ReadTerminated(Marshal.ReadIntPtr(data + TextOffset + 8));
            var style = (TalkStyle) Marshal.ReadByte(data + StyleOffset);

            var name = this.SeStringManager.Parse(rawName);
            var text = this.SeStringManager.Parse(rawText);

            try {
                this.OnTalk?.Invoke(ref name, ref text, ref style);
            } catch (Exception ex) {
                PluginLog.LogError(ex, "Exception in Talk detour");
            }

            var newName = name.Encode().Terminate();
            var newText = text.Encode().Terminate();

            Marshal.WriteByte(data + StyleOffset, (byte) style);

            unsafe {
                fixed (byte* namePtr = newName, textPtr = newText) {
                    this.SetAtkValueString(data + NameOffset, (IntPtr) namePtr);
                    this.SetAtkValueString(data + TextOffset, (IntPtr) textPtr);
                }
            }

            this.AddonTalkV45Hook!.Original(addon, a2, data);
        }
    }

    /// <summary>
    /// Talk window styles.
    /// </summary>
    public enum TalkStyle : byte {
        /// <summary>
        /// The normal style with a white background.
        /// </summary>
        Normal = 0,

        /// <summary>
        /// A style with lights on the top and bottom border.
        /// </summary>
        Lights = 2,

        /// <summary>
        /// A style used for when characters are shouting.
        /// </summary>
        Shout = 3,

        /// <summary>
        /// Like <see cref="Shout"/> but with flatter edges.
        /// </summary>
        FlatShout = 4,

        /// <summary>
        /// The style used when dragons (and some other NPCs) talk.
        /// </summary>
        Dragon = 5,

        /// <summary>
        /// The style used for Allagan machinery.
        /// </summary>
        Allagan = 6,

        /// <summary>
        /// The style used for system messages.
        /// </summary>
        System = 7,

        /// <summary>
        /// A mixture of the system message style and the dragon style.
        /// </summary>
        DragonSystem = 8,

        /// <summary>
        /// The system message style with a purple background.
        /// </summary>
        PurpleSystem = 9,
    }
}
