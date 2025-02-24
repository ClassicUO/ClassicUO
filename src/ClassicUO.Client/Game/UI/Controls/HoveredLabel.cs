// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Assets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class HoveredLabel : Label
    {
        private readonly ushort _overHue, _normalHue, _selectedHue;

        public HoveredLabel
        (
            string text,
            bool isunicode,
            ushort hue,
            ushort overHue,
            ushort selectedHue,
            int maxwidth = 0,
            byte font = 255,
            FontStyle style = FontStyle.None,
            TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT
        ) : base
        (
            $" {text}",
            isunicode,
            hue,
            maxwidth,
            font,
            style,
            align
        )
        {
            _overHue = overHue;
            _normalHue = hue;
            _selectedHue = selectedHue;
            AcceptMouseInput = true;
        }

        public bool DrawBackgroundCurrentIndex;
        public bool IsSelected, ForceHover;

        public override void Update()
        {
            if (IsSelected)
            {
                if (Hue != _selectedHue)
                {
                    Hue = _selectedHue;
                }
            }
            else if (MouseIsOver || ForceHover)
            {
                if (Hue != _overHue)
                {
                    Hue = _overHue;
                }
            }
            else if (Hue != _normalHue)
            {
                Hue = _normalHue;
            }


            base.Update();
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (DrawBackgroundCurrentIndex && MouseIsOver && !string.IsNullOrWhiteSpace(Text))
            {
                Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

                batcher.Draw
                (
                    SolidColorTextureCache.GetTexture(Color.Gray),
                    new Rectangle
                    (
                        x,
                        y + 2,
                        Width - 4,
                        Height - 4
                    ),
                    hueVector
                );
            }

            return base.Draw(batcher, x, y);
        }
    }
}