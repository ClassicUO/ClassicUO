#region license
//  Copyright (C) 2019 ClassicUO Development Community on Github
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

using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Platforms;

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
        ClientCommand,
        UOAMChat,
        Prompt
    }

    internal class SystemChatControl : Control
    {
        private const int MAX_MESSAGE_LENGHT = 100;
        private readonly List<Tuple<ChatMode, string>> _messageHistory;
        private readonly Deque<ChatLineTime> _textEntries;
        private readonly Label _currentChatModeLabel;
        private int _messageHistoryIndex = -1;
        private ChatMode _mode = ChatMode.Default;
        private readonly AlphaBlendControl _trans;

        public readonly TextBox textBox;

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

            textBox = new TextBox(1, MAX_MESSAGE_LENGHT, Width, Width, true, FontStyle.BlackBorder | FontStyle.Fixed, 33)
            {
                X = 0,
                Y = Height - height - 3,
                Width = Width,
                Height = height - 3,
            };

            Add(_trans = new AlphaBlendControl
            {
                X = textBox.X,
                Y = textBox.Y,
                Width = Width,
                Height = height + 5,
                IsVisible = (!Engine.Profile.Current.ActivateChatAfterEnter),
                AcceptMouseInput = true
            });
            Add(textBox);

            _trans.MouseClick += (sender, e) =>
            {
                if (Engine.Profile.Current.ActivateChatAfterEnter && _trans.Y != textBox.Y)
                    ToggleChatVisibility();
            };
            _trans.DragBegin += (sender, e) =>
            {
                if (Engine.Profile.Current.ActivateChatAfterEnter && _trans.Y != textBox.Y)
                    ToggleChatVisibility();
            };
            _trans.MouseEnter += (sender, e) =>
            {
                if (Engine.Profile.Current.ActivateChatAfterEnter)
                    _trans.Alpha = 0.3f;
            };
            _trans.MouseExit += (sender, e) =>
            {
                if (Engine.Profile.Current.ActivateChatAfterEnter)
                    _trans.Alpha = 0.5f;
            };

            Add(_currentChatModeLabel = new Label(string.Empty, true, 0, style: FontStyle.BlackBorder)
            {
                X = textBox.X,
                Y = textBox.Y,
                IsVisible = false
            });

            WantUpdateSize = false;

            Chat.MessageReceived += ChatOnMessageReceived;
            Mode = ChatMode.Default;
        }

        public void ToggleChatVisibility()
        {
            ChatVisibility = !textBox.IsVisible;
        }

        public bool ChatVisibility
        {
            get => textBox.IsVisible;
            set
            {
                if (value)
                {
                    Engine.Profile.Current.ActivateChatStatus = textBox.IsVisible = _trans.IsVisible = value;
                    _trans.Y = textBox.Y;
                    textBox.SetText(string.Empty);
                    textBox.SetKeyboardFocus();
                }
                else
                {
                    Engine.Profile.Current.ActivateChatStatus = textBox.IsVisible = value;
                    _trans.Y = textBox.Y + ((Engine.Profile.Current.ActivateChatCompletelyHide) ? 20 : 10);
                }
            }
        }

        private ChatMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;

                if (ChatVisibility)
                switch (value)
                {
                    case ChatMode.Default:
                        DisposeChatModePrefix();
                        textBox.Hue = Engine.Profile.Current.SpeechHue;
                        textBox.SetText(string.Empty);

                        break;
                    case ChatMode.Whisper:
                        AppendChatModePrefix("[Whisper]: ", Engine.Profile.Current.WhisperHue);

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
                    case ChatMode.UOAMChat:
                        DisposeChatModePrefix();
                        AppendChatModePrefix("[UOAM]: ", 83);

                        break;
                }
            }
        }

        private void ChatOnMessageReceived(object sender, UOMessageEventArgs e)
        {
            switch (e.Type)
            {
                case MessageType.Regular when e.Parent == null || !e.Parent.Serial.IsValid:
                case MessageType.System:
                    AddLine(e.Text, (byte) e.Font, e.Hue, e.IsUnicode);
                    break;

                case MessageType.Party:
                    AddLine($"[Party][{e.Name}]: {e.Text}", (byte) e.Font, Engine.Profile.Current.PartyMessageHue, e.IsUnicode);
                    break;

                case MessageType.Guild:
                    AddLine($"[Guild][{e.Name}]: {e.Text}", (byte) e.Font, Engine.Profile.Current.GuildMessageHue, e.IsUnicode);
                    break;

                case MessageType.Alliance:
                    AddLine($"[Alliance][{e.Name}]: {e.Text}", (byte) e.Font, Engine.Profile.Current.AllyMessageHue, e.IsUnicode);
                    break;
            }
        }

        public override void Dispose()
        {
            Chat.MessageReceived -= ChatOnMessageReceived;
            base.Dispose();
        }

        private void AppendChatModePrefix(string labelText, Hue hue)
        {
            if (!_currentChatModeLabel.IsVisible)
            {
                _currentChatModeLabel.Hue = hue;
                _currentChatModeLabel.Text = labelText;
                _currentChatModeLabel.IsVisible = true;
                _currentChatModeLabel.Location = textBox.Location;
                textBox.X = _currentChatModeLabel.Width;
                textBox.Hue = hue;
                textBox.SetText(string.Empty);
            }
        }

        private void DisposeChatModePrefix()
        {
            if (_currentChatModeLabel.IsVisible)
            {
                textBox.Hue = 33;
                textBox.X -= _currentChatModeLabel.Width;
                _currentChatModeLabel.IsVisible = false;
            }
        }

        public void AddLine(string text, byte font, Hue hue, bool isunicode)
        {
            if (_textEntries.Count >= 30)
                _textEntries.RemoveFromFront().Destroy();

            _textEntries.AddToBack(new ChatLineTime(text, font, isunicode, hue));
        }

        internal void Resize()
        {
            if (textBox != null)
            {
                int height = FileManager.Fonts.GetHeightUnicode(1, "ABC", Width, 0, (ushort) (FontStyle.BlackBorder | FontStyle.Fixed));
                textBox.Y = Height - height - 3;
                textBox.Width = Width;
                textBox.Height = height - 3;
                _trans.Location = textBox.Location;
                _trans.Width = Width;
                _trans.Height = height + 5;
            }
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            textBox.SetKeyboardFocus();
        }

        public override void Update(double totalMS, double frameMS)
        {
            for (int i = 0; i < _textEntries.Count; i++)
            {
                _textEntries[i].Update(totalMS, frameMS);

                if (_textEntries[i].IsDispose)
                    _textEntries.RemoveAt(i--);
            }

            if ( Mode == ChatMode.Default && ChatVisibility )
            {
                if (textBox.Text.Length == 1)
                {
                    switch (textBox.Text[0])
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
                else if (textBox.Text.Length == 2)
                {
                    if (textBox.Text[0] == ':' && textBox.Text[1] == ' ')
                        Mode = ChatMode.Emote;
                }
            }
            else if (Mode == ChatMode.ClientCommand && textBox.Text.Length == 1 && textBox.Text[0] == '-')
                Mode = ChatMode.UOAMChat;

            if (Engine.Profile.Current.SpeechHue != textBox.Hue)
            {
                textBox.Hue = Engine.Profile.Current.SpeechHue;
            }

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(Batcher2D batcher, int x, int y)
        {
            int yy = textBox.Y + y - 20;

            for (int i = _textEntries.Count - 1; i >= 0; i--)
            {
                yy -= _textEntries[i].TextHeight;

                if (yy >= y)
                    _textEntries[i].Draw(batcher, x + 2, yy);
            }

            return base.Draw(batcher, x, y);
        }

        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            switch (key)
            {
                case SDL.SDL_Keycode.SDLK_q when Input.Keyboard.IsModPressed(mod, SDL.SDL_Keymod.KMOD_CTRL) && _messageHistoryIndex > -1:

                    if (!ChatVisibility)
                        ChatVisibility = true;

                    if (_messageHistoryIndex > 0)
                        _messageHistoryIndex--;

                    Mode = _messageHistory[_messageHistoryIndex].Item1;
                    textBox.SetText(_messageHistory[_messageHistoryIndex].Item2);

                    break;

                case SDL.SDL_Keycode.SDLK_w when Input.Keyboard.IsModPressed(mod, SDL.SDL_Keymod.KMOD_CTRL):

                    if (!ChatVisibility)
                        ChatVisibility = true;

                    if (_messageHistoryIndex < _messageHistory.Count - 1)
                    {
                        _messageHistoryIndex++;
                        Mode = _messageHistory[_messageHistoryIndex].Item1;
                        textBox.SetText(_messageHistory[_messageHistoryIndex].Item2);
                    }
                    else
                        textBox.SetText(string.Empty);

                    break;

                case SDL.SDL_Keycode.SDLK_BACKSPACE when Input.Keyboard.IsModPressed(mod, SDL.SDL_Keymod.KMOD_NONE) && string.IsNullOrEmpty(textBox.Text):
                    Mode = ChatMode.Default;
                    break;

                case SDL.SDL_Keycode.SDLK_ESCAPE when Chat.PromptData.Prompt != ConsolePrompt.None:
                    if (Chat.PromptData.Prompt == ConsolePrompt.ASCII)
                        NetClient.Socket.Send(new PASCIIPromptResponse(string.Empty, true));
                    else if (Chat.PromptData.Prompt == ConsolePrompt.Unicode)
                        NetClient.Socket.Send(new PUnicodePromptResponse(string.Empty, "ENU", true));
                    Chat.PromptData = default;
                    break;

                case SDL.SDL_Keycode.SDLK_1 when Input.Keyboard.IsModPressed(mod, SDL.SDL_Keymod.KMOD_SHIFT): // !
                case SDL.SDL_Keycode.SDLK_BACKSLASH when Input.Keyboard.IsModPressed(mod, SDL.SDL_Keymod.KMOD_SHIFT): // \
                    if (Engine.Profile.Current.ActivateChatAfterEnter && Engine.Profile.Current.ActivateChatAdditionalButtons && !ChatVisibility)
                        ChatVisibility = true;
                    break;

                case SDL.SDL_Keycode.SDLK_EXCLAIM: // !
                case SDL.SDL_Keycode.SDLK_SEMICOLON: // ;
                case SDL.SDL_Keycode.SDLK_COLON: // :
                case SDL.SDL_Keycode.SDLK_QUESTION: // ?
                case SDL.SDL_Keycode.SDLK_SLASH: // /
                case SDL.SDL_Keycode.SDLK_BACKSLASH: // \
                case SDL.SDL_Keycode.SDLK_PERIOD: // .
                case SDL.SDL_Keycode.SDLK_KP_PERIOD: // .
                case SDL.SDL_Keycode.SDLK_COMMA: // ,
                case SDL.SDL_Keycode.SDLK_LEFTBRACKET: // [
                case SDL.SDL_Keycode.SDLK_MINUS: // -
                case SDL.SDL_Keycode.SDLK_KP_MINUS: // -
                    if (Input.Keyboard.IsModPressed(mod, SDL.SDL_Keymod.KMOD_NONE) && Engine.Profile.Current.ActivateChatAfterEnter && Engine.Profile.Current.ActivateChatAdditionalButtons && !ChatVisibility)
                        ChatVisibility = true;
                    break;

                case SDL.SDL_Keycode.SDLK_KP_ENTER:
                case SDL.SDL_Keycode.SDLK_RETURN:
                    if (Engine.Profile.Current.ActivateChatAfterEnter)
                    {
                        Mode = ChatMode.Default;
                        if (!(Input.Keyboard.IsModPressed(mod, SDL.SDL_Keymod.KMOD_SHIFT) && Engine.Profile.Current.ActivateChatShiftEnterSupport))
                            ToggleChatVisibility();
                    }
                    break;
            }
        }

        public override void OnKeyboardReturn(int textID, string text)
        {
            if (!textBox.IsVisible && Engine.Profile.Current.ActivateChatAfterEnter)
            {
                textBox.SetText(string.Empty);
                text = string.Empty;
                Mode = ChatMode.Default;
            }

            if (string.IsNullOrEmpty(text))
                return;

            ChatMode sentMode = Mode;
            textBox.SetText(string.Empty);
            _messageHistory.Add(new Tuple<ChatMode, string>(Mode, text));
            _messageHistoryIndex = _messageHistory.Count;
            Mode = ChatMode.Default;

            if (Chat.PromptData.Prompt != ConsolePrompt.None)
            {
                if (Chat.PromptData.Prompt == ConsolePrompt.ASCII)
                    NetClient.Socket.Send(new PASCIIPromptResponse(text, text.Length < 1));
                else if (Chat.PromptData.Prompt == ConsolePrompt.Unicode)
                    NetClient.Socket.Send(new PUnicodePromptResponse(text, "ENU", text.Length < 1));

                Chat.PromptData = default;
            }
            else
            {
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

                    case ChatMode.UOAMChat:
                        UoAssist.SignalMessage(text);
                        break;
                }
            }

            DisposeChatModePrefix();
        }

        private class ChatLineTime : IUpdateable
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

            private string Text => _renderedText.Text;

            public bool IsDispose { get; private set; }

            public int TextHeight => _renderedText.Height;

           
            public bool Draw(Batcher2D batcher, int x, int y)
            {
                return _renderedText.Draw(batcher, x, y/*, ShaderHuesTraslator.GetHueVector(0, false, _alpha, true)*/);
            }

            public void Update(double totalMS, double frameMS)
            {
                _createdTime -= (float)frameMS;

                if (_createdTime > 0 && _createdTime <= Constants.TIME_FADEOUT_TEXT)
                {
                    _alpha = 1.0f - (_createdTime / Constants.TIME_FADEOUT_TEXT);
                }
                if (_createdTime <= 0.0f)
                    Destroy();

                //else if (time > Constants.TIME_DISPLAY_SYSTEM_MESSAGE_TEXT - Constants.TIME_FADEOUT_TEXT)
                //    _alpha = (time - (Constants.TIME_DISPLAY_SYSTEM_MESSAGE_TEXT - Constants.TIME_FADEOUT_TEXT)) / Constants.TIME_FADEOUT_TEXT;
            }

            public override string ToString()
            {
                return Text;
            }

            public void Destroy()
            {
                if (IsDispose)
                    return;

                IsDispose = true;
                _renderedText?.Destroy();
            }
        }
    }
}