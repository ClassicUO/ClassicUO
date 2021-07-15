#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

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

                UOTexture texture = _is_gump ? GumpsLoader.Instance.GetTexture(value) : ArtLoader.Instance.GetTexture(value);

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


        public override void Update(double totalTime, double frameTime)
        {
            if (IsDisposed)
            {
                return;
            }

            base.Update(totalTime, frameTime);

            if (World.InGame)
            {
                if (CanPickUp && !ItemHold.Enabled && Mouse.LButtonPressed && UIManager.LastControlMouseDown(MouseButtonType.Left) == this && (Mouse.LastLeftButtonClickTime != 0xFFFF_FFFF && Mouse.LastLeftButtonClickTime != 0 && Mouse.LastLeftButtonClickTime + Mouse.MOUSE_DELAY_DOUBLE_CLICK < Time.Ticks || CanPickup()))
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
            
            bool partialHue = IsPartialHue;
            ushort hue = Hue;
            
            if (HighlightOnMouseOver && MouseIsOver)
            {
                hue = 0x0035;
                partialHue = false;
            }
            
            ShaderHueTranslator.GetHueVector(ref HueVector, hue, partialHue, 0);

            UOTexture texture = _is_gump ? GumpsLoader.Instance.GetTexture(Graphic) : ArtLoader.Instance.GetTexture(Graphic);

            if (texture != null)
            {
                batcher.Draw2D
                (
                    texture,
                    x,
                    y,
                    Width,
                    Height,
                    ref HueVector
                );

                Item item = World.Items.Get(LocalSerial);

                if (item != null && !item.IsMulti && !item.IsCoin && item.Amount > 1 && item.ItemData.IsStackable)
                {
                    batcher.Draw2D
                    (
                        texture,
                        x + 5,
                        y + 5,
                        Width,
                        Height,
                        ref HueVector
                    );
                }
            }

            return true;
        }

        public override bool Contains(int x, int y)
        {
            UOTexture texture = _is_gump ? GumpsLoader.Instance.GetTexture(Graphic) : ArtLoader.Instance.GetTexture(Graphic);

            if (texture == null)
            {
                return false;
            }

            x -= Offset.X;
            y -= Offset.Y;

            if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.ScaleItemsInsideContainers)
            {
                float scale = UIManager.ContainerScale;

                x = (int) (x / scale);
                y = (int) (y / scale);
            }

            if (_is_gump)
            {
                if (GumpsLoader.Instance.PixelCheck(Graphic, x, y))
                {
                    return true;
                }

                Item item = World.Items.Get(LocalSerial);

                if (item != null && !item.IsCoin && item.Amount > 1 && item.ItemData.IsStackable)
                {
                    if (GumpsLoader.Instance.PixelCheck(Graphic, x - 5, y - 5))
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (ArtLoader.Instance.PixelCheck(Graphic, x, y))
                {
                    return true;
                }

                Item item = World.Items.Get(LocalSerial);

                if (item != null && !item.IsCoin && item.Amount > 1 && item.ItemData.IsStackable)
                {
                    if (ArtLoader.Instance.PixelCheck(Graphic, x - 5, y - 5))
                    {
                        return true;
                    }
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

            if (Math.Abs(offset.X) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS && Math.Abs(offset.Y) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
            {
                return false;
            }

            SplitMenuGump split = UIManager.GetGump<SplitMenuGump>(LocalSerial);

            if (split == null)
            {
                return true;
            }

            split.X = Mouse.LClickPosition.X - 80;
            split.Y = Mouse.LClickPosition.Y - 40;
            UIManager.AttemptDragControl(split, true);
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

            if (!Keyboard.Ctrl && ProfileManager.CurrentProfile.DoubleClickToLootInsideContainers && item != null && !item.IsDestroyed && !item.ItemData.IsContainer && item.IsEmpty && (container = World.Items.Get(item.RootContainer)) != null && container != World.Player.FindItemByLayer(Layer.Backpack))
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
                UOTexture texture = _is_gump ? GumpsLoader.Instance.GetTexture(Graphic) : ArtLoader.Instance.GetTexture(Graphic);

                Rectangle bounds = texture.Bounds;
                int centerX = bounds.Width >> 1;
                int centerY = bounds.Height >> 1;

                if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.ScaleItemsInsideContainers)
                {
                    float scale = UIManager.ContainerScale;
                    centerX = (int) (centerX * scale);
                    centerY = (int) (centerY * scale);
                }

                if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.RelativeDragAndDropItems)
                {
                    Point p = new Point(centerX - (Mouse.Position.X - ScreenCoordinateX), centerY - (Mouse.Position.Y - ScreenCoordinateY));

                    GameActions.PickUp
                    (
                        LocalSerial,
                        centerX,
                        centerY,
                        offset: p,
                        is_gump: _is_gump
                    );
                }
                else
                {
                    GameActions.PickUp(LocalSerial, centerX, centerY, is_gump: _is_gump);
                }
            }
        }
    }
}
