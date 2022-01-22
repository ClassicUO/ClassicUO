#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System.Collections.Generic;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

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

            if (width > 0)
            {
                Width = width;
            }

            if (heigth > 0)
            {
                Height = heigth;
            }
        }

        public GumpPicTiled(List<string> parts) : this(UInt16Converter.Parse(parts[5]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[3]);
            Height = int.Parse(parts[4]);
            IsFromServer = true;
        }


        public ushort Graphic
        {
            get => _graphic;
            set
            {
                if (_graphic != value && value != 0xFFFF)
                {
                    _graphic = value;

                    var texture = GumpsLoader.Instance.GetGumpTexture(_graphic, out var bounds);

                    if (texture == null)
                    {
                        Dispose();

                        return;
                    }

                    Width = bounds.Width;
                    Height = bounds.Height;
                }
            }
        }

        public ushort Hue { get; set; }


        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 hueVector = ShaderHueTranslator.GetHueVector
            (
                Hue,
                false,
                Alpha,
                true
            );

            var texture = GumpsLoader.Instance.GetGumpTexture(Graphic, out var bounds);

            if (texture != null)
            {
                batcher.DrawTiled
                (
                    texture,
                    new Rectangle
                    (
                        x,
                        y,
                        Width,
                        Height
                    ),
                    bounds,
                    hueVector
                );
            }

            return base.Draw(batcher, x, y);
        }

        public override bool Contains(int x, int y)
        {
            int width = Width;
            int height = Height;

            x -= Offset.X;
            y -= Offset.Y;

            var texture = GumpsLoader.Instance.GetGumpTexture(Graphic, out var bounds);

            if (texture == null)
            {
                return false;
            }

            if (width == 0)
            {
                width = bounds.Width;
            }

            if (height == 0)
            {
                height = bounds.Height;
            }

            while (x > bounds.Width && width > bounds.Width)
            {
                x -= bounds.Width;
                width -= bounds.Width;
            }

            while (y > bounds.Height && height > bounds.Height)
            {
                y -= bounds.Height;
                height -= bounds.Height;
            }


            if (x > width || y > height)
            {
                return false;
            }


            return GumpsLoader.Instance.PixelCheck(Graphic, x, y);
        }
    }
}