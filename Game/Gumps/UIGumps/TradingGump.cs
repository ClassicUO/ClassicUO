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
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.IO;
using ClassicUO.IO.Resources;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class TradingGump : Gump
    {
        private Checkbox _myCheckbox;
        private GumpPic _hisPic;

        private bool _imAccepting, _heIsAccepting;

        private readonly Entity _entity1, _entity2;

        private DataBox _myBox, _hisBox;
        private readonly string _name;

        public TradingGump(Serial local, string name, Serial id1, Serial id2) : base(local, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;

            _name = name;

            ID1 = id1;
            ID2 = id2;

            _entity1 = World.Get(id1);
            _entity2 = World.Get(id2);

            _entity1.Items.Added += ItemsOnAdded1;
            _entity2.Items.Added += ItemsOnAdded2;

            BuildGump();
        }

        public Serial ID1 { get; }
        public Serial ID2 { get; }
      
        public bool ImAccepting
        {
            get => _imAccepting;
            set
            {
                if (_imAccepting != value)
                {
                    _imAccepting = value;
                    BuildGump();
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
                    BuildGump();
                }
            }
        }


        private void ItemsOnAdded1(object sender, CollectionChangedEventArgs<Item> e)
        {
            foreach (Item item in e)
            {
                ItemGump g = new ItemGump(item)
                {
                    HighlightOnMouseOver = true,
                };


                //if (g.X >= 155)
                //    g.X = 125;

                //if (g.Y >= 150)
                //    g.Y = 120;

                _myBox.AddChildren(g);
            }
        }

        private void ItemsOnAdded2(object sender, CollectionChangedEventArgs<Item> e)
        {
            foreach (Item item in e)
            {
                ItemGump g = new ItemGump(item)
                {
                    HighlightOnMouseOver = true,
                };


                //if (g.X >= 155)
                //    g.X = 125;

                //if (g.Y >= 150)
                //    g.Y = 120;

                _hisBox.AddChildren(g);
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            _entity1.Items.Added -= ItemsOnAdded1;
            _entity2.Items.Added -= ItemsOnAdded2;

            GameActions.CancelTrade(ID1);
        }


        private void BuildGump()
        {
            Clear();

            AddChildren(new GumpPic(0, 0, 0x0866, 0));

            if (FileManager.ClientVersion < ClientVersions.CV_500A)
            {
                // TODO: implement
            }

            AddChildren(new Label(World.Player.Name, false, 0x0386, font: 1)
                            { X = 84, Y = 40 });

            int fontWidth = 260 - Fonts.GetWidthASCII(1, _name);

            AddChildren(new Label(_name, false, 0x0386, font: 1)
                            { X = fontWidth, Y = 170 });

            AddChildren(_myBox = new DataBox(45, 70, 110, 80));
            AddChildren(_hisBox = new DataBox(192, 70, 110, 80));

            _myCheckbox?.Dispose();
            _hisPic?.Dispose();

            if (ImAccepting)
            {
                _myCheckbox = new Checkbox(0x0869, 0x086A)
                {
                    X = 52,
                    Y = 29
                };
            }
            else
            {
                _myCheckbox = new Checkbox(0x0867, 0x0868)
                {
                    X = 52,
                    Y = 29
                };
            }

            _myCheckbox.ValueChanged -= MyCheckboxOnValueChanged;
            _myCheckbox.ValueChanged += MyCheckboxOnValueChanged;

            AddChildren(_myCheckbox);


            if (HeIsAccepting)
            {
                _hisPic = new GumpPic(266, 160, 0x0869, 0);
            }
            else
            {
                _hisPic = new GumpPic(266, 160, 0x0867, 0);
            }

            AddChildren(_hisPic);



            foreach (Item item in _entity1.Items)
            {
                _myBox.AddChildren(new ItemGump(item));
            }

            foreach (Item item in _entity2.Items)
            {
                _hisBox.AddChildren(new ItemGump(item));
            }
        }

        private void MyCheckboxOnValueChanged(object sender, EventArgs e)
        {
            ImAccepting = !ImAccepting;
            GameActions.AcceptTrade(ID1, ImAccepting);
        }
    }
}
