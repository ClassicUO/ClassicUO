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
using ClassicUO.Game.Scenes;
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

    internal class ChatControl : Control
    {
        private const int MAX_MESSAGE_LENGHT = 100;
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
            CanCloseWithRightClick = false;
            AcceptMouseInput = false;
            AcceptKeyboardInput = false;


            int height = Fonts.GetHeightUnicode(1, "ABC", Width, 0, (ushort)(FontStyle.BlackBorder | FontStyle.Fixed));

            _textBox = new TextBox(1, MAX_MESSAGE_LENGHT, Width, Width, true, FontStyle.BlackBorder | FontStyle.Fixed, 33)
            {
                X = 0,
                Y = Height - height - 3,
                Width = Width,
                Height = height - 3
            };
            Mode = ChatMode.Default;

            AddChildren(new CheckerTrans
            {
                X = _textBox.X,
                Y = _textBox.Y,
                Width = Width,
                Height = height + 5
            });
            AddChildren(_textBox);


            WantUpdateSize = false;
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
                        AppendChatModePrefix("[Alliance]: ", 487);

                        break;
                    case ChatMode.ClientCommand:
                        AppendChatModePrefix("[Command]: ", 1161);

                        break;
                }
            }
        }

        private void AppendChatModePrefix(string labelText, Hue hue)
        {
            _currentChatModeLabel?.Dispose();

            _currentChatModeLabel = new Label(labelText, true, hue, style: FontStyle.BlackBorder)
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
            int y = _textBox.Y + position.Y - 6;

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

        public override void OnKeyboardReturn(int textID, string text)
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
                    GameActions.Say(text, hue, speechType);
                    break;
                case ChatMode.Whisper:
                    speechType = MessageType.Whisper;
                    GameActions.Say(text, hue, speechType);
                    break;
                case ChatMode.Emote:
                    speechType = MessageType.Emote;
                    GameActions.Say(text, hue, speechType);
                    break;
                case ChatMode.Party:

                    text = text.ToLower();

                    if (text.Equals("add"))
                    {
                        World.Party.TriggerAddPartyMember();
                        break;
                    }

                    if (World.Party.IsInParty)
                    {
                        if (text.Equals("loot"))
                        {
                            World.Party.AllowPartyLoot = !World.Party.AllowPartyLoot ? true : false;
                            break;
                        }

                        if (text.Equals("quit"))
                        {
                            World.Party.QuitParty();
                            break;
                        }

                        World.Party.PartyMessage(text);
                    }
                    else
                    {
                        if (text.Equals("accept"))
                        {
                            World.Party.AcceptPartyInvite();
                        }
                        else if (text.Equals("decline"))
                        {
                            World.Party.DeclinePartyInvite();
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
                            Engine.SceneManager.GetScene<GameScene>().Journal.Add(NoteToSelf, noteToSelfFont, noteToSelfHue, "");
                        }
                    }

                    break;
                case ChatMode.PartyPrivate:
                    
                    //GameActions.Say(text, hue, speechType);
                    break;
                case ChatMode.Guild:
                    speechType = MessageType.Guild;
                    GameActions.Say(text, hue, speechType);
                    break;
                case ChatMode.Alliance:
                    speechType = MessageType.Alliance;
                    GameActions.Say(text, hue, speechType);
                    break;
                case ChatMode.ClientCommand:
                    Commands.Execute(text);

                    break;
            }

            DisposeChatModePrefix();

            //GameActions.Say(text, hue, speechType, 0);
        }

        private class ChatLineTime : IUpdateable, IDisposable
        {           
            private readonly float _createdTime;
            private readonly RenderedText _renderedText;
            private float _alpha;

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
                _createdTime = Engine.Ticks;
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
                float time = (float) totalMS - _createdTime;

                if (time > Constants.TIME_DISPLAY_SYSTEM_MESSAGE_TEXT)
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