using System;
using System.Collections.Generic;
using ClassicUO.AssetsLoader;
using ClassicUO.Game.Map;
using ClassicUO.Game.WorldObjects;

namespace ClassicUO.Game
{
    //public enum PATH_STEP_STATE
    //{
    //    PSS_NORMAL = 0,
    //    PSS_DEAD_OR_GM,
    //    PSS_ON_SEA_HORSE,
    //    PSS_FLYING
    //}

    //public enum PATH_OBJECT_FLAGS
    //{
    //    POF_IMPASSABLE_OR_SURFACE = 0x00000001,
    //    POF_SURFACE = 0x00000002,
    //    POF_BRIDGE = 0x00000004,
    //    POF_NO_DIAGONAL = 0x00000008
    //}

    public static class Pathfinder
    {
        private const long IMPASSABLE_SURFACE = 0x00000040 | 0x00000200;
        private const int PERSON_HEIGHT = 16;
        private const int STEP_HEIGHT = 2;

        private static readonly List<Item>[] _poolsItems =
            {new List<Item>(), new List<Item>(), new List<Item>(), new List<Item>()};

        private static readonly List<Static>[] _poolsStatics =
            {new List<Static>(), new List<Static>(), new List<Static>(), new List<Static>()};

        private static readonly List<Tile> _tiles = new List<Tile>();

        public static bool CanWalk(in Mobile m, ref int newX, ref int newY, ref sbyte newZ, ref Direction newDir)
        {
            var (tileX, tileY) = OffsetTile(m.Position, newDir);

            return GetNextTile(m, m.Position, tileX, tileY, out newDir, out newX, out newY, out newZ);
        }

        public static bool GetNextTile(in Mobile m, in Position current, in int goalX, in int goalY,
            out Direction direction, out int nextX, out int nextY, out sbyte nextZ)
        {
            direction = GetNextDirection(current, goalX, goalY);
            var initialDir = direction;

            (nextX, nextY) = OffsetTile(current, direction);
            var moveIsOK = CheckMovement(m, current, direction, out nextZ);

            if (!moveIsOK)
            {
                direction = (Direction) (((int) direction - 1) & 0x87);
                (nextX, nextY) = OffsetTile(current, direction);
                moveIsOK = CheckMovement(m, current, direction, out nextZ);
            }

            if (!moveIsOK)
            {
                direction = (Direction) (((int) direction + 2) & 0x87);
                (nextX, nextY) = OffsetTile(current, direction);
                moveIsOK = CheckMovement(m, current, direction, out nextZ);
            }

            if (moveIsOK) return true;
            return false;
        }

        private static Direction GetNextDirection(in Position current, in int goalX, in int goalY)
        {
            Direction direction;

            if (goalX < current.X)
            {
                if (goalY < current.Y)
                    direction = Direction.Up;
                else if (goalY > current.Y)
                    direction = Direction.Left;
                else
                    direction = Direction.West;
            }
            else if (goalX > current.X)
            {
                if (goalY < current.Y)
                    direction = Direction.Right;
                else if (goalY > current.Y)
                    direction = Direction.Down;
                else
                    direction = Direction.East;
            }
            else
            {
                if (goalY < current.Y)
                    direction = Direction.North;
                else if (goalY > current.Y)
                    direction = Direction.South;
                else
                    throw new Exception("Wrong direction");
            }

            return direction;
        }

        private static (int, int) OffsetTile(in Position position, Direction direction)
        {
            int nextX = position.X;
            int nextY = position.Y;

            switch (direction & Direction.Up)
            {
                case Direction.North:
                    nextY--;
                    break;
                case Direction.South:
                    nextY++;
                    break;
                case Direction.West:
                    nextX--;
                    break;
                case Direction.East:
                    nextX++;
                    break;
                case Direction.Right:
                    nextX++;
                    nextY--;
                    break;
                case Direction.Left:
                    nextX--;
                    nextY++;
                    break;
                case Direction.Down:
                    nextX++;
                    nextY++;
                    break;
                case Direction.Up:
                    nextX--;
                    nextY--;
                    break;
            }

            return (nextX, nextY);
        }


