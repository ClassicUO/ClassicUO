#region license

//  Copyright (C) 2020 ClassicUO Development Community on Github
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
using System.IO;
using System.Linq;
using ClassicUO.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal sealed class TradingGump : TextContainerGump
    {
        private readonly string _name;
        private GumpPic _hisPic;

        private bool _imAccepting, _heIsAccepting;

        private DataBox _myBox, _hisBox;
        private Checkbox _myCheckbox;

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

        public bool ImAccepting
        {
            get => _imAccepting;
            set
            {
                if (_imAccepting != value)
                {
                    _imAccepting = value;
                    SetCheckboxes();
                    //BuildGump();
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
                    //BuildGump();
                }
            }
        }


        public void UpdateContent()
        {
            Entity container = World.Get(ID1);

            if (container == null)
                return;

            foreach (ItemGump v in _myBox.Children.OfType<ItemGump>().Where(s => container.Items.Contains(s.LocalSerial)))
                v.Dispose();

            foreach (Item item in container.Items)
            {
                ItemGump g = new ItemGump(item)
                {
                    HighlightOnMouseOver = true
                };

                int x = g.X;
                int y = g.Y;

                if (x + g.Texture.Width > 110)
                    x = 110 - g.Texture.Width;

                if (y + g.Texture.Height > 80)
                    y = 80 - g.Texture.Height;

                if (x < 0)
                    x = 0;

                if (y < 0)
                    y = 0;


                g.X = x;
                g.Y = y;

                _myBox.Add(g);
            }


            container = World.Get(ID2);

            if (container == null)
                return;

            foreach (ItemGump v in _hisBox.Children.OfType<ItemGump>().Where(s => container.Items.Contains(s.LocalSerial)))
                v.Dispose();

            foreach (Item item in container.Items)
            {
                ItemGump g = new ItemGump(item)
                {
                    HighlightOnMouseOver = true
                };

                int x = g.X;
                int y = g.Y;

                if (x + g.Texture.Width > 110)
                    x = 110 - g.Texture.Width;

                if (y + g.Texture.Height > 80)
                    y = 80 - g.Texture.Height;

                if (x < 0)
                    x = 0;

                if (y < 0)
                    y = 0;


                g.X = x;
                g.Y = y;

                _hisBox.Add(g);
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


            _hisPic = HeIsAccepting ? new GumpPic(otherX, otherY, 0x0869, 0) :
                          new GumpPic(otherX, otherY, 0x0867, 0);

            Add(_hisPic);
        }


        private void BuildGump()
        {
            int mydbX, mydbY, opdbX, opdbY;

            if (Client.Version >= ClientVersion.CV_704565)
            {
                Add(new GumpPic(0, 0, 0x088A, 0));
                Add(new Label(World.Player.Name, false, 0x0481, font: 3)
                        { X = 73, Y = 32 });
                int fontWidth = 250 - UOFileManager.Fonts.GetWidthASCII(3, _name);

                Add(new Label(_name, false, 0x0481, font: 3)
                        { X = fontWidth, Y = 244 });

                mydbX = 30;
                mydbY = 110;
                opdbX = 192;
                opdbY = 110;
            }
            else
            {
                Add(new GumpPic(0, 0, 0x0866, 0));
                Add(new Label(World.Player.Name, false, 0x0386, font: 1)
                        { X = 84, Y = 40 });
                int fontWidth = 260 - UOFileManager.Fonts.GetWidthASCII(1, _name);

                Add(new Label(_name, false, 0x0386, font: 1)
                        { X = fontWidth, Y = 170 });

                mydbX = 45;
                mydbY = 70;
                opdbX = 192;
                opdbY = 70;
            }


            if (Client.Version < ClientVersion.CV_500A)
            {
                Add(new ColorBox(110, 60, 0, 0xFF000001)
                {
                    X = 45, Y = 90
                });
                Add(new ColorBox(110, 60, 0, 0xFF000001)
                {
                    X = 192, Y = 70
                });
            }


            Add(_myBox = new DataBox(mydbX, mydbY, 110, 80)
            {
                WantUpdateSize = false,
                ContainsByBounds = true,
                AcceptMouseInput = true,
                CanMove = true
            });

            Add(_hisBox = new DataBox(opdbX, opdbY, 110, 80)
            {
                WantUpdateSize = false,
                ContainsByBounds = true,
                AcceptMouseInput = true,
                CanMove = true
            });

            SetCheckboxes();

            UpdateContent();

            _myBox.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    GameScene gs = Client.Game.GetScene<GameScene>();

                    if (!gs.IsHoldingItem || !gs.IsMouseOverUI)
                        return;

                    ArtTexture texture = UOFileManager.Art.GetTexture(gs.HeldItem.DisplayedGraphic);

                    int x = e.X;
                    int y = e.Y;

                    if (texture != null)
                    {
                        x -= texture.Width >> 1;
                        y -= texture.Height >> 1;

                        if (x + texture.Width > 110)
                            x = 110 - texture.Width;

                        if (y + texture.Height > 80)
                            y = 80 - texture.Height;
                    }

                    if (x < 0)
                        x = 0;

                    if (y < 0)
                        y = 0;

                    GameActions.DropItem(gs.HeldItem.Serial, x, y, 0, ID1);
                    gs.HeldItem.Dropped = true;
                    gs.HeldItem.Enabled = false;
                    //Mouse.CancelDoubleClick = true;
                }
            };
        }

        private void MyCheckboxOnValueChanged(object sender, EventArgs e)
        {
            ImAccepting = !ImAccepting;
            GameActions.AcceptTrade(ID1, ImAccepting);
        }
    }
}