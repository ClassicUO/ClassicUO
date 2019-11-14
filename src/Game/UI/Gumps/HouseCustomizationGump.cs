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


            var button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_WALL, 0x5654, 0x5656, 0x5655)
            {
                X = 9,
                Y = 41,
                ButtonAction = ButtonAction.Activate,
            };
            button.SetTooltip("Walls");
            Add(button);

            button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_DOOR, 0x5657, 0x5659, 0x5658)
            {
                X = 39,
                Y = 40,
                ButtonAction = ButtonAction.Activate
            };
            button.SetTooltip("Doors");
            Add(button);

            button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_FLOOR, 0x565A, 0x565C, 0x565B)
            {
                X = 70,
                Y = 40,
                ButtonAction = ButtonAction.Activate
            };
            button.SetTooltip("Floors");
            Add(button);

            button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_STAIR, 0x565D, 0x565F, 0x565E)
            {
                X = 9,
                Y = 72,
                ButtonAction = ButtonAction.Activate
            };
            button.SetTooltip("Stairs");
            Add(button);

            button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_ROOF, 0x5788, 0x578A, 0x5789)
            {
                X = 39,
                Y = 72,
                ButtonAction = ButtonAction.Activate
            };
            button.SetTooltip("Roofs");
            Add(button);

            button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_MISC, 0x5663, 0x5665, 0x5664)
            {
                X = 69,
                Y = 72,
                ButtonAction = ButtonAction.Activate
            };
            button.SetTooltip("Miscellaneous");
            Add(button);

            button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_MENU, 0x566C, 0x566E, 0x566D)
            {
                X = 69,
                Y = 100,
                ButtonAction = ButtonAction.Activate
            };
            button.SetTooltip("System Menu");
            Add(button);

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

            var button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_ERASE, (ushort) (0x5666 + (Erasing ? 1 : 0)), 0x5668, 0x5667)
            {
                X = 9,
                Y = 100,
                ButtonAction = ButtonAction.Activate
            };
            button.SetTooltip("Erase");
            _dataBoxGUI.Add(button);

            button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_EYEDROPPER, (ushort) (0x5669 + (SeekTile ? 1 : 0)), 0x566B, 0x566A)
            {
                X = 39,
                Y = 100,
                ButtonAction = ButtonAction.Activate
            };
            button.SetTooltip("Eyedropper Tool");
            Add(button);

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

            button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_1, floorVisionGraphic,
                                    (ushort) (floorVisionGraphic + 2), (ushort) (floorVisionGraphic + 1))
            {
                X = 533,
                Y = 108,
                ButtonAction = ButtonAction.Activate
            };
            button.SetTooltip("Store 1 Visibility");
            _dataBoxGUI.Add(button);

            button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_1, (ushort) (0x56CD + graphicOffset2),
                                0x56D1, (ushort) (0x56CD + graphicOffset2))
            {
                X = 583,
                Y = 96,
                ButtonAction = ButtonAction.Activate
            };
            button.SetTooltip("Go To Story 1");
            _dataBoxGUI.Add(button);

            button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_1, (ushort) (0x56F6 + graphicOffset),
                                (ushort) (0x56F8 + graphicOffset), (ushort) (0x56F7 + graphicOffset))
            {
                X = 623,
                Y = 103,
                ButtonAction = ButtonAction.Activate
            };
            button.SetTooltip("Go To Story 1");
            _dataBoxGUI.Add(button);


            floorVisionGraphic = floorVisionGraphic2[associateGraphicTable[_floorVisionState[1]]];
            graphicOffset = CurrentFloor == 2 ? 3 : 0;
            graphicOffset2 = CurrentFloor == 2 ? 4 : 0;

            button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_2, floorVisionGraphic,
                                (ushort) (floorVisionGraphic + 2), (ushort) (floorVisionGraphic + 1))
            {
                X = 533,
                Y = 86,
                ButtonAction = ButtonAction.Activate
            };
            button.SetTooltip("Store 2 Visibility");
            _dataBoxGUI.Add(button);

            button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_2, (ushort) (0x56CE + graphicOffset2),
                                0x56D2, (ushort) (0x56CE + graphicOffset2))
            {
                X = 583,
                Y = 73,
                ButtonAction = ButtonAction.Activate
            };
            button.SetTooltip("Go To Story 2");
            _dataBoxGUI.Add(button);

            button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_2, (ushort) (0x56F0 + graphicOffset),
                                (ushort) (0x56F2 + graphicOffset), (ushort) (0x56F1 + graphicOffset))
            {
                X = 623,
                Y = 86,
                ButtonAction = ButtonAction.Activate
            };
            button.SetTooltip("Go To Story 2");
            _dataBoxGUI.Add(button);


            graphicOffset = CurrentFloor == 3 ? 3 : 0;
            graphicOffset2 = CurrentFloor == 3 ? 4 : 0;
            if (FloorCount == 4)
            {
                floorVisionGraphic = floorVisionGraphic2[associateGraphicTable[_floorVisionState[2]]];

                button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_3, floorVisionGraphic,
                                    (ushort) (floorVisionGraphic + 2), (ushort) (floorVisionGraphic + 1))
                {
                    X = 533,
                    Y = 64,
                    ButtonAction = ButtonAction.Activate
                };
                button.SetTooltip("Store 3 Visibility");
                _dataBoxGUI.Add(button);

                button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_3, (ushort) (0x56CE + graphicOffset2),
                                    0x56D2, (ushort) (0x56CE + graphicOffset2))
                {
                    X = 582,
                    Y = 56,
                    ButtonAction = ButtonAction.Activate
                };
                button.SetTooltip("Go To Story 3");
                _dataBoxGUI.Add(button);

                button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_3, (ushort) (0x56F0 + graphicOffset),
                                    (ushort) (0x56F2 + graphicOffset), (ushort) (0x56F1 + graphicOffset))
                {
                    X = 623,
                    Y = 69,
                    ButtonAction = ButtonAction.Activate
                };
                button.SetTooltip("Go To Story 3");
                _dataBoxGUI.Add(button);



                floorVisionGraphic = floorVisionGraphic2[associateGraphicTable[_floorVisionState[3]]];
                graphicOffset = CurrentFloor == 4 ? 3 : 0;
                graphicOffset2 = CurrentFloor == 4 ? 4 : 0;

                button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_4, floorVisionGraphic,
                                    (ushort) (floorVisionGraphic + 2), (ushort) (floorVisionGraphic + 1))
                {
                    X = 533,
                    Y = 42,
                    ButtonAction = ButtonAction.Activate
                };
                button.SetTooltip("Store 4 Visibility");
                _dataBoxGUI.Add(button);

                button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_4, (ushort) (0x56D0 + graphicOffset2),
                                    0x56D4, (ushort) (0x56D0 + graphicOffset2))
                {
                    X = 583,
                    Y = 42,
                    ButtonAction = ButtonAction.Activate
                };
                button.SetTooltip("Go To Story 4");
                _dataBoxGUI.Add(button);

                button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_4, (ushort) (0x56EA + graphicOffset),
                                    (ushort) (0x56EC + graphicOffset), (ushort) (0x56EB + graphicOffset))
                {
                    X = 623,
                    Y = 50,
                    ButtonAction = ButtonAction.Activate
                };
                button.SetTooltip("Go To Story 4");
                _dataBoxGUI.Add(button);
            }
            else
            {
                floorVisionGraphic = floorVisionGraphic2[associateGraphicTable[_floorVisionState[2]]];

                button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_3, floorVisionGraphic,
                                    (ushort) (floorVisionGraphic + 2), (ushort) (floorVisionGraphic + 1))
                {
                    X = 533,
                    Y = 64,
                    ButtonAction = ButtonAction.Activate
                };
                button.SetTooltip("Store 3 Visibility");
                _dataBoxGUI.Add(button);

                button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_3, (ushort) (0x56D0 + graphicOffset2),
                                    0x56D4, (ushort) (0x56D0 + graphicOffset2))
                {
                    X = 582,
                    Y = 56,
                    ButtonAction = ButtonAction.Activate
                };
                button.SetTooltip("Go To Story 3");
                _dataBoxGUI.Add(button);

                button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_3, (ushort) (0x56EA + graphicOffset),
                                    (ushort) (0x56EC + graphicOffset), (ushort) (0x56EB + graphicOffset))
                {
                    X = 623,
                    Y = 69,
                    ButtonAction = ButtonAction.Activate
                };
                button.SetTooltip("Go To Story 3");
                _dataBoxGUI.Add(button);
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
                button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_LIST_LEFT, 0x5625, 0x5627, 0x5626)
                {
                    X = 110,
                    Y = 63,
                    ButtonAction = ButtonAction.Activate
                };
                button.SetTooltip("Previous Page");
                _dataBoxGUI.Add(button);

                button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_LIST_RIGHT, 0x5628, 0x562A, 0x5629)
                {
                    X = 510,
                    Y = 63,
                    ButtonAction = ButtonAction.Activate
                };
                button.SetTooltip("Next Page");
                _dataBoxGUI.Add(button);
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
                        if (item.IsCustom &&
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

        public bool ValidatePlaceStructure(Item foundationItem, House house, Multi multi, int minZ, int maxZ, int flags)
        {
            if (house == null || multi == null)
                return false;


            foreach (Multi item in house.Components)
            {
                List<Point> validatedFloors = new List<Point>();

                if (item.IsCustom && (item.State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR | 
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

                    StaticPic pic = new StaticPic((ushort) vec[0].East1, 0)
                    {
                        X = offsetX,
                        Y = offsetY,
                        LocalSerial = (uint) (ID_GUMP_CUSTOM_HOUSE.ID_GCH_ITEM_IN_LIST + i)
                    };
                    pic.MouseDown += (sender, e) =>
                    {
                        OnButtonClick((int) pic.LocalSerial.Value);
                    };
                    _dataBox.Add(pic);

                    //_dataBox.Add(new HitBox(offsetX, offsetY,
                    //    bounds.Width, bounds.Height)
                    //{
                    //    Priority = ClickPriority.Default
                    //});

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

                            var pic = new StaticPic(graphic, 0)
                            {
                                X = offsetX,
                                Y = offsetY,
                                LocalSerial = (uint) (ID_GUMP_CUSTOM_HOUSE.ID_GCH_ITEM_IN_LIST + i)
                            };
                            pic.MouseUp += (sender, e) =>
                            {
                                OnButtonClick((int) pic.LocalSerial.Value);
                            };
                            _dataBox.Add(pic);
                            //_dataBox.Add(new HitBox(offsetX, offsetY, bounds.Width, bounds.Height)
                            //{
                            //    Priority = ClickPriority.Default
                            //});
                        }

                        x += 48;
                    }

                    // remove scissor
                }

                _dataBoxGUI.Add(new GumpPic(152, 0, 0x55F3, 0));

                var button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_CATEGORY, 0x5622, 0x5624, 0x5623)
                {
                    X = 167,
                    Y = 5,
                    ButtonAction = ButtonAction.Activate
                };
                button.SetTooltip("To Category");
                _dataBoxGUI.Add(button);

                _dataBoxGUI.Add(new GumpPic(218, 4, 0x55F4, 0));

                if (ShowWindow)
                {
                    button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_WALL_SHOW_WINDOW, 0x562E, 0x5630, 0x562F)
                    {
                        X = 228,
                        Y = 9,
                        ButtonAction = ButtonAction.Activate
                    };
                    button.SetTooltip("Window Toggle");
                    _dataBoxGUI.Add(button);
                }
                else
                {
                    button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_WALL_SHOW_WINDOW, 0x562B, 0x562D, 0x562C)
                    {
                        X = 228,
                        Y = 9,
                        ButtonAction = ButtonAction.Activate
                    };
                    button.SetTooltip("Window Toggle");
                    _dataBoxGUI.Add(button);
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

                        var pic = new StaticPic(graphic, 0)
                        {
                            X = offsetX,
                            Y = offsetY,
                            LocalSerial = (uint)(ID_GUMP_CUSTOM_HOUSE.ID_GCH_ITEM_IN_LIST + i)
                        };
                        pic.MouseUp += (sender, e) => { OnButtonClick((int)pic.LocalSerial.Value); };
                        _dataBox.Add(pic);
                        //_dataBox.Add(new StaticPic(graphic, 0)
                        //{
                        //    X = offsetX,
                        //    Y = offsetY
                        //});
                        //_dataBox.Add(new HitBox(offsetX, offsetY, bounds.Width, bounds.Height)
                        //{
                        //    Priority = ClickPriority.Default
                        //});
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

                            var pic = new StaticPic(graphic, 0)
                            {
                                X = offsetX,
                                Y = offsetY,
                                LocalSerial = (uint)(ID_GUMP_CUSTOM_HOUSE.ID_GCH_ITEM_IN_LIST + i + combinedStair)
                            };
                            pic.MouseUp += (sender, e) => { OnButtonClick((int) pic.LocalSerial.Value);};
                            _dataBox.Add(pic);
                            //_dataBox.Add(new HitBox(offsetX, offsetY, bounds.Width, bounds.Height)
                            //{
                            //    Priority = ClickPriority.Default
                            //});
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

                    var pic = new StaticPic((ushort)vec[0].NSCrosspiece, 0)
                    {
                        X = offsetX,
                        Y = offsetY,
                        LocalSerial = (uint)(ID_GUMP_CUSTOM_HOUSE.ID_GCH_ITEM_IN_LIST + i)
                    };
                    pic.MouseUp += (sender, e) => { OnButtonClick((int)pic.LocalSerial.Value); };
                    _dataBox.Add(pic);

                    //_dataBox.Add(new StaticPic((ushort)vec[0].NSCrosspiece, 0)
                    //{
                    //    X = offsetX,
                    //    Y = offsetY
                    //});
                    //_dataBox.Add(new HitBox(offsetX, offsetY, bounds.Width, bounds.Height)
                    //{
                    //    Priority = ClickPriority.Default
                    //});

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

                                var pic = new StaticPic(graphic, 0)
                                {
                                    X = offsetX,
                                    Y = offsetY,
                                    LocalSerial = (uint)(ID_GUMP_CUSTOM_HOUSE.ID_GCH_ITEM_IN_LIST + i)
                                };
                                pic.MouseUp += (sender, e) => { OnButtonClick((int)pic.LocalSerial.Value); };
                                _dataBox.Add(pic);

                                //_dataBox.Add(new StaticPic(graphic, 0)
                                //{
                                //    X = offsetX,
                                //    Y = offsetY
                                //});
                                //_dataBox.Add(new HitBox(offsetX, offsetY, bounds.Width, bounds.Height)
                                //{
                                //    Priority = ClickPriority.Default
                                //});
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

                var button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_CATEGORY, 0x5622, 0x5624, 0x5623)
                {
                    X = 167,
                    Y = 5,
                    ButtonAction = ButtonAction.Activate
                };
                button.SetTooltip("To Category");
                _dataBoxGUI.Add(button);

                button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_ROOF_Z_DOWN, 0x578B, 0x578D, 0x578C)
                {
                    X = 305,
                    Y = 0,
                    ButtonAction = ButtonAction.Activate
                };
                button.SetTooltip("Lower Roof Placement Level");
                _dataBoxGUI.Add(button);

                button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_ROOF_Z_UP, 0x578E, 0x5790, 0x578F)
                {
                    X = 349,
                    Y = 0,
                    ButtonAction = ButtonAction.Activate
                };
                button.SetTooltip("Raise Roof Placement Level");
                _dataBoxGUI.Add(button);

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

                    //_dataBox.Add(new StaticPic((ushort)vec[0].Piece5, 0)
                    //{
                    //    X = offsetX,
                    //    Y = offsetY
                    //});
                    //_dataBox.Add(new HitBox(offsetX, offsetY, bounds.Width, bounds.Height)
                    //{
                    //    Priority = ClickPriority.Default
                    //});

                    var pic = new StaticPic((ushort) vec[0].Piece5, 0)
                    {
                        X = offsetX,
                        Y = offsetY,
                        LocalSerial = (uint)(ID_GUMP_CUSTOM_HOUSE.ID_GCH_ITEM_IN_LIST + i)
                    };
                    pic.MouseUp += (sender, e) => { OnButtonClick((int)pic.LocalSerial.Value); };
                    _dataBox.Add(pic);

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

                            var pic = new StaticPic(graphic, 0)
                            {
                                X = offsetX,
                                Y = offsetY,
                                LocalSerial = (uint)(ID_GUMP_CUSTOM_HOUSE.ID_GCH_ITEM_IN_LIST + i)
                            };
                            pic.MouseUp += (sender, e) => { OnButtonClick((int)pic.LocalSerial.Value); };
                            _dataBox.Add(pic);

                            //_dataBox.Add(new StaticPic(graphic, 0)
                            //{
                            //    X = offsetX,
                            //    Y = offsetY
                            //});
                            //_dataBox.Add(new HitBox(offsetX, offsetY, bounds.Width, bounds.Height)
                            //{
                            //    Priority = ClickPriority.Default
                            //});
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

            var button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_BACKUP,
                                    0x098D, 0x098D, 0x098D)
            {
                X = 150,
                Y = 50,
                ButtonAction = ButtonAction.Activate
            };
            button.SetTooltip("Store design in progress in a back up buffer, but do not finalize design.");
            _dataBox.Add(button);

            Label entry = new Label("Backup", true, 0x0036, TEXT_WIDTH, font: 0, align: TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 150,
                Y = 50
            };
            _dataBox.Add(entry);


            button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_RESTORE,
                                0x098D, 0x098D, 0x098D)
            {
                X = 150,
                Y = 90,
                ButtonAction = ButtonAction.Activate
            };
            button.SetTooltip("Restore your design in progress to a design you have previously backed up.");
            _dataBox.Add(button);
            entry = new Label("Restore", true, 0x0036, TEXT_WIDTH, font: 0, align: TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 150,
                Y = 90
            };
            _dataBox.Add(entry);


            button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_SYNCH,
                                0x098D, 0x098D, 0x098D)
            {
                X = 270,
                Y = 50,
                ButtonAction = ButtonAction.Activate
            };
            button.SetTooltip("Synchronize design state with server.");
            _dataBox.Add(button);
            entry = new Label("Synch", true, 0x0036, TEXT_WIDTH, font: 0, align: TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 270,
                Y = 50
            };
            _dataBox.Add(entry);


            button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_CLEAR,
                                0x098D, 0x098D, 0x098D)
            {
                X = 270,
                Y = 90,
                ButtonAction = ButtonAction.Activate
            };
            button.SetTooltip("Clear all changes, returning your design in progress to a blank foundation.");
            _dataBox.Add(button);
            entry = new Label("Clear", true, 0x0036, TEXT_WIDTH, font: 0, align: TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 270,
                Y = 90
            };
            _dataBox.Add(entry);


            button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_COMMIT,
                                0x098D, 0x098D, 0x098D)
            {
                X = 390,
                Y = 50,
                ButtonAction = ButtonAction.Activate
            };
            button.SetTooltip("Save existing changes and finalize design.");
            _dataBox.Add(button);
            entry = new Label("Commit", true, 0x0036, TEXT_WIDTH, font: 0, align: TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 390,
                Y = 50
            };
            _dataBox.Add(entry);


            button = new Button((int) ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_REVERT,
                                    0x098D, 0x098D, 0x098D)
            {
                X = 390,
                Y = 90,
                ButtonAction = ButtonAction.Activate
            };
            button.SetTooltip("Revert your design in progress to match your currently visible, finalized design.");
            _dataBox.Add(button);
            entry = new Label("Revert", true, 0x0036, TEXT_WIDTH, font: 0, align: TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 390,
                Y = 90
            };
            _dataBox.Add(entry);
        }

        public override void OnButtonClick(int buttonID)
        {
            ID_GUMP_CUSTOM_HOUSE idd = (ID_GUMP_CUSTOM_HOUSE) buttonID;

            if (idd >= ID_GUMP_CUSTOM_HOUSE.ID_GCH_ITEM_IN_LIST)
            {
                int index = idd - ID_GUMP_CUSTOM_HOUSE.ID_GCH_ITEM_IN_LIST;

                if (Category == -1 && (State == CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL ||
                                       State == CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF ||
                                       State == CUSTOM_HOUSE_GUMP_STATE.CHGS_MISC))
                {
                    int newCategory = -1;

                    if (State == CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL && index >= 0 && index < _walls.Count)
                    {
                        newCategory = _walls[index].Index;
                    }
                    else if (State == CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF && index >= 0 && index < _roofs.Count)
                    {
                        newCategory = _roofs[index].Index;
                    }
                    else if (State == CUSTOM_HOUSE_GUMP_STATE.CHGS_MISC && index >= 0 && index < _miscs.Count)
                    {
                        newCategory = _miscs[index].Index;
                    }


                    if (newCategory != -1)
                    {
                        Category = newCategory;
                        Page = 0;
                        SelectedGraphic = 0;
                        Erasing = false;
                        SeekTile = false;
                        CombinedStair = false;
                        UpdateMaxPage();
                        Update();
                    }
                }
                else if (index >= 0 && Page >= 0)
                {
                    bool combinedStairs = false;
                    ushort graphic = 0;

                    if (State == CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL ||
                        State == CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF ||
                        State == CUSTOM_HOUSE_GUMP_STATE.CHGS_MISC)
                    {
                        if (Category >= 0)
                        {
                            if (State == CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL && Category < _walls.Count && index < CustomHouseWall.GRAPHICS_COUNT)
                            {
                                var list = _walls[Category].Items;

                                if (Page < list.Count)
                                {
                                    graphic = (ShowWindow ? list[Page].WindowGraphics[index] : list[Page].Graphics[index]);
                                }
                            }
                            else if (State == CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF && Category < _roofs.Count && index < CustomHouseRoof.GRAPHICS_COUNT)
                            {
                                var list = _roofs[Category].Items;

                                if (Page < list.Count)
                                {
                                    graphic = list[Page].Graphics[index];
                                }
                            }
                            else if (State == CUSTOM_HOUSE_GUMP_STATE.CHGS_MISC && Category < _miscs.Count && index < CustomHouseMisc.GRAPHICS_COUNT)
                            {
                                var list = _miscs[Category].Items;

                                if (Page < list.Count)
                                {
                                    graphic = list[Page].Graphics[index];
                                }
                            }
                        }
                    }
                    else
                    {
                        if (State == CUSTOM_HOUSE_GUMP_STATE.CHGS_DOOR && Page < _doors.Count && index < CustomHouseDoor.GRAPHICS_COUNT)
                        {
                            graphic = _doors[Page].Graphics[index];
                        }
                        else if (State == CUSTOM_HOUSE_GUMP_STATE.CHGS_FLOOR && Page < _floors.Count && index < CustomHouseFloor.GRAPHICS_COUNT)
                        {
                            graphic = _floors[Page].Graphics[index];
                        }
                        else if (State == CUSTOM_HOUSE_GUMP_STATE.CHGS_STAIR && Page < _stairs.Count)
                        {
                            if (index > 10)
                            {
                                combinedStairs = true;
                                index -= 10;
                            }

                            if (index < CustomHouseStair.GRAPHICS_COUNT)
                            {
                                graphic = _stairs[Page].Graphics[index];
                            }
                        }
                    }

                    if (graphic != 0)
                    {
                        ushort z = (ushort) (World.Items.Get(LocalSerial).Z + 7 + ((CurrentFloor - 1) * 20));
                        TargetManager.SetTargetingMulti(Serial.INVALID, graphic, 0, 0, z, 0, true);
                        CombinedStair = combinedStairs;
                        SelectedGraphic = graphic;
                        Update();
                    }
                }

                return;
            }

            switch (idd)
            {
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_WALL:
                    Category = -1;
                    State = CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL;
                    Page = 0;
                    SelectedGraphic = 0;
                    CombinedStair = false;
                    UpdateMaxPage();
                    TargetManager.CancelTarget();
                    Update();
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_DOOR:
                    Category = -1;
                    State = CUSTOM_HOUSE_GUMP_STATE.CHGS_DOOR;
                    Page = 0;
                    SelectedGraphic = 0;
                    CombinedStair = false;
                    UpdateMaxPage();
                    TargetManager.CancelTarget();
                    Update();
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_FLOOR:
                    Category = -1;
                    State = CUSTOM_HOUSE_GUMP_STATE.CHGS_FLOOR;
                    Page = 0;
                    SelectedGraphic = 0;
                    CombinedStair = false;
                    UpdateMaxPage();
                    TargetManager.CancelTarget();
                    Update();
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_STAIR:
                    Category = -1;
                    State = CUSTOM_HOUSE_GUMP_STATE.CHGS_STAIR;
                    Page = 0;
                    SelectedGraphic = 0;
                    CombinedStair = false;
                    UpdateMaxPage();
                    TargetManager.CancelTarget();
                    Update();
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_ROOF:
                    Category = -1;
                    State = CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF;
                    Page = 0;
                    SelectedGraphic = 0;
                    CombinedStair = false;
                    UpdateMaxPage();
                    TargetManager.CancelTarget();
                    Update();
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_MISC:
                    Category = -1;
                    State = CUSTOM_HOUSE_GUMP_STATE.CHGS_MISC;
                    Page = 0;
                    SelectedGraphic = 0;
                    CombinedStair = false;
                    UpdateMaxPage();
                    TargetManager.CancelTarget();
                    Update();
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_ERASE:
                    // TODO: TARGET
                    Erasing = !Erasing;
                    SelectedGraphic = 0;
                    CombinedStair = false;
                    Update();
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_EYEDROPPER:
                    // TODO: TARGET
                    SeekTile = true;
                    SelectedGraphic = 0;
                    CombinedStair = false;
                    Update();
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_MENU:
                    Category = -1;
                    State = CUSTOM_HOUSE_GUMP_STATE.CHGS_MENU;
                    Page = 0;
                    MaxPage = 1;
                    SelectedGraphic = 0;
                    CombinedStair = false;
                    TargetManager.CancelTarget();
                    Update();
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_1:
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_2:
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_3:
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_4:
                    int selectedFloor = (ID_GUMP_CUSTOM_HOUSE) buttonID - ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_1;

                    _floorVisionState[selectedFloor]++;

                    if (_floorVisionState[selectedFloor] > (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_HIDE_ALL)
                    {
                        _floorVisionState[selectedFloor] = (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_NORMAL;
                    }

                    GenerateFloorPlace();
                    Update();
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_1:
                    CurrentFloor = 1;
                    NetClient.Socket.Send(new PCustomHouseGoToFloor(1));

                    for (int i = 0; i < _floorVisionState.Length; i++)
                        _floorVisionState[i] = (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_NORMAL;

                    if (SelectedGraphic != 0)
                    {
                        TargetManager.MultiTargetInfo.ZOff = (ushort)(World.Items.Get(LocalSerial).Z + 7 + (CurrentFloor - 1) * 20);
                    }

                    Update();
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_2:
                    CurrentFloor = 2;
                    NetClient.Socket.Send(new PCustomHouseGoToFloor(2));

                    for (int i = 0; i < _floorVisionState.Length; i++)
                        _floorVisionState[i] = (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_NORMAL;

                    if (SelectedGraphic != 0)
                    {
                        TargetManager.MultiTargetInfo.ZOff = (ushort) (World.Items.Get(LocalSerial).Z + 7 + (CurrentFloor - 1) * 20);
                    }

                    Update();
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_3:
                    CurrentFloor = 3;
                    NetClient.Socket.Send(new PCustomHouseGoToFloor(3));

                    for (int i = 0; i < _floorVisionState.Length; i++)
                        _floorVisionState[i] = (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_NORMAL;

                    if (SelectedGraphic != 0)
                    {
                        TargetManager.MultiTargetInfo.ZOff = (ushort) (World.Items.Get(LocalSerial).Z + 7 + (CurrentFloor - 1) * 20);
                    }

                    Update();
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_4:
                    CurrentFloor = 4;
                    NetClient.Socket.Send(new PCustomHouseGoToFloor(4));

                    for (int i = 0; i < _floorVisionState.Length; i++)
                        _floorVisionState[i] = (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_NORMAL;

                    if (SelectedGraphic != 0)
                    {
                        TargetManager.MultiTargetInfo.ZOff = (ushort) (World.Items.Get(LocalSerial).Z + 7 + (CurrentFloor - 1) * 20);
                    }

                    Update();
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_LIST_LEFT:
                    Page--;

                    if (Page < 0)
                    {
                        Page = MaxPage - 1;

                        if (Page < 0)
                            Page = 0;
                    }
                    Update();
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_LIST_RIGHT:
                    Page++;

                    if (Page >= MaxPage)
                        Page = 0;

                    Update();
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_BACKUP:
                    NetClient.Socket.Send(new PCustomHouseBackup());
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_RESTORE:
                    NetClient.Socket.Send(new PCustomHouseRestore());
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_SYNCH:
                    NetClient.Socket.Send(new PCustomHouseSync());
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_CLEAR:
                    NetClient.Socket.Send(new PCustomHouseClear());
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_COMMIT:
                    NetClient.Socket.Send(new PCustomHouseCommit());
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_REVERT:
                    NetClient.Socket.Send(new PCustomHouseRevert());
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_CATEGORY:
                    Category = -1;
                    Page = 0;
                    SelectedGraphic = 0;
                    CombinedStair = false;
                    UpdateMaxPage();
                    TargetManager.CancelTarget();
                    Update();
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_WALL_SHOW_WINDOW:
                    ShowWindow = !ShowWindow;
                    Update();
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_ROOF_Z_UP:
                    if (RoofZ < 6)
                    {
                        RoofZ++;
                        Update();
                    }
                    break;
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_ROOF_Z_DOWN:
                    if (RoofZ > 1)
                    {
                        RoofZ--;
                        Update();
                    }
                    break;
            }
        }


        public void GenerateFloorPlace()
        {
            Item foundationItem = World.Items.Get(LocalSerial);

            if (foundationItem != null && World.HouseManager.TryGetHouse(LocalSerial, out var house))
            {
                house.ClearCustomHouseComponents(CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL);

                foreach (Multi item in house.Components)
                {
                    if (!item.IsCustom)
                        continue;

                    int currentFloor = -1;
                    int floorZ = foundationItem.Z + 7;
                    int itemZ = item.Z;

                    bool ignore = false;

                    for (int i = 0; i < 4; i++)
                    {
                        int offset = 0; //i != 0 ? 0 : 7;

                        if (itemZ >= floorZ - offset && itemZ < floorZ + 20)
                        {
                            currentFloor = i;
                            break;
                        }

                        floorZ += 20;
                    }

                    if (currentFloor == -1)
                    {
                        ignore = true;
                        currentFloor = 0;
                        //continue;
                    }

                    (int floorCheck1, int floorCheck2) = SeekGraphicInCustomHouseObjectList(_floors, item.Graphic);

                    var state = item.State;

                    if (floorCheck1 != -1 && floorCheck2 != -1)
                    {
                        state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR;

                        if (_floorVisionState[currentFloor] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_HIDE_FLOOR)
                        {
                            state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER;
                        }
                        else if (_floorVisionState[currentFloor] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_TRANSPARENT_FLOOR ||
                                 _floorVisionState[currentFloor] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_TRANSLUCENT_FLOOR)
                        {
                            state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_TRANSPARENT;
                        }
                    }
                    else
                    {
                        (int stairCheck1, int stairCheck2) = SeekGraphicInCustomHouseObjectList(_stairs, item.Graphic);

                        if (stairCheck1 != -1 && stairCheck2 != -1)
                        {
                            state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR;
                        }
                        else
                        {
                            (int roofCheck1, int roofCheck2) = SeekGraphicInCustomHouseObjectListWithCategory<CustomHouseRoof, CustomHouseRoofCategory>(_roofs, item.Graphic);

                            if (roofCheck1 != -1 && roofCheck2 != -1)
                            {
                                state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF;
                            }
                            else
                            {
                                (int fixtureCheck1, int fixtureCheck2) = SeekGraphicInCustomHouseObjectList(_doors, item.Graphic);

                                if (fixtureCheck1 == -1 || fixtureCheck2 == -1)
                                {
                                    (fixtureCheck1, fixtureCheck2) = SeekGraphicInCustomHouseObjectList(_teleports, item.Graphic);
                                }

                                if (fixtureCheck1 != -1 && fixtureCheck2 != -1)
                                {
                                    state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FIXTURE;
                                }
                            }
                        }

                        if (!ignore)
                        {
                            if (_floorVisionState[currentFloor] == (int)CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_HIDE_CONTENT)
                            {
                                state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER;
                            }
                            else if (_floorVisionState[currentFloor] == (int)CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_TRANSPARENT_CONTENT)
                            {
                                state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_TRANSPARENT;
                            }
                        }
                      
                    }

                    if (!ignore)
                    {
                        if (_floorVisionState[currentFloor] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_HIDE_ALL)
                        {
                            state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER;
                        }
                    }
                    
                    item.State = state;
                }

                int z = foundationItem.Z + 7;

                for (int x = StartPos.X + 1; x < EndPos.X; x++)
                {
                    for (int y = StartPos.Y + 1; y < EndPos.Y; y++)
                    {
                        var multi = house.GetMultiAt(x, y);

                        if (multi == null)
                            continue;

                        Multi floorMulti = null;
                        Multi floorCustomMulti = null;

                        foreach (Multi item in house.Components)
                        {
                            if (item.Z != z || (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) == 0)
                            {
                                continue;
                            }

                            if (item.IsCustom)
                            {
                                floorCustomMulti = item;
                            }
                            else
                            {
                                floorMulti = item;
                            }
                        }

                        if (floorMulti != null && floorCustomMulti == null)
                        {
                            var mo = house.Add(floorMulti.Graphic, 0, x - foundationItem.X, y - foundationItem.Y, (sbyte) z, true);

                            CUSTOM_HOUSE_MULTI_OBJECT_FLAGS state = CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR;

                            if (_floorVisionState[0] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_HIDE_FLOOR)
                            {
                                state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER;
                            }
                            else if (_floorVisionState[0] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_TRANSPARENT_FLOOR ||
                                     _floorVisionState[0] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_TRANSLUCENT_FLOOR)
                            {
                                state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_TRANSPARENT;
                            }

                            mo.State = state;
                        }
                    }
                }

                for (int i = 0; i < FloorCount; i++)
                {
                    int minZ = foundationItem.Z + 7 + i * 20;
                    int maxZ = minZ + 20;

                    for (int j = 0; j < 2; j++)
                    {
                        List<Point> validatedFloors = new List<Point>();

                        for (int x = StartPos.X; x < EndPos.X + 1; x++)
                        {
                            for (int y = StartPos.Y; y < EndPos.Y + 1; y++)
                            {
                                var multi = house.GetMultiAt(x, y);

                                if (multi == null)
                                    continue;

                                foreach (Multi item in house.Components)
                                {
                                    if (!item.IsCustom)
                                        continue;

                                    if (j == 0)
                                    {
                                        if (i == 0 && item.Z < minZ)
                                        {
                                            item.State = item.State | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE;
                                            continue;
                                        }

                                        if (((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) == 0))
                                        {
                                            continue;
                                        }

                                        if (i == 0 && item.Z >= minZ && item.Z < maxZ)
                                        {
                                            item.State = item.State | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE;
                                            continue;
                                        }
                                    }

                                    if (((item.State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL)) == 0) && item.Z >= minZ && item.Z < maxZ)
                                    {
                                        if (!ValidateItemPlace(foundationItem, item, minZ, maxZ, validatedFloors))
                                        {
                                            item.State = item.State | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE;
                                        }
                                        else
                                        {
                                            item.State = item.State | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE;
                                        }
                                    }
                                }
                            }
                        }

                        if (i != 0 && j == 0)
                        {
                            foreach (Point point in validatedFloors)
                            {
                                var multi = house.GetMultiAt(point.X, point.Y);

                                if (multi == null)
                                    continue;

                                foreach (Multi item in house.Components)
                                {
                                    if (item.IsCustom && (((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0) && item.Z >= minZ && item.Z < maxZ))
                                    {
                                        item.State = item.State & ~CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE;
                                    }
                                }
                            }

                            for (int x = StartPos.X; x < EndPos.X + 1; x++)
                            {
                                int minY = 0, maxY = 0;

                                for (int y = StartPos.Y; y < EndPos.Y + 1; y++)
                                {
                                    var multi = house.GetMultiAt(x, y);

                                    if (multi == null)
                                        continue;

                                    foreach (Multi item in house.Components)
                                    {
                                        if (item.IsCustom && ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0) &&
                                            ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0) &&
                                            ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0) &&
                                            item.Z >= minZ && item.Z < maxZ)
                                        {
                                            minY = y;
                                            break;
                                        }
                                    }

                                    if (minY != 0)
                                    {
                                        break;
                                    }
                                }

                                for (int y = EndPos.Y; y >= StartPos.Y; y--)
                                {
                                    var multi = house.GetMultiAt(x, y);
                                    if (multi == null)
                                        continue;

                                    foreach (Multi item in house.Components)
                                    {
                                        if (item.IsCustom && ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0) &&
                                            ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0) &&
                                            ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0) &&
                                            item.Z >= minZ && item.Z < maxZ)
                                        {
                                            maxY = y;
                                            break;
                                        }
                                    }

                                    if (maxY != 0)
                                    {
                                        break;
                                    }
                                }

                                for (int y = minY; y < maxY; y++)
                                {
                                    var multi = house.GetMultiAt(x, y);
                                    if (multi == null)
                                        continue;

                                    foreach (Multi item in house.Components)
                                    {
                                        if (item.IsCustom && ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0) &&
                                            ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0) &&
                                            item.Z >= minZ && item.Z < maxZ)
                                        {
                                            item.State = item.State & ~CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE;
                                        }
                                    }
                                }
                            }

                            for (int y = StartPos.Y; y < EndPos.Y + 1; y++)
                            {
                                int minX = 0;
                                int maxX = 0;

                                for (int x = StartPos.X; x < EndPos.X + 1; x++)
                                {
                                    var multi = house.GetMultiAt(x, y);
                                    if (multi == null)
                                        continue;

                                    foreach (Multi item in house.Components)
                                    {
                                        if (item.IsCustom && ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0) &&
                                            ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0) &&
                                            ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0) &&
                                            item.Z >= minZ && item.Z < maxZ)
                                        {
                                            minX = x;
                                            break;
                                        }
                                    }

                                    if (minX != 0)
                                    {
                                        break;
                                    }
                                }

                                for (int x = EndPos.X; x >= StartPos.X; x--)
                                {
                                    var multi = house.GetMultiAt(x, y);
                                    if (multi == null)
                                        continue;

                                    foreach (Multi item in house.Components)
                                    {
                                        if (item.IsCustom && ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0) &&
                                            ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0) &&
                                            ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0) &&
                                            item.Z >= minZ && item.Z < maxZ)
                                        {
                                            maxX = x;
                                            break;
                                        }
                                    }

                                    if (maxX != 0)
                                    {
                                        break;
                                    }
                                }

                                for (int x = minX; x < maxX; x++)
                                {
                                    var multi = house.GetMultiAt(x, y);
                                    if (multi == null)
                                        continue;

                                    foreach (Multi item in house.Components)
                                    {
                                        if (item.IsCustom && ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0) &&
                                            ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0) &&
                                            item.Z >= minZ && item.Z < maxZ)
                                        {
                                            item.State = item.State & ~CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                z = foundationItem.Z + 7 + 20;

                ushort color = 0x0051;

                for (int i = 1; i < CurrentFloor; i++)
                {
                    for (int x = StartPos.X; x < EndPos.X; x++)
                    {
                        for (int y = StartPos.Y; y < EndPos.Y; y++)
                        {
                            ushort tempColor = color;

                            if (x == StartPos.X || y == StartPos.Y)
                            {
                                tempColor++;
                            }

                            var mo = house.Add(0x0496, tempColor, x - foundationItem.X, y - foundationItem.Y, (sbyte) z, true);

                            mo.State = CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_TRANSPARENT;
                            mo.AddToTile();
                        }
                    }

                    color += 5;
                    z += 20;
                }
            }
        }

        public bool GetBuildZ(ref sbyte z)
        {
            if (SelectedGraphic != 0)
            {
                var foundationItem = World.Items.Get(LocalSerial);

                if (foundationItem == null)
                    return false;

                List<Multi> list = new List<Multi>();

                if (CanBuildHere(list, out var type) && list.Count != 0)
                {
                    if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR)
                    {
                        z = foundationItem.Z;

                        return true;
                    }
                    z = (sbyte) (foundationItem.Z + 7 + (CurrentFloor - 1) * 20);

                    return true;
                }
            }

            return false;
        }

        private void SeekGraphic(ushort graphic)
        {

        }

        public bool CanBuildHere(List<Multi> list, out CUSTOM_HOUSE_BUILD_TYPE type)
        {
            type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_NORMAL;

            if (SelectedGraphic == 0)
                return false;

            if (CombinedStair)
            {
                if (Components + 10 > MaxComponets)
                {
                    return false;
                }

                (int res1, int res2) = SeekGraphicInCustomHouseObjectList(_stairs, SelectedGraphic);

                if (res1 == -1 || res2 == -1 || res1 >= _stairs.Count)
                {
                    Multi m = Multi.Create(SelectedGraphic);
                    list.Add(m);

                    return false;
                }

                var item = _stairs[res1];

                if (SelectedGraphic == item.North)
                {
                    list.Add(new Multi((ushort) item.Block) { MultiOffsetX = 0, MultiOffsetY = -3, MultiOffsetZ = 0});
                    list.Add(new Multi((ushort) item.Block) { MultiOffsetX = 0, MultiOffsetY = -2, MultiOffsetZ = 0 });
                    list.Add(new Multi((ushort) item.Block) { MultiOffsetX = 0, MultiOffsetY = -1, MultiOffsetZ = 0 });
                    list.Add(new Multi((ushort) item.North) { MultiOffsetX = 0, MultiOffsetY = 0, MultiOffsetZ = 0 });
                    list.Add(new Multi((ushort) item.Block) { MultiOffsetX = 0, MultiOffsetY = -3, MultiOffsetZ = 5 });
                    list.Add(new Multi((ushort) item.Block) { MultiOffsetX = 0, MultiOffsetY = -2, MultiOffsetZ = 5 });
                    list.Add(new Multi((ushort) item.North) { MultiOffsetX = 0, MultiOffsetY = -1, MultiOffsetZ = 5 });
                    list.Add(new Multi((ushort) item.Block) { MultiOffsetX = 0, MultiOffsetY = -3, MultiOffsetZ = 10 });
                    list.Add(new Multi((ushort) item.North) { MultiOffsetX = 0, MultiOffsetY = -2, MultiOffsetZ = 10});
                    list.Add(new Multi((ushort) item.North) { MultiOffsetX = 0, MultiOffsetY = -3, MultiOffsetZ = 15 });
                }
                else if (SelectedGraphic == item.East)
                {
                    list.Add(new Multi((ushort) item.East) { MultiOffsetX = 0, MultiOffsetY = 0, MultiOffsetZ = 0 });
                    list.Add(new Multi((ushort) item.Block) { MultiOffsetX = 1, MultiOffsetY = 0, MultiOffsetZ = 0 });
                    list.Add(new Multi((ushort) item.Block) { MultiOffsetX = 2, MultiOffsetY = 0, MultiOffsetZ = 0 });
                    list.Add(new Multi((ushort) item.Block) { MultiOffsetX = 3, MultiOffsetY = 0, MultiOffsetZ = 0 });
                    list.Add(new Multi((ushort) item.East) { MultiOffsetX = 1, MultiOffsetY = 0, MultiOffsetZ = 5 });
                    list.Add(new Multi((ushort) item.Block) { MultiOffsetX = 2, MultiOffsetY = 0, MultiOffsetZ = 5 });
                    list.Add(new Multi((ushort) item.Block) { MultiOffsetX = 3, MultiOffsetY = 0, MultiOffsetZ = 5 });
                    list.Add(new Multi((ushort) item.East) { MultiOffsetX = 2, MultiOffsetY = 0, MultiOffsetZ = 10 });
                    list.Add(new Multi((ushort) item.Block) { MultiOffsetX = 3, MultiOffsetY = 0, MultiOffsetZ = 10 });
                    list.Add(new Multi((ushort) item.East) { MultiOffsetX = 3, MultiOffsetY = 0, MultiOffsetZ = 15 });
                }
                else if (SelectedGraphic == item.South)
                {
                    list.Add(new Multi((ushort) item.South) { MultiOffsetX = 0, MultiOffsetY = 0, MultiOffsetZ = 0 });
                    list.Add(new Multi((ushort) item.Block) { MultiOffsetX = 0, MultiOffsetY = 1, MultiOffsetZ = 0 });
                    list.Add(new Multi((ushort) item.Block) { MultiOffsetX = 0, MultiOffsetY = 2, MultiOffsetZ = 0 });
                    list.Add(new Multi((ushort) item.Block) { MultiOffsetX = 0, MultiOffsetY = 3, MultiOffsetZ = 0 });
                    list.Add(new Multi((ushort) item.South) { MultiOffsetX = 0, MultiOffsetY = 1, MultiOffsetZ = 5 });
                    list.Add(new Multi((ushort) item.Block) { MultiOffsetX = 0, MultiOffsetY = 2, MultiOffsetZ = 5 });
                    list.Add(new Multi((ushort) item.Block) { MultiOffsetX = 0, MultiOffsetY = 3, MultiOffsetZ = 5 });
                    list.Add(new Multi((ushort) item.South) { MultiOffsetX = 0, MultiOffsetY = 2, MultiOffsetZ = 10 });
                    list.Add(new Multi((ushort) item.Block) { MultiOffsetX = 0, MultiOffsetY = 3, MultiOffsetZ = 10 });
                    list.Add(new Multi((ushort) item.South) { MultiOffsetX = 0, MultiOffsetY = 3, MultiOffsetZ = 15 });
                }
                else if (SelectedGraphic == item.West)
                {
                    list.Add(new Multi((ushort) item.Block) { MultiOffsetX = -3, MultiOffsetY = 0, MultiOffsetZ = 0 });
                    list.Add(new Multi((ushort) item.Block) { MultiOffsetX = -2, MultiOffsetY = 0, MultiOffsetZ = 0 });
                    list.Add(new Multi((ushort) item.Block) { MultiOffsetX = -1, MultiOffsetY = 0, MultiOffsetZ = 0 });
                    list.Add(new Multi((ushort) item.West) {  MultiOffsetX = 0,  MultiOffsetY = 0, MultiOffsetZ = 0 });
                    list.Add(new Multi((ushort) item.Block) { MultiOffsetX = -3, MultiOffsetY = 0, MultiOffsetZ = 5 });
                    list.Add(new Multi((ushort) item.Block) { MultiOffsetX = -2, MultiOffsetY = 0, MultiOffsetZ = 5 });
                    list.Add(new Multi((ushort) item.West) {  MultiOffsetX = -1, MultiOffsetY = 0, MultiOffsetZ = 5 });
                    list.Add(new Multi((ushort) item.Block) { MultiOffsetX = -3, MultiOffsetY = 0, MultiOffsetZ = 10 });
                    list.Add(new Multi((ushort) item.West) {  MultiOffsetX = -2, MultiOffsetY = 0, MultiOffsetZ = 10 });
                    list.Add(new Multi((ushort) item.West) {  MultiOffsetX = -3, MultiOffsetY = 0, MultiOffsetZ = 15 });
                }
                else
                {
                    list.Add(new Multi(SelectedGraphic));
                }

                type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR;
            }
            else
            {
                (int fixCheck1, int fixCheck2) = SeekGraphicInCustomHouseObjectList(_doors, SelectedGraphic);

                bool isFixture = false;

                if (fixCheck1 == -1 || fixCheck2 == -1)
                {
                    (fixCheck1, fixCheck2) = SeekGraphicInCustomHouseObjectList(_teleports, SelectedGraphic);

                    isFixture = fixCheck1 != -1 && fixCheck2 != -1;
                }
                else
                {
                    isFixture = true;
                }

                if (isFixture)
                {
                    if (Fixtures + 1 > MaxFixtures)
                    {
                        return false;
                    }
                }
                else if (Components + 1 > MaxComponets)
                {
                    return false;
                }

                if (State == CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF)
                {
                    list.Add(new Multi(SelectedGraphic) { MultiOffsetZ = (RoofZ - 2) * 3});
                    type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_ROOF;
                }
                else
                {
                    if (State == CUSTOM_HOUSE_GUMP_STATE.CHGS_STAIR)
                    {
                        type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR;
                        list.Add(new Multi(SelectedGraphic) { MultiOffsetY = 1 });
                    }
                    else
                    {
                        if (State == CUSTOM_HOUSE_GUMP_STATE.CHGS_FLOOR)
                        {
                            type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_FLOOR;
                        }

                        list.Add(new Multi(SelectedGraphic));
                    }
                }
            }

            if (SelectedObject.Object is GameObject gobj)
            {
                if ((type != CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR || CombinedStair) && gobj.Z < MinHouseZ &&
                    (gobj.X == EndPos.X - 1 || gobj.Y == EndPos.Y - 1))
                {
                    return false;
                }

                Item foundationItem = World.Items.Get(LocalSerial);

                int minZ = (foundationItem?.Z ?? 0) + 7 + (CurrentFloor - 1) * 20;
                int maxZ = minZ + 20;

                int boundsOffset = State != CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL ? 1 : 0;

                Rectangle rect = new Rectangle(StartPos.X + boundsOffset, StartPos.Y + boundsOffset, EndPos.X, EndPos.Y);

                foreach (Multi item in list)
                {
                    if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR)
                    {
                        if (CombinedStair)
                        {
                            if (item.Z != 0)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (gobj.Y + item.MultiOffsetY < EndPos.Y || gobj.X + item.MultiOffsetX == StartPos.X ||
                                gobj.Z >= MinHouseZ)
                            {
                                return false;
                            }

                            if (gobj.Y + item.MultiOffsetY != EndPos.Y)
                            {
                                list[0].MultiOffsetY = 0;
                            }

                            continue;
                        }
                    }

                    if (!ValidateItemPlace(rect, item.Graphic, gobj.X + item.MultiOffsetX, gobj.Y + item.MultiOffsetY))
                    {
                        return false;
                    }

                    if (type != CUSTOM_HOUSE_BUILD_TYPE.CHBT_FLOOR && foundationItem != null && World.HouseManager.TryGetHouse(LocalSerial, out var house))
                    {
                        var multi = house.GetMultiAt(gobj.X + item.MultiOffsetX, gobj.Y + item.MultiOffsetY);

                        if (multi != null)
                        {
                            foreach (Multi multiObject in house.Components)
                            {
                                if (multiObject.IsCustom && (((multiObject.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL) == 0) &&
                                                             multiObject.Z >= minZ && multiObject.Z < maxZ))
                                {
                                    if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR)
                                    {
                                        if ((multiObject.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) == 0)
                                        {
                                            return false;
                                        }
                                    }
                                    else
                                    {
                                        if ((multiObject.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR) != 0)
                                        {
                                            return false;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        private bool ValidateItemPlace(Rectangle rect, ushort graphic , int x, int y)
        {
            if (!rect.Contains(x, y))
                return false;

            (int infoCheck1, int infoCheck2) = SeekGraphicInCustomHouseObjectList(_objectsInfo, graphic);

            if (infoCheck1 != -1 && infoCheck2 != -1)
            {
                var info = _objectsInfo[infoCheck1];

                if (info.CanGoW == 0 && x == StartPos.X)
                    return false;

                if (info.CanGoN == 0 && y == StartPos.Y)
                    return false;

                if (info.CanGoNWS == 0 && x == StartPos.X && y == StartPos.Y)
                    return false;
            }
            
            return true;
        }

        private bool ValidateItemPlace(Item foundationItem, Multi item, int minZ, int maxZ,
            List<Point> validatedFloors)
        {

            if (item == null || !World.HouseManager.TryGetHouse(foundationItem, out var house) || !item.IsCustom)
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

                if (ValidatePlaceStructure(
                        foundationItem,
                        house,
                        house.GetMultiAt(item.X, item.Y),
                        minZ - 20,
                        maxZ - 20,
                        (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_DIRECT_SUPPORT) ||

                    ValidatePlaceStructure(
                        foundationItem,
                        house,
                        house.GetMultiAt(item.X - 1, item.Y - 1),
                        minZ - 20,
                        maxZ - 20,
                        (int) (CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_DIRECT_SUPPORT | CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_CANGO_W )) ||

                    ValidatePlaceStructure(
                        foundationItem,
                        house,
                        house.GetMultiAt(item.X, item.Y - 1),
                        minZ - 20,
                        maxZ - 20,
                        (int)(CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_DIRECT_SUPPORT | CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_CANGO_N)))
                {
                    Point[] table =
                    {
                        new Point(-1, 0),
                        new Point(0, -1),
                        new Point(1, 0),
                        new Point(0, 1)
                    };

                    for (int i = 0; i < 4; i++)
                    {
                        Point testPoint = new Point(item.X + table[i].X,
                            item.Y + table[i].Y);

                        if (!existsInList(validatedFloors, testPoint))
                        {
                            validatedFloors.Add(testPoint);
                        }
                    }

                    return true;
                }

                return false;
            }

            if ((item.State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR |
                               CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF |
                               CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FIXTURE)) != 0)
            {
                foreach (Multi temp in house.Components)
                {
                    if ((temp.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0 &&
                        temp.Z >= minZ && temp.Z < maxZ)
                    {
                        if ((temp.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0 &&
                            (temp.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0)
                        {
                            return true;
                        }
                    }
                }

                // TODO ?

                return false;
            }


            (int infoCheck1, int infoCheck2) = SeekGraphicInCustomHouseObjectList(_objectsInfo, item.Graphic);

            if (infoCheck1 != -1 && infoCheck2 != -1)
            {
                var info = _objectsInfo[infoCheck1];

                if (info.CanGoW == 0 && item.X == StartPos.X)
                    return false;
                if (info.CanGoN == 0 && item.Y == StartPos.Y)
                    return false;
                if (info.CanGoNWS == 0 && item.X == StartPos.X && item.Y == StartPos.Y)
                    return false;

                if (info.Bottom == 0)
                {
                    bool found = false;

                    if (info.AdjUN != 0)
                    {
                        found = ValidatePlaceStructure(foundationItem, house, house.GetMultiAt(item.X, item.Y + 1), minZ,
                            maxZ,
                            (int) (CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_BOTTOM |
                                   CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_N));
                    }

                    if (!found && info.AdjUE != 0)
                    {
                        found = ValidatePlaceStructure(foundationItem, house, house.GetMultiAt(item.X - 1, item.Y), minZ,
                            maxZ,
                            (int)(CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_BOTTOM |
                                  CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_E));
                    }

                    if (!found && info.AdjUS != 0)
                    {
                        found = ValidatePlaceStructure(foundationItem, house, house.GetMultiAt(item.X, item.Y - 1), minZ,
                            maxZ,
                            (int)(CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_BOTTOM |
                                  CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_S));
                    }

                    if (!found && info.AdjUW != 0)
                    {
                        found = ValidatePlaceStructure(foundationItem, house, house.GetMultiAt(item.X + 1, item.Y), minZ,
                            maxZ,
                            (int)(CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_BOTTOM |
                                  CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_W));
                    }

                    if (!found)
                        return false;
                }

                if (info.Top == 0)
                {
                    bool found = false;

                    if (info.AdjLN != 0)
                    {
                        found = ValidatePlaceStructure(foundationItem, house, house.GetMultiAt(item.X, item.Y + 1), minZ,
                            maxZ,
                            (int)(CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_TOP |
                                  CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_N));
                    }

                    if (!found && info.AdjLE != 0)
                    {
                        found = ValidatePlaceStructure(foundationItem, house, house.GetMultiAt(item.X - 1, item.Y), minZ,
                            maxZ,
                            (int)(CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_TOP |
                                  CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_E));
                    }

                    if (!found && info.AdjLS != 0)
                    {
                        found = ValidatePlaceStructure(foundationItem, house, house.GetMultiAt(item.X, item.Y - 1), minZ,
                            maxZ,
                            (int)(CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_TOP |
                                  CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_S));
                    }

                    if (!found && info.AdjLW != 0)
                    {
                        found = ValidatePlaceStructure(foundationItem, house, house.GetMultiAt(item.X + 1, item.Y), minZ,
                            maxZ,
                            (int)(CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_TOP |
                                  CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_W));
                    }

                    if (!found)
                        return false;
                }
            }

            return true;
        }

        private bool CanEraseHere(GameObject place, ref CUSTOM_HOUSE_BUILD_TYPE type)
        {
            type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_NORMAL;

            if (place != null && place is Multi multi)
            {
                if (multi.IsCustom && (multi.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL) == 0)
                {
                    if ((multi.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0)
                    {
                        type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_FLOOR;
                    }
                    else if ((multi.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR) != 0)
                    {
                        type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR;
                    }
                    else if ((multi.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF) != 0)
                    {
                        type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_ROOF;
                    }

                    return true;
                }
            }

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

    [Flags]
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