        private static int GetNextZ(in Mobile mobile, in Position loc, in Direction d)
        {
            if (CheckMovement(mobile, loc, d, out var newZ, true))
                return newZ;
            return loc.Z;
        }

        // servuo
        public static bool CheckMovement(in Mobile mobile, in Position loc, in Direction d, out sbyte newZ,
            bool forceOK = false)
        {
            var map = World.Map;

            if (map == null)
            {
                newZ = 0;
                return true;
            }


            int xStart = loc.X;
            int yStart = loc.Y;
            int xForward = xStart, yForward = yStart;
            int xRight = xStart, yRight = yStart;
            int xLeft = xStart, yLeft = yStart;

            var checkDiagonals = ((int) d & 0x1) == 0x1;

            OffsetXY(d, ref xForward, ref yForward);
            OffsetXY((Direction) (((int) d - 1) & 0x7), ref xLeft, ref yLeft);
            OffsetXY((Direction) (((int) d + 1) & 0x7), ref xRight, ref yRight);

            if (xForward < 0 || yForward < 0 || xForward >= AssetsLoader.Map.MapsDefaultSize[map.Index][0] ||
                yForward >= AssetsLoader.Map.MapsDefaultSize[map.Index][1])
            {
                newZ = 0;
                return false;
            }


            var itemsStart = _poolsItems[0];
            var itemsForward = _poolsItems[1];
            var itemsLeft = _poolsItems[2];
            var itemsRight = _poolsItems[3];

            var staticStart = _poolsStatics[0];
            var staticForward = _poolsStatics[1];
            var staticLeft = _poolsStatics[2];
            var staticRight = _poolsStatics[3];

            var reqFlags = IMPASSABLE_SURFACE;

            if (checkDiagonals)
            {
                var tileStart = map.GetTile(xStart, yStart);
                var tileForward = map.GetTile(xForward, yForward);
                var tileLeft = map.GetTile(xLeft, yLeft);
                var tileRight = map.GetTile(xRight, yRight);

                if (tileForward == null || tileStart == null || tileLeft == null || tileRight == null)
                {
                    newZ = loc.Z;
                    return false;
                }

                var tiles = _tiles;
                tiles.Add(tileStart);
                tiles.Add(tileForward);
                tiles.Add(tileLeft);
                tiles.Add(tileRight);

                for (var i = 0; i < tiles.Count; ++i)
                {
                    var tile = tiles[i];

                    for (var j = 0; j < tile.ObjectsOnTiles.Count; ++j)
                    {
                        var entity = tile.ObjectsOnTiles[j];

                        // if (ignoreMovableImpassables && item.Movable && item.ItemData.Impassable)
                        //     continue;

                        if (entity is Item item)
                        {
                            if (((long) item.ItemData.Flags & reqFlags) == 0)
                                continue;

                            if (tile == tileStart && item.Position.X == xStart && item.Position.Y == yStart &&
                                item.Graphic < 0x4000)
                                itemsStart.Add(item);
                            else if (tile == tileForward && item.Position.X == xForward &&
                                     item.Position.Y == yForward && item.Graphic < 0x4000)
                                itemsForward.Add(item);
                            else if (tile == tileLeft && item.Position.X == xLeft && item.Position.Y == yLeft &&
                                     item.Graphic < 0x4000)
                                itemsLeft.Add(item);
                            else if (tile == tileRight && item.Position.X == xRight && item.Position.Y == yRight &&
                                     item.Graphic < 0x4000)
                                itemsRight.Add(item);
                        }
                        else if (entity is Static stat)
                        {
                            if (((long) stat.ItemData.Flags & reqFlags) == 0)
                                continue;

                            if (tile == tileStart && stat.Position.X == xStart && stat.Position.Y == yStart &&
                                stat.Graphic < 0x4000)
                                staticStart.Add(stat);
                            else if (tile == tileForward && stat.Position.X == xForward &&
                                     stat.Position.Y == yForward && stat.Graphic < 0x4000)
                                staticForward.Add(stat);
                            else if (tile == tileLeft && stat.Position.X == xLeft && stat.Position.Y == yLeft &&
                                     stat.Graphic < 0x4000)
                                staticLeft.Add(stat);
                            else if (tile == tileRight && stat.Position.X == xRight && stat.Position.Y == yRight &&
                                     stat.Graphic < 0x4000)
                                staticRight.Add(stat);
                        }
                    }
                }
            }
            else
            {
                var tileStart = map.GetTile(xStart, yStart);
                var tileForward = map.GetTile(xForward, yForward);
                if (tileForward == null || tileStart == null)
                {
                    newZ = loc.Z;
                    return false;
                }

                if (tileStart == tileForward)
                {
                    for (var i = 0; i < tileStart.ObjectsOnTiles.Count; i++)
                    {
                        var entity = tileStart.ObjectsOnTiles[i];

                        // if (ignoreMovableImpassables && item.Movable && item.ItemData.Impassable)
                        //     continue;

                        if (entity is Item item)
                        {
                            if (((long) item.ItemData.Flags & reqFlags) == 0)
                                continue;

                            if (item.Position.X == xStart && item.Position.Y == yStart && item.Graphic < 0x4000)
                                itemsStart.Add(item);
                            else if (item.Position.X == xForward && item.Position.Y == yForward &&
                                     item.Graphic < 0x4000)
                                itemsForward.Add(item);
                        }
                        else if (entity is Static stat)
                        {
                            if (((long) stat.ItemData.Flags & reqFlags) == 0)
                                continue;

                            if (stat.Position.X == xStart && stat.Position.Y == yStart && stat.Graphic < 0x4000)
                                staticStart.Add(stat);
                            else if (stat.Position.X == xForward && stat.Position.Y == yForward &&
                                     stat.Graphic < 0x4000)
                                staticForward.Add(stat);
                        }
                    }
                }
                else
                {
                    for (var i = 0; i < tileForward.ObjectsOnTiles.Count; i++)
                    {
                        var entity = tileForward.ObjectsOnTiles[i];

                        // if (ignoreMovableImpassables && item.Movable && item.ItemData.Impassable)
                        //     continue;

                        if (entity is Item item)
                        {
                            if (((long) item.ItemData.Flags & reqFlags) == 0)
                                continue;

                            if (item.Position.X == xForward && item.Position.Y == yForward && item.Graphic < 0x4000)
                                itemsForward.Add(item);
                        }
                        else if (entity is Static stat)
                        {
                            if (((long) stat.ItemData.Flags & reqFlags) == 0)
                                continue;

                            if (stat.Position.X == xForward && stat.Position.Y == yForward && stat.Graphic < 0x4000)
                                staticForward.Add(stat);
                        }
                    }

                    for (var i = 0; i < tileStart.ObjectsOnTiles.Count; i++)
                    {
                        var entity = tileStart.ObjectsOnTiles[i];

                        // if (ignoreMovableImpassables && item.Movable && item.ItemData.Impassable)
                        //     continue;

                        if (entity is Item item)
                        {
                            if (((long) item.ItemData.Flags & reqFlags) == 0)
                                continue;

                            if (item.Position.X == xStart && item.Position.Y == yStart && item.Graphic < 0x4000)
                                itemsStart.Add(item);
                        }
                        else if (entity is Static stat)
                        {
                            if (((long) stat.ItemData.Flags & reqFlags) == 0)
                                continue;

                            if (stat.Position.X == xStart && stat.Position.Y == yStart && stat.Graphic < 0x4000)
                                staticStart.Add(stat);
                        }
                    }
                }
            }

            GetStartZ(mobile, loc, itemsStart, staticStart, out var startZ, out var startTop);

            var moveIsOk = Check(mobile, itemsForward, staticForward, xForward, yForward, startTop, startZ, out newZ) ||
                           forceOK;

            if (moveIsOk && checkDiagonals)
                if (!Check(mobile, itemsLeft, staticLeft, xLeft, yLeft, startTop, startZ, out var hold) &&
                    !Check(mobile, itemsRight, staticRight, xRight, yRight, startTop, startZ, out hold))
                    moveIsOk = false;

            for (var i = 0; i < (checkDiagonals ? 4 : 2); i++)
            {
                if (_poolsItems[i].Count > 0)
                    _poolsItems[i].Clear();
                if (_poolsStatics[i].Count > 0)
                    _poolsStatics[i].Clear();
            }

            if (!moveIsOk)
                newZ = startZ;

            return moveIsOk;
        }

