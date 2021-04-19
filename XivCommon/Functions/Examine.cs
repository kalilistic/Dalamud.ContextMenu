using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Game.ClientState.Actors.Types;

namespace XivCommon.Functions {
    /// <summary>
    /// Class containing examine functions
    /// </summary>
    public class Examine {
        private GameFunctions Functions { get; }

        private delegate IntPtr GetListDelegate(IntPtr basePtr);

        private delegate long RequestCharInfoDelegate(IntPtr ptr);

        private RequestCharInfoDelegate RequestCharacterInfo { get; }

        internal Examine(GameFunctions functions, SigScanner scanner) {
            this.Functions = functions;

            // got this by checking what accesses rciData below
            var rciPtr = scanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 40 BA ?? ?? ?? ?? 48 8B D9 E8 ?? ?? ?? ?? 48 8B F8 48 85 C0 74 16");
            this.RequestCharacterInfo = Marshal.GetDelegateForFunctionPointer<RequestCharInfoDelegate>(rciPtr);
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
        /// Opens the Examine window for the specified actor.
        /// </summary>
        /// <param name="actor">Actor to open window for</param>
        public void OpenExamineWindow(Actor actor) {
            this.OpenExamineWindow(actor.ActorId);
        }

        /// <summary>
        /// Opens the Examine window for the actor with the specified ID.
        /// </summary>
        /// <param name="actorId">Actor ID to open window for</param>
        public void OpenExamineWindow(int actorId) {
            // NOTES LAST UPDATED: 5.45

            // offsets and stuff come from the beginning of case 0x2c (around line 621 in IDA)
            // if 29f8 ever changes, I'd just scan for it in old binary and find what it is in the new binary at the same spot
            // 40 55 53 57 41 54 41 55 41 56 48 8D 6C 24 ??
            var uiModule = this.Functions.GetUiModule();
            var getListPtr = FollowPtrChain(uiModule, new[] {0, 0x110});
            var getList = Marshal.GetDelegateForFunctionPointer<GetListDelegate>(getListPtr);
            var list = getList(uiModule);
            var rciData = Marshal.ReadIntPtr(list + 0x1A0);

            unsafe {
                // offsets at sig E8 ?? ?? ?? ?? 33 C0 EB 4C
                // this is called at the end of the 2c case
                var raw = (int*) rciData;
                *(raw + 10) = actorId;
                *(raw + 11) = actorId;
                *(raw + 12) = actorId;
                *(raw + 13) = -536870912;
                *(raw + 311) = 0;
            }

            this.RequestCharacterInfo(rciData);
        }
    }
}
