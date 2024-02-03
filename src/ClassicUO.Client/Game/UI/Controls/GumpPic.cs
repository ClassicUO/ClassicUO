#region license

// Copyright (c) 2024, andreakarasho
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
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal abstract class GumpPicBase : Control
    {
        private ushort _graphic;

        protected GumpPicBase()
        {
            CanMove = true;
            AcceptMouseInput = true;
        }

        public ushort Graphic
        {
            get => _graphic;
            set
            {
                _graphic = value;

                ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(_graphic);

                if (gumpInfo.Texture == null)
                {
                    Dispose();

                    return;
                }

                Width = gumpInfo.UV.Width;
                Height = gumpInfo.UV.Height;
            }
        }

        public ushort Hue { get; set; }

        public override bool Contains(int x, int y)
        {
            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(_graphic);

            if (gumpInfo.Texture == null)
            {
                return false;
            }

            if (Client.Game.UO.Gumps.PixelCheck(Graphic, x - Offset.X, y - Offset.Y))
            {
                return true;
            }

            for (int i = 0; i < Children.Count; i++)
            {
                Control c = Children[i];

                // might be wrong x, y. They should be calculated by position
                if (c.Contains(x, y))
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal class GumpPic : GumpPicBase
    {
        public GumpPic(int x, int y, ushort graphic, ushort hue)
        {
            X = x;
            Y = y;
            Graphic = graphic;
            Hue = hue;
            IsFromServer = true;
        }

        public GumpPic(List<string> parts)
            : this(
                int.Parse(parts[1]),
                int.Parse(parts[2]),
                UInt16Converter.Parse(parts[3]),
                (ushort)(
                    parts.Count > 4
                        ? TransformHue(
                            (ushort)(
                                UInt16Converter.Parse(parts[4].Substring(parts[4].IndexOf('=') + 1))
                                + 1
                            )
                        )
                        : 0
                )
            )
        { }

        public bool IsPartialHue { get; set; }
        public bool ContainsByBounds { get; set; }

        public override bool Contains(int x, int y)
        {
            return ContainsByBounds || base.Contains(x, y);
        }

        private static ushort TransformHue(ushort hue)
        {
            if (hue <= 2)
            {
                hue = 0;
            }

            //if (hue < 2)
            //    hue = 1;
            return hue;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(Hue, IsPartialHue, Alpha, true);

            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(Graphic);

            if (gumpInfo.Texture != null)
            {
                batcher.Draw(
                    gumpInfo.Texture,
                    new Rectangle(x, y, Width, Height),
                    gumpInfo.UV,
                    hueVector
                );
            }

            return base.Draw(batcher, x, y);
        }
    }

    internal class VirtueGumpPic : GumpPic
    {
        private readonly World _world;

        public VirtueGumpPic(World world, List<string> parts) : base(parts)
        {
            _world = world;
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                NetClient.Socket.Send_VirtueGumpResponse(_world.Player, Graphic);

                return true;
            }

            return base.OnMouseDoubleClick(x, y, button);
        }
    }

    internal class GumpPicInPic : GumpPicBase
    {
        private readonly Rectangle _picInPicBounds;

        public GumpPicInPic(
            int x,
            int y,
            ushort graphic,
            ushort sx,
            ushort sy,
            ushort width,
            ushort height
        )
        {
            X = x;
            Y = y;
            Graphic = graphic;
            Width = width;
            Height = height;
            _picInPicBounds = new Rectangle(sx, sy, Width, Height);
            IsFromServer = true;
        }

        public GumpPicInPic(List<string> parts)
            : this(
                int.Parse(parts[1]),
                int.Parse(parts[2]),
                UInt16Converter.Parse(parts[3]),
                UInt16Converter.Parse(parts[4]),
                UInt16Converter.Parse(parts[5]),
                UInt16Converter.Parse(parts[6]),
                UInt16Converter.Parse(parts[7])
            )
        { }

        public override bool Contains(int x, int y)
        {
            return true;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(Hue, false, Alpha, true);

            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(Graphic);

            var sourceBounds = new Rectangle(gumpInfo.UV.X + _picInPicBounds.X, gumpInfo.UV.Y + _picInPicBounds.Y, _picInPicBounds.Width, _picInPicBounds.Height);

            if (gumpInfo.Texture != null)
            {
                batcher.Draw(
                    gumpInfo.Texture,
                    new Rectangle(x, y, Width, Height),
                    sourceBounds,
                    hueVector
                );
            }

            return base.Draw(batcher, x, y);
        }
    }
}
