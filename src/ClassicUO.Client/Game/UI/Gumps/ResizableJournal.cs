using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility.Collections;
using System;
using System.Drawing;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ResizableJournal : ResizableGump
    {
        #region CONSTANTS
        private const int MIN_WIDTH = 350;
        private const int MIN_HEIGHT = 350;
        private const int LINE_SPACING = 4;
        private const int BORDER_WIDTH = 5;
        private const int SCROLL_BAR_WIDTH = 15;
        #endregion

        #region CONTROLS
        private AlphaBlendControl _background;
        private RenderedTextList _journalArea;
        private ScrollBar _scrollBarBase;
        #endregion

        #region OTHER
        private static int _lastX = 100, _lastY = 100;
        private static int _lastWidth = MIN_WIDTH, _lastHeight = MIN_HEIGHT;
        #endregion
        public ResizableJournal() : base(_lastWidth, _lastHeight, MIN_WIDTH, MIN_HEIGHT, 0, 0)
        {
            X = _lastX;
            Y = _lastY;

            #region Background
            _background = new AlphaBlendControl();
            _background.Width = Width;
            _background.Height = Height;
            Add(_background);
            #endregion

            #region Journal Area
            _scrollBarBase = new ScrollBar(Width - SCROLL_BAR_WIDTH -BORDER_WIDTH, 0, Height);
            Add(_scrollBarBase);
            _journalArea = new RenderedTextList(BORDER_WIDTH, BORDER_WIDTH, Width - SCROLL_BAR_WIDTH - (BORDER_WIDTH * 2), Height - (BORDER_WIDTH * 2), _scrollBarBase);
            Add(_journalArea);
            #endregion

            InitJournalEntries();
            World.Journal.EntryAdded += (sender, e) => { AddJournalEntry(e); };
        }

        public override GumpType GumpType => GumpType.Journal;

        private void AddJournalEntry(JournalEntry journalEntry)
        {
            if (journalEntry == null)
                return;
            _journalArea.AddEntry($"{journalEntry.Name}: {journalEntry.Text}", journalEntry.Font, journalEntry.Hue, journalEntry.IsUnicode, journalEntry.Time, journalEntry.TextType);
        }

        private void InitJournalEntries()
        {
            foreach (JournalEntry entry in JournalManager.Entries)
            {
                AddJournalEntry(entry);
            }
        }

        public override void Update()
        {
            base.Update();

            if (X != _lastX) _lastX = X;
            if (Y != _lastY) _lastY = Y;
            if (Width != _lastWidth)
            {
                _lastWidth = Width;
                _background.Width = Width;
                _scrollBarBase.X = Width - SCROLL_BAR_WIDTH - BORDER_WIDTH;
                _journalArea.Width = Width - SCROLL_BAR_WIDTH - (BORDER_WIDTH*2);
            }
            if (Height != _lastHeight)
            {
                _lastHeight = Height;
                _background.Height = Height;
                _journalArea.Height = Height - (BORDER_WIDTH * 2);
                _scrollBarBase.Height = Height - (BORDER_WIDTH * 2);
            }
        }

        private class RenderedTextList : Control
        {
            private readonly Deque<RenderedText> _entries, _hours;
            private readonly ScrollBarBase _scrollBar;
            private readonly Deque<TextType> _text_types;
            private int lastWidth = 0, lastHeight = 0;

            public RenderedTextList(int x, int y, int width, int height, ScrollBarBase scrollBarControl)
            {
                _scrollBar = scrollBarControl;
                _scrollBar.IsVisible = false;
                AcceptMouseInput = true;
                CanMove = true;
                X = x;
                Y = y;
                Width = lastWidth = width;
                Height = lastHeight = height;

                _entries = new Deque<RenderedText>();
                _hours = new Deque<RenderedText>();
                _text_types = new Deque<TextType>();

                WantUpdateSize = false;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                base.Draw(batcher, x, y);

                int mx = x;
                int my = y;

                int height = 0;
                int maxheight = _scrollBar.Value + _scrollBar.Height;
                Console.WriteLine("TEST DRAW");
                for (int i = 0; i < _entries.Count; i++)
                {
                    RenderedText t = _entries[i];
                    RenderedText hour = _hours[i];

                    TextType type = _text_types[i];


                    if (!CanBeDrawn(type))
                    {
                        continue;
                    }


                    if (height + t.Height <= _scrollBar.Value)
                    {
                        // this entry is above the renderable area.
                        height += t.Height;
                    }
                    else if (height + t.Height <= maxheight)
                    {
                        int yy = height - _scrollBar.Value;

                        if (yy < 0)
                        {
                            // this entry starts above the renderable area, but exists partially within it.
                            hour.Draw
                            (
                                batcher,
                                hour.Width,
                                hour.Height,
                                mx,
                                y,
                                t.Width,
                                t.Height + yy,
                                0,
                                -yy
                            );

                            t.Draw
                            (
                                batcher,
                                t.Width,
                                t.Height,
                                mx + hour.Width,
                                y,
                                t.Width,
                                t.Height + yy,
                                0,
                                -yy
                            );

                            my += t.Height + yy;
                        }
                        else
                        {
                            // this entry is completely within the renderable area.
                            hour.Draw(batcher, mx, my);
                            t.Draw(batcher, mx + hour.Width, my);
                            my += t.Height;
                        }

                        height += t.Height;
                    }
                    else
                    {
                        int yyy = maxheight - height;

                        hour.Draw
                        (
                            batcher,
                            hour.Width,
                            hour.Height,
                            mx,
                            y + _scrollBar.Height - yyy,
                            t.Width,
                            yyy,
                            0,
                            0
                        );

                        t.Draw
                        (
                            batcher,
                            t.Width,
                            t.Height,
                            mx + hour.Width,
                            y + _scrollBar.Height - yyy,
                            t.Width,
                            yyy,
                            0,
                            0
                        );

                        // can't fit any more entries - so we break!
                        break;
                    }
                }

                return true;
            }

            public override void Update()
            {
                base.Update();

                if (!IsVisible)
                {
                    return;
                }
                if(Width != lastWidth || Height != lastHeight)
                {
                    lastWidth = Width;
                    lastHeight = Height;
                    _scrollBar.IsVisible = true;
                    CalculateScrollBarMaxValue();
                }

            }

            private void CalculateScrollBarMaxValue()
            {
                bool maxValue = _scrollBar.Value == _scrollBar.MaxValue;
                int height = 0;

                for (int i = 0; i < _entries.Count; i++)
                {
                    if (i < _text_types.Count && CanBeDrawn(_text_types[i]))
                    {
                        height += _entries[i].Height;
                    }
                }

                height -= _scrollBar.Height;

                if (height > 0)
                {
                    _scrollBar.MaxValue = height;

                    if (maxValue)
                    {
                        _scrollBar.Value = _scrollBar.MaxValue;
                    }
                }
                else
                {
                    _scrollBar.MaxValue = 0;
                    _scrollBar.Value = 0;
                }
            }

            public void AddEntry
            (
                string text,
                int font,
                ushort hue,
                bool isUnicode,
                DateTime time,
                TextType text_type
            )
            {
                bool maxScroll = _scrollBar.Value == _scrollBar.MaxValue;

                while (_entries.Count > 199)
                {
                    _entries.RemoveFromFront().Destroy();

                    _hours.RemoveFromFront().Destroy();

                    _text_types.RemoveFromFront();
                }

                RenderedText h = RenderedText.Create
                (
                    $"{time:t} ",
                    1150,
                    1,
                    true,
                    FontStyle.BlackBorder
                );

                _hours.AddToBack(h);

                RenderedText rtext = RenderedText.Create
                (
                    text,
                    hue,
                    (byte)font,
                    isUnicode,
                    FontStyle.Indention | FontStyle.BlackBorder,
                    maxWidth: Width - (18 + h.Width)
                );

                _entries.AddToBack(rtext);

                _text_types.AddToBack(text_type);

                _scrollBar.MaxValue += rtext.Height;

                if (maxScroll)
                {
                    _scrollBar.Value = _scrollBar.MaxValue;
                }
            }

            private static bool CanBeDrawn(TextType type)
            {
                if (type == TextType.CLIENT && !ProfileManager.CurrentProfile.ShowJournalClient)
                {
                    return false;
                }

                if (type == TextType.SYSTEM && !ProfileManager.CurrentProfile.ShowJournalSystem)
                {
                    return false;
                }

                if (type == TextType.OBJECT && !ProfileManager.CurrentProfile.ShowJournalObjects)
                {
                    return false;
                }

                if (type == TextType.GUILD_ALLY && !ProfileManager.CurrentProfile.ShowJournalGuildAlly)
                {
                    return false;
                }

                return true;
            }

            public override void Dispose()
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    _entries[i].Destroy();

                    _hours[i].Destroy();
                }

                _entries.Clear();
                _hours.Clear();
                _text_types.Clear();

                base.Dispose();
            }
        }
    }
}
