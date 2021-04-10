using System;
using System.Collections.Generic;

namespace XivCommon {
    internal static class Util {
        public static byte[] Terminate(this byte[] array) {
            var terminated = new byte[array.Length + 1];
            Array.Copy(array, terminated, array.Length);
            terminated[terminated.Length - 1] = 0;

            return terminated;
        }

        public static unsafe byte[] ReadTerminated(IntPtr memory) {
            var buf = new List<byte>();

            var ptr = (byte*) memory;
            while (*ptr != 0) {
                buf.Add(*ptr);
                ptr += 1;
            }

            return buf.ToArray();
        }
    }
}
