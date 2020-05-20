#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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
using System.Xml;

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
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

        public ContainerGump(uint serial, ushort gumpid, bool playsound) : base(serial, 0)
        {
            Item item = World.Items.Get(serial);

            if (item == null)
            {
                Dispose();
                return;
            }

            Graphic = gumpid;

            BuildGump();

            if (_data.OpenSound != 0 && playsound)
                Client.Game.Scene.Audio.PlaySound(_data.OpenSound);
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
            CanCloseWithRightClick = true;
            WantUpdateSize = false;
            _isCorspeContainer = Graphic == 0x0009;

          
            Item item = World.Items.Get(LocalSerial);

            if (item == null)
            {
                Dispose();
                return;
            }
            
            float scale = UIManager.ContainerScale;

            _data = ContainerManager.Get(Graphic);
            ushort g = _data.Graphic;


            _gumpPicContainer?.Dispose();
            _hitBox?.Dispose();

            _hitBox = new HitBox((int) (_data.MinimizerArea.X * scale), (int) (_data.MinimizerArea.Y * scale), (int) (_data.MinimizerArea.Width * scale), (int) (_data.MinimizerArea.Height * scale));
            _hitBox.MouseUp += HitBoxOnMouseUp;
            Add(_hitBox);

            Add(_gumpPicContainer = new GumpPicContainer(0, 0, g, 0));
            _gumpPicContainer.MouseDoubleClick += GumpPicContainerOnMouseDoubleClick;
            if (_isCorspeContainer)
            {
                //if (World.Player.ManualOpenedCorpses.Contains(LocalSerial))
                //    World.Player.ManualOpenedCorpses.Remove(LocalSerial);
                //else if(World.Player.AutoOpenedCorpses.Contains(LocalSerial) &&
                //ProfileManager.Current != null && ProfileManager.Current.SkipEmptyCorpse)
                //{
                //    IsVisible = false;
                //    _hideIfEmpty = true;
                //}

                _eyeGumpPic?.Dispose();
                Add(_eyeGumpPic = new GumpPic((int) (45 * scale), (int) (30 * scale), 0x0045, 0));

                _eyeGumpPic.Width = (int)(_eyeGumpPic.Width * scale);
                _eyeGumpPic.Height = (int)(_eyeGumpPic.Height * scale);
            }


            Width = _gumpPicContainer.Width = (int)(_gumpPicContainer.Width * scale);
            Height = _gumpPicContainer.Height = (int) (_gumpPicContainer.Height * scale);
        }

        private void HitBoxOnMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtonType.Left && !IsMinimized && !ItemHold.Enabled)
            {
                Point offset = Mouse.LDroppedOffset;

                if (Math.Abs(offset.X) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS &&
                    Math.Abs(offset.Y) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
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

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left || UIManager.IsMouseOverWorld)
                return;

            uint serial = SelectedObject.Object is Entity it ? it.Serial : 0;
            uint dropcontainer = LocalSerial;

            if (TargetManager.IsTargeting && !ItemHold.Enabled && SerialHelper.IsValid(serial))
            {
                TargetManager.Target(serial);
                Mouse.CancelDoubleClick = true;
            }
            else
            {
                Entity thisCont = World.Items.Get(dropcontainer);
                if (thisCont == null)
                    return;

                thisCont = World.Get(((Item) thisCont).RootContainer);
                if (thisCont == null)
                    return;

                bool candrop = thisCont.Distance <= Constants.DRAG_ITEMS_DISTANCE;

                if (candrop && SerialHelper.IsValid(serial))
                {
                    candrop = false;

                    if (ItemHold.Enabled)
                    {
                        candrop = true;

                        Item target = World.Items.Get(serial);

                        if (target != null)
                        {
                            if (target.ItemData.IsContainer)
                            {
                                dropcontainer = target.Serial;
                            }
                            else if (target.ItemData.IsStackable && target.Graphic == ItemHold.Graphic)
                            {
                                dropcontainer = target.Serial;
                            }
                            else
                            {
                                switch (target.Graphic)
                                {
                                    case 0x0EFA:
                                    case 0x2253:
                                    case 0x2252:
                                    case 0x238C:
                                    case 0x23A0:
                                    case 0x2D50:
                                    {
                                        dropcontainer = target.Serial;
                                        break;
                                    }
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                }

                if (!candrop && ItemHold.Enabled)
                {
                    Client.Game.Scene.Audio.PlaySound(0x0051);
                }

                if (candrop && ItemHold.Enabled)
                {
                    ((GameScene) Client.Game.Scene).DropHeldItemToContainer(World.Items.Get(dropcontainer), x, y);
                    Mouse.CancelDoubleClick = true;
                }
                else if (!ItemHold.Enabled && SerialHelper.IsValid(serial))
                {
                    if (!DelayedObjectClickManager.IsEnabled)
                    {
                        DelayedObjectClickManager.Set(serial,
                                                      Mouse.Position.X - ScreenCoordinateX,
                                                      Mouse.Position.Y - ScreenCoordinateY,
                                                      Time.Ticks + Mouse.MOUSE_DELAY_DOUBLE_CLICK);
                    }
                }
            }
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (IsDisposed)
                return;

            Item item = World.Items.Get(LocalSerial);

            if (item == null || item.IsDestroyed)
            {
                Dispose();
                return;
            }

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


        protected override void UpdateContents()
        {
            Clear();
            BuildGump();
            IsMinimized = IsMinimized;
            ItemsOnAdded();
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
            Client.Game.GetScene<GameScene>()?.DoubleClickDelayed(LocalSerial);
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

            Client.Game.GetScene<GameScene>()?.DoubleClickDelayed(LocalSerial);
            Dispose();
        }

        

        private void ItemsOnAdded()
        {
            Entity container = World.Get(LocalSerial);

            if (container == null)
                return;

            bool is_chessboard = Graphic == 0x091A || Graphic == 0x092E;
            const ushort CHESSBOARD_OFFSET = 11369;
            bool is_corpse = container.Graphic == 0x2006;

            for (var i = container.Items; i != null; i = i.Next)
            {
                var item = (Item) i;

                if (item.Layer == 0 || (is_corpse && Constants.BAD_CONTAINER_LAYERS[(int) item.Layer] && item.Amount > 0))
                {
                    var itemControl = new ItemGump(item.Serial,
                                                   item.DisplayedGraphic,
                                                   //(ushort) (item.DisplayedGraphic - (is_chessboard ? 0 : 0)), 
                                                   item.Hue,
                                                   item.X,
                                                   item.Y);

                    if (is_chessboard)
                        itemControl.IsPartialHue = false;

                    itemControl.IsVisible = !IsMinimized;

                    float scale = UIManager.ContainerScale;

                    if (ProfileManager.Current != null && ProfileManager.Current.ScaleItemsInsideContainers)
                    {
                        itemControl.Width = (int) (itemControl.Width * scale);
                        itemControl.Height = (int) (itemControl.Height * scale);
                    }

                    itemControl.X = (int) (item.X * scale);
                    itemControl.Y = (int) ((item.Y - (is_chessboard ? 20 : 0)) * scale);

                    //if ((_hideIfEmpty && !IsVisible))
                    //    IsVisible = true;

                    Add(itemControl);
                }
            }
        }
    

        public void CheckItemControlPosition(Item item)
        {
            var bounds = _data.Bounds;
            bool is_chessboard = Graphic == 0x091A || Graphic == 0x092E;

            ushort boundX = (ushort) (bounds.X);
            ushort boundY = (ushort) (bounds.Y);
            ushort boundWidth = (ushort) (bounds.Width);
            ushort boundHeight = (ushort) (bounds.Height + (is_chessboard ? 20 : 0));

            ArtTexture texture = ArtLoader.Instance.GetTexture(item.DisplayedGraphic);

            if (texture != null)
            {
                boundWidth -= (ushort)  (texture.Width  / UIManager.ContainerScale);
                boundHeight -= (ushort) (texture.Height / UIManager.ContainerScale);
            }

            if (item.X < boundX)
                item.X = boundX;
            else if (item.X > boundWidth)
                item.X = boundWidth;

            if (item.Y < boundY)
                item.Y = boundY;
            else if (item.Y > boundHeight)
                item.Y = boundHeight;
        }


        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            if (CUOEnviroment.Debug && !IsMinimized)
            {
                var bounds = _data.Bounds;
                float scale = UIManager.ContainerScale;
                ushort boundX = (ushort) (bounds.X * scale);
                ushort boundY = (ushort) (bounds.Y * scale);
                ushort boundWidth = (ushort) (bounds.Width * scale);
                ushort boundHeight = (ushort) (bounds.Height * scale);

                ResetHueVector();
                batcher.DrawRectangle(Texture2DCache.GetTexture(Color.Red), x + boundX, y + boundY, boundWidth - boundX, boundHeight - boundY, ref _hueVector);
            }

            return true;
        }


        public override void Dispose()
        {
            TextContainer.Clear();

            Item item = World.Items.Get(LocalSerial);

            if (item != null)
            {
                if (World.Player != null && (ProfileManager.Current?.OverrideContainerLocationSetting == 3))
                    UIManager.SavePosition(item, Location);

                for (var i = item.Items; i != null; i = i.Next)
                {
                    Item child = (Item) i;

                    if (child.Container == item)
                        UIManager.GetGump<ContainerGump>(child)?.Dispose();
                }
            }

            base.Dispose();
        }

        protected override void CloseWithRightClick()
        {
            base.CloseWithRightClick();

            if (_data.ClosedSound != 0)
                Client.Game.Scene.Audio.PlaySound(_data.ClosedSound);
        }

        protected override void OnDragEnd(int x, int y)
        {
            if (ProfileManager.Current.OverrideContainerLocation && ProfileManager.Current.OverrideContainerLocationSetting >= 2)
            {
                Point gumpCenter = new Point(X + (Width >> 1), Y + (Height >> 1));
                ProfileManager.Current.OverrideContainerLocationPosition = gumpCenter;
            }

            base.OnDragEnd(x, y);
        }

        private class GumpPicContainer : GumpPic
        {
            public GumpPicContainer(int x, int y, ushort graphic, ushort hue) : base(x, y, graphic, hue)
            {
            }

            //protected override void OnMouseUp(int x, int y, MouseButtonType button)
            //{
            //    base.OnMouseUp(x, y, button);

            //    //if (button != MouseButtonType.Left)
            //    //    return;

            //    //GameScene gs = Client.Game.GetScene<GameScene>();

            //    //if (!ItemHold.Enabled || !gs.IsMouseOverUI)
            //    //    return;

            //    //if (Item.Layer == Layer.Backpack || !Item.OnGround || Item.Distance < Constants.DRAG_ITEMS_DISTANCE)
            //    //{
            //    //    SelectedObject.Object = Item;
            //    //    gs.DropHeldItemToContainer(Item, x, y);
            //    //}
            //    //else
            //    //    gs.Audio.PlaySound(0x0051);
            //}

            public override bool Contains(int x, int y)
            {
                float scale = UIManager.ContainerScale;

                x = (int) (x / scale);
                y = (int) (y / scale);

                return base.Contains(x, y);
            }
        }
    }
}