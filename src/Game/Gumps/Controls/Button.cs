#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.Controls
{
    public enum ButtonAction
    {
        Default = 0,
        SwitchPage = 0,
        Activate = 1
    }

    public class Button : Control
    {
        private const int NORMAL = 0;
        private const int PRESSED = 1;
        private const int OVER = 2;
        private readonly string _caption;
        private readonly RenderedText[] _fontTexture = new RenderedText[2];
        private readonly Graphic[] _gumpGraphics = new Graphic[3];
        private readonly SpriteTexture[] _textures = new SpriteTexture[3];
        private bool _clicked;

        public Button(int buttonID, ushort normal, ushort pressed, ushort over = 0, string caption = "", byte font = 0, bool isunicode = true, ushort normalHue = ushort.MaxValue, ushort hoverHue = ushort.MaxValue)
        {
            ButtonID = buttonID;
            _gumpGraphics[NORMAL] = normal;
            _gumpGraphics[PRESSED] = pressed;
            _gumpGraphics[OVER] = over;
            _textures[NORMAL] = FileManager.Gumps.GetTexture(normal);
            _textures[PRESSED] = FileManager.Gumps.GetTexture(pressed);
            if (over > 0) _textures[OVER] = FileManager.Gumps.GetTexture(over);
            ref SpriteTexture t = ref _textures[NORMAL];

            if (t == null)
            {
                Dispose();

                return;
            }

            Width = t.Width;
            Height = t.Height;
            FontHue = normalHue == ushort.MaxValue ? (ushort) 0 : normalHue;
            HueHover = hoverHue == ushort.MaxValue ? normalHue : hoverHue;

            if (!string.IsNullOrEmpty(caption) && normalHue != ushort.MaxValue)
            {
                _caption = caption;

                _fontTexture[0] = new RenderedText
                {
                    IsUnicode = isunicode,
                    Hue = FontHue,
                    Font = font,
                    Text = caption
                }; 

                if (hoverHue != ushort.MaxValue)
                {
                    _fontTexture[1] = new RenderedText
                    {
                        IsUnicode = isunicode, Hue = HueHover, Font = font, Text = caption
                    };
                }
            }

            CanMove = false;
            AcceptMouseInput = true;
            CanCloseWithRightClick = false;
            CanCloseWithEsc = false;
        }

        public Button(string[] parts) : this(parts.Length >= 8 ? int.Parse(parts[7]) : 0, ushort.Parse(parts[3]), ushort.Parse(parts[4]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            ButtonAction = parts.Length >= 6 ? (ButtonAction) int.Parse(parts[5]) : 0;
            ToPage = parts.Length >= 7 ? int.Parse(parts[6]) : 0;
        }

        public int ButtonID { get; }

        public ButtonAction ButtonAction { get; set; }

        public int ToPage { get; set; }

        protected override ClickPriority Priority => ClickPriority.High;

        public int ButtonGraphicNormal
        {
            get => _gumpGraphics[NORMAL];
            set => _textures[NORMAL] = FileManager.Gumps.GetTexture((ushort) value);
        }

        public int ButtonGraphicPressed
        {
            get => _gumpGraphics[PRESSED];
            set => _textures[PRESSED] = FileManager.Gumps.GetTexture((ushort) value);
        }

        public int ButtonGraphicOver
        {
            get => _gumpGraphics[OVER];
            set => _textures[OVER] = FileManager.Gumps.GetTexture((ushort) value);
        }

        public Hue FontHue { get; }

        public Hue HueHover { get; }

        public bool FontCenter { get; set; }

        public override void Update(double totalMS, double frameMS)
        {
            for (int i = 0; i < _textures.Length; i++)
            {
                if (_textures[i] != null)
                    _textures[i].Ticks = Engine.Ticks;
            }

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {
            SpriteTexture texture = GetTextureByState();
            batcher.Draw2D(texture, new Rectangle(position.X, position.Y, Width, Height), IsTransparent ? ShaderHuesTraslator.GetHueVector(0, false, 0.5f, false) : Vector3.Zero);

            //Draw1(batcher, texture, new Rectangle((int) position.X, (int) position.Y, Width, Height), -1, 0, IsTransparent ? ShaderHuesTraslator.GetHueVector(0, false, 0.5f, false) : Vector3.Zero);

            if (!string.IsNullOrEmpty(_caption))
            {
                RenderedText textTexture = _fontTexture[Engine.UI.MouseOverControl == this ? 1 : 0];

                if (FontCenter)
                {
                    int yoffset = _clicked ? 1 : 0;
                    textTexture.Draw(batcher, new Point(position.X + ((Width - textTexture.Width) >> 1), position.Y + yoffset + ((Height - textTexture.Height) >> 1)));
                }
                else
                    textTexture.Draw(batcher, position);
            }

            return base.Draw(batcher, position, hue);
        }

        protected override void OnMouseDown(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
                _clicked = true;
        }

        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
                _clicked = false;
        }

        private SpriteTexture GetTextureByState()
        {
            if (MouseIsOver)
            {
                if (_clicked && _textures[PRESSED] != null)
                    return _textures[PRESSED];

                if (_textures[OVER] != null)
                    return _textures[OVER];
            }

            return _textures[NORMAL];
        }

        private Graphic GetGraphicByState()
        {
            if (MouseIsOver)
            {
                if (_clicked && _textures[PRESSED] != null)
                    return _gumpGraphics[PRESSED];

                if (_textures[OVER] != null)
                    return _gumpGraphics[OVER];
            }

            return _gumpGraphics[NORMAL];
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                switch (ButtonAction)
                {
                    case ButtonAction.SwitchPage:
                        ChangePage(ToPage);

                        break;
                    case ButtonAction.Activate:
                        OnButtonClick(ButtonID);

                        break;
                }
            }
        }

        protected override bool Contains(int x, int y)
        {
            SpriteTexture texture = GetTextureByState();

            return texture.Contains(x, y);
            //return (Texture != null && Texture.Contains(x, y)) /*|| Bounds.Contains(X + x, Y + y)*/;
            //return FileManager.Gumps.Contains(GetGraphicByState(), x, y) || Bounds.Contains(X + x, Y + y);
        }

        public override void Dispose()
        {
            for (int i = 0; i < _fontTexture.Length; i++) _fontTexture[i]?.Dispose();
            base.Dispose();
        }
    }
}