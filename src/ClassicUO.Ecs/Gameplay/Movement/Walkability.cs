using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Renderer;

namespace ClassicUO.Ecs;

// Legacy Pathfinder.PATH_STEP_STATE — how the player currently moves.
// Dead/GM pass doors and light obstacles, a sea-horse mount walks water
// only, a flying gargoyle ignores NoDiagonal blockers within 25 z.
enum PathStepState
{
    Normal = 0,
    DeadOrGm,
    OnSeaHorse,
    Flying
}

[Flags]
enum TerrainFlags : uint
{
    ImpassableOrSurface = 0x00000001,
    Surface = 0x00000002,
    Bridge = 0x00000004,
    NoDiagonal = 0x00000008
}

struct TerrainInfo : IComparable<TerrainInfo>
{
    public TerrainFlags Flags;
    public int Z;
    public int AvgZ;
    public int Height;
    public bool LandStretched;
    public UltimaBatcher2D.YOffsets LandBounds;
    public int RealZ;

    public readonly int CompareTo(TerrainInfo other)
    {
        var comparision = Z - other.Z;

        if (comparision == 0)
        {
            comparision = Height - other.Height;
        }

        return comparision;
    }
}

// Per-frame movement modifiers shared by manual walking and the pathfinder.
// Computed once by PlayerMovementPlugin.ComputeMovementState from the player's
// body graphic, flags, stamina and mount (legacy CreateItemList preamble).
sealed class MovementState
{
    public List<TerrainInfo> Scratch = new();
    public PathStepState StepState;
    public bool IgnoreCharacters;
    public bool IsGm;
    public bool SmoothDoors;
    public bool IsParalyzed;
}

// Legacy Pathfinder walkability math, query-free: callers fill a
// List&lt;TerrainInfo&gt; for a tile (live ECS scan or pathfinder snapshot) and the
// cores compute the same step-z results as legacy CalculateMinMaxZ /
// CalculateNewZ.
internal static class Walkability
{
    public static void GetNewXY(Direction direction, out int offsetX, out int offsetY)
    {
        direction &= Direction.Mask;
        offsetX = 0;
        offsetY = 0;

        switch (direction)
        {
            case Direction.North: offsetY--; break;
            case Direction.Right: offsetX++; offsetY--; break;
            case Direction.East: offsetX++; break;
            case Direction.Down: offsetX++; offsetY++; break;
            case Direction.South: offsetY++; break;
            case Direction.Left: offsetX--; offsetY++; break;
            case Direction.West: offsetX--; break;
            case Direction.Up: offsetX--; offsetY--; break;
        }
    }

    // Legacy Mobile.IsDead ghost bodies — dead mobiles never block a path.
    public static bool IsDeadBody(ushort graphic)
        => graphic == 0x0192 || graphic == 0x0193 ||
           (graphic >= 0x025F && graphic <= 0x0260) ||
           graphic == 0x02B6 || graphic == 0x02B7;

    // Legacy CreateItemList land filter — cave entrances / void tiles are
    // skipped entirely.
    public static bool LandPasses(ushort graphic)
        => (graphic < 0x01AE && graphic != 2) || (graphic > 0x01B5 && graphic != 0x01DB);

    public static TerrainFlags LandFlags(ref readonly LandTiles landData, PathStepState stepState)
    {
        var flags = TerrainFlags.ImpassableOrSurface;

        if (stepState == PathStepState.OnSeaHorse)
        {
            if (landData.IsWet)
                flags = TerrainFlags.ImpassableOrSurface | TerrainFlags.Surface | TerrainFlags.Bridge;
        }
        else
        {
            if (!landData.IsImpassable)
                flags = TerrainFlags.ImpassableOrSurface | TerrainFlags.Surface | TerrainFlags.Bridge;

            if (stepState == PathStepState.Flying && landData.IsNoDiagonal)
                flags |= TerrainFlags.NoDiagonal;
        }

        return flags;
    }

