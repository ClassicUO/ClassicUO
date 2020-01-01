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
using System.Xml;

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ContainerGump : TextContainerGump
    {
        private long _corpseEyeTicks;
        private bool _hideIfEmpty;
        private ContainerData _data;
        private int _eyeCorspeOffset;
        private GumpPic _eyeGumpPic;
        private bool _isCorspeContainer;
        private bool _isMinimized;
        private GumpPicContainer _gumpPicContainer;
        private HitBox _hitBox;

        public ContainerGump() : base(0, 0)
        {
        }

        public ContainerGump(uint serial, ushort gumpid) : this()
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
            {
                var x = new ItemGump(i);
                x.Initialize();
                Add(x);
            }
        }

        public ushort Graphic { get; }

        public override GUMP_TYPE GumpType => GUMP_TYPE.GT_CONTAINER;

        public TextContainer TextContainer { get; } = new TextContainer();

        public bool IsMinimized
        {
            get => _isMinimized;
            set
            {
                //if (_isMinimized != value)
                {
                    _isMinimized = value;
                    _gumpPicContainer.Graphic = value ? _data.IconizedGraphic : Graphic;
                    float scale = UIManager.ContainerScale;

                    Width = _gumpPicContainer.Width = (int) (_gumpPicContainer.Width * scale);
                    Height = _gumpPicContainer.Height = (int) (_gumpPicContainer.Height * scale);

                    foreach (var c in Children)
                    {
                        if (!c.IsInitialized)
                            c.Initialize();
                        c.IsVisible = !value;
                    }

                    _gumpPicContainer.IsVisible = true;

                    SetInScreen();
                }
            }
        }

        private void BuildGump()
        {
            CanMove = true;
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
            ushort g = _data.Graphic;


            _gumpPicContainer?.Dispose();
            _hitBox?.Dispose();

            _hitBox = new HitBox((int) (_data.MinimizerArea.X * scale), (int) (_data.MinimizerArea.Y * scale), (int) (_data.MinimizerArea.Width * scale), (int) (_data.MinimizerArea.Height * scale));
            _hitBox.MouseUp += HitBoxOnMouseUp;
            Add(_hitBox);

            Add(_gumpPicContainer = new GumpPicContainer(0, 0, g, 0, item));
            _gumpPicContainer.MouseDoubleClick += GumpPicContainerOnMouseDoubleClick;
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


            Width = _gumpPicContainer.Width = (int)(_gumpPicContainer.Width * scale);
            Height = _gumpPicContainer.Height = (int) (_gumpPicContainer.Height * scale);

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

            // workaroud to force the children update
            if (IsInitialized)
            {
                IsMinimized = IsMinimized;
            }


            if (_data.OpenSound != 0)
                CUOEnviroment.Client.Scene.Audio.PlaySound(_data.OpenSound);
        }

        private void HitBoxOnMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtonType.Left && !IsMinimized)
            {
                IsMinimized = true;
            }
        }

        private void GumpPicContainerOnMouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
        {
            if (e.Button == MouseButtonType.Left && IsMinimized)
            {
                IsMinimized = false;
                e.Result = true;
            }
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

            if (_isCorspeContainer && _corpseEyeTicks < totalMS)
            {
                _eyeCorspeOffset = _eyeCorspeOffset == 0 ? 1 : 0;
                _corpseEyeTicks = (long) totalMS + 750;
                _eyeGumpPic.Graphic = (ushort) (0x0045 + _eyeCorspeOffset);
                float scale = UIManager.ContainerScale;
                _eyeGumpPic.Width = (int)(_eyeGumpPic.Texture.Width * scale);
                _eyeGumpPic.Height = (int)(_eyeGumpPic.Texture.Height * scale);
            }
        }

        public void ForceUpdate()
        {
            BuildGump();
            ItemsOnAdded(null, new CollectionChangedEventArgs<uint>(FindControls<ItemGump>().Select(s => s.LocalSerial)));
        }
        
        public override void Save(BinaryWriter writer)
        {
            base.Save(writer);
            writer.Write(LocalSerial);
            writer.Write(Graphic);
            writer.Write(IsMinimized);
        }

        public override void Restore(BinaryReader reader)
        {
            base.Restore(reader);

            if (Configuration.Profile.GumpsVersion == 2)
            {
                reader.ReadUInt32();
                _isMinimized = reader.ReadBoolean();
            }

            LocalSerial = reader.ReadUInt32();
            CUOEnviroment.Client.GetScene<GameScene>()?.DoubleClickDelayed(LocalSerial);
            reader.ReadUInt16();

            if (Profile.GumpsVersion >= 3)
            {
                _isMinimized = reader.ReadBoolean();
            }

            Dispose();
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            writer.WriteAttributeString("graphic", Graphic.ToString());
            writer.WriteAttributeString("isminimized", IsMinimized.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);
            // skip loading

            CUOEnviroment.Client.GetScene<GameScene>()?.DoubleClickDelayed(LocalSerial);
            Dispose();
        }

      
        private void ItemsOnRemoved(object sender, CollectionChangedEventArgs<uint> e)
        {
            foreach (ItemGump v in Children.OfType<ItemGump>().Where(s => e.Contains(s.LocalSerial)))
                v.Dispose();
        }

        private void ItemsOnAdded(object sender, CollectionChangedEventArgs<uint> e)
        {
            foreach (ItemGump v in Children.OfType<ItemGump>().Where(s => e.Contains(s.LocalSerial)))
                v.Dispose();

            foreach (uint s in e)
            {
                var item = World.Items.Get(s);

                if (item == null || !item.IsLootable)
                    continue;


                var itemControl = new ItemGump(item);
                itemControl.Initialize();
                itemControl.IsVisible = !IsMinimized;

                CheckItemControlPosition(itemControl, item);

                if (ProfileManager.Current != null && ProfileManager.Current.ScaleItemsInsideContainers)
                {
                    float scale = UIManager.ContainerScale;

                    itemControl.Width = (int)(itemControl.Width * scale);
                    itemControl.Height = (int)(itemControl.Height * scale);
                }

                if ((_hideIfEmpty && !IsVisible))
                    IsVisible = true;

                Add(itemControl);
            }
        }
    

        private void CheckItemControlPosition(ItemGump itemGump, Item item)
        {
            float scale = UIManager.ContainerScale;

            int x = (int) (itemGump.X * scale);
            int y = (int) (itemGump.Y * scale);
          
            ArtTexture texture = UOFileManager.Art.GetTexture(item.DisplayedGraphic);

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

        private void SetPositionNearGameObject(ushort g, Item item)
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
            else if (SerialHelper.IsMobile(item.Container))
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