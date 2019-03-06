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

using System;
using System.Collections.Generic;
using System.Linq;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class ItemGump : Control
    {
        protected bool _clickedCanDrag;
        private float _picUpTime;
        private float _sClickTime;
        private bool _sendClickIfNotDClick;

        public ItemGump(Item item)
        {
            AcceptMouseInput = true;
            Item = item;
            X = item.X;
            Y = item.Y;
            HighlightOnMouseOver = true;
            CanPickUp = true;
            ArtTexture texture = FileManager.Art.GetTexture(item.DisplayedGraphic);
            Texture = texture;
            Width = texture.Width;
            Height = texture.Height;
            LocalSerial = Item.Serial;

            Item.Disposed += ItemOnDisposed;

            WantUpdateSize = false;
            ShowLabel = true;

            if (!World.ClientFlags.TooltipsEnabled)
                Item.OverheadAdded += ItemOnOverheadAdded;
        }


        private void ItemOnOverheadAdded(object sender, EventArgs e)
        {
            LabelContainer container = Engine.UI.GetByLocalSerial<LabelContainer>(Item);

            if (container == null)
            {
                container = new LabelContainer(Item);
                Engine.UI.Add(container);
            }

            TextOverhead overhead = (TextOverhead) sender;

            overhead.TimeToLive = 4000;

            FadeOutLabel label = new FadeOutLabel(overhead.Text, overhead.IsUnicode, overhead.Hue, overhead.TimeToLive, overhead.MaxWidth, overhead.Font, overhead.Style, TEXT_ALIGN_TYPE.TS_CENTER);
            container.Add(label);
        }

        private void ItemOnDisposed(object sender, EventArgs e)
        {
            Dispose();
        }

        public Item Item { get; }

        public bool HighlightOnMouseOver { get; set; }

        public bool CanPickUp { get; set; }

        public bool ShowLabel { get; set; }

        public override void Update(double totalMS, double frameMS)
        {
            if (IsDisposed)
                return;

            Texture.Ticks = (long) totalMS;

            if (_clickedCanDrag && totalMS >= _picUpTime)
            {
                _clickedCanDrag = false;
                AttempPickUp();
            }

            if (_sendClickIfNotDClick && totalMS >= _sClickTime)
            {
                if (!World.ClientFlags.TooltipsEnabled)
                    GameActions.SingleClick(Item);
                GameActions.OpenPopupMenu(Item);
                _sendClickIfNotDClick = false;
            }

            if (ShowLabel)
                UpdateLabel();
            base.Update(totalMS, frameMS);
        }

        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {
            Vector3 huev = ShaderHuesTraslator.GetHueVector(MouseIsOver && HighlightOnMouseOver ? 0x0035 : Item.Hue, Item.ItemData.IsPartialHue, 0, false);
            batcher.Draw2D(Texture, position, huev);
            if (Item.Amount > 1 && Item.ItemData.IsStackable && Item.DisplayedGraphic == Item.Graphic)
                batcher.Draw2D(Texture, new Point(position.X + 5, position.Y + 5), huev);
            return base.Draw(batcher, position, huev);
        }

        protected override bool Contains(int x, int y)
        {
            if (Texture.Contains(x, y))
                return true;

            if (!Item.IsCoin && Item.Amount > 1 && Item.ItemData.IsStackable)
            {
                if (Texture.Contains(x - 5, y - 5))
                    return true;
            }

            return false;
        }

        protected override void OnMouseDown(int x, int y, MouseButton button)
        {
            if (button != MouseButton.Left)
                return;

            if (TargetManager.IsTargeting)
            {
                if (Mouse.IsDragging && Mouse.LDroppedOffset != Point.Zero)
                    return;
            }

            _clickedCanDrag = true;
            _picUpTime = Engine.Ticks + 500f;
        }

        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            _clickedCanDrag = false;

            if (button == MouseButton.Left)
            {              
                GameScene gs = Engine.SceneManager.GetScene<GameScene>();

                if (TargetManager.IsTargeting)
                {
                    if (Mouse.IsDragging && Mouse.LDroppedOffset != Point.Zero)
                        return;

                    switch (TargetManager.TargetingState)
                    {
                        case CursorTarget.Position:
                        case CursorTarget.Object:
                            gs.SelectedObject = Item;


                            if (Item != null)
                            {
                                TargetManager.TargetGameObject(Item);
                                Mouse.LastLeftButtonClickTime = 0;
                            }

                            break;

                        case CursorTarget.SetTargetClientSide:
                            gs.SelectedObject = Item;

                            if (Item != null)
                            {
                                TargetManager.TargetGameObject(Item);
                                Mouse.LastLeftButtonClickTime = 0;
                                Engine.UI.Add(new InfoGump(Item));
                            }
                            break;
                    }
                }
                else
                {

                    if (!gs.IsHoldingItem || !gs.IsMouseOverUI)
                    {
                        return;
                    }

                    gs.SelectedObject = Item;

                    if (Item.ItemData.IsContainer)
                        gs.DropHeldItemToContainer(Item);
                    else if (gs.HeldItem.Graphic == Item.Graphic && gs.HeldItem.IsStackable)
                        gs.MergeHeldItem(Item);
                    else
                    {
                        if (Item.Container.IsItem)
                            gs.DropHeldItemToContainer(World.Items.Get(Item.Container), X + (Mouse.Position.X - ScreenCoordinateX), Y + (Mouse.Position.Y - ScreenCoordinateY));
                    }
                }
            }
        }

        protected override void OnMouseOver(int x, int y)
        {
            if (_clickedCanDrag)
            {
                Point offset = Mouse.LDroppedOffset;

                if (Math.Abs(offset.X) > Constants.MIN_PICKUP_DRAG_DISTANCE || Math.Abs(offset.Y) > Constants.MIN_PICKUP_DRAG_DISTANCE)
                {
                    _clickedCanDrag = false;
                    AttempPickUp();
                }
            }
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {      
            if (button != MouseButton.Left)
                return;

            var gs = Engine.SceneManager.GetScene<GameScene>();

            if (gs == null || gs.IsHoldingItem)
                return;

            if (TargetManager.IsTargeting)
            {
                if (TargetManager.TargetingState == CursorTarget.Position || TargetManager.TargetingState == CursorTarget.Object)
                {
                    TargetManager.TargetGameObject(Item);
                    Mouse.LastLeftButtonClickTime = 0;
                }
            }
            else
            {
                if (_clickedCanDrag)
                {
                    _clickedCanDrag = false;
                    _sendClickIfNotDClick = true;
                    float totalMS = Engine.Ticks;
                    _sClickTime = totalMS + Mouse.MOUSE_DELAY_DOUBLE_CLICK;
                }
            }
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            GameActions.DoubleClick(Item);
            _sendClickIfNotDClick = false;

            return true;
        }

        public override void Dispose()
        {
            Engine.UI.GetByLocalSerial<LabelContainer>(Item)?.Dispose();
            if (!World.ClientFlags.TooltipsEnabled)
                Item.OverheadAdded -= ItemOnOverheadAdded;
            Item.Disposed -= ItemOnDisposed;
            base.Dispose();
        }

        private void AttempPickUp()
        {
            if (CanPickUp)
            {
                if (this is ItemGumpPaperdoll)
                {
                    Rectangle bounds = FileManager.Art.GetTexture(Item.DisplayedGraphic).Bounds;
                    GameActions.PickUp(Item, bounds.Width >> 1, bounds.Height >> 1);
                }
                else
                    GameActions.PickUp(Item, Point.Zero);
            }
        }

       
        protected virtual void UpdateLabel()
        {
            if (World.ClientFlags.TooltipsEnabled)
                return;

            if (!Item.IsDisposed && Item.Overheads.Count > 0)
            {
                LabelContainer container = Engine.UI.GetByLocalSerial<LabelContainer>(Item);

                if (container == null)
                {
                    container = new LabelContainer(Item);
                    Engine.UI.Add(container);
                }
                container.X = ScreenCoordinateX + (Width >> 1) /*- (container.Width >> 1)*/;
                container.Y = ScreenCoordinateY /*- (Height >> 1) */- (container.Height);
                
                Engine.UI.MakeTopMostGumpOverAnother(container, this);
            }
        }

       
        protected class LabelContainer : Gump
        {
            public LabelContainer(Item item) : base(item, 0)
            {
                AcceptMouseInput = false;
                CanMove = true;
                WantUpdateSize = false;
            }

            public override void Update(double totalMS, double frameMS)
            {
                if (Children.Count == 0)
                    Dispose();

                base.Update(totalMS, frameMS);
            }

            public override void Add(Control c, int page = 0)
            {
                c.X = Width - (c.Width >> 1);

                if (Children.Count > 0)
                {
                    var a = Children[Children.Count - 1];
                    c.Y = a.Y + a.Height;
                }

                if (Width > c.Width)
                    Width = c.Width;

                Height += c.Height;

                base.Add(c, page);
            }


            public override void Remove(Control c)
            {
                base.Remove(c);

                if (IsDisposed)
                    return;

                if (Width == c.Width)
                {
                    int newWidth = 0;
                    int newHeight = 0;
                    foreach (Control control in Children)
                    {
                        if (newWidth < control.Width)
                            newWidth = control.Width;
                        newHeight += control.Height;
                    }

                    Width = newWidth;
                    Height = newHeight;
                }

            }
        }

    }
}