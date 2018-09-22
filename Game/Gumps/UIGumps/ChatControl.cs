#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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
using IUpdateable = ClassicUO.Interfaces.IUpdateable;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class ChatControl : GumpControl
    {
        private const int MAX_MESSAGE_LENGHT = 100;

        private TextBox _textBox;
        private readonly List<ChatLineTime> _textEntries;
        private readonly List<Tuple<MessageType, string>> _messageHistory;
        private InputManager _uiManager;
        private int _messageHistoryIndex = -1;
        private Serial _privateMsgSerial = 0;
        private string _privateMsgName;

        private MessageType _mode = MessageType.Regular;

        private MessageType Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                switch (value)
                {
                    case MessageType.Regular:
                        _textBox.SetText(string.Empty);
                        break;
                }
            }
        }

        public ChatControl(int x, int y, int w, int h)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;

            _textEntries = new List<ChatLineTime>();
            _messageHistory = new List<Tuple<MessageType, string>>();

            CanCloseWithRightClick = false;
            AcceptMouseInput = false;
            AcceptKeyboardInput = false;
        }

        public void AddLine()
        {
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (_textBox == null)
            {
                int height = Fonts.GetHeightUnicode(1, "ABC", Width, 0,
                    (ushort) (FontStyle.BlackBorder | FontStyle.Fixed));

                _textBox = new TextBox(1, MAX_MESSAGE_LENGHT, Width, true, FontStyle.BlackBorder | FontStyle.Fixed, 33)
                {
                    X = 0,
                    Y = Height - height - 3,
                    Width = Width,
                    Height = height - 3
                };


                Mode = MessageType.Regular;

                AddChildren(new CheckerTrans {X = _textBox.X, Y = _textBox.Y, Width = Width, Height = Height});
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

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            int y = _textBox.Y - (int) position.Y - 6;

            for (int i = _textEntries.Count - 1; i >= 0; i--)
            {
                y -= _textEntries[i].TextHeight;
                _textEntries[i].Draw(spriteBatch, new Vector3(position.X + 2, y, 0));
            }

            return base.Draw(spriteBatch, position, hue);
        }

        public override void OnKeybaordReturn(int textID, string text)
        {
            MessageType sentMode = Mode;
            MessageType speechType = MessageType.Regular;

            ushort hue = 0;
            _textBox.SetText(string.Empty);
            _messageHistory.Add(new Tuple<MessageType, string>(Mode, text));
            _messageHistoryIndex = _messageHistory.Count;
            Mode = MessageType.Regular;

            switch (sentMode)
            {
                case MessageType.Regular:
                    speechType = MessageType.Regular;
                    hue = 33;
                    break;
            }

            //GameActions.Say(text, hue, speechType, 0);
            NetClient.Socket.Send(new PASCIISpeechRequest(text, speechType, MessageFont.Normal, hue));
        }


        private class ChatLineTime : IUpdateable, IDrawableUI, IDisposable
        {
            private readonly RenderedText _renderedText;
            private float _createdTime = float.MinValue;
            private int _width;

            private const float TIME_DISPLAY = 10000.0f;
            private const float TIME_FADEOUT = 4000.0f;

            public ChatLineTime(string text, int width)
            {
                _renderedText = new RenderedText
                {
                    IsUnicode = true,
                    Font = 1,
                    MaxWidth = width,
                    FontStyle = FontStyle.BlackBorder
                };

                _renderedText.Text = text;
                _width = width;
            }

            public string Text => _renderedText.Text;
            public bool IsExpired { get; private set; }
            public float Alpha { get; private set; } = 1.0f;
            public bool AllowedToDraw { get; set; } = true;
            public int TextHeight => _renderedText.Height;
            public SpriteTexture Texture { get; set; }


            public bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null) =>
                _renderedText.Draw(spriteBatch, position, RenderExtentions.GetHueVector(0, false, Alpha < 1, true));

            public void Update(double totalMS, double frameMS)
            {
                if (_createdTime == float.MinValue)
                    _createdTime = (float) totalMS;
                float time = (float) totalMS - _createdTime;
                if (time > TIME_DISPLAY)
                    IsExpired = true;
                else if (time > TIME_DISPLAY - TIME_FADEOUT)
                    Alpha = 1.0f - (time - (TIME_DISPLAY - TIME_FADEOUT)) / TIME_FADEOUT;
            }

            public void Dispose()
            {
                _renderedText.Dispose();
            }

            public override string ToString() => Text;
        }
    }
}