#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Interfaces;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility.Collections;
using ClassicUO.Utility.Platforms;

using SDL2;

namespace ClassicUO.Game.UI.Gumps
{
    enum ChatMode
    {
        Default,
        Whisper,
        Emote,
        Yell,
        Party,
        //PartyPrivate,
        Guild,
        Alliance,
        ClientCommand,
        UOAMChat,
        Prompt,
        UOChat,
    }

    internal class SystemChatControl : Control
    {
        private const int MAX_MESSAGE_LENGHT = 100;
        private readonly Label _currentChatModeLabel;
        private static readonly List<Tuple<ChatMode, string>> _messageHistory = new List<Tuple<ChatMode, string>>();
        private static int _messageHistoryIndex = -1;

        private readonly Deque<ChatLineTime> _textEntries;
        private readonly AlphaBlendControl _trans;

        public readonly TextBox TextBoxControl;

        private bool _isActive;
        private ChatMode _mode = ChatMode.Default;

     

        public SystemChatControl(int x, int y, int w, int h)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            _textEntries = new Deque<ChatLineTime>();
            CanCloseWithRightClick = false;
            AcceptMouseInput = false;
            AcceptKeyboardInput = false;

            int height = FontsLoader.Instance.GetHeightUnicode(ProfileManager.Current.ChatFont, "123ABC", Width, 0, (ushort) (FontStyle.BlackBorder | FontStyle.Fixed));

            TextBoxControl = new TextBox(ProfileManager.Current.ChatFont, MAX_MESSAGE_LENGHT, Width, Width, true, FontStyle.BlackBorder | FontStyle.Fixed, 33)
            {
                X = 0,
                Y = Height - height - 3,
                Width = Width,
                Height = height - 3
            };

            float gradientTransparency = (ProfileManager.Current != null && ProfileManager.Current.HideChatGradient) ? 1.0f : 0.5f;

            Add(_trans = new AlphaBlendControl(gradientTransparency)
            {
                X = TextBoxControl.X,
                Y = TextBoxControl.Y,
                Width = Width,
                Height = height + 5,
                IsVisible = !ProfileManager.Current.ActivateChatAfterEnter,
                AcceptMouseInput = true
            });
            Add(TextBoxControl);

            Add(_currentChatModeLabel = new Label(string.Empty, true, 0, style: FontStyle.BlackBorder)
            {
                X = TextBoxControl.X,
                Y = TextBoxControl.Y,
                IsVisible = false
            });

            WantUpdateSize = false;

            MessageManager.MessageReceived += ChatOnMessageReceived;
            Mode = ChatMode.Default;

            IsActive = !ProfileManager.Current.ActivateChatAfterEnter;

            SetFocus();
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = TextBoxControl.IsVisible = TextBoxControl.IsEditable = value;

