// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;

namespace ClassicUO.Game.UI.Controls
{
    internal class ItemGump : Control
    {
        private ushort _graphic;
        private readonly bool _is_gump;
        private readonly Gump _gump;
        private int _cachedMinX, _cachedMinY, _cachedMaxX, _cachedMaxY;
        private bool _cachedHasVisiblePixels;
        private bool _cachedBoundsValid;

        public ItemGump(Gump gump, uint serial, ushort graphic, ushort hue, int x, int y, bool is_gump = false)
        {
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
                    ? ref Client.Game.UO.Gumps.GetGump(value)
                    : ref Client.Game.UO.Arts.GetArt(value);

                if (spriteInfo.Texture == null)
                {
                    Dispose();

                    return;
                }

                Width = spriteInfo.UV.Width;
                Height = spriteInfo.UV.Height;

                IsPartialHue = !_is_gump && Client.Game.UO.FileManager.TileData.StaticData[value].IsPartialHue;

                // Calculate visible pixel bounds immediately when feature is enabled
                // This distributes the cost during container loading rather than causing
                // frame hitches on first hover
                if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.UseItemBoxesInContainers)
                {
                    (_cachedMinX, _cachedMinY, _cachedMaxX, _cachedMaxY, _cachedHasVisiblePixels) =
                        GetVisiblePixelBounds(spriteInfo.UV.Width, spriteInfo.UV.Height);
                    _cachedBoundsValid = true;
                }
                else
                {
                    _cachedBoundsValid = false;
                }
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
                    && !Client.Game.UO.GameCursor.ItemHold.Enabled
                    && Mouse.LButtonPressed
                    && UIManager.LastControlMouseDown(MouseButtonType.Left) == this
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

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            if (IsDisposed)
            {
                return false;
            }

            base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
            float layerDepth = layerDepthRef;

            bool partialHue = IsPartialHue;
            ushort hue = Hue;

            if (HighlightOnMouseOver && MouseIsOver || LocalSerial == SelectedObject.SelectedContainer?.Serial)
            {
                hue = 0x0035;
                partialHue = false;
            }

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(hue, partialHue, 1);

            ref readonly var spriteInfo = ref _is_gump
                ? ref Client.Game.UO.Gumps.GetGump(Graphic)
                : ref Client.Game.UO.Arts.GetArt(Graphic);

            var texture = spriteInfo.Texture;
            if (spriteInfo.Texture != null)
            {
                Rectangle rect = new Rectangle(x, y, Width, Height);

                var sourceRectangle = spriteInfo.UV;

                renderLists.AddGumpWithAtlas
                (
                    batcher =>
                    {
                        batcher.Draw(texture, rect, sourceRectangle, hueVector, layerDepth);

                        Item item = _gump.World.Items.Get(LocalSerial);

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

                            batcher.Draw(texture, rect, sourceRectangle, hueVector, layerDepth);
                        }

                        return true;
                    }
                );
            }

