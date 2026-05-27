// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.IO;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility.Collections;

namespace ClassicUO.Game.UI.Gumps
{
    internal class JournalGump : Gump
    {
        private const int _diffY = 22;
        private readonly ExpandableScroll _background;
        private readonly Checkbox[] _filters_chekboxes = new Checkbox[4];
        private readonly GumpPic _gumpPic;
        private readonly HitBox _hitBox;
        private bool _isMinimized;
        private readonly RenderedTextList _journalEntries;
        private readonly ScrollFlag _scrollBar;

        public JournalGump(World world) : base(world, 0, 0)
        {
            Height = 300;
            CanMove = true;
            CanCloseWithRightClick = true;
            Add(_gumpPic = new GumpPic(160, 0, 0x82D, 0));

            Add
            (
                _background = new ExpandableScroll(0, _diffY, Height - _diffY, 0x1F40)
                {
                    TitleGumpID = 0x82A
                }
            );

            const ushort DARK_MODE_JOURNAL_HUE = 903;

            string str = ResGumps.DarkMode;
            int width = Client.Game.UO.FileManager.Fonts.GetWidthASCII(6, str);

            Checkbox darkMode;

            Add
            (
                darkMode = new Checkbox
                (
                    0x00D2,
                    0x00D3,
                    str,
                    6,
                    0x0288,
                    false
                )
                {
                    X = _background.Width - width - 2,
                    Y = _diffY + 7,
                    IsChecked = ProfileManager.CurrentProfile.JournalDarkMode
                }
            );

            Hue = (ushort) (ProfileManager.CurrentProfile.JournalDarkMode ? DARK_MODE_JOURNAL_HUE : 0);

            darkMode.ValueChanged += (sender, e) =>
            {
                bool ok = ProfileManager.CurrentProfile.JournalDarkMode = !ProfileManager.CurrentProfile.JournalDarkMode;
                Hue = (ushort) (ok ? DARK_MODE_JOURNAL_HUE : 0);
            };

            _scrollBar = new ScrollFlag(-25, _diffY + 36, Height - _diffY, true);

            Add
            (
                _journalEntries = new RenderedTextList
                (
                    25,
                    _diffY + 36,
                    _background.Width - (_scrollBar.Width >> 1) - 5,
                    200,
                    _scrollBar
                )
            );

            Add(_scrollBar);

            Add(_hitBox = new HitBox(160, 0, 23, 24));
            _hitBox.MouseUp += _hitBox_MouseUp;
            _gumpPic.MouseDoubleClick += _gumpPic_MouseDoubleClick;

            int cx = 43;   // 63
            int dist = 75; // 85
            byte font = 6; // 1

            _filters_chekboxes[0] = new Checkbox
            (
                0x00D2,
                0x00D3,
                "System",
                font,
                0x0386,
                false
            )
            {
                X = cx,
                LocalSerial = 1,
                IsChecked = ProfileManager.CurrentProfile.ShowJournalSystem
            };

            _filters_chekboxes[1] = new Checkbox
            (
                0x00D2,
                0x00D3,
                "Objects",
                font,
                0x0386,
                false
            )
            {
                X = cx + dist,
                LocalSerial = 2,
                IsChecked = ProfileManager.CurrentProfile.ShowJournalObjects
            };

            _filters_chekboxes[2] = new Checkbox
            (
                0x00D2,
                0x00D3,
                "Client",
                font,
                0x0386,
                false
            )
            {
                X = cx + dist * 2,
                LocalSerial = 0,
                IsChecked = ProfileManager.CurrentProfile.ShowJournalClient
            };

            _filters_chekboxes[3] = new Checkbox
            (
                0x00D2,
                0x00D3,
                "Guild",
                font,
                0x0386,
                false
            )
            {
                X = cx + dist * 3,
                LocalSerial = 3,
                IsChecked = ProfileManager.CurrentProfile.ShowJournalGuildAlly
            };

            void on_check_box(object sender, EventArgs e)
            {
                Checkbox c = (Checkbox) sender;

                if (c != null)
                {
                    switch ((TextType) c.LocalSerial)
                    {
                        case TextType.CLIENT:
                            ProfileManager.CurrentProfile.ShowJournalClient = c.IsChecked;

                            break;

                        case TextType.SYSTEM:
                            ProfileManager.CurrentProfile.ShowJournalSystem = c.IsChecked;

                            break;

                        case TextType.OBJECT:
                            ProfileManager.CurrentProfile.ShowJournalObjects = c.IsChecked;

                            break;

                        case TextType.GUILD_ALLY:
                            ProfileManager.CurrentProfile.ShowJournalGuildAlly = c.IsChecked;

                            break;
                    }
                }
            }

            for (int i = 0; i < _filters_chekboxes.Length; i++)
            {
                _filters_chekboxes[i].ValueChanged += on_check_box;

                Add(_filters_chekboxes[i]);
            }

            InitializeJournalEntries();
            World.Journal.EntryAdded += AddJournalEntry;
        }

