#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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

using System;
using System.Collections.Generic;

using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.IO;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;

using SDL2;

using IUpdateable = ClassicUO.Interfaces.IUpdateable;

namespace ClassicUO.Game.UI.Gumps
{
    public enum ChatMode
    {
        Default,
        Whisper,
        Emote,
        Party,
        PartyPrivate,
        Guild,
        Alliance,
        ClientCommand
    }

    internal class SystemChatControl : Control
    {
        private const int MAX_MESSAGE_LENGHT = 100;
        private readonly List<Tuple<ChatMode, string>> _messageHistory;
        private readonly Deque<ChatLineTime> _textEntries;
        private readonly Label _currentChatModeLabel;
        private int _messageHistoryIndex = -1;
        private ChatMode _mode = ChatMode.Default;
        private readonly TextBox _textBox;
        private readonly CheckerTrans _trans;

        //public override bool AcceptKeyboardInput
        //{
        //    get;
        //    set;
        //}

        public SystemChatControl(int x, int y, int w, int h)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            _textEntries = new Deque<ChatLineTime>();
            _messageHistory = new List<Tuple<ChatMode, string>>();
            CanCloseWithRightClick = false;
            AcceptMouseInput = false;
            AcceptKeyboardInput = false;

            int height = FileManager.Fonts.GetHeightUnicode(1, "ABC", Width, 0, (ushort)(FontStyle.BlackBorder | FontStyle.Fixed));

            _textBox = new TextBox(1, MAX_MESSAGE_LENGHT, Width, Width, true, FontStyle.BlackBorder | FontStyle.Fixed, 33)
            {
                X = 0,
                Y = Height - height - 3,
                Width = Width,
                Height = height - 3
            };

            AddChildren(_trans = new CheckerTrans
            {
                X = _textBox.X,
                Y = _textBox.Y,
                Width = Width,
                Height = height + 5
            });
            AddChildren(_textBox);

            AddChildren(_currentChatModeLabel = new Label(string.Empty, true, 0, style: FontStyle.BlackBorder)
            {
                X = _textBox.X,
                Y = _textBox.Y,
                IsVisible = false
            });
            WantUpdateSize = false;

            Chat.Message += ChatOnMessage;
            Mode = ChatMode.Default;
        }
        
        private ChatMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;

