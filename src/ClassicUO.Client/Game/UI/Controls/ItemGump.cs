// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Sdk.Assets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.Services;

namespace ClassicUO.Game.UI.Controls
{
    internal class ItemGump : Control
    {
        private ushort _graphic;
        private readonly bool _is_gump;
        private readonly Gump _gump;

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
                var uoService = ServiceProvider.Get<UOService>();

                ref readonly var spriteInfo = ref _is_gump
                    ? ref uoService.Gumps.GetGump(value)
                    : ref uoService.Arts.GetArt(value);

                if (spriteInfo.Texture == null)
                {
                    Dispose();

                    return;
                }

                Width = spriteInfo.UV.Width;
                Height = spriteInfo.UV.Height;

                IsPartialHue = !_is_gump && uoService.FileManager.TileData.StaticData[value].IsPartialHue;
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
                var uoService = ServiceProvider.Get<UOService>();
                if (
                    CanPickUp
                    && !uoService.GameCursor.ItemHold.Enabled
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

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            var uoService = ServiceProvider.Get<UOService>();
            ref readonly var spriteInfo = ref _is_gump
                ? ref uoService.Gumps.GetGump(Graphic)
                : ref uoService.Arts.GetArt(Graphic);

            if (spriteInfo.Texture == null)
            {
                return false;
            }

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(Hue, IsPartialHue, 1.0f);

            batcher.Draw(
                spriteInfo.Texture,
                new Vector2(x, y),
                spriteInfo.UV,
                hueVector
            );

            return true;
        }

        public override bool Contains(int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            var uoService = ServiceProvider.Get<UOService>();
            if (_is_gump)
            {
                if (uoService.Gumps.PixelCheck(Graphic, x, y))
                {
                    return true;
                }
            }
            else
            {
                if (uoService.Gumps.PixelCheck(Graphic, x - 5, y - 5))
                {
                    return true;
                }

                if (uoService.Arts.PixelCheck(Graphic, x, y))
                {
                    return true;
                }

                if (uoService.Arts.PixelCheck(Graphic, x - 5, y - 5))
                {
                    return true;
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
            if (IsDisposed)
            {
                return false;
            }

            var uoService = ServiceProvider.Get<UOService>();
            ref readonly var spriteInfo = ref _is_gump
                ? ref uoService.Gumps.GetGump(Graphic)
                : ref uoService.Arts.GetArt(Graphic);

            if (spriteInfo.Texture == null)
            {
                return false;
            }

            return true;
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
            if (IsDisposed)
            {
                return;
            }

            var uoService = ServiceProvider.Get<UOService>();
            GameActions.PickUp(_gump.World, LocalSerial, 0, 0);
        }
    }
}
