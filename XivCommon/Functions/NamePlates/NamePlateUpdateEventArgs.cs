using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.Graphics;

namespace XivCommon.Functions.NamePlates {
    public class NamePlateUpdateEventArgs {
        public uint ActorId { get; }
        public SeString Name { get; set; }
        public SeString FreeCompany { get; set; }
        public SeString Title { get; set; }
        public SeString Level { get; set; }
        public uint Icon { get; set; }
        public ByteColor Colour { get; set; }

        internal NamePlateUpdateEventArgs(uint actorId) {
            this.ActorId = actorId;
        }
    }
}
