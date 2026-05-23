// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Houses.Customization
{
    /// <summary>
    /// Concrete <see cref="IHouseValidator"/>. Holds a reference to the
    /// owning <see cref="HouseCustomizationManager"/> facade so the rules can
    /// read the live counters / lists / bounds without those having to be
    /// passed in on every call. Knows nothing about networking, file I/O or
    /// gump state — only about geometry and the static category tables.
    /// </summary>
    internal sealed class HouseValidator : IHouseValidator
    {
        private readonly HouseCustomizationManager _facade;
        private readonly IHouseDataLoader _data;
        private readonly World _world;

        public HouseValidator(HouseCustomizationManager facade, IHouseDataLoader data, World world)
        {
            _facade = facade;
            _data = data;
            _world = world;
        }

        public bool CanBuildHere(List<CustomBuildObject> list, out CUSTOM_HOUSE_BUILD_TYPE type)
        {
            type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_NORMAL;

            if (_facade.SelectedGraphic == 0)
            {
                return false;
            }

            var foundationItem = _world.Items.Get(_facade.Serial);

            if (foundationItem == null || !_world.HouseManager.TryGetHouse(foundationItem, out House house))
                return false;

            bool result = true;

            if (_facade.CombinedStair)
            {
                if (_facade.Components + 10 > _facade.MaxComponets || _facade.CurrentFloor >= _facade.FloorCount)
                {
                    return false;
                }

                (int res1, int res2) = _data.SeekGraphic(_facade.Stairs, _facade.SelectedGraphic);

                if (res1 == -1 || res2 == -1 || res1 >= _facade.Stairs.Count)
                {
                    list.Add(new CustomBuildObject()
                    {
                        Graphic = _facade.SelectedGraphic,
                        X = 0,
                        Y = 0,
                        Z = 0
                    });

                    return false;
                }

                CustomHouseStair item = _facade.Stairs[res1];

                if (_facade.SelectedGraphic == item.North)
                {
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 0, Y = -3, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 0, Y = -2, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 0, Y = -1, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.North, X = 0, Y = 0, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 0, Y = -3, Z = 5 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 0, Y = -2, Z = 5 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.North, X = 0, Y = -1, Z = 5 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 0, Y = -3, Z = 10 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.North, X = 0, Y = -2, Z = 10 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.North, X = 0, Y = -3, Z = 15 });
                }
                else if (_facade.SelectedGraphic == item.East)
                {
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.East, X = 0, Y = 0, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 1, Y = 0, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 2, Y = 0, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 3, Y = 0, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.East, X = 1, Y = 0, Z = 5 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 2, Y = 0, Z = 5 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 3, Y = 0, Z = 5 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.East, X = 2, Y = 0, Z = 10 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 3, Y = 0, Z = 10 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.East, X = 3, Y = 0, Z = 15 });
                }
                else if (_facade.SelectedGraphic == item.South)
                {
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.South, X = 0, Y = 0, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 0, Y = 1, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 0, Y = 2, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 0, Y = 3, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.South, X = 0, Y = 1, Z = 5 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 0, Y = 2, Z = 5 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 0, Y = 3, Z = 5 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.South, X = 0, Y = 2, Z = 10 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 0, Y = 3, Z = 10 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.South, X = 0, Y = 3, Z = 15 });
                }
                else if (_facade.SelectedGraphic == item.West)
                {
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = -3, Y = 0, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = -2, Y = 0, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = -1, Y = 0, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.West, X = 0, Y = 0, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = -3, Y = 0, Z = 5 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = -2, Y = 0, Z = 5 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.West, X = -1, Y = 0, Z = 5 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = -3, Y = 0, Z = 10 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.West, X = -2, Y = 0, Z = 10 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.West, X = -3, Y = 0, Z = 15 });
                }
                else
                {
                    list.Add(new CustomBuildObject { Graphic = _facade.SelectedGraphic, X = 0, Y = 0, Z = 0 });
                }

                type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR;
            }
            else
            {
                (int fixCheck1, int fixCheck2) = _data.SeekGraphic(_facade.Doors, _facade.SelectedGraphic);

                bool isFixture = false;

                if (fixCheck1 == -1 || fixCheck2 == -1)
                {
                    (fixCheck1, fixCheck2) = _data.SeekGraphic(_facade.Teleports, _facade.SelectedGraphic);

                    isFixture = fixCheck1 != -1 && fixCheck2 != -1;

                    if (isFixture)
                    {
                        type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_FLOOR;
                    }
                }
                else
                {
                    isFixture = true;
                    type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_NORMAL;
                }

                if (isFixture)
                {
                    if (_facade.Fixtures + 1 > _facade.MaxFixtures)
                    {
                        result = false;
                    }
                }
                else if (_facade.Components + 1 > _facade.MaxComponets)
                {
                    result = false;
                }

                if (_facade.State == CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF)
                {
                    list.Add(new CustomBuildObject { Graphic = _facade.SelectedGraphic, X = 0, Y = 0, Z = (_facade.RoofZ - 2) * 3 });
                    type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_ROOF;
                }
                else
                {
                    if (_facade.State == CUSTOM_HOUSE_GUMP_STATE.CHGS_STAIR)
                    {
                        list.Add(new CustomBuildObject { Graphic = _facade.SelectedGraphic, X = 0, Y = 1, Z = 0 });
                        type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR;
                    }
                    else
                    {
                        if (_facade.State == CUSTOM_HOUSE_GUMP_STATE.CHGS_FLOOR)
                        {
                            type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_FLOOR;
                        }

                        list.Add(new CustomBuildObject { Graphic = _facade.SelectedGraphic, X = 0, Y = 0, Z = 0 });
                    }
                }
            }

            if (SelectedObject.Object is GameObject gobj)
            {
                if (!_facade.Bounds.Contains(gobj.X, gobj.Y))
                    return false;

                var minZ = foundationItem.Z + 0 + (_facade.CurrentFloor - 1) * 20;
                var maxZ = minZ + 20;

                // var boundsOffset = State != CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL ? 1 : 0;

                for (var i = 0; i < list.Count; ++i)
                {
                    var item = list[i];
                    if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR)
                    {
                        if (_facade.CombinedStair)
                        {
                            if (item.Z != 0)
                                continue;
                        }
                        else
                        {
                            if (gobj.Y + item.Y < _facade.EndPos.Y || gobj.X + item.X == _facade.Bounds.X || gobj.Z >= _facade.MinHouseZ)
                                return false;

                            if (gobj.Y + item.Y != _facade.EndPos.Y)
                            {
                                item.Y = 0;
                                list[0] = item;
                            }
                            continue;
                        }
                    }

                    if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR && _facade.CombinedStair)
                    {
                        int tileX = gobj.X + item.X;
                        int tileY = gobj.Y + item.Y;

                        if (tileX < _facade.StartPos.X || tileX >= _facade.EndPos.X || tileY < _facade.StartPos.Y || tileY >= _facade.EndPos.Y)
                            return false;
                    }
                    else if (!ValidateItemPlaceRect(_facade.Bounds, item.Graphic, gobj.X + item.X, gobj.Y + item.Y))
                    {
                        return false;
                    }

                    if (type != CUSTOM_HOUSE_BUILD_TYPE.CHBT_FLOOR)
                    {
                        foreach (var multi in house.GetMultiAt(gobj.X + item.X, gobj.Y + item.Y))
                        {
                            if (!multi.IsCustom)
                                continue;

                            int collisionMaxZ = (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR && _facade.CombinedStair) ? maxZ + 20 : maxZ;

                            if ((multi.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL) == 0 && multi.Z >= minZ && multi.Z < collisionMaxZ)
                            {
                                if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR)
                                {
                                    if ((multi.State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_DONT_REMOVE)) == 0)
                                        return false;
                                }
                                else
                                {
                                    if ((multi.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR) != 0)
                                        return false;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        public bool CanEraseHere(GameObject place, out CUSTOM_HOUSE_BUILD_TYPE type)
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
                    else if (_facade.Bounds.Contains(place.X, place.Y) && place.Z >= _facade.MinHouseZ)
                    {
                        // it's into the bounds
                    }
                    else
                    {
                        return false;
                    }

                    return true;
                }
            }

            return false;
        }

        public (int, int) ExistsInList(ref CUSTOM_HOUSE_GUMP_STATE state, ushort graphic)
        {
            (int res1, int res2) = _data.SeekGraphicWithCategory<CustomHouseWall, CustomHouseWallCategory>(_facade.Walls, graphic);

            if (res1 == -1 || res2 == -1)
            {
                (res1, res2) = _data.SeekGraphic(_facade.Floors, graphic);

                if (res1 == -1 || res2 == -1)
                {
                    (res1, res2) = _data.SeekGraphic(_facade.Doors, graphic);

                    if (res1 == -1 || res2 == -1)
                    {
                        (res1, res2) = _data.SeekGraphicWithCategory<CustomHouseMisc, CustomHouseMiscCategory>(_facade.Miscs, graphic);

                        if (res1 == -1 || res2 == -1)
                        {
                            (res1, res2) = _data.SeekGraphic(_facade.Stairs, graphic);

                            if (res1 == -1 || res2 == -1)
                            {
                                (res1, res2) = _data.SeekGraphicWithCategory<CustomHouseRoof, CustomHouseRoofCategory>(_facade.Roofs, graphic);

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
                            (int res_1, int res_2) = _data.SeekGraphic(_facade.Teleports, graphic);

                            if (res_1 != -1 && res_2 != -1)
                            {
                                state = CUSTOM_HOUSE_GUMP_STATE.CHGS_FIXTURE;
                                res1 = res_1;
                                res2 = res_2;
                            }
                            else
                            {
                                state = CUSTOM_HOUSE_GUMP_STATE.CHGS_MISC;
                            }
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

        private bool ValidateItemPlaceRect(Rectangle rect, ushort graphic, int x, int y)
        {
            if (!rect.Contains(x, y))
            {
                return false;
            }

            (int infoCheck1, int infoCheck2) = _data.SeekGraphic(_facade.ObjectsInfo, graphic);

            if (infoCheck1 != -1 && infoCheck2 != -1)
            {
                CustomHousePlaceInfo info = _facade.ObjectsInfo[infoCheck1];

                if (info.CanGoW == 0 && x == rect.X)
                {
                    return false;
                }

                if (info.CanGoN == 0 && y == rect.Y)
                {
                    return false;
                }

                if (info.CanGoNWS == 0 && x == rect.X && y == rect.Y)
                {
                    return false;
                }
            }

            return true;
        }

        public bool ValidateItemPlace(Item foundationItem, Multi item, int minZ, int maxZ, List<Point> validatedFloors)
        {
            if (item == null || !_world.HouseManager.TryGetHouse(foundationItem, out House house) || !item.IsCustom)
            {
                return true;
            }

            if ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0)
            {
                bool existsInList(List<Point> list, Point testedPoint)
                {
                    foreach (Point point in list)
                    {
                        if (testedPoint == point)
                        {
                            return true;
                        }
                    }

                    return false;
                }

                if (ValidatePlaceStructure
                (
                    foundationItem,
                    house,
                    house.GetMultiAt(item.X, item.Y),
                    minZ - 20,
                    maxZ - 20,
                    (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_DIRECT_SUPPORT
                ) || ValidatePlaceStructure
                (
                    foundationItem,
                    house,
                    house.GetMultiAt(item.X - 1, item.Y),
                    minZ - 20,
                    maxZ - 20,
                    (int) (CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_DIRECT_SUPPORT | CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_CANGO_W)
                ) || ValidatePlaceStructure
                (
                    foundationItem,
                    house,
                    house.GetMultiAt(item.X, item.Y - 1),
                    minZ - 20,
                    maxZ - 20,
                    (int) (CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_DIRECT_SUPPORT | CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_CANGO_N)
                ))
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
                        Point testPoint = new Point(item.X + table[i].X, item.Y + table[i].Y);

                        if (!existsInList(validatedFloors, testPoint))
                        {
                            validatedFloors.Add(testPoint);
                        }
                    }

                    return true;
                }

                return false;
            }


            if ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF) != 0)
            {
                return true;
            }

            if ((item.State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FIXTURE)) != 0)
            {
                foreach (Multi temp in house.GetMultiAt(item.X, item.Y))
                {
                    if (temp == item)
                    {
                        continue;
                    }

                    if ((temp.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0 && temp.Z >= minZ && temp.Z < maxZ)
                    {
                        if ((temp.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0 && (temp.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }


            (int infoCheck1, int infoCheck2) = _data.SeekGraphic(_facade.ObjectsInfo, item.Graphic);

            if (infoCheck1 != -1 && infoCheck2 != -1)
            {
                CustomHousePlaceInfo info = _facade.ObjectsInfo[infoCheck1];

                if (info.CanGoW == 0 && item.X == _facade.Bounds.X)
                {
                    return false;
                }

                if (info.CanGoN == 0 && item.Y == _facade.Bounds.Y)
                {
                    return false;
                }

                if (info.CanGoNWS == 0 && item.X == _facade.Bounds.X && item.Y == _facade.Bounds.Y)
                {
                    return false;
                }

                if (info.Bottom == 0)
                {
                    bool found = false;

                    if (info.AdjUN != 0)
                    {
                        found = ValidatePlaceStructure
                        (
                            foundationItem,
                            house,
                            house.GetMultiAt(item.X, item.Y + 1),
                            minZ,
                            maxZ,
                            (int) (CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_BOTTOM | CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_N)
                        );
                    }

                    if (!found && info.AdjUE != 0)
                    {
                        found = ValidatePlaceStructure
                        (
                            foundationItem,
                            house,
                            house.GetMultiAt(item.X - 1, item.Y),
                            minZ,
                            maxZ,
                            (int) (CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_BOTTOM | CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_E)
                        );
                    }

                    if (!found && info.AdjUS != 0)
                    {
                        found = ValidatePlaceStructure
                        (
                            foundationItem,
                            house,
                            house.GetMultiAt(item.X, item.Y - 1),
                            minZ,
                            maxZ,
                            (int) (CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_BOTTOM | CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_S)
                        );
                    }

                    if (!found && info.AdjUW != 0)
                    {
                        found = ValidatePlaceStructure
                        (
                            foundationItem,
                            house,
                            house.GetMultiAt(item.X + 1, item.Y),
                            minZ,
                            maxZ,
                            (int) (CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_BOTTOM | CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_W)
                        );
                    }

                    if (!found && minZ == foundationItem.Z + 7)
                    {
                        return false;
                    }
                }

                if (info.Top == 0)
                {
                    bool found = false;

                    if (info.AdjLN != 0)
                    {
                        found = ValidatePlaceStructure
                        (
                            foundationItem,
                            house,
                            house.GetMultiAt(item.X, item.Y + 1),
                            minZ,
                            maxZ,
                            (int) (CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_TOP | CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_N)
                        );
                    }

                    if (!found && info.AdjLE != 0)
                    {
                        found = ValidatePlaceStructure
                        (
                            foundationItem,
                            house,
                            house.GetMultiAt(item.X - 1, item.Y),
                            minZ,
                            maxZ,
                            (int) (CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_TOP | CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_E)
                        );
                    }

                    if (!found && info.AdjLS != 0)
                    {
                        found = ValidatePlaceStructure
                        (
                            foundationItem,
                            house,
                            house.GetMultiAt(item.X, item.Y - 1),
                            minZ,
                            maxZ,
                            (int) (CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_TOP | CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_S)
                        );
                    }

                    if (!found && info.AdjLW != 0)
                    {
                        found = ValidatePlaceStructure
                        (
                            foundationItem,
                            house,
                            house.GetMultiAt(item.X + 1, item.Y),
                            minZ,
                            maxZ,
                            (int) (CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_TOP | CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_W)
                        );
                    }

                    if (!found && minZ == foundationItem.Z + 7)
                    {
                        return false;
                    }
                }
            }

            if (minZ > foundationItem.Z + 7)
            {
                int belowMinZ = minZ - 20;

                // 1) Check same position on the floor below for wall-type support.
                bool foundAnyWallBelow = false;
                bool hasFloorTileBelow = false;

                foreach (Multi below in house.GetMultiAt(item.X, item.Y))
                {
                    if (below.IsCustom && below.Z >= belowMinZ && below.Z < minZ)
                    {
                        if ((below.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0 &&
                            (below.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL) == 0)
                        {
                            hasFloorTileBelow = true;
                        }

                        if ((below.State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR |
                                           CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR |
                                           CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF |
                                           CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FIXTURE |
                                           CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL)) == 0)
                        {
                            foundAnyWallBelow = true;

                            if ((below.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0)
                            {
                                return true;
                            }
                        }
                    }
                }

                if (foundAnyWallBelow)
                {
                    return false;
                }

                // 2) No wall at same position below. If there's a floor tile below,
                //    check ±1 adjacent positions on the floor below for wall support.
                if (hasFloorTileBelow)
                {
                    int[] adx = { -1, 1, 0, 0 };
                    int[] ady = { 0, 0, -1, 1 };

                    for (int d = 0; d < 4; d++)
                    {
                        foreach (Multi adj in house.GetMultiAt(item.X + adx[d], item.Y + ady[d]))
                        {
                            if (adj.IsCustom &&
                                adj.Z >= belowMinZ && adj.Z < minZ &&
                                (adj.State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR |
                                             CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR |
                                             CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF |
                                             CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FIXTURE |
                                             CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL)) == 0 &&
                                (adj.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0)
                            {
                                return true;
                            }
                        }
                    }
                }

                // 3) No below-support. Check if there's a validated same-floor wall
                //    neighbor (propagation from walls that do have below-support).
                int[] dx = { -1, 1, 0, 0 };
                int[] dy = { 0, 0, -1, 1 };

                for (int d = 0; d < 4; d++)
                {
                    foreach (Multi neighbor in house.GetMultiAt(item.X + dx[d], item.Y + dy[d]))
                    {
                        if (neighbor.IsCustom &&
                            neighbor.Z >= minZ && neighbor.Z < maxZ &&
                            (neighbor.State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR |
                                              CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR |
                                              CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF |
                                              CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FIXTURE |
                                              CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL)) == 0 &&
                            (neighbor.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0 &&
                            (neighbor.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            return true;
        }

        public bool ValidatePlaceStructure(Item foundationItem, House house, IEnumerable<Multi> multi, int minZ, int maxZ, int flags)
        {
            if (house == null)
            {
                return false;
            }

            var validatedFloors = new List<Point>();
            foreach (Multi item in multi)
            {
                validatedFloors.Clear();

                if (item.IsCustom && (item.State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FIXTURE)) == 0 && item.Z >= minZ && item.Z < maxZ)
                {
                    (int info1, int info2) = _data.SeekGraphic(_facade.ObjectsInfo, item.Graphic);

                    if (info1 != -1 && info2 != -1)
                    {
                        CustomHousePlaceInfo info = _facade.ObjectsInfo[info1];

                        if ((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_DIRECT_SUPPORT) != 0)
                        {
                            if ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) != 0 || info.DirectSupports == 0)
                            {
                                continue;
                            }

                            if ((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_CANGO_W) != 0)
                            {
                                if (info.CanGoW != 0)
                                {
                                    return true;
                                }
                            }
                            else if ((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_CANGO_N) != 0)
                            {
                                if (info.CanGoN != 0)
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                return true;
                            }
                        }
                        else if (((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_BOTTOM) != 0 && info.Bottom != 0) || ((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_TOP) != 0 && info.Top != 0))
                        {
                            if ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) == 0)
                            {
                                item.State |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE;

                                if (!ValidateItemPlace
                                (
                                    foundationItem,
                                    item,
                                    minZ,
                                    maxZ,
                                    validatedFloors
                                ))
                                {
                                    item.State = item.State | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE;
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
                                    if ((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_N) != 0 && info.AdjUN != 0)
                                    {
                                        return true;
                                    }

                                    if ((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_E) != 0 && info.AdjUE != 0)
                                    {
                                        return true;
                                    }

                                    if ((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_S) != 0 && info.AdjUS != 0)
                                    {
                                        return true;
                                    }

                                    if ((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_W) != 0 && info.AdjUW != 0)
                                    {
                                        return true;
                                    }
                                }
                                else
                                {
                                    if ((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_N) != 0 && info.AdjLN != 0)
                                    {
                                        return true;
                                    }

                                    if ((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_E) != 0 && info.AdjLE != 0)
                                    {
                                        return true;
                                    }

                                    if ((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_S) != 0 && info.AdjLS != 0)
                                    {
                                        return true;
                                    }

                                    if ((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_W) != 0 && info.AdjLW != 0)
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
    }
}