                switch (value)
                {
                    case ChatMode.Default:
                        DisposeChatModePrefix();
                        _textBox.Hue = Engine.Profile.Current.SpeechHue;
                        _textBox.SetText(string.Empty);

                        break;
                    case ChatMode.Whisper:
                        AppendChatModePrefix("[Whisper]: ", 33);

                        break;
                    case ChatMode.Emote:
                        AppendChatModePrefix("[Emote]: ", Engine.Profile.Current.EmoteHue);

                        break;
                    case ChatMode.Party:
                        AppendChatModePrefix("[Party]: ", Engine.Profile.Current.PartyMessageHue);

                        break;
                    case ChatMode.PartyPrivate:
                        AppendChatModePrefix("[Private Party Message]: ", Engine.Profile.Current.PartyMessageHue);

                        break;
                    case ChatMode.Guild:
                        AppendChatModePrefix("[Guild]: ", Engine.Profile.Current.GuildMessageHue);

                        break;
                    case ChatMode.Alliance:
                        AppendChatModePrefix("[Alliance]: ", Engine.Profile.Current.AllyMessageHue);

                        break;
                    case ChatMode.ClientCommand:
                        AppendChatModePrefix("[Command]: ", 1161);

                        break;
                }
            }
        }

        private void ChatOnMessage(object sender, UOMessageEventArgs e)
        {
            switch (e.Type)
            {
                case MessageType.Regular when e.Parent == null || !e.Parent.Serial.IsValid:
                case MessageType.System:
                case MessageType.Party:
                case MessageType.Guild:
                case MessageType.Alliance:
                    AddLine(e.Text, (byte)e.Font, e.Hue, e.IsUnicode);
                    break;
            }
        }

        public override void Dispose()
        {
            Chat.Message -= ChatOnMessage;
            base.Dispose();
        }

        private void AppendChatModePrefix(string labelText, Hue hue)
        {
            if (!_currentChatModeLabel.IsVisible)
            {
                _currentChatModeLabel.Hue = hue;
                _currentChatModeLabel.Text = labelText;
                _currentChatModeLabel.IsVisible = true;
                _currentChatModeLabel.Location = _textBox.Location;
                _textBox.X = _currentChatModeLabel.Width;
                _textBox.Hue = hue;
                _textBox.SetText(string.Empty);
            }
        }

        private void DisposeChatModePrefix()
        {
            if (_currentChatModeLabel.IsVisible)
            {
                _textBox.Hue = 33;
                _textBox.X -= _currentChatModeLabel.Width;
                _currentChatModeLabel.IsVisible = false;
            }
        }

        public void AddLine(string text, byte font, Hue hue, bool isunicode)
        {
            if (_textEntries.Count >= 30)
                _textEntries.RemoveFromFront().Dispose();

            _textEntries.AddToBack(new ChatLineTime(text, font, isunicode, hue));
        }

        internal void Resize()
        {
            if (_textBox != null)
            {
                int height = FileManager.Fonts.GetHeightUnicode(1, "ABC", Width, 0, (ushort) (FontStyle.BlackBorder | FontStyle.Fixed));
                _textBox.Y = Height - height - 3;
                _textBox.Width = Width;
                _textBox.Height = height - 3;
                _trans.Location = _textBox.Location;
                _trans.Width = Width;
                _trans.Height = height + 5;
            }
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _textBox.SetKeyboardFocus();
        }

        public override void Update(double totalMS, double frameMS)
        {
            for (int i = 0; i < _textEntries.Count; i++)
            {
                _textEntries[i].Update(totalMS, frameMS);

                if (_textEntries[i].IsDispose)
                    _textEntries.RemoveAt(i--);
            }

            if (Mode == ChatMode.Default)
            {
                if (_textBox.Text.Length == 1)
                {
                    switch (_textBox.Text[0])
                    {
                        case ';':
                            Mode = ChatMode.Whisper;

                            break;
                        case '/':
                            Mode = ChatMode.Party;

                            break;
                        case '\\':
                            Mode = ChatMode.Guild;

                            break;
                        case '|':
                            Mode = ChatMode.Alliance;

                            break;
                        case '-':
                            Mode = ChatMode.ClientCommand;

                            break;
                    }
                }
                else if (_textBox.Text.Length == 2 && _textBox.Text[0] == ':' && _textBox.Text[1] == ' ')
                    Mode = ChatMode.Emote;
            }

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {
            int y = _textBox.Y + position.Y - 20;

            for (int i = _textEntries.Count - 1; i >= 0; i--)
            {
                y -= _textEntries[i].TextHeight;

                if (y >= position.Y)
                    _textEntries[i].Draw(batcher, new Point(position.X + 2, y));
            }

            return base.Draw(batcher, position, hue);
        }

        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            switch (key)
            {
                case SDL.SDL_Keycode.SDLK_q when Input.Keyboard.IsModPressed(mod, SDL.SDL_Keymod.KMOD_CTRL) && _messageHistoryIndex > -1:

                    if (_messageHistoryIndex > 0)
                        _messageHistoryIndex--;
                    Mode = _messageHistory[_messageHistoryIndex].Item1;
                    _textBox.SetText(_messageHistory[_messageHistoryIndex].Item2);

                    break;
                case SDL.SDL_Keycode.SDLK_w when Input.Keyboard.IsModPressed(mod, SDL.SDL_Keymod.KMOD_CTRL):

                    if (_messageHistoryIndex < _messageHistory.Count - 1)
                    {
                        _messageHistoryIndex++;
                        Mode = _messageHistory[_messageHistoryIndex].Item1;
                        _textBox.SetText(_messageHistory[_messageHistoryIndex].Item2);
                    }
                    else
                        _textBox.SetText(string.Empty);

                    break;
                case SDL.SDL_Keycode.SDLK_BACKSPACE when Input.Keyboard.IsModPressed(mod, SDL.SDL_Keymod.KMOD_NONE) && string.IsNullOrEmpty(_textBox.Text):
                    Mode = ChatMode.Default;

                    break;
            }
        }

        public override void OnKeyboardReturn(int textID, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;
            ChatMode sentMode = Mode;
            _textBox.SetText(string.Empty);
            _messageHistory.Add(new Tuple<ChatMode, string>(Mode, text));
            _messageHistoryIndex = _messageHistory.Count;
            Mode = ChatMode.Default;

            switch (sentMode)
            {
                case ChatMode.Default:
                    GameActions.Say(text, Engine.Profile.Current.SpeechHue);
                    break;
                case ChatMode.Whisper:
                    GameActions.Say(text, 33, MessageType.Whisper);
                    break;
                case ChatMode.Emote:
                    GameActions.Say(text, Engine.Profile.Current.EmoteHue, MessageType.Emote);
                    break;
                case ChatMode.Party:

                    text = text.ToLower();

                    switch (text)
                    {
                        case "add":
                            World.Party.TriggerAddPartyMember();
                            break;
                        case "loot":
                            if (World.Party.IsInParty)
                                World.Party.AllowPartyLoot = !World.Party.AllowPartyLoot;
                            break;
                        case "quit":
                            if (World.Party.IsInParty)
                                World.Party.QuitParty();
                            break;
                        case "accept":
                            if (!World.Party.IsInParty)
                                World.Party.AcceptPartyInvite();
                            break;
                        case "decline":
                            if (!World.Party.IsInParty)
                                World.Party.DeclinePartyInvite();
                            break;
                        default:

                            if (World.Party.IsInParty)
                            {
                                World.Party.PartyMessage(text);
                            }
                            break;
                    }
                    break;
                case ChatMode.PartyPrivate:
                    
                    //GameActions.Say(text, hue, speechType);
                    break;
                case ChatMode.Guild:
                    GameActions.Say(text, Engine.Profile.Current.GuildMessageHue, MessageType.Guild);
                    break;
                case ChatMode.Alliance:
                    GameActions.Say(text, Engine.Profile.Current.AllyMessageHue, MessageType.Alliance);
                    break;
                case ChatMode.ClientCommand:
                    CommandManager.Execute(text);

                    break;
            }

            DisposeChatModePrefix();
        }

        private class ChatLineTime : IUpdateable, IDisposable
        {           
            private float _createdTime;
            private readonly RenderedText _renderedText;
            private float _alpha;

            public ChatLineTime(string text, byte font, bool isunicode, Hue hue)
            {
                _renderedText = new RenderedText
                {
                    IsUnicode = isunicode,
                    Font = font,
                    MaxWidth = 320,
                    FontStyle = FontStyle.BlackBorder,
                    Hue = hue,
                    Text = text
                };
                _createdTime = Constants.TIME_DISPLAY_SYSTEM_MESSAGE_TEXT;
            }

            public string Text => _renderedText.Text;

            public bool IsDispose { get; private set; }

            public int TextHeight => _renderedText.Height;

           
            public bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
            {
                return _renderedText.Draw(batcher, position, ShaderHuesTraslator.GetHueVector(0, false, _alpha, true));
            }

            public void Update(double totalMS, double frameMS)
            {
                _createdTime -= (float)frameMS;

                if (_createdTime > 0 && _createdTime <= Constants.TIME_FADEOUT_TEXT)
                {
                    _alpha = 1.0f - (_createdTime / Constants.TIME_FADEOUT_TEXT);
                }
                if (_createdTime <= 0.0f)
                    Dispose();

                //else if (time > Constants.TIME_DISPLAY_SYSTEM_MESSAGE_TEXT - Constants.TIME_FADEOUT_TEXT)
                //    _alpha = (time - (Constants.TIME_DISPLAY_SYSTEM_MESSAGE_TEXT - Constants.TIME_FADEOUT_TEXT)) / Constants.TIME_FADEOUT_TEXT;
            }

            public override string ToString()
            {
                return Text;
            }

            public void Dispose()
            {
                if (IsDispose)
                    return;

                IsDispose = true;
                _renderedText.Dispose();
            }
        }
    }
}