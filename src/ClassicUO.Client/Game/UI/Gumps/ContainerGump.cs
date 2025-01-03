// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.IO;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ContainerGump : TextContainerGump
    {
        private long _corpseEyeTicks;
        private ContainerData _data;
        private int _eyeCorspeOffset;
        private GumpPic _eyeGumpPic;
        private GumpPicContainer _gumpPicContainer;
        private readonly bool _hideIfEmpty;
        private HitBox _hitBox;
        private bool _isMinimized;

        internal const int CORPSES_GUMP = 0x0009;

        public ContainerGump(World world) : base(world, 0, 0) { }

        public ContainerGump(World world, uint serial, ushort gumpid, bool playsound) : base(world, serial, 0)
        {
            Item item = world.Items.Get(serial);

            if (item == null)
            {
                Dispose();

                return;
            }

            Graphic = gumpid;

            // New Backpack gumps. Client Version 7.0.53.1
            if (
                item == world.Player.FindItemByLayer(Layer.Backpack)
                && Client.Game.UO.Version >= ClassicUO.Utility.ClientVersion.CV_705301
                && ProfileManager.CurrentProfile != null
            )
            {
                var gumps = Client.Game.UO.Gumps;

                switch (ProfileManager.CurrentProfile.BackpackStyle)
                {
                    case 1:
                        if (gumps.GetGump(0x775E).Texture != null)
                        {
                            Graphic = 0x775E; // Suede Backpack
                        }

                        break;
                    case 2:
                        if (gumps.GetGump(0x7760).Texture != null)
                        {
                            Graphic = 0x7760; // Polar Bear Backpack
                        }

                        break;
                    case 3:
                        if (gumps.GetGump(0x7762).Texture != null)
                        {
                            Graphic = 0x7762; // Ghoul Skin Backpack
                        }

                        break;
                    default:
                        if (gumps.GetGump(0x003C).Texture != null)
                        {
                            Graphic = 0x003C; // Default Backpack
                        }

                        break;
                }
            }

            BuildGump();

            if (Graphic == CORPSES_GUMP)
            {
                if (world.Player.ManualOpenedCorpses.Contains(LocalSerial))
                {
                    world.Player.ManualOpenedCorpses.Remove(LocalSerial);
                }
                else if (
                    world.Player.AutoOpenedCorpses.Contains(LocalSerial)
                    && ProfileManager.CurrentProfile != null
                    && ProfileManager.CurrentProfile.SkipEmptyCorpse
                )
                {
                    IsVisible = false;
                    _hideIfEmpty = true;
                }
            }

            if (_data.OpenSound != 0 && playsound)
            {
                Client.Game.Audio.PlaySound(_data.OpenSound);
            }
        }

        public ushort Graphic { get; }

        public override GumpType GumpType => GumpType.Container;

        public bool IsMinimized
        {
            get => _isMinimized;
            set
            {
                //if (_isMinimized != value)
                {
                    _isMinimized = value;
                    _gumpPicContainer.Graphic = value ? _data.IconizedGraphic : Graphic;
                    float scale = GetScale();

                    Width = _gumpPicContainer.Width = (int)(_gumpPicContainer.Width * scale);
                    Height = _gumpPicContainer.Height = (int)(_gumpPicContainer.Height * scale);

                    foreach (Control c in Children)
                    {
                        c.IsVisible = !value;
                    }

                    _gumpPicContainer.IsVisible = true;

                    SetInScreen();
                }
            }
        }

        public bool IsChessboard =>
            Graphic == 0x091A;

        public bool IsBackgammonBoard =>
            Graphic == 0x092E;

        private void BuildGump()
        {
            CanMove = true;
            CanCloseWithRightClick = true;
            WantUpdateSize = false;

            Item item = World.Items.Get(LocalSerial);

            if (item == null)
            {
                Dispose();

                return;
            }

            float scale = GetScale();

            _data = World.ContainerManager.Get(Graphic);
            ushort g = _data.Graphic;

            _gumpPicContainer?.Dispose();
            _hitBox?.Dispose();

            _hitBox = new HitBox(
                (int)(_data.MinimizerArea.X * scale),
                (int)(_data.MinimizerArea.Y * scale),
                (int)(_data.MinimizerArea.Width * scale),
                (int)(_data.MinimizerArea.Height * scale)
            );

            _hitBox.MouseUp += HitBoxOnMouseUp;
            Add(_hitBox);

            Add(_gumpPicContainer = new GumpPicContainer(0, 0, g, 0));
            _gumpPicContainer.MouseDoubleClick += GumpPicContainerOnMouseDoubleClick;

            if (Graphic == CORPSES_GUMP)
            {
                _eyeGumpPic?.Dispose();
                Add(_eyeGumpPic = new GumpPic((int)(45 * scale), (int)(30 * scale), 0x0045, 0));

                _eyeGumpPic.Width = (int)(_eyeGumpPic.Width * scale);
                _eyeGumpPic.Height = (int)(_eyeGumpPic.Height * scale);
            }
            else if (ProfileManager.CurrentProfile.HueContainerGumps)
            {
                _gumpPicContainer.Hue = item.Hue;
            }

            Width = _gumpPicContainer.Width = (int)(_gumpPicContainer.Width * scale);
            Height = _gumpPicContainer.Height = (int)(_gumpPicContainer.Height * scale);
        }

        private void HitBoxOnMouseUp(object sender, MouseEventArgs e)
        {
            if (
                e.Button == MouseButtonType.Left
                && !IsMinimized
                && !Client.Game.UO.GameCursor.ItemHold.Enabled
            )
            {
                Point offset = Mouse.LDragOffset;

                if (
                    Math.Abs(offset.X) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS
                    && Math.Abs(offset.Y) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS
                )
                {
                    IsMinimized = true;
                }
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
            {
                return;
            }

            Entity it = SelectedObject.Object as Entity;
            uint serial = it != null ? it.Serial : 0;
            uint dropcontainer = LocalSerial;

            if (
                World.TargetManager.IsTargeting
                && !Client.Game.UO.GameCursor.ItemHold.Enabled
                && SerialHelper.IsValid(serial)
            )
            {
                World.TargetManager.Target(serial);
                Mouse.CancelDoubleClick = true;

                if (World.TargetManager.TargetingState == CursorTarget.SetTargetClientSide)
                {
                    UIManager.Add(new InspectorGump(World, World.Get(serial)));
                }
            }
            else
            {
                Entity thisCont = World.Items.Get(dropcontainer);

                if (thisCont == null)
                {
                    return;
                }

                thisCont = World.Get(((Item)thisCont).RootContainer);

                if (thisCont == null)
                {
                    return;
                }

                bool candrop = thisCont.Distance <= Constants.DRAG_ITEMS_DISTANCE;

                if (candrop && SerialHelper.IsValid(serial))
                {
                    candrop = false;

                    if (
                        Client.Game.UO.GameCursor.ItemHold.Enabled
                        && !Client.Game.UO.GameCursor.ItemHold.IsFixedPosition
                    )
                    {
                        candrop = true;

                        Item target = World.Items.Get(serial);

                        if (target != null)
                        {
                            if (target.ItemData.IsContainer)
                            {
                                dropcontainer = target.Serial;
                                x = 0xFFFF;
                                y = 0xFFFF;
                            }
                            else if (
                                target.ItemData.IsStackable
                                && target.Graphic == Client.Game.UO.GameCursor.ItemHold.Graphic
                            )
                            {
                                dropcontainer = target.Serial;
                                x = target.X;
                                y = target.Y;
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
                                        x = target.X;
                                        y = target.Y;

                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                if (
                    !candrop
                    && Client.Game.UO.GameCursor.ItemHold.Enabled
                    && !Client.Game.UO.GameCursor.ItemHold.IsFixedPosition
                )
                {
                    Client.Game.Audio.PlaySound(0x0051);
                }

                if (
                    candrop
                    && Client.Game.UO.GameCursor.ItemHold.Enabled
                    && !Client.Game.UO.GameCursor.ItemHold.IsFixedPosition
                )
                {
                    ContainerGump gump = UIManager.GetGump<ContainerGump>(dropcontainer);

                    if (
                        gump != null
                        && (
                            it == null
                            || it.Serial != dropcontainer
                                && it is Item item
                                && !item.ItemData.IsContainer
                        )
                    )
                    {
                        if (gump.IsChessboard)
                        {
                            y += 20;
                        }

                        Rectangle containerBounds = World.ContainerManager.Get(gump.Graphic).Bounds;

                        ref readonly var spriteInfo = ref (
                            (gump.IsChessboard || gump.IsBackgammonBoard)
                                ? ref Client.Game.UO.Gumps.GetGump(
                                    (ushort)(
                                        Client.Game.UO.GameCursor.ItemHold.DisplayedGraphic
                                        - Constants.ITEM_GUMP_TEXTURE_OFFSET
                                    )
                                )
                                : ref Client.Game.UO.Arts.GetArt(Client.Game.UO.GameCursor.ItemHold.DisplayedGraphic)
                        );

                        float scale = GetScale();

                        containerBounds.X = (int)(containerBounds.X * scale);
                        containerBounds.Y = (int)(containerBounds.Y * scale);
                        containerBounds.Width = (int)(containerBounds.Width * scale);
                        containerBounds.Height = (int)(
                            (containerBounds.Height + (gump.IsChessboard ? 20 : 0)) * scale
                        );

                        if (spriteInfo.Texture != null)
                        {
                            int textureW,
                                textureH;

                            if (
                                ProfileManager.CurrentProfile != null
                                && ProfileManager.CurrentProfile.ScaleItemsInsideContainers
                            )
                            {
                                textureW = (int)(spriteInfo.UV.Width * scale);
                                textureH = (int)(spriteInfo.UV.Height * scale);
                            }
                            else
                            {
                                textureW = spriteInfo.UV.Width;
                                textureH = spriteInfo.UV.Height;
                            }

                            if (
                                ProfileManager.CurrentProfile != null
                                && ProfileManager.CurrentProfile.RelativeDragAndDropItems
                            )
                            {
                                x += Client.Game.UO.GameCursor.ItemHold.MouseOffset.X;
                                y += Client.Game.UO.GameCursor.ItemHold.MouseOffset.Y;
                            }

                            x -= textureW >> 1;
                            y -= textureH >> 1;

                            if (x + textureW > containerBounds.Width)
                            {
                                x = containerBounds.Width - textureW;
                            }

                            if (y + textureH > containerBounds.Height)
                            {
                                y = containerBounds.Height - textureH;
                            }
                        }

                        if (x < containerBounds.X)
                        {
                            x = containerBounds.X;
                        }

                        if (y < containerBounds.Y)
                        {
                            y = containerBounds.Y;
                        }

                        x = (int)(x / scale);
                        y = (int)(y / scale);
                    }

                    GameActions.DropItem(
                        Client.Game.UO.GameCursor.ItemHold.Serial,
                        x,
                        y,
                        0,
                        dropcontainer
                    );

                    Mouse.CancelDoubleClick = true;
                }
                else if (!Client.Game.UO.GameCursor.ItemHold.Enabled && SerialHelper.IsValid(serial))
                {
                    if (!World.DelayedObjectClickManager.IsEnabled)
                    {
                        Point off = Mouse.LDragOffset;

                        World.DelayedObjectClickManager.Set(
                            serial,
                            Mouse.Position.X - off.X - ScreenCoordinateX,
                            Mouse.Position.Y - off.Y - ScreenCoordinateY,
                            Time.Ticks + Mouse.MOUSE_DELAY_DOUBLE_CLICK
                        );
                    }
                }
            }
        }

        public override void Update()
        {
            base.Update();

            if (IsDisposed)
            {
                return;
            }

            Item item = World.Items.Get(LocalSerial);

            if (item == null || item.IsDestroyed)
            {
                Dispose();

                return;
            }

            if (
                UIManager.MouseOverControl != null
                && UIManager.MouseOverControl.RootParent == this
                && ProfileManager.CurrentProfile != null
                && ProfileManager.CurrentProfile.HighlightContainerWhenSelected
            )
            {
                SelectedObject.SelectedContainer = item;
            }

            if (Graphic == CORPSES_GUMP && _corpseEyeTicks < Time.Ticks)
            {
                _eyeCorspeOffset = _eyeCorspeOffset == 0 ? 1 : 0;
                _corpseEyeTicks = (long)Time.Ticks + 750;
                _eyeGumpPic.Graphic = (ushort)(0x0045 + _eyeCorspeOffset);
                float scale = GetScale();
                _eyeGumpPic.Width = (int)(_eyeGumpPic.Width * scale);
                _eyeGumpPic.Height = (int)(_eyeGumpPic.Height * scale);
            }
        }

        protected override void UpdateContents()
        {
            Clear();
            BuildGump();
            IsMinimized = IsMinimized;
            ItemsOnAdded();
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

        private float GetScale()
        {
            return (IsChessboard || IsBackgammonBoard) ? 1f : UIManager.ContainerScale;
        }

        private void ItemsOnAdded()
        {
            Entity container = World.Get(LocalSerial);

            if (container == null)
            {
                return;
            }

            bool is_corpse = container.Graphic == 0x2006;

            if (!container.IsEmpty && _hideIfEmpty && !IsVisible)
            {
                IsVisible = true;
            }

            for (LinkedObject i = container.Items; i != null; i = i.Next)
            {
                Item item = (Item)i;

                // NOTE: Switched from 'item.Layer' property which comes from server to 'ItemData.Layer' from tiledata.mul.
                //       In the past I found some issues using the server property.
                //       Probably lack of knowledge about some client behaviour.
                //       Remember it.

                if (item.Amount <= 0)
                {
                    continue;
                }

                var layer = (Layer)item.ItemData.Layer;

                if (is_corpse && item.Layer > 0 && !Constants.BAD_CONTAINER_LAYERS[(int)layer])
                {
                    continue;
                }

                // some items has layer = [face | beard | hair] and we need to check if it's a wearable item or not.
                // when the item is wearable we dont add it to the container.
                // Tested with --> client = 7.0.95.0 | graphic = 0x0A02
                if (
                    item.ItemData.IsWearable
                    && (layer == Layer.Face || layer == Layer.Beard || layer == Layer.Hair)
                )
                {
                    continue;
                }

                ItemGump itemControl = new ItemGump(
                    this,
                    item.Serial,
                    (ushort)(
                        item.DisplayedGraphic
                        - ((IsChessboard || IsBackgammonBoard) ? Constants.ITEM_GUMP_TEXTURE_OFFSET : 0)
                    ),
                    item.Hue,
                    item.X,
                    item.Y,
                    (IsChessboard || IsBackgammonBoard)
                );

                itemControl.IsVisible = !IsMinimized;

                float scale = GetScale();

                if (
                    ProfileManager.CurrentProfile != null
                    && ProfileManager.CurrentProfile.ScaleItemsInsideContainers
                )
                {
                    itemControl.Width = (int)(itemControl.Width * scale);
                    itemControl.Height = (int)(itemControl.Height * scale);
                }

                itemControl.X = (int)((short)item.X * scale);
                itemControl.Y = (int)(((short)item.Y - (IsChessboard ? 20 : 0)) * scale);

                Add(itemControl);
            }
        }

        public void CheckItemControlPosition(Item item)
        {
            Rectangle dataBounds = _data.Bounds;

            int boundX = dataBounds.X;
            int boundY = dataBounds.Y;
            int boundWidth = dataBounds.Width;
            int boundHeight = dataBounds.Height + (IsChessboard ? 20 : 0);

            ref readonly var spriteInfo = ref (
                (IsChessboard || IsBackgammonBoard)
                    ? ref Client.Game.UO.Gumps.GetGump(
                        (ushort)(
                            item.DisplayedGraphic
                            - Constants.ITEM_GUMP_TEXTURE_OFFSET
                        )
                    )
                    : ref Client.Game.UO.Arts.GetArt(item.DisplayedGraphic)
            );

            if (spriteInfo.Texture != null)
            {
                float scale = GetScale();

                boundWidth -= (int)(spriteInfo.UV.Width / scale);
                boundHeight -= (int)(spriteInfo.UV.Height / scale);
            }

            if (item.X < boundX)
            {
                item.X = (ushort)boundX;
            }
            else if (item.X > boundWidth)
            {
                item.X = (ushort)boundWidth;
            }

            if (item.Y < boundY)
            {
                item.Y = (ushort)boundY;
            }
            else if (item.Y > boundHeight)
            {
                item.Y = (ushort)boundHeight;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            if (CUOEnviroment.Debug && !IsMinimized)
            {
                Rectangle bounds = _data.Bounds;
                float scale = GetScale();
                ushort boundX = (ushort)(bounds.X * scale);
                ushort boundY = (ushort)(bounds.Y * scale);
                ushort boundWidth = (ushort)(bounds.Width * scale);
                ushort boundHeight = (ushort)(bounds.Height * scale);

                Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

                batcher.DrawRectangle(
                    SolidColorTextureCache.GetTexture(Color.Red),
                    x + boundX,
                    y + boundY,
                    boundWidth - boundX,
                    boundHeight - boundY,
                    hueVector
                );
            }

            return true;
        }

        public override void Dispose()
        {
            Item item = World.Items.Get(LocalSerial);

            if (item != null)
            {
                if (
                    World.Player != null
                    && ProfileManager.CurrentProfile?.OverrideContainerLocationSetting == 3
                )
                {
                    UIManager.SavePosition(item, Location);
                }

                for (LinkedObject i = item.Items; i != null; i = i.Next)
                {
                    Item child = (Item)i;

                    if (child.Container == item)
                    {
                        UIManager.GetGump<ContainerGump>(child)?.Dispose();
                    }
                }
            }

            base.Dispose();
        }

        protected override void CloseWithRightClick()
        {
            base.CloseWithRightClick();

            if (_data.ClosedSound != 0)
            {
                Client.Game.Audio.PlaySound(_data.ClosedSound);
            }
        }

        protected override void OnDragEnd(int x, int y)
        {
            if (
                ProfileManager.CurrentProfile.OverrideContainerLocation
                && ProfileManager.CurrentProfile.OverrideContainerLocationSetting >= 2
            )
            {
                Point gumpCenter = new Point(X + (Width >> 1), Y + (Height >> 1));
                ProfileManager.CurrentProfile.OverrideContainerLocationPosition = gumpCenter;
            }

            base.OnDragEnd(x, y);
        }

        private class GumpPicContainer : GumpPic
        {
            public GumpPicContainer(int x, int y, ushort graphic, ushort hue)
                : base(x, y, graphic, hue) { }

            public override bool Contains(int x, int y)
            {
                float scale =
                    Graphic == 0x091A || Graphic == 0x092E ? 1f : UIManager.ContainerScale;

                x = (int)(x / scale);
                y = (int)(y / scale);

                return base.Contains(x, y);
            }
        }
    }
}
