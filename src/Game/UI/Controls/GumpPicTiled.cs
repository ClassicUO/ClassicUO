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

        internal GumpPicTiled(int x, int y, int width, int heigth, UOTexture texture)
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

                    UOTexture texture = GumpsLoader.Instance.GetTexture(_graphic);

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
            ShaderHueTranslator.GetHueVector(ref HueVector, Hue, false, Alpha, true);

            UOTexture texture = GumpsLoader.Instance.GetTexture(Graphic);

            if (texture != null)
            {
                batcher.Draw2DTiled(texture, x, y, Width, Height, ref HueVector);
            }

            return base.Draw(batcher, x, y);
        }

        public override bool Contains(int x, int y)
        {
            int width = Width;
            int height = Height;

            x -= Offset.X;
            y -= Offset.Y;

            UOTexture texture = GumpsLoader.Instance.GetTexture(Graphic);

            if (texture == null)
            {
                return false;
            }

            if (width == 0)
            {
                width = texture.Width;
            }

            if (height == 0)
            {
                height = texture.Height;
            }

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
            {
                return false;
            }


            return texture.Contains(x, y);
        }
    }
}