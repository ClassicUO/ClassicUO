// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Services;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class ItemGump : Control
    {
        private ushort _graphic;
        private readonly bool _is_gump;
        private readonly Gump _gump;
        private readonly GuiService _uiService;
        private readonly UOService _uoService;
        private readonly AssetsService _assetsService;
        private readonly GameCursorService _cursorService;

        public ItemGump(Gump gump, uint serial, ushort graphic, ushort hue, int x, int y, bool is_gump = false)
        {
            _uiService = ServiceProvider.Get<GuiService>();
            _uoService = ServiceProvider.Get<UOService>();
            _cursorService = ServiceProvider.Get<GameCursorService>();
            _assetsService = ServiceProvider.Get<AssetsService>();

            _gump = gump;
            _is_gump = is_gump;

            AcceptMouseInput = true;
            X = (short)x;
            Y = (short)y;
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

                ref readonly var spriteInfo = ref _is_gump
                    ? ref _uoService.Gumps.GetGump(value)
                    : ref _uoService.Arts.GetArt(value);

                if (spriteInfo.Texture == null)
                {
                    Dispose();

                    return;
                }

                Width = spriteInfo.UV.Width;
                Height = spriteInfo.UV.Height;

                IsPartialHue = !_is_gump && _assetsService.TileData.StaticData[value].IsPartialHue;
            }
        }

        public ushort Hue { get; set; }
        public bool IsPartialHue { get; set; }
        public bool HighlightOnMouseOver { get; set; }
        public bool CanPickUp { get; set; }

        public override void Update()
        {
            if (IsDisposed)
            {
                return;
            }

            base.Update();

            if (_gump.World.InGame)
            {
                if (
                    CanPickUp
                    && !_cursorService.GameCursor.ItemHold.Enabled
                    && Mouse.LButtonPressed
                    && _uiService.LastControlMouseDown(MouseButtonType.Left) == this
                    && (
                        Mouse.LastLeftButtonClickTime != 0xFFFF_FFFF
                            && Mouse.LastLeftButtonClickTime != 0
                            && Mouse.LastLeftButtonClickTime + Mouse.MOUSE_DELAY_DOUBLE_CLICK
                                < Time.Ticks
                        || CanPickup()
                    )
                )
                {
                    AttemptPickUp();
                }
                else if (MouseIsOver)
                {
                    SelectedObject.Object = _gump.World.Get(LocalSerial);
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

            bool partialHue = IsPartialHue;
            ushort hue = Hue;

            if (HighlightOnMouseOver && MouseIsOver || LocalSerial == SelectedObject.SelectedContainer?.Serial)
            {
                hue = 0x0035;
                partialHue = false;
            }

            var hueVector = ShaderHueTranslator.GetHueVector(hue, partialHue, 1);

            ref readonly var spriteInfo = ref _is_gump
                ? ref _uoService.Gumps.GetGump(Graphic)
                : ref _uoService.Arts.GetArt(Graphic);

            if (spriteInfo.Texture != null)
            {
                var rect = new Rectangle(x, y, Width, Height);

                batcher.Draw(spriteInfo.Texture, rect, spriteInfo.UV, hueVector);

                var item = _gump.World.Items.Get(LocalSerial);

                if (
                    item != null
                    && !item.IsMulti
                    && !item.IsCoin
                    && item.Amount > 1
                    && item.ItemData.IsStackable
                )
                {
                    rect.X += 5;
                    rect.Y += 5;

                    batcher.Draw(spriteInfo.Texture, rect, spriteInfo.UV, hueVector);
                }
            }

            return true;
        }

        public override bool Contains(int x, int y)
        {
            ref readonly var spriteInfo = ref _is_gump
                ? ref _uoService.Gumps.GetGump(Graphic)
                : ref _uoService.Arts.GetArt(Graphic);

            if (spriteInfo.Texture == null)
            {
                return false;
            }

            x -= Offset.X;
            y -= Offset.Y;

            if (
                ProfileManager.CurrentProfile != null
                && ProfileManager.CurrentProfile.ScaleItemsInsideContainers
            )
            {
                var scale = _uiService.ContainerScale;

                x = (int)(x / scale);
                y = (int)(y / scale);
            }

            if (_is_gump)
            {
                if (_uoService.Gumps.PixelCheck(Graphic, x, y))
                {
                    return true;
                }

                var item = _gump.World.Items.Get(LocalSerial);

                if (item != null && !item.IsCoin && item.Amount > 1 && item.ItemData.IsStackable)
                {
                    if (_uoService.Gumps.PixelCheck(Graphic, x - 5, y - 5))
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (_uoService.Arts.PixelCheck(Graphic, x, y))
                {
                    return true;
                }

                var item = _gump.World.Items.Get(LocalSerial);

                if (item != null && !item.IsCoin && item.Amount > 1 && item.ItemData.IsStackable)
                {
                    if (_uoService.Arts.PixelCheck(Graphic, x - 5, y - 5))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            SelectedObject.Object = _gump.World.Get(LocalSerial);
            base.OnMouseUp(x, y, button);
        }

        protected override void OnMouseOver(int x, int y)
        {
            SelectedObject.Object = _gump.World.Get(LocalSerial);
        }

        private bool CanPickup()
        {
            var offset = Mouse.LDragOffset;

            if (
                Math.Abs(offset.X) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS
                && Math.Abs(offset.Y) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS
            )
            {
                return false;
            }

            var split = _uiService.GetGump<SplitMenuGump>(LocalSerial);

            if (split == null)
            {
                return true;
            }

            split.X = Mouse.LClickPosition.X - 80;
            split.Y = Mouse.LClickPosition.Y - 40;
            _uiService.AttemptDragControl(split, true);
            split.BringOnTop();

            return false;
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left || _gump.World.TargetManager.IsTargeting)
            {
                return false;
            }

            var item = _gump.World.Items.Get(LocalSerial);
            Item? container;

            if (
                !Keyboard.Ctrl
                && ProfileManager.CurrentProfile.DoubleClickToLootInsideContainers
                && item != null
                && !item.IsDestroyed
                && !item.ItemData.IsContainer
                && item.IsEmpty
                && (container = _gump.World.Items.Get(item.RootContainer)) != null
                && container != _gump.World.Player.FindItemByLayer(Layer.Backpack)
            )
            {
                GameActions.GrabItem(_gump.World, LocalSerial, item.Amount);
            }
            else
            {
                GameActions.DoubleClick(_gump.World, LocalSerial);
            }

            return true;
        }

        private void AttemptPickUp()
        {
            if (CanPickUp)
            {
                ref readonly var spriteInfo = ref _is_gump
                    ? ref _uoService.Gumps.GetGump(Graphic)
                    : ref _uoService.Arts.GetArt(Graphic);

                int centerX = spriteInfo.UV.Width >> 1;
                int centerY = spriteInfo.UV.Height >> 1;

                if (
                    ProfileManager.CurrentProfile != null
                    && ProfileManager.CurrentProfile.ScaleItemsInsideContainers
                )
                {
                    var scale = _uiService.ContainerScale;
                    centerX = (int)(centerX * scale);
                    centerY = (int)(centerY * scale);
                }

                if (
                    ProfileManager.CurrentProfile != null
                    && ProfileManager.CurrentProfile.RelativeDragAndDropItems
                )
                {
                    Point p = new Point(
                        centerX - (Mouse.Position.X - ScreenCoordinateX),
                        centerY - (Mouse.Position.Y - ScreenCoordinateY)
                    );

                    GameActions.PickUp(_gump.World, LocalSerial, centerX, centerY, offset: p, is_gump: _is_gump);
                }
                else
                {
                    GameActions.PickUp(_gump.World, LocalSerial, centerX, centerY, is_gump: _is_gump);
                }
            }
        }
    }
}