using Dalamud.Game.Text.SeStringHandling;

namespace XivCommon.Functions.Tooltips {
    public unsafe class ActionTooltip : BaseTooltip {
        public ActionTooltip(SeStringManager manager, Tooltips.StringArrayDataSetStringDelegate sadSetString, byte*** stringArrayData, int** numberArrayData) : base(manager, sadSetString, stringArrayData, numberArrayData) {
        }

        public SeString this[ActionTooltipString ats] {
            get => this[(int) ats];
            set => this[(int) ats] = value;
        }

        public ActionTooltipFields Fields {
            get => (ActionTooltipFields) (**(this.NumberArrayData + 4));
            set => **(this.NumberArrayData + 4) = (int) value;
        }
    }
}
