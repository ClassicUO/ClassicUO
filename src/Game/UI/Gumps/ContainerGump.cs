#region license
//  Copyright (C) 2019 ClassicUO Development Community on Github
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
using ClassicUO.Input;
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
        private ContainerData _data;

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

            foreach (Item i in _item.Items.Where(s => s.ItemData.Layer != (int) Layer.Hair && s.ItemData.Layer != (int)Layer.Beard && s.ItemData.Layer != (int)Layer.Face))
                Add(new ItemGump(i));
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

            _data = ContainerManager.Get(_gumpID);
            Graphic g = _data.Graphic;

            Add(new GumpPicContainer(0, 0, g, 0, _item));
            if (_isCorspeContainer)
                Add(_eyeGumpPic = new GumpPic(45, 30, 0x0045, 0));

            ContainerManager.CalculateContainerPosition(g);
            X = ContainerManager.X;
            Y = ContainerManager.Y;

            if (_data.OpenSound != 0)
                Engine.SceneManager.CurrentScene.Audio.PlaySound(_data.OpenSound);
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (_item.OnGround && _item.Distance > 3)
                Dispose();

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
            foreach (ItemGump v in Children.OfType<ItemGump>().Where(s => e.Contains(s.Item)))
                v.Dispose();  
        }

        private void ItemsOnAdded(object sender, CollectionChangedEventArgs<Item> e)
        {
            foreach (ItemGump v in Children.OfType<ItemGump>().Where(s => e.Contains(s.Item)))
                v.Dispose();

            foreach (Item item in e.Where(s => s.ItemData.Layer != (int)Layer.Hair && s.ItemData.Layer != (int)Layer.Beard && s.ItemData.Layer != (int)Layer.Face))
            {
                CheckItemPosition(item);
                Add(new ItemGump(item));
            }
        }

        private void CheckItemPosition(Item item)
        {
            int x = item.X;
            int y = item.Y;

            ArtTexture texture = FileManager.Art.GetTexture(item.DisplayedGraphic);

            if (texture != null && !texture.IsDisposed)
            {
                if (x < _data.Bounds.X)
                    x = _data.Bounds.X;

                if (y < _data.Bounds.Y)
                    y = _data.Bounds.Y;

                if (x + texture.Width > _data.Bounds.Right)
                    x = _data.Bounds.Right - texture.Width;

                if (y + texture.Height > _data.Bounds.Bottom)
                    y = _data.Bounds.Bottom - texture.Height;
            }
            else
            {
                x = _data.Bounds.X;
                y = _data.Bounds.Y;
            }

            if (x != item.X || y != item.Y)
            {
                item.Position = new Position((ushort)x, (ushort)y);
            }

        }

        public override void Dispose()
        {
            if (_item != null)
            {
                _item.Items.Added -= ItemsOnAdded;
                _item.Items.Removed -= ItemsOnRemoved;

                foreach (Item child in _item.Items)
                {
                    if (child.Container == _item)
                        Engine.UI.GetByLocalSerial<ContainerGump>(child)?.Dispose();

                    child.Dispose();
                }

                if (_data.ClosedSound != 0)
                    Engine.SceneManager.CurrentScene.Audio.PlaySound(_data.ClosedSound);
            }

            base.Dispose();
        }
    }
}