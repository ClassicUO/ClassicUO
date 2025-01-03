// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Assets;
using Microsoft.Xna.Framework;
using MathHelper = ClassicUO.Utility.MathHelper;

namespace ClassicUO.Game
{
    internal sealed class Pathfinder
    {
        private const int PATHFINDER_MAX_NODES = 10000;
        private int _goalNode;
        private bool _goalFound;
        private int _activeOpenNodes, _activeCloseNodes, _pathfindDistance;
        private readonly PathNode[] _openList = new PathNode[PATHFINDER_MAX_NODES];
        private readonly PathNode[] _closedList = new PathNode[PATHFINDER_MAX_NODES];
        private readonly PathNode[] _path = new PathNode[PATHFINDER_MAX_NODES];
        private int _pointIndex, _pathSize;
        private bool _run;
        private static readonly int[] _offsetX =
        {
            0, 1, 1, 1, 0, -1, -1, -1, 0, 1
        };
        private static readonly int[] _offsetY =
        {
            -1, -1, 0, 1, 1, 1, 0, -1, -1, -1
        };
        private static readonly sbyte[] _dirOffset =
        {
            1, -1
        };
        private Point _startPoint, _endPoint;

        private readonly World _world;

        public Pathfinder(World world)
        {
            _world = world;
        }

        public bool AutoWalking { get; set; }

        public bool PathindingCanBeCancelled { get; set; }

        public bool BlockMoving { get; set; }

        public bool FastRotation { get; set; }