    public static TerrainInfo MakeLand(TerrainFlags flags, sbyte z, bool stretchedValid, in TileStretched stretched)
    {
        if (!stretchedValid)
        {
            return new TerrainInfo
            {
                Flags = flags,
                Z = z,
                AvgZ = z,
                Height = 0,
                LandStretched = false,
                LandBounds = default,
                RealZ = z
            };
        }

        return new TerrainInfo
        {
            Flags = flags,
            Z = stretched.MinZ,
            AvgZ = stretched.AvgZ,
            Height = stretched.AvgZ - stretched.MinZ,
            LandStretched = true,
            LandBounds = stretched.Offset,
            RealZ = z
        };
    }

    // Legacy CreateItemList static/item flag chain: doors and energy fields
    // drop the impassable bit (SmoothDoors / dead-or-GM pass-through), wet
    // checks for sea-horse riders, NoDiagonal for flying gargoyles.
    // NOTE: legacy also lets a GM pass unlocked items only — item lock state
    // isn't tracked ECS-side, so a GM body passes all light items.
    public static TerrainFlags StaticFlags(ushort graphic, ref readonly StaticTiles itemData, PathStepState stepState, bool smoothDoors, bool isGm)
    {
        if (stepState == PathStepState.OnSeaHorse)
        {
            return itemData.IsWet ? TerrainFlags.Surface | TerrainFlags.Bridge : 0;
        }

        bool dropFlags;
        if (stepState == PathStepState.DeadOrGm && (itemData.IsDoor || itemData.Weight <= 0x5A || isGm))
            dropFlags = true;
        else if (smoothDoors && itemData.IsDoor)
            dropFlags = true;
        else
            dropFlags = (graphic >= 0x3946 && graphic <= 0x3964) || graphic == 0x0082;

        TerrainFlags flags = 0;

        if (itemData.IsImpassable || itemData.IsSurface)
            flags = TerrainFlags.ImpassableOrSurface;

        if (!itemData.IsImpassable)
        {
            if (itemData.IsSurface)
                flags |= TerrainFlags.Surface;

            if (itemData.IsBridge)
                flags |= TerrainFlags.Bridge;
        }

        if (stepState == PathStepState.DeadOrGm)
        {
            if (graphic <= 0x0846)
            {
                if (!(graphic != 0x0846 && graphic != 0x0692 && (graphic <= 0x06F4 || graphic > 0x06F6)))
                    dropFlags = true;
            }
            else if (graphic == 0x0873)
            {
                dropFlags = true;
            }
        }

        if (dropFlags)
            flags &= ~TerrainFlags.ImpassableOrSurface;

        if (stepState == PathStepState.Flying && itemData.IsNoDiagonal)
            flags |= TerrainFlags.NoDiagonal;

        return flags;
    }

    public static TerrainInfo MakeStatic(TerrainFlags flags, sbyte z, ref readonly StaticTiles itemData)
    {
        return new TerrainInfo
        {
            Flags = flags,
            Z = z,
            AvgZ = z + (itemData.IsBridge ? itemData.Height / 2 : itemData.Height),
            Height = itemData.Height
        };
    }

    // Legacy CreateItemList mobile branch — a living character occupies
    // z .. z + DEFAULT_CHARACTER_HEIGHT as an impassable column.
    public static TerrainInfo MakeMobile(sbyte z)
    {
        return new TerrainInfo
        {
            Flags = TerrainFlags.ImpassableOrSurface,
            Z = z,
            AvgZ = z + Constants.DEFAULT_CHARACTER_HEIGHT,
            Height = Constants.DEFAULT_CHARACTER_HEIGHT
        };
    }

