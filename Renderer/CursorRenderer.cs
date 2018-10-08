#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Gumps;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    public sealed class CursorRenderer
    {
        private static readonly ushort[,] _cursorData = new ushort[2, 16]
        {
            {
                0x206A, 0x206B, 0x206C, 0x206D, 0x206E, 0x206F, 0x2070, 0x2071, 0x2072, 0x2073, 0x2074, 0x2075, 0x2076,
                0x2077, 0x2078, 0x2079
            },
            {
                0x2053, 0x2054, 0x2055, 0x2056, 0x2057, 0x2058, 0x2059, 0x205A, 0x205B, 0x205C, 0x205D, 0x205E, 0x205F,
                0x2060, 0x2061, 0x2062
            }
        };

        private readonly int[,] _cursorOffset = new int[2, 16];

        private Texture2D _blackTexture;
        private Graphic _graphic = 0x2073;
        private bool _needGraphicUpdate;
        private readonly InputManager _inputManager;
        private readonly UIManager _uiManager;
        private Settings _settings;

        public CursorRenderer(UIManager ui)
        {
            _inputManager = Service.Get<InputManager>();
            _uiManager = ui;

            _settings = Service.Get<Settings>();

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    ushort id = _cursorData[i, j];

                    Texture2D texture = Art.GetStaticTexture(id);


                    if (i == 0)
                    {
                        if (texture != null)
                        {
                            float offX = 0;
                            float offY = 0;

                            float dw = texture.Width;
                            float dh = texture.Height;

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

                            _cursorOffset[0, j] = (int) offX;
                            _cursorOffset[1, j] = (int) offY;
                        }
                        else
                        {
                            _cursorOffset[0, j] = 0;
                            _cursorOffset[1, j] = 0;
                        }
                    }
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

        public SpriteTexture Texture { get; private set; }
        public Point ScreenPosition => _inputManager.MousePosition;


        public void Update(double totalMS, double frameMS)
        {
            Graphic = AssignGraphicByState();

            if (Texture == null || Texture.IsDisposed || _needGraphicUpdate)
            {
                Texture = Art.GetStaticTexture(Graphic);

                //_blackTexture = new Texture2D(Service.GetByLocalSerial<SpriteBatch3D>().GraphicsDevice, 1, 1);
                //_blackTexture.SetData(new[] { Color.Black });


                _needGraphicUpdate = false;
            }
            else
                Texture.Ticks = (long)totalMS;
        }

        public void Draw(SpriteBatchUI sb)
        {
            ushort id = Graphic;

            if (id < 0x206A)
                id -= 0x2053;
            else
                id -= 0x206A;

            if (id < 16)
            {
                Vector3 v = new Vector3(ScreenPosition.X + _cursorOffset[0, id],
                    ScreenPosition.Y + _cursorOffset[1, id], 0);
                sb.Draw2D(Texture, v, Vector3.Zero);


                // tooltip testing, very nice!
                //sb.Draw2D(_blackTexture, new Rectangle(ScreenPosition.X + _cursorOffset[0, id] - 100, ScreenPosition.Y + _cursorOffset[1, id] - 50, 100, 50), RenderExtentions.GetHueVector(0, false, true, false));
            }
        }


        private ushort AssignGraphicByState()
        {
            int war = World.InGame && World.Player.InWarMode ? 1 : 0;
            ushort result = _cursorData[war, 9];

            if (!_uiManager.IsMouseOverWorld)
                return result;

            int windowCenterX = _settings.GameWindowX + _settings.GameWindowWidth / 2;
            int windowCenterY = _settings.GameWindowY + _settings.GameWindowHeight / 2;

            return _cursorData[war,
                GetMouseDirection(windowCenterX, windowCenterY, ScreenPosition.X, ScreenPosition.Y, 1)];
        }


        private int GetMouseDirection(int x1, int y1, int to_x, int to_y, int current_facing)
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
            else if (shiftX <= 0)
            {
                if (shiftY <= 0)
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
                    return (int) Direction.Right; // N
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

        private int Sgn(int val)
        {
            int a = 0 < val ? 1 : 0;
            int b = val < 0 ? 1 : 0;
            return a - b;
        }
    }
}