        private bool CreateItemList(List<PathObject> list, int x, int y, int stepState)
        {
            GameObject tile = _world.Map.GetTile(x, y, false);

            if (tile == null)
            {
                return false;
            }

            bool ignoreGameCharacters = ProfileManager.CurrentProfile.IgnoreStaminaCheck || stepState == (int) PATH_STEP_STATE.PSS_DEAD_OR_GM || _world.Player.IgnoreCharacters || !(_world.Player.Stamina < _world.Player.StaminaMax && _world.Map.Index == 0);

            bool isGM = _world.Player.Graphic == 0x03DB;

            GameObject obj = tile;

            while (obj.TPrevious != null)
            {
                obj = obj.TPrevious;
            }

            for (; obj != null; obj = obj.TNext)
            {
                if (_world.CustomHouseManager != null && obj.Z < _world.Player.Z)
                {
                    continue;
                }

                ushort graphicHelper = obj.Graphic;

                switch (obj)
                {
                    case Land tile1:

                        if (graphicHelper < 0x01AE && graphicHelper != 2 || graphicHelper > 0x01B5 && graphicHelper != 0x01DB)
                        {
                            uint flags = (uint) PATH_OBJECT_FLAGS.POF_IMPASSABLE_OR_SURFACE;

                            if (stepState == (int) PATH_STEP_STATE.PSS_ON_SEA_HORSE)
                            {
                                if (tile1.TileData.IsWet)
                                {
                                    flags = (uint) (PATH_OBJECT_FLAGS.POF_IMPASSABLE_OR_SURFACE | PATH_OBJECT_FLAGS.POF_SURFACE | PATH_OBJECT_FLAGS.POF_BRIDGE);
                                }
                            }
                            else
                            {
                                if (!tile1.TileData.IsImpassable)
                                {
                                    flags = (uint) (PATH_OBJECT_FLAGS.POF_IMPASSABLE_OR_SURFACE | PATH_OBJECT_FLAGS.POF_SURFACE | PATH_OBJECT_FLAGS.POF_BRIDGE);
                                }

                                if (stepState == (int) PATH_STEP_STATE.PSS_FLYING && tile1.TileData.IsNoDiagonal)
                                {
                                    flags |= (uint) PATH_OBJECT_FLAGS.POF_NO_DIAGONAL;
                                }
                            }

                            int landMinZ = tile1.MinZ;
                            int landAverageZ = tile1.AverageZ;
                            int landHeight = landAverageZ - landMinZ;

                            list.Add
                            (
                                new PathObject
                                (
                                    flags,
                                    landMinZ,
                                    landAverageZ,
                                    landHeight,
                                    obj
                                )
                            );
                        }

                        break;

                    case GameEffect _: break;

                    default:
                        bool canBeAdd = true;
                        bool dropFlags = false;

                        switch (obj)
                        {
                            case Mobile mobile:
                            {
                                if (!ignoreGameCharacters && !mobile.IsDead && !mobile.IgnoreCharacters)
                                {
                                    list.Add
                                    (
                                        new PathObject
                                        (
                                            (uint) PATH_OBJECT_FLAGS.POF_IMPASSABLE_OR_SURFACE,
                                            mobile.Z,
                                            mobile.Z + Constants.DEFAULT_CHARACTER_HEIGHT,
                                            Constants.DEFAULT_CHARACTER_HEIGHT,
                                            mobile
                                        )
                                    );
                                }

                                canBeAdd = false;

                                break;
                            }

                            case Item item when item.IsMulti || item.ItemData.IsInternal:
                            {
                                //canBeAdd = false;

                                break;
                            }

                            case Item item2:
                                if (stepState == (int) PATH_STEP_STATE.PSS_DEAD_OR_GM && (item2.ItemData.IsDoor || item2.ItemData.Weight <= 0x5A || isGM && !item2.IsLocked))
                                {
                                    dropFlags = true;
                                }
                                else if (ProfileManager.CurrentProfile.SmoothDoors && item2.ItemData.IsDoor)
                                {
                                    dropFlags = true;
                                }
                                else
                                {
                                    dropFlags = graphicHelper >= 0x3946 && graphicHelper <= 0x3964 || graphicHelper == 0x0082;
                                }

                                break;

                            case Multi m:

                                if ((_world.CustomHouseManager != null && m.IsCustom && (m.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL) == 0) || m.IsHousePreview)
                                {
                                    canBeAdd = false;
                                }

                                if ((m.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER) != 0)
                                {
                                    dropFlags = true;
                                }

                                break;
                        }

                        if (canBeAdd)
                        {
                            uint flags = 0;

                            if (!(obj is Mobile))
                            {
                                var graphic = obj is Item it && it.IsMulti ? it.MultiGraphic : obj.Graphic;
                                ref StaticTiles itemdata = ref Client.Game.UO.FileManager.TileData.StaticData[graphic];

                                if (stepState == (int) PATH_STEP_STATE.PSS_ON_SEA_HORSE)
                                {
                                    if (itemdata.IsWet)
                                    {
                                        flags = (uint) (PATH_OBJECT_FLAGS.POF_SURFACE | PATH_OBJECT_FLAGS.POF_BRIDGE);
                                    }
                                }
                                else
                                {
                                    if (itemdata.IsImpassable || itemdata.IsSurface)
                                    {
                                        flags = (uint) PATH_OBJECT_FLAGS.POF_IMPASSABLE_OR_SURFACE;
                                    }

                                    if (!itemdata.IsImpassable)
                                    {
                                        if (itemdata.IsSurface)
                                        {
                                            flags |= (uint) PATH_OBJECT_FLAGS.POF_SURFACE;
                                        }

                                        if (itemdata.IsBridge)
                                        {
                                            flags |= (uint) PATH_OBJECT_FLAGS.POF_BRIDGE;
                                        }
                                    }

                                    if (stepState == (int) PATH_STEP_STATE.PSS_DEAD_OR_GM)
                                    {
                                        if (graphicHelper <= 0x0846)
                                        {
                                            if (!(graphicHelper != 0x0846 && graphicHelper != 0x0692 && (graphicHelper <= 0x06F4 || graphicHelper > 0x06F6)))
                                            {
                                                dropFlags = true;
                                            }
                                        }
                                        else if (graphicHelper == 0x0873)
                                        {
                                            dropFlags = true;
                                        }
                                    }

                                    if (dropFlags)
                                    {
                                        flags &= 0xFFFFFFFE;
                                    }

                                    if (stepState == (int) PATH_STEP_STATE.PSS_FLYING && itemdata.IsNoDiagonal)
                                    {
                                        flags |= (uint) PATH_OBJECT_FLAGS.POF_NO_DIAGONAL;
                                    }
                                }

                                if (flags != 0)
                                {
                                    int objZ = obj.Z;
                                    int staticHeight = itemdata.Height;
                                    int staticAverageZ = staticHeight;

                                    if (itemdata.IsBridge)
                                    {
                                        staticAverageZ /= 2;
                                        // revert fix from fwiffo because it causes unwalkable stairs [down --> up]
                                        //staticAverageZ += staticHeight % 2;
                                    }

                                    list.Add
                                    (
                                        new PathObject
                                        (
                                            flags,
                                            objZ,
                                            staticAverageZ + objZ,
                                            staticHeight,
                                            obj
                                        )
                                    );
                                }
                            }
                        }

                        break;
                }
            }

            return list.Count != 0;
        }

