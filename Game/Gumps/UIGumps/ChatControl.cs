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
using System.Linq;

using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.System;
using ClassicUO.Input;
using ClassicUO.Interfaces;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

using SDL2;

using IUpdateable = ClassicUO.Interfaces.IUpdateable;

namespace ClassicUO.Game.Gumps.UIGumps
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

    internal class ChatControl : GumpControl
    {
        private const int MAX_MESSAGE_LENGHT = 100;
        private readonly InputManager _inputManager;
        private readonly List<Tuple<ChatMode, string>> _messageHistory;
        private readonly List<ChatLineTime> _textEntries;
        private Label _currentChatModeLabel;
        private int _messageHistoryIndex = -1;
        private ChatMode _mode = ChatMode.Default;
        private string _privateMsgName;
        private Serial _privateMsgSerial = 0;
        private TextBox _textBox;

        public ChatControl(int x, int y, int w, int h)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            _textEntries = new List<ChatLineTime>();
            _messageHistory = new List<Tuple<ChatMode, string>>();
            _inputManager = Service.Get<InputManager>();
            CanCloseWithRightClick = false;
            AcceptMouseInput = false;
            AcceptKeyboardInput = false;
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
                        _textBox.Hue = 33;
                        _textBox.SetText(string.Empty);

                        break;
                    case ChatMode.Whisper:
                        AppendChatModePrefix("[Whisper]: ", 33);

                        break;
                    case ChatMode.Emote:
                        AppendChatModePrefix("[Emote]: ", 646);

                        break;
                    case ChatMode.Party:
                        AppendChatModePrefix("[Party]: ", 0xFFFF);

                        break;
                    case ChatMode.PartyPrivate:
                        AppendChatModePrefix("[Private Party Message]: ", 1918);

                        break;
                    case ChatMode.Guild:
                        AppendChatModePrefix("[Guild]: ", 70);

                        break;
                    case ChatMode.Alliance:
                        AppendChatModePrefix("[Aliance]: ", 487);

                        break;
                    case ChatMode.ClientCommand:
                        AppendChatModePrefix("[Command]: ", 1161);

                        break;
                }
            }
        }

        private void AppendChatModePrefix(string labelText, Hue hue)
        {
            _currentChatModeLabel = new Label(labelText, true, hue)
            {
                X = _textBox.X, Y = _textBox.Y
            };
            _textBox.X = _currentChatModeLabel.Width;
            _textBox.Hue = hue;
            _textBox.SetText(string.Empty);
            AddChildren(_currentChatModeLabel);
        }

        private void DisposeChatModePrefix()
        {
            if (_currentChatModeLabel != null)
            {
                _textBox.Hue = 33;
                _textBox.X -= _currentChatModeLabel.Width;
                _currentChatModeLabel.Dispose();
                _currentChatModeLabel = null;
            }
        }

        public void AddLine(string text, byte font, Hue hue, bool isunicode)
        {
            _textEntries.Add(new ChatLineTime(text, 320, font, isunicode, hue));
        }

        internal void Resize()
        {
            if (_textBox != null)
            {
                int height = Fonts.GetHeightUnicode(1, "ABC", Width, 0, (ushort) (FontStyle.BlackBorder | FontStyle.Fixed));
                _textBox.Y = Height - height - 3;
                _textBox.Width = Width;
                _textBox.Height = height - 3;
                CheckerTrans trans = FindControls<CheckerTrans>().FirstOrDefault();
                trans.Location = new Point(_textBox.X, _textBox.Y);
                trans.Width = Width;
                trans.Height = height + 5;
            }
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (_textBox == null)
            {
                int height = Fonts.GetHeightUnicode(1, "ABC", Width, 0, (ushort) (FontStyle.BlackBorder | FontStyle.Fixed));

                _textBox = new TextBox(1, MAX_MESSAGE_LENGHT, Width, Width, true, FontStyle.BlackBorder | FontStyle.Fixed, 33)
                {
                    X = 0, Y = Height - height - 3, Width = Width, Height = height - 3
                };
                Mode = ChatMode.Default;

                AddChildren(new CheckerTrans
                {
                    X = _textBox.X, Y = _textBox.Y, Width = Width, Height = height + 5
                });
                AddChildren(_textBox);
            }

            for (int i = 0; i < _textEntries.Count; i++)
            {
                _textEntries[i].Update(totalMS, frameMS);

                if (_textEntries[i].IsExpired)
                {
                    _textEntries[i].Dispose();
                    _textEntries.RemoveAt(i--);
                }
            }

            //if (IsFocused)
            //{
            //    if (_inputManager.HandleKeybaordEvent(KeyboardEvent.Down, SDL.SDL_Keycode.SDLK_q, false, false, true) && _messageHistoryIndex > -1)
            //    {
            //        if (_messageHistoryIndex > 0)
            //            _messageHistoryIndex--;
            //        Mode = _messageHistory[_messageHistoryIndex].Item1;
            //        _textBox.SetText(_messageHistory[_messageHistoryIndex].Item2);
            //    }
            //    else if (_inputManager.HandleKeybaordEvent(KeyboardEvent.Down, SDL.SDL_Keycode.SDLK_w, false, false, true))
            //    {
            //        if (_messageHistoryIndex < _messageHistory.Count - 1)
            //        {
            //            _messageHistoryIndex++;
            //            Mode = _messageHistory[_messageHistoryIndex].Item1;
            //            _textBox.SetText(_messageHistory[_messageHistoryIndex].Item2);
            //        }
            //        else
            //        {
            //            _textBox.SetText(string.Empty);
            //        }
            //    }
            //    else if (_inputManager.HandleKeybaordEvent(KeyboardEvent.Down, SDL.SDL_Keycode.SDLK_BACKSPACE, false, false, false) && _textBox.Text == string.Empty)
            //    {
            //        Mode = ChatMode.Default;
            //    }
            //}

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
                        case '#':
                            Mode = ChatMode.ClientCommand;

                            break;
                    }
                }
                else if (_textBox.Text.Length == 2 && _textBox.Text[0] == ':' && _textBox.Text[1] == ' ') Mode = ChatMode.Emote;
            }

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Point position, Vector3? hue = null)
        {
            int y = _textBox.Y + position.Y - 6;

            for (int i = _textEntries.Count - 1; i >= 0; i--)
            {
                y -= _textEntries[i].TextHeight;

                if (y >= position.Y)
                    _textEntries[i].Draw(spriteBatch, new Point(position.X + 2, y));
            }

            return base.Draw(spriteBatch, position, hue);
        }

        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            switch (key)
            {
                case SDL.SDL_Keycode.SDLK_q when mod == SDL.SDL_Keymod.KMOD_LCTRL && _messageHistoryIndex > -1:

                    if (_messageHistoryIndex > 0)
                        _messageHistoryIndex--;
                    Mode = _messageHistory[_messageHistoryIndex].Item1;
                    _textBox.SetText(_messageHistory[_messageHistoryIndex].Item2);

                    break;
                case SDL.SDL_Keycode.SDLK_w when mod == SDL.SDL_Keymod.KMOD_LCTRL:

                    if (_messageHistoryIndex < _messageHistory.Count - 1)
                    {
                        _messageHistoryIndex++;
                        Mode = _messageHistory[_messageHistoryIndex].Item1;
                        _textBox.SetText(_messageHistory[_messageHistoryIndex].Item2);
                    }
                    else
                        _textBox.SetText(string.Empty);

                    break;
                case SDL.SDL_Keycode.SDLK_BACKSPACE when mod == SDL.SDL_Keymod.KMOD_NONE && string.IsNullOrEmpty(_textBox.Text):
                    Mode = ChatMode.Default;

                    break;
            }
        }

        public override void OnKeybaordReturn(int textID, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;
            ChatMode sentMode = Mode;
            MessageType speechType = MessageType.Regular;
            ushort hue = 0;
            _textBox.SetText(string.Empty);
            _messageHistory.Add(new Tuple<ChatMode, string>(Mode, text));
            _messageHistoryIndex = _messageHistory.Count;
            Mode = ChatMode.Default;

            switch (sentMode)
            {
                case ChatMode.Default:
                    speechType = MessageType.Regular;
                    hue = 33;
                    NetClient.Socket.Send(new PUnicodeSpeechRequest(text, speechType, MessageFont.Normal, hue, "ENU"));

                    break;
                case ChatMode.Whisper:

                    break;
                case ChatMode.Emote:

                    break;
                case ChatMode.Party:

                    if (text.Equals("add"))
                    {
                        PartySystem.TriggerAddPartyMember();
                        break;
                    }

                    if (PartySystem.IsInParty)
                    {
                        if (text.Equals("loot"))
                        {
                            PartySystem.AllowPartyLoot = !PartySystem.AllowPartyLoot ? true : false;
                            break;
                        }

                        if (text.Equals("quit"))
                        {
                            PartySystem.QuitParty();
                            break;
                        }

                        PartySystem.PartyMessage(text);
                    }
                    else
                    {
                        if (text.Equals("accept"))
                        {
                            PartySystem.AcceptPartyInvite();
                        }
                        else if (text.Equals("decline"))
                        {
                            PartySystem.DeclinePartyInvite();
                        }
                        else if (text.Equals("quit"))
                        {
                            string notInPartyMessage = "You are not in a party.";
                            Hue notInPartyHue = 0x03B2; //white
                            MessageType type = MessageType.Regular;
                            MessageFont notInPartyFont = MessageFont.Normal;
                            bool isUnicode = true;

                            Chat.OnMessage(null, new UOMessageEventArgs(notInPartyMessage, notInPartyHue, type, notInPartyFont, isUnicode));
                        }
                        else
                        {
                            Hue noteToSelfHue = 0x7FFF; //grey
                            MessageFont noteToSelfFont = MessageFont.Normal;
                            string NoteToSelf = "Note to self: " + text;

                            //we write directly to the journal to avoid 'System:' prefix
                            Service.Get<ChatControl>().AddLine(NoteToSelf, (byte)noteToSelfFont, noteToSelfHue, false);
                            Service.Get<JournalData>().AddEntry(NoteToSelf, (byte)noteToSelfFont, noteToSelfHue, "");
                        }
                    }

                    break;
                case ChatMode.PartyPrivate:

                    break;
                case ChatMode.Guild:

                    break;
                case ChatMode.Alliance:

                    break;
                case ChatMode.ClientCommand:
                    CommandSystem.TriggerCommandHandler(text);

                    break;
            }

            DisposeChatModePrefix();

            //GameActions.Say(text, hue, speechType, 0);
        }

        private class ChatLineTime : IUpdateable, IDrawableUI, IDisposable
        {
            private const float TIME_DISPLAY = 10000.0f;
            private const float TIME_FADEOUT = 1000.0f;
            private readonly float _createdTime;
            private readonly RenderedText _renderedText;
            private int _width;

            public ChatLineTime(string text, int width, byte font, bool isunicode, Hue hue)
            {
                _renderedText = new RenderedText
                {
                    IsUnicode = isunicode,
                    Font = font,
                    MaxWidth = width,
                    FontStyle = FontStyle.BlackBorder,
                    Hue = hue,
                    Text = text
                };
                _width = width;
                _createdTime = CoreGame.Ticks;
            }

            public string Text => _renderedText.Text;

            public bool IsExpired { get; private set; }

            public float Alpha { get; private set; }

            public int TextHeight => _renderedText.Height;

            public void Dispose()
            {
                _renderedText.Dispose();
            }

            public bool AllowedToDraw { get; set; } = true;

            public SpriteTexture Texture { get; set; }

            public bool Draw(SpriteBatchUI spriteBatch, Point position, Vector3? hue = null)
            {
                return _renderedText.Draw(spriteBatch, position, ShaderHuesTraslator.GetHueVector(0, false, Alpha < 1.0f ? Alpha : 0, true));
            }

            public void Update(double totalMS, double frameMS)
            {
                float time = (float) totalMS - _createdTime;

                if (time > TIME_DISPLAY)
                    IsExpired = true;
                else if (time > TIME_DISPLAY - TIME_FADEOUT) Alpha = (time - (TIME_DISPLAY - TIME_FADEOUT)) / TIME_FADEOUT;
            }

            public override string ToString()
            {
                return Text;
            }
        }
    }
}