// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game;
using ClassicUO.Game.Scenes;
using ClassicUO.Utility;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.Controls
{
    internal class CroppedText : Control
    {
        private RenderedText _gameText;

        public CroppedText(string text, ushort hue, GameContext context, int maxWidth = 0) : base(context)
        {
            AcceptMouseInput = false;

            var uo = Context?.Game?.UO;
            if (uo != null)
            {
                _gameText = RenderedText.Create
                (
                    uo,
                    text,
                    hue,
                    (byte) (uo.Version >= ClientVersion.CV_305D ? 1 : 0),
                    true,
                    maxWidth > 0 ? FontStyle.BlackBorder | FontStyle.Cropped : FontStyle.BlackBorder,
                    maxWidth: maxWidth
                );
            }
        }

        public CroppedText(List<string> parts, string[] lines, GameContext context) : this(int.TryParse(parts[6], out int lineIndex) && lineIndex >= 0 && lineIndex < lines.Length ? lines[lineIndex] : string.Empty, (ushort) (UInt16Converter.Parse(parts[5]) + 1), context, int.Parse(parts[3]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[3]);
            Height = int.Parse(parts[4]);
            IsFromServer = true;
        }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            if (_gameText == null)
                return false;

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