        private static void OffsetXY(in Direction d, ref int x, ref int y)
        {
            switch (d & Direction.Up)
            {
                case Direction.North:
                    --y;
                    break;
                case Direction.South:
                    ++y;
                    break;
                case Direction.West:
                    --x;
                    break;
                case Direction.East:
                    ++x;
                    break;
                case Direction.Right:
                    ++x;
                    --y;
                    break;
                case Direction.Left:
                    --x;
                    ++y;
                    break;
                case Direction.Down:
                    ++x;
                    ++y;
                    break;
                case Direction.Up:
                    --x;
                    --y;
                    break;
            }
        }

        private static void GetStartZ(in WorldObject e, in Position loc, in List<Item> itemList,
            in List<Static> staticList, out sbyte zLow, out sbyte zTop)
        {
            int xCheck = loc.X, yCheck = loc.Y;

            var mapTile = World.Map.GetTile(xCheck, yCheck);
            if (mapTile == null)
            {
                zLow = sbyte.MinValue;
                zTop = sbyte.MinValue;
            }

            var landBlocks = TileData.IsImpassable((long) TileData.LandData[mapTile.Graphic].Flags);

            sbyte landLow = 0, landTop = 0;
            var landCenter = World.Map.GetAverageZ((short) xCheck, (short) yCheck, ref landLow, ref landTop);

            var considerLand = !mapTile.IsIgnored;

            var zCenter = 0;
            zLow = 0;
            zTop = 0;
            var isSet = false;

            if (considerLand && !landBlocks && loc.Z >= landCenter)
            {
                zLow = landLow;
                zCenter = landCenter;

                if (!isSet || landTop > zTop)
                    zTop = landTop;

                isSet = true;
            }

            var staticTiles = mapTile.GetWorldObjects<Static>();

            for (var i = 0; i < staticTiles.Length; i++)
            {
                var tile = staticTiles[i];
                var id = tile.ItemData;

                var calcTop = tile.Position.Z + ((id.Flags & 0x00000400) != 0 ? id.Height / 2 : id.Height);

                if ((!isSet || calcTop >= zCenter) && (id.Flags & 0x00000200) != 0 && loc.Z >= calcTop)
                {
                    //  || (m.CanSwim && (id.Flags & TileFlag.Wet) != 0)
                    // if (m.CantWalk && (id.Flags & TileFlag.Wet) == 0)
                    //     continue;

                    zLow = tile.Position.Z;
                    zCenter = calcTop;

                    var top = (sbyte) (tile.Position.Z + id.Height);

                    if (!isSet || top > zTop)
                        zTop = top;

                    isSet = true;
                }
            }

            for (var i = 0; i < itemList.Count; i++)
            {
                var item = itemList[i];
                var id = item.ItemData;

                var calcTop = item.Position.Z + ((id.Flags & 0x00000400) != 0 ? id.Height / 2 : id.Height);

                if ((!isSet || calcTop >= zCenter) && (id.Flags & 0x00000200) != 0 && loc.Z >= calcTop)
                {
                    //  || (m.CanSwim && (id.Flags & TileFlag.Wet) != 0)
                    // if (m.CantWalk && (id.Flags & TileFlag.Wet) == 0)
                    //     continue;

                    zLow = item.Position.Z;
                    zCenter = calcTop;

                    var top = (sbyte) (item.Position.Z + id.Height);

                    if (!isSet || top > zTop)
                        zTop = top;

                    isSet = true;
                }
            }

            for (var i = 0; i < staticList.Count; i++)
            {
                var item = staticList[i];
                var id = item.ItemData;

                var calcTop = item.Position.Z + ((id.Flags & 0x00000400) != 0 ? id.Height / 2 : id.Height);

                if ((!isSet || calcTop >= zCenter) && (id.Flags & 0x00000200) != 0 && loc.Z >= calcTop)
                {
                    //  || (m.CanSwim && (id.Flags & TileFlag.Wet) != 0)
                    // if (m.CantWalk && (id.Flags & TileFlag.Wet) == 0)
                    //     continue;

                    zLow = item.Position.Z;
                    zCenter = calcTop;

                    var top = (sbyte) (item.Position.Z + id.Height);

                    if (!isSet || top > zTop)
                        zTop = top;

                    isSet = true;
                }
            }

            if (!isSet)
                zLow = zTop = loc.Z;
            else if (loc.Z > zTop)
                zTop = loc.Z;
        }

