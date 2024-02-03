#region license

// Copyright (c) 2024, andreakarasho
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
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL2;

namespace ClassicUO.Game
{
    internal sealed class GameCursor
    {
        private static readonly ushort[,] _cursorData = new ushort[3, 16]
        {
            {
                0x206A,
                0x206B,
                0x206C,
                0x206D,
                0x206E,
                0x206F,
                0x2070,
                0x2071,
                0x2072,
                0x2073,
                0x2074,
                0x2075,
                0x2076,
                0x2077,
                0x2078,
                0x2079
            },
            {
                0x2053,
                0x2054,
                0x2055,
                0x2056,
                0x2057,
                0x2058,
                0x2059,
                0x205A,
                0x205B,
                0x205C,
                0x205D,
                0x205E,
                0x205F,
                0x2060,
                0x2061,
                0x2062
            },
            {
                0x206A,
                0x206B,
                0x206C,
                0x206D,
                0x206E,
                0x206F,
                0x2070,
                0x2071,
                0x2072,
                0x2073,
                0x2074,
                0x2075,
                0x2076,
                0x2077,
                0x2078,
                0x2079
            }
        };

        private readonly Aura _aura;
        private readonly CustomBuildObject[] _componentsList = new CustomBuildObject[10];
        private readonly int[,] _cursorOffset = new int[2, 16];
        private readonly IntPtr[,] _cursors_ptr = new IntPtr[3, 16];
        private ushort _graphic = 0x2073;
        private bool _needGraphicUpdate = true;
        private readonly List<Multi> _temp = new List<Multi>();
        private readonly Tooltip _tooltip;
        private readonly World _world;

