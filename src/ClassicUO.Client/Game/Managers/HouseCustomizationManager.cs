// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Network;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Managers
{
    internal struct CustomBuildObject
    {
        public CustomBuildObject(ushort graphic)
        {
            Graphic = graphic;
            X = Y = Z = 0;
        }

        public ushort Graphic;
        public int X, Y, Z;
    }

    internal sealed class HouseCustomizationManager
    {
        public readonly List<CustomHouseWallCategory> Walls = new List<CustomHouseWallCategory>();
        public readonly List<CustomHouseFloor> Floors = new List<CustomHouseFloor>();
        public readonly List<CustomHouseDoor> Doors = new List<CustomHouseDoor>();
        public readonly List<CustomHouseMiscCategory> Miscs = new List<CustomHouseMiscCategory>();
        public readonly List<CustomHouseStair> Stairs = new List<CustomHouseStair>();
        public readonly List<CustomHouseTeleport> Teleports = new List<CustomHouseTeleport>();
        public readonly List<CustomHouseRoofCategory> Roofs = new List<CustomHouseRoofCategory>();
        public readonly List<CustomHousePlaceInfo> ObjectsInfo = new List<CustomHousePlaceInfo>();

        private readonly World _world;
        private Rectangle _bounds;

        public HouseCustomizationManager(World world, uint serial)
        {
            _world = world;
            Serial = serial;

            var fileManager = Client.Game.UO.FileManager;
            // TODO: don't load the file txt every time the housemanager get initialized
            ParseFileWithCategory<CustomHouseWall, CustomHouseWallCategory>(Walls, fileManager.GetUOFilePath("walls.txt"));

            ParseFile(Floors, fileManager.GetUOFilePath("floors.txt"));
            ParseFile(Doors, fileManager.GetUOFilePath("doors.txt"));

            ParseFileWithCategory<CustomHouseMisc, CustomHouseMiscCategory>(Miscs, fileManager.GetUOFilePath("misc.txt"));

            ParseFile(Stairs, fileManager.GetUOFilePath("stairs.txt"));
            ParseFile(Teleports, fileManager.GetUOFilePath("teleprts.txt"));

            ParseFileWithCategory<CustomHouseRoof, CustomHouseRoofCategory>(Roofs, fileManager.GetUOFilePath("roof.txt"));

            ParseFile(ObjectsInfo, fileManager.GetUOFilePath("suppinfo.txt"));
            //


            InitializeHouse();
        }

        public int Category = -1, MaxPage = 1, CurrentFloor = 1, FloorCount = 4, RoofZ = 1, MinHouseZ = -120, Components, Fixtures, MaxComponets, MaxFixtures;
        public bool Erasing, SeekTile, ShowWindow, CombinedStair;


        public readonly int[] FloorVisionState = new int[4];


        public ushort SelectedGraphic;

        public readonly uint Serial;
        public Point StartPos, EndPos;
        public CUSTOM_HOUSE_GUMP_STATE State = CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL;

        private void InitializeHouse()
        {
            Item foundation = _world.Items.Get(Serial);

            if (foundation == null)
            {
                return;
            }

            MinHouseZ = foundation.Z + 7;

            if (foundation.MultiInfo.HasValue)
            {
                var multi = foundation.MultiInfo.Value;

                StartPos.X = foundation.X + multi.X + 1;
                StartPos.Y = foundation.Y + multi.Y + 1;
                EndPos.X = foundation.X + multi.Width + 1;
                EndPos.Y = foundation.Y + multi.Height + 1;
            }

            int width = Math.Abs(EndPos.X - StartPos.X);
            int height = Math.Abs(EndPos.Y - StartPos.Y);

            _bounds = new Rectangle(StartPos.X - 1, StartPos.Y - 1, width + 1, height + 2);

            if (width >= 13 || height >= 13)
            {
                FloorCount = 4;
            }
            else
            {
                FloorCount = 3;
            }

            int componentsOnFloor = (width - 1) * (height - 1);

            MaxComponets = FloorCount * (componentsOnFloor + 2 * (width + height) - 4) - (int) (FloorCount * componentsOnFloor * -0.25) + 2 * width + 3 * height - 5;

            MaxFixtures = MaxComponets / 20;
        }

        public void GenerateFloorPlace()
        {
            Item foundationItem = _world.Items.Get(Serial);

            if (foundationItem == null || !_world.HouseManager.TryGetHouse(Serial, out House house))
            {
                return;
            }

            house.ClearCustomHouseComponents(CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL);

            foreach (Multi item in house.Components)
            {
                if (!item.IsCustom)
                {
                    continue;
                }

                int currentFloor = -1;
                int floorZ = foundationItem.Z + 7;
                int itemZ = item.Z;

                bool ignore = false;

                for (int i = 0; i < 4; i++)
                {
                    int offset = 0 /*i != 0 ? 0 : 7*/;

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

                (int floorCheck1, int floorCheck2) = SeekGraphicInCustomHouseObjectList(Floors, item.Graphic);

                CUSTOM_HOUSE_MULTI_OBJECT_FLAGS state = item.State;

                if (floorCheck1 != -1 && floorCheck2 != -1)
                {
                    state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR;

                    if (FloorVisionState[currentFloor] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_HIDE_FLOOR)
                    {
                        state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER;
                    }
                    else if (FloorVisionState[currentFloor] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_TRANSPARENT_FLOOR
                             || FloorVisionState[currentFloor] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_TRANSLUCENT_FLOOR)
                    {
                        state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_TRANSPARENT;
                    }
                }
                else
                {
                    (int stairCheck1, int stairCheck2) = SeekGraphicInCustomHouseObjectList(Stairs, item.Graphic);

                    if (stairCheck1 != -1 && stairCheck2 != -1)
                    {
                        state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR;
                    }
                    else
                    {
                        (int roofCheck1, int roofCheck2) = SeekGraphicInCustomHouseObjectListWithCategory<CustomHouseRoof, CustomHouseRoofCategory>(Roofs, item.Graphic);

                        if (roofCheck1 != -1 && roofCheck2 != -1)
                        {
                            state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF;
                        }
                        else
                        {
                            (int fixtureCheck1, int fixtureCheck2) = SeekGraphicInCustomHouseObjectList(Doors, item.Graphic);

                            if (fixtureCheck1 == -1 || fixtureCheck2 == -1)
                            {
                                (fixtureCheck1, fixtureCheck2) = SeekGraphicInCustomHouseObjectList(Teleports, item.Graphic);

                                if (fixtureCheck1 != -1 && fixtureCheck2 != -1)
                                {
                                    state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR;
                                }
                            }
                            else
                            {
                                state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FIXTURE;
                            }
                        }
                    }

                    if (!ignore)
                    {
                        if (FloorVisionState[currentFloor] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_HIDE_CONTENT)
                        {
                            state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER;
                        }
                        else if (FloorVisionState[currentFloor] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_TRANSPARENT_CONTENT)
                        {
                            state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_TRANSPARENT;
                        }
                    }
                }

                if (!ignore)
                {
                    if (FloorVisionState[currentFloor] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_HIDE_ALL)
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
                    IEnumerable<Multi> multi = house.Components.Where(s => s.X == x && s.Y == y);

                    if (multi == null)
                    {
                        continue;
                    }

                    Multi floorMulti = null;
                    Multi floorCustomMulti = null;

                    foreach (Multi item in multi)
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
                        Multi mo = house.Add
                        (
                            floorMulti.Graphic,
                            0,
                            (ushort) (foundationItem.X + (x - foundationItem.X)),
                            (ushort) (foundationItem.Y + (y - foundationItem.Y)),
                            (sbyte) z,
                            true,
                            false
                        );

                        mo.AlphaHue = 0xFF;

                        CUSTOM_HOUSE_MULTI_OBJECT_FLAGS state = CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR;

                        if (FloorVisionState[0] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_HIDE_FLOOR)
                        {
                            state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER;
                        }
                        else if (FloorVisionState[0] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_TRANSPARENT_FLOOR
                                 || FloorVisionState[0] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_TRANSLUCENT_FLOOR)
                        {
                            state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_TRANSPARENT;
                        }

                        mo.State = state;
                    }
                }
            }

            var validatedFloors = new List<Point>();
            for (int i = 0; i < FloorCount; i++)
            {
                int minZ = foundationItem.Z + 7 + i * 20;
                int maxZ = minZ + 20;

                for (int j = 0; j < 2; j++)
                {
                    validatedFloors.Clear();

                    for (int x = _bounds.X; x < EndPos.X + 1; x++)
                    {
                        for (int y = _bounds.Y; y < EndPos.Y + 1; y++)
                        {
                            IEnumerable<Multi> multi = house.GetMultiAt(x, y);

                            if (multi == null)
                            {
                                continue;
                            }

                            foreach (Multi item in multi)
                            {
                                if (!item.IsCustom)
                                {
                                    continue;
                                }

                                if (j == 0)
                                {
                                    if (i == 0 && item.Z < minZ)
                                    {
                                        item.State = item.State | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE;

                                        continue;
                                    }

                                    if ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) == 0)
                                    {
                                        continue;
                                    }

                                    if (i == 0 && item.Z >= minZ && item.Z < maxZ)
                                    {
                                        item.State = item.State | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE;

                                        continue;
                                    }
                                }

                                if ((item.State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL)) == 0 &&
                                    item.Z >= minZ && item.Z < maxZ)
                                {
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
                            }
                        }
                    }

                    if (i != 0 && j == 0)
                    {
                        foreach (Point point in validatedFloors)
                        {
                            IEnumerable<Multi> multi = house.GetMultiAt(point.X, point.Y);

                            if (multi == null)
                            {
                                continue;
                            }

                            foreach (Multi item in multi)
                            {
                                if (item.IsCustom && (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0 && item.Z >= minZ && item.Z < maxZ)
                                {
                                    item.State = item.State & ~CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE;
                                }
                            }
                        }

                        for (int x = _bounds.X; x < EndPos.X + 1; x++)
                        {
                            int minY = 0, maxY = 0;

                            for (int y = _bounds.Y; y < EndPos.Y + 1; y++)
                            {
                                IEnumerable<Multi> multi = house.GetMultiAt(x, y);

                                if (multi == null)
                                {
                                    continue;
                                }

                                foreach (Multi item in multi)
                                {
                                    if (item.IsCustom && (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0 && item.Z >= minZ && item.Z < maxZ)
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

                            for (int y = EndPos.Y; y >= _bounds.Y; y--)
                            {
                                IEnumerable<Multi> multi = house.GetMultiAt(x, y);

                                if (multi == null)
                                {
                                    continue;
                                }

                                foreach (Multi item in multi)
                                {
                                    if (item.IsCustom && (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0 && item.Z >= minZ && item.Z < maxZ)
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
                                IEnumerable<Multi> multi = house.GetMultiAt(x, y);

                                if (multi == null)
                                {
                                    continue;
                                }

                                foreach (Multi item in multi)
                                {
                                    if (item.IsCustom && (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0 && item.Z >= minZ && item.Z < maxZ)
                                    {
                                        item.State = item.State & ~CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE;
                                    }
                                }
                            }
                        }

                        for (int y = _bounds.Y; y < EndPos.Y + 1; y++)
                        {
                            int minX = 0;
                            int maxX = 0;

                            for (int x = _bounds.X; x < EndPos.X + 1; x++)
                            {
                                IEnumerable<Multi> multi = house.GetMultiAt(x, y);

                                if (multi == null)
                                {
                                    continue;
                                }

                                foreach (Multi item in multi)
                                {
                                    if (item.IsCustom && (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0 && item.Z >= minZ && item.Z < maxZ)
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

                            for (int x = EndPos.X; x >= _bounds.X; x--)
                            {
                                IEnumerable<Multi> multi = house.GetMultiAt(x, y);

                                if (multi == null)
                                {
                                    continue;
                                }

                                foreach (Multi item in multi)
                                {
                                    if (item.IsCustom && (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0 && item.Z >= minZ && item.Z < maxZ)
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
                                IEnumerable<Multi> multi = house.GetMultiAt(x, y);

                                if (multi == null)
                                {
                                    continue;
                                }

                                foreach (Multi item in multi)
                                {
                                    if (item.IsCustom && (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0 && item.Z >= minZ && item.Z < maxZ)
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
                for (int x = _bounds.X; x < EndPos.X; x++)
                {
                    for (int y = _bounds.Y; y < EndPos.Y; y++)
                    {
                        var mo = house.Add
                        (
                            0x0496,
                            (ushort)(x == _bounds.X || y == _bounds.Y ? 0x34 : color),
                            (ushort)(foundationItem.X + (x - foundationItem.X)),
                            (ushort)(foundationItem.Y + (y - foundationItem.Y)),
                            (sbyte) z,
                            true,
                            false
                        );

                        mo.AlphaHue = 0xFF;
                        mo.State = CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_TRANSPARENT;
                        mo.AddToTile();
                    }
                }

                color += 5;
                z += 20;
            }
        }

        public void OnTargetWorld(GameObject place)
        {
            if (place == null /*&& place is Multi m*/)
            {
                return;
            }

            if (!_bounds.Contains(place.X, place.Y))
                return;

            // apply a minor offset for roof tiles
            int zOffset = -3;

            HouseCustomizationGump gump = UIManager.GetGump<HouseCustomizationGump>(Serial);

            if (CurrentFloor == 1)
            {
                zOffset = -7;
            }

            if (SeekTile)
            {
                if (place is Multi)
                {
                    SeekGraphic(place.Graphic);
                }
            }
            else if (place.Z >= _world.Player.Z + zOffset && place.Z < _world.Player.Z + 20)
            {
                Item foundationItem = _world.Items.Get(Serial);

                if (foundationItem == null || !_world.HouseManager.TryGetHouse(Serial, out House house))
                {
                    return;
                }

                if (Erasing)
                {
                    if (!(place is Multi))
                    {
                        return;
                    }

                    if (CanEraseHere(place, out CUSTOM_HOUSE_BUILD_TYPE type))
                    {
                        IEnumerable<Multi> multi = house.GetMultiAt(place.X, place.Y);

                        if (multi == null || !multi.Any())
                        {
                            return;
                        }

                        int z = 7 + (CurrentFloor - 1) * 20;

                        if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR || type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_ROOF)
                        {
                            z = place.Z - (foundationItem.Z + z) + z;
                        }

                        if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_ROOF)
                        {
                            NetClient.Socket.Send_CustomHouseDeleteRoof(_world, place.Graphic, place.X - foundationItem.X, place.Y - foundationItem.Y, z);
                        }
                        else
                        {
                            NetClient.Socket.Send_CustomHouseDeleteItem(_world, place.Graphic, place.X - foundationItem.X, place.Y - foundationItem.Y, z);
                        }

                        place.Destroy();
                    }
                }
                else if (SelectedGraphic != 0)
                {
                    var list = new List<CustomBuildObject>();

                    if (CanBuildHere(list, out CUSTOM_HOUSE_BUILD_TYPE type) && list.Count > 0)
                    {
                        //if (type != CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR && !(place is Multi))
                        //    return;

                        int placeX = place.X;
                        int placeY = place.Y;

                        if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR && CombinedStair)
                        {
                            if (gump.Page >= 0 && gump.Page < Stairs.Count)
                            {
                                CustomHouseStair stair = Stairs[gump.Page];

                                ushort graphic = 0;

                                if (SelectedGraphic == stair.North)
                                {
                                    graphic = (ushort) stair.MultiNorth;
                                }
                                else if (SelectedGraphic == stair.East)
                                {
                                    graphic = (ushort) stair.MultiEast;
                                }
                                else if (SelectedGraphic == stair.South)
                                {
                                    graphic = (ushort) stair.MultiSouth;
                                }
                                else if (SelectedGraphic == stair.West)
                                {
                                    graphic = (ushort) stair.MultiWest;
                                }

                                if (graphic != 0)
                                {
                                    NetClient.Socket.Send_CustomHouseAddStair(_world, graphic, placeX - foundationItem.X, placeY - foundationItem.Y);
                                }
                            }
                        }
                        else
                        {
                            CustomBuildObject item = list[0];

                            int x = placeX - foundationItem.X + item.X;
                            int y = placeY - foundationItem.Y + item.Y;
                            IEnumerable<Multi> multi = house.GetMultiAt(placeX + item.X, placeY + item.Y);

                            if (multi.Any() || type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR)
                            {
                                if (!CombinedStair)
                                {
                                    int minZ = foundationItem.Z + 7 + (CurrentFloor - 1) * 20;
                                    int maxZ = minZ + 20;

                                    if (CurrentFloor == 1)
                                    {
                                        minZ -= 7;
                                    }

                                    foreach (Multi multiObject in multi)
                                    {
                                        int testMinZ = minZ;

                                        if ((multiObject.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF) != 0)
                                        {
                                            testMinZ -= 3;
                                        }

                                        if (multiObject.Z < testMinZ || multiObject.Z >= maxZ || !multiObject.IsCustom || (multiObject.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL) != 0 /*|| (multiObject.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_DONT_REMOVE) != 0*/
                                           )
                                        {
                                            continue;
                                        }

                                        if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR)
                                        {
                                            if ((multiObject.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR) != 0)
                                            {
                                                multiObject.Destroy();
                                            }
                                        }
                                        else if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_ROOF)
                                        {
                                            if ((multiObject.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF) != 0)
                                            {
                                                multiObject.Destroy();
                                            }
                                        }
                                        else if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_FLOOR)
                                        {
                                            if ((multiObject.State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FIXTURE)) != 0)
                                            {
                                                multiObject.Destroy();
                                            }
                                        }
                                        else
                                        {
                                            if ((multiObject.State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_DONT_REMOVE)) == 0)
                                            {
                                                multiObject.Destroy();
                                            }
                                        }
                                    }

                                    // todo: remove foundation if no components
                                }

                                if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_ROOF)
                                {
                                    NetClient.Socket.Send_CustomHouseAddRoof(_world, item.Graphic, x, y, item.Z);
                                }
                                else
                                {
                                    NetClient.Socket.Send_CustomHouseAddItem(_world, item.Graphic, x, y);
                                }
                            }
                        }

                        int xx = placeX - foundationItem.X;
                        int yy = placeY - foundationItem.Y;
                        int z = foundationItem.Z + 7 + (CurrentFloor - 1) * 20;

                        if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR && !CombinedStair)
                        {
                            z = foundationItem.Z;
                        }

                        foreach (CustomBuildObject item in list)
                        {
                            house.Add
                            (
                                item.Graphic,
                                0,
                                (ushort) (foundationItem.X + xx + item.X),
                                (ushort) (foundationItem.Y + yy + item.Y),
                                (sbyte) (z + item.Z),
                                true,
                                false
                            );
                        }
                    }
                }

                GenerateFloorPlace();
                gump.Update();
            }
        }

        private void SeekGraphic(ushort graphic)
        {
            CUSTOM_HOUSE_GUMP_STATE state = 0;
            (int res1, int res2) = ExistsInList(ref state, graphic);

            if (res1 != -1 && res2 != -1)
            {
                State = state;
                HouseCustomizationGump gump = UIManager.GetGump<HouseCustomizationGump>(Serial);

                if (State == CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL || State == CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF || State == CUSTOM_HOUSE_GUMP_STATE.CHGS_MISC)
                {
                    Category = res1;
                    gump.Page = res2;
                }
                else
                {
                    Category = -1;
                    gump.Page = res1;
                }

                gump.UpdateMaxPage();
                SetTargetMulti();
                SelectedGraphic = graphic;
                gump.Update();
            }
        }

        public void SetTargetMulti()
        {
            _world.TargetManager.SetTargetingMulti
            (
                0,
                0,
                0,
                0,
                0,
                0
            );

            Erasing = false;
            SeekTile = false;
            SelectedGraphic = 0;
            CombinedStair = false;
        }

        public bool CanBuildHere(List<CustomBuildObject> list, out CUSTOM_HOUSE_BUILD_TYPE type)
        {
            type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_NORMAL;

            if (SelectedGraphic == 0)
            {
                return false;
            }

            var foundationItem = _world.Items.Get(Serial);

            if (foundationItem == null || !_world.HouseManager.TryGetHouse(foundationItem, out House house))
                return false;

            bool result = true;

            if (CombinedStair)
            {
                if (Components + 10 > MaxComponets)
                {
                    return false;
                }

                (int res1, int res2) = SeekGraphicInCustomHouseObjectList(Stairs, SelectedGraphic);

                if (res1 == -1 || res2 == -1 || res1 >= Stairs.Count)
                {
                    list.Add(new CustomBuildObject()
                    {
                        Graphic = SelectedGraphic,
                        X = 0,
                        Y = 0,
                        Z = 0
                    });

                    return false;
                }

                CustomHouseStair item = Stairs[res1];

                if (SelectedGraphic == item.North)
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
                else if (SelectedGraphic == item.East)
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
                else if (SelectedGraphic == item.South)
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
                else if (SelectedGraphic == item.West)
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
                    list.Add(new CustomBuildObject { Graphic = SelectedGraphic, X = 0, Y = 0, Z = 0 });
                }

                type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR;
            }
            else
            {
                (int fixCheck1, int fixCheck2) = SeekGraphicInCustomHouseObjectList(Doors, SelectedGraphic);

                bool isFixture = false;

                if (fixCheck1 == -1 || fixCheck2 == -1)
                {
                    (fixCheck1, fixCheck2) = SeekGraphicInCustomHouseObjectList(Teleports, SelectedGraphic);

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
                    if (Fixtures + 1 > MaxFixtures)
                    {
                        result = false;
                    }
                }
                else if (Components + 1 > MaxComponets)
                {
                    result = false;
                }

                if (State == CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF)
                {
                    list.Add(new CustomBuildObject { Graphic = SelectedGraphic, X = 0, Y = 0, Z = (RoofZ - 2) * 3 });
                    type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_ROOF;
                }
                else
                {
                    if (State == CUSTOM_HOUSE_GUMP_STATE.CHGS_STAIR)
                    {
                        list.Add(new CustomBuildObject { Graphic = SelectedGraphic, X = 0, Y = 1, Z = 0 });
                        type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR;
                    }
                    else
                    {
                        if (State == CUSTOM_HOUSE_GUMP_STATE.CHGS_FLOOR)
                        {
                            type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_FLOOR;
                        }

                        list.Add(new CustomBuildObject { Graphic = SelectedGraphic, X = 0, Y = 0, Z = 0 });
                    }
                }
            }

            if (SelectedObject.Object is GameObject gobj)
            {
                if (!_bounds.Contains(gobj.X, gobj.Y))
                    return false;

                var minZ = foundationItem.Z + 0 + (CurrentFloor - 1) * 20;
                var maxZ = minZ + 20;

                // var boundsOffset = State != CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL ? 1 : 0;

                for (var i = 0; i < list.Count; ++i)
                {
                    var item = list[i];
                    if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR)
                    {
                        if (CombinedStair)
                        {
                            if (item.Z != 0)
                                continue;
                        }
                        else
                        {
                            if (gobj.Y + item.Y < EndPos.Y || gobj.X + item.X == _bounds.X || gobj.Z >= MinHouseZ)
                                return false;

                            if (gobj.Y + item.Y != EndPos.Y)
                            {
                                item.Y = 0;
                                list[0] = item;
                            }
                            continue;
                        }
                    }

                    if (!ValidateItemPlace(_bounds, item.Graphic, gobj.X + item.X, gobj.Y + item.Y))
                    {
                        return false;
                    }

                    if (type != CUSTOM_HOUSE_BUILD_TYPE.CHBT_FLOOR)
                    {
                        foreach (var multi in house.GetMultiAt(gobj.X + item.X, gobj.Y + item.Y))
                        {
                            if (!multi.IsCustom)
                                continue;

                            if ((multi.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL) == 0 && multi.Z >= minZ && multi.Z < maxZ)
                            {
                                if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR)
                                {
                                    if ((multi.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) == 0)
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
                    else if (_bounds.Contains(place.X, place.Y) && place.Z >= MinHouseZ)
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
            (int res1, int res2) = SeekGraphicInCustomHouseObjectListWithCategory<CustomHouseWall, CustomHouseWallCategory>(Walls, graphic);

            if (res1 == -1 || res2 == -1)
            {
                (res1, res2) = SeekGraphicInCustomHouseObjectList(Floors, graphic);

                if (res1 == -1 || res2 == -1)
                {
                    (res1, res2) = SeekGraphicInCustomHouseObjectList(Doors, graphic);

                    if (res1 == -1 || res2 == -1)
                    {
                        (res1, res2) = SeekGraphicInCustomHouseObjectListWithCategory<CustomHouseMisc, CustomHouseMiscCategory>(Miscs, graphic);

                        if (res1 == -1 || res2 == -1)
                        {
                            (res1, res2) = SeekGraphicInCustomHouseObjectList(Stairs, graphic);

                            if (res1 == -1 || res2 == -1)
                            {
                                (res1, res2) = SeekGraphicInCustomHouseObjectListWithCategory<CustomHouseRoof, CustomHouseRoofCategory>(Roofs, graphic);

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
                            (int res_1, int res_2) = SeekGraphicInCustomHouseObjectList(Teleports, graphic);

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

        private bool ValidateItemPlace(Rectangle rect, ushort graphic, int x, int y)
        {
            if (!rect.Contains(x, y))
            {
                return false;
            }

            (int infoCheck1, int infoCheck2) = SeekGraphicInCustomHouseObjectList(ObjectsInfo, graphic);

            if (infoCheck1 != -1 && infoCheck2 != -1)
            {
                CustomHousePlaceInfo info = ObjectsInfo[infoCheck1];

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


            if ((item.State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FIXTURE)) != 0)
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


            (int infoCheck1, int infoCheck2) = SeekGraphicInCustomHouseObjectList(ObjectsInfo, item.Graphic);

            if (infoCheck1 != -1 && infoCheck2 != -1)
            {
                CustomHousePlaceInfo info = ObjectsInfo[infoCheck1];

                if (info.CanGoW == 0 && item.X == _bounds.X)
                {
                    return false;
                }

                if (info.CanGoN == 0 && item.Y == _bounds.Y)
                {
                    return false;
                }

                if (info.CanGoNWS == 0 && item.X == _bounds.X && item.Y == _bounds.Y)
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

                    if (!found)
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

                    if (!found)
                    {
                        return false;
                    }
                }

                // if ((item.State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR |
                //                    CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF |
                //                    CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR |
                //                    CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_DONT_REMOVE)) == 0)
                // {
                //     var multis = house.GetMultiAt(item.X, item.Y).Where(s => s.IsCustom && s.Z < item.Z - 20);
                //
                //     if (multis.Any())
                //     {
                //         if (multis.Any(s => (s.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) != 0))
                //             return false;
                //
                //         if (!multis.Any
                //             (
                //                 s => (s.State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF |
                //                                  CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_DONT_REMOVE)) == 0
                //             ))
                //             return false;
                //     }
                // }
            }

            return true;
        }

        public bool ValidatePlaceStructure
        (
            Item foundationItem,
            House house,
            IEnumerable<Multi> multi,
            int minZ,
            int maxZ,
            int flags
        )
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
                    (int info1, int info2) = SeekGraphicInCustomHouseObjectList(ObjectsInfo, item.Graphic);

                    if (info1 != -1 && info2 != -1)
                    {
                        CustomHousePlaceInfo info = ObjectsInfo[info1];

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

        private void ParseFile<T>(List<T> list, string path) where T : CustomHouseObject, new()
        {
            FileInfo file = new FileInfo(path);

            if (!file.Exists)
            {
                return;
            }

            using (StreamReader reader = File.OpenText(file.FullName))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    T item = new T();

                    if (item.Parse(line))
                    {
                        if (item.FeatureMask == 0 || ((int)_world.ClientLockedFeatures.Flags & item.FeatureMask) != 0)
                        {
                            list.Add(item);
                        }
                    }
                }
            }
        }

        private void ParseFileWithCategory<T, U>(List<U> list, string path) where T : CustomHouseObject, new() where U : CustomHouseObjectCategory<T>, new()
        {
            FileInfo file = new FileInfo(path);

            if (!file.Exists)
            {
                return;
            }

            using (StreamReader reader = File.OpenText(file.FullName))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    T item = new T();

                    if (item.Parse(line))
                    {
                        if (item.FeatureMask != 0 && ((int)_world.ClientLockedFeatures.Flags & item.FeatureMask) == 0)
                        {
                            continue;
                        }

                        bool found = false;

                        foreach (U c in list)
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


        private static (int, int) SeekGraphicInCustomHouseObjectListWithCategory<T, U>(List<U> list, ushort graphic) where T : CustomHouseObject where U : CustomHouseObjectCategory<T>
        {
            for (int i = 0; i < list.Count; i++)
            {
                U c = list[i];

                for (int j = 0; j < c.Items.Count; j++)
                {
                    int contains = c.Items[j].Contains(graphic);

                    if (contains != -1)
                    {
                        return (i, j);
                    }
                }
            }

            return (-1, -1);
        }

        private static (int, int) SeekGraphicInCustomHouseObjectList<T>(List<T> list, ushort graphic) where T : CustomHouseObject
        {
            for (int i = 0; i < list.Count; i++)
            {
                int contains = list[i].Contains(graphic);

                if (contains != -1)
                {
                    return (i, graphic);
                }
            }

            return (-1, -1);
        }
    }

    internal enum CUSTOM_HOUSE_GUMP_STATE
    {
        CHGS_WALL = 0,
        CHGS_DOOR,
        CHGS_FLOOR,
        CHGS_STAIR,
        CHGS_ROOF,
        CHGS_MISC,
        CHGS_MENU,
        CHGS_FIXTURE
    }

    internal enum CUSTOM_HOUSE_FLOOR_VISION_STATE
    {
        CHGVS_NORMAL = 0,
        CHGVS_TRANSPARENT_CONTENT,
        CHGVS_HIDE_CONTENT,
        CHGVS_TRANSPARENT_FLOOR,
        CHGVS_HIDE_FLOOR,
        CHGVS_TRANSLUCENT_FLOOR,
        CHGVS_HIDE_ALL
    }

    internal enum CUSTOM_HOUSE_BUILD_TYPE
    {
        CHBT_NORMAL = 0,
        CHBT_ROOF,
        CHBT_FLOOR,
        CHBT_STAIR
    }

    [Flags]
    internal enum CUSTOM_HOUSE_MULTI_OBJECT_FLAGS
    {
        CHMOF_GENERIC_INTERNAL = 0x01,
        CHMOF_FLOOR = 0x02,
        CHMOF_STAIR = 0x04,
        CHMOF_ROOF = 0x08,
        CHMOF_FIXTURE = 0x10,
        CHMOF_TRANSPARENT = 0x20,
        CHMOF_IGNORE_IN_RENDER = 0x40,
        CHMOF_VALIDATED_PLACE = 0x80,
        CHMOF_INCORRECT_PLACE = 0x100,

        CHMOF_DONT_REMOVE = 0x200,
        CHMOF_PREVIEW = 0x400
    }

    [Flags]
    internal enum CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS
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
