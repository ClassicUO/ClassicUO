#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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
using System;
using System.Collections.Generic;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;
using ClassicUO.Interfaces;
using ClassicUO.IO;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game
{
    public static class Pathfinder
    {
        private const long IMPASSABLE_SURFACE = 0x00000040 | 0x00000200;
        private const int PERSON_HEIGHT = 16;
        private const int STEP_HEIGHT = 2;

        private static readonly List<IDynamicItem>[] _pool =
            {new List<IDynamicItem>(), new List<IDynamicItem>(), new List<IDynamicItem>(), new List<IDynamicItem>()};

        private static readonly List<Tile> _tiles = new List<Tile>();

        public static bool CanWalk(Mobile m, ref int newX, ref int newY, ref sbyte newZ, ref Direction newDir)
        {
            (int tileX, int tileY) = OffsetTile(m.Position, newDir);

            return GetNextTile(m, m.Position, tileX, tileY, out newDir, out newX, out newY, out newZ);
        }

        public static bool GetNextTile(Mobile m, Position current, int goalX, int goalY, out Direction direction,
            out int nextX, out int nextY, out sbyte nextZ)
        {
            direction = GetNextDirection(current, goalX, goalY);
            Direction initialDir = direction;

            (nextX, nextY) = OffsetTile(current, direction);
            bool moveIsOK = CheckMovement(m, current, direction, out nextZ);

            if (!moveIsOK)
            {
                direction = (Direction) (((byte) direction - 1) & 0x87);
                (nextX, nextY) = OffsetTile(current, direction);
                moveIsOK = CheckMovement(m, current, direction, out nextZ);
            }

            if (!moveIsOK)
            {
                direction = (Direction) (((byte) direction + 2) & 0x87);
                (nextX, nextY) = OffsetTile(current, direction);
                moveIsOK = CheckMovement(m, current, direction, out nextZ);
            }

            if (!moveIsOK)
                direction = initialDir;

            return moveIsOK;
        }

        private static Direction GetNextDirection(Position current, int goalX, int goalY)
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

        private static (int, int) OffsetTile(Position position, Direction direction)
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


        public static int GetNextZ(Mobile mobile, Position loc, Direction d)
        {
            if (CheckMovement(mobile, loc, d, out sbyte newZ, true)) return newZ;

            return loc.Z;
        }

        public static bool TryGetNextZ(Mobile mobile, Position loc, Direction d, out sbyte z) =>
            CheckMovement(mobile, loc, d, out z, true);

        // servuo
        public static bool CheckMovement(Mobile mobile, Position loc, Direction d, out sbyte newZ, bool forceOK = false)
        {
            Facet map = World.Map;

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

            bool checkDiagonals = ((int) d & 0x1) == 0x1;

            OffsetXY(d, ref xForward, ref yForward);
            OffsetXY((Direction) (((int) d - 1) & 0x7), ref xLeft, ref yLeft);
            OffsetXY((Direction) (((int) d + 1) & 0x7), ref xRight, ref yRight);

            if (xForward < 0 || yForward < 0 || xForward >= IO.Resources.Map.MapsDefaultSize[map.Index][0] ||
                yForward >= IO.Resources.Map.MapsDefaultSize[map.Index][1])
            {
                newZ = 0;
                return false;
            }


            List<IDynamicItem> itemsStart = _pool[0];
            List<IDynamicItem> itemsForward = _pool[1];
            List<IDynamicItem> itemsLeft = _pool[2];
            List<IDynamicItem> itemsRight = _pool[3];


            const long REQ_FLAGS = IMPASSABLE_SURFACE;

            if (checkDiagonals)
            {
                Tile tileStart = map.GetTile(xStart, yStart);
                Tile tileForward = map.GetTile(xForward, yForward);
                Tile tileLeft = map.GetTile(xLeft, yLeft);
                Tile tileRight = map.GetTile(xRight, yRight);

                if (tileForward == null || tileStart == null || tileLeft == null || tileRight == null)
                {
                    newZ = loc.Z;
                    return false;
                }

                List<Tile> tiles = _tiles;
                tiles.Add(tileStart);
                tiles.Add(tileForward);
                tiles.Add(tileLeft);
                tiles.Add(tileRight);

                for (int i = 0; i < tiles.Count; ++i)
                {
                    Tile tile = tiles[i];

                    for (int j = 0; j < tile.ObjectsOnTiles.Count; ++j)
                    {
                        GameObject entity = tile.ObjectsOnTiles[j];

                        // if (ignoreMovableImpassables && item.Movable && item.ItemData.Impassable)
                        //     continue;

                        if (entity is IDynamicItem item)
                        {
                            if (((long) item.ItemData.Flags & REQ_FLAGS) == 0) continue;

                            if (tile == tileStart && item.IsAtWorld(xStart, yStart) && item.Graphic < 0x4000)
                                itemsStart.Add(item);
                            else if (tile == tileForward && item.IsAtWorld(xForward, yForward) && item.Graphic < 0x4000)
                                itemsForward.Add(item);
                            else if (tile == tileLeft && item.IsAtWorld(xLeft, yLeft) && item.Graphic < 0x4000)
                                itemsLeft.Add(item);
                            else if (tile == tileRight && item.IsAtWorld(xRight, yRight) && item.Graphic < 0x4000)
                                itemsRight.Add(item);
                        }
                    }
                }

                if (_tiles.Count > 0) _tiles.Clear();
            }
            else
            {
                Tile tileStart = map.GetTile(xStart, yStart);
                Tile tileForward = map.GetTile(xForward, yForward);
                if (tileForward == null || tileStart == null)
                {
                    newZ = loc.Z;
                    return false;
                }

                if (tileStart == tileForward)
                {
                    for (int i = 0; i < tileStart.ObjectsOnTiles.Count; i++)
                    {
                        GameObject entity = tileStart.ObjectsOnTiles[i];

                        // if (ignoreMovableImpassables && item.Movable && item.ItemData.Impassable)
                        //     continue;

                        if (entity is IDynamicItem item)
                        {
                            if (((long) item.ItemData.Flags & REQ_FLAGS) == 0) continue;

                            if (item.IsAtWorld(xStart, yStart) && item.Graphic < 0x4000)
                                itemsStart.Add(item);
                            else if (item.IsAtWorld(xForward, yForward) && item.Graphic < 0x4000)
                                itemsForward.Add(item);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < tileForward.ObjectsOnTiles.Count; i++)
                    {
                        GameObject entity = tileForward.ObjectsOnTiles[i];

                        // if (ignoreMovableImpassables && item.Movable && item.ItemData.Impassable)
                        //     continue;

                        if (entity is IDynamicItem item)
                        {
                            if (((long) item.ItemData.Flags & REQ_FLAGS) == 0) continue;

                            if (item.IsAtWorld(xForward, yForward) && item.Graphic < 0x4000) itemsForward.Add(item);
                        }
                    }

                    for (int i = 0; i < tileStart.ObjectsOnTiles.Count; i++)
                    {
                        GameObject entity = tileStart.ObjectsOnTiles[i];

                        // if (ignoreMovableImpassables && item.Movable && item.ItemData.Impassable)
                        //     continue;

                        if (entity is IDynamicItem item)
                        {
                            if (((long) item.ItemData.Flags & REQ_FLAGS) == 0) continue;

                            if (item.IsAtWorld(xStart, yStart) && item.Graphic < 0x4000) itemsStart.Add(item);
                        }
                    }
                }
            }

            GetStartZ(mobile, loc, itemsStart, out sbyte startZ, out sbyte startTop);

            bool moveIsOk = Check(mobile, itemsForward, xForward, yForward, startTop, startZ, out newZ) || forceOK;

            if (moveIsOk && checkDiagonals)
            {
                if (!Check(mobile, itemsLeft, xLeft, yLeft, startTop, startZ, out sbyte hold) ||
                    !Check(mobile, itemsRight, xRight, yRight, startTop, startZ, out hold))
                    moveIsOk = false;
            }

            for (int i = 0; i < (checkDiagonals ? 4 : 2); i++)
            {
                if (_pool[i].Count > 0)
                    _pool[i].Clear();
            }

            if (!moveIsOk) newZ = startZ;

            return moveIsOk;
        }

        private static void OffsetXY(Direction d, ref int x, ref int y)
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

        private static void GetStartZ(GameObject e, Position loc, List<IDynamicItem> itemList, out sbyte zLow,
            out sbyte zTop)
        {
            int xCheck = loc.X, yCheck = loc.Y;

            Tile mapTile = World.Map.GetTile(xCheck, yCheck);
            if (mapTile == null)
            {
                zLow = sbyte.MinValue;
                zTop = sbyte.MinValue;
                return;
            }

            bool landBlocks = TileData.IsImpassable((long) mapTile.TileData.Flags);

            sbyte landLow = 0, landTop = 0;
            int landCenter = World.Map.GetAverageZ((short) xCheck, (short) yCheck, ref landLow, ref landTop);

            bool considerLand = !mapTile.IsIgnored;

            int zCenter = 0;
            zLow = 0;
            zTop = 0;
            bool isSet = false;

            if (considerLand && !landBlocks && loc.Z >= landCenter)
            {
                zLow = landLow;
                zCenter = landCenter;

                if (!isSet || landTop > zTop) zTop = landTop;

                isSet = true;
            }

            List<Static> staticTiles = mapTile.GetStatics();

            for (int i = 0; i < staticTiles.Count; i++)
            {
                Static tile = staticTiles[i];
                StaticTiles id = tile.ItemData;

                int calcTop = tile.Position.Z + ((id.Flags & 0x00000400) != 0 ? id.Height / 2 : id.Height);

                if ((!isSet || calcTop >= zCenter) && (id.Flags & 0x00000200) != 0 && loc.Z >= calcTop)
                {
                    //  || (m.CanSwim && (id.Flags & TileFlag.Wet) != 0)
                    // if (m.CantWalk && (id.Flags & TileFlag.Wet) == 0)
                    //     continue;

                    zLow = tile.Position.Z;
                    zCenter = calcTop;

                    sbyte top = (sbyte) (tile.Position.Z + id.Height);

                    if (!isSet || top > zTop) zTop = top;

                    isSet = true;
                }
            }

            for (int i = 0; i < itemList.Count; i++)
            {
                IDynamicItem item = itemList[i];
                StaticTiles id = item.ItemData;

                int calcTop = item.Position.Z + ((id.Flags & 0x00000400) != 0 ? id.Height / 2 : id.Height);

                if ((!isSet || calcTop >= zCenter) && (id.Flags & 0x00000200) != 0 && loc.Z >= calcTop)
                {
                    //  || (m.CanSwim && (id.Flags & TileFlag.Wet) != 0)
                    // if (m.CantWalk && (id.Flags & TileFlag.Wet) == 0)
                    //     continue;

                    zLow = item.Position.Z;
                    zCenter = calcTop;

                    sbyte top = (sbyte) (item.Position.Z + id.Height);

                    if (!isSet || top > zTop) zTop = top;

                    isSet = true;
                }
            }

            if (!isSet)
                zLow = zTop = loc.Z;
            else if (loc.Z > zTop) zTop = loc.Z;
        }

        private static bool IsOK(bool ignoreDoors, int ourZ, int ourTop, List<Static> tiles, List<IDynamicItem> items)
        {
            for (int i = 0; i < tiles.Count; ++i)
            {
                Static item = tiles[i];
                if ((item.ItemData.Flags & IMPASSABLE_SURFACE) != 0) // Impassable || Surface
                {
                    int checkZ = item.Position.Z;
                    int checkTop = checkZ + ((item.ItemData.Flags & 0x00000400) != 0
                                       ? item.ItemData.Height / 2
                                       : item.ItemData.Height);

                    if (checkTop > ourZ && ourTop > checkZ) return false;
                }
            }

            for (int i = 0; i < items.Count; ++i)
            {
                IDynamicItem item = items[i];
                int itemID = item.Graphic & FileManager.GraphicMask;
                StaticTiles itemData = TileData.StaticData[itemID];
                ulong flags = itemData.Flags;

                if ((flags & IMPASSABLE_SURFACE) != 0) // Impassable || Surface
                {
                    if (ignoreDoors && (TileData.IsDoor((long) flags) || itemID == 0x692 || itemID == 0x846 ||
                                        itemID == 0x873 || itemID >= 0x6F5 && itemID <= 0x6F6)) continue;

                    int checkZ = item.Position.Z;
                    int checkTop = checkZ + ((item.ItemData.Flags & 0x00000400) != 0
                                       ? item.ItemData.Height / 2
                                       : item.ItemData.Height);

                    if (checkTop > ourZ && ourTop > checkZ) return false;
                }
            }

            return true;
        }

        private static bool Check(Mobile m, List<IDynamicItem> items, int x, int y, int startTop, sbyte startZ,
            out sbyte newZ)
        {
            newZ = 0;

            Tile mapTile = World.Map.GetTile(x, y);
            if (mapTile == null) return false;

            LandTiles id = mapTile.TileData;

            List<Static> tiles = mapTile.GetStatics();
            bool landBlocks = (id.Flags & 0x00000040) != 0;
            bool considerLand = !mapTile.IsIgnored;

            sbyte landCenter = 0;
            sbyte landLow = 0, landTop = 0;
            landCenter = (sbyte) World.Map.GetAverageZ((short) x, (short) y, ref landLow, ref landTop);

            bool moveIsOk = false;

            int stepTop = startTop + STEP_HEIGHT;
            int checkTop = startZ + PERSON_HEIGHT;

            bool ignoreDoors = m.IsDead || m.Graphic == 0x3DB;


            for (int i = 0; i < tiles.Count; ++i)
            {
                Static tile = tiles[i];

                if ((tile.ItemData.Flags & IMPASSABLE_SURFACE) == 0x00000200
                ) //  || (canSwim && (flags & TileFlag.Wet) != 0) Surface && !Impassable
                {
                    // if (cantWalk && (flags & TileFlag.Wet) == 0)
                    //     continue;

                    int itemZ = tile.Position.Z;
                    int itemTop = itemZ;
                    sbyte ourZ = (sbyte) (itemZ + ((tile.ItemData.Flags & 0x00000400) != 0
                                              ? tile.ItemData.Height / 2
                                              : tile.ItemData.Height));
                    int ourTop = ourZ + PERSON_HEIGHT;
                    int testTop = checkTop;

                    if (moveIsOk)
                    {
                        int cmp = Math.Abs(ourZ - m.Position.Z) - Math.Abs(newZ - m.Position.Z);

                        if (cmp > 0 || cmp == 0 && ourZ > newZ) continue;
                    }

                    if (ourZ + PERSON_HEIGHT > testTop) testTop = ourZ + PERSON_HEIGHT;

                    if ((tile.ItemData.Flags & 0x00000400) == 0) itemTop += tile.ItemData.Height;

                    if (stepTop >= itemTop)
                    {
                        int landCheck = itemZ;

                        if (tile.ItemData.Height >= STEP_HEIGHT)
                            landCheck += STEP_HEIGHT;
                        else
                            landCheck += tile.ItemData.Height;

                        if (considerLand && landCheck < landCenter && landCenter > ourZ && testTop > landLow) continue;

                        if (IsOK(ignoreDoors, ourZ, testTop, tiles, items))
                        {
                            newZ = ourZ;
                            moveIsOk = true;
                        }
                    }
                }
            }

            for (int i = 0; i < items.Count; ++i)
            {
                IDynamicItem item = items[i];
                StaticTiles itemData = item.ItemData;
                ulong flags = itemData.Flags;

                if ((flags & IMPASSABLE_SURFACE) == 0x00000200) // Surface && !Impassable && !Movable
                {
                    //  || (m.CanSwim && (flags & TileFlag.Wet) != 0))
                    // !item.Movable && 
                    // if (cantWalk && (flags & TileFlag.Wet) == 0)
                    //     continue;

                    int itemZ = item.Position.Z;
                    int itemTop = itemZ;
                    sbyte ourZ = (sbyte) (itemZ + ((item.ItemData.Flags & 0x00000400) != 0
                                              ? item.ItemData.Height / 2
                                              : item.ItemData.Height));
                    int ourTop = ourZ + PERSON_HEIGHT;
                    int testTop = checkTop;

                    if (moveIsOk)
                    {
                        int cmp = Math.Abs(ourZ - m.Position.Z) - Math.Abs(newZ - m.Position.Z);

                        if (cmp > 0 || cmp == 0 && ourZ > newZ) continue;
                    }

                    if (ourZ + PERSON_HEIGHT > testTop) testTop = ourZ + PERSON_HEIGHT;

                    if ((itemData.Flags & 0x00000400) == 0) itemTop += itemData.Height;

                    if (stepTop >= itemTop)
                    {
                        int landCheck = itemZ;

                        if (itemData.Height >= STEP_HEIGHT)
                            landCheck += STEP_HEIGHT;
                        else
                            landCheck += itemData.Height;

                        if (considerLand && landCheck < landCenter && landCenter > ourZ && testTop > landLow) continue;

                        if (IsOK(ignoreDoors, ourZ, testTop, tiles, items))
                        {
                            newZ = ourZ;
                            moveIsOk = true;
                        }
                    }
                }
            }


            if (considerLand && !landBlocks && stepTop >= landLow)
            {
                sbyte ourZ = landCenter;
                int ourTop = ourZ + PERSON_HEIGHT;
                int testTop = checkTop;

                if (ourZ + PERSON_HEIGHT > testTop) testTop = ourZ + PERSON_HEIGHT;

                bool shouldCheck = true;

                if (moveIsOk)
                {
                    int cmp = Math.Abs(ourZ - m.Position.Z) - Math.Abs(newZ - m.Position.Z);

                    if (cmp > 0 || cmp == 0 && ourZ > newZ) shouldCheck = false;
                }

                if (shouldCheck && IsOK(ignoreDoors, ourZ, testTop, tiles, items))
                {
                    newZ = ourZ;
                    moveIsOk = true;
                }
            }


            return moveIsOk;
        }
    }
}