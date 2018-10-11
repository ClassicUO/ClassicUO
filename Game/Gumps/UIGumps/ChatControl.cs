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
        Alliance
    }

    internal class ChatControl : GumpControl
    {
        private const int MAX_MESSAGE_LENGHT = 100;

        private TextBox _textBox;
        private readonly List<ChatLineTime> _textEntries;
        private readonly List<Tuple<ChatMode, string>> _messageHistory;
        private readonly InputManager _inputManager;
        private int _messageHistoryIndex = -1;
        private Serial _privateMsgSerial = 0;
        private string _privateMsgName;

        private ChatMode _mode = ChatMode.Default;

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
                        break;
                    case ChatMode.Whisper:
                        break;
                    case ChatMode.Emote:
                        break;
                    case ChatMode.Party:
                        break;
                    case ChatMode.PartyPrivate:
                        break;
                    case ChatMode.Guild:
                        break;
                    case ChatMode.Alliance:
                        break;
                }

                _textBox.SetText(string.Empty);
            }
        }

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

        public void AddLine(string text, byte font, Hue hue, bool isunicode)
        { 
            _textEntries.Add(new ChatLineTime(text, 320, font, isunicode, hue));
        }

        protected override void OnResize()
        {
            if (_textBox != null)
            {
                int height = Fonts.GetHeightUnicode(1, "ABC", Width, 0,
                    (ushort)(FontStyle.BlackBorder | FontStyle.Fixed));

                _textBox.Y = Height - height - 3;
                _textBox.Width = Width;
                _textBox.Height = height - 3;

                CheckerTrans trans = GetControls<CheckerTrans>()[0];
                trans.Location = new Point(_textBox.X, _textBox.Y);
                trans.Width = Width;
                trans.Height = height + 5;
            }
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (_textBox == null)
            {
                int height = Fonts.GetHeightUnicode(1, "ABC", Width, 0,
                    (ushort) (FontStyle.BlackBorder | FontStyle.Fixed));

                _textBox = new TextBox(1, MAX_MESSAGE_LENGHT, Width, Width, true, FontStyle.BlackBorder | FontStyle.Fixed, 33)
                {
                    X = 0,
                    Y = Height - height - 3,
                    Width = Width,
                    Height = height - 3
                };


                Mode = ChatMode.Default;

                AddChildren(new CheckerTrans {X = _textBox.X, Y = _textBox.Y, Width = Width, Height = height + 5 });
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

            if (IsFocused)
            {
                if (_inputManager.HandleKeybaordEvent(KeyboardEvent.Down, SDL.SDL_Keycode.SDLK_q, false, false, true) &&
                    _messageHistoryIndex > -1)
                {
                    if (_messageHistoryIndex > 0)
                        _messageHistoryIndex--;
                    Mode = _messageHistory[_messageHistoryIndex].Item1;
                    _textBox.SetText(_messageHistory[_messageHistoryIndex].Item2);
                }
                else if (_inputManager.HandleKeybaordEvent(KeyboardEvent.Down, SDL.SDL_Keycode.SDLK_w, false, false, true))
                {
                    if (_messageHistoryIndex < _messageHistory.Count - 1)
                    {
                        _messageHistoryIndex++;
                        Mode = _messageHistory[_messageHistoryIndex].Item1;
                        _textBox.SetText(_messageHistory[_messageHistoryIndex].Item2);
                    }
                    else
                    {
                        _textBox.SetText(string.Empty);
                    }
                }
                else if (_inputManager.HandleKeybaordEvent(KeyboardEvent.Down, SDL.SDL_Keycode.SDLK_BACKSPACE, false, false, false) && _textBox.Text == string.Empty)
                {
                    Mode = ChatMode.Default;
                }
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
                    }
                }
                else if (_textBox.Text.Length == 2 && _textBox.Text[0] == ':' && _textBox.Text[1] == ' ')
                {
                    Mode = ChatMode.Emote;
                }
            }

           

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            int y = _textBox.Y + (int) position.Y - 6;

            for (int i = _textEntries.Count - 1; i >= 0; i--)
            {
                y -= _textEntries[i].TextHeight;

                if (y >= (int)position.Y)
                    _textEntries[i].Draw(spriteBatch, new Vector3(position.X + 2, y, 0));
            }

            return base.Draw(spriteBatch, position, hue);
        }

        public override void OnKeybaordReturn(int textID, string text)
        {
            if (string.IsNullOrEmpty((text)))
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
                    break;
                case ChatMode.Whisper:
                    break;
                case ChatMode.Emote:
                    break;
                case ChatMode.Party:
                    break;
                case ChatMode.PartyPrivate:
                    break;
                case ChatMode.Guild:
                    break;
                case ChatMode.Alliance:
                    break;
            }

            //GameActions.Say(text, hue, speechType, 0);
            NetClient.Socket.Send(new PUnicodeSpeechRequest(text, speechType, MessageFont.Normal, hue, "ENU"));
        }


        private class ChatLineTime : IUpdateable, IDrawableUI, IDisposable
        {
            private readonly RenderedText _renderedText;
            private readonly float _createdTime;
            private int _width;

            private const float TIME_DISPLAY = 10000.0f;
            private const float TIME_FADEOUT = 2000.0f;

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
            public bool AllowedToDraw { get; set; } = true;
            public int TextHeight => _renderedText.Height;
            public SpriteTexture Texture { get; set; }


            public bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null) =>
                _renderedText.Draw(spriteBatch, position, RenderExtentions.GetHueVector(0, false, Alpha < 1.0f ? Alpha : 0 , true));

            public void Update(double totalMS, double frameMS)
            {                    
                float time = (float) totalMS - _createdTime;
                if (time > TIME_DISPLAY)
                    IsExpired = true;
                else if (time > TIME_DISPLAY - TIME_FADEOUT)
                {
                    Alpha = (time - (TIME_DISPLAY - TIME_FADEOUT)) / TIME_FADEOUT;
                }
            }

            public void Dispose()
            {
                _renderedText.Dispose();
            }

            public override string ToString() => Text;
        }
    }
}