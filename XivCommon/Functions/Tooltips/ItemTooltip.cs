using Dalamud.Game.Text.SeStringHandling;

namespace XivCommon.Functions.Tooltips {
    public unsafe class ItemTooltip : BaseTooltip {
        public ItemTooltip(SeStringManager manager, Tooltips.StringArrayDataSetStringDelegate sadSetString, byte*** stringArrayData, int** numberArrayData) : base(manager, sadSetString, stringArrayData, numberArrayData) {
        }

        public SeString this[ItemTooltipString its] {
            get => this[(int) its];
            set => this[(int) its] = value;
        }

        public ItemTooltipFields Fields {
            get => (ItemTooltipFields) (*(*(this.NumberArrayData + 4) + 4));
            set => *(*(this.NumberArrayData + 4) + 4) = (int) value;
        }
    }
}
