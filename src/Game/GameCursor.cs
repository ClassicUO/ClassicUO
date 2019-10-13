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

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using SDL2;

namespace ClassicUO.Game
{
    internal sealed class GameCursor
    {
        private static readonly ushort[,] _cursorData = new ushort[2, 16]
        {
            {
                0x206A, 0x206B, 0x206C, 0x206D, 0x206E, 0x206F, 0x2070, 0x2071, 0x2072, 0x2073, 0x2074, 0x2075, 0x2076, 0x2077, 0x2078, 0x2079
            },
            {
                0x2053, 0x2054, 0x2055, 0x2056, 0x2057, 0x2058, 0x2059, 0x205A, 0x205B, 0x205C, 0x205D, 0x205E, 0x205F, 0x2060, 0x2061, 0x2062
            }
        };

        private readonly Texture2D _aura;
        private readonly int[,] _cursorOffset = new int[2, 16];

        private readonly CursorInfo[,] _cursorPixels = new CursorInfo[2, 16];
        private readonly Tooltip _tooltip;
        private Vector3 _auraVector = new Vector3(0, 13, 0);
        private RenderedText _targetDistanceText = RenderedText.Create(String.Empty, 0x0481, style: FontStyle.BlackBorder);

        private IntPtr _cursor, _surface;
        private UOTexture _draggedItemTexture;
        private Graphic _graphic = 0x2073;

        private ItemHold _itemHold;
        private bool _needGraphicUpdate = true;
        private Point _offset;
        private Rectangle _rect;

        public GameCursor()
        {
            short ww = 0;
            short hh = 0;
            uint[] data = CircleOfTransparency.CreateTexture(25, ref ww, ref hh);

            for (int i = 0; i < data.Length; i++)
            {
                ref uint pixel = ref data[i];

                if (pixel != 0)
                {
                    ushort value = (ushort) (pixel << 3);

                    if (value > 0xFF)
                        value = 0xFF;

                    pixel = (uint) ((value << 24) | (value << 16) | (value << 8) | value);
                }
            }

            _aura = new Texture2D(Engine.Batcher.GraphicsDevice, ww, hh);
            _aura.SetData(data);

            _tooltip = new Tooltip();

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    ushort id = _cursorData[i, j];

                    ushort[] pixels = FileManager.Art.ReadStaticArt(id, out short w, out short h, out _);

                    if (i == 0)
                    {
                        if (pixels != null && pixels.Length > 0)
                        {
                            float offX = 0;
                            float offY = 0;
                            float dw = w;
                            float dh = h;

                            if (id == 0x206A)
                                offX = -4f;
                            else if (id == 0x206B)
                                offX = -dw + 3f;
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
                                offY = -dh + 4f;
                            else if (id == 0x2070)
                                offY = -dh + 4f;
                            else if (id == 0x2075)
                                offY = -4f;
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
                                offY = -(dh * 0.66f);
                            else if (id == 0x2079) offY = -(dh / 2f);

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

                    if (pixels != null && pixels.Length != 0) _cursorPixels[i, j] = new CursorInfo(pixels, w, h);
                }
            }
        }

        public Graphic Graphic
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


        public void SetDraggedItem(ItemHold hold)
        {
            _itemHold = hold;
            _draggedItemTexture = FileManager.Art.GetTexture(_itemHold.DisplayedGraphic);
            _offset.X = _draggedItemTexture.Width >> 1;
            _offset.Y = _draggedItemTexture.Height >> 1;

            _rect.Width = _draggedItemTexture.Width;
            _rect.Height = _draggedItemTexture.Height;
        }

        public unsafe void Update(double totalMS, double frameMS)
        {
            Graphic = AssignGraphicByState();

            if (_needGraphicUpdate)
            {
                _needGraphicUpdate = false;

                if (Engine.GlobalSettings.RunMouseInASeparateThread)
                {
                    if (_cursor != IntPtr.Zero)
                        SDL.SDL_FreeCursor(_cursor);

                    ushort id = Graphic;

                    if (id < 0x206A)
                        id -= 0x2053;
                    else
                        id -= 0x206A;
                    int war = World.InGame && World.Player.InWarMode ? 1 : 0;

                    ref readonly CursorInfo info = ref _cursorPixels[war, id];

                    fixed (ushort* ptr = info.Pixels)
                        _surface = SDL.SDL_CreateRGBSurfaceWithFormatFrom((IntPtr) ptr, info.Width, info.Height, 16, 2 * info.Width, SDL.SDL_PIXELFORMAT_ARGB1555);

                    if (_surface != IntPtr.Zero)
                    {
                        int hotX = -_cursorOffset[0, id];
                        int hotY = -_cursorOffset[1, id];

                        _cursor = SDL.SDL_CreateColorCursor(_surface, hotX, hotY);
                        SDL.SDL_SetCursor(_cursor);
                        SDL.SDL_FreeSurface(_surface);
                    }
                }
            }

            if (_itemHold != null && _itemHold.Enabled)
                _draggedItemTexture.Ticks = (long) totalMS;
        }

