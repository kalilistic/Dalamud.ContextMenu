using System;
using Dalamud.Game.Text.SeStringHandling;

namespace XivCommon.Functions.Tooltips {
    public abstract unsafe class BaseTooltip {
        protected SeStringManager Manager { get; }
        protected Tooltips.StringArrayDataSetStringDelegate SadSetString { get; }
        protected readonly byte*** StringArrayData; // this is StringArrayData* when ClientStructs is updated
        protected readonly int** NumberArrayData;

        internal BaseTooltip(SeStringManager manager, Tooltips.StringArrayDataSetStringDelegate sadSetString, byte*** stringArrayData, int** numberArrayData) {
            this.Manager = manager;
            this.SadSetString = sadSetString;
            this.StringArrayData = stringArrayData;
            this.NumberArrayData = numberArrayData;
        }

        protected SeString this[int offset] {
            get {
                var ptr = *(this.StringArrayData + 4) + offset;
                return Util.ReadSeString((IntPtr) (*ptr), this.Manager);
            }
            set {
                var encoded = value.Encode().Terminate();

                fixed (byte* encodedPtr = encoded) {
                    this.SadSetString((IntPtr) this.StringArrayData, offset, encodedPtr, 0, 1, 1);
                }
            }
        }
    }
}