        private static bool IsOK(in bool ignoreDoors, in int ourZ, in int ourTop, in Static[] tiles,
            in List<Item> items, in List<Static> statics)
        {
            for (var i = 0; i < tiles.Length; ++i)
            {
                var item = tiles[i];
                if ((item.ItemData.Flags & IMPASSABLE_SURFACE) != 0) // Impassable || Surface
                {
                    int checkZ = item.Position.Z;
                    var checkTop = checkZ + ((item.ItemData.Flags & 0x00000400) != 0
                                       ? item.ItemData.Height / 2
                                       : item.ItemData.Height);

                    if (checkTop > ourZ && ourTop > checkZ)
                        return false;
                }
            }

            for (var i = 0; i < items.Count; ++i)
            {
                var item = items[i];
                var itemID = item.Graphic & FileManager.GraphicMask;
                var itemData = TileData.StaticData[itemID];
                var flags = itemData.Flags;

                if ((flags & IMPASSABLE_SURFACE) != 0) // Impassable || Surface
                {
                    if (ignoreDoors && (TileData.IsDoor((long) flags) || itemID == 0x692 || itemID == 0x846 ||
                                        itemID == 0x873 || itemID >= 0x6F5 && itemID <= 0x6F6))
                        continue;

                    int checkZ = item.Position.Z;
                    var checkTop = checkZ + ((item.ItemData.Flags & 0x00000400) != 0
                                       ? item.ItemData.Height / 2
                                       : item.ItemData.Height);

                    if (checkTop > ourZ && ourTop > checkZ)
                        return false;
                }
            }

            for (var i = 0; i < statics.Count; ++i)
            {
                var item = statics[i];
                var itemID = item.Graphic & FileManager.GraphicMask;
                var itemData = TileData.StaticData[itemID];
                var flags = itemData.Flags;

                if ((flags & IMPASSABLE_SURFACE) != 0) // Impassable || Surface
                {
                    if (ignoreDoors && (TileData.IsDoor((long) flags) || itemID == 0x692 || itemID == 0x846 ||
                                        itemID == 0x873 || itemID >= 0x6F5 && itemID <= 0x6F6))
                        continue;

                    int checkZ = item.Position.Z;
                    var checkTop = checkZ + ((item.ItemData.Flags & 0x00000400) != 0
                                       ? item.ItemData.Height / 2
                                       : item.ItemData.Height);

                    if (checkTop > ourZ && ourTop > checkZ)
                        return false;
                }
            }

            return true;
        }

