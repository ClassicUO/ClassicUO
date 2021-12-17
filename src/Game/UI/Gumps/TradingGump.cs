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

using System;
using ClassicUO.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal sealed class TradingGump : TextContainerGump
    {
        private uint _gold, _platinum, _hisGold, _hisPlatinum;
        private readonly Label[] _hisCoins = new Label[2];
        private GumpPic _hisPic;

        private bool _imAccepting, _heIsAccepting;

        private DataBox _myBox, _hisBox;
        private Checkbox _myCheckbox;
        private readonly Label[] _myCoins = new Label[2];
        private readonly StbTextBox[] _myCoinsEntries = new StbTextBox[2];
        private readonly string _name;

        public TradingGump(uint local, string name, uint id1, uint id2) : base(local, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;

            _name = name;

            ID1 = id1;
            ID2 = id2;

            BuildGump();
        }

        public uint ID1 { get; }
        public uint ID2 { get; }

        public uint Gold
        {
            get => _gold;
            set
            {
                if (_gold != value)
                {
                    _gold = value;

                    if (Client.Version >= ClientVersion.CV_704565)
                    {
                        _myCoins[0].Text = _gold.ToString();
                    }
                }
            }
        }

        public uint Platinum
        {
            get => _platinum;
            set
            {
                if (_platinum != value)
                {
                    _platinum = value;

                    if (Client.Version >= ClientVersion.CV_704565)
                    {
                        _myCoins[1].Text = _platinum.ToString();
                    }
                }
            }
        }

        public uint HisGold
        {
            get => _hisGold;
            set
            {
                if (_hisGold != value)
                {
                    _hisGold = value;

                    if (Client.Version >= ClientVersion.CV_704565)
                    {
                        _hisCoins[0].Text = _hisGold.ToString();
                    }
                }
            }
        }

        public uint HisPlatinum
        {
            get => _hisPlatinum;
            set
            {
                if (_hisPlatinum != value)
                {
                    _hisPlatinum = value;

                    if (Client.Version >= ClientVersion.CV_704565)
                    {
                        _hisCoins[1].Text = _hisPlatinum.ToString();
                    }
                }
            }
        }

        public bool ImAccepting
        {
            get => _imAccepting;
            set
            {
                if (_imAccepting != value)
                {
                    _imAccepting = value;
                    SetCheckboxes();
                }
            }
        }

        public bool HeIsAccepting
        {
            get => _heIsAccepting;
            set
            {
                if (_heIsAccepting != value)
                {
                    _heIsAccepting = value;
                    SetCheckboxes();
                }
            }
        }


        protected override void UpdateContents()
        {
            Entity container = World.Get(ID1);

            if (container == null)
            {
                return;
            }

            foreach (Control v in _myBox.Children)
            {
                v.Dispose();
            }

            ArtLoader loader = ArtLoader.Instance;

            for (LinkedObject i = container.Items; i != null; i = i.Next)
            {
                Item it = (Item) i;

                ItemGump g = new ItemGump
                (
                    it.Serial,
                    it.DisplayedGraphic,
                    it.Hue,
                    it.X,
                    it.Y
                )
                {
                    HighlightOnMouseOver = true
                };

                int x = g.X;
                int y = g.Y;

                var texture = loader.GetStaticTexture(it.DisplayedGraphic, out var bounds);

                if (texture != null)
                {
                    if (x + bounds.Width > _myBox.Width)
                    {
                        x = _myBox.Width - bounds.Width;
                    }

                    if (y + bounds.Height > _myBox.Height)
                    {
                        y = _myBox.Height - bounds.Height;
                    }
                }

                if (x < 0)
                {
                    x = 0;
                }

                if (y < 0)
                {
                    y = 0;
                }


                g.X = x;
                g.Y = y;

                _myBox.Add(g);
            }

            container = World.Get(ID2);

            if (container == null)
            {
                return;
            }

            foreach (Control v in _hisBox.Children)
            {
                v.Dispose();
            }

            for (LinkedObject i = container.Items; i != null; i = i.Next)
            {
                Item it = (Item) i;

                ItemGump g = new ItemGump
                (
                    it.Serial,
                    it.DisplayedGraphic,
                    it.Hue,
                    it.X,
                    it.Y
                )
                {
                    HighlightOnMouseOver = true
                };

                int x = g.X;
                int y = g.Y;

                var texture = loader.GetStaticTexture(it.DisplayedGraphic, out var bounds);

                if (texture != null)
                {
                    if (x + bounds.Width > _myBox.Width)
                    {
                        x = _myBox.Width - bounds.Width;
                    }

                    if (y + bounds.Height > _myBox.Height)
                    {
                        y = _myBox.Height - bounds.Height;
                    }
                }

                if (x < 0)
                {
                    x = 0;
                }

                if (y < 0)
                {
                    y = 0;
                }


                g.X = x;
                g.Y = y;

                _hisBox.Add(g);
            }
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                if (ItemHold.Enabled && !ItemHold.IsFixedPosition)
                {
                    if (_myBox != null && _myBox.Bounds.Contains(x, y))
                    {
                        var texture = ArtLoader.Instance.GetStaticTexture(ItemHold.DisplayedGraphic, out var bounds);

                        x -= _myBox.X;
                        y -= _myBox.Y;

                        if (texture != null)
                        {
                            x -= bounds.Width >> 1;
                            y -= bounds.Height >> 1;

                            if (x + bounds.Width > _myBox.Width)
                            {
                                x = _myBox.Width - bounds.Width;
                            }

                            if (y + bounds.Height > _myBox.Height)
                            {
                                y = _myBox.Height - bounds.Height;
                            }
                        }

                        if (x < 0)
                        {
                            x = 0;
                        }

                        if (y < 0)
                        {
                            y = 0;
                        }

                        GameActions.DropItem
                        (
                            ItemHold.Serial,
                            x,
                            y,
                            0,
                            ID1
                        );
                    }
                }
                else if (SelectedObject.Object is Item it)
                {
                    if (TargetManager.IsTargeting)
                    {
                        TargetManager.Target(it.Serial);
                        Mouse.CancelDoubleClick = true;

                        if (TargetManager.TargetingState == CursorTarget.SetTargetClientSide)
                        {
                            UIManager.Add(new InspectorGump(it));
                        }
                    }
                    else if (!DelayedObjectClickManager.IsEnabled)
                    {
                        Point off = Mouse.LDragOffset;

                        DelayedObjectClickManager.Set(it.Serial, Mouse.Position.X - off.X - ScreenCoordinateX, Mouse.Position.Y - off.Y - ScreenCoordinateY, Time.Ticks + Mouse.MOUSE_DELAY_DOUBLE_CLICK);
                    }
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            GameActions.CancelTrade(ID1);
        }

        private void SetCheckboxes()
        {
            _myCheckbox?.Dispose();
            _hisPic?.Dispose();

            int myX, myY, otherX, otherY;

            if (Client.Version >= ClientVersion.CV_704565)
            {
                myX = 37;
                myY = 29;

                otherX = 258;
                otherY = 240;
            }
            else
            {
                myX = 52;
                myY = 29;

                otherX = 266;
                otherY = 160;
            }

            if (ImAccepting)
            {
                _myCheckbox = new Checkbox(0x0869, 0x086A)
                {
                    X = myX,
                    Y = myY
                };
            }
            else
            {
                _myCheckbox = new Checkbox(0x0867, 0x0868)
                {
                    X = myX,
                    Y = myY
                };
            }

            _myCheckbox.ValueChanged -= MyCheckboxOnValueChanged;
            _myCheckbox.ValueChanged += MyCheckboxOnValueChanged;

            Add(_myCheckbox);


            _hisPic = HeIsAccepting ? new GumpPic(otherX, otherY, 0x0869, 0) : new GumpPic(otherX, otherY, 0x0867, 0);

            Add(_hisPic);
        }


        private void BuildGump()
        {
            int mydbX, mydbY, opdbX, opdbY;

            if (Client.Version >= ClientVersion.CV_704565)
            {
                Add(new GumpPic(0, 0, 0x088A, 0));

                Add(new Label(World.Player.Name, false, 0x0481, font: 3) { X = 73, Y = 32 });

                int fontWidth = 250 - FontsLoader.Instance.GetWidthASCII(3, _name);

                Add(new Label(_name, false, 0x0481, font: 3) { X = fontWidth, Y = 244 });


                _myCoins[0] = new Label("0", false, 0x0481, font: 9)
                {
                    X = 43,
                    Y = 67
                };

                Add(_myCoins[0]);

                _myCoins[1] = new Label("0", false, 0x0481, font: 9)
                {
                    X = 180,
                    Y = 67
                };

                Add(_myCoins[1]);

                _hisCoins[0] = new Label("0", false, 0x0481, font: 9)
                {
                    X = 180,
                    Y = 190
                };

                Add(_hisCoins[0]);

                _hisCoins[1] = new Label("0", false, 0x0481, font: 9)
                {
                    X = 180,
                    Y = 210
                };

                Add(_hisCoins[1]);

                _myCoinsEntries[0] = new StbTextBox(9, -1, 100, false)
                {
                    X = 43,
                    Y = 190,
                    Width = 100,
                    Height = 20,
                    NumbersOnly = true,
                    Tag = 0
                };

                _myCoinsEntries[0].SetText("0");

                Add(_myCoinsEntries[0]);

                _myCoinsEntries[1] = new StbTextBox(9, -1, 100, false)
                {
                    X = 43,
                    Y = 210,
                    Width = 100,
                    Height = 20,
                    NumbersOnly = true,
                    Tag = 1
                };

                _myCoinsEntries[1].SetText("0");

                Add(_myCoinsEntries[1]);

                uint my_gold_entry = 0, my_plat_entry = 0;

                void OnTextChanged(object sender, EventArgs e)
                {
                    StbTextBox entry = (StbTextBox) sender;
                    bool send = false;

                    if (entry != null)
                    {
                        if (string.IsNullOrEmpty(entry.Text))
                        {
                            entry.SetText("0");

                            if ((int) entry.Tag == 0)
                            {
                                if (my_gold_entry != 0)
                                {
                                    my_gold_entry = 0;
                                    send = true;
                                }
                            }
                            else if (my_plat_entry != 0)
                            {
                                my_plat_entry = 0;
                                send = true;
                            }
                        }
                        else if (uint.TryParse(entry.Text, out uint value))
                        {
                            if ((int) entry.Tag == 0) // gold
                            {
                                if (value > Gold)
                                {
                                    value = Gold;
                                    send = true;
                                }

                                if (my_gold_entry != value)
                                {
                                    send = true;
                                }

                                my_gold_entry = value;
                            }
                            else // platinum
                            {
                                if (value > Platinum)
                                {
                                    value = Platinum;
                                    send = true;
                                }

                                if (my_plat_entry != value)
                                {
                                    send = true;
                                }

                                my_plat_entry = value;
                            }

                            if (send)
                            {
                                entry.SetText(value.ToString());
                            }
                        }

                        if (send)
                        {
                            NetClient.Socket.Send_TradeUpdateGold(ID1, my_gold_entry, my_plat_entry);
                        }
                    }
                }

                _myCoinsEntries[0].TextChanged += OnTextChanged;

                _myCoinsEntries[1].TextChanged += OnTextChanged;


                mydbX = 30;
                mydbY = 110;
                opdbX = 192;
                opdbY = 110;
            }
            else
            {
                Add(new GumpPic(0, 0, 0x0866, 0));

                Add(new Label(World.Player.Name, false, 0x0386, font: 1) { X = 84, Y = 40 });

                int fontWidth = 260 - FontsLoader.Instance.GetWidthASCII(1, _name);

                Add(new Label(_name, false, 0x0386, font: 1) { X = fontWidth, Y = 170 });

                mydbX = 45;
                mydbY = 70;
                opdbX = 192;
                opdbY = 70;
            }


            if (Client.Version < ClientVersion.CV_500A)
            {
                Add
                (
                    new ColorBox(110, 60, 0)
                    {
                        X = 45, Y = 90
                    }
                );

                Add
                (
                    new ColorBox(110, 60, 0)
                    {
                        X = 192, Y = 70
                    }
                );
            }


            Add
            (
                _myBox = new DataBox(mydbX, mydbY, 110, 80)
                {
                    WantUpdateSize = false,
                    ContainsByBounds = true,
                    AcceptMouseInput = true,
                    CanMove = true
                }
            );

            Add
            (
                _hisBox = new DataBox(opdbX, opdbY, 110, 80)
                {
                    WantUpdateSize = false,
                    ContainsByBounds = true,
                    AcceptMouseInput = true,
                    CanMove = true
                }
            );

            SetCheckboxes();

            RequestUpdateContents();
        }

        private void MyCheckboxOnValueChanged(object sender, EventArgs e)
        {
            ImAccepting = !ImAccepting;
            GameActions.AcceptTrade(ID1, ImAccepting);
        }
    }
}