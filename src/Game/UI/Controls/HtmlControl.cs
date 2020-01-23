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
using System.Diagnostics;

using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class HtmlControl : Control
    {
        private RenderedText _gameText;
        private ScrollBarBase _scrollBar;

        public HtmlControl(List<string> parts, string[] lines) : this()
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[3]);
            Height = int.Parse(parts[4]);
            int textIndex = int.Parse(parts[5]);
            HasBackground = parts[6] == "1";
            HasScrollbar = parts[7] != "0";
            UseFlagScrollbar = HasScrollbar && parts[7] == "2";
            _gameText.IsHTML = true;
            _gameText.MaxWidth = Width - (HasScrollbar ? 16 : 0) - (HasBackground ? 8 : 0);

            if (textIndex >= 0 && textIndex < lines.Length)
                InternalBuild(lines[textIndex], 0);
        }

        public HtmlControl(int x, int y, int w, int h, bool hasbackground, bool hasscrollbar, bool useflagscrollbar = false, string text = "", int hue = 0, bool ishtml = false, byte font = 1, bool isunicode = true, FontStyle style = FontStyle.None, TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT) : this()
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            HasBackground = hasbackground;
            HasScrollbar = hasscrollbar;
            UseFlagScrollbar = useflagscrollbar; //hasscrollbar != 0 && hasscrollbar == 2;

            if (!string.IsNullOrEmpty(text))
            {
                _gameText.IsHTML = ishtml;
                _gameText.FontStyle = style;
                _gameText.Align = align;
                _gameText.Font = font;
                _gameText.IsUnicode = isunicode;
                _gameText.MaxWidth = w - (HasScrollbar ? 16 : 0) - (HasBackground ? 8 : 0);
            }

            InternalBuild(text, hue);
        }

        public HtmlControl()
        {
            _gameText = RenderedText.Create(string.Empty, isunicode: true, font: 1);

            CanMove = true;
        }

        public bool HasScrollbar { get; }

        public bool HasBackground { get; }

        public bool UseFlagScrollbar { get; }

        public int ScrollX { get; set; }

        public int ScrollY { get; set; }

        public string Text
        {
            get => _gameText.Text;
            set => _gameText.Text = value;
        }

        private void InternalBuild(string text, int hue)
        {
            if (!string.IsNullOrEmpty(text))
            {
                if (_gameText.IsHTML)
                {
                    uint htmlColor = 0xFFFFFFFF;
                    ushort color = 0;

                    if (hue > 0)
                    {
                        if (hue == 0x00FFFFFF)
                            htmlColor = 0xFFFFFFFE;
                        else
                            htmlColor = (HuesHelper.Color16To32((ushort) hue) << 8) | 0xFF;
                    }
                    else if (!HasBackground)
                    {
                        color = 0xFFFF;

                        if (!HasScrollbar)
                            htmlColor = 0x010101FF;
                    }
                    else
                    {
                        _gameText.MaxWidth -= 9;
                        htmlColor = 0x010101FF;
                    }

                    _gameText.HTMLColor = htmlColor;
                    _gameText.Hue = color;
                }
                else
                {
                    _gameText.Hue = (ushort) hue;
                }

                _gameText.HasBackgroundColor = !HasBackground;
                _gameText.Text = text;
            }

            if (HasBackground)
            {
                Add(new ResizePic(0x2486)
                {
                    Width = Width - (HasScrollbar ? 16 : 0), Height = Height, AcceptMouseInput = false
                });
            }

            if (HasScrollbar)
            {
                if (UseFlagScrollbar)
                {
                    _scrollBar = new ScrollFlag
                    {
                        Location = new Point(Width - 14, 0)
                    };
                }
                else
                    _scrollBar = new ScrollBar(Width - 14, 0, Height);

                _scrollBar.Height = Height;
                _scrollBar.MinValue = 0;
                _scrollBar.MaxValue = /* _gameText.Height*/ /* Children.Sum(s => s.Height) - Height +*/ _gameText.Height - Height + (HasBackground ? 8 : 0);
                ScrollY = _scrollBar.Value;

                Add(_scrollBar);
            }

            //if (Width != _gameText.Width)
            //    Width = _gameText.Width;
        }

        protected override void OnMouseWheel(MouseEventType delta)
        {
            if (!HasScrollbar)
                return;

            switch (delta)
            {
                case MouseEventType.WheelScrollUp:
                    _scrollBar.Value -= _scrollBar.ScrollStep;

                    break;

                case MouseEventType.WheelScrollDown:
                    _scrollBar.Value += _scrollBar.ScrollStep;

                    break;
            }
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (HasScrollbar)
            {
                if (WantUpdateSize)
                {
                    _scrollBar.Height = Height;
                    _scrollBar.MinValue = 0;
                    _scrollBar.MaxValue = /* _gameText.Height*/ /*Children.Sum(s => s.Height) - Height */_gameText.Height - Height + (HasBackground ? 8 : 0);
                    //_scrollBar.IsVisible = _scrollBar.MaxValue > _scrollBar.MinValue;
                    WantUpdateSize = false;
                }

                ScrollY = _scrollBar.Value;
            }

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
                return false;

            ResetHueVector();

            Rectangle scissor = ScissorStack.CalculateScissors(Matrix.Identity, x, y, Width, Height);

            if (ScissorStack.PushScissors(scissor))
            {
                batcher.EnableScissorTest(true);
                base.Draw(batcher, x, y);

                _gameText.Draw(batcher,
                    Width + ScrollX, Height + ScrollY,
                    x + (HasBackground ? 4 : 0),
                    y + (HasBackground ? 4 : 0),
                    Width - (HasBackground ? 8 : 0),
                    Height - (HasBackground ? 8 : 0),
                    ScrollX,
                    ScrollY);

                batcher.EnableScissorTest(false);
                ScissorStack.PopScissors();
            }

            return true;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                if (_gameText != null)
                {
                    foreach (WebLinkRect link in _gameText.Links)
                    {
                        Rectangle rect = new Rectangle(link.StartX, link.StartY, link.EndX, link.EndY);
                        bool inbounds = rect.Contains(x, (_scrollBar == null ? 0 : _scrollBar.Value) + y);

                        if (inbounds && FontsLoader.Instance.GetWebLink(link.LinkID, out WebLink result))
                        {
                            Log.Info("LINK CLICKED: " + result.Link);
                            Process.Start(result.Link);

                            break;
                        }
                    }
                }
            }

            base.OnMouseUp(x, y, button);
        }

        public override void Dispose()
        {
            _gameText?.Destroy();
            _gameText = null;
            base.Dispose();
        }
    }
}