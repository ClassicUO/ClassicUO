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
        private static int _lastWidth = MIN_WIDTH, _lastHeight = 300;
        private readonly GumpPicTiled _backgroundTexture;
        #endregion
        public ResizableJournal() : base(_lastWidth, _lastHeight, MIN_WIDTH, MIN_HEIGHT, 0, 0)
        {
            AnchorType = ProfileManager.CurrentProfile.JournalAnchorEnabled ? ANCHOR_TYPE.NONE : ANCHOR_TYPE.DISABLED;
            CanMove = true;
            _prevCanMove = true;
            AcceptMouseInput = true;
            WantUpdateSize = true;
            CanCloseWithRightClick = true;
            _prevCloseWithRightClick = true;
            if (ProfileManager.CurrentProfile != null)
            {
                _lastX = ProfileManager.CurrentProfile.JournalPosition.X;
                _lastY = ProfileManager.CurrentProfile.JournalPosition.Y;
                IsLocked = ProfileManager.CurrentProfile.JournalLocked;
            }
            X = _lastX;
            Y = _lastY;


            #region Background
            _background = new AlphaBlendControl((float)ProfileManager.CurrentProfile.JournalOpacity / 100);
            _background.Hue = ProfileManager.CurrentProfile.AltJournalBackgroundHue;
            _background.Width = Width - (BORDER_WIDTH * 2);
            _background.Height = Height - (BORDER_WIDTH * 2);
            _background.X = BORDER_WIDTH;
            _background.Y = BORDER_WIDTH;
            _background.CanCloseWithRightClick = true;
            _background.DragBegin += (sender, e) =>
            {
                InvokeDragBegin(e.Location);
            };

            _backgroundTexture = new GumpPicTiled(0);
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
            Add(_backgroundTexture);
            Add(_scrollBarBase);

            Add(_journalArea);

            Add(_newTabButton = new NiceButton(0, 0, 20, TAB_HEIGHT, ButtonAction.Activate, "+") { IsSelectable = false });
            _newTabButton.SetTooltip("Add a new tab");
            _newTabButton.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    UIManager.Add(new InputRequest("Enter a tab name", "Save", "Cancel", (r, entry) =>
                    {
                        if (r == InputRequest.Result.BUTTON1 && !string.IsNullOrEmpty(entry))
                        {
                            ProfileManager.CurrentProfile.JournalTabs.Add(entry, new MessageType[] { MessageType.Regular });
                            ResizableJournal.ReloadTabs = true;
                        }
                    })
                    { X = X, Y = Y });
                }
            };

            BuildTabs();

            InitJournalEntries();
            ResizeWindow(ProfileManager.CurrentProfile.ResizeJournalSize);
            BuildBorder();
            EventSink.JournalEntryAdded += EventSink_EntryAdded; ;
        }

        private void EventSink_EntryAdded(object sender, JournalEntry e)
        {
            AddJournalEntry(e);
        }

        public override GumpType GumpType => GumpType.Journal;

        public enum BorderStyle
        {
            Default,
            Style1,
            Style2,
            Style3,
            Style4,
            Style5,
            Style6,
            Style7,
            Style8,
            //Style9
        }

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
            if (ProfileManager.CurrentProfile.LastJournalTab < _tab.Count)
            {
                _tab[ProfileManager.CurrentProfile.LastJournalTab].IsSelected = true;
                OnButtonClick(ProfileManager.CurrentProfile.LastJournalTab); //Simulate selecting a tab
            }

            for (int i = 0; i < _tab.Count; i++)
                Add(_tab[i]);

            _newTabButton.X = (_tab.Count * TAB_WIDTH) + 4;
        }

        public void BuildBorder()
        {
            int graphic = 0, borderSize = 0;
            switch ((BorderStyle)ProfileManager.CurrentProfile.JournalStyle)
            {
                case BorderStyle.Style1:
                    graphic = 3500; borderSize = 26;
                    break;
                case BorderStyle.Style2:
                    graphic = 5054; borderSize = 12;
                    break;
                case BorderStyle.Style3:
                    graphic = 5120; borderSize = 10;
                    break;
                case BorderStyle.Style4:
                    graphic = 9200; borderSize = 7;
                    break;
                case BorderStyle.Style5:
                    graphic = 9270; borderSize = 10;
                    break;
                case BorderStyle.Style6:
                    graphic = 9300; borderSize = 4;
                    break;
                case BorderStyle.Style7:
                    graphic = 9260; borderSize = 17;
                    break;
                case BorderStyle.Style8:
                    {
                        if (Client.Game.Gumps.GetGump(40303).Texture != null)
                            graphic = 40303;
                        else
                            graphic = 83;
                        borderSize = 16;
                        break;
                    }
                //case BorderStyle.Style9:
                //    {
                //        if (Assets.GumpsLoader.Instance.GetGumpTexture(40313, out var bounds) != null)
                //        {
                //            graphic = 40313;
                //            borderSize = 75;
                //        }
                //        else
                //        {
                //            graphic = 83;
                //            borderSize = 16;
                //        }
                //        break;
                //    }

                default:
                case BorderStyle.Default:
                    BorderControl.DefaultGraphics();
                    _backgroundTexture.IsVisible = false;
                    _background.IsVisible = true;
                    BORDER_WIDTH = 4;
                    break;
            }

            if ((BorderStyle)ProfileManager.CurrentProfile.JournalStyle != BorderStyle.Default)
            {
                BorderControl.T_Left = (ushort)graphic;
                BorderControl.H_Border = (ushort)(graphic + 1);
                BorderControl.T_Right = (ushort)(graphic + 2);
                BorderControl.V_Border = (ushort)(graphic + 3);

                _backgroundTexture.Graphic = (ushort)(graphic + 4);
                _backgroundTexture.IsVisible = true;
                _backgroundTexture.Hue = _background.Hue;
                BorderControl.Hue = _background.Hue;
                BorderControl.Alpha = (float)ProfileManager.CurrentProfile.JournalOpacity / 100;
                _background.IsVisible = false;

                BorderControl.V_Right_Border = (ushort)(graphic + 5);
                BorderControl.B_Left = (ushort)(graphic + 6);
                BorderControl.H_Bottom_Border = (ushort)(graphic + 7);
                BorderControl.B_Right = (ushort)(graphic + 8);
                BorderControl.BorderSize = borderSize;
                BORDER_WIDTH = borderSize;
            }
            Reposition();

            if (ProfileManager.CurrentProfile.HideJournalBorder)
                BorderControl.IsVisible = false;
            else
                BorderControl.IsVisible = true;
        }

        private void Reposition()
        {
            if (IsDisposed)
                return;
            _background.X = BORDER_WIDTH;
            _background.Y = BORDER_WIDTH;
            _background.Width = Width - (BORDER_WIDTH * 2);
            _background.Height = Height - (BORDER_WIDTH * 2);

            _backgroundTexture.X = _background.X;
            _backgroundTexture.Y = _background.Y;
            _backgroundTexture.Width = _background.Width;
            _backgroundTexture.Height = _background.Height;
            _backgroundTexture.Alpha = (float)ProfileManager.CurrentProfile.JournalOpacity / 100;
            BorderControl.Alpha = (float)ProfileManager.CurrentProfile.JournalOpacity / 100;

            _journalArea.X = BORDER_WIDTH;
            _journalArea.Y = TAB_HEIGHT;
            _journalArea.Width = Width - SCROLL_BAR_WIDTH - (BORDER_WIDTH * 2);
            _journalArea.Height = Height - BORDER_WIDTH - TAB_HEIGHT;

            _lastWidth = Width;
            _lastHeight = Height;

            _scrollBarBase.X = Width - SCROLL_BAR_WIDTH - BORDER_WIDTH;
            _scrollBarBase.Y = _journalArea.Y;
            _scrollBarBase.Height = Height - BORDER_WIDTH - TAB_HEIGHT;
            ProfileManager.CurrentProfile.ResizeJournalSize = new Point(Width, Height);
        }

        public void UpdateOptions()
        {
            _backgroundTexture.Alpha = (float)ProfileManager.CurrentProfile.JournalOpacity / 100;
            BorderControl.Alpha = (float)ProfileManager.CurrentProfile.JournalOpacity / 100;
            _background.Hue = ProfileManager.CurrentProfile.AltJournalBackgroundHue;
            AnchorType = ProfileManager.CurrentProfile.JournalAnchorEnabled ? ANCHOR_TYPE.NONE : ANCHOR_TYPE.DISABLED;

            BuildBorder();
        }

        public static void UpdateJournalOptions()
        {
            foreach (ResizableJournal j in UIManager.Gumps.OfType<ResizableJournal>())
            {
                j.UpdateOptions();
            }
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
            BuildBorder();
        }

        protected override void OnMouseWheel(MouseEventType delta)
        {
            base.OnMouseWheel(delta);
            if (_scrollBarBase != null)
                _scrollBarBase.InvokeMouseWheel(delta);
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
                ProfileManager.CurrentProfile.LastJournalTab = buttonID;
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
                ContextMenu = new TabContextEntry(Name)
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
            _journalArea.AddEntry($"{journalEntry.Name}: {journalEntry.Text}", journalEntry.Hue, journalEntry.Time, journalEntry.TextType, journalEntry.MessageType);
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

        protected override void UpdateContents()
        {
            base.UpdateContents();
            _background.Alpha = (float)ProfileManager.CurrentProfile.JournalOpacity / 100;
        }

        public override void Update()
        {
            base.Update();

            if (IsDisposed) { return; }

            if (X != _lastX || Y != _lastY)
            {
                _lastX = X;
                _lastY = Y;
                ProfileManager.CurrentProfile.JournalPosition = Location;
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
            EventSink.JournalEntryAdded -= EventSink_EntryAdded;
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
                bool hideTimestamp = ProfileManager.CurrentProfile.HideJournalTimestamp;

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
                            if (!hideTimestamp)
                                journalEntry.TimeStamp.Draw(batcher, x, my - _scrollBar.Value);
                            journalEntry.EntryText.Draw(batcher, hideTimestamp ? x : x + (journalEntry.TimeStamp.Width + 5), my - _scrollBar.Value);
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

                    foreach (JournalData _ in journalDatas)
                    {
                        _.EntryText.Width = Width - BORDER_WIDTH - (ProfileManager.CurrentProfile.HideJournalTimestamp ? 0 : _.TimeStamp.Width);
                        _.EntryText.Update();
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

            public void AddEntry(string text, ushort hue, DateTime time, TextType text_type, MessageType messageType)
            {
                bool maxScroll = _scrollBar.Value == _scrollBar.MaxValue;

                while (journalDatas.Count > (ProfileManager.CurrentProfile == null ? 200 : ProfileManager.CurrentProfile.MaxJournalEntries))
                    journalDatas.RemoveFromFront().Destroy();

                TextBox timeS = new TextBox($"{time:t}", ProfileManager.CurrentProfile.SelectedTTFJournalFont, ProfileManager.CurrentProfile.SelectedJournalFontSize - 2, null, 1150, strokeEffect: false);

                journalDatas.AddToBack(
                    new JournalData(
                        new TextBox(text, ProfileManager.CurrentProfile.SelectedTTFJournalFont, ProfileManager.CurrentProfile.SelectedJournalFontSize, Width - (ProfileManager.CurrentProfile.HideJournalTimestamp ? 0 : timeS.Width), hue, strokeEffect: false),
                        timeS,
                        text_type,
                        messageType
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

                        if (messageType == MessageType.ChatSystem && currentfilter == MessageType.ChatSystem)
                            return true;

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
                public JournalData(TextBox textBox, TextBox timeStamp, TextType textType, MessageType messageType)
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

                public TextBox EntryText { get; }
                public TextBox TimeStamp { get; }
                public TextType TextType { get; }
                public MessageType MessageType { get; }
            }
        }

        private class TabContextEntry : ContextMenuControl
        {
            public TabContextEntry(string name)
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
                            case MessageType.ChatSystem:
                                entryName = "Global Chat";
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
                    UIManager.Add(new QuestionGump($"Delete [{name}] tab?", (yes) =>
                    {
                        if (yes)
                        {
                            if (ProfileManager.CurrentProfile.JournalTabs.ContainsKey(name))
                            {
                                ProfileManager.CurrentProfile.JournalTabs.Remove(name);
                                ResizableJournal.ReloadTabs = true;
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
