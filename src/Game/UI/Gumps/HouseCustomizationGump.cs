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
using ClassicUO.IO.Resources;
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
            AcceptMouseInput = false;

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
                               (int)((double)(FloorCount * componentsOnFloor) * -0.25) + 2 * width + 3 * height - 5;
                MaxFixtures = MaxComponets / 20;
            }

            Add(new GumpPicTiled(121, 36, 397, 120, 0x0E14));
            _dataBox = new DataBox(0, 0, 0, 0)
            {
                WantUpdateSize = true,
                CanMove = false,
                AcceptMouseInput = false
            };
            Add(_dataBox);

            Add(new GumpPic(0, 17, 0x55F0, 0));

            _gumpPic = new GumpPic(486, 17, (ushort)(FloorCount == 4 ?
                                        0x55F2 : 0x55F9), 0);
            Add(_gumpPic);

            Add(new GumpPicTiled(153, 17, 333, 154, 0x55F1));


            Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_WALL, 0x5654, 0x5656, 0x5655)
            {
                X = 9,
                Y = 41,
                ButtonAction = ButtonAction.Activate
            });
            Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_DOOR, 0x5657, 0x5659, 0x5658)
            {
                X = 39,
                Y = 40,
                ButtonAction = ButtonAction.Activate
            });
            Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_FLOOR, 0x565A, 0x565C, 0x565B)
            {
                X = 70,
                Y = 40,
                ButtonAction = ButtonAction.Activate
            });
            Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_STAIR, 0x565D, 0x565F, 0x565E)
            {
                X = 9,
                Y = 72,
                ButtonAction = ButtonAction.Activate
            });
            Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_ROOF, 0x5788, 0x578A, 0x5789)
            {
                X = 39,
                Y = 72,
                ButtonAction = ButtonAction.Activate
            });
            Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_MISC, 0x5663, 0x5665, 0x5664)
            {
                X = 69,
                Y = 72,
                ButtonAction = ButtonAction.Activate
            });
            Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_MENU, 0x566C, 0x566E, 0x566D)
            {
                X = 69,
                Y = 100,
                ButtonAction = ButtonAction.Activate
            });

            _textComponents = new Label(string.Empty, false,
                                        0x0481)
            {
                X = 82,
                Y = 142
            };
            Add(_textComponents);

            Label text = new Label(":", false, 0x0481, font: 9)
            {
                X = 84,
                Y = 142
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

            HitBox box = new HitBox(36, 137, 84, 23)
            {
                Priority = ClickPriority.Default
            };
            Add(box);

            box = new HitBox(522, 137, 84, 23)
            {
                Priority = ClickPriority.Default
            };
            Add(box);

            _dataBoxGUI = new DataBox(0, 0, 0, 0)
            {
                WantUpdateSize = true,
                CanMove = false,
                AcceptMouseInput = false
            };
            Add(_dataBoxGUI);


            UpdateMaxPage();
            Update();
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
        public CUSTOM_HOUSE_GUMP_STATE State = CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL;


        private void Update()
        {
            _dataBox.Clear();
            _dataBoxGUI.Clear();

            _gumpPic.Graphic = (ushort)(FloorCount == 4 ? 0x55F2 : 0x55F9);

            _dataBoxGUI.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_ERASE, (ushort)(0x5666 + (Erasing ? 1 : 0)), 0x5668, 0x5667)
            {
                X = 9,
                Y = 100,
                ButtonAction = ButtonAction.Activate
            });

            Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_EYEDROPPER, (ushort)(0x5669 + (SeekTile ? 1 : 0)), 0x566B, 0x566A)
            {
                X = 39,
                Y = 100,
                ButtonAction = ButtonAction.Activate
            });

            ushort[] floorVisionGraphic1 = { 0x572E, 0x5734, 0x5731 };
            ushort[] floorVisionGraphic2 = { 0x5725, 0x5728, 0x572B };
            ushort[] floorVisionGraphic3 = { 0x571C, 0x571F, 0x5722 };
            int[] associateGraphicTable =
            {
                0, 1, 2, 1, 2, 1, 2
            };

            ushort floorVisionGraphic = floorVisionGraphic1[associateGraphicTable[_floorVisionState[0]]];
            int graphicOffset = CurrentFloor == 1 ? 3 : 0;
            int graphicOffset2 = CurrentFloor == 1 ? 4 : 0;
            _dataBoxGUI.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_1, floorVisionGraphic,
                (ushort)(floorVisionGraphic + 2), (ushort)(floorVisionGraphic + 1))
            {
                X = 533,
                Y = 108,
                ButtonAction = ButtonAction.Activate
            });
            _dataBoxGUI.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_1, (ushort)(0x56CD + graphicOffset2),
                0x56D1, (ushort)(0x56CD + graphicOffset2))
            {
                X = 583,
                Y = 96,
                ButtonAction = ButtonAction.Activate
            });
            _dataBoxGUI.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_1, (ushort)(0x56F6 + graphicOffset),
                (ushort)(0x56F8 + graphicOffset), (ushort)(0x56F7 + graphicOffset))
            {
                X = 623,
                Y = 103,
                ButtonAction = ButtonAction.Activate
            });


            floorVisionGraphic = floorVisionGraphic2[associateGraphicTable[_floorVisionState[1]]];
            graphicOffset = CurrentFloor == 2 ? 3 : 0;
            graphicOffset2 = CurrentFloor == 2 ? 4 : 0;
            _dataBoxGUI.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_2, floorVisionGraphic,
                (ushort)(floorVisionGraphic + 2), (ushort)(floorVisionGraphic + 1))
            {
                X = 533,
                Y = 86,
                ButtonAction = ButtonAction.Activate
            });
            _dataBoxGUI.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_2, (ushort)(0x56CE + graphicOffset2),
                0x56D2, (ushort)(0x56CE + graphicOffset2))
            {
                X = 583,
                Y = 73,
                ButtonAction = ButtonAction.Activate
            });
            _dataBoxGUI.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_2, (ushort)(0x56F0 + graphicOffset),
                (ushort)(0x56F2 + graphicOffset), (ushort)(0x56F1 + graphicOffset))
            {
                X = 623,
                Y = 86,
                ButtonAction = ButtonAction.Activate
            });


            graphicOffset = CurrentFloor == 3 ? 3 : 0;
            graphicOffset2 = CurrentFloor == 3 ? 4 : 0;
            if (FloorCount == 4)
            {
                floorVisionGraphic = floorVisionGraphic2[associateGraphicTable[_floorVisionState[2]]];
                _dataBoxGUI.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_3, floorVisionGraphic,
                    (ushort)(floorVisionGraphic + 2), (ushort)(floorVisionGraphic + 1))
                {
                    X = 533,
                    Y = 64,
                    ButtonAction = ButtonAction.Activate
                });
                _dataBoxGUI.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_3, (ushort)(0x56CE + graphicOffset2),
                    0x56D2, (ushort)(0x56CE + graphicOffset2))
                {
                    X = 582,
                    Y = 56,
                    ButtonAction = ButtonAction.Activate
                });
                _dataBoxGUI.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_3, (ushort)(0x56F0 + graphicOffset),
                    (ushort)(0x56F2 + graphicOffset), (ushort)(0x56F1 + graphicOffset))
                {
                    X = 623,
                    Y = 69,
                    ButtonAction = ButtonAction.Activate
                });



                floorVisionGraphic = floorVisionGraphic2[associateGraphicTable[_floorVisionState[3]]];
                graphicOffset = CurrentFloor == 4 ? 3 : 0;
                graphicOffset2 = CurrentFloor == 4 ? 4 : 0;
                _dataBoxGUI.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_4, floorVisionGraphic,
                    (ushort)(floorVisionGraphic + 2), (ushort)(floorVisionGraphic + 1))
                {
                    X = 533,
                    Y = 42,
                    ButtonAction = ButtonAction.Activate
                });
                _dataBoxGUI.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_4, (ushort)(0x56D0 + graphicOffset2),
                    0x56D4, (ushort)(0x56D0 + graphicOffset2))
                {
                    X = 583,
                    Y = 42,
                    ButtonAction = ButtonAction.Activate
                });
                _dataBoxGUI.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_4, (ushort)(0x56EA + graphicOffset),
                    (ushort)(0x56EC + graphicOffset), (ushort)(0x56EB + graphicOffset))
                {
                    X = 623,
                    Y = 50,
                    ButtonAction = ButtonAction.Activate
                });
            }
            else
            {
                floorVisionGraphic = floorVisionGraphic2[associateGraphicTable[_floorVisionState[2]]];
                _dataBoxGUI.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_3, floorVisionGraphic,
                    (ushort)(floorVisionGraphic + 2), (ushort)(floorVisionGraphic + 1))
                {
                    X = 533,
                    Y = 64,
                    ButtonAction = ButtonAction.Activate
                });
                _dataBoxGUI.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_3, (ushort)(0x56D0 + graphicOffset2),
                    0x56D4, (ushort)(0x56D0 + graphicOffset2))
                {
                    X = 582,
                    Y = 56,
                    ButtonAction = ButtonAction.Activate
                });
                _dataBoxGUI.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_3, (ushort)(0x56EA + graphicOffset),
                    (ushort)(0x56EC + graphicOffset), (ushort)(0x56EB + graphicOffset))
                {
                    X = 623,
                    Y = 69,
                    ButtonAction = ButtonAction.Activate
                });
            }

            switch (State)
            {
                case CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL:
                    AddWall();
                    break;
                case CUSTOM_HOUSE_GUMP_STATE.CHGS_DOOR:
                    AddDoor();
                    break;
                case CUSTOM_HOUSE_GUMP_STATE.CHGS_FLOOR:
                    AddFloor();
                    break;
                case CUSTOM_HOUSE_GUMP_STATE.CHGS_STAIR:
                    AddStair();
                    break;
                case CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF:
                    AddRoof();
                    break;
                case CUSTOM_HOUSE_GUMP_STATE.CHGS_MISC:
                    AddMisc();
                    break;
                case CUSTOM_HOUSE_GUMP_STATE.CHGS_MENU:
                    AddMenu();
                    break;
            }

            if (MaxPage > 1)
            {
                _dataBoxGUI.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_LIST_LEFT, 0x5625, 0x5627, 0x5626)
                {
                    X = 110,
                    Y = 63,
                    ButtonAction = ButtonAction.Activate
                });
                _dataBoxGUI.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_LIST_RIGHT, 0x5628, 0x562A, 0x5629)
                {
                    X = 510,
                    Y = 63,
                    ButtonAction = ButtonAction.Activate
                });
            }

            Components = 0;
            Fixtures = 0;

            Item foundationItem = World.Items.Get(LocalSerial);

            if (foundationItem != null)
            {
                if (World.HouseManager.TryGetHouse(LocalSerial, out var house))
                {
                    foreach (Multi item in house.Components)
                    {
                        if (house.IsCustom &&
                            (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL) == 0)
                        {
                            CUSTOM_HOUSE_GUMP_STATE state = 0;

                            (int res1, int res2) = ExistsInList(ref state, item.Graphic);

                            if (res1 != -1 && res2 != -1)
                            {
                                if (state == CUSTOM_HOUSE_GUMP_STATE.CHGS_DOOR)
                                {
                                    Fixtures++;
                                }
                                else
                                {
                                    Components++;
                                }
                            }
                        }
                    }
                }
            }


            _textComponents.Hue = (ushort)(Components >= MaxComponets ? 0x0026 : 0x0481);
            _textComponents.Text = Components.ToString();
            _textComponents.X = 82 - _textComponents.Width;

            _textFixtures.Hue = (ushort)(Fixtures >= MaxFixtures ? 0x0026 : 0x0481);
            _textFixtures.Text = Fixtures.ToString();

            _textCost.Text = ((Components + Fixtures) * 500).ToString();

        }

        public (int, int) ExistsInList(ref CUSTOM_HOUSE_GUMP_STATE state, ushort graphic)
        {
            (int res1, int res2) =
                SeekGraphicInCustomHouseObjectListWithCategory<CustomHouseWall,
                    CustomHouseWallCategory>(_walls, graphic);

            if (res1 == -1 || res2 == -1)
            {
                (res1, res2) = SeekGraphicInCustomHouseObjectList(_floors, graphic);

                if (res1 == -1 || res2 == -1)
                {
                    (res1, res2) = SeekGraphicInCustomHouseObjectList(_doors, graphic);

                    if (res1 == -1 || res2 == -1)
                    {
                        (res1, res2) =
                            SeekGraphicInCustomHouseObjectListWithCategory<CustomHouseMisc, CustomHouseMiscCategory>(
                                _miscs, graphic);

                        if (res1 == -1 || res2 == -1)
                        {
                            (res1, res2) = SeekGraphicInCustomHouseObjectList(_stairs, graphic);

                            if (res1 == -1 || res2 == -1)
                            {
                                (res1, res2) =
                                    SeekGraphicInCustomHouseObjectListWithCategory<CustomHouseRoof,
                                        CustomHouseRoofCategory>(_roofs, graphic);

                                if (res1 != -1 && res2 != -1)
                                {
                                    state = CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF;
                                }
                            }
                            else
                            {
                                state = CUSTOM_HOUSE_GUMP_STATE.CHGS_STAIR;
                            }
                        }
                        else
                        {
                            state = CUSTOM_HOUSE_GUMP_STATE.CHGS_MISC;
                        }
                    }
                    else
                    {
                        state = CUSTOM_HOUSE_GUMP_STATE.CHGS_DOOR;
                    }
                }
                else
                {
                    state = CUSTOM_HOUSE_GUMP_STATE.CHGS_FLOOR;
                }
            }
            else
            {
                state = CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL;
            }

            return (res1, res2);
        }

        public bool ValidatePlaceStructure(Item foundationItem, House house, int minZ, int maxZ, int flags)
        {
            if (house == null)
                return false;


            foreach (Multi item in house.Components)
            {
                List<Point> validatedFloors = new List<Point>();

                if (house.IsCustom && (item.State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR | 
                                                     CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR |
                                                     CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF | 
                                                     CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FIXTURE)) == 0 && 
                                       item.Z >= minZ && item.Z < maxZ)
                {
                    (int info1, int info2) = SeekGraphicInCustomHouseObjectList(_objectsInfo, item.Graphic);

                    if (info1 != -1 && info2 != -1)
                    {
                        var info = _objectsInfo[info1];

                        if ((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_DIRECT_SUPPORT) != 0)
                        {
                            if ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) != 0 ||
                                info.DirectSupports == 0)
                            {
                                continue;
                            }

                            if ((flags & (int)CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_CANGO_W) != 0)
                            {
                                if (info.CanGoW != 0)
                                    return true;
                            }
                            else if ((flags & (int)CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_CANGO_N) != 0)
                            {
                                if (info.CanGoN != 0)
                                    return true;
                            }
                            else
                                return true;
                        }
                        else if (
                            (((flags & (int)CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_BOTTOM) != 0) && info.Bottom != 0) ||
                            (((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_TOP)!= 0) && info.Top != 0)
                        )
                        {
                            if ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) == 0)
                            {
                                if (!ValidateItemPlace(foundationItem, item, minZ, maxZ, validatedFloors))
                                {
                                    item.State = item.State | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE |
                                                 CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE;
                                }
                                else
                                {
                                    item.State = item.State | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE;
                                }
                            }

                            if ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0)
                            {
                                if ((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_BOTTOM) != 0)
                                {
                                    if (((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_N) != 0) && (info.AdjUN != 0))
                                    {
                                        return true;
                                    }
                                    if (((flags & (int)CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_E) != 0) && (info.AdjUE != 0))
                                    {
                                        return true;
                                    }
                                    if (((flags & (int)CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_S) != 0) && (info.AdjUS != 0))
                                    {
                                        return true;
                                    }
                                    if (((flags & (int)CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_W) != 0) && (info.AdjUW != 0))
                                    {
                                        return true;
                                    }
                                }
                                else
                                {
                                    if (((flags & (int)CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_N) != 0) && (info.AdjLN != 0))
                                    {
                                        return true;
                                    }
                                    if (((flags & (int)CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_E) != 0) && (info.AdjLE != 0))
                                    {
                                        return true;
                                    }
                                    if (((flags & (int)CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_S) != 0) && (info.AdjLS != 0))
                                    {
                                        return true;
                                    }
                                    if (((flags & (int)CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_W) != 0) && (info.AdjLW != 0))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static (int, int) SeekGraphicInCustomHouseObjectListWithCategory<T, U>(List<U> list, ushort graphic)
            where T : CustomHouseObject
            where U : CustomHouseObjectCategory<T>
        {
            for (int i = 0; i < list.Count; i++)
            {
                U c = list[i];

                for (int j = 0; j < c.Items.Count; j++)
                {
                    int contains = c.Items[j].Contains(graphic);

                    if (contains != -1)
                        return (i, j);
                }
            }

            return (-1, -1);
        }

        private static (int, int) SeekGraphicInCustomHouseObjectList<T>(List<T> list, ushort graphic)
            where T : CustomHouseObject
        {
            for (int i = 0; i < list.Count; i++)
            {
                int contains = list[i].Contains(graphic);
                if (contains != -1)
                    return (i, graphic);
            }
            return (-1, -1);
        }

        private void UpdateMaxPage()
        {
            MaxPage = 1;

            switch (State)
            {
                case CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL:
                    if (Category == -1)
                    {
                        MaxPage = (int) Math.Ceiling(_walls.Count / 16.0f);
                    }
                    else
                    {
                        foreach (CustomHouseWallCategory c in _walls)
                        {
                            if (c.Index == Category)
                            {
                                MaxPage = c.Items.Count;
                                break;
                            }
                        }
                    }
                    break;
                case CUSTOM_HOUSE_GUMP_STATE.CHGS_DOOR:
                    MaxPage = _doors.Count;
                    break;
                case CUSTOM_HOUSE_GUMP_STATE.CHGS_FLOOR:
                    MaxPage = _floors.Count;
                    break;
                case CUSTOM_HOUSE_GUMP_STATE.CHGS_STAIR:
                    MaxPage = _stairs.Count;
                    break;
                case CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF:
                    if (Category == -1)
                    {
                        MaxPage = (int) Math.Ceiling(_roofs.Count / 16.0f);
                    }
                    else
                    {
                        foreach (var c in _roofs)
                        {
                            if (c.Index == Category)
                            {
                                MaxPage = c.Items.Count;
                                break;
                            }
                        }
                    }
                    break;
                case CUSTOM_HOUSE_GUMP_STATE.CHGS_MISC:
                    if (Category == -1)
                    {
                        MaxPage = (int) Math.Ceiling(_miscs.Count / 16.0f);
                    }
                    else
                    {
                        foreach (var c in _miscs)
                        {
                            if (c.Index == Category)
                            {
                                MaxPage = c.Items.Count;
                                break;
                            }
                        }
                    }
                    break;
            }
        }

        private void AddWall()
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

                    Rectangle bounds = FileManager.Art.GetTexture((ushort)vec[0].East1).Bounds;

                    int offsetX = x + 121 + (48 - bounds.Width) / 2;
                    int offsetY = y + 36;

                    _dataBox.Add(new StaticPic((ushort)vec[0].East1, 0)
                    {
                        X = offsetX,
                        Y = offsetY
                    });

                    _dataBox.Add(new HitBox(offsetX, offsetY,
                        bounds.Width, bounds.Height)
                    {
                        Priority = ClickPriority.Default
                    });

                    x += 48;

                    if (x >= 384)
                    {
                        x = 0;
                        y += 60;

                        // disable scissor
                        // add scissor
                    }
                }

                // disable scissor
            }
            else if (Category >= 0 && Category <= _walls.Count)
            {
                var vec = _walls[Category].Items;
                if (Page >= 0 && Page < vec.Count)
                {
                    var item = vec[Page];
                    // add scissor

                    for (int i = 0; i < 8; i++)
                    {
                        ushort graphic = (ShowWindow ? item.WindowGraphics[i] : item.Graphics[i]);

                        if (graphic != 0)
                        {
                            Rectangle bounds = FileManager.Art.GetTexture(graphic).Bounds;

                            int offsetX = x + 130 + (48 - bounds.Width) / 2;
                            int offsetY = y + 36 + (120 - bounds.Height) / 2;

                            _dataBox.Add(new StaticPic(graphic, 0)
                            {
                                X = offsetX,
                                Y = offsetY
                            });
                            _dataBox.Add(new HitBox(offsetX, offsetY, bounds.Width, bounds.Height)
                            {
                                Priority = ClickPriority.Default
                            });
                        }

                        x += 48;
                    }

                    // remove scissor
                }

                _dataBoxGUI.Add(new GumpPic(152, 0, 0x55F3, 0));
                _dataBoxGUI.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_CATEGORY, 0x5622, 0x5624, 0x5623)
                {
                    X = 167,
                    Y = 5,
                    ButtonAction = ButtonAction.Activate
                });

                _dataBoxGUI.Add(new GumpPic(218, 4, 0x55F4, 0));

                if (ShowWindow)
                {
                    _dataBoxGUI.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_WALL_SHOW_WINDOW, 0x562E, 0x5630, 0x562F)
                    {
                        X = 228,
                        Y = 9,
                        ButtonAction = ButtonAction.Activate
                    });
                }
                else
                {
                    _dataBoxGUI.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_WALL_SHOW_WINDOW, 0x562B, 0x562D, 0x562C)
                    {
                        X = 228,
                        Y = 9,
                        ButtonAction = ButtonAction.Activate
                    });
                }
            }
        }

        private void AddDoor()
        {
            if (Page >= 0 && Page < _doors.Count)
            {
                var item = _doors[Page];

                int x = 0, y = 0;

                // add scissor

                for (int i = 0; i < 8; i++)
                {
                    ushort graphic = item.Graphics[i];
                    if (graphic != 0)
                    {
                        Rectangle bounds = FileManager.Art.GetTexture(graphic).Bounds;

                        int offsetX = x + 138 + (48 - bounds.Width) / 2;

                        if (i > 3)
                        {
                            offsetX -= 20;
                        }

                        int offsetY = y + 36 + (120 - bounds.Height) / 2;

                        _dataBox.Add(new StaticPic(graphic, 0)
                        {
                            X = offsetX,
                            Y = offsetY
                        });
                        _dataBox.Add(new HitBox(offsetX, offsetY, bounds.Width, bounds.Height)
                        {
                            Priority = ClickPriority.Default
                        });
                    }

                    x += 48;
                }

                int direction = 0;

                switch (item.Category)
                {
                    case 16:
                    case 17:
                    case 18:
                        direction = 1;
                        break;
                    case 15:
                        direction = 2;
                        break;
                    case 19:
                    case 20:
                    case 21:
                    case 22:
                    case 23:
                    case 26:
                    case 27:
                    case 28:
                    case 29:
                    case 31:
                    case 32:
                    case 34:
                        direction = 3;
                        break;
                    case 30:
                    case 33:
                        direction = 4;
                        break;
                    default:
                        break;
                }

                switch (direction)
                {
                    case 0:
                        _dataBox.Add(new GumpPic(151, 39, 0x5780, 0));
                        _dataBox.Add(new GumpPic(196, 39, 0x5781, 0));
                        _dataBox.Add(new GumpPic(219, 133, 0x5782, 0));
                        _dataBox.Add(new GumpPic(266, 136, 0x5783, 0));
                        _dataBox.Add(new GumpPic(357, 136, 0x5784, 0));
                        _dataBox.Add(new GumpPic(404, 133, 0x5785, 0));
                        _dataBox.Add(new GumpPic(431, 39, 0x5786, 0));
                        _dataBox.Add(new GumpPic(474, 39, 0x5787, 0));

                        break;

                    case 1:
                        _dataBox.Add(new GumpPic(245, 39, 0x5785, 0));
                        _dataBox.Add(new GumpPic(290, 39, 0x5787, 0));
                        _dataBox.Add(new GumpPic(337, 39, 0x5780, 0));
                        _dataBox.Add(new GumpPic(380, 39, 0x5782, 0));

                        break;

                    case 2:
                        _dataBox.Add(new GumpPic(219, 133, 0x5782, 0));
                        _dataBox.Add(new GumpPic(404, 133, 0x5785, 0));
                        break;

                    case 3:
                        _dataBox.Add(new GumpPic(245, 39, 0x5780, 0));
                        _dataBox.Add(new GumpPic(290, 39, 0x5781, 0));
                        _dataBox.Add(new GumpPic(337, 39, 0x5786, 0));
                        _dataBox.Add(new GumpPic(380, 39, 0x5787, 0));

                        break;

                    case 4:
                        _dataBox.Add(new GumpPic(151, 39, 0x5780, 0));
                        _dataBox.Add(new GumpPic(196, 39, 0x5781, 0));
                        _dataBox.Add(new GumpPic(245, 39, 0x5780, 0));
                        _dataBox.Add(new GumpPic(290, 39, 0x5781, 0));
                        _dataBox.Add(new GumpPic(337, 39, 0x5786, 0));
                        _dataBox.Add(new GumpPic(380, 39, 0x5787, 0));
                        _dataBox.Add(new GumpPic(431, 39, 0x5786, 0));
                        _dataBox.Add(new GumpPic(474, 39, 0x5787, 0));

                        break;
                }

                // remove scissor
            }
        }

        private void AddFloor()
        {
            if (Page >= 0 && Page < _floors.Count)
            {
                var item = _floors[Page];

                int x = 0, y = 0;

                // add scissor

                int index = 0;

                for (int j = 0; j < 2; j++)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        ushort graphic = item.Graphics[index];

                        if (graphic != 0)
                        {
                            Rectangle bounds = FileManager.Art.GetTexture(graphic).Bounds;

                            int offsetX = x + 123 + (48 - bounds.Width) / 2;
                            int offsetY = y + 36 + (60 - bounds.Height) / 2;

                            _dataBox.Add(new StaticPic(graphic, 0)
                            {
                                X = offsetX,
                                Y = offsetY
                            });
                            _dataBox.Add(new HitBox(offsetX, offsetY, bounds.Width, bounds.Height)
                            {
                                Priority = ClickPriority.Default
                            });
                        }

                        x += 48;
                        index++;
                    }

                    x = 0;
                    y += 60;
                }

                // remove scissor
            }
        }

        private void AddStair()
        {
            if (Page >= 0 && Page < _stairs.Count)
            {
                var item = _stairs[Page];

                for (int j = 0; j < 2; j++)
                {
                    int x = (j != 0 ? 96 : 192);
                    int y = (j != 0 ? 60 : 0);

                    // add scissor

                    Label text = new Label(FileManager.Cliloc.GetString(1062113 + j), true, 0xFFFF, 90, 0)
                    {
                        X = 137,
                        Y = j != 0 ? 111 : 51
                    };
                    _dataBox.Add(text);

                    int start = (j != 0 ? 0 : 5);
                    int end = (j != 0 ? 6 : 9);
                    int combinedStair = (j != 0 ? 0 : 10);

                    for (int i = start; i < end; i++)
                    {
                        ushort graphic = item.Graphics[i];

                        if (graphic != 0)
                        {
                            Rectangle bounds = FileManager.Art.GetTexture(graphic).Bounds;

                            int offsetX = x + 123 + (48 - bounds.Width) / 2;
                            int offsetY = y + 36 + (60 - bounds.Height) / 2;

                            _dataBox.Add(new StaticPic(graphic, 0)
                            {
                                X = offsetX,
                                Y = offsetY
                            });
                            _dataBox.Add(new HitBox(offsetX, offsetY, bounds.Width, bounds.Height)
                            {
                                Priority = ClickPriority.Default
                            });
                        }

                        x += 48;
                    }
                    // remove scissor
                }

                _dataBox.Add(new ColorBox(384, 2, 0, 0xFF7F7F7F)
                {
                    X = 123,
                    Y = 96
                });
            }
        }

        private void AddRoof()
        {
            int x = 0, y = 0;

            if (Category == -1)
            {
                int startCategory = Page * 16;
                int endCategory = startCategory + 16;

                if (endCategory > _roofs.Count)
                    endCategory = _roofs.Count;

                // push scissor

                for (int i = startCategory; i < endCategory; i++)
                {
                    var vec = _roofs[i].Items;

                    if (vec.Count == 0)
                        continue;

                    Rectangle bounds = FileManager.Art.GetTexture((ushort)vec[0].NSCrosspiece).Bounds;

                    int offsetX = x + 121 + (48 - bounds.Width) / 2;
                    int offsetY = y + 36;

                    _dataBox.Add(new StaticPic((ushort)vec[0].NSCrosspiece, 0)
                    {
                        X = offsetX,
                        Y = offsetY
                    });
                    _dataBox.Add(new HitBox(offsetX, offsetY, bounds.Width, bounds.Height)
                    {
                        Priority = ClickPriority.Default
                    });

                    x += 48;

                    if (x >= 384)
                    {
                        x = 0;
                        y += 60;

                        // pop scissor,
                        // push scissor
                    }
                }

                // pop scissor
            }
            else if (Category >= 0 && Category < _roofs.Count)
            {
                var vec = _roofs[Category].Items;

                if (Page >= 0 && Page < vec.Count)
                {
                    var item = vec[Page];

                    // push scissor

                    int index = 0;

                    for (int j = 0; j < 2; j++)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            ushort graphic = item.Graphics[index];

                            if (graphic != 0)
                            {
                                Rectangle bounds = FileManager.Art.GetTexture(graphic).Bounds;

                                int offsetX = x + 130 + (48 - bounds.Width) / 2;
                                int offsetY = y + 44 + (60 - bounds.Height) / 2;

                                _dataBox.Add(new StaticPic(graphic, 0)
                                {
                                    X = offsetX,
                                    Y = offsetY
                                });
                                _dataBox.Add(new HitBox(offsetX, offsetY, bounds.Width, bounds.Height)
                                {
                                    Priority = ClickPriority.Default
                                });
                            }

                            x += 48;
                            index++;
                        }

                        x = 0;
                        y += 60;
                    }

                    // pop scissor
                }

                _dataBoxGUI.Add(new GumpPic(152, 0, 0x55F3, 0));
                _dataBoxGUI.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_CATEGORY, 0x5622, 0x5624, 0x5623)
                {
                    X = 167,
                    Y = 5,
                    ButtonAction = ButtonAction.Activate
                });
                _dataBoxGUI.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_ROOF_Z_DOWN, 0x578B, 0x578D, 0x578C)
                {
                    X = 305,
                    Y = 0,
                    ButtonAction = ButtonAction.Activate
                });
                _dataBoxGUI.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_ROOF_Z_UP, 0x578E, 0x5790, 0x578F)
                {
                    X = 349,
                    Y = 0,
                    ButtonAction = ButtonAction.Activate
                });

                _dataBoxGUI.Add(new GumpPic(583, 4, 0x55F4, 0)
                {

                });

                Label text = new Label(RoofZ.ToString(), false, 0x04E9, font: 3)
                {
                    X = 405,
                    Y = 15
                };
                _dataBoxGUI.Add(text);
            }
        }

        private void AddMisc()
        {
            int x = 0, y = 0;

            if (Category == -1)
            {
                int startCategory = Page * 16;
                int endCategory = startCategory + 16;

                if (endCategory > _miscs.Count)
                    endCategory = _miscs.Count;

                // push scissor

                for (int i = startCategory; i < endCategory; i++)
                {
                    var vec = _miscs[i].Items;

                    if (vec.Count == 0)
                        continue;

                    Rectangle bounds = FileManager.Art.GetTexture((ushort)vec[0].Piece5).Bounds;

                    int offsetX = x + 121 + (48 - bounds.Width) / 2;
                    int offsetY = y + 36;

                    _dataBox.Add(new StaticPic((ushort)vec[0].Piece5, 0)
                    {
                        X = offsetX,
                        Y = offsetY
                    });
                    _dataBox.Add(new HitBox(offsetX, offsetY, bounds.Width, bounds.Height)
                    {
                        Priority = ClickPriority.Default
                    });

                    x += 48;

                    if (x >= 384)
                    {
                        x = 0;
                        y += 60;

                        // pop scissor
                        // push scissor
                    }
                }

                // pop scissor
            }
            else if (Category >= 0 && Category < _miscs.Count)
            {
                var vec = _miscs[Category].Items;

                if (Page >= 0 && Page < vec.Count)
                {
                    var item = vec[Page];

                    // push scissor

                    for (int i = 0; i < 8; i++)
                    {
                        ushort graphic = item.Graphics[i];

                        if (graphic != 0)
                        {
                            Rectangle bounds = FileManager.Art.GetTexture(graphic).Bounds;

                            int offsetX = x + 130 + (48 - bounds.Width) / 2;
                            int offsetY = y + 44 + (120 - bounds.Height) / 2;

                            _dataBox.Add(new StaticPic(graphic, 0)
                            {
                                X = offsetX,
                                Y = offsetY
                            });
                            _dataBox.Add(new HitBox(offsetX, offsetY, bounds.Width, bounds.Height)
                            {
                                Priority = ClickPriority.Default
                            });
                        }

                        x += 48;
                    }

                    // pop scissor
                }

                _dataBoxGUI.Add(new GumpPic(152, 0, 0x55F3, 0));
                _dataBoxGUI.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_CATEGORY, 0x5622, 0x5624, 0x5623)
                {
                    X = 167,
                    Y = 5,
                    ButtonAction = ButtonAction.Activate
                });
            }
        }

        private void AddMenu()
        {
            const int TEXT_WIDTH = 108;

            _dataBox.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_BACKUP,
                0x098D, 0x098D, 0x098D)
            {
                X = 150,
                Y = 50,
                ButtonAction = ButtonAction.Activate
            });

            Label entry = new Label("Backup", true, 0x0036, TEXT_WIDTH, font: 0, align: TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 150,
                Y = 50
            };
            _dataBox.Add(entry);



            _dataBox.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_RESTORE,
                0x098D, 0x098D, 0x098D)
            {
                X = 150,
                Y = 90,
                ButtonAction = ButtonAction.Activate
            });
            entry = new Label("Restore", true, 0x0036, TEXT_WIDTH, font: 0, align: TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 150,
                Y = 90
            };
            _dataBox.Add(entry);


            _dataBox.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_SYNCH,
                0x098D, 0x098D, 0x098D)
            {
                X = 270,
                Y = 50,
                ButtonAction = ButtonAction.Activate
            });
            entry = new Label("Synch", true, 0x0036, TEXT_WIDTH, font: 0, align: TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 270,
                Y = 50
            };
            _dataBox.Add(entry);



            _dataBox.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_CLEAR,
                0x098D, 0x098D, 0x098D)
            {
                X = 270,
                Y = 90,
                ButtonAction = ButtonAction.Activate
            });
            entry = new Label("Clear", true, 0x0036, TEXT_WIDTH, font: 0, align: TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 270,
                Y = 90
            };
            _dataBox.Add(entry);



            _dataBox.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_COMMIT,
                0x098D, 0x098D, 0x098D)
            {
                X = 390,
                Y = 50,
                ButtonAction = ButtonAction.Activate
            });
            entry = new Label("Commit", true, 0x0036, TEXT_WIDTH, font: 0, align: TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 390,
                Y = 50
            };
            _dataBox.Add(entry);



            _dataBox.Add(new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_REVERT,
                0x098D, 0x098D, 0x098D)
            {
                X = 390,
                Y = 90,
                ButtonAction = ButtonAction.Activate
            });
            entry = new Label("Revert", true, 0x0036, TEXT_WIDTH, font: 0, align: TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 390,
                Y = 90
            };
            _dataBox.Add(entry);
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((ID_GUMP_CUSTOM_HOUSE)buttonID)
            {
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_WALL:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_DOOR:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_FLOOR:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_STAIR:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_ROOF:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_MISC:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_ERASE:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_EYEDROPPER:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_MENU:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_1:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_2:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_3:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_4:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_1:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_2:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_3:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_4:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_LIST_LEFT:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_LIST_RIGHT:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_BACKUP:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_RESTORE:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_SYNCH:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_CLEAR:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_COMMIT:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_REVERT:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_CATEGORY:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_WALL_SHOW_WINDOW:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_ROOF_Z_UP:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_ROOF_Z_DOWN:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_AREA_OBJECTS_INFO:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_AREA_COST_INFO:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_AREA_ROOF_Z_INFO:
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_ITEM_IN_LIST:
                    break;
                default:
                    break;
            }
        }


        private void GenerateFloorPlace()
        {

        }

        private void SeekGraphic(ushort graphic)
        {

        }

        private bool CanBuildHere()
        {
            return false;
        }

        private bool ValidateItemPlace(Rectangle rect, ushort grpahic , int x, int y)
        {
            
            return false;
        }

        private bool ValidateItemPlace(Item foundationItem, Multi item, int minZ, int maxZ,
            List<Point> validatedFloors)
        {

            if (item == null || !World.HouseManager.TryGetHouse(foundationItem, out var house) || !house.IsCustom)
                return true;

            if ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0)
            {
                bool existsInList(List<Point> list, Point testedPoint)
                {
                    foreach (Point point in list)
                    {
                        if (testedPoint == point)
                            return true;
                    }

                    return false;
                }
                // TODO HERE

                //if (ValidatePlaceStructure(foundationItem, house, minZ - 20, maxZ - 20,
                //    (int) (CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_DIRECT_SUPPORT)) ||
                //    ValidatePlaceStructure(foundationItem,))
                //{

                //}
            }

            return false;
        }

        private bool CanEraseHere(GameObject place, ref CUSTOM_HOUSE_BUILD_TYPE type)
        {
            return false;
        }

        private void OnTargetWorld(GameObject place)
        {

        }

        public override void Dispose()
        {
            NetClient.Socket.Send(new PCustomHouseBuildingExit());
            base.Dispose();
        }


        private static void ParseFile<T>(List<T> list, string path) where T : CustomHouseObject, new()
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
                        if (item.FeatureMask == 0 || ((int)World.ClientLockedFeatures.Flags & item.FeatureMask) != 0)
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
                        if (item.FeatureMask != 0 && ((int)World.ClientLockedFeatures.Flags & item.FeatureMask) == 0)
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


    enum CUSTOM_HOUSE_GUMP_STATE
    {
        CHGS_WALL = 0,
        CHGS_DOOR,
        CHGS_FLOOR,
        CHGS_STAIR,
        CHGS_ROOF,
        CHGS_MISC,
        CHGS_MENU
    }

    enum CUSTOM_HOUSE_FLOOR_VISION_STATE
    {
        CHGVS_NORMAL = 0,
        CHGVS_TRANSPARENT_CONTENT,
        CHGVS_HIDE_CONTENT,
        CHGVS_TRANSPARENT_FLOOR,
        CHGVS_HIDE_FLOOR,
        CHGVS_TRANSLUCENT_FLOOR,
        CHGVS_HIDE_ALL
    }

    enum CUSTOM_HOUSE_BUILD_TYPE
    {
        CHBT_NORMAL = 0,
        CHBT_ROOF,
        CHBT_FLOOR,
        CHBT_STAIR
    }

    [Flags]
    enum CUSTOM_HOUSE_MULTI_OBJECT_FLAGS
    {
        CHMOF_GENERIC_INTERNAL = 0x01,
        CHMOF_FLOOR = 0x02,
        CHMOF_STAIR = 0x04,
        CHMOF_ROOF = 0x08,
        CHMOF_FIXTURE = 0x10,
        CHMOF_TRANSPARENT = 0x20,
        CHMOF_IGNORE_IN_RENDER = 0x40,
        CHMOF_VALIDATED_PLACE = 0x80,
        CHMOF_INCORRECT_PLACE = 0x100
    }

    enum CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS
    {
        CHVCF_TOP = 0x01,
        CHVCF_BOTTOM = 0x02,
        CHVCF_N = 0x04,
        CHVCF_E = 0x08,
        CHVCF_S = 0x10,
        CHVCF_W = 0x20,
        CHVCF_DIRECT_SUPPORT = 0x40,
        CHVCF_CANGO_W = 0x80,
        CHVCF_CANGO_N = 0x100
    }
}
