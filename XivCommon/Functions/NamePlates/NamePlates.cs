using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace XivCommon.Functions.NamePlates {
    /// <summary>
    /// The class containing name plate functionality
    /// </summary>
    public class NamePlates : IDisposable {
        private static class Signatures {
            internal const string NamePlateUpdate = "48 8B C4 41 56 48 81 EC ?? ?? ?? ?? 48 89 58 F0";
        }

        private unsafe delegate IntPtr NamePlateUpdateDelegate(AddonNamePlate* addon, NumberArrayData** numberData, StringArrayData** stringData);

        /// <summary>
        /// The delegate for name plate update events.
        /// </summary>
        public delegate void NamePlateUpdateEvent(NamePlateUpdateEventArgs args);

        /// <summary>
        /// <para>
        /// The event that is fired when a name plate is due to update.
        /// </para>
        /// <para>
        /// Requires the <see cref="Hooks.NamePlates"/> hook to be enabled.
        /// </para>
        /// </summary>
        public event NamePlateUpdateEvent? OnUpdate;

        private GameFunctions Functions { get; }
        private SeStringManager SeStringManager { get; }
        private readonly Hook<NamePlateUpdateDelegate>? _namePlateUpdateHook;

        /// <summary>
        /// <para>
        /// If all name plates should be forced to redraw.
        /// </para>
        /// <para>
        /// This is useful for forcing your changes to apply to existing name plates when the plugin is hot-loaded.
        /// </para>
        /// </summary>
        public bool ForceRedraw { get; set; }

        internal NamePlates(GameFunctions functions, SigScanner scanner, SeStringManager manager, bool hookEnabled) {
            this.Functions = functions;
            this.SeStringManager = manager;

            if (!hookEnabled) {
                return;
            }

            if (scanner.TryScanText(Signatures.NamePlateUpdate, out var updatePtr)) {
                unsafe {
                    this._namePlateUpdateHook = new Hook<NamePlateUpdateDelegate>(updatePtr, new NamePlateUpdateDelegate(this.NamePlateUpdateDetour));
                }

                this._namePlateUpdateHook.Enable();
            }
        }

        /// <inheritdoc />
        public void Dispose() {
            this._namePlateUpdateHook?.Dispose();
        }

        private const int PlateTypeIndex = 1;
        private const int UpdateIndex = 2;
        private const int ColourIndex = 8;
        private const int IconIndex = 13;
        private const int NamePlateObjectIndex = 15;
        private const int FlagsIndex = 17;
        private const int NameIndex = 0;
        private const int TitleIndex = 50;
        private const int FreeCompanyIndex = 100;
        private const int LevelIndex = 150;

        private unsafe IntPtr NamePlateUpdateDetour(AddonNamePlate* addon, NumberArrayData** numberData, StringArrayData** stringData) {
            // don't skip to original if no subscribers because of ForceRedraw

            var numbers = numberData[5];
            var strings = stringData[4];
            var atkModule = (RaptureAtkModule*) this.Functions.GetAtkModule();

            var active = numbers->IntArray[0];

            var force = this.ForceRedraw;
            if (force) {
                this.ForceRedraw = false;
            }

            for (var i = 0; i < active; i++) {
                var numbersIndex = i * 19 + 5;

                if (force) {
                    numbers->SetValue(numbersIndex + UpdateIndex, numbers->IntArray[numbersIndex + UpdateIndex] | 1 | 2);
                }

                if (this.OnUpdate == null) {
                    continue;
                }

                if (numbers->IntArray[numbersIndex + UpdateIndex] == 0) {
                    continue;
                }

                var npObjIndex = numbers->IntArray[numbersIndex + NamePlateObjectIndex];
                var info = (&atkModule->NamePlateInfoArray)[npObjIndex];

                var icon = numbers->IntArray[numbersIndex + IconIndex];
                var nameColour = *(ByteColor*) &numbers->IntArray[numbersIndex + ColourIndex];
                var plateType = numbers->IntArray[numbersIndex + PlateTypeIndex];
                var flags = numbers->IntArray[numbersIndex + FlagsIndex];

                var nameRaw = strings->StringArray[NameIndex + i];
                var name = Util.ReadSeString((IntPtr) nameRaw, this.SeStringManager);

                var titleRaw = strings->StringArray[TitleIndex + i];
                var title = Util.ReadSeString((IntPtr) titleRaw, this.SeStringManager);

                var fcRaw = strings->StringArray[FreeCompanyIndex + i];
                var fc = Util.ReadSeString((IntPtr) fcRaw, this.SeStringManager);

                var levelRaw = strings->StringArray[LevelIndex + i];
                var level = Util.ReadSeString((IntPtr) levelRaw, this.SeStringManager);

                var args = new NamePlateUpdateEventArgs((uint) info.ActorID) {
                    Name = new SeString(name.Payloads),
                    FreeCompany = new SeString(fc.Payloads),
                    Title = new SeString(title.Payloads),
                    Level = new SeString(level.Payloads),
                    Colour = nameColour,
                    Icon = (uint) icon,
                    Type = (PlateType) plateType,
                    Flags = flags,
                };

                this.OnUpdate?.Invoke(args);

                void Replace(byte[] bytes, int i) {
                    var mem = this.Functions.UiAlloc.Alloc((ulong) bytes.Length + 1);
                    Marshal.Copy(bytes, 0, mem, bytes.Length);
                    *(byte*) (mem + bytes.Length) = 0;
                    this.Functions.UiAlloc.Free((IntPtr) strings->StringArray[i]);
                    strings->StringArray[i] = (byte*) mem;
                }

                if (name != args.Name) {
                    Replace(args.Name.Encode(), NameIndex + i);
                }

                if (title != args.Title) {
                    Replace(args.Title.Encode(), TitleIndex + i);
                }

                if (fc != args.FreeCompany) {
                    Replace(args.FreeCompany.Encode(), FreeCompanyIndex + i);
                }

                if (level != args.Level) {
                    Replace(args.Level.Encode(), LevelIndex + i);
                }

                if (icon != args.Icon) {
                    numbers->SetValue(numbersIndex + IconIndex, (int) args.Icon);
                }

                var colour = args.Colour;
                var colourInt = *(int*) &colour;
                if (colourInt != numbers->IntArray[numbersIndex + ColourIndex]) {
                    numbers->SetValue(numbersIndex + ColourIndex, colourInt);
                }

                if (plateType != (int) args.Type) {
                    numbers->SetValue(numbersIndex + PlateTypeIndex, (int) args.Type);
                }

                if (flags != args.Flags) {
                    numbers->SetValue(numbersIndex + FlagsIndex, args.Flags);
                }
            }

            return this._namePlateUpdateHook!.Original(addon, numberData, stringData);
        }
    }
}