        private int CalculateMinMaxZ
        (
            ref int minZ,
            ref int maxZ,
            int newX,
            int newY,
            int currentZ,
            int newDirection,
            int stepState
        )
        {
            minZ = -128;
            maxZ = currentZ;
            newDirection &= 7;
            int direction = newDirection ^ 4;
            newX += _offsetX[direction];
            newY += _offsetY[direction];
            List<PathObject> list = new List<PathObject>();

            if (!CreateItemList(list, newX, newY, stepState) || list.Count == 0)
            {
                return 0;
            }

            foreach (PathObject obj in list)
            {
                GameObject o = obj.Object;
                int averageZ = obj.AverageZ;

                if (averageZ <= currentZ && o is Land tile && tile.IsStretched)
                {
                    int avgZ = tile.CalculateCurrentAverageZ(newDirection);

                    if (minZ < avgZ)
                    {
                        minZ = avgZ;
                    }

                    if (maxZ < avgZ)
                    {
                        maxZ = avgZ;
                    }
                }
                else
                {
                    if ((obj.Flags & (uint) PATH_OBJECT_FLAGS.POF_IMPASSABLE_OR_SURFACE) != 0 && averageZ <= currentZ && minZ < averageZ)
                    {
                        minZ = averageZ;
                    }

                    if ((obj.Flags & (uint) PATH_OBJECT_FLAGS.POF_BRIDGE) != 0 && currentZ == averageZ)
                    {
                        int z = obj.Z;
                        int height = z + obj.Height;

                        if (maxZ < height)
                        {
                            maxZ = height;
                        }

                        if (minZ > z)
                        {
                            minZ = z;
                        }
                    }
                }
            }

            maxZ += 2;

            return maxZ;
        }