        private static Vector3 _vec = Vector3.Zero;
        public void Draw(UltimaBatcher2D sb)
        {
            if (TargetManager.IsTargeting && Engine.Profile.Current != null)
            {
                if (Engine.Profile.Current.AuraOnMouse)
                {
                    ushort id = Graphic;

                    if (id < 0x206A)
                        id -= 0x2053;
                    else
                        id -= 0x206A;

                    int hotX = _cursorOffset[0, id];
                    int hotY = _cursorOffset[1, id];

                    switch (TargetManager.TargeringType)
                    {
                        case TargetType.Neutral:
                            _auraVector.X = 0x03B2;

                            break;

                        case TargetType.Harmful:
                            _auraVector.X = 0x0023;

                            break;

                        case TargetType.Beneficial:
                            _auraVector.X = 0x005A;

                            break;
                    }

                    sb.Draw2D(_aura, Mouse.Position.X + hotX - (25 >> 1), Mouse.Position.Y + hotY - (25 >> 1), ref _auraVector);
                }

                if (Engine.Profile.Current.ShowTargetRangeIndicator)
                {
                    GameScene gs = Engine.SceneManager.GetScene<GameScene>();

                    if (gs != null && gs.IsMouseOverViewport)
                    {
                        if (SelectedObject.Object is GameObject obj)
                        {
                            _targetDistanceText.Text = obj.Distance.ToString();

                            _targetDistanceText.Draw(sb, Mouse.Position.X - 25, Mouse.Position.Y - 20, 0);
                        }
                    }
                }
            }

           
            if (_itemHold != null && _itemHold.Enabled && !_itemHold.Dropped)
            {
                float scale = 1;

                if (Engine.Profile.Current != null && Engine.Profile.Current.ScaleItemsInsideContainers)
                    scale = Engine.UI.ContainerScale;

                int x = Mouse.Position.X - (int) (_offset.X * scale);
                int y = Mouse.Position.Y - (int) (_offset.Y * scale);

                Vector3 hue = Vector3.Zero;
                ShaderHuesTraslator.GetHueVector(ref hue, _itemHold.Hue, _itemHold.IsPartialHue, _itemHold.HasAlpha ? .5f : 0);

                sb.Draw2D(_draggedItemTexture, x, y, _rect.Width * scale, _rect.Height * scale, _rect.X, _rect.Y, _rect.Width, _rect.Height, ref hue);

                if (_itemHold.Amount > 1 && _itemHold.DisplayedGraphic == _itemHold.Graphic && _itemHold.IsStackable)
                {
                    x += 5;
                    y += 5;
                    sb.Draw2D(_draggedItemTexture, x, y, _rect.Width * scale, _rect.Height * scale, _rect.X, _rect.Y, _rect.Width, _rect.Height, ref hue);
                }
            }

            DrawToolTip(sb, Mouse.Position);

            if (!Engine.GlobalSettings.RunMouseInASeparateThread)
            {
                ushort graphic = Graphic;

                if (graphic < 0x206A)
                    graphic -= 0x2053;
                else
                    graphic -= 0x206A;

                int offX = _cursorOffset[0, graphic];
                int offY = _cursorOffset[1, graphic];

                sb.Draw2D(FileManager.Art.GetTexture(Graphic), Mouse.Position.X + offX, Mouse.Position.Y + offY, ref _vec);
            }

        }

