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

        // Recompute the legality of every placed piece.
        //
        // The design is laid out on a fixed 32x32x4 grid of 24-byte cells (a flat byte
        // buffer). Each cell holds three graphic slots (floor, wall/object, roof), three
        // legal flags and four directional support markers. Legality is settled one
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

            // 32 * 0xC00 = 0x18000 cells, plus slack so neighbour writes never run off the end.
            byte[] g = new byte[0x1A000];

            int Cell(int x, int y, int l) => x * 0xC00 + y * 0x60 + l * 0x18;
            int FloorG(int c) => g[c + 8] | (g[c + 9] << 8);
            int ObjG(int c) => g[c + 0xC] | (g[c + 0xD] << 8);
            int RoofG(int c) => g[c + 0x10] | (g[c + 0x11] << 8);

            // Slot a graphic by its tile data: a short or surface tile is a floor (+8), a
            // roof-flagged tile is a roof (+0x10), anything else is a wall/object (+0xC).
            // Stairs are surfaces, so they land in the floor slot and are not validated
            // like walls.
            int SlotOf(ushort gfx)
            {
                ref StaticTiles td = ref Client.Game.UO.FileManager.TileData.StaticData[gfx];

                if (td.Height < 2 || (td.Flags & TileFlag.Surface) != 0)
                {
                    return 8;
                }

                if ((td.Flags & TileFlag.Roof) != 0)
                {
                    return 0x10;
                }

                return 0xC;
            }

            int LegalByteOff(int slot) => slot == 8 ? 4 : slot == 0x10 ? 5 : 6;

            int CanGoW(int gfx) { CustomHousePlaceInfo i = GetPlaceInfo((ushort)gfx); return i == null ? 0 : i.CanGoW; }
            int CanGoN(int gfx) { CustomHousePlaceInfo i = GetPlaceInfo((ushort)gfx); return i == null ? 0 : i.CanGoN; }
            int CanGoNWS(int gfx) { CustomHousePlaceInfo i = GetPlaceInfo((ushort)gfx); return i == null ? 0 : i.CanGoNWS; }
            int DirectSup(int gfx) { CustomHousePlaceInfo i = GetPlaceInfo((ushort)gfx); return i == null ? 0 : i.DirectSupports; }
            int Bottom(int gfx) { CustomHousePlaceInfo i = GetPlaceInfo((ushort)gfx); return i == null ? 0 : i.Bottom; }

            // Adjacency support: does neighbour piece "other" brace "me" from (dx,dy)?
            bool AdjPair(int me, int other, int dx, int dy)
            {
                if (me == 0 || other == 0)
                {
                    return false;
                }

                CustomHousePlaceInfo m = GetPlaceInfo((ushort)me);
                CustomHousePlaceInfo o = GetPlaceInfo((ushort)other);

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

                if (gx < 0 || gx >= w || gy < 0 || gy >= h)
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

                int c = Cell(gx, gy, l);
                ushort gr = mm.Graphic;
                int slot = SlotOf(gr);

                g[c + slot] = (byte)gr;
                g[c + slot + 1] = (byte)(gr >> 8);

                // Certain fixed floor tiles are locked: they are not re-validated and they
                // block wall validation on their cell.
                if (slot == 8 && gr >= 0x181D && gr < 0x1829)
                {
                    g[c + 0x15] = 1;
                }
            }

            // Run-length support scan along (dx,dy): walk while the slot graphic is present;
            // on reaching a cell already marked supported, back-fill support along the run.
            bool Run(int x, int y, int l, int dx, int dy, int gfxOff, int fillByte)
            {
                for (int step = 1; step < 0x13; step++)
                {
                    int X = x + dx * step;
                    int Y = y + dy * step;

                    if ((uint)X > 0x1f || (uint)Y > 0x1f || (uint)l > 4)
                    {
                        return false;
                    }

                    int c = Cell(X, Y, l);

                    if ((g[c + gfxOff] | (g[c + gfxOff + 1] << 8)) == 0)
                    {
                        return false;
                    }

                    if (g[c + 1] != 0 || g[c + fillByte] != 0)
                    {
                        for (int k = 1; k <= step; k++)
                        {
                            g[Cell(x + dx * k, y + dy * k, l) + fillByte] = 1;
                        }

                        return true;
                    }
                }

                return false;
            }

            bool FloorRun4(int x, int y, int l) =>
                Run(x, y, l, 0, 1, 8, 2) | Run(x, y, l, 1, 0, 8, 2) | Run(x, y, l, -1, 0, 8, 2) | Run(x, y, l, 0, -1, 8, 2);

            bool RoofRun4(int x, int y, int l) =>
                Run(x, y, l, 0, 1, 0x10, 3) | Run(x, y, l, 1, 0, 0x10, 3) | Run(x, y, l, -1, 0, 0x10, 3) | Run(x, y, l, 0, -1, 0x10, 3);

            // Spread support from a supported piece below into this level's neighbourhood.
            void Spread(int x, int y, int l)
            {
                int p = Cell(x, y, l);
                g[p + 1] = 1;

                int oBelow = l >= 1 ? ObjG(Cell(x, y, l - 1)) : 0;
                bool skip = false;

                if ((uint)x < 0x20)
                {
                    if ((uint)y < 0x20 && (uint)(x + 1) < 0x20 && (uint)l < 5 && CanGoW(oBelow) != 0)
                    {
                        g[p + 0xC02] = 1;
                        g[p + 0xC03] = 1;
                        skip = true;
                    }

                    if (!skip && (uint)y < 0x20 && (uint)(y + 1) < 0x20 && (uint)l < 5 && CanGoN(oBelow) != 0)
                    {
                        g[p + 0x62] = 1;
                        g[p + 0x63] = 1;
                    }
                }

                if (FloorG(p) != 0)
                {
                    if ((uint)(x + 1) < 0x20 && (uint)y < 0x20 && (uint)l < 5) g[p + 0xC02] = 1;
                    if ((uint)(x - 1) < 0x20 && (uint)y < 0x20 && (uint)l < 5) g[p - 0xBFE] = 1;
                    if ((uint)x < 0x20 && (uint)(y + 1) < 0x20 && (uint)l < 5) g[p + 0x62] = 1;
                    if ((uint)x < 0x20 && (uint)(y - 1) < 0x20 && (uint)l < 5) g[p - 0x5E] = 1;
                }

                if (RoofG(p) != 0)
                {
                    int xm = x - 1, ym = y - 1, yp = y + 1, xp = x + 1;

                    if ((uint)xm < 0x20)
                    {
                        if ((uint)ym < 0x20 && (uint)l < 5) g[p - 0xC5D] = 1;
                        if ((uint)y < 0x20 && (uint)l < 5) g[p - 0xBFD] = 1;
                    }

                    if ((uint)xm < 0x20 && (uint)yp < 0x20 && (uint)l < 5) g[p - 0xB9D] = 1;

                    if ((uint)x < 0x20)
                    {
                        if ((uint)ym < 0x20 && (uint)l < 5) g[p - 0x5D] = 1;
                        if ((uint)yp < 0x20 && (uint)l < 5) g[p + 0x63] = 1;
                    }

                    if ((uint)xp < 0x20)
                    {
                        if ((uint)ym < 0x20 && (uint)l < 5) g[p + 0xBA3] = 1;
                        if ((uint)y < 0x20 && (uint)l < 5) g[p + 0xC03] = 1;
                        if ((uint)yp < 0x20 && (uint)l < 5) g[p + 0xC63] = 1;
                    }
                }
            }

            for (int l = 0; l < levels; l++)
            {
                // Reset working/legal bytes; ground cells start supported.
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        int c = Cell(x, y, l);
                        g[c + 0] = 0; g[c + 1] = 0; g[c + 2] = 0; g[c + 3] = 0; g[c + 4] = 0; g[c + 5] = 0; g[c + 6] = 0;

                        if (l == 0)
                        {
                            g[c + 1] = 1; g[c + 2] = 1; g[c + 3] = 1;
                        }
                    }
                }

                // Push support up from a legal supporting wall on the floor below.
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        int c = Cell(x, y, l);

                        if (l == 0)
                        {
                            g[c + 0] = 1; g[c + 1] = 1; g[c + 2] = 1; g[c + 3] = 1;
                        }
                        else
                        {
                            int b = Cell(x, y, l - 1);

                            if (DirectSup(ObjG(b)) != 0 && g[b + 6] != 0)
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
                            int c = Cell(x, y, l);

                            if (l == 0)
                            {
                                g[c + 0] = 1; g[c + 1] = 1; g[c + 2] = 1; g[c + 3] = 1;
                            }
                            else if (g[c + 0] == 0)
                            {
                                if ((g[c + 2] == 0 && g[c + 1] == 0) || FloorG(c) == 0)
                                {
                                    if (g[c + 3] != 0 && RoofG(c) != 0)
                                    {
                                        changed |= RoofRun4(x, y, l);
                                        g[c + 0] = 1;
                                    }
                                }
                                else
                                {
                                    changed |= FloorRun4(x, y, l);

                                    if (RoofG(c) != 0)
                                    {
                                        changed |= RoofRun4(x, y, l);
                                    }

                                    g[c + 0] = 1;
                                }
                            }
                        }
                    }
                }
                while (changed);

                // Floor legal: a floor (not on the plot edge) backed by support.
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        int c = Cell(x, y, l);

                        if (g[c + 0x14] != 0 || FloorG(c) == 0 || x == 0 || y == 0)
                        {
                            continue;
                        }

                        if (l == levels - 1)
                        {
                            ushort fg = (ushort)FloorG(c);

                            if (Client.Game.UO.FileManager.TileData.StaticData[fg].Height >= 2 &&
                                (Client.Game.UO.FileManager.TileData.StaticData[fg].Flags & TileFlag.Surface) != 0)
                            {
                                continue;
                            }
                        }

                        if (l == 0 || g[c + 1] != 0 || g[c + 2] != 0)
                        {
                            g[c + 4] = 1;
                        }
                    }
                }

                // Roof legal.
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        int c = Cell(x, y, l);

                        if (RoofG(c) == 0)
                        {
                            continue;
                        }

                        if (l == 0 || g[c + 1] != 0 || g[c + 3] != 0 || g[c + 4] != 0)
                        {
                            g[c + 5] = 1;
                        }
                    }
                }

                // Object/wall legal: braced by support, an underlying floor, or a can-go
                // neighbour floor.
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        bool guard = x != 0 ||
                            ((y != 0 || CanGoNWS(ObjG(Cell(0, 0, l))) != 0) && CanGoW(ObjG(Cell(0, y, l))) != 0);
                        guard = guard && (y != 0 || CanGoN(ObjG(Cell(x, 0, l))) != 0);

                        if (!guard)
                        {
                            continue;
                        }

                        int c = Cell(x, y, l);

                        if (Bottom(ObjG(c)) == 0 || g[c + 0x15] != 0)
                        {
                            continue;
                        }

                        bool ok = g[c + 1] != 0 || g[c + 4] != 0
                            || (CanGoN(ObjG(c)) != 0 && (uint)(y + 1) < 0x20 && (uint)l < 5 && g[c + 0x64] != 0)
                            || (CanGoW(ObjG(c)) != 0 && (uint)(x + 1) < 0x20 && (uint)l < 5 && g[c + 0xC04] != 0)
                            || (CanGoNWS(ObjG(c)) != 0 && (uint)(x + 1) < 0x20 && (uint)(y + 1) < 0x20 && (uint)l < 5 && g[c + 0xC64] != 0);

                        if (ok)
                        {
                            g[c + 6] = 1;
                        }
                    }
                }

                // Object/wall legal by adjacency to an already-legal lower-rank neighbour.
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        bool guard = x != 0 ||
                            ((y != 0 || CanGoNWS(ObjG(Cell(0, 0, l))) != 0) && CanGoW(ObjG(Cell(0, y, l))) != 0);
                        guard = guard && (y != 0 || CanGoN(ObjG(Cell(x, 0, l))) != 0);

                        if (!guard)
                        {
                            continue;
                        }

                        int c = Cell(x, y, l);
                        int me = ObjG(c);

                        if (me == 0 || g[c + 6] != 0 || g[c + 0x15] != 0)
                        {
                            continue;
                        }

                        const int rank = 2;
                        bool supported = false;
                        int[] ndx = { -1, 0, 0, 1 };
                        int[] ndy = { 0, -1, 1, 0 };

                        for (int d = 0; d < 4 && !supported; d++)
                        {
                            int nx = x + ndx[d];
                            int ny = y + ndy[d];

                            if ((uint)nx > 0x1f || (uint)ny > 0x1f)
                            {
                                continue;
                            }

                            int nc = Cell(nx, ny, l);

                            if (g[nc + 6] != 0 && g[nc + 6] < rank && AdjPair(me, ObjG(nc), ndx[d], ndy[d]))
                            {
                                supported = true;
                            }
                        }

                        if (supported)
                        {
                            g[c + 6] = rank;
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

                if (gx < 0 || gx >= w || gy < 0 || gy >= h)
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

                int c = Cell(gx, gy, l);

                // Locked cells are fixed foundation tiles; their pieces are never flagged.
                if (g[c + 0x15] != 0)
                {
                    continue;
                }

                int legalByte = c + LegalByteOff(SlotOf(mm.Graphic));

                if (g[legalByte] == 0)
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
