#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System.Collections.Generic;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ChatGump : Gump
    {
        private ChannelCreationBox _channelCreationBox;

        private readonly List<ChannelListItemControl> _channelList = new List<ChannelListItemControl>();
        private readonly Label _currentChannelLabel;
        private readonly DataBox _databox;
        private string _selectedChannelText;

        public ChatGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            WantUpdateSize = false;
            Width = 345;
            Height = 390;

            Add
            (
                new ResizePic(0x0A28)
                {
                    Width = Width,
                    Height = Height
                }
            );

            int startY = 25;

            Label text = new Label
            (
                ResGumps.Channels,
                false,
                0x0386,
                345,
                2,
                FontStyle.None,
                TEXT_ALIGN_TYPE.TS_CENTER
            )
            {
                Y = startY
            };

            Add(text);

            startY += 40;

            Add
            (
                new BorderControl
                (
                    61,
                    startY - 3,
                    220 + 8,
                    200 + 6,
                    3
                )
            );

            Add(new AlphaBlendControl(1f) { X = 64, Y = startY, Width = 220, Height = 200 });

            ScrollArea area = new ScrollArea
            (
                64,
                startY,
                220,
                200,
                true
            )
            {
                ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways
            };

            Add(area);

            _databox = new DataBox(0, 0, 1, 1);
            _databox.WantUpdateSize = true;
            area.Add(_databox);

            foreach (KeyValuePair<string, ChatChannel> k in ChatManager.Channels)
            {
                ChannelListItemControl chan = new ChannelListItemControl(k.Key, 195);
                _databox.Add(chan);
                _channelList.Add(chan);
            }

            _databox.ReArrangeChildren();

            startY = 275;

            text = new Label
            (
                ResGumps.YourCurrentChannel,
                false,
                0x0386,
                345,
                2,
                FontStyle.None,
                TEXT_ALIGN_TYPE.TS_CENTER
            )
            {
                Y = startY
            };

            Add(text);

            startY += 25;

            _currentChannelLabel = new Label
            (
                ChatManager.CurrentChannelName,
                false,
                0x0386,
                345,
                2,
                FontStyle.None,
                TEXT_ALIGN_TYPE.TS_CENTER
            )
            {
                Y = startY
            };

            Add(_currentChannelLabel);


            startY = 337;

            Button button = new Button(0, 0x0845, 0x0846, 0x0845)
            {
                X = 48,
                Y = startY + 5,
                ButtonAction = ButtonAction.Activate
            };

            Add(button);

            button = new Button(1, 0x0845, 0x0846, 0x0845)
            {
                X = 123,
                Y = startY + 5,
                ButtonAction = ButtonAction.Activate
            };

            Add(button);

            button = new Button(2, 0x0845, 0x0846, 0x0845)
            {
                X = 216,
                Y = startY + 5,
                ButtonAction = ButtonAction.Activate
            };

            Add(button);

            text = new Label
            (
                ResGumps.Join,
                false,
                0x0386,
                0,
                2
            )
            {
                X = 65,
                Y = startY
            };

            Add(text);

            text = new Label
            (
                ResGumps.Leave,
                false,
                0x0386,
                0,
                2
            )
            {
                X = 140,
                Y = startY
            };

            Add(text);

            text = new Label
            (
                ResGumps.Create,
                false,
                0x0386,
                0,
                2
            )
            {
                X = 233,
                Y = startY
            };

            Add(text);
        }

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 0: // join
                    if (!string.IsNullOrEmpty(_selectedChannelText))
                    {
                        NetClient.Socket.Send_ChatJoinCommand(_selectedChannelText);
                    }

                    break;

                case 1: // leave
                    NetClient.Socket.Send_ChatLeaveChannelCommand();

                    break;

                case 2: // create
                    if (_channelCreationBox == null || _channelCreationBox.IsDisposed)
                    {
                        _channelCreationBox = new ChannelCreationBox(Width / 2, Height / 2);
                        Add(_channelCreationBox);
                    }

                    break;
            }
        }

        public void UpdateConference()
        {
            if (_currentChannelLabel.Text != ChatManager.CurrentChannelName)
            {
                _currentChannelLabel.Text = ChatManager.CurrentChannelName;
            }
        }

        protected override void UpdateContents()
        {
            foreach (ChannelListItemControl control in _channelList)
            {
                control.Dispose();
            }

            _channelList.Clear();

            foreach (KeyValuePair<string, ChatChannel> k in ChatManager.Channels)
            {
                ChannelListItemControl c = new ChannelListItemControl(k.Key, 195);
                _databox.Add(c);
                _channelList.Add(c);
            }

            _databox.WantUpdateSize = true;
            _databox.ReArrangeChildren();
        }

        private void OnChannelSelected(string text)
        {
            _selectedChannelText = text;

            foreach (ChannelListItemControl control in _channelList)
            {
                control.IsSelected = control.Text == text;
            }
        }

        private class ChannelCreationBox : Control
        {
            private readonly StbTextBox _textBox;

            public ChannelCreationBox(int x, int y)
            {
                CanMove = true;
                AcceptMouseInput = true;
                AcceptKeyboardInput = false;

                Width = 200;
                Height = 60;
                X = x - Width / 2;
                Y = y - Height / 2;


                const int BORDER_SIZE = 3;
                const int ROW_HEIGHT = 25;

                Add(new AlphaBlendControl(1f) { Width = Width, Height = Height });

                Add
                (
                    new BorderControl
                    (
                        0,
                        0,
                        Width,
                        ROW_HEIGHT,
                        BORDER_SIZE
                    )
                );

                Label text = new Label
                (
                    ResGumps.CreateAChannel,
                    true,
                    0x23,
                    Width - 4,
                    1
                )
                {
                    X = 6,
                    Y = BORDER_SIZE
                };

                Add(text);

                Add
                (
                    new BorderControl
                    (
                        0,
                        ROW_HEIGHT - BORDER_SIZE,
                        Width,
                        ROW_HEIGHT,
                        BORDER_SIZE
                    )
                );

                text = new Label
                (
                    ResGumps.Name,
                    true,
                    0x23,
                    Width - 4,
                    1
                )
                {
                    X = 6,
                    Y = ROW_HEIGHT
                };

                Add(text);

                _textBox = new StbTextBox
                (
                    1,
                    -1,
                    Width - 50,
                    hue: 0x0481,
                    style: FontStyle.Fixed
                )
                {
                    X = 45,
                    Y = ROW_HEIGHT,
                    Width = Width - 50,
                    Height = ROW_HEIGHT - BORDER_SIZE * 2
                };

                Add(_textBox);

                Add
                (
                    new BorderControl
                    (
                        0,
                        ROW_HEIGHT * 2 - BORDER_SIZE * 2,
                        Width,
                        ROW_HEIGHT,
                        BORDER_SIZE
                    )
                );

                // close
                Add
                (
                    new Button(0, 0x0A94, 0x0A95, 0x0A94)
                    {
                        X = Width - 19 - BORDER_SIZE,
                        Y = Height - 19 + BORDER_SIZE * 2,
                        ButtonAction = ButtonAction.Activate
                    }
                );

                // ok
                Add
                (
                    new Button(1, 0x0A9A, 0x0A9B, 0x0A9A)
                    {
                        X = Width - 19 * 2 - BORDER_SIZE,
                        Y = Height - 19 + BORDER_SIZE * 2,
                        ButtonAction = ButtonAction.Activate
                    }
                );
            }


            public override void OnButtonClick(int buttonID)
            {
                if (buttonID == 0) // close
                {
                }
                else if (buttonID == 1) // ok
                {
                    NetClient.Socket.Send_ChatCreateChannelCommand(_textBox.Text);
                }

                Dispose();
            }
        }

        private class ChannelListItemControl : Control
        {
            private bool _isSelected;
            private readonly Label _label;

            public ChannelListItemControl(string text, int width)
            {
                Text = text;
                Width = width;

                Add
                (
                    _label = new Label
                    (
                        text,
                        false,
                        0x49,
                        Width,
                        3
                    )
                    {
                        X = 3
                    }
                );

                Height = _label.Height;
            }

            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    if (_isSelected != value)
                    {
                        _isSelected = value;
                        _label.Hue = (ushort) (value ? 0x22 : 0x49);
                    }
                }
            }

            public readonly string Text;

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                base.OnMouseUp(x, y, button);

                if (RootParent is ChatGump g)
                {
                    g.OnChannelSelected(Text);
                }
            }

            protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
            {
                base.OnButtonClick(0);

                return true;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

                if (MouseIsOver)
                {
                    batcher.Draw
                    (
                        SolidColorTextureCache.GetTexture(Color.Cyan),
                        new Rectangle
                        (
                            x,
                            y,
                            Width, 
                            Height
                        ),                    
                        hueVector
                    );
                }

                return base.Draw(batcher, x, y);
            }
        }
    }
}