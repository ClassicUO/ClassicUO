// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Scenes;
using ClassicUO.Utility;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.Controls
{
    internal class CroppedText : Control
    {
        private readonly RenderedText _gameText;

        public CroppedText(string text, ushort hue, int maxWidth = 0)
        {
            _gameText = RenderedText.Create
            (
                text,
                hue,
                (byte) (Client.Game.UO.Version >= ClientVersion.CV_305D ? 1 : 0),
                true,
                maxWidth > 0 ? FontStyle.BlackBorder | FontStyle.Cropped : FontStyle.BlackBorder,
                maxWidth: maxWidth
            );

            AcceptMouseInput = false;
        }

        public CroppedText(List<string> parts, string[] lines) : this(int.TryParse(parts[6], out int lineIndex) && lineIndex >= 0 && lineIndex < lines.Length ? lines[lineIndex] : string.Empty, (ushort) (UInt16Converter.Parse(parts[5]) + 1), int.Parse(parts[3]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[3]);
            Height = int.Parse(parts[4]);
            IsFromServer = true;
        }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            float layerDepth = layerDepthRef;
            renderLists.AddGumpNoAtlas
            (
                (batcher) =>
                {
                    _gameText.Draw(batcher, x, y, layerDepth);
                    return true;
                }
            );

            return base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
        }

        public override void Dispose()
        {
            base.Dispose();
            _gameText?.Destroy();
        }
    }
}