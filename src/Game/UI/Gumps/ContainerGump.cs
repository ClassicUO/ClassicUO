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
    internal class ContainerGump : MinimizableGump
    {
        private GumpPic _iconized;
        internal override GumpPic Iconized => _iconized;
        private HitBox _iconizerArea;
        internal override HitBox IconizerArea => _iconizerArea;
        private readonly Item _item;
        private long _corpseEyeTicks;
        private ContainerData _data;
        private int _eyeCorspeOffset;
        private GumpPic _eyeGumpPic;
        private bool _isCorspeContainer;

        public ContainerGump() : base(0, 0)
        {
        }

        public ContainerGump(Item item, Graphic gumpid) : this()
        {
            _item = item;
            Graphic = gumpid;

            BuildGump();

            foreach (var c in Children.OfType<ItemGump>())
                c.Dispose();

            foreach (Item i in _item.Items.Where(s => s.ItemData.Layer != (int) Layer.Hair && s.ItemData.Layer != (int) Layer.Beard && s.ItemData.Layer != (int) Layer.Face))
                //FIXME: this should be disabled. Server sends the right position
                //CheckItemPosition(i);
                Add(new ItemGump(i));
        }

        public Graphic Graphic { get; }

        public TextContainer TextContainer { get; } = new TextContainer();

        private void BuildGump()
        {
            CanMove = true;
            CanBeSaved = true;
            LocalSerial = _item.Serial;
            WantUpdateSize = false;
            _isCorspeContainer = Graphic == 0x0009;

            _item.Items.Added -= ItemsOnAdded;
            _item.Items.Removed -= ItemsOnRemoved;
            _item.Items.Added += ItemsOnAdded;
            _item.Items.Removed += ItemsOnRemoved;

            float scale = Engine.UI.ContainerScale;

            _data = ContainerManager.Get(Graphic);
            if(_data.MinimizerArea != Rectangle.Empty && _data.IconizedGraphic != 0)
            {
                _iconizerArea = new HitBox((int) (_data.MinimizerArea.X* scale), 
                                           (int) (_data.MinimizerArea.Y * scale),
                                           (int) (_data.MinimizerArea.Width * scale),
                                           (int) (_data.MinimizerArea.Height * scale));
                _iconized = new GumpPic(0, 0, _data.IconizedGraphic, 0);
            }
            Graphic g = _data.Graphic;

            GumpPicContainer container;
            Add(container = new GumpPicContainer(0, 0, g, 0, _item));

            if (_isCorspeContainer)
            {
                _eyeGumpPic?.Dispose();
                Add(_eyeGumpPic = new GumpPic((int) (45 * scale), (int) (30 * scale), 0x0045, 0));

                _eyeGumpPic.Width = (int)(_eyeGumpPic.Width * scale);
                _eyeGumpPic.Height = (int)(_eyeGumpPic.Height * scale);
            }


            Width = container.Width = (int)(container.Width * scale);
            Height = container.Height = (int) (container.Height * scale);

            ContainerGump gg = Engine.UI.Gumps.OfType<ContainerGump>().FirstOrDefault(s => s.LocalSerial == LocalSerial);

            if (gg == null)
            {
                if (Engine.UI.GetGumpCachePosition(LocalSerial, out Point location) && _item.Serial == World.Player.Equipment[(int) Layer.Backpack])
                    Location = location;
                else
                {
                    if (Engine.Profile.Current.OverrideContainerLocation)
                    {
                        switch (Engine.Profile.Current.OverrideContainerLocationSetting)
                        {
                            case 0:
                                SetPositionNearGameObject(g);
                                break;
                            case 1:
                                SetPositionTopRight();
                                break;
                            case 2:
                                SetPositionByLastDragged();
                                break;
                        }

                        if ((X + Width) > Engine.WindowWidth)
                        {
                            X -= Width;
                        }

                        if ((Y + Height) > Engine.WindowHeight)
                        {
                            Y -= Height;
                        }
                    }
                    else
                    {
                        ContainerManager.CalculateContainerPosition(g);
                        X = ContainerManager.X;
                        Y = ContainerManager.Y;
                    }
                }
            }
            else
            {
                X = gg.X;
                Y = gg.Y;
            }


            if (_data.OpenSound != 0)
                Engine.SceneManager.CurrentScene.Audio.PlaySound(_data.OpenSound);
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (_item == null || _item.IsDestroyed)
            {
                Dispose();
                return;
            }

            if (IsDisposed) return;

            if (_isCorspeContainer && _corpseEyeTicks < totalMS)
            {
                _eyeCorspeOffset = _eyeCorspeOffset == 0 ? 1 : 0;
                _corpseEyeTicks = (long) totalMS + 750;
                _eyeGumpPic.Graphic = (Graphic) (0x0045 + _eyeCorspeOffset);
                float scale = Engine.UI.ContainerScale;
                _eyeGumpPic.Width = (int)(_eyeGumpPic.Texture.Width * scale);
                _eyeGumpPic.Height = (int)(_eyeGumpPic.Texture.Height * scale);
            }
            if(Iconized != null) Iconized.Hue = _item.Hue;
        }

        public void ForceUpdate()
        {
            Children[0].Dispose();
            _iconizerArea?.Dispose();
            _iconized?.Dispose();
            LocalSerial = 0;

            BuildGump();
            ItemsOnAdded(null, new CollectionChangedEventArgs<Serial>(FindControls<ItemGump>().Select(s => s.LocalSerial)));
        }

        public override void Save(BinaryWriter writer)
        {
            base.Save(writer);
            writer.Write(_item.Serial.Value);
            writer.Write(Graphic);
        }

        public override void Restore(BinaryReader reader)
        {
            base.Restore(reader);


            LocalSerial = reader.ReadUInt32();
            Engine.SceneManager.GetScene<GameScene>()?.DoubleClickDelayed(LocalSerial);
            reader.ReadUInt16();

            Dispose();
        }

        private void ItemsOnRemoved(object sender, CollectionChangedEventArgs<Serial> e)
        {
            foreach (ItemGump v in Children.OfType<ItemGump>().Where(s => s.Item != null && e.Contains(s.Item)))
                v.Dispose();
        }

        private void ItemsOnAdded(object sender, CollectionChangedEventArgs<Serial> e)
        {
            foreach (ItemGump v in Children.OfType<ItemGump>().Where(s => s.Item != null && e.Contains(s.Item)))
                v.Dispose();

            foreach (Serial s in e)
            {
                var item = World.Items.Get(s);

                if (item == null || item.ItemData.Layer == (int) Layer.Hair || item.ItemData.Layer == (int) Layer.Beard || item.ItemData.Layer == (int) Layer.Face)
                    continue;


                var itemControl = new ItemGump(item);
                CheckItemControlPosition(itemControl);

                if (Engine.Profile.Current != null && Engine.Profile.Current.ScaleItemsInsideContainers)
                {
                    float scale = Engine.UI.ContainerScale;

                    itemControl.Width = (int)(itemControl.Width * scale);
                    itemControl.Height = (int)(itemControl.Height * scale);
                }

                Add(itemControl);
            }
        }
    

        private void CheckItemControlPosition(ItemGump item)
        {
            float scale = Engine.UI.ContainerScale;

            int x = (int) (item.X * scale);
            int y = (int) (item.Y * scale);
          
            ArtTexture texture = FileManager.Art.GetTexture(item.Item.DisplayedGraphic);

            int boundX = (int)(_data.Bounds.X * scale);
            int boundY = (int)(_data.Bounds.Y * scale);

            if (texture != null && !texture.IsDisposed)
            {
                int boundW = (int)(_data.Bounds.Width * scale);
                int boundH = (int)(_data.Bounds.Height * scale);

                int textureW, textureH;

                if (Engine.Profile.Current != null && Engine.Profile.Current.ScaleItemsInsideContainers)
                {
                    textureW = (int)(texture.Width * scale);
                    textureH = (int)(texture.Height * scale);
                }
                else
                {
                    textureW = texture.Width;
                    textureH = texture.Height;
                }

                if (x < boundX)
                    x = boundX;

                if (y < boundY)
                    y = boundY;


                if (x + textureW > boundW)
                    x = boundW - textureW;

                if (y + textureH > boundH)
                    y = boundH - textureH;
            }
            else
            {
                x = boundX;
                y = boundY;
            }

            if (x < 0)
                x = 0;

            if (y < 0)
                y = 0;


            if (x != item.X || y != item.Y)
            {
                item.X = x;
                item.Y = y;
            }
        }

        private void SetPositionNearGameObject(Graphic g)
        {
            if (World.Player.Equipment[(int)Layer.Bank] != null && _item.Serial == World.Player.Equipment[(int)Layer.Bank])
            {
                // open bank near player
                X = World.Player.RealScreenPosition.X + Engine.Profile.Current.GameWindowPosition.X + 40;
                Y = World.Player.RealScreenPosition.Y + Engine.Profile.Current.GameWindowPosition.Y - (Height >> 1);
            }
            else if (_item.OnGround)
            {
                // item is in world
                X = _item.RealScreenPosition.X + Engine.Profile.Current.GameWindowPosition.X + 40;
                Y = _item.RealScreenPosition.Y + Engine.Profile.Current.GameWindowPosition.Y - (Height >> 1);
            }
            else if (_item.Container.IsMobile)
            {
                // pack animal, snooped player, npc vendor
                Mobile mobile = World.Mobiles.Get(_item.Container);
                if (mobile != null)
                {
                    X = mobile.RealScreenPosition.X + Engine.Profile.Current.GameWindowPosition.X + 40;
                    Y = mobile.RealScreenPosition.Y + Engine.Profile.Current.GameWindowPosition.Y - (Height >> 1);
                }
            }
            else
            {
                // in a container, open near the container
                ContainerGump parentContainer = Engine.UI.Gumps.OfType<ContainerGump>().FirstOrDefault(s => s.LocalSerial == _item.Container);
                if (parentContainer != null)
                {
                    X = parentContainer.X + (Width >> 1);
                    Y = parentContainer.Y;
                }
                else
                {
                    // I don't think we ever get here?
                    ContainerManager.CalculateContainerPosition(g);
                    X = ContainerManager.X;
                    Y = ContainerManager.Y;
                }
            }
        }

        private void SetPositionTopRight()
        {
            X = Engine.WindowWidth - Width;
            Y = 0;
        }

        private void SetPositionByLastDragged()
        {
            X = Engine.Profile.Current.OverrideContainerLocationPosition.X - (Width >> 1);
            Y = Engine.Profile.Current.OverrideContainerLocationPosition.Y - (Height >> 1);
        }

        public override void Dispose()
        {
            TextContainer.Clear();

            if (_item != null)
            {
                _item.Items.Added -= ItemsOnAdded;
                _item.Items.Removed -= ItemsOnRemoved;

                if (World.Player != null && _item == World.Player.Equipment[(int) Layer.Backpack]) Engine.UI.SavePosition(_item, Location);

                foreach (Item child in _item.Items)
                {
                    if (child.Container == _item)
                        Engine.UI.GetGump<ContainerGump>(child)?.Dispose();
                }

                if (_data.ClosedSound != 0)
                    Engine.SceneManager.CurrentScene.Audio.PlaySound(_data.ClosedSound);
            }

            base.Dispose();
        }

        protected override void OnDragEnd(int x, int y)
        {
            if (Engine.Profile.Current.OverrideContainerLocation && Engine.Profile.Current.OverrideContainerLocationSetting == 2)
            {
                Point gumpCenter = new Point(X + (Width >> 1), Y + (Height >> 1));
                Engine.Profile.Current.OverrideContainerLocationPosition = gumpCenter;
            }

            base.OnDragEnd(x, y);
        }
    }
}