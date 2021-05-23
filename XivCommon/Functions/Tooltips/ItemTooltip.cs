using Dalamud.Game.Text.SeStringHandling;

namespace XivCommon.Functions.Tooltips {
    public unsafe class ItemTooltip : BaseTooltip {
        public ItemTooltip(SeStringManager manager, Tooltips.StringArrayDataSetStringDelegate sadSetString, byte*** pointer) : base(manager, sadSetString, pointer) {
        }

        public SeString this[ItemTooltipString its] {
            get => this[(int) its];
            set => this[(int) its] = value;
        }
    }
}