        private static bool Check(in Mobile m, in List<Item> items, in List<Static> statics, in int x, int y,
            in int startTop, in sbyte startZ, out sbyte newZ)
        {
            newZ = 0;

            var mapTile = World.Map.GetTile(x, y);
            if (mapTile == null)
                return false;

            var id = TileData.LandData[mapTile.Graphic];

            var tiles = mapTile.GetWorldObjects<Static>();
            var landBlocks = (id.Flags & 0x00000040) != 0;
            var considerLand = !mapTile.IsIgnored;

            sbyte landCenter = 0;
            sbyte landLow = 0, landTop = 0;
            landCenter = (sbyte) World.Map.GetAverageZ((short) x, (short) y, ref landLow, ref landTop);

            var moveIsOk = false;

            var stepTop = startTop + STEP_HEIGHT;
            var checkTop = startZ + PERSON_HEIGHT;

            var ignoreDoors = m.IsDead || m.Graphic == 0x3DB;


            for (var i = 0; i < tiles.Length; ++i)
            {
                var tile = tiles[i];

                if ((tile.ItemData.Flags & IMPASSABLE_SURFACE) == 0x00000200
                ) //  || (canSwim && (flags & TileFlag.Wet) != 0) Surface && !Impassable
                {
                    // if (cantWalk && (flags & TileFlag.Wet) == 0)
                    //     continue;

                    int itemZ = tile.Position.Z;
                    var itemTop = itemZ;
                    var ourZ = (sbyte) (itemZ + ((tile.ItemData.Flags & 0x00000400) != 0
                                            ? tile.ItemData.Height / 2
                                            : tile.ItemData.Height));
                    var ourTop = ourZ + PERSON_HEIGHT;
                    var testTop = checkTop;

                    if (moveIsOk)
                    {
                        var cmp = Math.Abs(ourZ - m.Position.Z) - Math.Abs(newZ - m.Position.Z);

                        if (cmp > 0 || cmp == 0 && ourZ > newZ)
                            continue;
                    }

                    if (ourZ + PERSON_HEIGHT > testTop)
                        testTop = ourZ + PERSON_HEIGHT;

                    if ((tile.ItemData.Flags & 0x00000400) == 0)
                        itemTop += tile.ItemData.Height;

                    if (stepTop >= itemTop)
                    {
                        var landCheck = itemZ;

                        if (tile.ItemData.Height >= STEP_HEIGHT)
                            landCheck += STEP_HEIGHT;
                        else
                            landCheck += tile.ItemData.Height;

                        if (considerLand && landCheck < landCenter && landCenter > ourZ && testTop > landLow)
                            continue;

                        if (IsOK(ignoreDoors, ourZ, testTop, tiles, items, statics))
                        {
                            newZ = ourZ;
                            moveIsOk = true;
                        }
                    }
                }
            }

            for (var i = 0; i < items.Count; ++i)
            {
                var item = items[i];
                var itemData = item.ItemData;
                var flags = itemData.Flags;

                if ((flags & IMPASSABLE_SURFACE) == 0x00000200) // Surface && !Impassable && !Movable
                {
                    //  || (m.CanSwim && (flags & TileFlag.Wet) != 0))
                    // !item.Movable && 
                    // if (cantWalk && (flags & TileFlag.Wet) == 0)
                    //     continue;

                    int itemZ = item.Position.Z;
                    var itemTop = itemZ;
                    var ourZ = (sbyte) (itemZ + ((item.ItemData.Flags & 0x00000400) != 0
                                            ? item.ItemData.Height / 2
                                            : item.ItemData.Height));
                    var ourTop = ourZ + PERSON_HEIGHT;
                    var testTop = checkTop;

                    if (moveIsOk)
                    {
                        var cmp = Math.Abs(ourZ - m.Position.Z) - Math.Abs(newZ - m.Position.Z);

                        if (cmp > 0 || cmp == 0 && ourZ > newZ)
                            continue;
                    }

                    if (ourZ + PERSON_HEIGHT > testTop)
                        testTop = ourZ + PERSON_HEIGHT;

                    if ((itemData.Flags & 0x00000400) == 0)
                        itemTop += itemData.Height;

                    if (stepTop >= itemTop)
                    {
                        var landCheck = itemZ;

                        if (itemData.Height >= STEP_HEIGHT)
                            landCheck += STEP_HEIGHT;
                        else
                            landCheck += itemData.Height;

                        if (considerLand && landCheck < landCenter && landCenter > ourZ && testTop > landLow)
                            continue;

                        if (IsOK(ignoreDoors, ourZ, testTop, tiles, items, statics))
                        {
                            newZ = ourZ;
                            moveIsOk = true;
                        }
                    }
                }
            }


            if (considerLand && !landBlocks && stepTop >= landLow)
            {
                var ourZ = landCenter;
                var ourTop = ourZ + PERSON_HEIGHT;
                var testTop = checkTop;

                if (ourZ + PERSON_HEIGHT > testTop)
                    testTop = ourZ + PERSON_HEIGHT;

                var shouldCheck = true;

                if (moveIsOk)
                {
                    var cmp = Math.Abs(ourZ - m.Position.Z) - Math.Abs(newZ - m.Position.Z);

                    if (cmp > 0 || cmp == 0 && ourZ > newZ)
                        shouldCheck = false;
                }

                if (shouldCheck && IsOK(ignoreDoors, ourZ, testTop, tiles, items, statics))
                {
                    newZ = ourZ;
                    moveIsOk = true;
                }
            }


            return moveIsOk;
        }