        public GameCursor(World world)
        {
            _world = world;
            _tooltip = new Tooltip(world);
            _aura = new Aura(30);

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    ushort id = _cursorData[i, j];

                    var surface = Client.Game.UO.Arts.CreateCursorSurfacePtr(
                        id,
                        (ushort)(i == 2 ? 0x0033 : 0),
                        out int hotX,
                        out int hotY
                    );

                    if (surface != IntPtr.Zero)
                    {
                        if (hotX != 0 || hotY != 0)
                        {
                            _cursorOffset[0, j] = hotX;
                            _cursorOffset[1, j] = hotY;
                        }

                        _cursors_ptr[i, j] = SDL.SDL_CreateColorCursor(surface, hotX, hotY);
                    }
                }
            }
        }

        public ushort Graphic
        {
            get => _graphic;
            set
            {
                if (_graphic != value)
                {
                    _graphic = value;
                    _needGraphicUpdate = true;
                }
            }
        }

        public bool IsLoading { get; set; }
        public bool IsDraggingCursorForced { get; set; }
        public bool AllowDrawSDLCursor { get; set; } = true;

        public ItemHold ItemHold { get; } = new ItemHold();

        private ushort GetDraggingItemGraphic()
        {
            if (ItemHold.Enabled)
            {
                if (ItemHold.IsGumpTexture)
                {
                    return (ushort)(ItemHold.DisplayedGraphic - Constants.ITEM_GUMP_TEXTURE_OFFSET);
                }

                return ItemHold.DisplayedGraphic;
            }

            return 0xFFFF;
        }

        private Point GetDraggingItemOffset()
        {
            ushort graphic = GetDraggingItemGraphic();

            if (graphic != 0xFFFF)
            {
                ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(graphic);

                float scale = 1;

                if (
                    ProfileManager.CurrentProfile != null
                    && ProfileManager.CurrentProfile.ScaleItemsInsideContainers
                )
                {
                    scale = UIManager.ContainerScale;
                }

                return new Point(
                    (int)((artInfo.UV.Width >> 1) * scale) - ItemHold.MouseOffset.X,
                    (int)((artInfo.UV.Height >> 1) * scale) - ItemHold.MouseOffset.Y
                );
            }

            return Point.Zero;
        }

        public void Update()
        {
            Graphic = AssignGraphicByState();

            if (_needGraphicUpdate)
            {
                _needGraphicUpdate = false;

                if (AllowDrawSDLCursor && Settings.GlobalSettings.RunMouseInASeparateThread)
                {
                    ushort id = Graphic;

                    if (id < 0x206A)
                    {
                        id -= 0x2053;
                    }
                    else
                    {
                        id -= 0x206A;
                    }

                    int war =
                        _world.InGame && _world.Player.InWarMode
                            ? 1
                            : _world.InGame && _world.MapIndex != 0
                                ? 2
                                : 0;

                    ref IntPtr ptrCursor = ref _cursors_ptr[war, id];

                    if (ptrCursor != IntPtr.Zero)
                    {
                        SDL.SDL_SetCursor(ptrCursor);
                    }
                }
            }

            if (ItemHold.Enabled)
            {
                ushort draggingGraphic = GetDraggingItemGraphic();

                if (draggingGraphic != 0xFFFF && ItemHold.IsFixedPosition && !UIManager.IsDragging)
                {
                    ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(draggingGraphic);

                    Point offset = GetDraggingItemOffset();

                    int x = ItemHold.FixedX - offset.X;
                    int y = ItemHold.FixedY - offset.Y;

                    if (
                        Mouse.Position.X >= x
                        && Mouse.Position.X < x + artInfo.UV.Width
                        && Mouse.Position.Y >= y
                        && Mouse.Position.Y < y + artInfo.UV.Height
                    )
                    {
                        if (!ItemHold.IgnoreFixedPosition)
                        {
                            ItemHold.IsFixedPosition = false;
                            ItemHold.FixedX = 0;
                            ItemHold.FixedY = 0;
                        }
                    }
                    else if (ItemHold.IgnoreFixedPosition)
                    {
                        ItemHold.IgnoreFixedPosition = false;
                    }
                }
            }
        }

        public void Draw(UltimaBatcher2D sb)
        {
            if (_world.InGame && _world.TargetManager.IsTargeting && ProfileManager.CurrentProfile != null)
            {
                if (_world.TargetManager.TargetingState == CursorTarget.MultiPlacement)
                {
                    if (
                        _world.CustomHouseManager != null
                        && _world.CustomHouseManager.SelectedGraphic != 0
                    )
                    {
                        ushort hue = 0;

                        Array.Clear(_componentsList, 0, 10);

                        if (
                            !_world.CustomHouseManager.CanBuildHere(
                                _componentsList,
                                out CUSTOM_HOUSE_BUILD_TYPE type
                            )
                        )
                        {
                            hue = 0x0021;
                        }

                        //if (_temp.Count != list.Count)
                        {
                            _temp.ForEach(s => s.Destroy());
                            _temp.Clear();

                            for (int i = 0; i < _componentsList.Length; i++)
                            {
                                if (_componentsList[i].Graphic == 0)
                                {
                                    break;
                                }

                                Multi m = Multi.Create(_world, _componentsList[i].Graphic);

                                m.AlphaHue = 0xFF;
                                m.Hue = hue;
                                m.State = CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_PREVIEW;
                                _temp.Add(m);
                            }
                        }

                        if (_componentsList.Length != 0)
                        {
                            if (SelectedObject.Object is GameObject selectedObj)
                            {
                                int z = 0;

                                if (selectedObj.Z < _world.CustomHouseManager.MinHouseZ)
                                {
                                    if (
                                        selectedObj.X >= _world.CustomHouseManager.StartPos.X
                                        && selectedObj.X <= _world.CustomHouseManager.EndPos.X - 1
                                        && selectedObj.Y >= _world.CustomHouseManager.StartPos.Y
                                        && selectedObj.Y <= _world.CustomHouseManager.EndPos.Y - 1
                                    )
                                    {
                                        if (type != CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR)
                                        {
                                            z += 7;
                                        }
                                    }
                                }

                                for (int i = 0; i < _componentsList.Length; i++)
                                {
                                    ref readonly CustomBuildObject item = ref _componentsList[i];

                                    if (item.Graphic == 0)
                                    {
                                        break;
                                    }

                                    _temp[i].SetInWorldTile(
                                        (ushort)(selectedObj.X + item.X),
                                        (ushort)(selectedObj.Y + item.Y),
                                        (sbyte)(selectedObj.Z + z + item.Z)
                                    );
                                }
                            }
                        }
                    }
                    else if (_temp.Count != 0)
                    {
                        _temp.ForEach(s => s.Destroy());
                        _temp.Clear();
                    }
                }
                else if (_temp.Count != 0)
                {
                    _temp.ForEach(s => s.Destroy());
                    _temp.Clear();
                }

                if (ProfileManager.CurrentProfile.AuraOnMouse)
                {
                    ushort id = Graphic;

                    if (id < 0x206A)
                    {
                        id -= 0x2053;
                    }
                    else
                    {
                        id -= 0x206A;
                    }

                    ushort hue = 0;

                    switch (_world.TargetManager.TargetingType)
                    {
                        case TargetType.Neutral:
                            hue = 0x03b2;

                            break;

                        case TargetType.Harmful:
                            hue = 0x0023;

                            break;

                        case TargetType.Beneficial:
                            hue = 0x005A;

                            break;
                    }

                    _aura.Draw(sb, Mouse.Position.X, Mouse.Position.Y, hue, 0f);
                }

                if (ProfileManager.CurrentProfile.ShowTargetRangeIndicator)
                {
                    if (UIManager.IsMouseOverWorld)
                    {
                        if (SelectedObject.Object is GameObject obj)
                        {
                            string dist = obj.Distance.ToString();

                            Vector3 hue = new Vector3(0, 1, 1f);
                            sb.DrawString(
                                Fonts.Bold,
                                dist,
                                Mouse.Position.X - 26,
                                Mouse.Position.Y - 21,
                                hue
                            );

                            hue.Y = 0;
                            sb.DrawString(
                                Fonts.Bold,
                                dist,
                                Mouse.Position.X - 25,
                                Mouse.Position.Y - 20,
                                hue
                            );
                        }
                    }
                }
            }
            else if (_temp.Count != 0)
            {
                _temp.ForEach(s => s.Destroy());
                _temp.Clear();
            }

            if (ItemHold.Enabled && !ItemHold.Dropped)
            {
                float scale = 1;

                if (
                    ProfileManager.CurrentProfile != null
                    && ProfileManager.CurrentProfile.ScaleItemsInsideContainers
                )
                {
                    scale = UIManager.ContainerScale;
                }

                ushort draggingGraphic = GetDraggingItemGraphic();

                ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(draggingGraphic);

                if (artInfo.Texture != null)
                {
                    Point offset = GetDraggingItemOffset();

                    int x =
                        (ItemHold.IsFixedPosition ? ItemHold.FixedX : Mouse.Position.X) - offset.X;
                    int y =
                        (ItemHold.IsFixedPosition ? ItemHold.FixedY : Mouse.Position.Y) - offset.Y;

                    Vector3 hue = ShaderHueTranslator.GetHueVector(
                        ItemHold.Hue,
                        ItemHold.IsPartialHue,
                        ItemHold.HasAlpha ? .5f : 1f
                    );

                    var rect = new Rectangle(
                        x,
                        y,
                        (int)(artInfo.UV.Width * scale),
                        (int)(artInfo.UV.Height * scale)
                    );

                    sb.Draw(artInfo.Texture, rect, artInfo.UV, hue);

                    if (
                        ItemHold.Amount > 1
                        && ItemHold.DisplayedGraphic == ItemHold.Graphic
                        && ItemHold.IsStackable
                    )
                    {
                        rect.X += 5;
                        rect.Y += 5;

                        sb.Draw(artInfo.Texture, rect, artInfo.UV, hue);
                    }
                }
            }

            DrawToolTip(sb, Mouse.Position);

            if (!Settings.GlobalSettings.RunMouseInASeparateThread)
            {
                Graphic = AssignGraphicByState();

                ushort graphic = Graphic;

                if (graphic < 0x206A)
                {
                    graphic -= 0x2053;
                }
                else
                {
                    graphic -= 0x206A;
                }

                int offX = _cursorOffset[0, graphic];
                int offY = _cursorOffset[1, graphic];

                Vector3 hueVec;

                if (_world.InGame && _world.MapIndex != 0 && !_world.Player.InWarMode)
                {
                    hueVec = ShaderHueTranslator.GetHueVector(0x0033);
                }
                else
                {
                    hueVec = ShaderHueTranslator.GetHueVector(0);
                }

                ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(Graphic);

                var rect = artInfo.UV;

                const int BORDER_SIZE = 1;
                rect.X += BORDER_SIZE;
                rect.Y += BORDER_SIZE;
                rect.Width -= BORDER_SIZE * 2;
                rect.Height -= BORDER_SIZE * 2;

                sb.Draw(
                    artInfo.Texture,
                    new Vector2(Mouse.Position.X - offX, Mouse.Position.Y - offY),
                    rect,
                    hueVec
                );
            }
        }

        private void DrawToolTip(UltimaBatcher2D batcher, Point position)
        {
            if (Client.Game.Scene is GameScene gs)
            {
                if (
                    !_world.ClientFeatures.TooltipsEnabled
                    || (
                        SelectedObject.Object is Item selectedItem
                        && selectedItem.IsLocked
                        && selectedItem.ItemData.Weight == 255
                        && !selectedItem.ItemData.IsContainer
                        &&
                        // We need to check if OPL contains data.
                        // If not we can ignore tooltip.
                        !_world.OPL.Contains(selectedItem)
                    )
                    || (ItemHold.Enabled && !ItemHold.IsFixedPosition)
                )
                {
                    if (
                        !_tooltip.IsEmpty
                        && (UIManager.MouseOverControl == null || UIManager.IsMouseOverWorld)
                    )
                    {
                        _tooltip.Clear();
                    }
                }
                else
                {
                    if (
                        UIManager.IsMouseOverWorld
                        && SelectedObject.Object is Entity item
                        && _world.OPL.Contains(item)
                    )
                    {
                        if (_tooltip.IsEmpty || item != _tooltip.Serial)
                        {
                            _tooltip.SetGameObject(item);
                        }

                        _tooltip.Draw(batcher, position.X, position.Y + 24);

                        return;
                    }

                    if (
                        UIManager.MouseOverControl != null
                        && UIManager.MouseOverControl.Tooltip is uint serial
                    )
                    {
                        if (SerialHelper.IsValid(serial) && _world.OPL.Contains(serial))
                        {
                            if (_tooltip.IsEmpty || serial != _tooltip.Serial)
                            {
                                _tooltip.SetGameObject(serial);
                            }

                            _tooltip.Draw(batcher, position.X, position.Y + 24);

                            return;
                        }
                    }
                }
            }

            if (
                UIManager.MouseOverControl != null
                && UIManager.MouseOverControl.HasTooltip
                && !Mouse.IsDragging
            )
            {
                if (UIManager.MouseOverControl.Tooltip is string text)
                {
                    if (_tooltip.IsEmpty || _tooltip.Text != text)
                    {
                        _tooltip.SetText(text, UIManager.MouseOverControl.TooltipMaxLength);
                    }

                    _tooltip.Draw(batcher, position.X, position.Y + 24);
                }
            }
            else if (!_tooltip.IsEmpty)
            {
                _tooltip.Clear();
            }
        }

        private ushort AssignGraphicByState()
        {
            int war = _world.InGame && _world.Player.InWarMode ? 1 : 0;

            if (_world.TargetManager.IsTargeting)
            {
                return _cursorData[war, 12];
            }

            if (UIManager.IsDragging || IsDraggingCursorForced)
            {
                return _cursorData[war, 8];
            }

            if (IsLoading)
            {
                return _cursorData[war, 13];
            }

            if (
                UIManager.MouseOverControl != null
                && UIManager.MouseOverControl.AcceptKeyboardInput
                && UIManager.MouseOverControl.IsEditable
            )
            {
                return _cursorData[war, 14];
            }

            ushort result = _cursorData[war, 9];

            if (!UIManager.IsMouseOverWorld)
            {
                return result;
            }

            if (ProfileManager.CurrentProfile == null)
            {
                return result;
            }

            var camera = Client.Game.Scene.Camera;

            int windowCenterX = camera.Bounds.X + (camera.Bounds.Width >> 1);
            int windowCenterY = camera.Bounds.Y + (camera.Bounds.Height >> 1);

            return _cursorData[
                war,
                GetMouseDirection(
                    windowCenterX,
                    windowCenterY,
                    Mouse.Position.X,
                    Mouse.Position.Y,
                    1
                )
            ];
        }

        public static int GetMouseDirection(int x1, int y1, int to_x, int to_y, int current_facing)
        {
            int shiftX = to_x - x1;
            int shiftY = to_y - y1;
            int hashf = 100 * (Sgn(shiftX) + 2) + 10 * (Sgn(shiftY) + 2);

            if (shiftX != 0 && shiftY != 0)
            {
                shiftX = Math.Abs(shiftX);
                shiftY = Math.Abs(shiftY);

                if (shiftY * 5 <= shiftX * 2)
                {
                    hashf = hashf + 1;
                }
                else if (shiftY * 2 >= shiftX * 5)
                {
                    hashf = hashf + 3;
                }
                else
                {
                    hashf = hashf + 2;
                }
            }
            else if (shiftX == 0)
            {
                if (shiftY == 0)
                {
                    return current_facing;
                }
            }

            switch (hashf)
            {
                case 111:
                    return (int)Direction.West; // W

                case 112:
                    return (int)Direction.Up; // NW

                case 113:
                    return (int)Direction.North; // N

                case 120:
                    return (int)Direction.West; // W

                case 131:
                    return (int)Direction.West; // W

                case 132:
                    return (int)Direction.Left; // SW

                case 133:
                    return (int)Direction.South; // S

                case 210:
                    return (int)Direction.North; // N

                case 230:
                    return (int)Direction.South; // S

                case 311:
                    return (int)Direction.East; // E

                case 312:
                    return (int)Direction.Right; // NE

                case 313:
                    return (int)Direction.North; // N

                case 320:
                    return (int)Direction.East; // E

                case 331:
                    return (int)Direction.East; // E

                case 332:
                    return (int)Direction.Down; // SE

                case 333:
                    return (int)Direction.South; // S
            }

            return current_facing;
        }

        private static int Sgn(int val)
        {
            int a = 0 < val ? 1 : 0;
            int b = val < 0 ? 1 : 0;

            return a - b;
        }

        private readonly struct CursorInfo
        {
            public CursorInfo(IntPtr ptr, int w, int h)
            {
                CursorPtr = ptr;
                Width = w;
                Height = h;
            }

            public readonly int Width,
                Height;
            public readonly IntPtr CursorPtr;
        }
    }
}
