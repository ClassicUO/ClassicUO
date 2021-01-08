﻿#region license

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
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
// ## BEGIN - END ## //
using ClassicUO.Game.InteropServices.Runtime.UOClassicCombat;
// ## BEGIN - END ## //
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
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
                0x206A, 0x206B, 0x206C, 0x206D, 0x206E, 0x206F, 0x2070, 0x2071, 0x2072, 0x2073, 0x2074, 0x2075, 0x2076,
                0x2077, 0x2078, 0x2079
            },
            {
                0x2053, 0x2054, 0x2055, 0x2056, 0x2057, 0x2058, 0x2059, 0x205A, 0x205B, 0x205C, 0x205D, 0x205E, 0x205F,
                0x2060, 0x2061, 0x2062
            },
            {
                0x206A, 0x206B, 0x206C, 0x206D, 0x206E, 0x206F, 0x2070, 0x2071, 0x2072, 0x2073, 0x2074, 0x2075, 0x2076,
                0x2077, 0x2078, 0x2079
            }
        };
        private static Vector3 _vec = Vector3.Zero;

        private readonly Texture2D _aura;
        private Vector3 _auraVector = new Vector3(0, 13, 0);

        private readonly CustomBuildObject[] _componentsList = new CustomBuildObject[10];
        private readonly int[,] _cursorOffset = new int[2, 16];
        private readonly IntPtr[,] _cursors_ptr = new IntPtr[3, 16];
        private UOTexture _draggedItemTexture;
        private ushort _graphic = 0x2073;
        private bool _needGraphicUpdate = true;
        private Point _offset;
        private readonly RenderedText _targetDistanceText = RenderedText.Create
            (string.Empty, 0x0481, style: FontStyle.BlackBorder);
        private readonly List<Multi> _temp = new List<Multi>();
        private readonly Tooltip _tooltip;
        // ## BEGIN - END ## //
        private Vector3 _spellIconVector = new Vector3(0, 13, 0);
        public static RenderedText _spellTimeText { get; set; }
        public static uint _spellTime { get; set; }
        public static uint _startSpellTime { get; set; }
        public static bool _fieldEastToWest { get; set; }
        // ## BEGIN - END ## //


        public GameCursor()
        {
            short ww = 0;
            short hh = 0;
            uint[] data = CircleOfTransparency.CreateCircleTexture(25, ref ww, ref hh);

            for (int i = 0; i < data.Length; i++)
            {
                ref uint pixel = ref data[i];

                if (pixel != 0)
                {
                    ushort value = (ushort) (pixel << 3);

                    if (value > 0xFF)
                    {
                        value = 0xFF;
                    }

                    pixel = (uint) ((value << 24) | (value << 16) | (value << 8) | value);
                }
            }

            _aura = new Texture2D(Client.Game.GraphicsDevice, ww, hh);
            _aura.SetData(data);

            _tooltip = new Tooltip();

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    ushort id = _cursorData[i, j];

                    uint[] pixels = ArtLoader.Instance.ReadStaticArt(id, out short w, out short h, out _);

                    if (i == 0)
                    {
                        if (pixels != null && pixels.Length > 0)
                        {
                            float offX = 0;
                            float offY = 0;
                            float dw = w;
                            float dh = h;

                            if (id == 0x206A)
                            {
                                offX = -4f;
                            }
                            else if (id == 0x206B)
                            {
                                offX = -dw + 3f;
                            }
                            else if (id == 0x206C)
                            {
                                offX = -dw + 3f;
                                offY = -(dh / 2f);
                            }
                            else if (id == 0x206D)
                            {
                                offX = -dw;
                                offY = -dh;
                            }
                            else if (id == 0x206E)
                            {
                                offX = -(dw * 0.66f);
                                offY = -dh;
                            }
                            else if (id == 0x206F)
                            {
                                offY = -dh + 4f;
                            }
                            else if (id == 0x2070)
                            {
                                offY = -dh + 4f;
                            }
                            else if (id == 0x2075)
                            {
                                offY = -4f;
                            }
                            else if (id == 0x2076)
                            {
                                offX = -12f;
                                offY = -14f;
                            }
                            else if (id == 0x2077)
                            {
                                offX = -(dw / 2f);
                                offY = -(dh / 2f);
                            }
                            else if (id == 0x2078)
                            {
                                offY = -(dh * 0.66f);
                            }
                            else if (id == 0x2079)
                            {
                                offY = -(dh / 2f);
                            }

                            switch (id)
                            {
                                case 0x206B:
                                    offX = -29;
                                    offY = -1;

                                    break;

                                case 0x206C:
                                    offX = -41;
                                    offY = -9;

                                    break;

                                case 0x206D:
                                    offX = -36;
                                    offY = -25;

                                    break;

                                case 0x206E:
                                    offX = -14;
                                    offY = -33;

                                    break;

                                case 0x206F:
                                    offX = -2;
                                    offY = -26;

                                    break;

                                case 0x2070:
                                    offX = -3;
                                    offY = -8;

                                    break;

                                case 0x2071:
                                    offX = -1;
                                    offY = -1;

                                    break;

                                case 0x206A:
                                    offX = -4;
                                    offY = -2;

                                    break;

                                case 0x2075:
                                    offX = -2;
                                    offY = -10;

                                    break;
                            }

                            //if (offX == 0 && offY == 0)
                            //{
                            //    offX = -1;
                            //    offY = -1;
                            //}


                            _cursorOffset[0, j] = (int) offX;
                            _cursorOffset[1, j] = (int) offY;
                        }
                        else
                        {
                            _cursorOffset[0, j] = 0;
                            _cursorOffset[1, j] = 0;
                        }
                    }

                    if (pixels != null && pixels.Length != 0)
                    {
                        unsafe
                        {
                            fixed (uint* ptr = pixels)
                            {
                                SDL.SDL_Surface* surface = (SDL.SDL_Surface*) SDL.SDL_CreateRGBSurfaceWithFormatFrom
                                    ((IntPtr) ptr, w, h, 32, 4 * w, SDL.SDL_PIXELFORMAT_ABGR8888);

                                if (i == 2)
                                {
                                    int stride = surface->pitch >> 2;
                                    uint* pixels_ptr = (uint*) surface->pixels;
                                    uint* p_line_end = pixels_ptr + w;
                                    uint* p_img_end = pixels_ptr + stride * h;
                                    int delta = stride - w;
                                    Color c = default;

                                    while (pixels_ptr < p_img_end)
                                    {
                                        while (pixels_ptr < p_line_end)
                                        {
                                            if (*pixels_ptr != 0 && *pixels_ptr != 0xFF_00_00_00)
                                            {
                                                c.PackedValue = *pixels_ptr;

                                                *pixels_ptr = HuesHelper.Color16To32
                                                              (
                                                                  HuesLoader.Instance.GetColor16
                                                                      (HuesHelper.ColorToHue(c), 0x0033)
                                                              ) |
                                                              0xFF_00_00_00;
                                            }

                                            ++pixels_ptr;
                                        }

                                        pixels_ptr += delta;
                                        p_line_end += stride;
                                    }
                                }

                                int hotX = -_cursorOffset[0, j];
                                int hotY = -_cursorOffset[1, j];

                                _cursors_ptr[i, j] = SDL.SDL_CreateColorCursor((IntPtr) surface, hotX, hotY);
                            }
                        }
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


        public void SetDraggedItem(Point? offset)
        {
            _draggedItemTexture = ItemHold.IsGumpTexture ?
                GumpsLoader.Instance.GetTexture
                    ((ushort) (ItemHold.DisplayedGraphic - Constants.ITEM_GUMP_TEXTURE_OFFSET)) :
                ArtLoader.Instance.GetTexture(ItemHold.DisplayedGraphic);

            if (_draggedItemTexture == null)
            {
                return;
            }

            float scale = 1;

            if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.ScaleItemsInsideContainers)
            {
                scale = UIManager.ContainerScale;
            }

            _offset.X = (int) ((_draggedItemTexture.Width >> 1) * scale);
            _offset.Y = (int) ((_draggedItemTexture.Height >> 1) * scale);

            if (offset.HasValue)
            {
                _offset -= offset.Value;
            }
        }

        public void Update(double totalTime, double frameTime)
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

                    int war = World.InGame && World.Player.InWarMode ? 1 : World.InGame && World.MapIndex != 0 ? 2 : 0;

                    ref IntPtr ptrCursor = ref _cursors_ptr[war, id];

                    if (ptrCursor != IntPtr.Zero)
                    {
                        SDL.SDL_SetCursor(ptrCursor);
                    }
                }
            }

            if (ItemHold.Enabled)
            {
                _draggedItemTexture.Ticks = (long) totalTime;

                if (ItemHold.IsFixedPosition && !UIManager.IsDragging)
                {
                    int x = ItemHold.FixedX - _offset.X;
                    int y = ItemHold.FixedY - _offset.Y;

                    if (Mouse.Position.X >= x && Mouse.Position.X < x + _draggedItemTexture.Width &&
                        Mouse.Position.Y >= y && Mouse.Position.Y < y + _draggedItemTexture.Height)
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
            if (World.InGame && TargetManager.IsTargeting && ProfileManager.CurrentProfile != null)
            {
                if (TargetManager.TargetingState == CursorTarget.MultiPlacement)
                {
                    if (World.CustomHouseManager != null && World.CustomHouseManager.SelectedGraphic != 0)
                    {
                        ushort hue = 0;

                        Array.Clear(_componentsList, 0, 10);

                        if (!World.CustomHouseManager.CanBuildHere(_componentsList, out CUSTOM_HOUSE_BUILD_TYPE type))
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

                                Multi m = Multi.Create(_componentsList[i].Graphic);

                                m.AlphaHue = 0xFF;
                                m.Hue = hue;
                                m.State = CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_PREVIEW;
                                _temp.Add(m);
                            }
                        }

                        if (_componentsList.Length != 0)
                        {
                            if (SelectedObject.LastObject is GameObject selectedObj)
                            {
                                int z = 0;

                                if (selectedObj.Z < World.CustomHouseManager.MinHouseZ)
                                {
                                    if (selectedObj.X >= World.CustomHouseManager.StartPos.X &&
                                        selectedObj.X <= World.CustomHouseManager.EndPos.X - 1 &&
                                        selectedObj.Y >= World.CustomHouseManager.StartPos.Y &&
                                        selectedObj.Y <= World.CustomHouseManager.EndPos.Y - 1)
                                    {
                                        if (type != CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR)
                                        {
                                            z += 7;
                                        }
                                    }
                                }

                                GameScene gs = Client.Game.GetScene<GameScene>();

                                for (int i = 0; i < _componentsList.Length; i++)
                                {
                                    ref readonly CustomBuildObject item = ref _componentsList[i];

                                    if (item.Graphic == 0)
                                    {
                                        break;
                                    }

                                    _temp[i].X = (ushort) (selectedObj.X + item.X);

                                    _temp[i].Y = (ushort) (selectedObj.Y + item.Y);

                                    _temp[i].Z = (sbyte) (selectedObj.Z + z + item.Z);

                                    _temp[i].UpdateRealScreenPosition(gs.ScreenOffset.X, gs.ScreenOffset.Y);

                                    _temp[i].UpdateScreenPosition();

                                    _temp[i].AddToTile();
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

                    int hotX = _cursorOffset[0, id];
                    int hotY = _cursorOffset[1, id];

                    switch (TargetManager.TargetingType)
                    {
                        case TargetType.Neutral:
                            _auraVector.X = 0x03b2;

                            break;

                        case TargetType.Harmful:
                            _auraVector.X = 0x0023;

                            break;

                        case TargetType.Beneficial:
                            _auraVector.X = 0x005A;

                            break;
                    }

                    sb.Draw2D
                    (
                        _aura, Mouse.Position.X + hotX - (25 >> 1), Mouse.Position.Y + hotY - (25 >> 1), ref _auraVector
                    );
                }

                // ## BEGIN - END ## //
                if (GameActions.LastSpellIndexCursor >= 1 && GameActions.LastSpellIndexCursor <= 64)
                {
                    UOClassicCombatCollection.UpdateSpelltime();

                    if (_spellTime < 10 && ProfileManager.CurrentProfile.SpellOnCursor)
                        _spellTimeText.Draw(sb, Mouse.Position.X + ProfileManager.CurrentProfile.SpellOnCursorOffset.X - 17, Mouse.Position.Y + ProfileManager.CurrentProfile.SpellOnCursorOffset.Y, 0);

                    _spellIconVector.X = UOClassicCombatCollection.SpellIconHue(_spellIconVector.X);

                    SpellDefinition def = SpellsMagery.GetSpell(GameActions.LastSpellIndexCursor);

                    if (ProfileManager.CurrentProfile.SpellOnCursor)
                        sb.Draw2D(GumpsLoader.Instance.GetTexture((ushort)def.GumpIconSmallID), Mouse.Position.X + ProfileManager.CurrentProfile.SpellOnCursorOffset.X, Mouse.Position.Y + ProfileManager.CurrentProfile.SpellOnCursorOffset.Y, 20, 20, ref _spellIconVector);
                }
                // ## BEGIN - END ## //

                if (ProfileManager.CurrentProfile.ShowTargetRangeIndicator)
                {
                    if (UIManager.IsMouseOverWorld)
                    {
                        if (SelectedObject.Object is GameObject obj)
                        {
                            _targetDistanceText.Text = obj.Distance.ToString();

                            _targetDistanceText.Draw(sb, Mouse.Position.X - 25, Mouse.Position.Y - 20);
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

                if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.ScaleItemsInsideContainers)
                {
                    scale = UIManager.ContainerScale;
                }

                int x = (ItemHold.IsFixedPosition ? ItemHold.FixedX : Mouse.Position.X) - _offset.X;
                int y = (ItemHold.IsFixedPosition ? ItemHold.FixedY : Mouse.Position.Y) - _offset.Y;

                Vector3 hue = Vector3.Zero;

                ShaderHueTranslator.GetHueVector
                    (ref hue, ItemHold.Hue, ItemHold.IsPartialHue, ItemHold.HasAlpha ? .5f : 0);

                sb.Draw2D
                (
                    _draggedItemTexture, x, y, _draggedItemTexture.Width * scale, _draggedItemTexture.Height * scale,
                    ref hue
                );

                if (ItemHold.Amount > 1 && ItemHold.DisplayedGraphic == ItemHold.Graphic && ItemHold.IsStackable)
                {
                    x += 5;
                    y += 5;

                    sb.Draw2D
                    (
                        _draggedItemTexture, x, y, _draggedItemTexture.Width * scale,
                        _draggedItemTexture.Height * scale, ref hue
                    );
                }
            }

            DrawToolTip(sb, Mouse.Position);

            if (!Settings.GlobalSettings.RunMouseInASeparateThread)
            {
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

                if (World.InGame && World.MapIndex != 0 && !World.Player.InWarMode)
                {
                    _vec.X = 0x0034;
                    _vec.Y = 1;
                    _vec.Z = 0;
                }
                else
                {
                    _vec = Vector3.Zero;
                }

                sb.Draw2D
                (
                    ArtLoader.Instance.GetTexture(Graphic), Mouse.Position.X + offX, Mouse.Position.Y + offY, ref _vec
                );
            }
        }

        private void DrawToolTip(UltimaBatcher2D batcher, Point position)
        {
            if (Client.Game.Scene is GameScene gs)
            {
                if (!World.ClientFeatures.TooltipsEnabled ||
                    SelectedObject.Object is Item selectedItem && selectedItem.IsLocked &&
                    selectedItem.ItemData.Weight == 255 && !selectedItem.ItemData.IsContainer ||
                    ItemHold.Enabled && !ItemHold.IsFixedPosition)
                {
                    if (!_tooltip.IsEmpty && (UIManager.MouseOverControl == null || UIManager.IsMouseOverWorld))
                    {
                        _tooltip.Clear();
                    }
                }
                else
                {
                    if (UIManager.IsMouseOverWorld && SelectedObject.Object is Entity item && World.OPL.Contains(item))
                    {
                        if (_tooltip.IsEmpty || item != _tooltip.Serial)
                        {
                            _tooltip.SetGameObject(item);
                        }

                        _tooltip.Draw(batcher, position.X, position.Y + 24);

                        return;
                    }

                    if (UIManager.MouseOverControl != null && UIManager.MouseOverControl.Tooltip is uint serial)
                    {
                        if (SerialHelper.IsValid(serial) && World.OPL.Contains(serial))
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

            if (UIManager.MouseOverControl != null && UIManager.MouseOverControl.HasTooltip && !Mouse.IsDragging)
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
            int war = World.InGame && World.Player.InWarMode ? 1 : 0;

            if (TargetManager.IsTargeting)
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

            if (UIManager.MouseOverControl != null && UIManager.MouseOverControl.AcceptKeyboardInput &&
                UIManager.MouseOverControl.IsEditable)
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

            int windowCenterX = ProfileManager.CurrentProfile.GameWindowPosition.X +
                                (ProfileManager.CurrentProfile.GameWindowSize.X >> 1);

            int windowCenterY = ProfileManager.CurrentProfile.GameWindowPosition.Y +
                                (ProfileManager.CurrentProfile.GameWindowSize.Y >> 1);

            return _cursorData[
                war, GetMouseDirection(windowCenterX, windowCenterY, Mouse.Position.X, Mouse.Position.Y, 1)];
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
                case 111: return (int) Direction.West; // W

                case 112: return (int) Direction.Up; // NW

                case 113: return (int) Direction.North; // N

                case 120: return (int) Direction.West; // W

                case 131: return (int) Direction.West; // W

                case 132: return (int) Direction.Left; // SW

                case 133: return (int) Direction.South; // S

                case 210: return (int) Direction.North; // N

                case 230: return (int) Direction.South; // S

                case 311: return (int) Direction.East; // E

                case 312: return (int) Direction.Right; // NE

                case 313: return (int) Direction.North; // N

                case 320: return (int) Direction.East; // E

                case 331: return (int) Direction.East; // E

                case 332: return (int) Direction.Down; // SE

                case 333: return (int) Direction.South; // S
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

            public readonly int Width, Height;
            public readonly IntPtr CursorPtr;
        }
    }
}