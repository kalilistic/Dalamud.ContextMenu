using System;
using System.Collections.Generic;
using Dalamud.Plugin;

namespace XivCommon {
    internal static class Util {
        internal static byte[] Terminate(this byte[] array) {
            var terminated = new byte[array.Length + 1];
            Array.Copy(array, terminated, array.Length);
            terminated[terminated.Length - 1] = 0;

            return terminated;
        }

        internal static unsafe byte[] ReadTerminated(IntPtr memory) {
            if (memory == IntPtr.Zero) {
                return new byte[0];
            }

            var buf = new List<byte>();

            var ptr = (byte*) memory;
            while (*ptr != 0) {
                buf.Add(*ptr);
                ptr += 1;
            }

            return buf.ToArray();
        }

        internal static void PrintMissingSig(string name) {
            Logger.LogWarning($"Could not find signature for {name}. This functionality will be disabled.");
        }
    }
}