            return true;
        }

        public override bool Contains(int x, int y)
        {
            ref readonly var spriteInfo = ref _is_gump
                ? ref Client.Game.UO.Gumps.GetGump(Graphic)
                : ref Client.Game.UO.Arts.GetArt(Graphic);

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
                float scale = UIManager.ContainerScale;
                x = (int)(x / scale);
                y = (int)(y / scale);
            }

            Item item = _gump.World.Items.Get(LocalSerial);

            // If UseItemBoxesInContainers is disabled, use pixel-perfect hit detection
            if (ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.UseItemBoxesInContainers)
            {
                if (x >= 0 && x < spriteInfo.UV.Width && y >= 0 && y < spriteInfo.UV.Height &&
                    (_is_gump ? Client.Game.UO.Gumps.PixelCheck(Graphic, x, y) : Client.Game.UO.Arts.PixelCheck(Graphic, x, y)))
                {
                    return true;
                }

                // Check stackable item offset (+5, +5)
                if (item != null && !item.IsMulti && !item.IsCoin && item.Amount > 1 && item.ItemData.IsStackable)
                {
                    int offsetX = x - 5;
                    int offsetY = y - 5;

                    if (offsetX >= 0 && offsetX < spriteInfo.UV.Width && offsetY >= 0 && offsetY < spriteInfo.UV.Height &&
                        (_is_gump ? Client.Game.UO.Gumps.PixelCheck(Graphic, offsetX, offsetY) : Client.Game.UO.Arts.PixelCheck(Graphic, offsetX, offsetY)))
                    {
                        return true;
                    }
                }

                return false;
            }

            // Ensure bounds are calculated if not already done
            // This handles edge cases where profile settings might have changed
            if (!_cachedBoundsValid)
            {
                (_cachedMinX, _cachedMinY, _cachedMaxX, _cachedMaxY, _cachedHasVisiblePixels) =
                    GetVisiblePixelBounds(spriteInfo.UV.Width, spriteInfo.UV.Height);
                _cachedBoundsValid = true;
            }

            int spriteWidth = spriteInfo.UV.Width;
            int spriteHeight = spriteInfo.UV.Height;
            int minX = _cachedMinX;
            int minY = _cachedMinY;
            int maxX = _cachedMaxX;
            int maxY = _cachedMaxY;
            bool hasVisiblePixels = _cachedHasVisiblePixels;

            // If no visible pixels found, fallback to full sprite
            if (!hasVisiblePixels)
            {
                return x >= 0 && x < spriteWidth && y >= 0 && y < spriteHeight;
            }

            // Calculate the visible content size
            int visibleWidth = maxX - minX + 1;
            int visibleHeight = maxY - minY + 1;

            // Add padding to the visible content
            int boxPadding = ProfileManager.CurrentProfile.ItemBoxPadding;
            int boxWidth = visibleWidth + (boxPadding * 2);
            int boxHeight = visibleHeight + (boxPadding * 2);

            // Calculate offsets to center the box on the visible content
            int contentCenterX = (minX + maxX) / 2;
            int contentCenterY = (minY + maxY) / 2;
            
            int boxOffsetX = contentCenterX - boxWidth / 2;
            int boxOffsetY = contentCenterY - boxHeight / 2;

            // Check if point is within the base box
            bool inBaseBox = x >= boxOffsetX && x < boxOffsetX + boxWidth &&
                             y >= boxOffsetY && y < boxOffsetY + boxHeight;

            // For stackable items with amount > 1, also check the +5,+5 offset position
            // This matches the rendering behavior in AddToRenderLists
            if (item != null && !item.IsMulti && !item.IsCoin && item.Amount > 1 && item.ItemData.IsStackable)
            {
                const int STACK_OFFSET = 5;
                bool inStackOffsetBox = x >= boxOffsetX + STACK_OFFSET && x < boxOffsetX + STACK_OFFSET + boxWidth &&
                                        y >= boxOffsetY + STACK_OFFSET && y < boxOffsetY + STACK_OFFSET + boxHeight;
                return inBaseBox || inStackOffsetBox;
            }

            return inBaseBox;
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

        private (int minX, int minY, int maxX, int maxY, bool hasVisiblePixels) GetVisiblePixelBounds(int spriteWidth, int spriteHeight)
        {
            int minX = spriteWidth;
            int minY = spriteHeight;
            int maxX = -1;
            int maxY = -1;

            // Scan from top to find minY
            bool foundTop = false;
            for (int py = 0; py < spriteHeight && !foundTop; py++)
            {
                for (int px = 0; px < spriteWidth; px++)
                {
                    if ((_is_gump && Client.Game.UO.Gumps.PixelCheck(Graphic, px, py)) ||
                        (!_is_gump && Client.Game.UO.Arts.PixelCheck(Graphic, px, py)))
                    {
                        minY = py;
                        foundTop = true;
                        break;
                    }
                }
            }

            // If no visible pixels found at all, return early
            if (!foundTop)
            {
                return (0, 0, 0, 0, false);
            }

            // Scan from bottom to find maxY
            bool foundBottom = false;
            for (int py = spriteHeight - 1; py >= minY && !foundBottom; py--)
            {
                for (int px = 0; px < spriteWidth; px++)
                {
                    if ((_is_gump && Client.Game.UO.Gumps.PixelCheck(Graphic, px, py)) ||
                        (!_is_gump && Client.Game.UO.Arts.PixelCheck(Graphic, px, py)))
                    {
                        maxY = py;
                        foundBottom = true;
                        break;
                    }
                }
            }

            // Ensure maxY is valid; if bottom scan found nothing, fall back to minY
            if (maxY < minY)
            {
                maxY = minY;
            }

            // Scan from left to find minX
            bool foundLeft = false;
            for (int px = 0; px < spriteWidth && !foundLeft; px++)
            {
                for (int py = minY; py <= maxY; py++)
                {
                    if ((_is_gump && Client.Game.UO.Gumps.PixelCheck(Graphic, px, py)) ||
                        (!_is_gump && Client.Game.UO.Arts.PixelCheck(Graphic, px, py)))
                    {
                        minX = px;
                        foundLeft = true;
                        break;
                    }
                }
            }

            // Fallback if left scan failed
            if (minX == spriteWidth)
            {
                minX = 0;
            }

            // Scan from right to find maxX
            bool foundRight = false;
            for (int px = spriteWidth - 1; px >= minX && !foundRight; px--)
            {
                for (int py = minY; py <= maxY; py++)
                {
                    if ((_is_gump && Client.Game.UO.Gumps.PixelCheck(Graphic, px, py)) ||
                        (!_is_gump && Client.Game.UO.Arts.PixelCheck(Graphic, px, py)))
                    {
                        maxX = px;
                        foundRight = true;
                        break;
                    }
                }
            }

            // Fallback if right scan failed
            if (maxX == -1)
            {
                maxX = spriteWidth - 1;
            }

            return (minX, minY, maxX, maxY, true);
        }

        private bool CanPickup()
        {
            Point offset = Mouse.LDragOffset;

            if (
                Math.Abs(offset.X) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS
                && Math.Abs(offset.Y) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS
            )
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
            if (button != MouseButtonType.Left || _gump.World.TargetManager.IsTargeting)
            {
                return false;
            }

            Item item = _gump.World.Items.Get(LocalSerial);
            Item container;

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
                    ? ref Client.Game.UO.Gumps.GetGump(Graphic)
                    : ref Client.Game.UO.Arts.GetArt(Graphic);

                int centerX = spriteInfo.UV.Width >> 1;
                int centerY = spriteInfo.UV.Height >> 1;

                if (
                    ProfileManager.CurrentProfile != null
                    && ProfileManager.CurrentProfile.ScaleItemsInsideContainers
                )
                {
                    float scale = UIManager.ContainerScale;
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
