// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Scenes;
using ClassicUO.Utility;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.Controls
{
    internal class Label : Control
    {
        private RenderedText _gText;

        public Label
        (
            GameContext context,
            string text,
            bool isunicode,
            ushort hue,
            int maxwidth = 0,
            byte font = 0xFF,
            FontStyle style = FontStyle.None,
            TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT,
            bool ishtml = false
        ) : base(context)
        {
            AcceptMouseInput = false;

            var uo = Context?.Game?.UO;
            if (uo != null)
            {
                _gText = RenderedText.Create
                (
                    uo,
                    text,
                    hue,
                    font,
                    isunicode,
                    style,
                    align,
                    maxwidth,
                    isHTML: ishtml
                );

                Width = _gText.Width;
                Height = _gText.Height;
            }
        }

        public Label(List<string> parts, string[] lines, GameContext context) : this
        (
            context,
            int.TryParse(parts[4], out int lineIndex) && lineIndex >= 0 && lineIndex < lines.Length ? lines[lineIndex] : string.Empty,
            true,
            (ushort) (UInt16Converter.Parse(parts[3]) + 1),
            0,
            style: FontStyle.BlackBorder
        )
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            IsFromServer = true;
        }

        public string Text
        {
            get => _gText?.Text ?? string.Empty;
            set
            {
                if (_gText != null)
                {
                    _gText.Text = value;
                    Width = _gText.Width;
                    Height = _gText.Height;
                }
            }
        }


        public ushort Hue
        {
            get => _gText?.Hue ?? 0;
            set
            {
                if (_gText != null && _gText.Hue != value)
                {
                    _gText.Hue = value;
                    _gText.CreateTexture();
                }
            }
        }


        public byte Font
        {
            get => _gText?.Font ?? 0xFF;
        }

        public bool Unicode
        {
            get => _gText?.IsUnicode ?? false;
        }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            if (IsDisposed)
            {
                return false;
            }

            if (_gText == null)
                return false;

            float layerDepth = layerDepthRef;
            renderLists.AddGumpNoAtlas(
                batcher =>
                {
                    _gText.Draw(batcher, x, y, layerDepth, Alpha);

                    return true;
                }
            );

            return base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
        }

        public override void Dispose()
        {
            base.Dispose();
            _gText?.Destroy();
        }
    }
}
