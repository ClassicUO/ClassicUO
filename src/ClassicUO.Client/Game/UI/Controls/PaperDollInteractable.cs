// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Sdk;
using ClassicUO.Sdk.Assets;
using Microsoft.Xna.Framework;
using ClassicUO.Game.Services;

namespace ClassicUO.Game.UI.Controls
{
    internal class PaperDollInteractable : Control
    {
        private static readonly Layer[] _layerOrder =
        {
            Layer.Cloak,
            Layer.Shirt,
            Layer.Pants,
            Layer.Shoes,
            Layer.Legs,
            Layer.Arms,
            Layer.Torso,
            Layer.Tunic,
            Layer.Ring,
            Layer.Bracelet,
            Layer.Face,
            Layer.Gloves,
            Layer.Skirt,
            Layer.Robe,
            Layer.Waist,
            Layer.Necklace,
            Layer.Hair,
            Layer.Beard,
            Layer.Earrings,
            Layer.Helmet,
            Layer.OneHanded,
            Layer.TwoHanded,
            Layer.Talisman
        };

        private static readonly Layer[] _layerOrder_quiver_fix =
        {
            Layer.Shirt,
            Layer.Pants,
            Layer.Shoes,
            Layer.Legs,
            Layer.Arms,
            Layer.Torso,
            Layer.Tunic,
            Layer.Ring,
            Layer.Bracelet,
            Layer.Face,
            Layer.Gloves,
            Layer.Skirt,
            Layer.Robe,
            Layer.Cloak,
            Layer.Waist,
            Layer.Necklace,
            Layer.Hair,
            Layer.Beard,
            Layer.Earrings,
            Layer.Helmet,
            Layer.OneHanded,
            Layer.TwoHanded,
            Layer.Talisman
        };

        private readonly PaperDollGump _paperDollGump;

        private bool _updateUI;

        public PaperDollInteractable(int x, int y, uint serial, PaperDollGump paperDollGump)
        {
            X = x;
            Y = y;
            _paperDollGump = paperDollGump;
            AcceptMouseInput = false;
            LocalSerial = serial;
            _updateUI = true;
        }

        public bool HasFakeItem { get; private set; }

        public override void Update()
        {
            base.Update();

            if (_updateUI)
            {
                UpdateUI();

                _updateUI = false;
            }
        }

        public void SetFakeItem(bool value)
        {
            _updateUI = HasFakeItem && !value || !HasFakeItem && value;
            HasFakeItem = value;
        }

