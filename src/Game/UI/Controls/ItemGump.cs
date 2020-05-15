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

using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Game.Data;

using Microsoft.Xna.Framework;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.UI.Controls
{
    internal class ItemGump : StaticPic
    {
        public ItemGump(uint serial, ushort graphic, ushort hue, int x, int y) : base(graphic, hue)
        {
            AcceptMouseInput = true;
            X = x;
            Y = y;
            HighlightOnMouseOver = true;
            CanPickUp = true;
            LocalSerial = serial;
            WantUpdateSize = false;
            CanMove = false;

            SetTooltip(serial);
        }


        public bool HighlightOnMouseOver { get; set; }
        public bool CanPickUp { get; set; }


        public override void Update(double totalMS, double frameMS)
        {
            if (IsDisposed)
                return;

            base.Update(totalMS, frameMS);

            if (CanPickUp && !ItemHold.Enabled && Mouse.LButtonPressed &&
                UIManager.LastControlMouseDown(MouseButtonType.Left) == this &&
                ((Mouse.LastLeftButtonClickTime != 0xFFFF_FFFF && Mouse.LastLeftButtonClickTime != 0 && Mouse.LastLeftButtonClickTime + Mouse.MOUSE_DELAY_DOUBLE_CLICK < Time.Ticks) ||
                CanPickup()))
            {
                AttempPickUp();
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed || ((ItemHold.Enabled || ItemHold.Dropped) && ItemHold.Serial == LocalSerial))
                return false;

            base.Draw(batcher, x, y);

            ResetHueVector();
            ShaderHuesTraslator.GetHueVector(ref _hueVector, HighlightOnMouseOver && MouseIsOver ? 0x0035 : Hue, IsPartialHue, 0, false);

            batcher.Draw2D(Texture, x, y, Width, Height, ref _hueVector);

            Item item = World.Items.Get(LocalSerial);

            if (item != null && !item.IsMulti && !item.IsCoin && item.Amount > 1 && item.ItemData.IsStackable)
            {
                batcher.Draw2D(Texture, x + 5, y + 5, Width, Height, ref _hueVector);
            }

            return true;
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
            {
                return true;
            }

            Item item = World.Items.Get(LocalSerial);

            if (item != null && !item.IsCoin && item.Amount > 1 && item.ItemData.IsStackable)
            {
                if (Texture.Contains(x - 5, y - 5))
                {
                    return true;
                }
            }

            return false;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                if (UIManager.MouseOverControl?.RootParent == RootParent)
                {
                    GameScene gs = Client.Game.GetScene<GameScene>();
                    if (gs != null)
                    {
                        Item item = World.Items.Get(LocalSerial);
                        if (item != null)
                        {
                            if (TargetManager.IsTargeting)
                            {
                                if (Mouse.IsDragging && CanPickup())
                                {
                                    if (ItemHold.Enabled && gs.IsMouseOverUI)
                                    {
                                        if (item.ItemData.IsContainer)
                                            gs.DropHeldItemToContainer(item);
                                        else if (ItemHold.Graphic == item.Graphic && ItemHold.IsStackable)
                                            gs.MergeHeldItem(item);
                                        else
                                        {
                                            if (SerialHelper.IsItem(item.Container))
                                                gs.DropHeldItemToContainer(World.Items.Get(item.Container), X + (Mouse.Position.X - ScreenCoordinateX), Y + (Mouse.Position.Y - ScreenCoordinateY));
                                        }

                                        Mouse.CancelDoubleClick = true;

                                        return;
                                    }                              
                                }

                                switch (TargetManager.TargetingState)
                                {
                                    case CursorTarget.Position:
                                    case CursorTarget.Object:
                                    case CursorTarget.Grab:
                                    case CursorTarget.SetGrabBag:

                                        if (item != null)
                                        {
                                            var p = RootParent;

                                            if (p != null)
                                            {
                                                DelayedObjectClickManager.X = Mouse.Position.X - p.ScreenCoordinateX;
                                                DelayedObjectClickManager.Y = Mouse.Position.Y - p.ScreenCoordinateY;
                                            }

                                            TargetManager.Target(item);
                                            Mouse.LastLeftButtonClickTime = 0;
                                        }

                                        break;

                                    case CursorTarget.SetTargetClientSide:

                                        if (item != null)
                                        {
                                            TargetManager.Target(item);
                                            Mouse.LastLeftButtonClickTime = 0;
                                            UIManager.Add(new InspectorGump(item));
                                        }

                                        break;

                                    case CursorTarget.HueCommandTarget:

                                        if (item != null)
                                        {
                                            CommandManager.OnHueTarget(item);
                                        }

                                        break;
                                }

                                return;
                            }
                            else
                            {
                                Point offset = Mouse.LDroppedOffset;

                                if ((Math.Abs(offset.X) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS &&
                                     Math.Abs(offset.Y) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS) && 
                                    (!ItemHold.Enabled || !gs.IsMouseOverUI))
                                {
                                    if (!DelayedObjectClickManager.IsEnabled)
                                    {
                                        var p = RootParent;
                                        if (p != null)
                                            DelayedObjectClickManager.Set(LocalSerial,
                                                                         Mouse.Position.X - p.ScreenCoordinateX,
                                                                         Mouse.Position.Y - p.ScreenCoordinateY,
                                                                         Time.Ticks + Mouse.MOUSE_DELAY_DOUBLE_CLICK);

                                        return;
                                    }
                                }
                                else if (item != null)
                                {
                                    if (item.ItemData.IsContainer)
                                        gs.DropHeldItemToContainer(item);
                                    else if (ItemHold.Graphic == item.Graphic && ItemHold.IsStackable)
                                        gs.MergeHeldItem(item);
                                    else if (SerialHelper.IsItem(item.Container))
                                        gs.DropHeldItemToContainer(World.Items.Get(item.Container), X + (Mouse.Position.X - ScreenCoordinateX), Y + (Mouse.Position.Y - ScreenCoordinateY));
                                    else
                                        base.OnMouseUp(x, y, button);

                                    Mouse.CancelDoubleClick = true;

                                    return;
                                }
                            }
                        }    
                    }
                }
            }
            else
                base.OnMouseUp(x, y, button);
        }

        protected override void OnMouseOver(int x, int y)
        {
        }

        private bool CanPickup()
        {
            Point offset = Mouse.LDroppedOffset;
            if (Math.Abs(offset.X) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS &&
                Math.Abs(offset.Y) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
                return false;

            var split = UIManager.GetGump<SplitMenuGump>(LocalSerial);
            if (split == null)
                return true;

            split.X = Mouse.Position.X - 80;
            split.Y = Mouse.Position.Y - 40;
            UIManager.AttemptDragControl(split, Mouse.Position, true);
            split.BringOnTop();

            return false;
        }


        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left || TargetManager.IsTargeting)
                return false;

            Item item = World.Items.Get(LocalSerial);
            Item container;

            if ( !Input.Keyboard.Ctrl &&
                ProfileManager.Current.DoubleClickToLootInsideContainers &&
                item != null && !item.IsDestroyed &&
                !item.ItemData.IsContainer && item.IsEmpty &&
                (container = World.Items.Get(item.RootContainer)) != null &&
                container != World.Player.FindItemByLayer(Layer.Backpack)
            )
            {
                GameActions.GrabItem(LocalSerial, item.Amount);
            }
            else
                GameActions.DoubleClick(LocalSerial);
            
            return true;
        }


        private void AttempPickUp()
        {
            if (CanPickUp)
            {
                Rectangle bounds = ArtLoader.Instance.GetTexture(Graphic).Bounds;
                int centerX = bounds.Width >> 1;
                int centerY = bounds.Height >> 1;

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
