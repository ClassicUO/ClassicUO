// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Assets;
using ClassicUO.Network;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;
using ClassicUO.Game.Scenes;

namespace ClassicUO.Game.UI.Gumps
{
    internal class HouseCustomizationGump : Gump
    {
        private readonly HouseCustomizationManager _customHouseManager;
        private readonly DataBox _dataBox;
        private readonly DataBox _dataBoxGUI;
        private readonly GumpPic _gumpPic;
        private readonly Label _textComponents;
        private readonly Label _textCost;
        private readonly Label _textFixtures;

        public HouseCustomizationGump(World world, uint serial, int x, int y) : base(world, serial, 0)
        {
            X = x;
            Y = y;
            CanMove = true;
            AcceptMouseInput = false;
            CanCloseWithRightClick = true;
            _customHouseManager = new HouseCustomizationManager(world, serial);
            World.CustomHouseManager = _customHouseManager;
            SetOtherHousesState(false);

            Add(new GumpPicTiled(121, 36, 397, 120, 0x0E14));

            _dataBox = new DataBox(0, 0, 0, 0)
            {
                WantUpdateSize = true,
                CanMove = false,
                AcceptMouseInput = false
            };

            Add(_dataBox);

            Add(new GumpPic(0, 17, 0x55F0, 0));

            _gumpPic = new GumpPic(
                486,
                17,
                (ushort)(_customHouseManager.FloorCount == 4 ? 0x55F2 : 0x55F9),
                0
            );
            Add(_gumpPic);

            Add(new GumpPicTiled(153, 17, 333, 154, 0x55F1));

            Button button = new Button(
                (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_WALL,
                0x5654,
                0x5656,
                0x5655
            )
            {
                X = 9,
                Y = 41,
                ButtonAction = ButtonAction.Activate
            };

            button.SetTooltip(ResGumps.Walls);
            Add(button);

            button = new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_DOOR, 0x5657, 0x5659, 0x5658)
            {
                X = 39,
                Y = 40,
                ButtonAction = ButtonAction.Activate
            };

            button.SetTooltip(ResGumps.Doors);
            Add(button);

            button = new Button(
                (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_FLOOR,
                0x565A,
                0x565C,
                0x565B
            )
            {
                X = 70,
                Y = 40,
                ButtonAction = ButtonAction.Activate
            };

            button.SetTooltip(ResGumps.Floors);
            Add(button);

            button = new Button(
                (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_STAIR,
                0x565D,
                0x565F,
                0x565E
            )
            {
                X = 9,
                Y = 72,
                ButtonAction = ButtonAction.Activate
            };

            button.SetTooltip(ResGumps.Stairs);
            Add(button);

            button = new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_ROOF, 0x5788, 0x578A, 0x5789)
            {
                X = 39,
                Y = 72,
                ButtonAction = ButtonAction.Activate
            };

            button.SetTooltip(ResGumps.Roofs);
            Add(button);

            button = new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_MISC, 0x5663, 0x5665, 0x5664)
            {
                X = 69,
                Y = 72,
                ButtonAction = ButtonAction.Activate
            };

            button.SetTooltip(ResGumps.Miscellaneous);
            Add(button);

            button = new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_MENU, 0x566C, 0x566E, 0x566D)
            {
                X = 69,
                Y = 100,
                ButtonAction = ButtonAction.Activate
            };

            button.SetTooltip(ResGumps.SystemMenu);
            Add(button);

            _textComponents = new Label(string.Empty, false, 0x0481, font: 9)
            {
                X = 82,
                Y = 142,
                AcceptMouseInput = true
            };

            Add(_textComponents);

            Label text = new Label(":", false, 0x0481, font: 9) { X = 84, Y = 142 };

            Add(text);

            _textFixtures = new Label(string.Empty, false, 0x0481, font: 9)
            {
                X = 94,
                Y = 142,
                AcceptMouseInput = true
            };

            Add(_textFixtures);

            _textCost = new Label(string.Empty, false, 0x0481, font: 9)
            {
                X = 524,
                Y = 142,
                AcceptMouseInput = true
            };

            _textCost.SetTooltip(ResGumps.Cost);
            Add(_textCost);

            //HitBox box = new HitBox(36, 137, 84, 23)
            //{
            //    Priority = ClickPriority.Default
            //};
            //Add(box);

            //HitBox box = new HitBox(522, 137, 84, 23)
            //{
            //    Priority = ClickPriority.Default
            //};
            //Add(box);

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

        private void SetOtherHousesState(bool visible)
        {
            foreach (var multi in World.HouseManager.Houses.Where(s => s.Serial != LocalSerial)
                                       .SelectMany(s => s.Components))
            {
                if (visible)
                    multi.State &= ~CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER;
                else
                    multi.State |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER;
            }
        }