        private void UpdateUI()
        {
            if (IsDisposed)
            {
                return;
            }

            var mobile = _paperDollGump.World.Mobiles.Get(LocalSerial);

            if (mobile == null || mobile.IsDestroyed)
            {
                Dispose();

                return;
            }

            Clear();

            // Add the base gump - the semi-naked paper doll.
            ushort body;
            ushort hue = mobile.Hue;

            if (mobile.Graphic == 0x0191 || mobile.Graphic == 0x0193)
            {
                body = 0x000D;
            }
            else if (mobile.Graphic == 0x025D)
            {
                body = 0x000E;
            }
            else if (mobile.Graphic == 0x025E)
            {
                body = 0x000F;
            }
            else if (mobile.Graphic == 0x029A || mobile.Graphic == 0x02B6)
            {
                body = 0x029A;
            }
            else if (mobile.Graphic == 0x029B || mobile.Graphic == 0x02B7)
            {
                body = 0x0299;
            }
            else if (mobile.Graphic == 0x04E5)
            {
                body = 0xC835;
            }
            else if (mobile.Graphic == 0x03DB)
            {
                body = 0x000C;
                hue = 0x03EA;
            }
            else if (mobile.IsFemale)
            {
                body = 0x000D;
            }
            else
            {
                body = 0x000C;
            }

            // body
            Add(new GumpPic(0, 0, body, hue) { IsPartialHue = true });

            if (mobile.Graphic == 0x03DB)
            {
                Add(
                    new GumpPic(0, 0, 0xC72B, mobile.Hue)
                    {
                        AcceptMouseInput = true,
                        IsPartialHue = true
                    }
                );
            }

            // equipment
            var equipItem = mobile.FindItemByLayer(Layer.Cloak);
            var arms = mobile.FindItemByLayer(Layer.Arms);

            bool switch_arms_with_torso = false;
            var uoService = ServiceProvider.Get<UOService>();

            if (
                HasFakeItem
                && uoService.GameCursor.ItemHold.Enabled
                && !uoService.GameCursor.ItemHold.IsFixedPosition
                && (byte)Layer.Arms == uoService.GameCursor.ItemHold.ItemData.Layer
            )
            {
                switch_arms_with_torso =
                    uoService.GameCursor.ItemHold.Graphic == 0x1410
                    || uoService.GameCursor.ItemHold.Graphic == 0x1417;
            }

            Layer[] layers;

            if (equipItem != null)
            {
                layers = equipItem.ItemData.IsContainer ? _layerOrder_quiver_fix : _layerOrder;
            }
            else if (
                HasFakeItem
                && uoService.GameCursor.ItemHold.Enabled
                && !uoService.GameCursor.ItemHold.IsFixedPosition
                && (byte)Layer.Cloak == uoService.GameCursor.ItemHold.ItemData.Layer
            )
            {
                layers = uoService.FileManager.TileData.StaticData[uoService.GameCursor.ItemHold.Graphic].IsContainer
                    ? _layerOrder_quiver_fix
                    : _layerOrder;
            }
            else
            {
                layers = _layerOrder;
            }

            for (int i = 0; i < layers.Length; i++)
            {
                Layer layer = layers[i];

                if (switch_arms_with_torso)
                {
                    if (layer == Layer.Arms)
                    {
                        layer = Layer.Torso;
                    }
                    else if (layer == Layer.Torso)
                    {
                        layer = Layer.Arms;
                    }
                }

                equipItem = mobile.FindItemByLayer(layer);

                if (equipItem != null)
                {
                    if (Mobile.IsCovered(mobile, layer))
                    {
                        continue;
                    }

                    ushort id = GetAnimID(
                        mobile.Graphic,
                        equipItem.Graphic,
                        equipItem.ItemData.AnimID,
                        mobile.IsFemale
                    );

                    Add(
                        new GumpPicEquipment(
                            _paperDollGump,
                            equipItem.Serial,
                            0,
                            0,
                            id,
                            (ushort)(equipItem.Hue & 0x3FFF),
                            layer
                        )
                        {
                            AcceptMouseInput = true,
                            IsPartialHue = equipItem.ItemData.IsPartialHue,
                            CanLift =
                                _paperDollGump.World.InGame
                                && !_paperDollGump.World.Player.IsDead
                                && layer != Layer.Beard
                                && layer != Layer.Hair
                                && (_paperDollGump.CanLift || LocalSerial == _paperDollGump.World.Player)
                        }
                    );
                }
                else if (
                    HasFakeItem
                    && uoService.GameCursor.ItemHold.Enabled
                    && !uoService.GameCursor.ItemHold.IsFixedPosition
                    && (byte)layer == uoService.GameCursor.ItemHold.ItemData.Layer
                    && uoService.GameCursor.ItemHold.ItemData.AnimID != 0
                )
                {
                    ushort id = GetAnimID(
                        mobile.Graphic,
                        uoService.GameCursor.ItemHold.Graphic,
                        uoService.GameCursor.ItemHold.ItemData.AnimID,
                        mobile.IsFemale
                    );

                    Add(
                        new GumpPicEquipment(
                            _paperDollGump,
                            0,
                            0,
                            0,
                            id,
                            (ushort)(uoService.GameCursor.ItemHold.Hue & 0x3FFF),
                            uoService.GameCursor.ItemHold.Layer
                        )
                        {
                            AcceptMouseInput = true,
                            IsPartialHue = uoService.GameCursor.ItemHold.IsPartialHue,
                            Alpha = 0.5f
                        }
                    );
                }
            }

            equipItem = mobile.FindItemByLayer(Layer.Backpack);

            if (equipItem != null && equipItem.ItemData.AnimID != 0)
            {
                ushort backpackGraphic = (ushort)(
                    equipItem.ItemData.AnimID + Constants.MALE_GUMP_OFFSET
                );

                // If player, apply backpack skin
                if (mobile.Serial == _paperDollGump.World.Player.Serial)
                {
                    var gump = uoService.Gumps;

                    switch (ProfileManager.CurrentProfile.BackpackStyle)
                    {
                        case 1:
                            if (gump.GetGump(0x777B).Texture != null)
                            {
                                backpackGraphic = 0x777B; // Suede Backpack
                            }

                            break;
                        case 2:
                            if (gump.GetGump(0x777C).Texture != null)
                            {
                                backpackGraphic = 0x777C; // Polar Bear Backpack
                            }

                            break;
                        case 3:
                            if (gump.GetGump(0x777D).Texture != null)
                            {
                                backpackGraphic = 0x777D; // Ghoul Skin Backpack
                            }

                            break;
                        default:
                            if (gump.GetGump(0xC4F6).Texture != null)
                            {
                                backpackGraphic = 0xC4F6; // Default Backpack
                            }

                            break;
                    }
                }

                int bx = 0;

                if (_paperDollGump.World.ClientFeatures.PaperdollBooks)
                {
                    bx = 6;
                }

                Add(
                    new GumpPicEquipment(
                        _paperDollGump,
                        equipItem.Serial,
                        -bx,
                        0,
                        backpackGraphic,
                        (ushort)(equipItem.Hue & 0x3FFF),
                        Layer.Backpack
                    )
                    {
                        AcceptMouseInput = true
                    }
                );
            }
        }

        public void RequestUpdate()
        {
            _updateUI = true;
        }

