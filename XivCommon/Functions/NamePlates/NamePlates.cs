using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace XivCommon.Functions.NamePlates {
    public class NamePlates : IDisposable {
        private static class Signatures {
            internal const string NamePlateUpdate = "48 8B C4 41 56 48 81 EC ?? ?? ?? ?? 48 89 58 F0";
        }

        private unsafe delegate IntPtr NamePlateUpdateDelegate(AddonNamePlate* addon, NumberArrayData** numberData, StringArrayData** stringData);

        public delegate void NamePlateUpdateEvent(NamePlateUpdateEventArgs args);

        public event NamePlateUpdateEvent? OnUpdate;

        private GameFunctions Functions { get; }
        private SeStringManager SeStringManager { get; }
        private readonly Hook<NamePlateUpdateDelegate>? _namePlateUpdateHook;

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

        private const int UpdateIndex = 2;
        private const int ColourIndex = 8;
        private const int IconIndex = 13;
        private const int NamePlateObjectIndex = 15;
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

                var nameRaw = strings->StringArray[NameIndex + i];
                var name = Util.ReadSeString((IntPtr) nameRaw, this.SeStringManager);

                var titleRaw = strings->StringArray[TitleIndex + i];
                var title = Util.ReadSeString((IntPtr) titleRaw, this.SeStringManager);

                var fcRaw = strings->StringArray[FreeCompanyIndex + i];
                var fc = Util.ReadSeString((IntPtr) fcRaw, this.SeStringManager);

                var levelRaw = strings->StringArray[LevelIndex + i];
                var level = Util.ReadSeString((IntPtr) levelRaw, this.SeStringManager);

                var args = new NamePlateUpdateEventArgs((uint) info.ActorID) {
                    Name = name,
                    FreeCompany = fc,
                    Title = title,
                    Level = level,
                    Colour = nameColour,
                    Icon = (uint) icon,
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
            }

            Original:
            return this._namePlateUpdateHook!.Original(addon, numberData, stringData);
        }
    }
}