        //private static readonly Direction[] _directions = new Direction[3]
        //{
        //    Direction.North, Direction.Right, Direction.NONE
        //};

        //public static bool CanWalk(ref Direction direction, ref Position pos)
        //{
        //    bool allowed = false;

        //    for(int i = 0; i < 3; i++)
        //    {
        //        int newX = pos.X;
        //        int newY = pos.Y;
        //        sbyte newZ = pos.Z;
        //        Direction newDirectiopn = (Direction)(((byte)direction + (byte)_directions[i]) % 9);

        //        GetNewXY(newDirectiopn, ref newX, ref newY);
        //        allowed = 
        //    }

        //    return allowed;
        //}


        //private static void GetNewXY(in Direction direction, ref int x, ref int y)
        //{
        //    switch (direction & Direction.Up)
        //    {
        //        case Direction.North:
        //            y--;
        //            break;
        //        case Direction.Right:
        //            x++;
        //            y--;
        //            break;
        //        case Direction.East:
        //            x++;
        //            break;
        //        case Direction.Down:
        //            x++;
        //            y++;
        //            break;
        //        case Direction.South:
        //            y++;
        //            break;
        //        case Direction.Left:
        //            x--;
        //            y++;
        //            break;
        //        case Direction.West:
        //            x--;
        //            break;
        //        case Direction.Up:
        //            x--;
        //            y--;
        //            break;
        //    }
        //}

