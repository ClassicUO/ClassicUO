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
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class ItemGump : Control
    {
        private ushort _graphic;
        private readonly bool _is_gump;

        public ItemGump
        (
            uint serial,
            ushort graphic,
            ushort hue,
            int x,
            int y,
            bool is_gump = false
        )
        {
            _is_gump = is_gump;

            AcceptMouseInput = true;
            X = (short) x;
            Y = (short) y;
            HighlightOnMouseOver = true;
            CanPickUp = true;
            LocalSerial = serial;
            WantUpdateSize = false;
            CanMove = false;


            Graphic = graphic;
            Hue = hue;

            SetTooltip(serial);
        }


        public ushort Graphic
        {
            get => _graphic;
            set
            {
                _graphic = value;

                UOTexture32 texture =
                    _is_gump ? GumpsLoader.Instance.GetTexture(value) : ArtLoader.Instance.GetTexture(value);

                if (texture == null)
                {
                    Dispose();

                    return;
                }

                Width = texture.Width;
                Height = texture.Height;

                IsPartialHue = !_is_gump && TileDataLoader.Instance.StaticData[value].IsPartialHue;
            }
        }

        public ushort Hue { get; set; }
        public bool IsPartialHue { get; set; }
        public bool HighlightOnMouseOver { get; set; }
        public bool CanPickUp { get; set; }


        public override void Update(double totalMS, double frameMS)
        {
            if (IsDisposed)
            {
                return;
            }

            base.Update(totalMS, frameMS);

            if (World.InGame)
            {
                if (CanPickUp && !ItemHold.Enabled && Mouse.LButtonPressed &&
                    UIManager.LastControlMouseDown(MouseButtonType.Left) == this &&
                    (Mouse.LastLeftButtonClickTime != 0xFFFF_FFFF && Mouse.LastLeftButtonClickTime != 0 &&
                        Mouse.LastLeftButtonClickTime + Mouse.MOUSE_DELAY_DOUBLE_CLICK < Time.Ticks || CanPickup()))
                {
                    AttemptPickUp();
                }
                else if (MouseIsOver)
                {
                    SelectedObject.Object = World.Get(LocalSerial);
                }
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            base.Draw(batcher, x, y);

            ResetHueVector();

            ShaderHueTranslator.GetHueVector
                (ref _hueVector, HighlightOnMouseOver && MouseIsOver ? 0x0035 : Hue, IsPartialHue, 0);

            UOTexture32 texture =
                _is_gump ? GumpsLoader.Instance.GetTexture(Graphic) : ArtLoader.Instance.GetTexture(Graphic);

            if (texture != null)
            {
                batcher.Draw2D(texture, x, y, Width, Height, ref _hueVector);

                Item item = World.Items.Get(LocalSerial);

                if (item != null && !item.IsMulti && !item.IsCoin && item.Amount > 1 && item.ItemData.IsStackable)
                {
                    batcher.Draw2D(texture, x + 5, y + 5, Width, Height, ref _hueVector);
                }
            }

            return true;
        }

        public override bool Contains(int x, int y)
        {
            UOTexture32 texture =
                _is_gump ? GumpsLoader.Instance.GetTexture(Graphic) : ArtLoader.Instance.GetTexture(Graphic);

            if (texture == null)
            {
                return false;
            }

            x -= Offset.X;
            y -= Offset.Y;

            if (ProfileManager.Current != null && ProfileManager.Current.ScaleItemsInsideContainers)
            {
                float scale = UIManager.ContainerScale;

                x = (int) (x / scale);
                y = (int) (y / scale);
            }

            if (texture.Contains(x, y))
            {
                return true;
            }

            Item item = World.Items.Get(LocalSerial);

            if (item != null && !item.IsCoin && item.Amount > 1 && item.ItemData.IsStackable)
            {
                if (texture.Contains(x - 5, y - 5))
                {
                    return true;
                }
            }

            return false;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            SelectedObject.Object = World.Get(LocalSerial);
            base.OnMouseUp(x, y, button);
        }

        protected override void OnMouseOver(int x, int y)
        {
            SelectedObject.Object = World.Get(LocalSerial);
        }

        private bool CanPickup()
        {
            Point offset = Mouse.LDragOffset;

            if (Math.Abs(offset.X) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS &&
                Math.Abs(offset.Y) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
            {
                return false;
            }

            SplitMenuGump split = UIManager.GetGump<SplitMenuGump>(LocalSerial);

            if (split == null)
            {
                return true;
            }

            split.X = Mouse.Position.X - 80;
            split.Y = Mouse.Position.Y - 40;
            UIManager.AttemptDragControl(split, Mouse.Position, true);
            split.BringOnTop();

            return false;
        }


        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left || TargetManager.IsTargeting)
            {
                return false;
            }

            Item item = World.Items.Get(LocalSerial);
            Item container;

            if (!Keyboard.Ctrl && ProfileManager.Current.DoubleClickToLootInsideContainers && item != null &&
                !item.IsDestroyed && !item.ItemData.IsContainer && item.IsEmpty &&
                (container = World.Items.Get(item.RootContainer)) != null && container != World.Player.FindItemByLayer
                    (Layer.Backpack))
            {
                GameActions.GrabItem(LocalSerial, item.Amount);
            }
            else
            {
                GameActions.DoubleClick(LocalSerial);
            }

            return true;
        }


        private void AttemptPickUp()
        {
            if (CanPickUp)
            {
                UOTexture32 texture = _is_gump ?
                    GumpsLoader.Instance.GetTexture(Graphic) :
                    ArtLoader.Instance.GetTexture(Graphic);

                Rectangle bounds = texture.Bounds;
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
                    Point p = new Point
                    (
                        centerX - (Mouse.Position.X - ScreenCoordinateX),
                        centerY - (Mouse.Position.Y - ScreenCoordinateY)
                    );

                    GameActions.PickUp(LocalSerial, centerX, centerY, offset: p, is_gump: _is_gump);
                }
                else
                {
                    GameActions.PickUp(LocalSerial, centerX, centerY, is_gump: _is_gump);
                }
            }
        }
    }
}