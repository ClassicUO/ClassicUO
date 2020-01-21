#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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

using System.Collections.Generic;

using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class StaticPic : Control
    {
        private readonly bool _isPartial;

        public StaticPic(ushort graphic, ushort hue)
        {
            Hue = hue;
            _isPartial = TileDataLoader.Instance.StaticData[graphic].IsPartialHue;
            CanMove = true;

            Texture = ArtLoader.Instance.GetTexture(graphic);
            Width = Texture.Width;
            Height = Texture.Height;

            WantUpdateSize = false;
        }

        public StaticPic(List<string> parts) : this(UInt16Converter.Parse(parts[3]), parts.Count > 4 ? UInt16Converter.Parse(parts[4]) : (ushort) 0)
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
        }

        public ushort Hue { get; set; }

        public override void Update(double totalMS, double frameMS)
        {
            Texture.Ticks = (long) totalMS;
            base.Update(totalMS, frameMS);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();
            ShaderHuesTraslator.GetHueVector(ref _hueVector, Hue, _isPartial, 0);

            batcher.Draw2D(Texture, x, y, /*Width, Height,*/ ref _hueVector);

            return base.Draw(batcher, x, y);
        }

        public override bool Contains(int x, int y)
        {
            return Texture.Contains(x, y);
        }
    }
}