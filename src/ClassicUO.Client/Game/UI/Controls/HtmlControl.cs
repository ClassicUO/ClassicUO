// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using ClassicUO.Utility.Platforms;
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
            IsFromServer = true;

            if (textIndex >= 0 && textIndex < lines.Length)
            {
                InternalBuild(lines[textIndex], 0);
            }
        }

        public HtmlControl
        (
            int x,
            int y,
            int w,
            int h,
            bool hasbackground,
            bool hasscrollbar,
            bool useflagscrollbar = false,
            string text = "",
            int hue = 0,
            bool ishtml = false,
            byte font = 1,
            bool isunicode = true,
            FontStyle style = FontStyle.None,
            TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT
        ) : this()
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
                        if (hue == 0x00FFFFFF || hue == 0xFFFF || hue == 0xFF)
                        {
                            htmlColor = 0xFFFFFFFE;
                        }
                        else
                        {
                            htmlColor = (HuesHelper.Color16To32((ushort) hue) << 8) | 0xFF;
                        }
                    }
                    else if (!HasBackground)
                    {
                        color = 0xFFFF;

                        if (!HasScrollbar)
                        {
                            htmlColor = 0x010101FF;
                        }
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
                Add
                (
                    new ResizePic(0x2486)
                    {
                        Width = Width - (HasScrollbar ? 16 : 0), Height = Height, AcceptMouseInput = false
                    }
                );
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
                {
                    _scrollBar = new ScrollBar(Width - 14, 0, Height);
                }

                _scrollBar.Height = Height;
                _scrollBar.MinValue = 0;

                _scrollBar.MaxValue = /* _gameText.Height*/ /* Children.Sum(s => s.Height) - Height +*/
                    _gameText.Height - Height + (HasBackground ? 8 : 0);

                ScrollY = _scrollBar.Value;

                Add(_scrollBar);
            }

            //if (Width != _gameText.Width)
            //    Width = _gameText.Width;
        }

        protected override void OnMouseWheel(MouseEventType delta)
        {
            if (!HasScrollbar)
            {
                return;
            }

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

        public override void Update()
        {
            if (HasScrollbar)
            {
                if (WantUpdateSize)
                {
                    _scrollBar.Height = Height;
                    _scrollBar.MinValue = 0;

                    _scrollBar.MaxValue = /* _gameText.Height*/ /*Children.Sum(s => s.Height) - Height */
                        _gameText.Height - Height + (HasBackground ? 8 : 0);

                    //_scrollBar.IsVisible = _scrollBar.MaxValue > _scrollBar.MinValue;
                    WantUpdateSize = false;
                }

                ScrollY = _scrollBar.Value;
            }

            base.Update();
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            if (batcher.ClipBegin(x, y, Width, Height))
            {
                base.Draw(batcher, x, y);

                int offset = HasBackground ? 4 : 0;

                _gameText.Draw
                (
                    batcher,
                    x + offset,
                    y + offset,
                    ScrollX,
                    ScrollY,
                    Width + ScrollX,
                    Height + ScrollY
                );

                batcher.ClipEnd();
            }


            return true;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                if (_gameText != null)
                {
                    for (int i = 0; i < _gameText.Links.Count; i++)
                    {
                        WebLinkRect link = _gameText.Links[i];

                        bool inbounds = link.Bounds.Contains(x, (_scrollBar == null ? 0 : _scrollBar.Value) + y);

                        if (inbounds && Client.Game.UO.FileManager.Fonts.GetWebLink(link.LinkID, out WebLink result))
                        {
                            Log.Info("LINK CLICKED: " + result.Link);

                            PlatformHelper.LaunchBrowser(result.Link);

                            _gameText.CreateTexture();

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