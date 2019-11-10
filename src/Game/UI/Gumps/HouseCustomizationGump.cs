using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.IO;
using ClassicUO.Network;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    class HouseCustomizationGump : Gump
    {
        private readonly List<CustomHouseWallCategory> _walls = new List<CustomHouseWallCategory>();
        private readonly List<CustomHouseFloor> _floors = new List<CustomHouseFloor>();
        private readonly List<CustomHouseDoor> _doors = new List<CustomHouseDoor>();
        private readonly List<CustomHouseMiscCategory> _miscs = new List<CustomHouseMiscCategory>();
        private readonly List<CustomHouseStair> _stairs = new List<CustomHouseStair>();
        private readonly List<CustomHouseTeleport> _teleports = new List<CustomHouseTeleport>();
        private readonly List<CustomHouseRoofCategory> _roofs = new List<CustomHouseRoofCategory>();
        private readonly List<CustomHousePlaceInfo> _objectsInfo = new List<CustomHousePlaceInfo>();

        private readonly int[] _floorVisionState = new int[4];

        private DataBox _dataBox, _dataBoxGUI;
        private GumpPic _gumpPic;
        private Label _textComponents, _textFixtures, _textCost;

        enum ID_GUMP_CUSTOM_HOUSE
        {
            ID_GCH_STATE_WALL = 1,
            ID_GCH_STATE_DOOR,
            ID_GCH_STATE_FLOOR,
            ID_GCH_STATE_STAIR,
            ID_GCH_STATE_ROOF,
            ID_GCH_STATE_MISC,
            ID_GCH_STATE_ERASE,
            ID_GCH_STATE_EYEDROPPER,
            ID_GCH_STATE_MENU,

            ID_GCH_VISIBILITY_STORY_1,
            ID_GCH_VISIBILITY_STORY_2,
            ID_GCH_VISIBILITY_STORY_3,
            ID_GCH_VISIBILITY_STORY_4,

            ID_GCH_GO_FLOOR_1,
            ID_GCH_GO_FLOOR_2,
            ID_GCH_GO_FLOOR_3,
            ID_GCH_GO_FLOOR_4,

            ID_GCH_LIST_LEFT,
            ID_GCH_LIST_RIGHT,

            ID_GCH_MENU_BACKUP,
            ID_GCH_MENU_RESTORE,
            ID_GCH_MENU_SYNCH,
            ID_GCH_MENU_CLEAR,
            ID_GCH_MENU_COMMIT,
            ID_GCH_MENU_REVERT,

            ID_GCH_GO_CATEGORY,
            ID_GCH_WALL_SHOW_WINDOW,
            ID_GCH_ROOF_Z_UP,
            ID_GCH_ROOF_Z_DOWN,

            ID_GCH_AREA_OBJECTS_INFO,
            ID_GCH_AREA_COST_INFO,
            ID_GCH_AREA_ROOF_Z_INFO,

            ID_GCH_ITEM_IN_LIST
        }



        public HouseCustomizationGump(Serial serial, int x, int y) : base(serial, 0)
        {
            X = x;
            Y = y;
            CanMove = true;
            AcceptMouseInput = true;

            ParseFileWithCategory<CustomHouseWall, CustomHouseWallCategory>(_walls, Path.Combine(FileManager.UoFolderPath, "walls.txt"));
            ParseFile(_floors, Path.Combine(FileManager.UoFolderPath, "floors.txt"));
            ParseFile(_doors, Path.Combine(FileManager.UoFolderPath, "doors.txt"));
            ParseFileWithCategory<CustomHouseMisc, CustomHouseMiscCategory>(_miscs, Path.Combine(FileManager.UoFolderPath, "misc.txt"));
            ParseFile(_stairs, Path.Combine(FileManager.UoFolderPath, "stairs.txt"));
            ParseFile(_teleports, Path.Combine(FileManager.UoFolderPath, "teleprts.txt"));
            ParseFileWithCategory<CustomHouseRoof, CustomHouseRoofCategory>(_roofs, Path.Combine(FileManager.UoFolderPath, "roof.txt"));
            ParseFile(_objectsInfo, Path.Combine(FileManager.UoFolderPath, "suppinfo.txt"));


            Item foundation = World.Items.Get(serial);

            if (foundation != null)
            {
                MinHouseZ = foundation.Z + 7;

                MultiInfo multi = foundation.MultiInfo;

                if (multi != null)
                {
                    StartPos.X = foundation.X + multi.MinX;
                    StartPos.Y = foundation.Y + multi.MinY;
                    EndPos.X = foundation.X + multi.MaxX + 1;
                    EndPos.Y = foundation.Y + multi.MaxY + 1;
                }

                int width = Math.Abs(EndPos.X - StartPos.X);
                int height = Math.Abs(EndPos.Y - StartPos.Y);

                if (width > 13 || height > 13)
                {
                    FloorCount = 4;
                }
                else
                {
                    FloorCount = 3;
                }

                int componentsOnFloor = (width - 1) * (height - 1);

                MaxComponets = FloorCount * (componentsOnFloor + 2 * (width + height) - 4) -
                               (int) ((double) (FloorCount * componentsOnFloor) * -0.25) + 2 * width + 3 * height - 5;
                MaxFixtures = MaxComponets / 20;
            }

            Add(new GumpPicTiled(121, 36, 397, 120, 0x0E14));
            _dataBox = new DataBox(0, 0, 0,0);
            Add(_dataBox);

            Add(new GumpPic(0, 17, 0x55F0, 0));
            
            _gumpPic = new GumpPic(486, 17, (ushort) (FloorCount == 4 ? 
                                        0x55F2 : 0x55F9), 0);
            Add(_gumpPic);

            Add(new GumpPicTiled(153, 17, 333, 154, 0x55F1));


            Add(new Button(0, 0x5654, 0x5656, 0x5655)
            {
                X = 9, 
                Y = 41
            });
            Add(new Button(1, 0x5657, 0x5659, 0x5658)
            {
                X = 39,
                Y = 40
            });
            Add(new Button(2, 0x565A, 0x565C, 0x565B)
            {
                X = 70,
                Y = 40
            }); 
            Add(new Button(3, 0x565D, 0x565F, 0x565E)
            {
                X = 9,
                Y = 72
            });
            Add(new Button(4, 0x5788, 0x578A, 0x5789)
            {
                X = 39,
                Y = 72
            });
            Add(new Button(5, 0x5663, 0x5665, 0x5664)
            {
                X = 69,
                Y = 72
            });
            Add(new Button(6, 0x566C, 0x566E, 0x566D)
            {
                X = 69,
                Y = 100
            });

            _textComponents = new Label(string.Empty, false,
                                        0x0481)
            {
                X = 82, Y = 142
            };
            Add(_textComponents);

            Label text = new Label(":", false, 0x0481, font: 9)
            {
                X = 84, Y = 184
            };
            Add(text);

            _textFixtures = new Label(string.Empty, false,
                                        0x0481)
            {
                X = 94,
                Y = 142
            };
            Add(_textFixtures);

            _textCost = new Label(string.Empty, false,
                                      0x0481)
            {
                X = 524,
                Y = 142
            };
            Add(_textCost);

            HitBox box = new HitBox(36, 137, 84, 23);
            Add(box);

            box = new HitBox(522, 137, 84, 23);
            Add(box);

            _dataBoxGUI = new DataBox(0,0 ,0,0);
            Add(_dataBoxGUI);
        }

        public int Category = -1,
                   MaxPage = 1,
                   CurrentFloor = 1,
                   FloorCount = 4,
                   RoofZ = 1,
                   MinHouseZ = -120,
                   Components,
                   Fixtures,
                   MaxComponets,
                   MaxFixtures;
        public ushort SelectedGraphic;
        public bool Erasing, SeekTile, ShowWindow, CombinedStair;
        public Point StartPos, EndPos;



        private void DrawWall()
        {
            int x = 0, y = 0;

            if (Category == -1)
            {
                int startCategory = Page * 16;
                int endCategory = startCategory + 16;

                if (endCategory > _walls.Count)
                    endCategory = _walls.Count;

                // TODO: scissor

                for (int i = startCategory; i < endCategory; i++)
                {
                    var vec = _walls[i].Items;

                    if (vec.Count == 0)
                        continue;

                    Rectangle bounds = FileManager.Art.GetTexture((ushort) vec[0].East1).Bounds;

                    int offsetX = x + 121 + (48 - bounds.Width) / 2;
                    int offsetY = y + 36;

                    _dataBox.Add(new StaticPic((ushort) vec[0].East1,0)
                    {
                        X = offsetX, Y = offsetY
                    });

                }
                
            }
        }





        public override void OnButtonClick(int buttonID)
        {
            
        }


        public override void Dispose()
        {
            NetClient.Socket.Send(new PCustomHouseBuildingExit());
            base.Dispose();
        }


        private static void ParseFile<T>(List<T> list, string path) where  T: CustomHouseObject, new()
        {
            FileInfo file = new FileInfo(path);
            if (!file.Exists)
                return;

            using (StreamReader reader = File.OpenText(file.FullName))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    T item = new T();

                    if (item.Parse(line))
                    {
                        if (item.FeatureMask == 0 || ((int) World.ClientLockedFeatures.Flags & item.FeatureMask) != 0)
                        {
                            list.Add(item);
                        }
                    }
                    
                }
            }
        }

        private static void ParseFileWithCategory<T, U>(List<U> list, string path) 
            where T : CustomHouseObject, new()
            where U : CustomHouseObjectCategory<T>, new()
        {
            FileInfo file = new FileInfo(path);
            if (!file.Exists)
                return;

            using (StreamReader reader = File.OpenText(file.FullName))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    T item = new T();

                    if (item.Parse(line))
                    {
                        if (item.FeatureMask != 0 && ((int) World.ClientLockedFeatures.Flags & item.FeatureMask) == 0)
                            continue;

                        bool found = false;

                        foreach (var c in list)
                        {
                            if (c.Index == item.Category)
                            {
                                c.Items.Add(item);
                                found = true;
                                break;
                            }
                        }


                        if (!found)
                        {
                            U c = new U
                            {
                                Index = item.Category
                            };
                            c.Items.Add(item);
                            list.Add(c);
                        }
                    }
                }
            }
        }
    }
}
