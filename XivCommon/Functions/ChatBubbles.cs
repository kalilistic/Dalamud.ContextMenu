using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Plugin;

namespace XivCommon.Functions {
    /// <summary>
    /// Class containing chat bubble events and functions
    /// </summary>
    public class ChatBubbles : IDisposable {
        private static class Signatures {
            internal const string ChatBubbleOpen = "E8 ?? ?? ?? ?? 80 BF ?? ?? ?? ?? ?? C7 07 ?? ?? ?? ??";
            internal const string ChatBubbleUpdate = "48 85 D2 0F 84 ?? ?? ?? ?? 48 89 5C 24 ?? 57 48 83 EC 20 8B 41 0C";
        }

        private Dalamud.Dalamud Dalamud { get; }
        private SeStringManager SeStringManager { get; }

        private delegate void OpenChatBubbleDelegate(IntPtr manager, IntPtr actor, IntPtr text, byte a4);

        private delegate void UpdateChatBubbleDelegate(IntPtr bubblePtr, IntPtr actor);

        private Hook<OpenChatBubbleDelegate>? OpenChatBubbleHook { get; }

        private Hook<UpdateChatBubbleDelegate>? UpdateChatBubbleHook { get; }

        /// <summary>
        /// The delegate for chat bubble events.
        /// </summary>
        public delegate void OnChatBubbleDelegate(ref Actor actor, ref SeString text);

        /// <summary>
        /// The delegate for chat bubble update events.
        /// </summary>
        public delegate void OnUpdateChatBubbleDelegate(ref Actor actor);

        /// <summary>
        /// <para>
        /// The event that is fired when a chat bubble is shown.
        /// </para>
        /// <para>
        /// Requires the <see cref="Hooks.ChatBubbles"/> hook to be enabled.
        /// </para>
        /// </summary>
        public event OnChatBubbleDelegate? OnChatBubble;

        /// <summary>
        /// <para>
        /// The event that is fired when a chat bubble is updated.
        /// </para>
        /// <para>
        /// Requires the <see cref="Hooks.ChatBubbles"/> hook to be enabled.
        /// </para>
        /// </summary>
        public event OnUpdateChatBubbleDelegate? OnUpdateBubble;

        internal ChatBubbles(Dalamud.Dalamud dalamud, SigScanner scanner, SeStringManager manager, bool hookEnabled) {
            this.Dalamud = dalamud;
            this.SeStringManager = manager;

            if (!hookEnabled) {
                return;
            }

            if (scanner.TryScanText(Signatures.ChatBubbleOpen, out var openPtr, "chat bubbles open")) {
                this.OpenChatBubbleHook = new Hook<OpenChatBubbleDelegate>(openPtr, new OpenChatBubbleDelegate(this.OpenChatBubbleDetour));
                this.OpenChatBubbleHook.Enable();
            }

            if (scanner.TryScanText(Signatures.ChatBubbleUpdate, out var updatePtr, "chat bubbles update")) {
                this.UpdateChatBubbleHook = new Hook<UpdateChatBubbleDelegate>(updatePtr + 9, new UpdateChatBubbleDelegate(this.UpdateChatBubbleDetour));
                this.UpdateChatBubbleHook.Enable();
            }
        }

        /// <inheritdoc />
        public void Dispose() {
            this.OpenChatBubbleHook?.Dispose();
            this.UpdateChatBubbleHook?.Dispose();
        }

        private void OpenChatBubbleDetour(IntPtr manager, IntPtr actor, IntPtr text, byte a4) {
            try {
                this.OpenChatBubbleDetourInner(manager, actor, text, a4);
            } catch (Exception ex) {
                Logger.LogError(ex, "Exception in chat bubble detour");
                this.OpenChatBubbleHook!.Original(manager, actor, text, a4);
            }
        }

        private void OpenChatBubbleDetourInner(IntPtr manager, IntPtr actorPtr, IntPtr textPtr, byte a4) {
            var actorStruct = Marshal.PtrToStructure<Dalamud.Game.ClientState.Structs.Actor>(actorPtr);
            var actor = new Actor(actorPtr, actorStruct, this.Dalamud);

            var rawText = Util.ReadTerminated(textPtr);
            var text = this.SeStringManager.Parse(rawText);

            try {
                this.OnChatBubble?.Invoke(ref actor, ref text);
            } catch (Exception ex) {
                Logger.LogError(ex, "Exception in chat bubble event");
            }

            var newText = text.Encode().Terminate();

            unsafe {
                fixed (byte* newTextPtr = newText) {
                    this.OpenChatBubbleHook!.Original(manager, actor.Address, (IntPtr) newTextPtr, a4);
                }
            }
        }

        private void UpdateChatBubbleDetour(IntPtr bubblePtr, IntPtr actor) {
            try {
                this.UpdateChatBubbleDetourInner(bubblePtr, actor);
            } catch (Exception ex) {
                Logger.LogError(ex, "Exception in update chat bubble detour");
                this.UpdateChatBubbleHook!.Original(bubblePtr, actor);
            }
        }

        private void UpdateChatBubbleDetourInner(IntPtr bubblePtr, IntPtr actorPtr) {
            // var bubble = (ChatBubble*) bubblePtr;
            var actorStruct = Marshal.PtrToStructure<Dalamud.Game.ClientState.Structs.Actor>(actorPtr);
            var actor = new Actor(actorPtr, actorStruct, this.Dalamud);

            try {
                this.OnUpdateBubble?.Invoke(ref actor);
            } catch (Exception ex) {
                Logger.LogError(ex, "Exception in chat bubble update event");
            }

            this.UpdateChatBubbleHook!.Original(bubblePtr, actor.Address);
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x80)]
    internal unsafe struct ChatBubble {
        [FieldOffset(0x0)]
        internal readonly uint Id;

        [FieldOffset(0x4)]
        internal float Timer;

        [FieldOffset(0x8)]
        internal readonly uint Unk_8; // enum probably

        [FieldOffset(0xC)]
        internal ChatBubbleStatus Status; // state of the bubble

        [FieldOffset(0x10)]
        internal readonly byte* Text;

        [FieldOffset(0x78)]
        internal readonly ulong Unk_78; // check whats in memory here
    }

    internal enum ChatBubbleStatus : uint {
        GetData = 0,
        On = 1,
        Init = 2,
        Off = 3,
    }
}
