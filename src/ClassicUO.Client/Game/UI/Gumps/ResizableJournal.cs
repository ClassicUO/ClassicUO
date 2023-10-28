using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility.Collections;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ResizableJournal : ResizableGump
    {
        #region VARS
        private static int BORDER_WIDTH = 4;
        private static int MIN_WIDTH = (BORDER_WIDTH * 2) + (TAB_WIDTH * 4);
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
        #endregion

        public static bool HasReceivedChatSystemMessage { get; set; } = false;
        private bool _hasReceivedChatSystemMessage = HasReceivedChatSystemMessage;

        #region OTHER
        private static int _lastX = 100, _lastY = 100;
        private static int _lastWidth = MIN_WIDTH, _lastHeight = 300;
        private readonly GumpPicTiled _backgroundTexture;
        #endregion
        public ResizableJournal() : base(_lastWidth, _lastHeight, MIN_WIDTH, MIN_HEIGHT, 0, 0)
        {
            AnchorType = ANCHOR_TYPE.NONE;
            CanMove = true;
            AcceptMouseInput = true;
            WantUpdateSize = true;
            CanCloseWithRightClick = true;
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

            #region Tab area
            AddTab("All", new MessageType[] {
                MessageType.Alliance, MessageType.Command, MessageType.Emote,
                MessageType.Encoded, MessageType.Focus, MessageType.Guild,
                MessageType.Label, MessageType.Limit3Spell, MessageType.Party,
                MessageType.Regular, MessageType.Spell, MessageType.System,
                MessageType.Whisper, MessageType.Yell, MessageType.ChatSystem
            });
            AddTab("Chat", new MessageType[] { MessageType.Regular, MessageType.Guild, MessageType.Alliance, MessageType.Emote, MessageType.Party, MessageType.Whisper, MessageType.Yell, MessageType.ChatSystem });
            AddTab("Guild|Party", new MessageType[] { MessageType.Guild, MessageType.Alliance, MessageType.Party });
            AddTab("System", new MessageType[] { MessageType.System });
            if (HasReceivedChatSystemMessage)
                AddTab("Global Chat", new MessageType[] { MessageType.ChatSystem });
            _tab[0].IsSelected = true;
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
            for (int i = 0; i < _tab.Count; i++)
                Add(_tab[i]);

            InitJournalEntries();
            ResizeWindow(ProfileManager.CurrentProfile.ResizeJournalSize);
            BuildBorder();
            World.Journal.EntryAdded += (sender, e) => { AddJournalEntry(e); };
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

        protected override void OnMouseWheel(MouseEventType delta)
        {
            base.OnMouseWheel(delta);
            if (_scrollBarBase != null)
                _scrollBarBase.InvokeMouseWheel(delta);
        }

        public override void OnButtonClick(int buttonID)
        {
            if (_tab.Count >= buttonID)
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
            _tab.Add(new NiceButton((_tab.Count * TAB_WIDTH) + 4, 0, TAB_WIDTH, TAB_HEIGHT, ButtonAction.Activate, Name, 1) { ButtonParameter = _tab.Count, IsSelectable = true });
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

            if (X != _lastX || Y != _lastY)
            {
                _lastX = X;
                _lastY = Y;
                ProfileManager.CurrentProfile.JournalPosition = Location;
            }
            if (((Width != _lastWidth || Height != _lastHeight) && !Mouse.LButtonPressed))
                Reposition();
            if (HasReceivedChatSystemMessage != _hasReceivedChatSystemMessage)
            {
                _hasReceivedChatSystemMessage = HasReceivedChatSystemMessage;

                if (_hasReceivedChatSystemMessage)
                {
                    AddTab("Global Chat", new MessageType[] { MessageType.ChatSystem });
                    Add(_tab[_tab.Count - 1]);
                }
            }
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
                            if(!hideTimestamp)
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
    }
}
