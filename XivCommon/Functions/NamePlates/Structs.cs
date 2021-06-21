using System.Runtime.InteropServices;

namespace XivCommon.Functions.NamePlates {
    [StructLayout(LayoutKind.Explicit, Size = 0x28)]
    internal unsafe struct NumberArrayData {
        [FieldOffset(0x0)]
        public AtkArrayData AtkArrayData;

        [FieldOffset(0x20)]
        public int* IntArray;

        public void SetValue(int index, int value) {
            if (index >= this.AtkArrayData.Size) {
                return;
            }

            if (this.IntArray[index] == value) {
                return;
            }

            this.IntArray[index] = value;
            this.AtkArrayData.HasModifiedData = 1;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x20)]
    internal unsafe struct AtkArrayData {
        [FieldOffset(0x0)]
        public void* vtbl;

        [FieldOffset(0x8)]
        public int Size;

        [FieldOffset(0x1C)]
        public byte Unk1C;

        [FieldOffset(0x1D)]
        public byte Unk1D;

        [FieldOffset(0x1E)]
        public byte HasModifiedData;

        [FieldOffset(0x1F)]
        public byte Unk1F; // initialized to -1
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x30)]
    internal unsafe struct StringArrayData {
        [FieldOffset(0x0)]
        public AtkArrayData AtkArrayData;

        [FieldOffset(0x20)]
        public byte** StringArray; // char * *

        [FieldOffset(0x28)]
        public byte* UnkString; // char *
    }

    /// <summary>
    /// The various different name plate types
    /// </summary>
    public enum PlateType {
        /// <summary>
        /// A normal player name plate
        /// </summary>
        Player = 0,

        /// <summary>
        /// A name plate with the icon and FC tag removed
        /// </summary>
        NoIconOrFc = 1, // 2, 5

        /// <summary>
        /// A name plate with a level string visible, title always below the name, and FC tag removed
        /// </summary>
        LevelNoFc = 3, // 4

        /// <summary>
        /// A name plate with only the name visible
        /// </summary>
        NameOnly = 6,

        /// <summary>
        /// A name plate with only the level string and name visible
        /// </summary>
        LevelAndName = 7,

        /// <summary>
        /// A name plate where the title always appears below the name and the FC tag is removed
        /// </summary>
        LowTitleNoFc = 8,
    }
}
