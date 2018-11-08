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

using ClassicUO.Game.GameObjects;
using ClassicUO.IO.Resources;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.Controls
{
    internal class EquipmentSlot : GumpControl
    {
        private StaticPic _itemGump;
        private Item _item;
        private readonly Mobile _mobile;
        private readonly Layer _layer;
        private bool _canDrag, _sendClickIfNotDClick;
        private float _pickupTime, _singleClickTime;

        public EquipmentSlot(int x, int y, Mobile mobile, Layer layer)
        {
            X = x;
            Y = y;
            _mobile = mobile;
            _layer = layer;
            AddChildren(new GumpPicTiled(0, 0, 19, 20, 0x243A));
            AddChildren(new GumpPic(0, 0, 0x2344, 0));

            AcceptMouseInput = true;
        }

        public Item Item
        {
            get => _item;
            set => _item = value;
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (_item != null && _item.IsDisposed)
            {
                _item = null;
                _itemGump.Dispose();
                _itemGump = null;
            }

            if (_item != _mobile.Equipment[(int) _layer])
            {
                if (_itemGump != null)
                {
                    _itemGump.Dispose();
                    _itemGump = null;
                }

                _item = _mobile.Equipment[(int) _layer];

                if (_item != null)
                    AddChildren( _itemGump = new StaticPic(_item.Graphic, _item.Hue));
            }

            if (_item != null)
            {
                if (_canDrag && totalMS >= _pickupTime)
                {
                    _canDrag = false;
                    AttempPickUp();
                }

                if (_sendClickIfNotDClick && totalMS >= _singleClickTime)
                {
                    _sendClickIfNotDClick = false;
                    GameActions.SingleClick(_item);
                }
            }

            base.Update(totalMS, frameMS);

            if (_itemGump != null)
            {
                _itemGump.Location = new Point(-14, 0);
            }
        }

        private void AttempPickUp()
        {
            Rectangle bounds = Art.GetStaticTexture(Item.DisplayedGraphic).Bounds;
            GameActions.PickUp(Item, bounds.Width / 2, bounds.Height / 2);
        }
    }
}