        public override GumpType GumpType => GumpType.Journal;

        public ushort Hue
        {
            get => _background.Hue;
            set => _background.Hue = value;
        }

        public bool IsMinimized
        {
            get => _isMinimized;
            set
            {
                if (_isMinimized != value)
                {
                    _isMinimized = value;

                    _gumpPic.Graphic = value ? (ushort) 0x830 : (ushort) 0x82D;

                    if (value)
                    {
                        _gumpPic.X = 0;
                    }
                    else
                    {
                        _gumpPic.X = 160;
                    }

                    foreach (Control c in Children)
                    {
                        c.IsVisible = !value;
                    }

                    _gumpPic.IsVisible = true;
                    WantUpdateSize = true;
                }
            }
        }

        protected override void OnMouseWheel(MouseEventType delta)
        {
            _scrollBar.InvokeMouseWheel(delta);
        }


        public override void Dispose()
        {
            World.Journal.EntryAdded -= AddJournalEntry;
            base.Dispose();
        }

        public override void Update()
        {
            base.Update();

            WantUpdateSize = true;
            _journalEntries.Height = Height - (98 + _diffY);

            for (int i = 0; i < _filters_chekboxes.Length; i++)
            {
                _filters_chekboxes[i].Y = _background.Height - _filters_chekboxes[i].Height - _diffY + 10;
            }
        }

        private void AddJournalEntry(object sender, JournalEntry entry)
        {
            var usrSend = entry.Name != string.Empty ? $"{entry.Name}" : string.Empty;

            // Check if ignored person
            if (!string.IsNullOrEmpty(usrSend) && World.IgnoreManager.IgnoredCharsList.Contains(usrSend))
                return;

            string text = $"{usrSend}: {entry.Text}";

            if (string.IsNullOrEmpty(usrSend))
            {
                text = entry.Text;
            }

            _journalEntries.AddEntry
            (
                text,
                entry.Font,
                entry.Hue,
                entry.IsUnicode,
                entry.Time,
                entry.TextType
            );
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            writer.WriteAttributeString("height", _background.SpecialHeight.ToString());
            writer.WriteAttributeString("isminimized", IsMinimized.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            _background.Height = _background.SpecialHeight = int.Parse(xml.GetAttribute("height"));
            IsMinimized = bool.Parse(xml.GetAttribute("isminimized"));
        }

        private void InitializeJournalEntries()
        {
            foreach (JournalEntry t in JournalManager.Entries)
            {
                AddJournalEntry(null, t);
            }

            _scrollBar.MinValue = 0;
        }

        private void _gumpPic_MouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
        {
            if (e.Button == MouseButtonType.Left && IsMinimized)
            {
                IsMinimized = false;
                e.Result = true;
            }
        }

        private void _hitBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtonType.Left && !IsMinimized)
            {
                IsMinimized = true;
            }
        }


        private class RenderedTextList : Control
        {
            private readonly Deque<RenderedText> _entries, _hours;
            private readonly ScrollBarBase _scrollBar;
            private readonly Deque<TextType> _text_types;

            public RenderedTextList(int x, int y, int width, int height, ScrollBarBase scrollBarControl)
            {
                _scrollBar = scrollBarControl;
                _scrollBar.IsVisible = false;
                AcceptMouseInput = true;
                CanMove = true;
                X = x;
                Y = y;
                Width = width;
                Height = height;

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

                _scrollBar.X = X + Width - (_scrollBar.Width >> 1) + 5;
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
                    (byte) font,
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