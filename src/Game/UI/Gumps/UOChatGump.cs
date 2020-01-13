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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    class UOChatGump : Gump
    {
        private readonly ScrollArea _area;
        private readonly Label _currentChannelLabel;
        private string _selectedChannelText;
        private ChannelCreationBox _channelCreationBox;
        

        private readonly List<ChannelListItemControl> _channelList = new List<ChannelListItemControl>();

        public UOChatGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            WantUpdateSize = false;
            Width = 345;
            Height = 390;

            Add(new ResizePic(0x0A28)
            {
                Width = Width,
                Height = Height
            });

            int startY = 25;

            Label text = new Label("Channels", false, 0x0386, 345, 2, FontStyle.None, TEXT_ALIGN_TYPE.TS_CENTER)
            {
                Y = startY
            };
            Add(text);

            startY += 40;

            Add(new BorderControl(61, startY - 3, 220 + 8, 200 + 6,3));
            Add(new AlphaBlendControl(0){ X = 64, Y = startY, Width = 220, Height = 200});
            _area = new ScrollArea(64, startY, 220, 200, true)
            {
                ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways
            };

            foreach (var k in UOChatManager.Channels)
            {
                var chan = new ChannelListItemControl(k.Key, 195);
                _area.Add(chan);
                _channelList.Add(chan);
            }

            Add(_area);

            startY = 275;

            text = new Label("Your current channel:", false, 0x0386, 345, 2, FontStyle.None, TEXT_ALIGN_TYPE.TS_CENTER)
            {
                Y = startY
            };
            Add(text);

            startY += 25;

            _currentChannelLabel = new Label(UOChatManager.CurrentChannelName, false, 0x0386, 345, 2, FontStyle.None, TEXT_ALIGN_TYPE.TS_CENTER)
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

            text = new Label("Join", false, 0x0386, 0, 2, FontStyle.None, TEXT_ALIGN_TYPE.TS_LEFT)
            {
                X = 65,
                Y = startY
            };
            Add(text);

            text = new Label("Leave", false, 0x0386, 0, 2, FontStyle.None, TEXT_ALIGN_TYPE.TS_LEFT)
            {
                X = 140,
                Y = startY
            };
            Add(text);

            text = new Label("Create", false, 0x0386, 0, 2, FontStyle.None, TEXT_ALIGN_TYPE.TS_LEFT)
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
                        NetClient.Socket.Send(new PChatJoinCommand(_selectedChannelText));
                    break;
                case 1: // leave
                    NetClient.Socket.Send(new PChatLeaveChannelCommand());
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
            if (_currentChannelLabel.Text != UOChatManager.CurrentChannelName)
                _currentChannelLabel.Text = UOChatManager.CurrentChannelName;
        }

        public void Update()
        {
            foreach (ChannelListItemControl control in _channelList)
            {
                control.Dispose();
            }

            _channelList.Clear();

            foreach (var k in UOChatManager.Channels)
            {
                var c = new ChannelListItemControl(k.Key, 195);
                _area.Add(c);
                _channelList.Add(c);
            }
        }

        private void OnChannelSelected(string text)
        {
            _selectedChannelText = text;
            foreach (ChannelListItemControl control in _channelList)
            {
                control.IsSelected = control.Text == text;
            }
        }

        class ChannelCreationBox : Control
        {
            private readonly TextBox _textBox;

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

                Add(new AlphaBlendControl(0){ Width = Width, Height = Height});

                Add(new BorderControl(0, 0, Width, ROW_HEIGHT, BORDER_SIZE));

                Label text = new Label("Create a channel:", true, 0x23, Width - 4, 1)
                {
                    X = 6,
                    Y = BORDER_SIZE
                };
                Add(text);

                Add(new BorderControl(0, ROW_HEIGHT - BORDER_SIZE, Width, ROW_HEIGHT, BORDER_SIZE));

                text = new Label("Name:", true, 0x23, Width - 4, 1)
                {
                    X = 6,
                    Y = ROW_HEIGHT
                };
                Add(text);

                _textBox = new TextBox(1, -1, 0, Width - 50, hue: 0x0481, style: FontStyle.Fixed)
                {
                    X = 45, 
                    Y = ROW_HEIGHT,
                    Width = Width - 50,
                    Height = ROW_HEIGHT - BORDER_SIZE * 2
                };
                Add(_textBox);

                Add(new BorderControl(0, ROW_HEIGHT * 2 - BORDER_SIZE * 2, Width, ROW_HEIGHT, BORDER_SIZE));

                // close
                Add(new Button(0, 0x0A94, 0x0A95, 0x0A94)
                {
                    X = (Width - 19) - BORDER_SIZE ,
                    Y = Height - 19 + BORDER_SIZE * 2,
                    ButtonAction = ButtonAction.Activate
                });

                // ok
                Add(new Button(1, 0x0A9A, 0x0A9B, 0x0A9A)
                {
                    X = (Width - 19 * 2) - BORDER_SIZE,
                    Y = Height - 19 + BORDER_SIZE * 2,
                    ButtonAction = ButtonAction.Activate
                });
            }


            public override void OnButtonClick(int buttonID)
            {
                if (buttonID == 0) // close
                {
                   
                }
                else if (buttonID == 1) // ok
                {
                    NetClient.Socket.Send(new PChatCreateChannelCommand(_textBox.Text));
                }

                Dispose();
            }
        }

        class ChannelListItemControl : Control
        {
            private readonly Label _label;
            private bool _isSelected;

            public ChannelListItemControl(string text, int width)
            {
                Text = text;
                Width = width;
                Add(_label = new Label(text, false, 0x49, Width, font: 3)
                {
                    X = 3
                });
            }

            public readonly string Text;

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

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                base.OnMouseUp(x, y, button);

                if (RootParent is UOChatGump g)
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
                ResetHueVector();

                if (MouseIsOver)
                {
                    batcher.Draw2D(Texture2DCache.GetTexture(Color.Cyan), x, y, Width, Height, ref _hueVector);
                }

                return base.Draw(batcher, x, y);
            }
        }
    }
}
