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
using ClassicUO.Renderer;
using ClassicUO.Game.Data;
using System.IO;

using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Utility;
using ClassicUO.IO.Resources;

using Microsoft.Xna.Framework;
using ClassicUO.Game.UI.Gumps;
using System.Linq;

namespace ClassicUO.Game.Managers
{
    internal static class ContainerManager
    {
        private static readonly Dictionary<ushort, ContainerData> _data = new Dictionary<ushort, ContainerData>
        {
            {
                0x0007, new ContainerData(0x0007, 0x0000, 0x0000, 30, 30, 270, 170)
            },
            {
                0x0009, new ContainerData(0x0009, 0x0000, 0x0000, 20, 85, 124, 196)
            },
            {
                0x003C, new ContainerData(0x003C, 0x0048, 0x0058, 44, 65, 186, 159)
            },
            {
                0x003D, new ContainerData(0x003D, 0x0048, 0x0058, 29, 34, 137, 128)
            },
            {
                0x003E, new ContainerData(0x003E, 0x002F, 0x002E, 33, 36, 142, 148)
            },
            {
                0x003F, new ContainerData(0x003F, 0x004F, 0x0058, 19, 47, 182, 123)
            },
            {
                0x0040, new ContainerData(0x0040, 0x002D, 0x002C, 16, 38, 152, 125)
            },
            {
                0x0041, new ContainerData(0x0041, 0x004F, 0x0058, 40, 30, 139, 123)
            },
            {
                0x0042, new ContainerData(0x0042, 0x002D, 0x002C, 18, 105, 162, 178)
            },
            {
                0x0043, new ContainerData(0x0043, 0x002D, 0x002C, 16, 51, 184, 124)
            },
            {
                0x0044, new ContainerData(0x0044, 0x002D, 0x002C, 20, 10, 170, 100)
            },
            {
                0x0047, new ContainerData(0x0047, 0x0000, 0x0000, 16, 10, 148, 138)
            },
            {
                0x0048, new ContainerData(0x0048, 0x002F, 0x002E, 16, 10, 154, 94)
            },
            {
                0x0049, new ContainerData(0x0049, 0x002D, 0x002C, 18, 105, 162, 178)
            },
            {
                0x004A, new ContainerData(0x004A, 0x002D, 0x002C, 18, 105, 162, 178)
            },
            {
                0x004B, new ContainerData(0x004B, 0x002D, 0x002C, 16, 51, 184, 124)
            },
            {
                0x004C, new ContainerData(0x004C, 0x002D, 0x002C, 46, 74, 196, 184)
            },
            {
                0x004D, new ContainerData(0x004D, 0x002F, 0x002E, 76, 12, 140, 68)
            },
            {
                0x004E, new ContainerData(0x004E, 0x002D, 0x002C, 24, 96, 140, 152)
            },
            {
                0x004F, new ContainerData(0x004F, 0x002D, 0x002C, 24, 96, 140, 152)
            },
            {
                0x0051, new ContainerData(0x0051, 0x002F, 0x002E, 16, 10, 154, 94)
            },
            {
                0x0052, new ContainerData(0x0052, 0x0000, 0x0000, 0, 0, 110, 62)
            },
            {
                0x0102, new ContainerData(0x0102, 0x004F, 0x0058, 35, 10, 190, 95)
            },
            {
                0x0103, new ContainerData(0x0103, 0x0048, 0x0058, 41, 21, 173, 104)
            },
            {
                0x0104, new ContainerData(0x0104, 0x002F, 0x002E, 10, 10, 160, 105)
            },
            {
                0x0105, new ContainerData(0x0105, 0x002F, 0x002E, 10, 10, 160, 105)
            },
            {
                0x0106, new ContainerData(0x0106, 0x002F, 0x002E, 10, 10, 160, 105)
            },
            {
                0x0107, new ContainerData(0x0107, 0x002F, 0x002E, 10, 10, 160, 105)
            },
            {
                0x0108, new ContainerData(0x0108, 0x004F, 0x0058, 10, 10, 160, 105)
            },
            {
                0x0109, new ContainerData(0x0109, 0x002D, 0x002C, 10, 10, 160, 105)
            },
            {
                0x010A, new ContainerData(0x010A, 0x002D, 0x002C, 10, 10, 160, 105)
            },
            {
                0x010B, new ContainerData(0x010B, 0x002D, 0x002C, 10, 10, 160, 105)
            },
            {
                0x010C, new ContainerData(0x010C, 0x002F, 0x002E, 10, 10, 160, 105)
            },
            {
                0x010D, new ContainerData(0x010D, 0x002F, 0x002E, 10, 10, 160, 105)
            },
            {
                0x010E, new ContainerData(0x010E, 0x002F, 0x002E, 10, 10, 160, 105)
            },
            {
                0x0116, new ContainerData(0x0116, 0x0000, 0x0000, 40, 25, 140, 110)
            },
            {
                0x011A, new ContainerData(0x011A, 0x0000, 0x0000, 10, 65, 125, 160)
            },
            {
                0x011B, new ContainerData(0x011B, 0x0000, 0x0000, 45, 10, 175, 95)
            },
            {
                0x011C, new ContainerData(0x011C, 0x0000, 0x0000, 37, 10, 175, 105)
            },
            {
                0x011D, new ContainerData(0x011D, 0x0000, 0x0000, 43, 10, 165, 110)
            },
            {
                0x011E, new ContainerData(0x011E, 0x0000, 0x0000, 30, 22, 263, 106)
            },
            {
                0x011F, new ContainerData(0x011F, 0x0000, 0x0000, 45, 10, 175, 95)
            },
            {
                0x0120, new ContainerData(0x0120, 0x0000, 0x0000, 56, 30, 160, 107)
            },
            {
                0x0121, new ContainerData(0x0121, 0x0000, 0x0000, 77, 32, 162, 107)
            },
            {
                0x0123, new ContainerData(0x0123, 0x0000, 0x0000, 36, 19, 111, 157)
            },
            {
                0x0484, new ContainerData(0x0484, 0x0000, 0x0000, 0, 45, 175, 125)
            },
            {
                0x058E, new ContainerData(0x058E, 0x0000, 0x0000, 50, 150, 348, 250)
            },
            {
                0x06D3, new ContainerData(0x06D3, 0x0000, 0x0000, 10, 65, 125, 160)
            },
            {
                0x06D4, new ContainerData(0x06D4, 0x0000, 0x0000, 10, 65, 125, 160)
            },
            {
                0x06D5, new ContainerData(0x06D5, 0x0000, 0x0000, 10, 65, 125, 160)
            },
            {
                0x06D6, new ContainerData(0x06D6, 0x0000, 0x0000, 10, 65, 125, 160)
            },
            {
                0x06E5, new ContainerData(0x06E5, 0x0000, 0x0000, 66, 74, 306, 520)
            },
            {
                0x06E6, new ContainerData(0x06E6, 0x0000, 0x0000, 66, 74, 306, 520)
            },
            {
                0x06E7, new ContainerData(0x06E7, 0x0000, 0x0000, 50, 60, 548, 308)
            },
            {
                0x06E8, new ContainerData(0x06E8, 0x0000, 0x0000, 50, 60, 548, 308)
            },
            {
                0x06E9, new ContainerData(0x06E9, 0x0000, 0x0000, 60, 80, 318, 324)
            },
            {
                0x06EA, new ContainerData(0x06EA, 0x0000, 0x0000, 50, 60, 548, 308)
            },
            {
                0x091A, new ContainerData(0x091A, 0x0000, 0x0000, 0, 0, 282, 230)
            },
            {
                0x092E, new ContainerData(0x092E, 0x0000, 0x0000, 0, 0, 282, 210)
            },
            {
                0x266A, new ContainerData(0x266A, 0x0000, 0x0000, 16, 51, 184, 124)
            },
            {
                0x266B, new ContainerData(0x266B, 0x0000, 0x0000, 16, 51, 184, 124)
            },
            {
                0x2A63, new ContainerData(0x2A63, 0x0187, 0x01C9, 60, 33, 460, 348)
            },
            {
                0x4D0C, new ContainerData(0x4D0C, 0x0000, 0x0000, 25, 65, 220, 155)
            },
            {
                0x777A, new ContainerData(0x777A, 0x0000, 0x0000, 32, 40, 184, 116)
            },
            {
                0x9CD9, new ContainerData(0x9CD9, 0x0000, 0x0000, 10, 10, 160, 105)
            },
            {
                0x9CDB, new ContainerData(0x9CDB, 0x0000, 0x0000, 50, 60, 548, 308)
            },
            {
                0x9CDD, new ContainerData(0x9CDD, 0x0000, 0x0000, 50, 60, 548, 308)
            },
            {
                0x9CDF, new ContainerData(0x9CDF, 0x0000, 0x0000, 50, 60, 548, 308)
            },
            {
                0x9CE3, new ContainerData(0x9CE3, 0x0000, 0x0000, 50, 60, 548, 308)
            },
            {
                0x9CE4, new ContainerData(0x9CE4, 0x0000, 0x0000, 44, 65, 186, 159)
            },
            {
                0x9CE5, new ContainerData(0x9CE5, 0x0000, 0x0000, 44, 65, 186, 159)
            },
            {
                0x9CE7, new ContainerData(0x9CE7, 0x0000, 0x0000, 44, 65, 186, 159)
            },

            //{
            //    0x2A63, new ContainerData(0x2A63, 0x0187, 0x01c9, 29, 34, 137, 128)//for this particular gump area is bugged also in original client, as it is similar to the bag, probably this is an unfinished one
            //}
        };