        private void DrawToolTip(UltimaBatcher2D batcher, Point position)
        {
            if (Engine.SceneManager.CurrentScene is GameScene gs)
            {
                if (!World.ClientFeatures.TooltipsEnabled || gs.IsHoldingItem)
                {
                    if (!_tooltip.IsEmpty)
                        _tooltip.Clear();
                }
                else
                {
                    if (gs.IsMouseOverViewport && SelectedObject.Object is Entity item && World.OPL.Contains(item))
                    {
                        if (_tooltip.IsEmpty || item != _tooltip.Object)
                            _tooltip.SetGameObject(item);
                        _tooltip.Draw(batcher, position.X, position.Y + 24);

                        return;
                    }

                    if (Engine.UI.IsMouseOverAControl)
                    {
                        Entity it = null;

                        switch (Engine.UI.MouseOverControl)
                        {
                            //case EquipmentSlot equipmentSlot:
                            //    it = equipmentSlot.Item;

                            //    break;

                            case ItemGump gumpling:
                                it = World.Items.Get(gumpling.LocalSerial);

                                break;

                            case Control control when control.Tooltip is Item i:
                                it = i;

                                break;

                            case NameOverheadGump overhead:
                                it = overhead.Entity;
                                break;
                        }

                        if (it != null && World.OPL.Contains(it))
                        {
                            if (_tooltip.IsEmpty || it != _tooltip.Object)
                                _tooltip.SetGameObject(it);
                            _tooltip.Draw(batcher, position.X, position.Y + 24);

                            return;
                        }
                    }
                }
            }

            if (Engine.UI.IsMouseOverAControl && Engine.UI.MouseOverControl != null && Engine.UI.MouseOverControl.HasTooltip && !Mouse.IsDragging)
            {
                if (Engine.UI.MouseOverControl.Tooltip is string text)
                {
                    if (_tooltip.Text != text)
                        _tooltip.Clear();

                    if (_tooltip.IsEmpty)
                        _tooltip.SetText(text, Engine.UI.MouseOverControl.TooltipMaxLength);

                    _tooltip.Draw(batcher, position.X, position.Y + 24);
                }
            }
            else if (!_tooltip.IsEmpty) _tooltip.Clear();
        }

        private ushort AssignGraphicByState()
        {
            int war = World.InGame && World.Player.InWarMode ? 1 : 0;

            if (TargetManager.IsTargeting)
            {
                GameScene gs = Engine.SceneManager.GetScene<GameScene>();

                if (gs != null && !gs.IsHoldingItem)
                    return _cursorData[war, 12];
            }

            if (Engine.UI.IsDragging || IsDraggingCursorForced)
                return _cursorData[war, 8];

            if (IsLoading)
                return _cursorData[war, 13];

            if (Engine.UI.MouseOverControl is AbstractTextBox t && t.IsEditable)
                return _cursorData[war, 14];

            ushort result = _cursorData[war, 9];

            if (!Engine.UI.IsMouseOverWorld)
                return result;

            if (Engine.Profile.Current == null)
                return result;

            int windowCenterX = Engine.Profile.Current.GameWindowPosition.X + (Engine.Profile.Current.GameWindowSize.X >> 1);
            int windowCenterY = Engine.Profile.Current.GameWindowPosition.Y + (Engine.Profile.Current.GameWindowSize.Y >> 1);

            return _cursorData[war, GetMouseDirection(windowCenterX, windowCenterY, Mouse.Position.X, Mouse.Position.Y, 1)];
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
                    hashf = hashf + 1;
                else if (shiftY * 2 >= shiftX * 5)
                    hashf = hashf + 3;
                else
                    hashf = hashf + 2;
            }
            else if (shiftX == 0)
            {
                if (shiftY == 0)
                    return current_facing;
            }

            switch (hashf)
            {
                case 111:

                    return (int) Direction.West; // W

                case 112:

                    return (int) Direction.Up; // NW

                case 113:

                    return (int) Direction.North; // N

                case 120:

                    return (int) Direction.West; // W

                case 131:

                    return (int) Direction.West; // W

                case 132:

                    return (int) Direction.Left; // SW

                case 133:

                    return (int) Direction.South; // S

                case 210:

                    return (int) Direction.North; // N

                case 230:

                    return (int) Direction.South; // S

                case 311:

                    return (int) Direction.East; // E

                case 312:

                    return (int) Direction.Right; // NE

                case 313:

                    return (int) Direction.North; // N

                case 320:

                    return (int) Direction.East; // E

                case 331:

                    return (int) Direction.East; // E

                case 332:

                    return (int) Direction.Down; // SE

                case 333:

                    return (int) Direction.South; // S
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
            public CursorInfo(ushort[] pixels, int w, int h)
            {
                Pixels = pixels;
                Width = w;
                Height = h;
            }

            public readonly ushort[] Pixels;
            public readonly int Width, Height;
        }
    }
}