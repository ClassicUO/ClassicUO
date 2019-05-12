using System;

using ClassicUO.Game.UI.Controls;
using ClassicUO.IO;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ProfileGump : Gump
    {
        private readonly ScrollArea _scrollArea;
        private readonly MultiLineBox _textBox;
        private readonly ExpandableScroll _scrollExp;

        public ProfileGump(Serial serial, string header, string footer, string body, bool canEdit) : base(serial, serial)
        {
            Height = 300;
            CanMove = true;
            AcceptKeyboardInput = true;
            Add(_scrollExp = new ExpandableScroll(0, 0, Height));
            _scrollArea = new ScrollArea(0, 32, 272, Height - 96, false);
            Control c = new Label(header, true, 0, font: 1, maxwidth: 140)
            {
                X = 85,
                Y = 0
            };
            _scrollArea.Add(c);
            AddHorizontalBar(_scrollArea, 92, 35, 220);
            _textBox = new MultiLineBox(new MultiLineEntry(1, -1, 0, 220, true, hue: 0), canEdit)
            {
                Height = FileManager.Fonts.GetHeightUnicode(1, body, 220, IO.Resources.TEXT_ALIGN_TYPE.TS_LEFT, 0x0),
                X = 35,
                Y = 0,
                Text = body
            };
            _scrollArea.Add(_textBox);
            AddHorizontalBar(_scrollArea, 95, 35, 220);
            _scrollArea.Add(new Label(footer, true, 0, font: 1, maxwidth: 220)
            {
                X = 35,
                Y = 0,
            });
            Add(_scrollArea);
        }

        /*protected override void OnMouseWheel(MouseEvent delta)
        {
            switch (delta)
            {
                case MouseEvent.WheelScrollUp:
                    _scrollBar.Value -= 5;

                    break;
                case MouseEvent.WheelScrollDown:
                    _scrollBar.Value += 5;

                    break;
            }
        }*/


        public override void OnButtonClick(int buttonID)
        {
            // necessary to avoid closing
        }

        public override void Update(double totalMS, double frameMS)
        {
            /*WantUpdateSize = true;

            if(_textBox.Height > 0)
                _textBox.Height = Height - 150;*/
            if(!_textBox.IsDisposed && _textBox.IsChanged)
            {
                _textBox.Height = Math.Max(FileManager.Fonts.GetHeightUnicode(1, _textBox.TxEntry.Text, 220, IO.Resources.TEXT_ALIGN_TYPE.TS_LEFT, 0x0) + 20, 40);
                foreach(Control c in _scrollArea.Children)
                {
                    if (c is ScrollAreaItem)
                        c.OnPageChanged();
                }
            }

            base.Update(totalMS, frameMS);
        }

        private void AddHorizontalBar(ScrollArea area, Graphic start, int x, int width)
        {
            var startBounds = FileManager.Gumps.GetTexture(start);
            var middleBounds = FileManager.Gumps.GetTexture((Graphic)(start + 1));
            var endBounds = FileManager.Gumps.GetTexture((Graphic)(start + 2));
            int y = -startBounds.Height;
            Control c;
            c = new GumpPic(x, (y >> 1) - 6, (Graphic)start, 0);
            c.Add(new GumpPicWithWidth(startBounds.Width, ((startBounds.Height - middleBounds.Height) >> 1), (Graphic)(start + 1), 0, width - startBounds.Width - endBounds.Width));
            c.Add(new GumpPic(width - endBounds.Width, 0, (Graphic)(start + 2), 0));
            area.Add(c);
        }

        public override void OnPageChanged()
        {
            Height = _scrollExp.SpecialHeight;
            _scrollArea.Height = _scrollExp.SpecialHeight - 96;
            foreach (Control c in _scrollArea.Children)
            {
                if (c is ScrollAreaItem)
                    c.OnPageChanged();
            }
        }

        public override void OnKeyboardReturn(int textID, string text)
        {
            if ((MultiLineBox.PasteRetnCmdID & textID) != 0 && !string.IsNullOrEmpty(text)) _textBox.TxEntry.InsertString(text.Replace("\r", string.Empty));
        }
    }
}