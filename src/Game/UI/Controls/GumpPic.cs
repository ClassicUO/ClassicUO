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

using System.Collections.Generic;

using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Renderer;

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

        public Graphic Graphic
        {
            get => _graphic;
            set
            {
                _graphic = value;

                Texture = FileManager.Gumps.GetTexture(_graphic);

                if (Texture == null)
                {
                    Dispose();
                    return;
                }

                Width = Texture.Width;
                Height = Texture.Height;
            }
        }

        public Hue Hue { get; set; }


        public override void Update(double totalMS, double frameMS)
        {
            if (Texture == null)
            {
                Dispose();

                return;
            }

            Texture.Ticks = (long) totalMS;

            base.Update(totalMS, frameMS);
        }

        public override bool Contains(int x, int y)
        {
            if (Texture.Contains(x, y))
                return true;

            for (int i = 0; i < Children.Count; i++)
            {
                var c = Children[i];

                if (c.Contains(x, y))
                    return true;
            }

            return false;
        }
    }

    internal class GumpPic : GumpPicBase
    {
        public GumpPic(int x, int y, Graphic graphic, Hue hue)
        {
            X = x;
            Y = y;
            Graphic = graphic;
            Hue = hue;

            if (Texture == null)
                Dispose();
            else
            {
                Width = Texture.Width;
                Height = Texture.Height;
            }
        }

        public GumpPic(List<string> parts) : this(int.Parse(parts[1]), int.Parse(parts[2]), Graphic.Parse(parts[3]), (ushort) (parts.Count > 4 ? TransformHue((ushort) (Hue.Parse(parts[4].Substring(parts[4].IndexOf('=') + 1)) + 1)) : 0))
        {
        }

        public GumpPic(int x, int y, UOTexture texture, Hue hue)
        {
            X = x;
            Y = y;

            Hue = hue;

            Texture = texture;

            if (Texture == null)
                Dispose();
            else
            {
                Width = Texture.Width;
                Height = Texture.Height;
            }
            WantUpdateSize = false;
        }

        public bool IsPartialHue { get; set; }
        public bool ContainsByBounds { get; set; }
        public bool IsVirtue { get; set; }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            if (IsVirtue && button == MouseButton.Left)
            {
                NetClient.Socket.Send(new PVirtueGumpReponse(World.Player, Graphic.Value));

                return false;
            }

            return base.OnMouseDoubleClick(x, y, button);
        }

        public override bool Contains(int x, int y)
        {
            return ContainsByBounds || base.Contains(x, y);
        }

        private static ushort TransformHue(ushort hue)
        {
            if (hue <= 2)
                hue = 0;

            //if (hue < 2)
            //    hue = 1;
            return hue;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
                return false;

            ResetHueVector();
            ShaderHuesTraslator.GetHueVector(ref _hueVector, Hue, IsPartialHue, Alpha, true);

            batcher.Draw2D(Texture, x, y, Width, Height, ref _hueVector);

            return base.Draw(batcher, x, y);
        }
    }
}