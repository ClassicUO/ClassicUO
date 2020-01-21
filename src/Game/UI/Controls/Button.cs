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

using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    enum ButtonAction
    {
        Default = 0,
        SwitchPage = 0,
        Activate = 1
    }

    internal class Button : Control
    {
        private const int NORMAL = 0;
        private const int PRESSED = 1;
        private const int OVER = 2;
        private readonly string _caption;
        private readonly RenderedText[] _fontTexture;
        private readonly ushort[] _gumpGraphics = new ushort[3];
        private readonly UOTexture[] _textures = new UOTexture[3];

        private bool _entered;

        public Button(int buttonID, ushort normal, ushort pressed, ushort over = 0, string caption = "", byte font = 0, bool isunicode = true, ushort normalHue = ushort.MaxValue, ushort hoverHue = ushort.MaxValue)
        {
            ButtonID = buttonID;
            _gumpGraphics[NORMAL] = normal;
            _gumpGraphics[PRESSED] = pressed;
            _gumpGraphics[OVER] = over;
            _textures[NORMAL] = GumpsLoader.Instance.GetTexture(normal);
            _textures[PRESSED] = GumpsLoader.Instance.GetTexture(pressed);
            if (over > 0) _textures[OVER] = GumpsLoader.Instance.GetTexture(over);
            UOTexture t = _textures[NORMAL];

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
                _fontTexture = new RenderedText[2];

                _caption = caption;

                _fontTexture[0] = RenderedText.Create(caption,FontHue, font, isunicode);

                if (hoverHue != ushort.MaxValue)
                {
                    _fontTexture[1] = RenderedText.Create(caption, HueHover, font, isunicode);
                }
            }

            CanMove = false;
            AcceptMouseInput = true;
            //CanCloseWithRightClick = false;
            CanCloseWithEsc = false;
        }

        public Button(List<string> parts) : this(parts.Count >= 8 ? int.Parse(parts[7]) : 0, UInt16Converter.Parse(parts[3]), UInt16Converter.Parse(parts[4]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);

            if (parts.Count >= 6)
            {
                int action = int.Parse(parts[5]);

                ButtonAction = action == 0 ? ButtonAction.SwitchPage : ButtonAction.Activate;
            }

            ToPage = parts.Count >= 7 ? int.Parse(parts[6]) : 0;
            WantUpdateSize = false;
            ContainsByBounds = true;
        }

        public bool IsClicked { get; set; }

        public int ButtonID { get; }

        public ButtonAction ButtonAction { get; set; }

        public int ToPage { get; set; }

        public override ClickPriority Priority => ClickPriority.High;

        public ushort ButtonGraphicNormal
        {
            get => _gumpGraphics[NORMAL];
            set
            {
                _textures[NORMAL] = GumpsLoader.Instance.GetTexture(value);
                _gumpGraphics[NORMAL] = value;

                Width = _textures[NORMAL].Width;
                Height = _textures[NORMAL].Height;
            }
        }

        public ushort ButtonGraphicPressed
        {
            get => _gumpGraphics[PRESSED];
            set
            {
                _textures[PRESSED] = GumpsLoader.Instance.GetTexture(value);
                _gumpGraphics[PRESSED] = value;
                
                Width = _textures[PRESSED].Width;
                Height = _textures[PRESSED].Height;
            }
        }

        public ushort ButtonGraphicOver
        {
            get => _gumpGraphics[OVER];
            set
            {
                _textures[OVER] = GumpsLoader.Instance.GetTexture(value);
                _gumpGraphics[OVER] = value;

                Width = _textures[OVER].Width;
                Height = _textures[OVER].Height;
            }
        }

        public ushort FontHue { get; }

        public ushort HueHover { get; }

        public bool FontCenter { get; set; }

        public bool ContainsByBounds { get; set; }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (IsDisposed)
                return;

            for (int i = 0; i < _textures.Length; i++)
            {
                UOTexture t = _textures[i];

                if (t != null)
                    t.Ticks = Time.Ticks;
            }
        }

        protected override void OnMouseEnter(int x, int y)
        {
            _entered = true;
        }

        protected override void OnMouseExit(int x, int y)
        {
            _entered = false;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            UOTexture texture = GetTextureByState();

            ResetHueVector();

            _hueVector.Z = Alpha;

            batcher.Draw2D(texture, x, y, Width, Height, ref _hueVector);

            if (!string.IsNullOrEmpty(_caption))
            {
                RenderedText textTexture = _fontTexture[_entered ? 1 : 0];

                if (FontCenter)
                {
                    int yoffset = IsClicked ? 1 : 0;
                    textTexture.Draw(batcher, x + ((Width - textTexture.Width) >> 1), y + yoffset + ((Height - textTexture.Height) >> 1));
                }
                else
                    textTexture.Draw(batcher, x, y);
            }

            return base.Draw(batcher, x, y);
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
                IsClicked = true;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                IsClicked = false;
                if (_entered || Client.Game.Scene is GameScene)
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

                    Mouse.LastLeftButtonClickTime = 0;
                    Mouse.CancelDoubleClick = true;
                }
            }
        }

        private UOTexture GetTextureByState()
        {
            if (_entered || IsClicked)
            {
                if (IsClicked && _textures[PRESSED] != null)
                    return _textures[PRESSED];

                if (_textures[OVER] != null)
                    return _textures[OVER];
            }

            return _textures[NORMAL];
        }

        private ushort GetGraphicByState()
        {
            if (_entered)
            {
                if (IsClicked && _textures[PRESSED] != null)
                    return _gumpGraphics[PRESSED];

                if (_textures[OVER] != null)
                    return _gumpGraphics[OVER];
            }

            return _gumpGraphics[NORMAL];
        }


      
        public override bool Contains(int x, int y)
        {
            if (IsDisposed)
                return false;

            return ContainsByBounds ? base.Contains(x, y) : _textures[NORMAL].Contains(x, y);
        }

        public sealed override void Dispose()
        {
            if (_fontTexture != null)
                foreach (RenderedText t in _fontTexture)
                    t?.Destroy();

            base.Dispose();
        }
    }
}