        static ContainerManager()
        {
            BuildContainerFile(false);
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


        public static void CalculateContainerPosition(uint serial, ushort g)
        {
            if (UIManager.GetGumpCachePosition(serial, out Point location))
            {
                X = location.X;
                Y = location.Y;
            }
            else
            {
                UOTexture texture = GumpsLoader.Instance.GetTexture(g);

                if (texture != null)
                {
                    float scale = UIManager.ContainerScale;

                    int width = (int) (texture.Width * scale);
                    int height = (int) (texture.Height * scale);

                    if (ProfileManager.Current.OverrideContainerLocation)
                    {
                        switch (ProfileManager.Current.OverrideContainerLocationSetting)
                        {
                            case 0:
                                SetPositionNearGameObject(g, serial, width, height);
                                break;
                            case 1:
                                X = Client.Game.Window.ClientBounds.Width - width;
                                Y = 0;
                                break;
                            case 2:
                            case 3:
                                X = ProfileManager.Current.OverrideContainerLocationPosition.X - (width >> 1);
                                Y = ProfileManager.Current.OverrideContainerLocationPosition.Y - (height >> 1);
                                break;
                        }

                        if ((X + width) > Client.Game.Window.ClientBounds.Width)
                        {
                            X -= width;
                        }

                        if ((Y + height) > Client.Game.Window.ClientBounds.Height)
                        {
                            Y -= height;
                        }
                    }
                    else
                    {
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
        }

        private static void SetPositionNearGameObject(ushort g, uint serial, int width, int height)
        {
            Item item = World.Items.Get(serial);
            if (item == null)
                return;

            Item bank = World.Player.FindItemByLayer(Layer.Bank);

            if (bank != null && serial == bank)
            {
                // open bank near player
                X = World.Player.RealScreenPosition.X + ProfileManager.Current.GameWindowPosition.X + 40;
                Y = World.Player.RealScreenPosition.Y + ProfileManager.Current.GameWindowPosition.Y - (height >> 1);
            }
            else if (item.OnGround)
            {
                // item is in world
                X = item.RealScreenPosition.X + ProfileManager.Current.GameWindowPosition.X + 40;
                Y = item.RealScreenPosition.Y + ProfileManager.Current.GameWindowPosition.Y - (height >> 1);
            }
            else if (SerialHelper.IsMobile(item.Container))
            {
                // pack animal, snooped player, npc vendor
                Mobile mobile = World.Mobiles.Get(item.Container);
                if (mobile != null)
                {
                    X = mobile.RealScreenPosition.X + ProfileManager.Current.GameWindowPosition.X + 40;
                    Y = mobile.RealScreenPosition.Y + ProfileManager.Current.GameWindowPosition.Y - (height >> 1);
                }
            }
            else
            {
                // in a container, open near the container
                ContainerGump parentContainer = UIManager.GetGump<ContainerGump>(item.Container);
                if (parentContainer != null)
                {
                    X = parentContainer.X + (width >> 1);
                    Y = parentContainer.Y;
                }
            }
        }

        public static void BuildContainerFile(bool force)
        {
            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            path = Path.Combine(path, "containers.txt");

            if (!File.Exists(path) || force)
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
    }
}