        public new void Update()
        {
            _dataBox.Clear();
            _dataBoxGUI.Clear();

            _gumpPic.Graphic = (ushort)(_customHouseManager.FloorCount == 4 ? 0x55F2 : 0x55F9);

            Button button = new Button(
                (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_ERASE,
                (ushort)(0x5666 + (_customHouseManager.Erasing ? 1 : 0)),
                0x5668,
                0x5667
            )
            {
                X = 9,
                Y = 100,
                ButtonAction = ButtonAction.Activate
            };

            button.SetTooltip(ResGumps.Erase);
            _dataBoxGUI.Add(button);

            button = new Button(
                (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_EYEDROPPER,
                (ushort)(0x5669 + (_customHouseManager.SeekTile ? 1 : 0)),
                0x566B,
                0x566A
            )
            {
                X = 39,
                Y = 100,
                ButtonAction = ButtonAction.Activate
            };

            button.SetTooltip(ResGumps.EyedropperTool);
            Add(button);

            ushort[] floorVisionGraphic1 = { 0x572E, 0x5734, 0x5731 };
            ushort[] floorVisionGraphic2 = { 0x5725, 0x5728, 0x572B };
            ushort[] floorVisionGraphic3 = { 0x571C, 0x571F, 0x5722 };

            int[] associateGraphicTable = { 0, 1, 2, 1, 2, 1, 2 };

            ushort floorVisionGraphic = floorVisionGraphic1[
                associateGraphicTable[_customHouseManager.FloorVisionState[0]]
            ];

            int graphicOffset = _customHouseManager.CurrentFloor == 1 ? 3 : 0;
            int graphicOffset2 = _customHouseManager.CurrentFloor == 1 ? 4 : 0;

            button = new Button(
                (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_1,
                floorVisionGraphic,
                (ushort)(floorVisionGraphic + 2),
                (ushort)(floorVisionGraphic + 1)
            )
            {
                X = 533,
                Y = 108,
                ButtonAction = ButtonAction.Activate
            };

            button.SetTooltip(string.Format(ResGumps.Store0Visibility, 1));
            _dataBoxGUI.Add(button);

            button = new Button(
                (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_1,
                (ushort)(0x56CD + graphicOffset2),
                0x56D1,
                (ushort)(0x56CD + graphicOffset2)
            )
            {
                X = 583,
                Y = 96,
                ButtonAction = ButtonAction.Activate
            };

            button.SetTooltip(string.Format(ResGumps.GoToStory0, 1));
            _dataBoxGUI.Add(button);

            button = new Button(
                (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_1,
                (ushort)(0x56F6 + graphicOffset),
                (ushort)(0x56F8 + graphicOffset),
                (ushort)(0x56F7 + graphicOffset)
            )
            {
                X = 623,
                Y = 103,
                ButtonAction = ButtonAction.Activate
            };

            button.SetTooltip(string.Format(ResGumps.GoToStory0, 1));
            _dataBoxGUI.Add(button);

            floorVisionGraphic = floorVisionGraphic2[
                associateGraphicTable[_customHouseManager.FloorVisionState[1]]
            ];
            graphicOffset = _customHouseManager.CurrentFloor == 2 ? 3 : 0;
            graphicOffset2 = _customHouseManager.CurrentFloor == 2 ? 4 : 0;

            button = new Button(
                (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_2,
                floorVisionGraphic,
                (ushort)(floorVisionGraphic + 2),
                (ushort)(floorVisionGraphic + 1)
            )
            {
                X = 533,
                Y = 86,
                ButtonAction = ButtonAction.Activate
            };

            button.SetTooltip(string.Format(ResGumps.Store0Visibility, 2));
            _dataBoxGUI.Add(button);

            button = new Button(
                (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_2,
                (ushort)(0x56CE + graphicOffset2),
                0x56D2,
                (ushort)(0x56CE + graphicOffset2)
            )
            {
                X = 583,
                Y = 73,
                ButtonAction = ButtonAction.Activate
            };

            button.SetTooltip(string.Format(ResGumps.GoToStory0, 2));
            _dataBoxGUI.Add(button);

            button = new Button(
                (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_2,
                (ushort)(0x56F0 + graphicOffset),
                (ushort)(0x56F2 + graphicOffset),
                (ushort)(0x56F1 + graphicOffset)
            )
            {
                X = 623,
                Y = 86,
                ButtonAction = ButtonAction.Activate
            };

            button.SetTooltip(string.Format(ResGumps.GoToStory0, 2));
            _dataBoxGUI.Add(button);

            graphicOffset = _customHouseManager.CurrentFloor == 3 ? 3 : 0;
            graphicOffset2 = _customHouseManager.CurrentFloor == 3 ? 4 : 0;

            if (_customHouseManager.FloorCount == 4)
            {
                floorVisionGraphic = floorVisionGraphic2[
                    associateGraphicTable[_customHouseManager.FloorVisionState[2]]
                ];

                button = new Button(
                    (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_3,
                    floorVisionGraphic,
                    (ushort)(floorVisionGraphic + 2),
                    (ushort)(floorVisionGraphic + 1)
                )
                {
                    X = 533,
                    Y = 64,
                    ButtonAction = ButtonAction.Activate
                };

                button.SetTooltip(string.Format(ResGumps.Store0Visibility, 3));
                _dataBoxGUI.Add(button);

                button = new Button(
                    (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_3,
                    (ushort)(0x56CE + graphicOffset2),
                    0x56D2,
                    (ushort)(0x56CE + graphicOffset2)
                )
                {
                    X = 582,
                    Y = 56,
                    ButtonAction = ButtonAction.Activate
                };

                button.SetTooltip(string.Format(ResGumps.GoToStory0, 3));
                _dataBoxGUI.Add(button);

                button = new Button(
                    (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_3,
                    (ushort)(0x56F0 + graphicOffset),
                    (ushort)(0x56F2 + graphicOffset),
                    (ushort)(0x56F1 + graphicOffset)
                )
                {
                    X = 623,
                    Y = 69,
                    ButtonAction = ButtonAction.Activate
                };

                button.SetTooltip(string.Format(ResGumps.GoToStory0, 3));
                _dataBoxGUI.Add(button);

                floorVisionGraphic = floorVisionGraphic2[
                    associateGraphicTable[_customHouseManager.FloorVisionState[3]]
                ];

                graphicOffset = _customHouseManager.CurrentFloor == 4 ? 3 : 0;
                graphicOffset2 = _customHouseManager.CurrentFloor == 4 ? 4 : 0;

                button = new Button(
                    (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_4,
                    floorVisionGraphic,
                    (ushort)(floorVisionGraphic + 2),
                    (ushort)(floorVisionGraphic + 1)
                )
                {
                    X = 533,
                    Y = 42,
                    ButtonAction = ButtonAction.Activate
                };

                button.SetTooltip(string.Format(ResGumps.Store0Visibility, 4));
                _dataBoxGUI.Add(button);

                button = new Button(
                    (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_4,
                    (ushort)(0x56D0 + graphicOffset2),
                    0x56D4,
                    (ushort)(0x56D0 + graphicOffset2)
                )
                {
                    X = 583,
                    Y = 42,
                    ButtonAction = ButtonAction.Activate
                };

                button.SetTooltip(string.Format(ResGumps.GoToStory0, 4));
                _dataBoxGUI.Add(button);

                button = new Button(
                    (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_4,
                    (ushort)(0x56EA + graphicOffset),
                    (ushort)(0x56EC + graphicOffset),
                    (ushort)(0x56EB + graphicOffset)
                )
                {
                    X = 623,
                    Y = 50,
                    ButtonAction = ButtonAction.Activate
                };

                button.SetTooltip(string.Format(ResGumps.GoToStory0, 4));
                _dataBoxGUI.Add(button);
            }
            else
            {
                floorVisionGraphic = floorVisionGraphic2[
                    associateGraphicTable[_customHouseManager.FloorVisionState[2]]
                ];

                button = new Button(
                    (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_3,
                    floorVisionGraphic,
                    (ushort)(floorVisionGraphic + 2),
                    (ushort)(floorVisionGraphic + 1)
                )
                {
                    X = 533,
                    Y = 64,
                    ButtonAction = ButtonAction.Activate
                };

                button.SetTooltip(string.Format(ResGumps.Store0Visibility, 3));
                _dataBoxGUI.Add(button);

                button = new Button(
                    (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_3,
                    (ushort)(0x56D0 + graphicOffset2),
                    0x56D4,
                    (ushort)(0x56D0 + graphicOffset2)
                )
                {
                    X = 582,
                    Y = 56,
                    ButtonAction = ButtonAction.Activate
                };

                button.SetTooltip(string.Format(ResGumps.GoToStory0, 3));
                _dataBoxGUI.Add(button);

                button = new Button(
                    (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_3,
                    (ushort)(0x56EA + graphicOffset),
                    (ushort)(0x56EC + graphicOffset),
                    (ushort)(0x56EB + graphicOffset)
                )
                {
                    X = 623,
                    Y = 69,
                    ButtonAction = ButtonAction.Activate
                };

                button.SetTooltip(string.Format(ResGumps.GoToStory0, 3));
                _dataBoxGUI.Add(button);
            }

            switch (_customHouseManager.State)
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

            if (_customHouseManager.MaxPage > 1)
            {
                button = new Button(
                    (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_LIST_LEFT,
                    0x5625,
                    0x5627,
                    0x5626
                )
                {
                    X = 110,
                    Y = 63,
                    ButtonAction = ButtonAction.Activate
                };

                button.SetTooltip(ResGumps.PreviousPage);
                _dataBoxGUI.Add(button);

                button = new Button(
                    (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_LIST_RIGHT,
                    0x5628,
                    0x562A,
                    0x5629
                )
                {
                    X = 510,
                    Y = 63,
                    ButtonAction = ButtonAction.Activate
                };

                button.SetTooltip(ResGumps.NextPage);
                _dataBoxGUI.Add(button);
            }

            _customHouseManager.Components = 0;
            _customHouseManager.Fixtures = 0;

            Item foundationItem = World.Items.Get(LocalSerial);

            if (foundationItem != null)
            {
                if (World.HouseManager.TryGetHouse(LocalSerial, out House house))
                {
                    foreach (Multi item in house.Components)
                    {
                        if (
                            item.IsCustom
                            && (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL)
                                == 0
                        )
                        {
                            CUSTOM_HOUSE_GUMP_STATE state = 0;

                            (int res1, int res2) = _customHouseManager.ExistsInList(
                                ref state,
                                item.Graphic
                            );

                            if (res1 != -1 && res2 != -1)
                            {
                                if (
                                    state == CUSTOM_HOUSE_GUMP_STATE.CHGS_DOOR
                                    || state == CUSTOM_HOUSE_GUMP_STATE.CHGS_FIXTURE
                                )
                                {
                                    _customHouseManager.Fixtures++;
                                }
                                else
                                {
                                    _customHouseManager.Components++;
                                }
                            }
                        }
                    }
                }
            }

            _textComponents.Hue = (ushort)(
                _customHouseManager.Components >= _customHouseManager.MaxComponets ? 0x0026 : 0x0481
            );

            _textComponents.Text = _customHouseManager.Components.ToString();
            _textComponents.X = 82 - _textComponents.Width;

            _textFixtures.Hue = (ushort)(
                _customHouseManager.Fixtures >= _customHouseManager.MaxFixtures ? 0x0026 : 0x0481
            );

            _textFixtures.Text = _customHouseManager.Fixtures.ToString();

            string tooltip = Client.Game.UO.FileManager.Clilocs.Translate(
                1061039,
                $"{_customHouseManager.MaxComponets}\t{_customHouseManager.MaxFixtures}",
                true
            );

            _textComponents.SetTooltip(tooltip);
            _textFixtures.SetTooltip(tooltip);

            _textCost.Text = (
                (_customHouseManager.Components + _customHouseManager.Fixtures) * 500
            ).ToString();
        }

        public void UpdateMaxPage()
        {
            _customHouseManager.MaxPage = 1;

            switch (_customHouseManager.State)
            {
                case CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL:
                    if (_customHouseManager.Category == -1)
                    {
                        _customHouseManager.MaxPage = (int)
                            Math.Ceiling(World.CustomHouseManager.Walls.Count / 16.0f);
                    }
                    else
                    {
                        foreach (CustomHouseWallCategory c in World.CustomHouseManager.Walls)
                        {
                            if (c.Index == _customHouseManager.Category)
                            {
                                _customHouseManager.MaxPage = c.Items.Count;

                                break;
                            }
                        }
                    }

                    break;

                case CUSTOM_HOUSE_GUMP_STATE.CHGS_DOOR:
                    _customHouseManager.MaxPage = World.CustomHouseManager.Doors.Count;

                    break;

                case CUSTOM_HOUSE_GUMP_STATE.CHGS_FLOOR:
                    _customHouseManager.MaxPage = World.CustomHouseManager.Floors.Count;

                    break;

                case CUSTOM_HOUSE_GUMP_STATE.CHGS_STAIR:
                    _customHouseManager.MaxPage = World.CustomHouseManager.Stairs.Count;

                    break;

                case CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF:
                    if (_customHouseManager.Category == -1)
                    {
                        _customHouseManager.MaxPage = (int)
                            Math.Ceiling(World.CustomHouseManager.Roofs.Count / 16.0f);
                    }
                    else
                    {
                        foreach (CustomHouseRoofCategory c in World.CustomHouseManager.Roofs)
                        {
                            if (c.Index == _customHouseManager.Category)
                            {
                                _customHouseManager.MaxPage = c.Items.Count;

                                break;
                            }
                        }
                    }

                    break;

                case CUSTOM_HOUSE_GUMP_STATE.CHGS_MISC:
                    if (_customHouseManager.Category == -1)
                    {
                        _customHouseManager.MaxPage = (int)
                            Math.Ceiling(World.CustomHouseManager.Miscs.Count / 16.0f);
                    }
                    else
                    {
                        foreach (CustomHouseMiscCategory c in World.CustomHouseManager.Miscs)
                        {
                            if (c.Index == _customHouseManager.Category)
                            {
                                _customHouseManager.MaxPage = c.Items.Count;

                                break;
                            }
                        }
                    }

                    break;
            }
        }

        private void AddWall()
        {
            int x = 0,
                y = 0;

            if (_customHouseManager.Category == -1)
            {
                int startCategory = Page * 16;
                int endCategory = startCategory + 16;

                if (endCategory > World.CustomHouseManager.Walls.Count)
                {
                    endCategory = World.CustomHouseManager.Walls.Count;
                }

                _dataBox.Add(new ScissorControl(true, 121, 36, 384, 60));

                for (int i = startCategory; i < endCategory; i++)
                {
                    List<CustomHouseWall> vec = World.CustomHouseManager.Walls[i].Items;

                    if (vec.Count == 0)
                    {
                        continue;
                    }

                    ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt((uint)vec[0].East1);

                    int offsetX = x + 121 + (48 - artInfo.UV.Width) / 2;
                    int offsetY = y + 36;

                    StaticPic pic = new StaticPic((ushort)vec[0].East1, 0)
                    {
                        X = offsetX,
                        Y = offsetY,
                        CanMove = false,
                        LocalSerial = (uint)(ID_GUMP_CUSTOM_HOUSE.ID_GCH_ITEM_IN_LIST + i),
                        Height = artInfo.UV.Height < 60 ? artInfo.UV.Height : 60
                    };

                    pic.MouseUp += (sender, e) =>
                    {
                        OnButtonClick((int)pic.LocalSerial);
                    };
                    _dataBox.Add(pic);

                    x += 48;

                    if (x >= 384)
                    {
                        x = 0;
                        y += 60;

                        _dataBox.Add(new ScissorControl(false));

                        _dataBox.Add(new ScissorControl(true, 121, 96, 384, 60));
                    }
                }

                // disable scissor
                _dataBox.Add(new ScissorControl(false));
            }
            else if (
                _customHouseManager.Category >= 0
                && _customHouseManager.Category < World.CustomHouseManager.Walls.Count
            )
            {
                List<CustomHouseWall> vec = World.CustomHouseManager.Walls[
                    _customHouseManager.Category
                ].Items;

                if (Page >= 0 && Page < vec.Count)
                {
                    CustomHouseWall item = vec[Page];

                    // add scissor
                    _dataBox.Add(new ScissorControl(true, 121, 36, 384, 120));

                    for (int i = 0; i < 8; i++)
                    {
                        ushort graphic = _customHouseManager.ShowWindow
                            ? item.WindowGraphics[i]
                            : item.Graphics[i];

                        if (graphic != 0)
                        {
                            ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(graphic);

                            int offsetX = x + 130 + (48 - artInfo.UV.Width) / 2;
                            int offsetY = y + 36 + (120 - artInfo.UV.Height) / 2;

                            StaticPic pic = new StaticPic(graphic, 0)
                            {
                                X = offsetX,
                                Y = offsetY,
                                CanMove = false,
                                LocalSerial = (uint)(ID_GUMP_CUSTOM_HOUSE.ID_GCH_ITEM_IN_LIST + i),
                                // Height = 120
                            };

                            pic.MouseUp += (sender, e) =>
                            {
                                OnButtonClick((int)pic.LocalSerial);
                            };
                            _dataBox.Add(pic);
                        }

                        x += 48;
                    }

                    // remove scissor
                    _dataBox.Add(new ScissorControl(false));
                }

                _dataBoxGUI.Add(new GumpPic(152, 0, 0x55F3, 0));

                Button button = new Button(
                    (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_CATEGORY,
                    0x5622,
                    0x5624,
                    0x5623
                )
                {
                    X = 167,
                    Y = 5,
                    ButtonAction = ButtonAction.Activate
                };

                button.SetTooltip(ResGumps.ToCustomHouseManagerCategory);
                _dataBoxGUI.Add(button);

                _dataBoxGUI.Add(new GumpPic(218, 4, 0x55F4, 0));

                if (_customHouseManager.ShowWindow)
                {
                    button = new Button(
                        (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_WALL_SHOW_WINDOW,
                        0x562E,
                        0x5630,
                        0x562F
                    )
                    {
                        X = 228,
                        Y = 9,
                        ButtonAction = ButtonAction.Activate
                    };

                    button.SetTooltip(ResGumps.WindowToggle);
                    _dataBoxGUI.Add(button);
                }
                else
                {
                    button = new Button(
                        (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_WALL_SHOW_WINDOW,
                        0x562B,
                        0x562D,
                        0x562C
                    )
                    {
                        X = 228,
                        Y = 9,
                        ButtonAction = ButtonAction.Activate
                    };

                    button.SetTooltip(ResGumps.WindowToggle);
                    _dataBoxGUI.Add(button);
                }
            }
        }

        private void AddDoor()
        {
            if (Page >= 0 && Page < World.CustomHouseManager.Doors.Count)
            {
                CustomHouseDoor item = World.CustomHouseManager.Doors[Page];

                int x = 0,
                    y = 0;

                // add scissor
                _dataBox.Add(new ScissorControl(true, 138, 36, 384, 120));

                for (int i = 0; i < 8; i++)
                {
                    ushort graphic = item.Graphics[i];

                    if (graphic != 0)
                    {
                        ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(graphic);

                        int offsetX = x + 138 + (48 - artInfo.UV.Width) / 2;

                        if (i > 3)
                        {
                            offsetX -= 20;
                        }

                        int offsetY = y + 36 + (120 - artInfo.UV.Height) / 2;

                        StaticPic pic = new StaticPic(graphic, 0)
                        {
                            X = offsetX,
                            Y = offsetY,
                            CanMove = false,
                            LocalSerial = (uint)(ID_GUMP_CUSTOM_HOUSE.ID_GCH_ITEM_IN_LIST + i),
                            // Height = 120
                        };

                        pic.MouseUp += (sender, e) =>
                        {
                            OnButtonClick((int)pic.LocalSerial);
                        };
                        _dataBox.Add(pic);
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
                _dataBox.Add(new ScissorControl(false));
            }
        }

        private void AddFloor()
        {
            if (Page >= 0 && Page < World.CustomHouseManager.Floors.Count)
            {
                CustomHouseFloor item = World.CustomHouseManager.Floors[Page];

                int x = 0,
                    y = 0;

                // add scissor
                _dataBox.Add(new ScissorControl(true, 123, 36, 384, 120));

                int index = 0;

                for (int j = 0; j < 2; j++)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        ushort graphic = item.Graphics[index];

                        if (graphic != 0)
                        {
                            ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(graphic);

                            int offsetX = x + 123 + (48 - artInfo.UV.Width) / 2;
                            int offsetY = y + 36 + (60 - artInfo.UV.Height) / 2;

                            StaticPic pic = new StaticPic(graphic, 0)
                            {
                                X = offsetX,
                                Y = offsetY,
                                CanMove = false,
                                LocalSerial = (uint)(
                                    ID_GUMP_CUSTOM_HOUSE.ID_GCH_ITEM_IN_LIST + index
                                )
                                //Height = 120
                            };

                            pic.MouseUp += (sender, e) =>
                            {
                                OnButtonClick((int)pic.LocalSerial);
                            };
                            _dataBox.Add(pic);
                        }

                        x += 48;
                        index++;
                    }

                    x = 0;
                    y += 60;
                }

                // remove scissor
                _dataBox.Add(new ScissorControl(false));
            }
        }

        private void AddStair()
        {
            if (Page >= 0 && Page < World.CustomHouseManager.Stairs.Count)
            {
                CustomHouseStair item = World.CustomHouseManager.Stairs[Page];

                for (int j = 0; j < 2; j++)
                {
                    int x = j != 0 ? 96 : 192;
                    int y = j != 0 ? 60 : 0;

                    // add scissor
                    _dataBox.Add(new ScissorControl(true, 121, 36 + y, 384, 60));

                    Label text = new Label(
                        Client.Game.UO.FileManager.Clilocs.GetString(1062113 + j),
                        true,
                        0xFFFF,
                        90,
                        0
                    )
                    {
                        X = 137,
                        Y = j != 0 ? 111 : 51
                    };

                    _dataBox.Add(text);

                    int start = j != 0 ? 0 : 5;
                    int end = j != 0 ? 6 : 9;
                    int combinedStair = j != 0 ? 0 : 10;

                    for (int i = start; i < end; i++)
                    {
                        ushort graphic = item.Graphics[i];

                        if (graphic != 0)
                        {
                            ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(graphic);

                            int offsetX = x + 123 + (48 - artInfo.UV.Width) / 2;
                            int offsetY = y + 36 + (60 - artInfo.UV.Height) / 2;

                            StaticPic pic = new StaticPic(graphic, 0)
                            {
                                X = offsetX,
                                Y = offsetY,
                                CanMove = false,
                                LocalSerial = (uint)(
                                    ID_GUMP_CUSTOM_HOUSE.ID_GCH_ITEM_IN_LIST + i + combinedStair
                                ),
                                Height = 60
                            };

                            pic.MouseUp += (sender, e) =>
                            {
                                OnButtonClick((int)pic.LocalSerial);
                            };
                            _dataBox.Add(pic);
                        }

                        x += 48;
                    }

                    // remove scissor
                    _dataBox.Add(new ScissorControl(false));
                }

                _dataBox.Add(new ColorBox(384, 2, 0) { X = 123, Y = 96 });
            }
        }

        private void AddRoof()
        {
            int x = 0,
                y = 0;

            if (_customHouseManager.Category == -1)
            {
                int startCategory = Page * 16;
                int endCategory = startCategory + 16;

                if (endCategory > World.CustomHouseManager.Roofs.Count)
                {
                    endCategory = World.CustomHouseManager.Roofs.Count;
                }

                // push scissor
                _dataBox.Add(new ScissorControl(true, 121, 36, 384, 60));

                for (int i = startCategory; i < endCategory; i++)
                {
                    List<CustomHouseRoof> vec = World.CustomHouseManager.Roofs[i].Items;

                    if (vec.Count == 0)
                    {
                        continue;
                    }

                    ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt((uint)vec[0].NSCrosspiece);

                    int offsetX = x + 121 + (48 - artInfo.UV.Width) / 2;
                    int offsetY = y + 36;

                    StaticPic pic = new StaticPic((ushort)vec[0].NSCrosspiece, 0)
                    {
                        X = offsetX,
                        Y = offsetY,
                        CanMove = false,
                        LocalSerial = (uint)(ID_GUMP_CUSTOM_HOUSE.ID_GCH_ITEM_IN_LIST + i),
                        Height = 60
                    };

                    pic.MouseUp += (sender, e) =>
                    {
                        OnButtonClick((int)pic.LocalSerial);
                    };
                    _dataBox.Add(pic);

                    x += 48;

                    if (x >= 384)
                    {
                        x = 0;
                        y += 60;

                        // pop scissor,
                        // push scissor

                        _dataBox.Add(new ScissorControl(false));

                        _dataBox.Add(new ScissorControl(true, 121, 96, 384, 60));
                    }
                }

                // pop scissor
                _dataBox.Add(new ScissorControl(false));
            }
            else if (
                _customHouseManager.Category >= 0
                && _customHouseManager.Category < World.CustomHouseManager.Roofs.Count
            )
            {
                List<CustomHouseRoof> vec = World.CustomHouseManager.Roofs[
                    _customHouseManager.Category
                ].Items;

                if (Page >= 0 && Page < vec.Count)
                {
                    CustomHouseRoof item = vec[Page];

                    // push scissor
                    _dataBox.Add(new ScissorControl(true, 130, 44, 384, 120));

                    int index = 0;

                    for (int j = 0; j < 2; j++)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            ushort graphic = item.Graphics[index];

                            if (graphic != 0)
                            {
                                ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(graphic);

                                int offsetX = x + 130 + (48 - artInfo.UV.Width) / 2;
                                int offsetY = y + 44 + (60 - artInfo.UV.Height) / 2;

                                StaticPic pic = new StaticPic(graphic, 0)
                                {
                                    X = offsetX,
                                    Y = offsetY,
                                    CanMove = false,
                                    LocalSerial = (uint)(
                                        ID_GUMP_CUSTOM_HOUSE.ID_GCH_ITEM_IN_LIST + index
                                    ),
                                    //Height = 120
                                };

                                pic.MouseUp += (sender, e) =>
                                {
                                    OnButtonClick((int)pic.LocalSerial);
                                };
                                _dataBox.Add(pic);
                            }

                            x += 48;
                            index++;
                        }

                        x = 0;
                        y += 60;
                    }

                    // pop scissor
                    _dataBox.Add(new ScissorControl(false));
                }

                _dataBoxGUI.Add(new GumpPic(152, 0, 0x55F3, 0));

                Button button = new Button(
                    (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_CATEGORY,
                    0x5622,
                    0x5624,
                    0x5623
                )
                {
                    X = 167,
                    Y = 5,
                    ButtonAction = ButtonAction.Activate
                };

                button.SetTooltip(ResGumps.ToCustomHouseManagerCategory);
                _dataBoxGUI.Add(button);

                button = new Button(
                    (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_ROOF_Z_DOWN,
                    0x578B,
                    0x578D,
                    0x578C
                )
                {
                    X = 305,
                    Y = 0,
                    ButtonAction = ButtonAction.Activate
                };

                button.SetTooltip(ResGumps.LowerRoofPlacementLevel);
                _dataBoxGUI.Add(button);

                button = new Button(
                    (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_ROOF_Z_UP,
                    0x578E,
                    0x5790,
                    0x578F
                )
                {
                    X = 349,
                    Y = 0,
                    ButtonAction = ButtonAction.Activate
                };

                button.SetTooltip(ResGumps.RaiseRoofPlacementLevel);
                _dataBoxGUI.Add(button);

                _dataBoxGUI.Add(new GumpPic(583, 4, 0x55F4, 0));

                Label text = new Label(_customHouseManager.RoofZ.ToString(), false, 0x04E9, font: 3)
                {
                    X = 405,
                    Y = 15
                };

                _dataBoxGUI.Add(text);
            }
        }

        private void AddMisc()
        {
            int x = 0,
                y = 0;

            if (_customHouseManager.Category == -1)
            {
                int startCategory = Page * 16;
                int endCategory = startCategory + 16;

                if (endCategory > World.CustomHouseManager.Miscs.Count)
                {
                    endCategory = World.CustomHouseManager.Miscs.Count;
                }

                // push scissor
                _dataBox.Add(new ScissorControl(true, 121, 36, 384, 60));

                for (int i = startCategory; i < endCategory; i++)
                {
                    List<CustomHouseMisc> vec = World.CustomHouseManager.Miscs[i].Items;

                    if (vec.Count == 0)
                    {
                        continue;
                    }

                    ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt((uint)vec[0].Piece5);

                    int offsetX = x + 121 + (48 - artInfo.UV.Width) / 2;
                    int offsetY = y + 36;

                    StaticPic pic = new StaticPic((ushort)vec[0].Piece5, 0)
                    {
                        X = offsetX,
                        Y = offsetY,
                        CanMove = false,
                        LocalSerial = (uint)(ID_GUMP_CUSTOM_HOUSE.ID_GCH_ITEM_IN_LIST + i),
                        Width = artInfo.UV.Width < 48 ? artInfo.UV.Width : 48,
                        Height = artInfo.UV.Height < 60 ? artInfo.UV.Height : 60
                    };

                    pic.MouseUp += (sender, e) =>
                    {
                        OnButtonClick((int)pic.LocalSerial);
                    };
                    _dataBox.Add(pic);

                    x += 48;

                    if (x >= 384)
                    {
                        x = 0;
                        y += 60;

                        // pop scissor
                        // push scissor
                        _dataBox.Add(new ScissorControl(false));

                        _dataBox.Add(new ScissorControl(true, 121, 96, 384, 60));
                    }
                }

                // pop scissor
                _dataBox.Add(new ScissorControl(false));
            }
            else if (
                _customHouseManager.Category >= 0
                && _customHouseManager.Category < World.CustomHouseManager.Miscs.Count
            )
            {
                List<CustomHouseMisc> vec = World.CustomHouseManager.Miscs[
                    _customHouseManager.Category
                ].Items;

                if (Page >= 0 && Page < vec.Count)
                {
                    CustomHouseMisc item = vec[Page];

                    // push scissor
                    _dataBox.Add(new ScissorControl(true, 130, 44, 384, 120));

                    for (int i = 0; i < 8; i++)
                    {
                        ushort graphic = item.Graphics[i];

                        if (graphic != 0)
                        {
                            ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(graphic);

                            int offsetX = x + 130 + (48 - artInfo.UV.Width) / 2;
                            int offsetY = y + 44 + (120 - artInfo.UV.Height) / 2;

                            StaticPic pic = new StaticPic(graphic, 0)
                            {
                                X = offsetX,
                                Y = offsetY,
                                CanMove = false,
                                LocalSerial = (uint)(ID_GUMP_CUSTOM_HOUSE.ID_GCH_ITEM_IN_LIST + i),
                                //Height = 120
                            };

                            pic.MouseUp += (sender, e) =>
                            {
                                OnButtonClick((int)pic.LocalSerial);
                            };
                            _dataBox.Add(pic);
                        }

                        x += 48;
                    }

                    // pop scissor
                    _dataBox.Add(new ScissorControl(false));
                }

                _dataBoxGUI.Add(new GumpPic(152, 0, 0x55F3, 0));

                _dataBoxGUI.Add(
                    new Button((int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_CATEGORY, 0x5622, 0x5624, 0x5623)
                    {
                        X = 167,
                        Y = 5,
                        ButtonAction = ButtonAction.Activate
                    }
                );
            }
        }

        private void AddMenu()
        {
            Button button = new Button(
                (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_BACKUP,
                0x098D,
                0x098D,
                0x098D,
                ResGumps.Backup,
                0,
                true,
                0,
                0x0036
            )
            {
                X = 150,
                Y = 50,
                ButtonAction = ButtonAction.Activate,
                FontCenter = true
            };

            button.SetTooltip(ResGumps.StoreDesignInProgress);
            _dataBox.Add(button);

            button = new Button(
                (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_RESTORE,
                0x098D,
                0x098D,
                0x098D,
                ResGumps.Restore,
                0,
                true,
                0,
                0x0036
            )
            {
                X = 150,
                Y = 90,
                ButtonAction = ButtonAction.Activate,
                FontCenter = true
            };

            button.SetTooltip(ResGumps.RestoreYourDesign);
            _dataBox.Add(button);

            button = new Button(
                (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_SYNCH,
                0x098D,
                0x098D,
                0x098D,
                ResGumps.Sync,
                0,
                true,
                0,
                0x0036
            )
            {
                X = 270,
                Y = 50,
                ButtonAction = ButtonAction.Activate,
                FontCenter = true
            };

            button.SetTooltip(ResGumps.SynchronizeDesignStateWithServer);
            _dataBox.Add(button);

            button = new Button(
                (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_CLEAR,
                0x098D,
                0x098D,
                0x098D,
                ResGumps.Clear,
                0,
                true,
                0,
                0x0036
            )
            {
                X = 270,
                Y = 90,
                ButtonAction = ButtonAction.Activate,
                FontCenter = true
            };

            button.SetTooltip(ResGumps.ClearAllChanges);
            _dataBox.Add(button);

            button = new Button(
                (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_COMMIT,
                0x098D,
                0x098D,
                0x098D,
                ResGumps.Commit,
                0,
                true,
                0,
                0x0036
            )
            {
                X = 390,
                Y = 50,
                ButtonAction = ButtonAction.Activate,
                FontCenter = true
            };

            button.SetTooltip(ResGumps.SaveExistingChanges);
            _dataBox.Add(button);

            button = new Button(
                (int)ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_REVERT,
                0x098D,
                0x098D,
                0x098D,
                ResGumps.Revert,
                0,
                true,
                0,
                0x0036
            )
            {
                X = 390,
                Y = 90,
                ButtonAction = ButtonAction.Activate,
                FontCenter = true
            };

            button.SetTooltip(ResGumps.RevertYourDesign);
            _dataBox.Add(button);
        }

        public override void OnButtonClick(int buttonID)
        {
            ID_GUMP_CUSTOM_HOUSE idd = (ID_GUMP_CUSTOM_HOUSE)buttonID;

            if (idd >= ID_GUMP_CUSTOM_HOUSE.ID_GCH_ITEM_IN_LIST)
            {
                int index = idd - ID_GUMP_CUSTOM_HOUSE.ID_GCH_ITEM_IN_LIST;

                if (
                    _customHouseManager.Category == -1
                    && (
                        _customHouseManager.State == CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL
                        || _customHouseManager.State == CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF
                        || _customHouseManager.State == CUSTOM_HOUSE_GUMP_STATE.CHGS_MISC
                    )
                )
                {
                    int newCategory = -1;

                    if (
                        _customHouseManager.State == CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL
                        && index >= 0
                        && index < World.CustomHouseManager.Walls.Count
                    )
                    {
                        newCategory = World.CustomHouseManager.Walls[index].Index;
                    }
                    else if (
                        _customHouseManager.State == CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF
                        && index >= 0
                        && index < World.CustomHouseManager.Roofs.Count
                    )
                    {
                        newCategory = World.CustomHouseManager.Roofs[index].Index;
                    }
                    else if (
                        _customHouseManager.State == CUSTOM_HOUSE_GUMP_STATE.CHGS_MISC
                        && index >= 0
                        && index < World.CustomHouseManager.Miscs.Count
                    )
                    {
                        newCategory = World.CustomHouseManager.Miscs[index].Index;
                    }

                    if (newCategory != -1)
                    {
                        _customHouseManager.Category = newCategory;
                        Page = 0;
                        _customHouseManager.SelectedGraphic = 0;
                        _customHouseManager.Erasing = false;
                        _customHouseManager.SeekTile = false;
                        _customHouseManager.CombinedStair = false;
                        UpdateMaxPage();
                        Update();
                    }
                }
                else if (index >= 0 && Page >= 0)
                {
                    bool combinedStairs = false;
                    ushort graphic = 0;

                    if (
                        _customHouseManager.State == CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL
                        || _customHouseManager.State == CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF
                        || _customHouseManager.State == CUSTOM_HOUSE_GUMP_STATE.CHGS_MISC
                    )
                    {
                        if (_customHouseManager.Category >= 0)
                        {
                            if (
                                _customHouseManager.State == CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL
                                && _customHouseManager.Category
                                    < World.CustomHouseManager.Walls.Count
                                && index < CustomHouseWall.GRAPHICS_COUNT
                            )
                            {
                                List<CustomHouseWall> list = World.CustomHouseManager.Walls[
                                    _customHouseManager.Category
                                ].Items;

                                if (Page < list.Count)
                                {
                                    graphic = _customHouseManager.ShowWindow
                                        ? list[Page].WindowGraphics[index]
                                        : list[Page].Graphics[index];
                                }
                            }
                            else if (
                                _customHouseManager.State == CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF
                                && _customHouseManager.Category
                                    < World.CustomHouseManager.Roofs.Count
                                && index < CustomHouseRoof.GRAPHICS_COUNT
                            )
                            {
                                List<CustomHouseRoof> list = World.CustomHouseManager.Roofs[
                                    _customHouseManager.Category
                                ].Items;

                                if (Page < list.Count)
                                {
                                    graphic = list[Page].Graphics[index];
                                }
                            }
                            else if (
                                _customHouseManager.State == CUSTOM_HOUSE_GUMP_STATE.CHGS_MISC
                                && _customHouseManager.Category
                                    < World.CustomHouseManager.Miscs.Count
                                && index < CustomHouseMisc.GRAPHICS_COUNT
                            )
                            {
                                List<CustomHouseMisc> list = World.CustomHouseManager.Miscs[
                                    _customHouseManager.Category
                                ].Items;

                                if (Page < list.Count)
                                {
                                    graphic = list[Page].Graphics[index];
                                }
                            }
                        }
                    }
                    else
                    {
                        if (
                            _customHouseManager.State == CUSTOM_HOUSE_GUMP_STATE.CHGS_DOOR
                            && Page < World.CustomHouseManager.Doors.Count
                            && index < CustomHouseDoor.GRAPHICS_COUNT
                        )
                        {
                            graphic = World.CustomHouseManager.Doors[Page].Graphics[index];
                        }
                        else if (
                            _customHouseManager.State == CUSTOM_HOUSE_GUMP_STATE.CHGS_FLOOR
                            && Page < World.CustomHouseManager.Floors.Count
                            && index < CustomHouseFloor.GRAPHICS_COUNT
                        )
                        {
                            graphic = World.CustomHouseManager.Floors[Page].Graphics[index];
                        }
                        else if (
                            _customHouseManager.State == CUSTOM_HOUSE_GUMP_STATE.CHGS_STAIR
                            && Page < World.CustomHouseManager.Stairs.Count
                        )
                        {
                            if (index > 10)
                            {
                                combinedStairs = true;
                                index -= 10;
                            }

                            if (index < CustomHouseStair.GRAPHICS_COUNT)
                            {
                                graphic = World.CustomHouseManager.Stairs[Page].Graphics[index];
                            }
                        }
                    }

                    if (graphic != 0)
                    {
                        _customHouseManager.SetTargetMulti();
                        _customHouseManager.CombinedStair = combinedStairs;
                        _customHouseManager.SelectedGraphic = graphic;
                        Update();
                    }
                }

                return;
            }

            switch (idd)
            {
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_WALL:
                    _customHouseManager.Category = -1;
                    _customHouseManager.State = CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL;
                    Page = 0;
                    _customHouseManager.SelectedGraphic = 0;
                    _customHouseManager.CombinedStair = false;
                    UpdateMaxPage();
                    World.TargetManager.CancelTarget();
                    Update();

                    break;

                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_DOOR:
                    _customHouseManager.Category = -1;
                    _customHouseManager.State = CUSTOM_HOUSE_GUMP_STATE.CHGS_DOOR;
                    Page = 0;
                    _customHouseManager.SelectedGraphic = 0;
                    _customHouseManager.CombinedStair = false;
                    UpdateMaxPage();
                    World.TargetManager.CancelTarget();
                    Update();

                    break;

                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_FLOOR:
                    _customHouseManager.Category = -1;
                    _customHouseManager.State = CUSTOM_HOUSE_GUMP_STATE.CHGS_FLOOR;
                    Page = 0;
                    _customHouseManager.SelectedGraphic = 0;
                    _customHouseManager.CombinedStair = false;
                    UpdateMaxPage();
                    World.TargetManager.CancelTarget();
                    Update();

                    break;

                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_STAIR:
                    _customHouseManager.Category = -1;
                    _customHouseManager.State = CUSTOM_HOUSE_GUMP_STATE.CHGS_STAIR;
                    Page = 0;
                    _customHouseManager.SelectedGraphic = 0;
                    _customHouseManager.CombinedStair = false;
                    UpdateMaxPage();
                    World.TargetManager.CancelTarget();
                    Update();

                    break;

                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_ROOF:
                    _customHouseManager.Category = -1;
                    _customHouseManager.State = CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF;
                    Page = 0;
                    _customHouseManager.SelectedGraphic = 0;
                    _customHouseManager.CombinedStair = false;
                    UpdateMaxPage();
                    World.TargetManager.CancelTarget();
                    Update();

                    break;

                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_MISC:
                    _customHouseManager.Category = -1;
                    _customHouseManager.State = CUSTOM_HOUSE_GUMP_STATE.CHGS_MISC;
                    Page = 0;
                    _customHouseManager.SelectedGraphic = 0;
                    _customHouseManager.CombinedStair = false;
                    UpdateMaxPage();
                    World.TargetManager.CancelTarget();
                    Update();

                    break;

                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_ERASE:
                    _customHouseManager.SetTargetMulti();
                    _customHouseManager.Erasing = !_customHouseManager.Erasing;
                    _customHouseManager.SelectedGraphic = 0;
                    _customHouseManager.CombinedStair = false;
                    Update();

                    break;

                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_EYEDROPPER:
                    _customHouseManager.SetTargetMulti();
                    _customHouseManager.SeekTile = true;
                    _customHouseManager.SelectedGraphic = 0;
                    _customHouseManager.CombinedStair = false;
                    Update();

                    break;

                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_STATE_MENU:
                    _customHouseManager.Category = -1;
                    _customHouseManager.State = CUSTOM_HOUSE_GUMP_STATE.CHGS_MENU;
                    Page = 0;
                    _customHouseManager.MaxPage = 1;
                    _customHouseManager.SelectedGraphic = 0;
                    _customHouseManager.CombinedStair = false;
                    World.TargetManager.CancelTarget();
                    Update();

                    break;

                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_1:
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_2:
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_3:
                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_4:
                    int selectedFloor =
                        (ID_GUMP_CUSTOM_HOUSE)buttonID
                        - ID_GUMP_CUSTOM_HOUSE.ID_GCH_VISIBILITY_STORY_1;

                    _customHouseManager.FloorVisionState[selectedFloor]++;

                    if (
                        _customHouseManager.FloorVisionState[selectedFloor]
                        > (int)CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_HIDE_ALL
                    )
                    {
                        _customHouseManager.FloorVisionState[selectedFloor] = (int)
                            CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_NORMAL;
                    }

                    _customHouseManager.GenerateFloorPlace();
                    Update();

                    break;

                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_1:
                    _customHouseManager.CurrentFloor = 1;
                    NetClient.Socket.Send_CustomHouseGoToFloor(World, 1);

                    for (int i = 0; i < _customHouseManager.FloorVisionState.Length; i++)
                    {
                        _customHouseManager.FloorVisionState[i] = (int)
                            CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_NORMAL;
                    }

                    Update();

                    break;

                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_2:
                    _customHouseManager.CurrentFloor = 2;
                    NetClient.Socket.Send_CustomHouseGoToFloor(World, 2);

                    for (int i = 0; i < _customHouseManager.FloorVisionState.Length; i++)
                    {
                        _customHouseManager.FloorVisionState[i] = (int)
                            CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_NORMAL;
                    }

                    Update();

                    break;

                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_3:
                    _customHouseManager.CurrentFloor = 3;
                    NetClient.Socket.Send_CustomHouseGoToFloor(World, 3);

                    for (int i = 0; i < _customHouseManager.FloorVisionState.Length; i++)
                    {
                        _customHouseManager.FloorVisionState[i] = (int)
                            CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_NORMAL;
                    }

                    Update();

                    break;

                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_FLOOR_4:
                    _customHouseManager.CurrentFloor = 4;
                    NetClient.Socket.Send_CustomHouseGoToFloor(World, 4);

                    for (int i = 0; i < _customHouseManager.FloorVisionState.Length; i++)
                    {
                        _customHouseManager.FloorVisionState[i] = (int)
                            CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_NORMAL;
                    }

                    Update();

                    break;

                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_LIST_LEFT:
                    Page--;

                    if (Page < 0)
                    {
                        Page = _customHouseManager.MaxPage - 1;

                        if (Page < 0)
                        {
                            Page = 0;
                        }
                    }

                    Update();

                    break;

                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_LIST_RIGHT:
                    Page++;

                    if (Page >= _customHouseManager.MaxPage)
                    {
                        Page = 0;
                    }

                    Update();

                    break;

                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_BACKUP:
                    NetClient.Socket.Send_CustomHouseBackup(World);

                    break;

                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_RESTORE:
                    NetClient.Socket.Send_CustomHouseRestore(World);

                    break;

                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_SYNCH:
                    NetClient.Socket.Send_CustomHouseSync(World);

                    break;

                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_CLEAR:
                    NetClient.Socket.Send_CustomHouseClear(World);

                    break;

                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_COMMIT:
                    NetClient.Socket.Send_CustomHouseCommit(World);

                    break;

                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_MENU_REVERT:
                    NetClient.Socket.Send_CustomHouseRevert(World);

                    break;

                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_GO_CATEGORY:
                    _customHouseManager.Category = -1;
                    Page = 0;
                    _customHouseManager.SelectedGraphic = 0;
                    _customHouseManager.CombinedStair = false;
                    UpdateMaxPage();
                    World.TargetManager.CancelTarget();
                    Update();

                    break;

                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_WALL_SHOW_WINDOW:
                    _customHouseManager.ShowWindow = !_customHouseManager.ShowWindow;
                    Update();

                    break;

                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_ROOF_Z_UP:
                    if (_customHouseManager.RoofZ < 6)
                    {
                        _customHouseManager.RoofZ++;
                        Update();
                    }

                    break;

                case ID_GUMP_CUSTOM_HOUSE.ID_GCH_ROOF_Z_DOWN:
                    if (_customHouseManager.RoofZ > 1)
                    {
                        _customHouseManager.RoofZ--;
                        Update();
                    }

                    break;
            }
        }

        public override void Dispose()
        {
            SetOtherHousesState(true);
            World.CustomHouseManager = null;
            NetClient.Socket.Send_CustomHouseBuildingExit(World);
            World.TargetManager.CancelTarget();

            base.Dispose();
        }

        private enum ID_GUMP_CUSTOM_HOUSE
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
    }
}
