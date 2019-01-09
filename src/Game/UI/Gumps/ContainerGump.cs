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

using System.IO;
using System.Linq;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.IO;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ContainerGump : Gump
    {
        private GumpPic _eyeGumpPic;
        private bool _isCorspeContainer;
        private readonly Graphic _gumpID;
        private readonly Item _item;
        private long _corpseEyeTicks;
        private int _eyeCorspeOffset;

        public ContainerGump() : base(0, 0)
        {
           
        }

        public ContainerGump(Item item, Graphic gumpid) : this()
        {
            _item = item;
            _gumpID = gumpid;

            BuildGump();

            foreach (var c in Children.OfType<ItemGump>())
                c.Dispose();

            foreach (Item i in _item.Items)
                AddChildren(new ItemGump(i));
        }

        public Graphic Graphic => _gumpID;

        private void BuildGump()
        {
            CanMove = true;
            CanBeSaved = true;
            LocalSerial = _item.Serial;
            _isCorspeContainer = _gumpID == 0x0009;
            _item.Items.Added += ItemsOnAdded;
            _item.Items.Removed += ItemsOnRemoved;

            Graphic g = ContainerManager.Get(_gumpID).Graphic;
            AddChildren(new GumpPicContainer(0, 0, g, 0, _item));
            if (_isCorspeContainer)
                AddChildren(_eyeGumpPic = new GumpPic(45, 30, 0x0045, 0));

            ContainerManager.CalculateContainerPosition(g);
            X = ContainerManager.X;
            Y = ContainerManager.Y;
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (IsDisposed)
            {
                return;
            }

            if (_item.IsDisposed)
            {
                Dispose();
                return;
            }

            if (_isCorspeContainer && _corpseEyeTicks < totalMS)
            {
                _eyeCorspeOffset = _eyeCorspeOffset == 0 ? 1 : 0;
                _corpseEyeTicks = (long) totalMS + 750;
                _eyeGumpPic.Graphic = (Graphic) (0x0045 + _eyeCorspeOffset);
                _eyeGumpPic.Texture = FileManager.Gumps.GetTexture(_eyeGumpPic.Graphic);
            }
        }

        public override void Save(BinaryWriter writer)
        {
            base.Save(writer);
            writer.Write(_item.Serial.Value);
            writer.Write(_gumpID);
        }

        public override void Restore(BinaryReader reader)
        {
            base.Restore(reader);


            LocalSerial = reader.ReadUInt32();
            Engine.SceneManager.GetScene<GameScene>().DoubleClickDelayed(LocalSerial);
            reader.ReadUInt16();

            Dispose();
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