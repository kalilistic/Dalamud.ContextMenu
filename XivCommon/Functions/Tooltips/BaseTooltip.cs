using System;
using Dalamud.Game.Text.SeStringHandling;

namespace XivCommon.Functions.Tooltips {
    public abstract unsafe class BaseTooltip {
        protected SeStringManager Manager { get; }
        protected Tooltips.StringArrayDataSetStringDelegate SadSetString { get; }
        protected readonly byte*** _pointer; // this is StringArrayData* when ClientStructs is updated

        internal BaseTooltip(SeStringManager manager, Tooltips.StringArrayDataSetStringDelegate sadSetString, byte*** pointer) {
            this.Manager = manager;
            this.SadSetString = sadSetString;
            this._pointer = pointer;
        }

        protected SeString this[int offset] {
            get {
                var ptr = *(this._pointer + 4) + offset;
                return Util.ReadSeString((IntPtr) (*ptr), this.Manager);
            }
            set {
                var encoded = value.Encode().Terminate();

                fixed (byte* encodedPtr = encoded) {
                    this.SadSetString((IntPtr) this._pointer, offset, encodedPtr, 0, 1, 1);
                }
            }
        }
    }
}