    // Legacy CalculateMinMaxZ inner loop. `list` holds the items at the tile
    // BEHIND the destination (direction ^ 4); dirIndex is the raw 0..7 step
    // direction.
    public static void GetMinMaxZCore(List<TerrainInfo> list, int dirIndex, int playerZ, out int minZ, out int maxZ)
    {
        maxZ = playerZ;
        minZ = -128;

        foreach (ref readonly var tinfo in CollectionsMarshal.AsSpan(list))
        {
            if (tinfo.AvgZ <= playerZ && tinfo.LandStretched)
            {
                var avgZ = getAvgZ(dirIndex, tinfo.LandBounds, tinfo.RealZ);
                if (minZ < avgZ)
                    minZ = avgZ;
                if (maxZ < avgZ)
                    maxZ = avgZ;
            }
            else
            {
                if ((tinfo.Flags & TerrainFlags.ImpassableOrSurface) != 0 && tinfo.AvgZ <= playerZ && minZ < tinfo.AvgZ)
                {
                    minZ = tinfo.AvgZ;
                }

                if ((tinfo.Flags & TerrainFlags.Bridge) != 0 && playerZ == tinfo.AvgZ)
                {
                    var height = tinfo.Z + tinfo.Height;

                    if (maxZ < height)
                        maxZ = height;

                    if (minZ > tinfo.Z)
                        minZ = tinfo.Z;
                }
            }
        }

        maxZ += 2;

        static int getAvgZ(int dir, UltimaBatcher2D.YOffsets offs, int curZ)
        {
            int res = getDirZ(((byte)(dir >> 1) + 1) & 3, offs, curZ);
            if ((dir & 1) != 0) return res;
            return (res + getDirZ(dir >> 1, offs, curZ)) >> 1;

            static int getDirZ(int dir, UltimaBatcher2D.YOffsets offs, int curZ) => dir switch
            {
                1 => offs.Right >> 2,
                2 => offs.Bottom >> 2,
                3 => offs.Left >> 2,
                _ => curZ
            };
        }
    }

    // Legacy CalculateNewZ result loop. `list` holds the items at the
    // destination tile; sorted + sentinel appended here. Returns the new z
    // via `z`, false when the tile can't be stood on.
    public static bool CalculateNewZCore(List<TerrainInfo> list, PathStepState stepState, int minZ, int maxZ, ref sbyte z)
    {
        if (list.Count == 0)
            return false;

        list.Sort();

        list.Add(new TerrainInfo
        {
            Flags = TerrainFlags.ImpassableOrSurface,
            Z = 128,
            AvgZ = 128,
            Height = 128
        });

        var result = -128;

        if (z < minZ)
        {
            z = (sbyte)minZ;
        }

        var currentTempZ = 1000000;
        var currentZ = -128;

        for (int i = 0; i < list.Count; ++i)
        {
            var tinfo = list[i];

            if ((tinfo.Flags & TerrainFlags.NoDiagonal) != 0 && stepState == PathStepState.Flying)
            {
                var delta = Math.Abs(tinfo.AvgZ - z);

                if (delta <= 25)
                {
                    result = tinfo.AvgZ != -128 ? tinfo.AvgZ : currentZ;
                    break;
                }
            }

            if ((tinfo.Flags & TerrainFlags.ImpassableOrSurface) != 0)
            {
                if (tinfo.Z - minZ >= Constants.DEFAULT_BLOCK_HEIGHT)
                {
                    for (int j = i - 1; j >= 0; --j)
                    {
                        var temptInfo = list[j];

                        if ((temptInfo.Flags & (TerrainFlags.Surface | TerrainFlags.Bridge)) != 0)
                        {
                            if (temptInfo.AvgZ >= currentZ && tinfo.Z - temptInfo.AvgZ >= Constants.DEFAULT_BLOCK_HEIGHT &&
                                (temptInfo.AvgZ <= maxZ && (temptInfo.Flags & TerrainFlags.Surface) != 0 ||
                                 (temptInfo.Flags & TerrainFlags.Bridge) != 0 && temptInfo.Z <= maxZ))
                            {
                                var delta = Math.Abs(z - temptInfo.AvgZ);
                                if (delta < currentTempZ)
                                {
                                    currentTempZ = delta;
                                    result = temptInfo.AvgZ;
                                }
                            }
                        }
                    }
                }

                if (minZ < tinfo.AvgZ)
                {
                    minZ = tinfo.AvgZ;
                }

                if (currentZ < tinfo.AvgZ)
                {
                    currentZ = tinfo.AvgZ;
                }
            }
        }

        z = (sbyte)result;

        return result != -128;
    }
}
