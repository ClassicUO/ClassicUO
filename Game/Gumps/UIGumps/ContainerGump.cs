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
using System.Collections.Generic;
using System.Linq;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class ContainerGump : Gump
    {
        private GumpPic _eyeGumpPic;
        private bool _isCorspeContainer;
        private Graphic _gumpID;
        private Item _item;
        private long _corpseEyeTicks;
        private int _eyeCorspeOffset;

        public ContainerGump() : base(0, 0)
        {
            CanMove = true;
            CanBeSaved = true;
        }

        public ContainerGump(Item item, Graphic gumpid) : this()
        {
            _item = item;
            _gumpID = gumpid;

            BuildGump();
        }

        private void BuildGump()
        {
            LocalSerial = _item.Serial;
            _isCorspeContainer = _gumpID == 0x0009;
            _item.Items.Added += ItemsOnAdded;
            _item.Items.Removed += ItemsOnRemoved;
            AddChildren(new GumpPicContainer(0, 0, ContainerManager.Get(_gumpID).Graphic, 0, _item));
            if (_isCorspeContainer) AddChildren(_eyeGumpPic = new GumpPic(45, 30, 0x0045, 0));
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (_isCorspeContainer && _corpseEyeTicks < totalMS)
            {
                _eyeCorspeOffset = _eyeCorspeOffset == 0 ? 1 : 0;
                _corpseEyeTicks = (long) totalMS + 750;
                _eyeGumpPic.Graphic = (Graphic) (0x0045 + _eyeCorspeOffset);
                _eyeGumpPic.Texture = IO.Resources.Gumps.GetGumpTexture(_eyeGumpPic.Graphic);
            }
        }

        public override bool Save(out Dictionary<string, object> data)
        {
            if (base.Save(out data))
            {
                data["serial"] = _item.Serial.Value;
                data["graphic"] = (ushort)_gumpID;
                return true;
            }

            return false;
        }

        public override bool Restore(Dictionary<string, object> data)
        {
            if (base.Restore(data))
            {

                if (data.TryGetValue("serial", out object s))
                {
                    //Item item = World.Items.Get(Serial.Parse(s.ToString()));

                    //if (item != null)
                    //{
                    //    _item = item;

                    //    if (data.TryGetValue("graphic", out object g))
                    //    {
                    //        _gumpID = Graphic.Parse(g.ToString());
                            
                    //        BuildGump();

                    //        return true;
                    //    }
                    //}

                    GameActions.DoubleClick(Serial.Parse(s.ToString()));
                    Dispose();
                    return true;
                }
             
            }

            return false;
        }

        private void ItemsOnRemoved(object sender, CollectionChangedEventArgs<Item> e)
        {
            Children.OfType<ItemGump>().Where(s => e.Contains(s.Item)).ToList().ForEach(RemoveChildren);
        }

        private void ItemsOnAdded(object sender, CollectionChangedEventArgs<Item> e)
        {
            Children.OfType<ItemGump>().Where(s => e.Contains(s.Item)).ToList().ForEach(RemoveChildren);

            foreach (Item item in e)
                AddChildren(new ItemGump(item));
        }

        protected override void OnInitialize()
        {
            foreach (Item item in _item.Items)
                AddChildren(new ItemGump(item));
        }

        public override void Dispose()
        {
            if (_item != null)
            {
                _item.Items.Added -= ItemsOnAdded;
                _item.Items.Removed -= ItemsOnRemoved;
            }

            base.Dispose();
        }
    }
}