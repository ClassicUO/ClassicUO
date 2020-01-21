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

using System.Collections.Generic;

using ClassicUO.IO;
using ClassicUO.Renderer;
using ClassicUO.Game.UI.Controls;

using Microsoft.Xna.Framework;
using ClassicUO.Game.Data;
using System.IO;
using ClassicUO.Utility;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.Managers
{
    internal static class ContainerManager
    {
        private static readonly Dictionary<ushort, ContainerData> _data = new Dictionary<ushort, ContainerData>
        {
            {
                0x9, new ContainerData(0x0009, 0x0000, 0x0000, 20, 85, 124, 196)
            },
            {
                0x3C, new ContainerData(0x003C, 0x0048, 0x0058, 44, 65, 186, 159, 0x50, 105, 162)
            },
            {
                0x3D, new ContainerData(0x003D, 0x0048, 0x0058, 29, 34, 137, 128)
            },
            {
                0x3E, new ContainerData(0x003E, 0x002F, 0x002E, 33, 36, 142, 148)
            },
            {
                0x3F, new ContainerData(0x003F, 0x004F, 0x0058, 19, 47, 182, 123)
            },
            {
                0x40, new ContainerData(0x0040, 0x002D, 0x002C, 16, 51, 150, 140)
            },
            {
                0x41, new ContainerData(0x0041, 0x004F, 0x0058, 35, 38, 145, 116)
            },
            {
                0x42, new ContainerData(0x0042, 0x002D, 0x002C, 18, 105, 162, 178)
            },
            {
                0x43, new ContainerData(0x0043, 0x002D, 0x002C, 16, 51, 181, 124)
            },
            {
                0x44, new ContainerData(0x0044, 0x002D, 0x002C, 20, 10, 170, 100)
            },
            {
                0x48, new ContainerData(0x0048, 0x002F, 0x002E, 16, 10, 154, 94)
            },
            {
                0x49, new ContainerData(0x0049, 0x002D, 0x002C, 18, 105, 162, 178)
            },
            {
                0x4A, new ContainerData(0x004A, 0x002D, 0x002C, 18, 105, 162, 178)
            },
            {
                0x4B, new ContainerData(0x004B, 0x002D, 0x002C, 16, 51, 184, 124)
            },
            {
                0x4C, new ContainerData(0x004C, 0x002D, 0x002C, 46, 74, 196, 184)
            },
            {
                0x4D, new ContainerData(0x004D, 0x002F, 0x002E, 76, 12, 140, 68)
            },
            {
                0x4E, new ContainerData(0x004E, 0x002D, 0x002C, 24, 18, 100, 152)
            },
            {
                0x4F, new ContainerData(0x004F, 0x002D, 0x002C, 24, 18, 100, 152)
            },
            {
                0x51, new ContainerData(0x0051, 0x002F, 0x002E, 16, 10, 154, 94)
            },
            {
                0x91A, new ContainerData(0x091A, 0x0000, 0x0000, 1, 13, 260, 199)
            },
            {
                0x92E, new ContainerData(0x092E, 0x0000, 0x0000, 1, 13, 260, 199)
            },
            {
                0x103, new ContainerData(0x0103, 0x0048, 0x0058, 29, 34, 137, 128)
            },
            {
                0x104, new ContainerData(0x0104, 0x002F, 0x002E, 0, 20, 168, 115)
            },
            {
                0x105, new ContainerData(0x0105, 0x002F, 0x002E, 0, 20, 168, 115)
            },
            {
                0x106, new ContainerData(0x0106, 0x002F, 0x002E, 0, 20, 168, 115)
            },
            {
                0x107, new ContainerData(0x0107, 0x002F, 0x002E, 0, 20, 168, 115)
            },
            {
                0x108, new ContainerData(0x0108, 0x004F, 0x0058, 0, 35, 150, 105)
            },
            {
                0x109, new ContainerData(0x0109, 0x002F, 0x002E, 0, 20, 175, 105)
            },
            {
                0x10A, new ContainerData(0x010A, 0x002F, 0x002E, 0, 20, 175, 105)
            },
            {
                0x10B, new ContainerData(0x010B, 0x002F, 0x002E, 0, 20, 175, 105)
            },
            {
                0x10C, new ContainerData(0x010C, 0x002F, 0x002E, 0, 20, 168, 115)
            },
            {
                0x10D, new ContainerData(0x010D, 0x002F, 0x002E, 0, 20, 168, 115)
            },
            {
                0x10E, new ContainerData(0x010E, 0x002F, 0x002E, 0, 20, 168, 115)
            },
            {
                0x102, new ContainerData(0x0102, 0x004F, 0x0058, 15, 10, 210, 110)
            },
            {
                0x11B, new ContainerData(0x011B, 0x004F, 0x0058, 15, 10, 220, 120)
            },
            {
                0x11C, new ContainerData(0x011C, 0x004F, 0x0058, 10, 10, 220, 145)
            },
            {
                0x11D, new ContainerData(0x011D, 0x004F, 0x0058, 10, 10, 220, 130)
            },
            {
                0x11E, new ContainerData(0x011E, 0x004F, 0x0058, 15, 10, 290, 130)
            },
            {
                0x11F, new ContainerData(0x011F, 0x004F, 0x0058, 15, 10, 220, 120)
            },
            {
                0x58E, new ContainerData(0x058E, 0x002D, 0x002C, 16, 51, 184, 124)
            },
            {
                0x484, new ContainerData(0x0484, 0x064F, 0x0000, 5, 43, 160, 100)
            },
            {
                0x2A63, new ContainerData(0x2A63, 0x0187, 0x01c9, 29, 34, 137, 128)//for this particular gump area is bugged also in original client, as it is similar to the bag, probably this is an unfinished one
            }
        };

        static ContainerManager()
        {
            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            path = Path.Combine(path, "containers.txt");

            if (!File.Exists(path))
            {
                using (StreamWriter writer = new StreamWriter(File.Create(path)))
                {
                    writer.WriteLine("# FORMAT");
                    writer.WriteLine("# GRAPHIC OPEN_SOUND_ID CLOSE_SOUND_ID LEFT TOP RIGHT BOTTOM ICONIZED_GRAPHIC [0 if not exists] MINIMIZER_AREA_X [0 if not exists] MINIMIZER_AREA_Y [0 if not exists]");
                    writer.WriteLine("# LEFT = X,  TOP = Y,  RIGHT = X + WIDTH,  BOTTOM = Y + HEIGHT");
                    writer.WriteLine();
                    writer.WriteLine();

                    foreach (var e in _data)
                    {
                        writer.WriteLine($"{e.Value.Graphic} {e.Value.OpenSound} {e.Value.ClosedSound} {e.Value.Bounds.X} {e.Value.Bounds.Y} {e.Value.Bounds.Width} {e.Value.Bounds.Height} {e.Value.IconizedGraphic} {e.Value.MinimizerArea.X} {e.Value.MinimizerArea.Y}");
                    }
                }
            }

            _data.Clear();

            TextFileParser containersParser = new TextFileParser(File.ReadAllText(path), new[] { ' ', '\t', ',' }, new[] { '#', ';' }, new[] { '"', '"' });

            while (!containersParser.IsEOF())
            {
                var ss = containersParser.ReadTokens();
                if (ss != null && ss.Count != 0)
                {
                    if (ushort.TryParse(ss[0], out ushort graphic) && 
                        ushort.TryParse(ss[1], out ushort open_sound_id) &&
                        ushort.TryParse(ss[2], out ushort close_sound_id) &&
                        int.TryParse(ss[3], out int x) && 
                        int.TryParse(ss[4], out int y) &&
                        int.TryParse(ss[5], out int w) && 
                        int.TryParse(ss[6], out int h))
                    {
                        ushort iconized_graphic = 0;
                        int minimizer_x = 0, minimizer_y = 0;

                        if (ss.Count >= 8 && ushort.TryParse(ss[7], out iconized_graphic))
                        {
                            if (ss.Count >= 9 && int.TryParse(ss[8], out minimizer_x))
                            {
                                if (ss.Count >= 10 && int.TryParse(ss[9], out minimizer_y))
                                {
                                    // nice!
                                }
                            }
                        }

                        _data[graphic] = new ContainerData(graphic, open_sound_id, close_sound_id, x, y, w, h, iconized_graphic, minimizer_x, minimizer_y);
                    }
                }
            }
        }


        public static int DefaultX { get; } = 40;
        public static int DefaultY { get; } = 40;

        public static int X { get; private set; } = 40;
        public static int Y { get; private set; } = 40;

        public static ContainerData Get(ushort graphic)
        {
            //if the server requests for a non present gump in container data dictionary, create it, but without any particular sound.
            if (!_data.TryGetValue(graphic, out ContainerData value))
                _data[graphic] = value = new ContainerData(graphic, 0, 0, 44, 65, 186, 159);
            return value;
        }


        public static void CalculateContainerPosition(ushort g)
        {
            UOTexture texture = GumpsLoader.Instance.GetTexture(g);

            int passed = 0;

            for (int i = 0; i < 4 && passed == 0; i++)
            {
                if (X + texture.Width + Constants.CONTAINER_RECT_STEP > Client.Game.Window.ClientBounds.Width)
                {
                    X = Constants.CONTAINER_RECT_DEFAULT_POSITION;

                    if (Y + texture.Height + Constants.CONTAINER_RECT_LINESTEP > Client.Game.Window.ClientBounds.Height)
                        Y = Constants.CONTAINER_RECT_DEFAULT_POSITION;
                    else
                        Y += Constants.CONTAINER_RECT_LINESTEP;
                }
                else if (Y + texture.Height + Constants.CONTAINER_RECT_STEP > Client.Game.Window.ClientBounds.Height)
                {
                    if (X + texture.Width + Constants.CONTAINER_RECT_LINESTEP > Client.Game.Window.ClientBounds.Width)
                        X = Constants.CONTAINER_RECT_DEFAULT_POSITION;
                    else
                        X += Constants.CONTAINER_RECT_LINESTEP;

                    Y = Constants.CONTAINER_RECT_DEFAULT_POSITION;
                }
                else
                    passed = i + 1;
            }

            if (passed == 0)
            {
                X = DefaultX;
                Y = DefaultY;
            }
            else if (passed == 1)
            {
                X += Constants.CONTAINER_RECT_STEP;
                Y += Constants.CONTAINER_RECT_STEP;
            }
        }
    }
}
