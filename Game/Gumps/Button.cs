#region license

//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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

namespace ClassicUO.Game.Gumps
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


        private readonly SpriteTexture[] _textures = new SpriteTexture[3];
        private readonly Graphic[] _gumpGraphics = new Graphic[3];
        private int _curentState = NORMAL;
        private RenderedText _gText;
        private bool _centerFont;


        public Button(int buttonID, ushort normal, ushort pressed, ushort over = 0)
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

            _gText = new RenderedText
            {
                IsUnicode = true
            };

            CanMove = false;
            AcceptMouseInput = true;
            CanCloseWithRightClick = false;
            CanCloseWithEsc = false;
        }

        public Button(string[] parts) :
            this(parts.Length > 7 ? int.Parse(parts[7]) : 0, ushort.Parse(parts[3]), ushort.Parse(parts[4]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);

            ButtonAction = (ButtonAction) int.Parse(parts[5]);
            ButtonParameter = int.Parse(parts[6]);
        }


        public int ButtonID { get; }
        public ButtonAction ButtonAction { get; set; }
        public int ButtonParameter { get; set; }

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

        public string Text
        {
            get => _gText.Text;
            set => _gText.Text = value;
        }

        public int Font
        {
            get => _gText.Font;
            set => _gText.Font = (byte) value;
        }

        public int FontHue
        {
            get => _gText.Hue;
            set => _gText.Hue = (byte) value;
        }

        public bool IsUnicode
        {
            get => _gText.IsUnicode;
            set => _gText.IsUnicode = value;
        }


        public bool FontCenter
        {
            get => FontCenter;
            set
            {
                if (value) _centerFont = true;
            }
        }


        public override void Update(double totalMS, double frameMS)
        {
            for (int i = 0; i < _textures.Length; i++)
            {
                if (_textures[i] != null)
                    _textures[i].Ticks = World.Ticks;
            }

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            SpriteTexture texture = GetTextureByState();

            spriteBatch.Draw2D(texture,
                new Rectangle((int) position.X, (int) position.Y + (_curentState == PRESSED ? 1 : 0), Width, Height),
                Vector3.Zero);

            if (Text != string.Empty)
            {
                if (_centerFont)
                {
                    if (MouseIsOver)
                    {
                        //var _blackTexture = new Texture2D(Service.Get<SpriteBatch3D>().GraphicsDevice, 1, 1);
                        //_blackTexture.SetData(new[] { Color.Black });
                        //spriteBatch.Draw2D(_blackTexture, new Rectangle((int)position.X + (this.Width - _gText.Width) / 2, (int)position.Y + (this.Height - _gText.Height) / 2 , 100, 50), RenderExtentions.GetHueVector(0, false, true, false));
                    }

                    int yoffset = _curentState == PRESSED ? 1 : 0;
                    _gText.Draw(spriteBatch,
                        new Vector3(position.X + (Width - _gText.Width) / 2,
                            position.Y + yoffset + (Height - _gText.Height) / 2, position.Z));
                }
                else
                    _gText.Draw(spriteBatch, position);
            }

            return base.Draw(spriteBatch, position, hue);
        }


        protected override void OnMouseDown(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
                _curentState = PRESSED;
        }

        private SpriteTexture GetTextureByState()
        {
            if (_curentState == PRESSED)
                return _textures[PRESSED];
            if (UIManager.MouseOverControl == this && _textures[OVER] != null)
                return _textures[OVER];
            return _textures[NORMAL];
        }

        private Graphic GetGraphicByState()
        {
            if (_curentState == PRESSED)
                return _gumpGraphics[PRESSED];
            if (UIManager.MouseOverControl == this && _textures[OVER] != null)
                return _gumpGraphics[OVER];
            return _gumpGraphics[NORMAL];
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                switch (ButtonAction)
                {
                    case ButtonAction.SwitchPage:
                        ChangePage(ButtonParameter);
                        break;
                    case ButtonAction.Activate:
                        OnButtonClick(ButtonID);
                        break;
                }
            }
        }

        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
                _curentState = NORMAL;
        }

        protected override bool Contains(int x, int y)
            => IO.Resources.Gumps.Contains(GetGraphicByState(), x, y);


        public override void Dispose()
        {
            base.Dispose();

            if (_gText != null)
            {
                _gText.Dispose();
                _gText = null;
            }

            for (int i = 0; i < _textures.Length; i++)
            {
                if (_textures[i] != null)
                    _textures[i].Dispose();
            }
        }
    }
}