        public bool CalculateNewZ(int x, int y, ref sbyte z, int direction)
        {
            int stepState = (int) PATH_STEP_STATE.PSS_NORMAL;

            if (_world.Player.IsDead || _world.Player.Graphic == 0x03DB)
            {
                stepState = (int) PATH_STEP_STATE.PSS_DEAD_OR_GM;
            }
            else
            {
                if (_world.Player.IsGargoyle && _world.Player.IsFlying)
                {
                    stepState = (int) PATH_STEP_STATE.PSS_FLYING;
                }
                else
                {
                    Item mount = _world.Player.FindItemByLayer(Layer.Mount);

                    if (mount != null && mount.Graphic == 0x3EB3) // sea horse
                    {
                        stepState = (int) PATH_STEP_STATE.PSS_ON_SEA_HORSE;
                    }
                }
            }

            int minZ = -128;
            int maxZ = z;

            CalculateMinMaxZ
            (
                ref minZ,
                ref maxZ,
                x,
                y,
                z,
                direction,
                stepState
            );

            List<PathObject> list = new List<PathObject>();

            if (_world.CustomHouseManager != null)
            {
                Rectangle rect = new Rectangle(_world.CustomHouseManager.StartPos.X, _world.CustomHouseManager.StartPos.Y, _world.CustomHouseManager.EndPos.X, _world.CustomHouseManager.EndPos.Y);

                if (!rect.Contains(x, y))
                {
                    return false;
                }
            }

            if (!CreateItemList(list, x, y, stepState) || list.Count == 0)
            {
                return false;
            }

            list.Sort();

            list.Add
            (
                new PathObject
                (
                    (uint) PATH_OBJECT_FLAGS.POF_IMPASSABLE_OR_SURFACE,
                    128,
                    128,
                    128,
                    null
                )
            );

            int resultZ = -128;

            if (z < minZ)
            {
                z = (sbyte) minZ;
            }

            int currentTempObjZ = 1000000;
            int currentZ = -128;

            for (int i = 0; i < list.Count; i++)
            {
                PathObject obj = list[i];

                if ((obj.Flags & (uint) PATH_OBJECT_FLAGS.POF_NO_DIAGONAL) != 0 && stepState == (int) PATH_STEP_STATE.PSS_FLYING)
                {
                    int objAverageZ = obj.AverageZ;
                    int delta = Math.Abs(objAverageZ - z);

                    if (delta <= 25)
                    {
                        resultZ = objAverageZ != -128 ? objAverageZ : currentZ;

                        break;
                    }
                }

                if ((obj.Flags & (uint) PATH_OBJECT_FLAGS.POF_IMPASSABLE_OR_SURFACE) != 0)
                {
                    int objZ = obj.Z;

                    if (objZ - minZ >= Constants.DEFAULT_BLOCK_HEIGHT)
                    {
                        for (int j = i - 1; j >= 0; j--)
                        {
                            PathObject tempObj = list[j];

                            if ((tempObj.Flags & (uint) (PATH_OBJECT_FLAGS.POF_SURFACE | PATH_OBJECT_FLAGS.POF_BRIDGE)) != 0)
                            {
                                int tempAverageZ = tempObj.AverageZ;

                                if (tempAverageZ >= currentZ && objZ - tempAverageZ >= Constants.DEFAULT_BLOCK_HEIGHT && (tempAverageZ <= maxZ && (tempObj.Flags & (uint) PATH_OBJECT_FLAGS.POF_SURFACE) != 0 || (tempObj.Flags & (uint) PATH_OBJECT_FLAGS.POF_BRIDGE) != 0 && tempObj.Z <= maxZ))
                                {
                                    int delta = Math.Abs(z - tempAverageZ);

                                    if (delta < currentTempObjZ)
                                    {
                                        currentTempObjZ = delta;
                                        resultZ = tempAverageZ;
                                    }
                                }
                            }
                        }
                    }

                    int averageZ = obj.AverageZ;

                    if (minZ < averageZ)
                    {
                        minZ = averageZ;
                    }

                    if (currentZ < averageZ)
                    {
                        currentZ = averageZ;
                    }
                }
            }

            z = (sbyte) resultZ;

            return resultZ != -128;
        }

        public static void GetNewXY(byte direction, ref int x, ref int y)
        {
            switch (direction & 7)
            {
                case 0:

                {
                    y--;

                    break;
                }

                case 1:

                {
                    x++;
                    y--;

                    break;
                }

                case 2:

                {
                    x++;

                    break;
                }

                case 3:

                {
                    x++;
                    y++;

                    break;
                }

                case 4:

                {
                    y++;

                    break;
                }

                case 5:

                {
                    x--;
                    y++;

                    break;
                }

                case 6:

                {
                    x--;

                    break;
                }

                case 7:

                {
                    x--;
                    y--;

                    break;
                }
            }
        }

