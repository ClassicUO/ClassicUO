using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class ChatControl : GumpControl
    {

        const int MAX_MESSAGE_LENGHT = 100;

        private TextBox _textBox;
        private List<ChatLineTime> _textEntries;
        private List<Tuple<MessageType, string>> _messageHistory;
        private Input.InputManager _uiManager;
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

        public ChatControl(int x, int y, int w, int h) : base()
        {
            X = x; Y = y;
            Width = w; Height = h;

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
                var height = IO.Resources.Fonts.GetHeightUnicode(1, "ABC", Width, 0, (ushort)(FontStyle.BlackBorder | FontStyle.Fixed));

                _textBox = new TextBox(1, MAX_MESSAGE_LENGHT, Width, true, FontStyle.BlackBorder | FontStyle.Fixed, 33)
                {
                    X = 0,
                    Y = Height - height - 3,
                    Width = Width,
                    Height = height - 3
                };

                
                Mode = MessageType.Regular;

                AddChildren(_textBox);
            }

            for( int i = 0; i < _textEntries.Count; i++)
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
            int y = _textBox.Y - (int)position.Y - 6;

            for (int i = _textEntries.Count -1; i >= 0; i--)
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
            Network.NetClient.Socket.Send(new Network.PASCIISpeechRequest(text, speechType, MessageFont.Normal, hue));
        }


        class ChatLineTime : Interfaces.IUpdateable, Interfaces.IDrawableUI, IDisposable
        {
            private RenderedText _renderedText;
            private float _createdTime = float.MinValue;
            private int _width;

            const float TIME_DISPLAY = 10000.0f;
            const float TIME_FADEOUT = 4000.0f;

            public ChatLineTime(string text, int width)
            {
                _renderedText = new RenderedText()
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


            public bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
            {
                return _renderedText.Draw(spriteBatch, position, RenderExtentions.GetHueVector(0, false, Alpha < 1, true));
            }

            public void Update(double totalMS, double frameMS)
            {
                if (_createdTime == float.MinValue)
                    _createdTime = (float)totalMS;
                float time = (float)totalMS - _createdTime;
                if (time > TIME_DISPLAY)
                    IsExpired = true;
                else if (time > TIME_DISPLAY - TIME_FADEOUT)
                    Alpha = 1.0f - (time - (TIME_DISPLAY - TIME_FADEOUT)) / TIME_FADEOUT;
            }

            public void Dispose()
            {
                _renderedText.Dispose();
            }

            public override string ToString()
            {
                return Text;
            }
        }
    }
}
