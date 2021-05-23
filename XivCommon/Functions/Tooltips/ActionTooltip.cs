using Dalamud.Game.Text.SeStringHandling;

namespace XivCommon.Functions.Tooltips {
    public unsafe class ActionTooltip : BaseTooltip {
        public ActionTooltip(SeStringManager manager, Tooltips.StringArrayDataSetStringDelegate sadSetString, byte*** pointer) : base(manager, sadSetString, pointer) {
        }

        public SeString this[ActionTooltipString ats] {
            get => this[(int) ats];
            set => this[(int) ats] = value;
        }
    }
}
