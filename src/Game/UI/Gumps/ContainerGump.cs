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
using System.Collections.Generic;
using System.Linq;

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
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
        private long _corpseEyeTicks;
        private float _autoLootGoldTimeout;
        private bool _hideIfEmpty, _hasGold;
        private readonly HashSet<Serial> _lootedGold = new HashSet<Serial>();
        private ContainerData _data;
        private int _eyeCorspeOffset;
        private GumpPic _eyeGumpPic;
        private bool _isCorspeContainer;

        public ContainerGump() : base(0, 0)
        {
        }

        public ContainerGump(Serial serial, Graphic gumpid) : this()
        {
            LocalSerial = serial;
            Item item = World.Items.Get(serial);

            if (item == null)
            {
                Dispose();
                return;
            }

            Graphic = gumpid;

            BuildGump();

            foreach (var c in Children.OfType<ItemGump>())
                c.Dispose();

            foreach (Item i in item.Items.Where(s => s != null && s.IsLootable))
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
            WantUpdateSize = false;
            _isCorspeContainer = Graphic == 0x0009;

            Item item = World.Items.Get(LocalSerial);

            if (item == null)
            {
                Dispose();
                return;
            }

            item.Items.Added -= ItemsOnAdded;
            item.Items.Removed -= ItemsOnRemoved;
            item.Items.Added += ItemsOnAdded;
            item.Items.Removed += ItemsOnRemoved;

            float scale = UIManager.ContainerScale;

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
            Add(container = new GumpPicContainer(0, 0, g, 0, item));

            if (_isCorspeContainer)
            {
                if (World.Player.ManualOpenedCorpses.Contains(LocalSerial))
                    World.Player.ManualOpenedCorpses.Remove(LocalSerial);
                else if(World.Player.AutoOpenedCorpses.Contains(LocalSerial) &&
                ProfileManager.Current != null && ProfileManager.Current.SkipEmptyCorpse)
                {
                    IsVisible = false;
                    _hideIfEmpty = true;
                }

                _eyeGumpPic?.Dispose();
                Add(_eyeGumpPic = new GumpPic((int) (45 * scale), (int) (30 * scale), 0x0045, 0));

                _eyeGumpPic.Width = (int)(_eyeGumpPic.Width * scale);
                _eyeGumpPic.Height = (int)(_eyeGumpPic.Height * scale);
            }


            Width = container.Width = (int)(container.Width * scale);
            Height = container.Height = (int) (container.Height * scale);

            ContainerGump gg = UIManager.Gumps.OfType<ContainerGump>().FirstOrDefault(s => s.LocalSerial == LocalSerial);

            if (gg == null)
            {
                if (UIManager.GetGumpCachePosition(LocalSerial, out Point location) && item.Serial == World.Player.Equipment[(int) Layer.Backpack])
                    Location = location;
                else
                {
                    if (ProfileManager.Current.OverrideContainerLocation)
                    {
                        switch (ProfileManager.Current.OverrideContainerLocationSetting)
                        {
                            case 0:
                                SetPositionNearGameObject(g, item);
                                break;
                            case 1:
                                SetPositionTopRight();
                                break;
                            case 2:
                                SetPositionByLastDragged();
                                break;
                        }

                        if ((X + Width) > CUOEnviroment.Client.Window.ClientBounds.Width)
                        {
                            X -= Width;
                        }

                        if ((Y + Height) > CUOEnviroment.Client.Window.ClientBounds.Height)
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
                CUOEnviroment.Client.Scene.Audio.PlaySound(_data.OpenSound);
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            Item item = World.Items.Get(LocalSerial);

            if (item == null || item.IsDestroyed)
            {
                Dispose();
                return;
            }

            if (IsDisposed) return;

            if (_isCorspeContainer)
            {
                if (_hasGold)
                {
                    if (_autoLootGoldTimeout == 0)
                        _autoLootGoldTimeout = (float) totalMS + 350;
                    else if (_autoLootGoldTimeout < totalMS)
                    {
                        _autoLootGoldTimeout = (float) totalMS + 350;

                        int count = 0;
                        foreach(Item gold in item.Items.Where(n => n.Graphic == 0x0EED))
                        {
                            count++;
                            if (!_lootedGold.Contains(gold))
                            {
                                _lootedGold.Add(gold);
                                GameActions.Print($"Auto looting {gold.Amount} gold from {item.ItemData.Name}", 0x35);
                            }
                            GameActions.GrabItem(gold, gold.Amount);
                        }
                        if (count <= 0)
                            _hasGold = false;
                    }
                }

                if (_corpseEyeTicks < totalMS)
                {
                    _eyeCorspeOffset = _eyeCorspeOffset == 0 ? 1 : 0;
                    _corpseEyeTicks = (long) totalMS + 750;
                    _eyeGumpPic.Graphic = (Graphic) (0x0045 + _eyeCorspeOffset);
                    float scale = UIManager.ContainerScale;
                    _eyeGumpPic.Width = (int)(_eyeGumpPic.Texture.Width * scale);
                    _eyeGumpPic.Height = (int)(_eyeGumpPic.Texture.Height * scale);
                }
            }
            if(Iconized != null) Iconized.Hue = item.Hue;
        }

        public void ForceUpdate()
        {
            Children[0].Dispose();
            _iconizerArea?.Dispose();
            _iconized?.Dispose();

            BuildGump();
            ItemsOnAdded(null, new CollectionChangedEventArgs<Serial>(FindControls<ItemGump>().Select(s => s.LocalSerial)));
        }

        public override void Save(BinaryWriter writer)
        {
            base.Save(writer);
            writer.Write(LocalSerial);
            writer.Write(Graphic);
        }

        public override void Restore(BinaryReader reader)
        {
            base.Restore(reader);


            LocalSerial = reader.ReadUInt32();
            CUOEnviroment.Client.GetScene<GameScene>()?.DoubleClickDelayed(LocalSerial);
            reader.ReadUInt16();

            Dispose();
        }

        private void ItemsOnRemoved(object sender, CollectionChangedEventArgs<Serial> e)
        {
            foreach (ItemGump v in Children.OfType<ItemGump>().Where(s => e.Contains(s.LocalSerial)))
                v.Dispose();
        }

        private void ItemsOnAdded(object sender, CollectionChangedEventArgs<Serial> e)
        {
            foreach (ItemGump v in Children.OfType<ItemGump>().Where(s => e.Contains(s.LocalSerial)))
                v.Dispose();

            foreach (Serial s in e)
            {
                var item = World.Items.Get(s);

                if (item == null || !item.IsLootable)
                    continue;


                var itemControl = new ItemGump(item);
                CheckItemControlPosition(itemControl, item);

                if (ProfileManager.Current != null && ProfileManager.Current.ScaleItemsInsideContainers)
                {
                    float scale = UIManager.ContainerScale;

                    itemControl.Width = (int)(itemControl.Width * scale);
                    itemControl.Height = (int)(itemControl.Height * scale);
                }

                if (_hideIfEmpty && !IsVisible &&
                !(item.Graphic == 0x0EED && ProfileManager.Current.AutoLootGold))
                    IsVisible = true;

                if (item.Graphic == 0x0EED && ProfileManager.Current.AutoLootGold)
                    _hasGold = true;


                Add(itemControl);
            }
        }
    

        private void CheckItemControlPosition(ItemGump itemGump, Item item)
        {
            float scale = UIManager.ContainerScale;

            int x = (int) (itemGump.X * scale);
            int y = (int) (itemGump.Y * scale);
          
            ArtTexture texture = FileManager.Art.GetTexture(item.DisplayedGraphic);

            int boundX = (int)(_data.Bounds.X * scale);
            int boundY = (int)(_data.Bounds.Y * scale);

            if (texture != null && !texture.IsDisposed)
            {
                int boundW = (int)(_data.Bounds.Width * scale);
                int boundH = (int)(_data.Bounds.Height * scale);

                int textureW, textureH;

                if (ProfileManager.Current != null && ProfileManager.Current.ScaleItemsInsideContainers)
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


            if (x != itemGump.X || y != itemGump.Y)
            {
                itemGump.X = x;
                itemGump.Y = y;
            }
        }

        private void SetPositionNearGameObject(Graphic g, Item item)
        {
            if (World.Player.Equipment[(int)Layer.Bank] != null && item.Serial == World.Player.Equipment[(int)Layer.Bank])
            {
                // open bank near player
                X = World.Player.RealScreenPosition.X + ProfileManager.Current.GameWindowPosition.X + 40;
                Y = World.Player.RealScreenPosition.Y + ProfileManager.Current.GameWindowPosition.Y - (Height >> 1);
            }
            else if (item.OnGround)
            {
                // item is in world
                X = item.RealScreenPosition.X + ProfileManager.Current.GameWindowPosition.X + 40;
                Y = item.RealScreenPosition.Y + ProfileManager.Current.GameWindowPosition.Y - (Height >> 1);
            }
            else if (item.Container.IsMobile)
            {
                // pack animal, snooped player, npc vendor
                Mobile mobile = World.Mobiles.Get(item.Container);
                if (mobile != null)
                {
                    X = mobile.RealScreenPosition.X + ProfileManager.Current.GameWindowPosition.X + 40;
                    Y = mobile.RealScreenPosition.Y + ProfileManager.Current.GameWindowPosition.Y - (Height >> 1);
                }
            }
            else
            {
                // in a container, open near the container
                ContainerGump parentContainer = UIManager.Gumps.OfType<ContainerGump>().FirstOrDefault(s => s.LocalSerial == item.Container);
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
            X = CUOEnviroment.Client.Window.ClientBounds.Width - Width;
            Y = 0;
        }

        private void SetPositionByLastDragged()
        {
            X = ProfileManager.Current.OverrideContainerLocationPosition.X - (Width >> 1);
            Y = ProfileManager.Current.OverrideContainerLocationPosition.Y - (Height >> 1);
        }

        public override void Dispose()
        {
            TextContainer.Clear();

            Item item = World.Items.Get(LocalSerial);

            if (item != null)
            {
                item.Items.Added -= ItemsOnAdded;
                item.Items.Removed -= ItemsOnRemoved;

                if (World.Player != null && item == World.Player.Equipment[(int) Layer.Backpack]) UIManager.SavePosition(item, Location);

                foreach (Item child in item.Items)
                {
                    if (child.Container == item)
                        UIManager.GetGump<ContainerGump>(child)?.Dispose();
                }

                if (_data.ClosedSound != 0)
                    CUOEnviroment.Client.Scene.Audio.PlaySound(_data.ClosedSound);
            }

            base.Dispose();
        }

        protected override void OnDragEnd(int x, int y)
        {
            if (ProfileManager.Current.OverrideContainerLocation && ProfileManager.Current.OverrideContainerLocationSetting == 2)
            {
                Point gumpCenter = new Point(X + (Width >> 1), Y + (Height >> 1));
                ProfileManager.Current.OverrideContainerLocationPosition = gumpCenter;
            }

            base.OnDragEnd(x, y);
        }
    }
}