        //private static bool CalculateNewZ(in int x, in int y, ref sbyte z, in Direction direction)
        //{
        //    PATH_STEP_STATE stepState = PATH_STEP_STATE.PSS_NORMAL;

        //    if (World.Player.IsDead || World.Player.Graphic == 0x3D8)
        //        stepState = PATH_STEP_STATE.PSS_DEAD_OR_GM;
        //    else
        //    {
        //        if (World.Player.IsFlying)
        //            stepState = PATH_STEP_STATE.PSS_FLYING;
        //        else
        //        {
        //            Item mount = World.Player.GetItemAtLayer(Layer.Mount);
        //            if (mount != null && mount.Graphic == 0x3EB3)
        //                stepState = PATH_STEP_STATE.PSS_ON_SEA_HORSE;
        //        }
        //    }

        //    int minZ = -128;
        //    int maxZ = z;


        //}

        //private static readonly int[] _offsetX = new int[10] { 0, 1, 1, 1, 0, -1, -1, -1, 0, 1 };
        //private static readonly int[] _offsetY = new int[10] { -1, -1, 0, 1, 1, 1, 0, -1, -1, -1 };

        //private static int CalculateMinMaxZ(ref int minZ, ref int maxZ, int newX, int newY, in int currentZ, int newDirection, in int stepState)
        //{
        //    minZ = -128;
        //    maxZ = currentZ;

        //    newDirection &= 7;
        //    int direction = (newDirection ^ 4);

        //    newX += _offsetX[direction];
        //    newY += _offsetY[direction];


        //    List<PathObject> list = new List<PathObject>();

        //    if (CreateItemsList(list, newX, newY, (int)stepState) || list.Count <= 0)
        //        return 0;

        //    foreach (PathObject obj in list)
        //    {
        //        var rwo = obj.Object;
        //        int averageZ = obj.AverageZ;

        //        if (averageZ <= currentZ && rwo is Tile tile && tile.ViewObject.IsStretched)
        //        {

        //        }
        //    }
        //}