        protected static ushort GetAnimID(ushort mobileGraphic, ushort itemGraphic, ushort animID, bool isfemale)
        {
            int offset = isfemale ? Constants.FEMALE_GUMP_OFFSET : Constants.MALE_GUMP_OFFSET;

            if (ServiceProvider.Get<UOService>().Version >= ClientVersion.CV_7000 && animID == 0x03CA && (mobileGraphic == 0x02B7 || mobileGraphic == 0x02B6))
            {
                animID = 0x0223;
            }

            ServiceProvider.Get<UOService>().Animations.ConvertBodyIfNeeded(ref mobileGraphic);

            if (ServiceProvider.Get<UOService>().FileManager.Animations.EquipConversions.TryGetValue(mobileGraphic, out var dict))
            {
                if (dict.TryGetValue(animID, out EquipConvData data))
                {
                    if (data.Gump > Constants.MALE_GUMP_OFFSET)
                    {
                        animID = (ushort)(
                            data.Gump >= Constants.FEMALE_GUMP_OFFSET
                                ? data.Gump - Constants.FEMALE_GUMP_OFFSET
                                : data.Gump - Constants.MALE_GUMP_OFFSET
                        );
                    }
                    else
                    {
                        animID = data.Gump;
                    }
                }
            }

            if (ServiceProvider.Get<UOService>().FileManager.TileArt.TryGetTileArtInfo(itemGraphic, out var tileArtInfo) && tileArtInfo != null)
            {
                if (tileArtInfo.TryGetAppearance(mobileGraphic, out var appareanceId))
                {
                    var gumpId = (ushort)(Constants.MALE_GUMP_OFFSET + appareanceId);
                    if (ServiceProvider.Get<UOService>().Gumps.GetGump(gumpId).Texture != null)
                    {
                        Log.Info($"Equip conversion through tileart.uop done: old {animID} -> new {appareanceId}");
                        return gumpId;
                    }
                }
            }

            _ = IsAnimExistsInGump(animID, ref offset, isfemale);

            return (ushort)(animID + offset);
        }

        private static bool IsAnimExistsInGump(ushort animID, ref int offset, bool isFemale)
        {
            if (
                    animID + offset > GumpsLoader.MAX_GUMP_DATA_INDEX_COUNT
                    || ServiceProvider.Get<UOService>().Gumps.GetGump((ushort)(animID + offset)).Texture == null
                )
            {
                // inverse
                offset = isFemale ? Constants.MALE_GUMP_OFFSET : Constants.FEMALE_GUMP_OFFSET;
            }

            if (ServiceProvider.Get<UOService>().Gumps.GetGump((ushort)(animID + offset)).Texture == null)
            {
                Log.Error(
                    $"Texture not found in paperdoll: gump_graphic: {(ushort)(animID + offset)}"
                );

                return false;
            }

            return true;
        }

        protected class GumpPicEquipment : GumpPic
        {
            private readonly Layer _layer;
            private readonly Gump _gump;

            public GumpPicEquipment(
                Gump gump,
                uint serial,
                int x,
                int y,
                ushort graphic,
                ushort hue,
                Layer layer
            ) : base(x, y, graphic, hue)
            {
                _gump = gump;
                LocalSerial = serial;
                CanMove = false;
                _layer = layer;

                if (SerialHelper.IsValid(serial) && _gump.World.InGame)
                {
                    SetTooltip(serial);
                }
            }

            public bool CanLift { get; set; }

            protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
            {
                if (button != MouseButtonType.Left)
                {
                    return false;
                }

                // this check is necessary to avoid crashes during character creation
                if (_gump.World.InGame)
                {
                    GameActions.DoubleClick(_gump.World, LocalSerial);
                }

                return true;
            }

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                SelectedObject.Object = _gump.World.Get(LocalSerial);
                base.OnMouseUp(x, y, button);
            }

            public override void Update()
            {
                base.Update();

                if (_gump.World.InGame)
                {
                    if (
                        CanLift
                        && !ServiceProvider.Get<UOService>().GameCursor.ItemHold.Enabled
                        && Mouse.LButtonPressed
                        && ServiceProvider.Get<UIService>().LastControlMouseDown(MouseButtonType.Left) == this
                        && (
                            Mouse.LastLeftButtonClickTime != 0xFFFF_FFFF
                                && Mouse.LastLeftButtonClickTime != 0
                                && Mouse.LastLeftButtonClickTime + Mouse.MOUSE_DELAY_DOUBLE_CLICK
                                    < Time.Ticks
                            || Mouse.LDragOffset != Point.Zero
                        )
                    )
                    {
                        GameActions.PickUp(_gump.World, LocalSerial, 0, 0);

                        if (_layer == Layer.OneHanded || _layer == Layer.TwoHanded)
                        {
                            _gump.World.Player.UpdateAbilities();
                        }
                    }
                    else if (MouseIsOver)
                    {
                        SelectedObject.Object = _gump.World.Get(LocalSerial);
                    }
                }
            }

            protected override void OnMouseOver(int x, int y)
            {
                SelectedObject.Object = _gump.World.Get(LocalSerial);
            }
        }
    }
}
