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

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO;
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

        public ItemGump(Serial serial)
        {
            Item item = World.Items.Get(serial);

            if (item == null)
            {
                Dispose();
                return;
            }

            AcceptMouseInput = true;
            X = item.X;
            Y = item.Y;
            HighlightOnMouseOver = true;
            CanPickUp = true;
            ArtTexture texture = FileManager.Art.GetTexture(item.DisplayedGraphic);
            Texture = texture;

            Width = texture.Width;
            Height = texture.Height;
            LocalSerial = serial;

            WantUpdateSize = false;
        }


        public bool HighlightOnMouseOver { get; set; }

        public bool CanPickUp { get; set; }


        public override void Update(double totalMS, double frameMS)
        {
            Item item = World.Items.Get(LocalSerial);

            if (item == null)
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
            Item item = World.Items.Get(LocalSerial);

            if (item == null)
            {
                Dispose();
            }

            if (IsDisposed)
                return false;

            ResetHueVector();
            ShaderHuesTraslator.GetHueVector(ref _hueVector, MouseIsOver && HighlightOnMouseOver ? 0x0035 : item.Hue, item.ItemData.IsPartialHue, 0, true);

            batcher.Draw2D(Texture, x, y, Width, Height, ref _hueVector);

            if (item.Amount > 1 && item.ItemData.IsStackable && item.DisplayedGraphic == item.Graphic)
                batcher.Draw2D(Texture, x + 5, y + 5, Width, Height, ref _hueVector);

            return base.Draw(batcher, x, y);
        }

        public override bool Contains(int x, int y)
        {
            if (Engine.Profile.Current != null && Engine.Profile.Current.ScaleItemsInsideContainers)
            {
                float scale = Engine.UI.ContainerScale;

                x = (int)(x / scale);
                y = (int)(y / scale);
            }

            if (Texture.Contains(x, y))
                return true;

            Item item = World.Items.Get(LocalSerial);

            if (item == null)
            {
                Dispose();
            }

            if (IsDisposed)
                return false;

            if (!item.IsCoin && item.Amount > 1 && item.ItemData.IsStackable)
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
            base.OnMouseUp(x, y, button);

            if (button == MouseButton.Left)
            {
                GameScene gs = Engine.SceneManager.GetScene<GameScene>();
                if (gs == null)
                    return;

                Item item = World.Items.Get(LocalSerial);

                if (item == null)
                {
                    Dispose();
                }

                if (IsDisposed)
                    return;

                if (TargetManager.IsTargeting)
                {
                    if (Mouse.IsDragging && Mouse.LDroppedOffset != Point.Zero)
                    {
                        if (!gs.IsHoldingItem || !gs.IsMouseOverUI) return;

                        SelectedObject.Object = item;

                        if (item.ItemData.IsContainer)
                            gs.DropHeldItemToContainer(item);
                        else if (gs.HeldItem.Graphic == item.Graphic && gs.HeldItem.IsStackable)
                            gs.MergeHeldItem(item);
                        else
                        {
                            if (item.Container.IsItem)
                                gs.DropHeldItemToContainer(World.Items.Get(item.Container), X + (Mouse.Position.X - ScreenCoordinateX), Y + (Mouse.Position.Y - ScreenCoordinateY));
                        }

                        return;
                    }

                    switch (TargetManager.TargetingState)
                    {
                        case CursorTarget.Position:
                        case CursorTarget.Object:
                        case CursorTarget.Grab:
                        case CursorTarget.SetGrabBag:
                            SelectedObject.Object = item;


                            if (item != null)
                            {
                                TargetManager.TargetGameObject(item);
                                Mouse.LastLeftButtonClickTime = 0;
                            }

                            break;

                        case CursorTarget.SetTargetClientSide:
                            SelectedObject.Object = item;

                            if (item != null)
                            {
                                TargetManager.TargetGameObject(item);
                                Mouse.LastLeftButtonClickTime = 0;
                                Engine.UI.Add(new InfoGump(item));
                            }

                            break;

                        case CursorTarget.HueCommandTarget:
                            SelectedObject.Object = item;

                            if (item != null)
                            {
                                CommandManager.OnHueTarget(item);
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
                            float totalMS = Engine.Ticks;
                            _sClickTime = totalMS + Mouse.MOUSE_DELAY_DOUBLE_CLICK;
                            _lastClickPosition.X = Mouse.Position.X;
                            _lastClickPosition.Y = Mouse.Position.Y;
                        }
                    }
                    else
                    {
                        SelectedObject.Object = item;

                        if (item.ItemData.IsContainer)
                            gs.DropHeldItemToContainer(item);
                        else if (gs.HeldItem.Graphic == item.Graphic && gs.HeldItem.IsStackable)
                            gs.MergeHeldItem(item);
                        else
                        {
                            if (item.Container.IsItem)
                                gs.DropHeldItemToContainer(World.Items.Get(item.Container), X + (Mouse.Position.X - ScreenCoordinateX), Y + (Mouse.Position.Y - ScreenCoordinateY));
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
                Point offset = Mouse.LDroppedOffset;
                var split = Engine.UI.GetGump<SplitMenuGump>(LocalSerial);

                if (split != null || Math.Abs(offset.X) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS || Math.Abs(offset.Y) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
                {
                    split?.Dispose();
                    _clickedCanDrag = false;
                    AttempPickUp();
                }
            }
        }


        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            if (button != MouseButton.Left)
                return false;

            GameActions.DoubleClick(LocalSerial);
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
                    Item item = World.Items.Get(LocalSerial);

                    if (item == null)
                    {
                        Dispose();
                    }

                    if (IsDisposed)
                        return;

                    Rectangle bounds = FileManager.Art.GetTexture(item.DisplayedGraphic).Bounds;
                    GameActions.PickUp(LocalSerial, bounds.Width >> 1, bounds.Height >> 1);
                }
                else
                    GameActions.PickUp(LocalSerial, Point.Zero);
            }
        }
    }
}
