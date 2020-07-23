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

using System;
using System.Collections.Generic;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Controls
{
    internal class GumpPicTiled : Control
    {
        private ushort _graphic;

        public GumpPicTiled(ushort graphic)
        {
            CanMove = true;
            AcceptMouseInput = true;
            Graphic = graphic;
        }

        public GumpPicTiled(int x, int y, int width, int heigth, ushort graphic) : this(graphic)
        {
            X = x;
            Y = y;
            Width = width;
            Height = heigth;
        }

        public GumpPicTiled(List<string> parts) : this(UInt16Converter.Parse(parts[5]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[3]);
            Height = int.Parse(parts[4]);
            IsFromServer = true;
        }

        internal GumpPicTiled(int x, int y, int width, int heigth, UOTexture32 texture)
        {
            CanMove = true;
            AcceptMouseInput = true;
            X = x;
            Y = y;
            Width = width;
            Height = heigth;
            Graphic = 0xFFFF;
        }

        public ushort Graphic
        {
            get => _graphic;
            set
            {
                if (_graphic != value && value != 0xFFFF)
                {
                    _graphic = value;

                    var texture = GumpsLoader.Instance.GetTexture(_graphic);

                    if (texture == null)
                    {
                        Dispose();
                        return;
                    }

                    Width = texture.Width;
                    Height = texture.Height;
                }
            }
        }

        public ushort Hue { get; set; }


        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();
            ShaderHuesTraslator.GetHueVector(ref _hueVector, Hue, false, Alpha, true);

            var texture = GumpsLoader.Instance.GetTexture(Graphic);

            if (texture != null)
            {
                batcher.Draw2DTiled(texture, x, y, Width, Height, ref _hueVector);
            }

            return base.Draw(batcher, x, y);
        }

        public override bool Contains(int x, int y)
        {
            int width = Width;
            int height = Height;

            var texture = GumpsLoader.Instance.GetTexture(Graphic);

            if (texture == null)
            {
                return false;
            }

            if (width == 0)
                width = texture.Width;

            if (height == 0)
                height = texture.Height;

            while (x > texture.Width && width > texture.Width)
            {
                x -= texture.Width;
                width -= texture.Width;
            }

            while (y > texture.Height && height > texture.Height)
            {
                y -= texture.Height;
                height -= texture.Height;
            }


            if (x > width || y > height)
                return false;


            return texture.Contains(x, y);
        }
    }
}