                if (_isActive)
                {
                    _trans.IsVisible = true;
                    _trans.Y = TextBoxControl.Y;
                    TextBoxControl.Width = _trans.Width;
                    TextBoxControl.SetText(string.Empty);
                    TextBoxControl.SetKeyboardFocus();
                }
                else
                {
                    int height = FontsLoader.Instance.GetHeightUnicode(ProfileManager.Current.ChatFont, "123ABC", Width, 0, (ushort) (FontStyle.BlackBorder | FontStyle.Fixed));
                    TextBoxControl.Width = 1;
                    _trans.Y = TextBoxControl.Y + height + 3;
                }
            }
        }

    
        public ChatMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;

                if (IsActive)
                {
                    switch (value)
                    {
                        case ChatMode.Default:
                            DisposeChatModePrefix();
                            TextBoxControl.Hue = ProfileManager.Current.SpeechHue;
                            TextBoxControl.SetText(string.Empty);

                            break;

                        case ChatMode.Whisper:
                            AppendChatModePrefix("[Whisper]: ", ProfileManager.Current.WhisperHue, TextBoxControl.Text);

                            break;

                        case ChatMode.Emote:
                            AppendChatModePrefix("[Emote]: ", ProfileManager.Current.EmoteHue, TextBoxControl.Text);

                            break;

                        case ChatMode.Yell:
                            AppendChatModePrefix("[Yell]: ", ProfileManager.Current.YellHue, TextBoxControl.Text);

                            break;

                        case ChatMode.Party:
                            AppendChatModePrefix("[Party]: ", ProfileManager.Current.PartyMessageHue, TextBoxControl.Text);

                            break;
                        
                        case ChatMode.Guild:
                            AppendChatModePrefix("[Guild]: ", ProfileManager.Current.GuildMessageHue, TextBoxControl.Text);

                            break;

                        case ChatMode.Alliance:
                            AppendChatModePrefix("[Alliance]: ", ProfileManager.Current.AllyMessageHue, TextBoxControl.Text);

                            break;

                        case ChatMode.ClientCommand:
                            AppendChatModePrefix("[Command]: ", 1161, TextBoxControl.Text);

                            break;

                        case ChatMode.UOAMChat:
                            DisposeChatModePrefix();
                            AppendChatModePrefix("[UOAM]: ", 83, TextBoxControl.Text);

                            break;
                        case ChatMode.UOChat:
                            DisposeChatModePrefix();
                            AppendChatModePrefix("Chat: ", ProfileManager.Current.ChatMessageHue, TextBoxControl.Text);
                            break;
                    }
                }
            }
        }

        public void SetFocus()
        {
            TextBoxControl.IsEditable = true;
            TextBoxControl.SetKeyboardFocus();
            TextBoxControl.IsEditable = _isActive;
        }

        public void ToggleChatVisibility()
        {
            IsActive = !IsActive;
        }

        private void ChatOnMessageReceived(object sender, UOMessageEventArgs e)
        {
            switch (e.Type)
            {
                case MessageType.Regular when e.Parent == null || !SerialHelper.IsValid(e.Parent.Serial):
                case MessageType.System:
                    if (!string.IsNullOrEmpty(e.Name) && e.Name.ToLowerInvariant() != "system")
                        AddLine($"{e.Name}: {e.Text}", e.Font, e.Hue, e.IsUnicode);
                    else
                        AddLine(e.Text, e.Font, e.Hue, e.IsUnicode);

                    break;
                case MessageType.Label when e.Parent == null || !SerialHelper.IsValid(e.Parent.Serial):
                    AddLine(e.Text, e.Font, e.Hue, e.IsUnicode);

                    break;

                case MessageType.Party:
                    AddLine($"[Party][{e.Name}]: {e.Text}", e.Font, ProfileManager.Current.PartyMessageHue, e.IsUnicode);

                    break;

                case MessageType.Guild:
                    AddLine($"[Guild][{e.Name}]: {e.Text}", e.Font, ProfileManager.Current.GuildMessageHue, e.IsUnicode);

                    break;

                case MessageType.Alliance:
                    AddLine($"[Alliance][{e.Name}]: {e.Text}", e.Font, ProfileManager.Current.AllyMessageHue, e.IsUnicode);

                    break;
            }
        }

        public override void Dispose()
        {
            MessageManager.MessageReceived -= ChatOnMessageReceived;
            base.Dispose();
        }

        private void AppendChatModePrefix(string labelText, ushort hue, string text)
        {
            if (!_currentChatModeLabel.IsVisible)
            {
                _currentChatModeLabel.Hue = hue;
                _currentChatModeLabel.Text = labelText;
                _currentChatModeLabel.IsVisible = true;
                _currentChatModeLabel.Location = TextBoxControl.Location;
                TextBoxControl.X = _currentChatModeLabel.Width;
                TextBoxControl.Hue = hue;

                int idx = string.IsNullOrEmpty(text) ? -1 : TextBoxControl.Text.IndexOf(text);
                string str = string.Empty;
                if (idx > 0)
                {
                    str = TextBoxControl.Text.Substring(idx, TextBoxControl.Text.Length - labelText.Length - 1);
                }

                TextBoxControl.SetText(str);
            }
        }

        private void DisposeChatModePrefix()
        {
            if (_currentChatModeLabel.IsVisible)
            {
                TextBoxControl.Hue = 33;
                TextBoxControl.X -= _currentChatModeLabel.Width;
                _currentChatModeLabel.IsVisible = false;
            }
        }

        public void AddLine(string text, byte font, ushort hue, bool isunicode)
        {
            if (_textEntries.Count >= 30)
                _textEntries.RemoveFromFront().Destroy();

            _textEntries.AddToBack(new ChatLineTime(text, font, isunicode, hue));
        }

        internal void Resize()
        {
            if (TextBoxControl != null)
            {
                int height = FontsLoader.Instance.GetHeightUnicode(ProfileManager.Current.ChatFont, "123ABC", Width, 0, (ushort) (FontStyle.BlackBorder | FontStyle.Fixed));
                TextBoxControl.Y = Height - height - 3;
                TextBoxControl.Width = IsActive ? Width : 1;
                TextBoxControl.Height = height - 3;
                _trans.Location = TextBoxControl.Location;
                _trans.Width = Width;
                _trans.Height = height + 5;
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

            if (Mode == ChatMode.Default && IsActive)
            {
                if (TextBoxControl.Text.Length > 0)
                {
                    switch (TextBoxControl.Text[0])
                    {                  
                        case '/':

                            int pos = 1;

                            while (pos < TextBoxControl.Text.Length && TextBoxControl.Text[pos] != ' ')
                            {
                                pos++;
                            }

                            if (pos < TextBoxControl.Text.Length && int.TryParse(TextBoxControl.Text.Substring(1, pos), out int index) && index > 0 && index < 11)
                            {
                                if (World.Party.Members[index - 1] != null && World.Party.Members[index - 1].Serial != 0)
                                {
                                    AppendChatModePrefix($"[Tell] [{World.Party.Members[index - 1].Name}]: ", ProfileManager.Current.PartyMessageHue, string.Empty);
                                }
                                else
                                {
                                    AppendChatModePrefix("[Tell] []: ", ProfileManager.Current.PartyMessageHue, string.Empty);
                                }

                                Mode = ChatMode.Party;
                                TextBoxControl.Text = $"{index} ";
                            }
                            else
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

                        case ',' when UOChatManager.ChatIsEnabled == CHAT_STATUS.ENABLED:
                            Mode = ChatMode.UOChat;
                            break;


                        case ':' when TextBoxControl.Text.Length > 1 && TextBoxControl.Text[1] == ' ':
                            Mode = ChatMode.Emote;
                            break;
                        case ';' when TextBoxControl.Text.Length > 1 && TextBoxControl.Text[1] == ' ':
                            Mode = ChatMode.Whisper;
                            break;
                        case '!' when TextBoxControl.Text.Length > 1 && TextBoxControl.Text[1] == ' ':
                            Mode = ChatMode.Yell;
                            break;
                    }
                }
            }
            else if (Mode == ChatMode.ClientCommand && TextBoxControl.Text.Length == 1 && TextBoxControl.Text[0] == '-')
                Mode = ChatMode.UOAMChat;

            if (ProfileManager.Current.SpeechHue != TextBoxControl.Hue) 
                TextBoxControl.Hue = ProfileManager.Current.SpeechHue;

            _trans.Alpha = (ProfileManager.Current != null && ProfileManager.Current.HideChatGradient) ? 1.0f : 0.5f;

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            int yy = TextBoxControl.Y + y - 20;

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
                case SDL.SDL_Keycode.SDLK_q when Keyboard.IsModPressed(mod, SDL.SDL_Keymod.KMOD_CTRL) && _messageHistoryIndex > -1 && !ProfileManager.Current.DisableCtrlQWBtn:

                    var scene = Client.Game.GetScene<GameScene>();
                    if (scene == null)
                        return;

                    if (scene.Macros.FindMacro(key, false, true, false) != null)
                        return;

                    if (!IsActive)
                        IsActive = true;

                    if (_messageHistoryIndex > 0)
                        _messageHistoryIndex--;

                    Mode = _messageHistory[_messageHistoryIndex].Item1;
                    TextBoxControl.SetText(_messageHistory[_messageHistoryIndex].Item2);

                    break;

                case SDL.SDL_Keycode.SDLK_w when Keyboard.IsModPressed(mod, SDL.SDL_Keymod.KMOD_CTRL) && !ProfileManager.Current.DisableCtrlQWBtn:

                    scene = Client.Game.GetScene<GameScene>();
                    if (scene == null)
                        return;

                    if (scene.Macros.FindMacro(key, false, true, false) != null)
                        return;

                    if (!IsActive)
                        IsActive = true;

                    if (_messageHistoryIndex < _messageHistory.Count - 1)
                    {
                        _messageHistoryIndex++;
                        Mode = _messageHistory[_messageHistoryIndex].Item1;
                        TextBoxControl.SetText(_messageHistory[_messageHistoryIndex].Item2);
                    }
                    else
                        TextBoxControl.SetText(string.Empty);

                    break;

                case SDL.SDL_Keycode.SDLK_BACKSPACE when Keyboard.IsModPressed(mod, SDL.SDL_Keymod.KMOD_NONE) && string.IsNullOrEmpty(TextBoxControl.Text):
                    Mode = ChatMode.Default;

                    break;

                case SDL.SDL_Keycode.SDLK_ESCAPE when MessageManager.PromptData.Prompt != ConsolePrompt.None:

                    if (MessageManager.PromptData.Prompt == ConsolePrompt.ASCII)
                        NetClient.Socket.Send(new PASCIIPromptResponse(string.Empty, true));
                    else if (MessageManager.PromptData.Prompt == ConsolePrompt.Unicode)
                        NetClient.Socket.Send(new PUnicodePromptResponse(string.Empty, "ENU", true));
                    MessageManager.PromptData = default;

                    break;
            }
        }

        public override void OnKeyboardReturn(int textID, string text)
        {
            if ((!IsActive && ProfileManager.Current.ActivateChatAfterEnter) || (Mode != ChatMode.Default && string.IsNullOrEmpty(text)))
            {
                TextBoxControl.SetText(string.Empty);
                text = string.Empty;
                Mode = ChatMode.Default;
            }

            if (string.IsNullOrEmpty(text))
            {
                return;
            }


            ChatMode sentMode = Mode;
            TextBoxControl.SetText(string.Empty);
            _messageHistory.Add(new Tuple<ChatMode, string>(Mode, text));
            _messageHistoryIndex = _messageHistory.Count;
            Mode = ChatMode.Default;

            if (MessageManager.PromptData.Prompt != ConsolePrompt.None)
            {
                if (MessageManager.PromptData.Prompt == ConsolePrompt.ASCII)
                    NetClient.Socket.Send(new PASCIIPromptResponse(text, text.Length < 1));
                else if (MessageManager.PromptData.Prompt == ConsolePrompt.Unicode)
                    NetClient.Socket.Send(new PUnicodePromptResponse(text, "ENU", text.Length < 1));

                MessageManager.PromptData = default;
            }
            else
            {
                switch (sentMode)
                {
                    case ChatMode.Default:
                        GameActions.Say(text, ProfileManager.Current.SpeechHue);
                        break;

                    case ChatMode.Whisper:
                        GameActions.Say(text, ProfileManager.Current.WhisperHue, MessageType.Whisper);
                        break;

                    case ChatMode.Emote:
                        text = "*" + text + "*";    
                        GameActions.Say(text, ProfileManager.Current.EmoteHue, MessageType.Emote);
                        break;

                    case ChatMode.Yell:
                        GameActions.Say(text, ProfileManager.Current.YellHue, MessageType.Yell);
                        break;

                    case ChatMode.Party:

                        switch (text.ToLower())
                        {
                            case "add":
                                if (World.Party.Leader == 0 || World.Party.Leader == World.Player)
                                    GameActions.RequestPartyInviteByTarget();
                                else
                                    MessageManager.HandleMessage(null, "You are not party leader.", "System", 0xFFFF, MessageType.Regular, 3);

                                break;

                            case "loot":

                                if (World.Party.Leader != 0)
                                    World.Party.CanLoot = !World.Party.CanLoot;
                                else
                                    MessageManager.HandleMessage(null, "You are not in a party.", "System", 0xFFFF, MessageType.Regular, 3);


                                break;

                            case "quit":

                                if (World.Party.Leader == 0)
                                    MessageManager.HandleMessage(null, "You are not in a party.", "System", 0xFFFF, MessageType.Regular, 3);
                                else
                                {
                                    GameActions.RequestPartyQuit();

                                    //for (int i = 0; i < World.Party.Members.Length; i++)
                                    //{
                                    //    if (World.Party.Members[i] != null && World.Party.Members[i].Serial != 0)
                                    //        GameActions.RequestPartyRemoveMember(World.Party.Members[i].Serial);
                                    //}
                                }

                                break;

                            case "accept":

                                if (World.Party.Leader == 0 && World.Party.Inviter != 0)
                                {
                                    GameActions.RequestPartyAccept(World.Party.Inviter);
                                    World.Party.Leader = World.Party.Inviter;
                                    World.Party.Inviter = 0;
                                }
                                else
                                    MessageManager.HandleMessage(null, "No one has invited you to be in a party.", "System", 0xFFFF, MessageType.Regular, 3);

                                break;

                            case "decline":

                                if (World.Party.Leader == 0 && World.Party.Inviter != 0)
                                {
                                    NetClient.Socket.Send(new PPartyDecline(World.Party.Inviter));
                                    World.Party.Leader = 0;
                                    World.Party.Inviter = 0;
                                }
                                else
                                    MessageManager.HandleMessage(null, "No one has invited you to be in a party.", "System", 0xFFFF, MessageType.Regular, 3);


                                break;

                            default:

                                if (World.Party.Leader != 0)
                                {
                                    uint serial = 0;

                                    int pos = 0;

                                    while (pos < text.Length && text[pos] != ' ')
                                    {
                                        pos++;
                                    }

                                    if (pos < text.Length)
                                    {
                                        if (int.TryParse(text.Substring(0, pos), out int index) && index > 0 && index < 11 && World.Party.Members[index - 1] != null && World.Party.Members[index - 1].Serial != 0)
                                            serial = World.Party.Members[index - 1].Serial;
                                    }

                                    GameActions.SayParty(text, serial);
                                }
                                else
                                {
                                    GameActions.Print($"Note to self: {text}", 0, MessageType.System, 3, false);
                                }

                                break;
                        }

                        break;
                    
                    case ChatMode.Guild:
                        GameActions.Say(text, ProfileManager.Current.GuildMessageHue, MessageType.Guild);

                        break;

                    case ChatMode.Alliance:
                        GameActions.Say(text, ProfileManager.Current.AllyMessageHue, MessageType.Alliance);

                        break;

                    case ChatMode.ClientCommand:
                        string[] tt = text.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                        CommandManager.Execute(tt[0], tt);

                        break;

                    case ChatMode.UOAMChat:
                        UoAssist.SignalMessage(text);

                        break;

                    case ChatMode.UOChat:
                        NetClient.Socket.Send(new PChatMessageCommand(text));
                        break;
                }
            }

            DisposeChatModePrefix();
        }

        private class ChatLineTime : IUpdateable
        {
            private readonly RenderedText _renderedText;
            private float _alpha;
            private float _createdTime;

            public ChatLineTime(string text, byte font, bool isunicode, ushort hue)
            {
                _renderedText = RenderedText.Create(text, hue, font, isunicode, FontStyle.BlackBorder, maxWidth: 320);
                _createdTime = Constants.TIME_DISPLAY_SYSTEM_MESSAGE_TEXT;
            }

            private string Text => _renderedText.Text;

            public bool IsDispose { get; private set; }

            public int TextHeight => _renderedText.Height;

            public void Update(double totalMS, double frameMS)
            {
                _createdTime -= (float) frameMS;

                if (_createdTime > 0 && _createdTime <= Constants.TIME_FADEOUT_TEXT) _alpha = 1.0f - _createdTime / Constants.TIME_FADEOUT_TEXT;

                if (_createdTime <= 0.0f)
                    Destroy();

                //else if (time > Constants.TIME_DISPLAY_SYSTEM_MESSAGE_TEXT - Constants.TIME_FADEOUT_TEXT)
                //    _alpha = (time - (Constants.TIME_DISPLAY_SYSTEM_MESSAGE_TEXT - Constants.TIME_FADEOUT_TEXT)) / Constants.TIME_FADEOUT_TEXT;
            }


            public bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                return _renderedText.Draw(batcher, x, y /*, ShaderHuesTraslator.GetHueVector(0, false, _alpha, true)*/);
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