        public bool CanWalk(ref Direction direction, ref int x, ref int y, ref sbyte z)
        {
            int newX = x;
            int newY = y;
            sbyte newZ = z;
            byte newDirection = (byte) direction;
            GetNewXY((byte) direction, ref newX, ref newY);
            bool passed = CalculateNewZ(newX, newY, ref newZ, (byte) direction);

            if ((sbyte) direction % 2 != 0)
            {
                if (passed)
                {
                    for (int i = 0; i < 2 && passed; i++)
                    {
                        int testX = x;
                        int testY = y;
                        sbyte testZ = z;
                        byte testDir = (byte) (((byte) direction + _dirOffset[i]) % 8);
                        GetNewXY(testDir, ref testX, ref testY);
                        passed = CalculateNewZ(testX, testY, ref testZ, testDir);
                    }
                }

                if (!passed)
                {
                    for (int i = 0; i < 2 && !passed; i++)
                    {
                        newX = x;
                        newY = y;
                        newZ = z;
                        newDirection = (byte) (((byte) direction + _dirOffset[i]) % 8);
                        GetNewXY(newDirection, ref newX, ref newY);
                        passed = CalculateNewZ(newX, newY, ref newZ, newDirection);
                    }
                }
            }

            if (passed)
            {
                x = newX;
                y = newY;
                z = newZ;
                direction = (Direction) newDirection;
            }

            return passed;
        }

        private int GetGoalDistCost(Point point, int cost)
        {
            return Math.Max(Math.Abs(_endPoint.X - point.X), Math.Abs(_endPoint.Y - point.Y));
        }

        private bool DoesNotExistOnOpenList(int x, int y, int z)
        {
            for (int i = 0; i < PATHFINDER_MAX_NODES; i++)
            {
                PathNode node = _openList[i];

                if (node.Used && node.X == x && node.Y == y && node.Z == z)
                {
                    return true;
                }
            }

            return false;
        }

        private bool DoesNotExistOnClosedList(int x, int y, int z)
        {
            for (int i = 0; i < PATHFINDER_MAX_NODES; i++)
            {
                PathNode node = _closedList[i];

                if (node.Used && node.X == x && node.Y == y && node.Z == z)
                {
                    return true;
                }
            }

            return false;
        }

