using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Point = Microsoft.Xna.Framework.Point;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ResizableJournal : ResizableGump
    {
        #region VARS
        public static bool ReloadTabs { get; set; } = false;

        private static int BORDER_WIDTH = 4;
        private static int MIN_WIDTH = (BORDER_WIDTH * 2) + (TAB_WIDTH * 4) + 20;
        private const int MIN_HEIGHT = 100;
        private const int SCROLL_BAR_WIDTH = 14;
        #region TABS
        private const int TAB_WIDTH = 80;
        private const int TAB_HEIGHT = 30;
        #endregion
        #endregion

        #region CONTROLS

        #region TABS
        private List<NiceButton> _tab = new List<NiceButton>();
        private List<string> _tabName = new List<string>();
        private List<MessageType[]> _tabTypes = new List<MessageType[]>();
        private MessageType[] _currentFilter;
        #endregion

        private AlphaBlendControl _background;
        private JournalEntriesContainer _journalArea;
        private ScrollBar _scrollBarBase;
        private NiceButton _newTabButton;
        #endregion

        #region OTHER
        private static int _lastX = 100, _lastY = 100;
        private static int _lastWidth = MIN_WIDTH, _lastHeight = 350;
        private World _world;
        #endregion
        public ResizableJournal(World world) : base(world, _lastWidth, _lastHeight, MIN_WIDTH, MIN_HEIGHT, 0, 0)
        {
            _world = world;
            CanMove = true;
            AcceptMouseInput = true;
            WantUpdateSize = true;
            CanCloseWithRightClick = true;
            X = _lastX;
            Y = _lastY;


            #region Background
            _background = new AlphaBlendControl(0.7f);
            _background.Hue = 0x0000;
            _background.Width = Width - (BORDER_WIDTH * 2);
            _background.Height = Height - (BORDER_WIDTH * 2);
            _background.X = BORDER_WIDTH;
            _background.Y = BORDER_WIDTH;
            _background.CanCloseWithRightClick = true;
            _background.DragBegin += (sender, e) =>
            {
                InvokeDragBegin(e.Location);
            };

            #endregion

            #region Journal Area
            _scrollBarBase = new ScrollBar(
                Width - SCROLL_BAR_WIDTH - BORDER_WIDTH,
                BORDER_WIDTH + TAB_HEIGHT,
                Height - TAB_HEIGHT - (BORDER_WIDTH * 2));

            _journalArea = new JournalEntriesContainer(
                BORDER_WIDTH,
                BORDER_WIDTH + TAB_HEIGHT,
                Width - SCROLL_BAR_WIDTH - (BORDER_WIDTH * 2),
                Height - (BORDER_WIDTH * 2) - TAB_HEIGHT,
                _scrollBarBase,
                this);
            _journalArea.CanCloseWithRightClick = true;
            _journalArea.DragBegin += (sender, e) => { InvokeDragBegin(e.Location); };
            #endregion

            Add(_background);
            Add(_scrollBarBase);

            Add(_journalArea);

            Add(_newTabButton = new NiceButton(0, 0, 20, TAB_HEIGHT, ButtonAction.Activate, "+") { IsSelectable = false });
            _newTabButton.SetTooltip("Add a new tab");
            _newTabButton.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    UIManager.Add(new EntryDialog(world, 250, 150, "Enter a tab name", (s) => {
                         ProfileManager.CurrentProfile.JournalTabs.Add(s, new MessageType[] { MessageType.Regular });
                         ReloadTabs = true;
                    }));
                }
            };

            BuildTabs();

            InitJournalEntries();

            world.Journal.EntryAdded += EventSink_EntryAdded;
        }

        private void EventSink_EntryAdded(object sender, JournalEntry e)
        {
            AddJournalEntry(e);
        }

        public override GumpType GumpType => GumpType.Journal;

        private void BuildTabs()
        {
            foreach (var tab in _tab)
            {
                tab.Dispose();
            }

            _tab.Clear();
            _tabName.Clear();
            _tabTypes.Clear();

            foreach (var tab in ProfileManager.CurrentProfile.JournalTabs)
            {
                AddTab(tab.Key, tab.Value);
            }

            for (int i = 0; i < _tab.Count; i++)
                Add(_tab[i]);

            _newTabButton.X = (_tab.Count * TAB_WIDTH) + 4;

            MIN_WIDTH = (BORDER_WIDTH * 2) + (TAB_WIDTH * _tab.Count) + 20;
        }

        private void Reposition()
        {
            if (IsDisposed)
                return;
            _background.X = BORDER_WIDTH;
            _background.Y = BORDER_WIDTH;
            _background.Width = Width - (BORDER_WIDTH * 2);
            _background.Height = Height - (BORDER_WIDTH * 2);

            _journalArea.X = BORDER_WIDTH;
            _journalArea.Y = TAB_HEIGHT;
            _journalArea.Width = Width - SCROLL_BAR_WIDTH - (BORDER_WIDTH * 2);
            _journalArea.Height = Height - BORDER_WIDTH - TAB_HEIGHT;

            _lastWidth = Width;
            _lastHeight = Height;

            _scrollBarBase.X = Width - SCROLL_BAR_WIDTH - BORDER_WIDTH;
            _scrollBarBase.Y = _journalArea.Y;
            _scrollBarBase.Height = Height - BORDER_WIDTH - TAB_HEIGHT;
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            writer.WriteAttributeString("rw", Width.ToString());
            writer.WriteAttributeString("rh", Height.ToString());

            int c = 0;
            foreach (var tab in _tab)
            {
                if (tab.IsSelected)
                {
                    writer.WriteAttributeString("tab", c.ToString());
                    break;
                }
                c++;
            }
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            Point savedSize = new Microsoft.Xna.Framework.Point(Width, Height);

            if (int.TryParse(xml.GetAttribute("rw"), out int width) && width > 0)
            {
                savedSize.X = width;
            }
            if (int.TryParse(xml.GetAttribute("rh"), out int height) && height > 0)
            {
                savedSize.Y = height;
            }
            if (int.TryParse(xml.GetAttribute("tab"), out int tab))
            {
                OnButtonClick(tab); //Simulate selecting a tab
            }

            ResizeWindow(savedSize);
        }

        protected override void OnMouseWheel(MouseEventType delta)
        {
            base.OnMouseWheel(delta);
            _scrollBarBase?.InvokeMouseWheel(delta);
        }

        public override void OnButtonClick(int buttonID)
        {
            if (_tab.Count > buttonID)
            {
                _tab[buttonID].IsSelected = true;
                _currentFilter = _tabTypes[buttonID];
                _journalArea.CalculateScrollBarMaxValue();
                _journalArea.Update();
                _scrollBarBase.Value = _scrollBarBase.MaxValue;
            }
        }

        private void AddTab(string Name, MessageType[] filters)
        {
            NiceButton nb;
            _tab.Add(nb = new NiceButton((_tab.Count * TAB_WIDTH) + 4, 0, TAB_WIDTH, TAB_HEIGHT, ButtonAction.Activate, Name, 1)
            {
                ButtonParameter = _tab.Count,
                IsSelectable = true,
                CanCloseWithRightClick = false,
                ContextMenu = new TabContextEntry(_world, this, Name)
            });

            nb.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Right)
                {
                    nb.ContextMenu.Show();
                }
            };
            _tabName.Add(Name);
            _tabTypes.Add(filters);
        }

        private void AddJournalEntry(JournalEntry journalEntry)
        {
            if (journalEntry == null)
                return;

            if (!string.IsNullOrEmpty(journalEntry.Name) && _world.IgnoreManager.IgnoredCharsList.Contains(journalEntry.Name))
                return;

            _journalArea.AddEntry(journalEntry);
        }

        private void InitJournalEntries()
        {
            foreach (JournalEntry entry in JournalManager.Entries)
            {
                if (entry == null)
                    continue;
                AddJournalEntry(entry);
            }
        }

        public override void Update()
        {
            base.Update();

            if (IsDisposed) return;

            if (X != _lastX || Y != _lastY)
            {
                _lastX = X;
                _lastY = Y;

            }
            if (((Width != _lastWidth || Height != _lastHeight) && !Mouse.LButtonPressed))
                Reposition();

            if (ReloadTabs)
            {
                ReloadTabs = false;
                BuildTabs();
            }
        }

        public override void Dispose()
        {
            _world.Journal.EntryAdded -= EventSink_EntryAdded;
            base.Dispose();
        }

        private class JournalEntriesContainer : Control
        {
            private Deque<JournalData> journalDatas = new Deque<JournalData>();


            private readonly ScrollBarBase _scrollBar;
            private int lastWidth = 0, lastHeight = 0;
            private ResizableJournal _resizableJournal;

            public JournalEntriesContainer(int x, int y, int width, int height, ScrollBarBase scrollBarControl, ResizableJournal resizableJournal)
            {
                _resizableJournal = resizableJournal;
                _scrollBar = scrollBarControl;
                _scrollBar.IsVisible = false;
                AcceptMouseInput = true;
                CanMove = true;

                X = x;
                Y = y;
                Width = lastWidth = width;
                Height = lastHeight = height;

                WantUpdateSize = false;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                base.Draw(batcher, x, y);
                int my = y;

                if (batcher.ClipBegin(x, y, Width, Height))
                {
                    foreach (JournalData journalEntry in journalDatas)
                    {
                        if (journalEntry == null || string.IsNullOrEmpty(journalEntry.EntryText.Text))
                            continue;

                        if (!CanBeDrawn(journalEntry.TextType, journalEntry.MessageType))
                            continue;

                        if (my + journalEntry.EntryText.Height - y >= _scrollBar.Value && my - y <= _scrollBar.Value + _scrollBar.Height)
                        {
                            journalEntry.TimeStamp.Draw(batcher, x, my - _scrollBar.Value);
                            journalEntry.EntryText.Draw(batcher, x + (journalEntry.TimeStamp.Width + 5), my - _scrollBar.Value);
                        }
                        my += journalEntry.EntryText.Height;
                    }

                    batcher.ClipEnd();
                }
                return true;
            }

            public override void Update()
            {
                base.Update();

                if (!IsVisible)
                    return;

                _scrollBar.IsVisible = _scrollBar.MaxValue > _scrollBar.MinValue;
                if (Width != lastWidth || Height != lastHeight)
                {
                    lastWidth = Width;
                    lastHeight = Height;

                    foreach (JournalData jdata in journalDatas)
                    {
                        jdata.EntryText = new Label(jdata.EntryText.Text, jdata.EntryText.Unicode, jdata.EntryText.Hue, Width - BORDER_WIDTH - jdata.TimeStamp.Width, font: jdata.EntryText.Font);
                        jdata.EntryText.Update();
                    }

                    CalculateScrollBarMaxValue();
                }

            }

            public void CalculateScrollBarMaxValue()
            {
                bool maxValue = _scrollBar.Value == _scrollBar.MaxValue;
                int height = 0;

                foreach (JournalData _ in journalDatas)
                {
                    if (_ != null)
                        if (CanBeDrawn(_.TextType, _.MessageType))
                            height += _.EntryText.Height;
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

            public void AddEntry(JournalEntry e)
            {
                bool maxScroll = _scrollBar.Value == _scrollBar.MaxValue;

                while (journalDatas.Count > Constants.MAX_JOURNAL_HISTORY_COUNT)
                    journalDatas.RemoveFromFront().Destroy();

                Label timeS = new Label($"{e.Time:t}", e.IsUnicode, e.Hue, font: e.Font);

                journalDatas.AddToBack(
                    new JournalData(
                        new Label($"{e.Name}: {e.Text}", e.IsUnicode, e.Hue, Width - BORDER_WIDTH - timeS.Width, font: e.Font),
                        timeS,
                        e.TextType,
                        e.MessageType
                    ));

                if (maxScroll)
                {
                    _scrollBar.Value = _scrollBar.MaxValue;
                }
                CalculateScrollBarMaxValue();
            }

            private bool CanBeDrawn(TextType type, MessageType messageType)
            {
                if (_resizableJournal._currentFilter != null)
                {
                    for (int i = 0; i < _resizableJournal._currentFilter.Length; i++)
                    {
                        MessageType currentfilter = _resizableJournal._currentFilter[i];

                        if (type == TextType.SYSTEM && currentfilter == MessageType.System)
                            return true;

                        if (type == TextType.SYSTEM && currentfilter != MessageType.System)
                            continue;

                        if (currentfilter == messageType)
                            return true;
                    }
                    return false;
                }

                return true;
            }

            private void Reset()
            {
                foreach (JournalData _ in journalDatas)
                    _.Destroy();

                journalDatas.Clear();
            }

            public override void Dispose()
            {
                Reset();

                base.Dispose();
            }

            public class JournalData
            {
                public JournalData(Label textBox, Label timeStamp, TextType textType, MessageType messageType)
                {
                    EntryText = textBox;
                    TimeStamp = timeStamp;
                    TextType = textType;
                    MessageType = messageType;
                }

                public void Destroy()
                {
                    EntryText?.Dispose();
                    TimeStamp?.Dispose();
                }

                public Label EntryText { get; set; }
                public Label TimeStamp { get; }
                public TextType TextType { get; }
                public MessageType MessageType { get; }
            }
        }

        private class TabContextEntry : ContextMenuControl
        {
            public TabContextEntry(World world, Gump parent, string name) : base(parent)
            {
                if (ProfileManager.CurrentProfile.JournalTabs.ContainsKey(name))
                {
                    MessageType[] selectedTypes = ProfileManager.CurrentProfile.JournalTabs[name];

                    foreach (MessageType item in Enum.GetValues(typeof(MessageType)))
                    {
                        string entryName = string.Empty;
                        switch (item)
                        {
                            case MessageType.Regular:
                                entryName = "Regular";
                                break;
                            case MessageType.System:
                                entryName = "System";
                                break;
                            case MessageType.Emote:
                                entryName = "Emote";
                                break;
                            case MessageType.Limit3Spell:
                                entryName = "Limit3Spell(Sphere)";
                                break;
                            case MessageType.Label:
                                entryName = "Label";
                                break;
                            case MessageType.Focus:
                                entryName = "Focus";
                                break;
                            case MessageType.Whisper:
                                entryName = "Whisper";
                                break;
                            case MessageType.Yell:
                                entryName = "Yell";
                                break;
                            case MessageType.Spell:
                                entryName = "Spell";
                                break;
                            case MessageType.Guild:
                                entryName = "Guild";
                                break;
                            case MessageType.Alliance:
                                entryName = "Alliance";
                                break;
                            case MessageType.Command:
                                entryName = "Command";
                                break;
                            case MessageType.Encoded:
                                entryName = "Encoded";
                                break;
                            case MessageType.Party:
                                entryName = "Party";
                                break;
                        }

                        Add(entryName,
                            () =>
                            {
                                if (ProfileManager.CurrentProfile.JournalTabs.ContainsKey(name))
                                {
                                    MessageType[] selectedTypes = ProfileManager.CurrentProfile.JournalTabs[name];

                                    if (selectedTypes.Contains(item))
                                    {
                                        ProfileManager.CurrentProfile.JournalTabs[name] = RemoveType(selectedTypes, item);
                                        ResizableJournal.ReloadTabs = true;
                                    }
                                    else
                                    {
                                        ProfileManager.CurrentProfile.JournalTabs[name] = AddType(selectedTypes, item);
                                        ResizableJournal.ReloadTabs = true;
                                    }
                                }
                            },
                            true,
                            selectedTypes.Contains(item));
                    }
                }

                Add("X Delete Tab", () =>
                {
                    UIManager.Add(new QuestionGump(world, $"Delete [{name}] tab?", (yes) =>
                    {
                        if (yes)
                        {
                            if (ProfileManager.CurrentProfile.JournalTabs.ContainsKey(name))
                            {
                                ProfileManager.CurrentProfile.JournalTabs.Remove(name);
                                ReloadTabs = true;
                            }
                        }
                    }));
                });
            }

            private static MessageType[] RemoveType(MessageType[] array, MessageType removeMe)
            {
                var modifiedList = new List<MessageType>();
                foreach (var item in array)
                {
                    if (item != removeMe)
                    {
                        modifiedList.Add(item);
                    }
                }

                return modifiedList.ToArray();
            }

            private static MessageType[] AddType(MessageType[] array, MessageType addMe)
            {
                var modifiedList = new List<MessageType>();

                modifiedList.AddRange(array);

                modifiedList.Add(addMe);

                return modifiedList.ToArray();
            }
        }
    }
}