        //private static int CalculateCurrentAverageZ (in int direction)
        //{

        //}


        //private static bool CreateItemsList(in List<PathObject> list, in int x, in int y, in  int stepState)
        //{
        //    //int blockX = x / 8;
        //    //int blockY = y / 8;
        //    //uint blockIndex = (uint)((blockX * AssetsLoader.Map.MapBlocksSize[World.Map.Index][1]) + blockY);

        //    //int bx = x % 8;
        //    //int by = y % 8;

        //    bool ignoreGameCharacters = stepState == (int)PATH_STEP_STATE.PSS_DEAD_OR_GM || (World.Player.Stamina >= World.Player.StaminaMax && World.Map.Index == 0 );
        //    bool isGM = World.Player.Graphic == 0x3D8;

        //    Tile tile = World.Map.GetTile(x, y);

        //    var objects = tile.ObjectsOnTiles;

        //    foreach (var obj in objects)
        //    {
        //        Graphic graphic = obj.Graphic;

        //        if (obj is Tile land)
        //        {
        //            if ((graphic < 0x01AE && graphic != 2) || (graphic > 0x01B5 && graphic != 0x01DB))
        //            {
        //                uint flags = (uint)PATH_OBJECT_FLAGS.POF_IMPASSABLE_OR_SURFACE;
        //                long tiledataFlags = (long)land.TileData.Flags;

        //                if (stepState == (int)PATH_STEP_STATE.PSS_ON_SEA_HORSE)
        //                {
        //                    if (AssetsLoader.TileData.IsWet(tiledataFlags))
        //                        flags = (uint)(PATH_OBJECT_FLAGS.POF_IMPASSABLE_OR_SURFACE | PATH_OBJECT_FLAGS.POF_SURFACE | PATH_OBJECT_FLAGS.POF_BRIDGE);
        //                }
        //                else
        //                {
        //                    if (AssetsLoader.TileData.IsImpassable(tiledataFlags))
        //                        flags = (uint)(PATH_OBJECT_FLAGS.POF_IMPASSABLE_OR_SURFACE | PATH_OBJECT_FLAGS.POF_SURFACE | PATH_OBJECT_FLAGS.POF_BRIDGE);

        //                    if (stepState == (int)PATH_STEP_STATE.PSS_FLYING && AssetsLoader.TileData.IsNoDiagonal(tiledataFlags))
        //                        flags |= (uint)PATH_OBJECT_FLAGS.POF_NO_DIAGONAL;
        //                }

        //                int landMinZ = land.Position.Z;
        //                int low = 0, top = 0;
        //                int landAverageZ = World.Map.GetAverageZ((short)land.Position.X, (short)land.Position.Y, ref low, ref top);
        //                int landHeight = landAverageZ - landMinZ;

        //                list.Add(new PathObject(flags, landMinZ, landAverageZ, landHeight, obj));
        //            }
        //        }
        //        else if (obj is Static stat)
        //        {
        //            bool canBeAdd = true;
        //            bool dropFlags = false;

        //            AssetsLoader.StaticTiles tileInfo = stat.ItemData;

        //            // check for npc

        //            if (stepState == (int)PATH_STEP_STATE.PSS_DEAD_OR_GM && ( AssetsLoader.TileData.IsDoor((long)tileInfo.Flags) || tileInfo.Weight <= 0x5A /*|| (isGM  )*/))
        //                dropFlags = true;
        //            else
        //                dropFlags = ((graphic >= 0x3946 && graphic <= 0x3964) || graphic == 0x0082);


        //            if (canBeAdd)
        //            {

        //            }

        //        }
        //    }

        //    return list.Count > 0;
        //}
    }

    public class PathObject
    {
        public PathObject(in uint flags, in int z, in int averageZ, in int height, in WorldObject obj)
        {
            Flags = flags;
            Z = z;
            AverageZ = averageZ;
            Height = height;
            Object = obj;
        }

        public uint Flags { get; }
        public int Z { get; }
        public int AverageZ { get; }
        public int Height { get; }
        public WorldObject Object { get; }
    }
}