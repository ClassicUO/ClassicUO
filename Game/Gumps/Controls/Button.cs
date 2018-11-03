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

    public class Button : GumpControl
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
            _textures[NORMAL] = IO.Resources.Gumps.GetGumpTexture(normal);
            _textures[PRESSED] = IO.Resources.Gumps.GetGumpTexture(pressed);
            if (over > 0) _textures[OVER] = IO.Resources.Gumps.GetGumpTexture(over);
            ref SpriteTexture t = ref _textures[NORMAL];
            Width = t.Width;
            Height = t.Height;
            FontHue = normalHue == ushort.MaxValue ? (ushort) 0 : normalHue;
            HueHover = hoverHue == ushort.MaxValue ? normalHue : hoverHue;

            if (!string.IsNullOrEmpty(caption) && normalHue != ushort.MaxValue)
            {
                _caption = caption;

                RenderedText renderedText = new RenderedText
                {
                    IsUnicode = isunicode, Hue = FontHue, Font = font, Text = caption
                };
                _fontTexture[0] = renderedText;

                if (hoverHue != ushort.MaxValue)
                {
                    renderedText = new RenderedText
                    {
                        IsUnicode = isunicode, Hue = HueHover, Font = font, Text = caption
                    };
                    _fontTexture[1] = renderedText;
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
            set => _textures[NORMAL] = IO.Resources.Gumps.GetGumpTexture((ushort) value);
        }

        public int ButtonGraphicPressed
        {
            get => _gumpGraphics[PRESSED];
            set => _textures[PRESSED] = IO.Resources.Gumps.GetGumpTexture((ushort) value);
        }

        public int ButtonGraphicOver
        {
            get => _gumpGraphics[OVER];
            set => _textures[OVER] = IO.Resources.Gumps.GetGumpTexture((ushort) value);
        }

        public Hue FontHue { get; }

        public Hue HueHover { get; }

        public bool FontCenter { get; set; }

        public override void Update(double totalMS, double frameMS)
        {
            for (int i = 0; i < _textures.Length; i++)
            {
                if (_textures[i] != null)
                    _textures[i].Ticks = CoreGame.Ticks;
            }

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            SpriteTexture texture = GetTextureByState();
            spriteBatch.Draw2D(texture, new Rectangle((int) position.X, (int) position.Y, Width, Height), IsTransparent ? RenderExtentions.GetHueVector(0, false, 0.5f, false) : Vector3.Zero);

            if (!string.IsNullOrEmpty(_caption))
            {
                RenderedText textTexture = _fontTexture[UIManager.MouseOverControl == this ? 1 : 0];

                if (FontCenter)
                {
                    int yoffset = _clicked ? 1 : 0;
                    textTexture.Draw(spriteBatch, new Vector3(position.X + (Width - textTexture.Width) / 2, position.Y + yoffset + (Height - textTexture.Height) / 2, position.Z));
                }
                else
                    textTexture.Draw(spriteBatch, position);
            }

            return base.Draw(spriteBatch, position, hue);
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
            return IO.Resources.Gumps.Contains(GetGraphicByState(), x, y) || Bounds.Contains(X + x, Y + y);
        }
    }
}