using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Utility;

namespace ClassicUO.Game.Gumps
{
    class RenderedTextList : GumpControl
    {
        private readonly List<RenderedText> _entries;
        private IScrollBar _scrollBar;
        
        
        public RenderedTextList(int x, int y, int width, int height, IScrollBar scrollBarControl)
            : base()
        {
            _scrollBar = scrollBarControl;
            _scrollBar.IsVisible = false;

            AcceptMouseInput = false;

            X = x;
            Y = y;
            Width = width;
            Height = height;
            _entries = new List<RenderedText>();
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            base.Draw(spriteBatch, position, hue);
        
            Vector3 p = new Vector3(position.X, position.Y, 0);
            int height = 0;
            int maxheight = _scrollBar.Value + _scrollBar.Height;

            for (int i = 0; i < _entries.Count; i++)
            {
                if (height + _entries[i].Height <= _scrollBar.Value)
                {
                    // this entry is above the renderable area.
                    height += _entries[i].Height;
                }
                else if (height + _entries[i].Height <= maxheight)
                {
                    int y = height - _scrollBar.Value;
                    if (y < 0)
                    {
                        // this entry starts above the renderable area, but exists partially within it.
                        _entries[i].Draw(spriteBatch, new Rectangle((int)p.X, (int)position.Y, _entries[i].Width, _entries[i].Height + y), 0, -y);
                        p.Y += _entries[i].Height + y;
                    }
                    else
                    {
                        // this entry is completely within the renderable area.
                        _entries[i].Draw(spriteBatch, p);
                        p.Y += _entries[i].Height;
                    }
                    height += _entries[i].Height;
                }
                else
                {
                    int y = maxheight - height;
                    _entries[i].Draw(spriteBatch, new Rectangle((int)p.X, (int)position.Y + _scrollBar.Height - y, _entries[i].Width, y), 0, 0);
                    // can't fit any more entries - so we break!
                    break;
                }
            }

            return true;
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            _scrollBar.Location = new Point(X + Width - 14, Y);
            _scrollBar.Height = Height;
            CalculateScrollBarMaxValue();
            _scrollBar.IsVisible = _scrollBar.MaxValue > _scrollBar.MinValue;
        }

        private void CalculateScrollBarMaxValue()
        {
            bool maxValue = _scrollBar.Value == _scrollBar.MaxValue;

            int height = 0;
            for (int i = 0; i < _entries.Count; i++)
            {
                height += _entries[i].Height;
            }

            height -= _scrollBar.Height;

            if (height > 0)
            {
                _scrollBar.MaxValue = height;
                if (maxValue)
                    _scrollBar.Value = _scrollBar.MaxValue;
            }
            else
            {
                _scrollBar.MaxValue = 0;
                _scrollBar.Value = 0;
            }
        }

        public void AddEntry(string text, int font, Hue hue)
        {
            bool maxScroll = (_scrollBar.Value == _scrollBar.MaxValue);

            while (_entries.Count > 99)
            {
                _entries.RemoveAt(0);
            }

            var entry = new RenderedText()
            {
                MaxWidth = Width -18,
                IsUnicode = true,
                Align = TEXT_ALIGN_TYPE.TS_LEFT,
                FontStyle = FontStyle.Indention | FontStyle.BlackBorder
                
            };

            entry.Hue = hue;
            entry.Font = (byte)font;
            entry.Text = text;
            _entries.Add(entry);
            
            _scrollBar.MaxValue += _entries[_entries.Count - 1].Height;
            if (maxScroll)
            {
                _scrollBar.Value = _scrollBar.MaxValue;
            }
        }

        public void UpdateEntry(int index, string text)
        {
            if (index < 0 || index >= _entries.Count)
            {
                Service.Get<Log>().Message(LogTypes.Error, $"Bad index in RenderedTextList.UpdateEntry: {index.ToString()}");
                return;
            }

            _entries[index].Text = text;
            CalculateScrollBarMaxValue();
        }
    }
}
