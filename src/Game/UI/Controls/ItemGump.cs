﻿#region license
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

using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;
using ClassicUO.Game.Data;

using Microsoft.Xna.Framework;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.UI.Controls
{
    internal class ItemGump : Control
    {
        protected bool _clickedCanDrag;

        private float _picUpTime;
        //private float _sClickTime;
        //private bool _sendClickIfNotDClick;

        public ItemGump(Item item)
        {
            if (item == null)
            {
                Dispose();
                return;
            }

            Item = item;
            AcceptMouseInput = true;
            X = item.X;
            Y = item.Y;
            HighlightOnMouseOver = true;
            CanPickUp = true;
            ArtTexture texture = ArtLoader.Instance.GetTexture(item.DisplayedGraphic);
            Texture = texture;

            Width = texture.Width;
            Height = texture.Height;
            LocalSerial = item;

            WantUpdateSize = false;

            SetTooltip(item);
        }


        public bool HighlightOnMouseOver { get; set; }

        public bool CanPickUp { get; set; }

        public Item Item { get; }

        public override void Update(double totalMS, double frameMS)
        {
            if (Item == null || Item.IsDestroyed)
            {
                Dispose();
            }

            if (IsDisposed)
                return;

            Texture.Ticks = (long) totalMS;

            if (_clickedCanDrag && totalMS >= _picUpTime)
            {
                _clickedCanDrag = false;
                AttempPickUp();
            }

            //if (_sendClickIfNotDClick && totalMS >= _sClickTime)
            //{
            //    if (!World.ClientFeatures.TooltipsEnabled) 
            //        GameActions.SingleClick(LocalSerial);
            //    GameActions.OpenPopupMenu(LocalSerial);
            //    _sendClickIfNotDClick = false;
            //}

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (Item == null || Item.IsDestroyed)
            {
                Dispose();
            }

            if (IsDisposed || !Item.AllowedToDraw)
                return false;

            ResetHueVector();
            ShaderHuesTraslator.GetHueVector(ref _hueVector, MouseIsOver && HighlightOnMouseOver ? 0x0035 : Item.Hue, Item.ItemData.IsPartialHue, 0, true);

            batcher.Draw2D(Texture, x, y, Width, Height, ref _hueVector);

            if (!Item.IsMulti && !Item.IsCoin && Item.Amount > 1 && Item.ItemData.IsStackable)
                batcher.Draw2D(Texture, x + 5, y + 5, Width, Height, ref _hueVector);

            return base.Draw(batcher, x, y);
        }

        public override bool Contains(int x, int y)
        {
            if (ProfileManager.Current != null && ProfileManager.Current.ScaleItemsInsideContainers)
            {
                float scale = UIManager.ContainerScale;

                x = (int)(x / scale);
                y = (int)(y / scale);
            }

            if (Texture.Contains(x, y))
                return true;

            if (Item == null || Item.IsDestroyed)
            {
                Dispose();
            }

            if (IsDisposed)
                return false;

            if (!Item.IsCoin && Item.Amount > 1 && Item.ItemData.IsStackable)
            {
                if (Texture.Contains(x - 5, y - 5))
                    return true;
            }

            return false;
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left)
                return;

            if (TargetManager.IsTargeting)
            {
                if (Mouse.IsDragging && Mouse.LDroppedOffset != Point.Zero)
                    return;
            }

            _clickedCanDrag = true;
            _picUpTime = Time.Ticks + 500f;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);

            if (button == MouseButtonType.Left)
            {
                GameScene gs = Client.Game.GetScene<GameScene>();
                if (gs == null)
                    return;

                if (Item == null || Item.IsDestroyed)
                {
                    Dispose();
                }

                if (IsDisposed)
                    return;

                if (TargetManager.IsTargeting)
                {
                    _clickedCanDrag = false;

                    if (Mouse.IsDragging && CanPickup())
                    {
                        if (!ItemHold.Enabled || !gs.IsMouseOverUI) 
                            return;

                        SelectedObject.Object = Item;

                        if (Item.ItemData.IsContainer)
                            gs.DropHeldItemToContainer(Item);
                        else if (ItemHold.Graphic == Item.Graphic && ItemHold.IsStackable)
                            gs.MergeHeldItem(Item);
                        else
                        {
                            if (SerialHelper.IsItem(Item.Container))
                                gs.DropHeldItemToContainer(World.Items.Get(Item.Container), X + (Mouse.Position.X - ScreenCoordinateX), Y + (Mouse.Position.Y - ScreenCoordinateY));
                        }

                        return;
                    }

                    switch (TargetManager.TargetingState)
                    {
                        case CursorTarget.Position:
                        case CursorTarget.Object:
                        case CursorTarget.Grab:
                        case CursorTarget.SetGrabBag:
                            SelectedObject.Object = Item;


                            if (Item != null)
                            {
                                TargetManager.Target(Item);
                                Mouse.LastLeftButtonClickTime = 0;
                            }

                            break;

                        case CursorTarget.SetTargetClientSide:
                            SelectedObject.Object = Item;

                            if (Item != null)
                            {
                                TargetManager.Target(Item);
                                Mouse.LastLeftButtonClickTime = 0;
                                UIManager.Add(new InspectorGump(Item));
                            }

                            break;

                        case CursorTarget.HueCommandTarget:
                            SelectedObject.Object = Item;

                            if (Item != null)
                            {
                                CommandManager.OnHueTarget(Item);
                            }

                            break;

                    }
                }
                else
                {
                    if (!ItemHold.Enabled || !gs.IsMouseOverUI)
                    {
                        //if (_clickedCanDrag)
                        //{
                        //    _clickedCanDrag = false;
                        //    _sendClickIfNotDClick = true;
                        //    _sClickTime = Time.Ticks + Mouse.MOUSE_DELAY_DOUBLE_CLICK;
                        //}
                        if (!DelayedObjectClickManager.IsEnabled)
                        {
                            DelayedObjectClickManager.Set(Item.Serial, Mouse.Position.X, Mouse.Position.Y, Time.Ticks + Mouse.MOUSE_DELAY_DOUBLE_CLICK);
                        }
                    }
                    else
                    {
                        SelectedObject.Object = Item;

                        if (Item.ItemData.IsContainer)
                            gs.DropHeldItemToContainer(Item);
                        else if (ItemHold.Graphic == Item.Graphic && ItemHold.IsStackable)
                            gs.MergeHeldItem(Item);
                        else
                        {
                            if (SerialHelper.IsItem(Item.Container))
                                gs.DropHeldItemToContainer(World.Items.Get(Item.Container), X + (Mouse.Position.X - ScreenCoordinateX), Y + (Mouse.Position.Y - ScreenCoordinateY));
                        }
                    }                   
                }

                _clickedCanDrag = false;
            }
        }

        protected override void OnMouseOver(int x, int y)
        {
            if (_clickedCanDrag)
            {
                if (CanPickup())
                {
                    _clickedCanDrag = false;
                    AttempPickUp();
                }
            }
        }

        private bool CanPickup()
        {
            Point offset = Mouse.LDroppedOffset;
            var split = UIManager.GetGump<SplitMenuGump>(LocalSerial);

            split?.Dispose();

            return (split != null || (Math.Abs(offset.X) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS || Math.Abs(offset.Y) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS));
        }


        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left)
                return false;
 
            Item container;

            if ( !Input.Keyboard.Ctrl &&
                ProfileManager.Current.DoubleClickToLootInsideContainers &&
                Item != null && !Item.IsDestroyed &&
                !Item.ItemData.IsContainer && Item.Items.Count == 0 &&
                (container = World.Items.Get(Item.RootContainer)) != null &&
                container != World.Player.Equipment[(int) Layer.Backpack]
            ){
                GameActions.GrabItem(Item, Item.Amount);
            } else
                GameActions.DoubleClick(LocalSerial);
 
            //_sendClickIfNotDClick = false;
            //_clickedCanDrag = false;
            //_sClickTime = _picUpTime = Time.Ticks + Mouse.MOUSE_DELAY_DOUBLE_CLICK;

            return true;
        }


        private void AttempPickUp()
        {
            if (CanPickUp)
            {
                Rectangle bounds = ArtLoader.Instance.GetTexture(Item.DisplayedGraphic).Bounds;
                int centerX = bounds.Width >> 1;
                int centerY = bounds.Height >> 1;

                if (this is ItemGumpPaperdoll)
                {
                    if (Item == null || Item.IsDestroyed)
                        Dispose();

                    if (IsDisposed)
                        return;

                    GameActions.PickUp(LocalSerial, centerX, centerY);
                }
                else
                {
                    if (ProfileManager.Current != null && ProfileManager.Current.ScaleItemsInsideContainers)
                    {
                        float scale = UIManager.ContainerScale;
                        centerX = (int) (centerX * scale);
                        centerY = (int) (centerY * scale);
                    }

                    if (ProfileManager.Current != null && ProfileManager.Current.RelativeDragAndDropItems)
                    {
                        Point p = new Point(centerX - (Mouse.Position.X - ScreenCoordinateX), centerY - (Mouse.Position.Y - ScreenCoordinateY));
                        GameActions.PickUp(LocalSerial, centerX, centerY, offset: p);
                    }
                    else
                    {
                        GameActions.PickUp(LocalSerial, centerX, centerY);
                    }
                }
            }
        }
    }
}