        private int AddNodeToList
        (
            int list,
            int direction,
            int x,
            int y,
            int z,
            PathNode parent,
            int cost
        )
        {
            if (list == 0)
            {
                if (!DoesNotExistOnClosedList(x, y, z))
                {
                    if (!DoesNotExistOnOpenList(x, y, z))
                    {
                        for (int i = 0; i < PATHFINDER_MAX_NODES; i++)
                        {
                            PathNode node = _openList[i];

                            if (!node.Used)
                            {
                                node.Used = true;
                                node.Direction = direction;
                                node.X = x;
                                node.Y = y;
                                node.Z = z;
                                Point p = new Point(x, y);
                                node.DistFromGoalCost = GetGoalDistCost(p, cost);
                                node.DistFromStartCost = parent.DistFromStartCost + cost;
                                node.Cost = node.DistFromGoalCost + node.DistFromStartCost;
                                node.Parent = parent;

                                if (MathHelper.GetDistance(_endPoint, p) <= _pathfindDistance)
                                {
                                    _goalFound = true;
                                    _goalNode = i;
                                }

                                _activeOpenNodes++;

                                return i;
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < PATHFINDER_MAX_NODES; i++)
                        {
                            PathNode node = _openList[i];

                            if (node.Used)
                            {
                                if (node.X == x && node.Y == y && node.Z == z)
                                {
                                    int startCost = parent.DistFromStartCost + cost;

                                    if (node.DistFromStartCost > startCost)
                                    {
                                        node.Parent = parent;
                                        node.DistFromStartCost = startCost + cost;
                                        node.Cost = node.DistFromGoalCost + node.DistFromStartCost;
                                    }

                                    return i;
                                }
                            }
                        }
                    }
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                parent.Used = false;

                for (int i = 0; i < PATHFINDER_MAX_NODES; i++)
                {
                    PathNode node = _closedList[i];

                    if (!node.Used)
                    {
                        node.Used = true;
                        node.DistFromGoalCost = parent.DistFromGoalCost;
                        node.DistFromStartCost = parent.DistFromStartCost;
                        node.Cost = node.DistFromGoalCost + node.DistFromStartCost;
                        node.Direction = parent.Direction;
                        node.X = parent.X;
                        node.Y = parent.Y;
                        node.Z = parent.Z;
                        node.Parent = parent.Parent;
                        _activeOpenNodes--;
                        _activeCloseNodes++;

                        return i;
                    }
                }
            }

            return -1;
        }

        private bool OpenNodes(PathNode node)
        {
            bool found = false;

            for (int i = 0; i < 8; i++)
            {
                Direction direction = (Direction) i;
                int x = node.X;
                int y = node.Y;
                sbyte z = (sbyte) node.Z;
                Direction oldDirection = direction;

                if (CanWalk(ref direction, ref x, ref y, ref z))
                {
                    if (direction != oldDirection)
                    {
                        continue;
                    }

                    int diagonal = i % 2;

                    if (diagonal != 0)
                    {
                        Direction wantDirection = (Direction) i;
                        int wantX = node.X;
                        int wantY = node.Y;
                        GetNewXY((byte) wantDirection, ref wantX, ref wantY);

                        if (x != wantX || y != wantY)
                        {
                            diagonal = -1;
                        }
                    }

                    if (diagonal >= 0 && AddNodeToList
                    (
                        0,
                        (int) direction,
                        x,
                        y,
                        z,
                        node,
                        diagonal == 0 ? 1 : 2
                    ) != -1)
                    {
                        found = true;
                    }
                }
            }

            return found;
        }

        private int FindCheapestNode()
        {
            int cheapestCost = 9999999;
            int cheapestNode = -1;

            for (int i = 0; i < PATHFINDER_MAX_NODES; i++)
            {
                if (_openList[i].Used)
                {
                    if (_openList[i].Cost < cheapestCost)
                    {
                        cheapestNode = i;

                        cheapestCost = _openList[i].Cost;
                    }
                }
            }

            int result = -1;

            if (cheapestNode != -1)
            {
                result = AddNodeToList
                (
                    1,
                    0,
                    0,
                    0,
                    0,
                    _openList[cheapestNode],
                    2
                );
            }

            return result;
        }

        private bool FindPath(int maxNodes)
        {
            int curNode = 0;

            _closedList[0].Used = true;
            _closedList[0].X = _startPoint.X;
            _closedList[0].Y = _startPoint.Y;
            _closedList[0].Z = _world.Player.Z;
            _closedList[0].Parent = null;
            _closedList[0].DistFromGoalCost = GetGoalDistCost(_startPoint, 0);
            _closedList[0].Cost = _closedList[0].DistFromGoalCost;

            if (GetGoalDistCost(_startPoint, 0) > 14)
            {
                _run = true;
            }

            while (AutoWalking)
            {
                OpenNodes(_closedList[curNode]);

                if (_goalFound)
                {
                    int totalNodes = 0;
                    PathNode goalNode = _openList[_goalNode];

                    while (goalNode.Parent != null && goalNode != goalNode.Parent)
                    {
                        goalNode = goalNode.Parent;
                        totalNodes++;
                    }

                    totalNodes++;
                    _pathSize = totalNodes;
                    goalNode = _openList[_goalNode];

                    while (totalNodes != 0)
                    {
                        totalNodes--;
                        _path[totalNodes] = goalNode;
                        goalNode = goalNode.Parent;
                    }

                    break;
                }

                curNode = FindCheapestNode();

                if (curNode == -1)
                {
                    return false;
                }

                if (_activeCloseNodes >= maxNodes)
                {
                    return false;
                }
            }

            return true;
        }

        public bool WalkTo(int x, int y, int z, int distance)
        {
            if (_world.Player == null /*|| World.Player.Stamina == 0*/ || _world.Player.IsParalyzed)
            {
                return false;
            }

            for (int i = 0; i < PATHFINDER_MAX_NODES; i++)
            {
                if (_openList[i] == null)
                {
                    _openList[i] = new PathNode();
                }

                _openList[i].Reset();

                if (_closedList[i] == null)
                {
                    _closedList[i] = new PathNode();
                }

                _closedList[i].Reset();
            }


            int playerX = _world.Player.X;
            int playerY = _world.Player.Y;
            //sbyte playerZ = 0;
            //Direction playerDir = Direction.None;

            //World.Player.GetEndPosition(ref playerX, ref playerY, ref playerZ, ref playerDir);
            _startPoint.X = playerX;
            _startPoint.Y = playerY;
            _endPoint.X = x;
            _endPoint.Y = y;
            _goalNode = 0;
            _goalFound = false;
            _activeOpenNodes = 0;
            _activeCloseNodes = 0;
            _pathfindDistance = distance;
            _pathSize = 0;
            PathindingCanBeCancelled = true;
            StopAutoWalk();
            AutoWalking = true;

            if (FindPath(PATHFINDER_MAX_NODES))
            {
                _pointIndex = 1;
                ProcessAutoWalk();
            }
            else
            {
                AutoWalking = false;
            }

            return _pathSize != 0;
        }

        public void ProcessAutoWalk()
        {
            if (AutoWalking && _world.InGame && _world.Player.Walker.StepsCount < Constants.MAX_STEP_COUNT && _world.Player.Walker.LastStepRequestTime <= Time.Ticks)
            {
                if (_pointIndex >= 0 && _pointIndex < _pathSize)
                {
                    PathNode p = _path[_pointIndex];

                    _world.Player.GetEndPosition(out int x, out int y, out sbyte z, out Direction dir);

                    if (dir == (Direction) p.Direction)
                    {
                        _pointIndex++;
                    }

                    if (!_world.Player.Walk((Direction) p.Direction, _run))
                    {
                        StopAutoWalk();
                    }
                }
                else
                {
                    StopAutoWalk();
                }
            }
        }

        public void StopAutoWalk()
        {
            AutoWalking = false;
            _run = false;
            _pathSize = 0;
        }

        private enum PATH_STEP_STATE
        {
            PSS_NORMAL = 0,
            PSS_DEAD_OR_GM,
            PSS_ON_SEA_HORSE,
            PSS_FLYING
        }

        [Flags]
        private enum PATH_OBJECT_FLAGS : uint
        {
            POF_IMPASSABLE_OR_SURFACE = 0x00000001,
            POF_SURFACE = 0x00000002,
            POF_BRIDGE = 0x00000004,
            POF_NO_DIAGONAL = 0x00000008
        }

        private class PathObject : IComparable<PathObject>
        {
            public PathObject(uint flags, int z, int avgZ, int h, GameObject obj)
            {
                Flags = flags;
                Z = z;
                AverageZ = avgZ;
                Height = h;
                Object = obj;
            }

            public uint Flags { get; }

            public int Z { get; }

            public int AverageZ { get; }

            public int Height { get; }

            public GameObject Object { get; }

            public int CompareTo(PathObject other)
            {
                int comparision = Z - other.Z;

                if (comparision == 0)
                {
                    comparision = Height - other.Height;
                }

                return comparision;
            }
        }

        private class PathNode
        {
            public int X { get; set; }

            public int Y { get; set; }

            public int Z { get; set; }

            public int Direction { get; set; }

            public bool Used { get; set; }

            public int Cost { get; set; }

            public int DistFromStartCost { get; set; }

            public int DistFromGoalCost { get; set; }

            public PathNode Parent { get; set; }

            public void Reset()
            {
                Parent = null;
                Used = false;
                X = Y = Z = Direction = Cost = DistFromGoalCost = DistFromStartCost = 0;
            }
        }
    }
}