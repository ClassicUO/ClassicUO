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

namespace ClassicUO.Game.UI.Controls
{
    internal class ItemGump : Control
    {
        protected bool _clickedCanDrag;

        private Point _lastClickPosition;
        private float _picUpTime;
        private float _sClickTime;
        private bool _sendClickIfNotDClick;

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
            ArtTexture texture = FileManager.Art.GetTexture(item.DisplayedGraphic);
            Texture = texture;

            Width = texture.Width;
            Height = texture.Height;
            LocalSerial = item;

            WantUpdateSize = false;
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

            if (_sendClickIfNotDClick && totalMS >= _sClickTime)
            {
                if (!World.ClientFeatures.TooltipsEnabled) GameActions.SingleClick(LocalSerial);
                GameActions.OpenPopupMenu(LocalSerial);
                _sendClickIfNotDClick = false;
            }

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (Item == null || Item.IsDestroyed)
            {
                Dispose();
            }

            if (IsDisposed)
                return false;

            ResetHueVector();
            ShaderHuesTraslator.GetHueVector(ref _hueVector, MouseIsOver && HighlightOnMouseOver ? 0x0035 : Item.Hue, Item.ItemData.IsPartialHue, 0, true);

            batcher.Draw2D(Texture, x, y, Width, Height, ref _hueVector);

            if (Item.Amount > 1 && Item.ItemData.IsStackable && Item.DisplayedGraphic == Item.Graphic)
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

        protected override void OnMouseDown(int x, int y, MouseButton button)
        {
            if (button != MouseButton.Left)
                return;

            _lastClickPosition.X = Mouse.Position.X;
            _lastClickPosition.Y = Mouse.Position.Y;

            if (TargetManager.IsTargeting)
            {
                if (Mouse.IsDragging && Mouse.LDroppedOffset != Point.Zero)
                    return;
            }

            _clickedCanDrag = true;
            _picUpTime = Time.Ticks + 500f;
        }

        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            base.OnMouseUp(x, y, button);

            if (button == MouseButton.Left)
            {
                GameScene gs = CUOEnviroment.Client.GetScene<GameScene>();
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
                        if (!gs.IsHoldingItem || !gs.IsMouseOverUI) 
                            return;

                        SelectedObject.Object = Item;

                        if (Item.ItemData.IsContainer)
                            gs.DropHeldItemToContainer(Item);
                        else if (gs.HeldItem.Graphic == Item.Graphic && gs.HeldItem.IsStackable)
                            gs.MergeHeldItem(Item);
                        else
                        {
                            if (Item.Container.IsItem)
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
                                TargetManager.TargetGameObject(Item);
                                Mouse.LastLeftButtonClickTime = 0;
                            }

                            break;

                        case CursorTarget.SetTargetClientSide:
                            SelectedObject.Object = Item;

                            if (Item != null)
                            {
                                TargetManager.TargetGameObject(Item);
                                Mouse.LastLeftButtonClickTime = 0;
                                UIManager.Add(new InfoGump(Item));
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
                    if (!gs.IsHoldingItem || !gs.IsMouseOverUI)
                    {
                        if (_clickedCanDrag)
                        {
                            _clickedCanDrag = false;
                            _sendClickIfNotDClick = true;
                            float totalMS = Time.Ticks;
                            _sClickTime = totalMS + Mouse.MOUSE_DELAY_DOUBLE_CLICK;
                            _lastClickPosition.X = Mouse.Position.X;
                            _lastClickPosition.Y = Mouse.Position.Y;
                        }
                    }
                    else
                    {
                        SelectedObject.Object = Item;

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


        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            if (button != MouseButton.Left)
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
 
            _sendClickIfNotDClick = false;
            _lastClickPosition = Point.Zero;
 
            return true;
        }


        private void AttempPickUp()
        {
            if (CanPickUp)
            {
                // fetch texture for item
                Rectangle bounds = Texture.Bounds;
                Point offset = Point.Zero;

                if (this is ItemGumpPaperdoll)
                {
                    if (Item == null || Item.IsDestroyed)
                        Dispose();

                    if (IsDisposed)
                        return;

                    // fetch DisplayedGraphic for paperdoll item
                    bounds = FileManager.Art.GetTexture(Item.DisplayedGraphic).Bounds;
                }
                else if (Parent != null && Parent is ContainerGump)
                {
                    float scale = 1;
                    if (ProfileManager.Current != null && ProfileManager.Current.ScaleItemsInsideContainers)
                        scale = UIManager.ContainerScale;

                    // drag with mouse offset from containers
                    offset = new Point(
                        (int)((_lastClickPosition.X - (ParentX + X)) / scale),
                        (int)((_lastClickPosition.Y - (ParentY + Y)) / scale));
                }

                if (offset == Point.Zero) // drag from center by default
                    offset = new Point(bounds.Width >> 1, bounds.Height >> 1);

                GameActions.PickUp(LocalSerial, offset);
            }
        }
    }
}
