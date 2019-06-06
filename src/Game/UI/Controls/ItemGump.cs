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

            WantUpdateSize = false;
        }

        public Item Item { get; }

        public bool HighlightOnMouseOver { get; set; }

        public bool CanPickUp { get; set; }


        public override void Update(double totalMS, double frameMS)
        {
            if (Item == null || Item.IsDestroyed)
                Dispose();

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
                {
                    GameActions.SingleClick(Item);
                }
                GameActions.OpenPopupMenu(Item);
                _sendClickIfNotDClick = false;
            }

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 hue = Vector3.Zero;
            ShaderHuesTraslator.GetHueVector(ref hue, MouseIsOver && HighlightOnMouseOver ? 0x0035 : Item.Hue, Item.ItemData.IsPartialHue, 0, true);

            batcher.Draw2D(Texture, x, y, ref hue);

            if (Item.Amount > 1 && Item.ItemData.IsStackable && Item.DisplayedGraphic == Item.Graphic)
                batcher.Draw2D(Texture, x + 5, y + 5, ref hue);

            return base.Draw(batcher, x, y);
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
            base.OnMouseUp(x, y, button);

            if (button == MouseButton.Left)
            {
                GameScene gs = Engine.SceneManager.GetScene<GameScene>();

                if (TargetManager.IsTargeting)
                {
                    if (Mouse.IsDragging && Mouse.LDroppedOffset != Point.Zero)
                    {
                        if (!gs.IsHoldingItem || !gs.IsMouseOverUI) return;

                        Game.SelectedObject.Object = Item;

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
                            Game.SelectedObject.Object = Item;


                            if (Item != null)
                            {
                                TargetManager.TargetGameObject(Item);
                                Mouse.LastLeftButtonClickTime = 0;
                            }

                            break;

                        case CursorTarget.SetTargetClientSide:
                            Game.SelectedObject.Object = Item;

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
                    if (!gs.IsHoldingItem || !gs.IsMouseOverUI) return;

                    Game.SelectedObject.Object = Item;

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

                if (Math.Abs(offset.X) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS || Math.Abs(offset.Y) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
                {
                    _clickedCanDrag = false;
                    AttempPickUp();
                }
            }
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            base.OnMouseClick(x, y, button);
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
                    _lastClickPosition.X = Mouse.Position.X;
                    _lastClickPosition.Y = Mouse.Position.Y;
                }
            }
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            GameActions.DoubleClick(Item);
            _sendClickIfNotDClick = false;
            _lastClickPosition = Point.Zero;

            return true;
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
    }
}