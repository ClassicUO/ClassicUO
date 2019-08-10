#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class ColorBox : Control
    {
        private Color _colorRGBA;

        public ColorBox(int width, int height, ushort hue, uint pol)
        {
            CanMove = false;

            SetColor(hue, pol);

            Width = width;
            Height = height;

            WantUpdateSize = false;
        }

        public ushort Hue { get; private set; }

        public void SetColor(ushort hue, uint pol)
        {
            Hue = hue;

            (byte b, byte g, byte r, byte a) = HuesHelper.GetBGRA(HuesHelper.RgbaToArgb(pol));

            _colorRGBA = new Color(a, b, g, r);

            if (_colorRGBA.A == 0)
                _colorRGBA.A = 0xFF;

            if (Texture == null || Texture.IsDisposed)
                Texture = new UOTexture32(1, 1);
            Texture.SetData(new Color[1] {_colorRGBA});
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (Texture != null && !Texture.IsDisposed)
                Texture.Ticks = (long) totalMS;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();
            return batcher.Draw2D(Texture, x, y, Width, Height, ref _hueVector);
        }

        public override void Dispose()
        {
            Texture?.Dispose();
            base.Dispose();
        }
    }
}