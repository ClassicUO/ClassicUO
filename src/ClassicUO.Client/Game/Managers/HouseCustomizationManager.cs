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

            int plotWidth = width + 1;
            int plotHeight = height + 1;
            int componentsOnFloor = (plotWidth - 1) * (plotHeight - 1);

            MaxComponets = FloorCount * (componentsOnFloor + 2 * (plotWidth + plotHeight) - 4) - (int) (FloorCount * componentsOnFloor * -0.25) + 2 * plotWidth + 3 * plotHeight - 5;

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

                        CUSTOM_HOUSE_MULTI_OBJECT_FLAGS state = CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL;

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

            // Recompute per-piece legality on the floor grid and flag any piece
            // that cannot be legally supported in its current position.
            ValidateDesignGrid(foundationItem, house);

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

                        if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR)
                        {
                            int floorBase = foundationItem.Z;
                            int stairFloorBase = floorBase;

                            for (int f = 0; f < FloorCount; f++)
                            {
                                int fz = floorBase + 7 + f * 20;

                                if (place.Z >= fz && place.Z < fz + 20)
                                {
                                    stairFloorBase = fz;
                                    break;
                                }
                            }

                            if (place.Z < floorBase + 7)
                                stairFloorBase = floorBase;

                            // Collect stair pieces sharing same X (N/S) or same Y (E/W) with clicked piece
                            var sameXPieces = new List<Multi>();
                            var sameYPieces = new List<Multi>();

                            foreach (Multi comp in house.Components)
                            {
                                if (comp.IsDestroyed || !comp.IsCustom)
                                    continue;

                                if ((comp.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR) == 0)
                                    continue;

                                if (comp.Z < stairFloorBase || comp.Z >= stairFloorBase + 20)
                                    continue;

                                if (comp.X == place.X)
                                    sameXPieces.Add(comp);

                                if (comp.Y == place.Y)
                                    sameYPieces.Add(comp);
                            }

                            // Determine orientation by piece count, then find exact 4-tile group
                            var stairPieces = new List<Multi>();

                            if (sameXPieces.Count >= sameYPieces.Count && sameXPieces.Count > 0)
                            {
                                // N/S orientation - find best 4-consecutive-Y window containing place.Y
                                int bestCount = 0;
                                int bestStart = place.Y;

                                for (int startY = place.Y - 3; startY <= place.Y; startY++)
                                {
                                    int count = 0;

                                    foreach (var p in sameXPieces)
                                    {
                                        if (p.Y >= startY && p.Y <= startY + 3)
                                            count++;
                                    }

                                    if (count > bestCount)
                                    {
                                        bestCount = count;
                                        bestStart = startY;
                                    }
                                }

                                foreach (var p in sameXPieces)
                                {
                                    if (p.Y >= bestStart && p.Y <= bestStart + 3)
                                        stairPieces.Add(p);
                                }
                            }
                            else if (sameYPieces.Count > 0)
                            {
                                // E/W orientation - find best 4-consecutive-X window containing place.X
                                int bestCount = 0;
                                int bestStart = place.X;

                                for (int startX = place.X - 3; startX <= place.X; startX++)
                                {
                                    int count = 0;

                                    foreach (var p in sameYPieces)
                                    {
                                        if (p.X >= startX && p.X <= startX + 3)
                                            count++;
                                    }

                                    if (count > bestCount)
                                    {
                                        bestCount = count;
                                        bestStart = startX;
                                    }
                                }

                                foreach (var p in sameYPieces)
                                {
                                    if (p.X >= bestStart && p.X <= bestStart + 3)
                                        stairPieces.Add(p);
                                }
                            }

                            // Combined staircases have pieces at multiple Z levels (0/5/10/15 offsets).
                            // Single stairs are all at one Z. Only group-delete for combined staircases.
                            bool isCombined = false;

                            if (stairPieces.Count > 1)
                            {
                                int firstZ = stairPieces[0].Z;

                                for (int i = 1; i < stairPieces.Count; i++)
                                {
                                    if (stairPieces[i].Z != firstZ)
                                    {
                                        isCombined = true;
                                        break;
                                    }
                                }
                            }

                            if (isCombined)
                            {
                                foreach (Multi piece in stairPieces)
                                {
                                    int pz = piece.Z - (foundationItem.Z + (7 + (CurrentFloor - 1) * 20)) + (7 + (CurrentFloor - 1) * 20);

                                    NetClient.Socket.Send_CustomHouseDeleteItem(_world, piece.Graphic, piece.X - foundationItem.X, piece.Y - foundationItem.Y, pz);
                                    piece.Destroy();
                                }
                            }
                            else
                            {
                                NetClient.Socket.Send_CustomHouseDeleteItem(_world, place.Graphic, place.X - foundationItem.X, place.Y - foundationItem.Y, z);
                                place.Destroy();
                            }
                        }
                        else if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_ROOF)
                        {
                            NetClient.Socket.Send_CustomHouseDeleteRoof(_world, place.Graphic, place.X - foundationItem.X, place.Y - foundationItem.Y, z);
                            place.Destroy();
                        }
                        else
                        {
                            NetClient.Socket.Send_CustomHouseDeleteItem(_world, place.Graphic, place.X - foundationItem.X, place.Y - foundationItem.Y, z);
                            place.Destroy();
                        }
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
                if (Components + 10 > MaxComponets || CurrentFloor >= FloorCount)
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

                    if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR && CombinedStair)
                    {
                        int tileX = gobj.X + item.X;
                        int tileY = gobj.Y + item.Y;

                        if (tileX < StartPos.X || tileX >= EndPos.X || tileY < StartPos.Y || tileY >= EndPos.Y)
                            return false;
                    }
                    else if (!ValidateItemPlace(_bounds, item.Graphic, gobj.X + item.X, gobj.Y + item.Y))
                    {
                        return false;
                    }

                    if (type != CUSTOM_HOUSE_BUILD_TYPE.CHBT_FLOOR)
                    {
                        foreach (var multi in house.GetMultiAt(gobj.X + item.X, gobj.Y + item.Y))
                        {
                            if (!multi.IsCustom)
                                continue;

                            int collisionMaxZ = (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR && CombinedStair) ? maxZ + 20 : maxZ;

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

        private CustomHousePlaceInfo GetPlaceInfo(ushort graphic)
        {
            if (graphic == 0)
            {
                return null;
            }

            (int i1, int i2) = SeekGraphicInCustomHouseObjectList(ObjectsInfo, graphic);

            return i1 != -1 && i2 != -1 ? ObjectsInfo[i1] : null;
        }

        // One cell of the floor-plan grid: the piece in each slot plus the legality and
        // support state settled by the validation passes.
        private struct DesignCell
        {
            public ushort Floor;
            public ushort Object;
            public ushort Roof;

            public bool FloorLegal;
            public bool RoofLegal;
            public byte ObjectLegal;   // 0 = not legal; higher values are support ranks

            public bool Visited;       // floor pass has processed this cell
            public bool Support;       // braced by a wall
            public bool FloorSupport;  // reached by a floor run
            public bool RoofSupport;   // reached by a roof run

            public bool Locked;        // fixed foundation cell, never validated
        }

        // Recompute the legality of every placed piece.
        //
        // The design is laid out on a width x height x floors grid of cells; each cell
        // tracks the floor, wall/object and roof piece on it. Legality is settled one
        // floor at a time through a fixed sequence of sweeps: reset, push support up from
        // the floor below, flow floor/roof support along runs back to a braced cell, then
        // mark floors, roofs and walls legal from the settled support and the per-piece
        // support columns. The settled legal flags are written back onto each Multi.
        private void ValidateDesignGrid(Item foundationItem, House house)
        {
            // The grid keeps an empty margin row/column at index 0: floor legality is
            // never settled there, so the plot proper must start at grid (1,1). Shift the
            // origin one tile out and widen by one to leave that margin.
            int ox = _bounds.X - 1;
            int oy = _bounds.Y - 1;
            int w = _bounds.Width + 1;
            int h = _bounds.Height + 1;
            int levels = FloorCount;

            if (w < 1 || w > 32 || h < 1 || h > 32 || levels < 1 || levels > 4)
            {
                return;
            }

            int baseZ = foundationItem.Z + 7;

            var grid = new DesignCell[32, 32, levels];

            int CanGoW(ushort gfx) { CustomHousePlaceInfo i = GetPlaceInfo(gfx); return i == null ? 0 : i.CanGoW; }
            int CanGoN(ushort gfx) { CustomHousePlaceInfo i = GetPlaceInfo(gfx); return i == null ? 0 : i.CanGoN; }
            int CanGoNWS(ushort gfx) { CustomHousePlaceInfo i = GetPlaceInfo(gfx); return i == null ? 0 : i.CanGoNWS; }
            int DirectSup(ushort gfx) { CustomHousePlaceInfo i = GetPlaceInfo(gfx); return i == null ? 0 : i.DirectSupports; }
            int Bottom(ushort gfx) { CustomHousePlaceInfo i = GetPlaceInfo(gfx); return i == null ? 0 : i.Bottom; }

            static bool InGrid(int x, int y) => (uint)x < 32 && (uint)y < 32;

            // Slot a graphic by its tile data: a short or surface tile is a floor, a
            // roof-flagged tile is a roof, anything else is a wall/object. Stairs are
            // surfaces, so they land in the floor slot and are not validated like walls.
            int SlotOf(ushort gfx)
            {
                ref StaticTiles td = ref Client.Game.UO.FileManager.TileData.StaticData[gfx];

                if (td.Height < 2 || (td.Flags & TileFlag.Surface) != 0)
                {
                    return 0;
                }

                return (td.Flags & TileFlag.Roof) != 0 ? 2 : 1;
            }

            // Does neighbour piece "other" brace "me" from direction (dx,dy)?
            bool AdjPair(ushort me, ushort other, int dx, int dy)
            {
                if (me == 0 || other == 0)
                {
                    return false;
                }

                CustomHousePlaceInfo m = GetPlaceInfo(me);
                CustomHousePlaceInfo o = GetPlaceInfo(other);

                if (m == null || o == null)
                {
                    return false;
                }

                if (dx < 0)
                {
                    return (m.AdjUW != 0 && o.AdjUE != 0) || (m.AdjLW != 0 && o.AdjLE != 0);
                }

                if (dx > 0)
                {
                    return (m.AdjUE != 0 && o.AdjUW != 0) || (m.AdjLE != 0 && o.AdjLW != 0);
                }

                if (dy < 0)
                {
                    return (m.AdjUN != 0 && o.AdjUS != 0) || (m.AdjLN != 0 && o.AdjLS != 0);
                }

                return (m.AdjUS != 0 && o.AdjUN != 0) || (m.AdjLS != 0 && o.AdjLN != 0);
            }

            // A wall on the plot edge is only allowed there if its can-go columns permit it.
            bool EdgeGuard(int x, int y, int l) =>
                (x != 0 || ((y != 0 || CanGoNWS(grid[0, 0, l].Object) != 0) && CanGoW(grid[0, y, l].Object) != 0))
                && (y != 0 || CanGoN(grid[x, 0, l].Object) != 0);

            // Fill the grid graphics from the placed components.
            foreach (Multi mm in house.Components)
            {
                if (!mm.IsCustom)
                {
                    continue;
                }

                // Skip the auto-generated floor-plan helpers: they are not real design
                // pieces and would drop phantom floors (and locks) under walls.
                if ((mm.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL) != 0)
                {
                    continue;
                }

                int gx = mm.X - ox;
                int gy = mm.Y - oy;

                if (!InGrid(gx, gy))
                {
                    continue;
                }

                int l = (mm.Z - baseZ) / 20;

                if (l < 0)
                {
                    l = 0;
                }

                if (l >= levels)
                {
                    continue;
                }

                ushort gr = mm.Graphic;

                switch (SlotOf(gr))
                {
                    case 0:
                        grid[gx, gy, l].Floor = gr;

                        // Certain fixed floor tiles are locked: they are not re-validated
                        // and they block wall validation on their cell.
                        if (gr >= 0x181D && gr < 0x1829)
                        {
                            grid[gx, gy, l].Locked = true;
                        }

                        break;

                    case 2:
                        grid[gx, gy, l].Roof = gr;
                        break;

                    default:
                        grid[gx, gy, l].Object = gr;
                        break;
                }
            }

            int[] dx = { -1, 1, 0, 0 };
            int[] dy = { 0, 0, -1, 1 };

            // Run-length support scan along (dx,dy): walk while the slot piece is present;
            // on reaching a cell already supported, mark every cell along the run supported.
            bool Run(int x, int y, int l, int sx, int sy, bool roof)
            {
                for (int step = 1; step < 0x13; step++)
                {
                    int cx = x + sx * step;
                    int cy = y + sy * step;

                    if (!InGrid(cx, cy))
                    {
                        return false;
                    }

                    ref DesignCell c = ref grid[cx, cy, l];

                    if ((roof ? c.Roof : c.Floor) == 0)
                    {
                        return false;
                    }

                    if (c.Support || (roof ? c.RoofSupport : c.FloorSupport))
                    {
                        for (int k = 1; k <= step; k++)
                        {
                            ref DesignCell run = ref grid[x + sx * k, y + sy * k, l];

                            if (roof)
                            {
                                run.RoofSupport = true;
                            }
                            else
                            {
                                run.FloorSupport = true;
                            }
                        }

                        return true;
                    }
                }

                return false;
            }

            bool FloorRun(int x, int y, int l) =>
                Run(x, y, l, 0, 1, false) | Run(x, y, l, 1, 0, false) | Run(x, y, l, -1, 0, false) | Run(x, y, l, 0, -1, false);

            bool RoofRun(int x, int y, int l) =>
                Run(x, y, l, 0, 1, true) | Run(x, y, l, 1, 0, true) | Run(x, y, l, -1, 0, true) | Run(x, y, l, 0, -1, true);

            // Spread support from a supported piece below into this level's neighbourhood.
            void Spread(int x, int y, int l)
            {
                grid[x, y, l].Support = true;

                ushort below = l >= 1 ? grid[x, y, l - 1].Object : (ushort)0;
                bool handled = false;

                if (InGrid(x + 1, y) && CanGoW(below) != 0)
                {
                    grid[x + 1, y, l].FloorSupport = true;
                    grid[x + 1, y, l].RoofSupport = true;
                    handled = true;
                }

                if (!handled && InGrid(x, y + 1) && CanGoN(below) != 0)
                {
                    grid[x, y + 1, l].FloorSupport = true;
                    grid[x, y + 1, l].RoofSupport = true;
                }

                if (grid[x, y, l].Floor != 0)
                {
                    if (InGrid(x + 1, y)) grid[x + 1, y, l].FloorSupport = true;
                    if (InGrid(x - 1, y)) grid[x - 1, y, l].FloorSupport = true;
                    if (InGrid(x, y + 1)) grid[x, y + 1, l].FloorSupport = true;
                    if (InGrid(x, y - 1)) grid[x, y - 1, l].FloorSupport = true;
                }

                if (grid[x, y, l].Roof != 0)
                {
                    for (int nx = x - 1; nx <= x + 1; nx++)
                    {
                        for (int ny = y - 1; ny <= y + 1; ny++)
                        {
                            if ((nx != x || ny != y) && InGrid(nx, ny))
                            {
                                grid[nx, ny, l].RoofSupport = true;
                            }
                        }
                    }
                }
            }

            for (int l = 0; l < levels; l++)
            {
                // Reset working state; ground cells start braced by the foundation.
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        ref DesignCell c = ref grid[x, y, l];
                        c.Visited = false;
                        c.Support = false;
                        c.FloorSupport = false;
                        c.RoofSupport = false;
                        c.FloorLegal = false;
                        c.RoofLegal = false;
                        c.ObjectLegal = 0;

                        if (l == 0)
                        {
                            c.Support = true;
                            c.FloorSupport = true;
                            c.RoofSupport = true;
                        }
                    }
                }

                // Push support up from a legal supporting wall on the floor below.
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        if (l == 0)
                        {
                            ref DesignCell c = ref grid[x, y, l];
                            c.Visited = true;
                            c.Support = true;
                            c.FloorSupport = true;
                            c.RoofSupport = true;
                        }
                        else
                        {
                            ref DesignCell below = ref grid[x, y, l - 1];

                            if (DirectSup(below.Object) != 0 && below.ObjectLegal != 0)
                            {
                                Spread(x, y, l);
                            }
                        }
                    }
                }

                // Flow floor/roof support along runs until nothing new is reached.
                bool changed;

                do
                {
                    changed = false;

                    for (int x = 0; x < w; x++)
                    {
                        for (int y = 0; y < h; y++)
                        {
                            ref DesignCell c = ref grid[x, y, l];

                            if (l == 0)
                            {
                                c.Visited = true;
                                c.Support = true;
                                c.FloorSupport = true;
                                c.RoofSupport = true;
                            }
                            else if (!c.Visited)
                            {
                                if ((!c.FloorSupport && !c.Support) || c.Floor == 0)
                                {
                                    if (c.RoofSupport && c.Roof != 0)
                                    {
                                        changed |= RoofRun(x, y, l);
                                        c.Visited = true;
                                    }
                                }
                                else
                                {
                                    changed |= FloorRun(x, y, l);

                                    if (c.Roof != 0)
                                    {
                                        changed |= RoofRun(x, y, l);
                                    }

                                    c.Visited = true;
                                }
                            }
                        }
                    }
                }
                while (changed);

                // Floor legal: a floor (not on the plot edge) backed by support.
                for (int x = 1; x < w; x++)
                {
                    for (int y = 1; y < h; y++)
                    {
                        ref DesignCell c = ref grid[x, y, l];

                        if (c.Floor == 0)
                        {
                            continue;
                        }

                        if (l == levels - 1)
                        {
                            ref StaticTiles td = ref Client.Game.UO.FileManager.TileData.StaticData[c.Floor];

                            if (td.Height >= 2 && (td.Flags & TileFlag.Surface) != 0)
                            {
                                continue;
                            }
                        }

                        if (l == 0 || c.Support || c.FloorSupport)
                        {
                            c.FloorLegal = true;
                        }
                    }
                }

                // Roof legal.
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        ref DesignCell c = ref grid[x, y, l];

                        if (c.Roof != 0 && (l == 0 || c.Support || c.RoofSupport || c.FloorLegal))
                        {
                            c.RoofLegal = true;
                        }
                    }
                }

                // Object/wall legal: braced by support, an underlying floor, or a can-go
                // neighbour floor.
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        if (!EdgeGuard(x, y, l))
                        {
                            continue;
                        }

                        ref DesignCell c = ref grid[x, y, l];

                        if (Bottom(c.Object) == 0 || c.Locked)
                        {
                            continue;
                        }

                        bool ok = c.Support || c.FloorLegal
                            || (CanGoN(c.Object) != 0 && InGrid(x, y + 1) && grid[x, y + 1, l].FloorLegal)
                            || (CanGoW(c.Object) != 0 && InGrid(x + 1, y) && grid[x + 1, y, l].FloorLegal)
                            || (CanGoNWS(c.Object) != 0 && InGrid(x + 1, y + 1) && grid[x + 1, y + 1, l].FloorLegal);

                        if (ok)
                        {
                            c.ObjectLegal = 1;
                        }
                    }
                }

                // Object/wall legal by adjacency to an already-legal lower-rank neighbour.
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        if (!EdgeGuard(x, y, l))
                        {
                            continue;
                        }

                        ref DesignCell c = ref grid[x, y, l];

                        if (c.Object == 0 || c.ObjectLegal != 0 || c.Locked)
                        {
                            continue;
                        }

                        const byte rank = 2;

                        for (int d = 0; d < 4; d++)
                        {
                            int nx = x + dx[d];
                            int ny = y + dy[d];

                            if (!InGrid(nx, ny))
                            {
                                continue;
                            }

                            DesignCell n = grid[nx, ny, l];

                            if (n.ObjectLegal != 0 && n.ObjectLegal < rank && AdjPair(c.Object, n.Object, dx[d], dy[d]))
                            {
                                c.ObjectLegal = rank;
                                break;
                            }
                        }
                    }
                }
            }

            // Write the settled legality back onto each piece.
            foreach (Multi mm in house.Components)
            {
                if (!mm.IsCustom)
                {
                    continue;
                }

                mm.State &= ~(CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE);
                mm.State |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE;

                if ((mm.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL) != 0)
                {
                    continue;
                }

                int gx = mm.X - ox;
                int gy = mm.Y - oy;

                if (!InGrid(gx, gy))
                {
                    continue;
                }

                int l = (mm.Z - baseZ) / 20;

                if (l < 0)
                {
                    l = 0;
                }

                if (l >= levels)
                {
                    continue;
                }

                ref DesignCell cell = ref grid[gx, gy, l];

                // Locked cells are fixed foundation tiles; their pieces are never flagged.
                if (cell.Locked)
                {
                    continue;
                }

                bool legal = SlotOf(mm.Graphic) switch
                {
                    0 => cell.FloorLegal,
                    2 => cell.RoofLegal,
                    _ => cell.ObjectLegal != 0,
                };

                if (!legal)
                {
                    mm.State |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE;
                }
            }
        }


        // Commit-time legality gate: refuse to send a design that contains any piece
        // which cannot be legally placed. Mirrors the editor's per-piece check.
        // Returns true when the design may be sent to the server.
        public bool ValidateHouseForCommit()
        {
            Item foundationItem = _world.Items.Get(Serial);

            if (foundationItem == null || !_world.HouseManager.TryGetHouse(Serial, out House house))
            {
                return false;
            }

            // Structural bounds: width/height in 1..32, floors in 1..4.
            int width = Math.Abs(EndPos.X - StartPos.X) + 1;
            int height = Math.Abs(EndPos.Y - StartPos.Y) + 1;

            if (width < 1 || width > 32 || height < 1 || height > 32 || FloorCount < 1 || FloorCount > 4)
            {
                return false;
            }

            // Refresh per-piece legality so the incorrect-place flags reflect the
            // current design before checking them.
            GenerateFloorPlace();

            // Every placed piece must be legal; a single bad piece rejects the design.
            foreach (Multi item in house.Components)
            {
                if (!item.IsCustom)
                {
                    continue;
                }

                // Generated/internal helpers are not user pieces — skip.
                if ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL) != 0)
                {
                    continue;
                }

                if ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) != 0)
                {
                    return false;
                }
            }

            return true;
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
                    return (i, contains);
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
}
