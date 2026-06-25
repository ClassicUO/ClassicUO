// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Runtime.CompilerServices;
using ClassicUO.Assets;

namespace ClassicUO.Ecs;

// Shared static-tile draw filters. The "nodraw" decision is the legacy hack of
// matching a tile's name against the "nodraw" prefix; the tile-data Name string
// never changes after load, so the per-graphic result is computed once into a
// frozen bool[] and every call site does a pure index lookup instead of a fresh
// OrdinalIgnoreCase StartsWith over the string.
internal static class MapDrawHelpers
{
    private static bool[] _noDraw;

    // Frozen lookup of the legacy "nodraw" name check, built once over the
    // immutable StaticData table. Equivalent to:
    //   !string.IsNullOrEmpty(data.Name) && data.Name.StartsWith("nodraw", OrdinalIgnoreCase)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNoDraw(TileDataLoader tileData, ushort g)
    {
        var table = _noDraw;
        if (table is null || table.Length != tileData.StaticData.Length)
            table = BuildNoDrawTable(tileData);

        return g < table.Length && table[g];
    }

    private static bool[] BuildNoDrawTable(TileDataLoader tileData)
    {
        var data = tileData.StaticData;
        var table = new bool[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            var name = data[i].Name;
            table[i] = !string.IsNullOrEmpty(name)
                && name.StartsWith("nodraw", StringComparison.OrdinalIgnoreCase);
        }

        _noDraw = table;
        return table;
    }

    // Radar subset of GameObject.CanBeDrawn shared by the minimap + world-map
    // bakes — filters no-draw / decorative statics so they don't speckle the
    // radar. (Drops the gargoyle-animated and client-version easel edge cases of
    // the full WorldRenderingPlugin predicate; not worth the race/version
    // plumbing for a baked radar.)
    public static bool CanBeDrawnRadar(TileDataLoader tileData, ushort g)
    {
        switch (g)
        {
            case 0x0001:
            case 0x21BC:
            case 0xA1FE:
            case 0xA1FF:
            case 0xA200:
            case 0xA201:
                return false;

            case 0x9E4C:
            case 0x9E64:
            case 0x9E65:
            case 0x9E7D:
            {
                ref var d = ref tileData.StaticData[g];
                return !d.IsBackground && !d.IsSurface;
            }
        }

        if (g == 0x63D3)
            return false;
        if (g >= 0x2198 && g <= 0x21A4)
            return false;

        if (g < tileData.StaticData.Length)
        {
            if (IsNoDraw(tileData, g))
                return false;
            ref var data = ref tileData.StaticData[g];
            if (!data.IsNoDiagonal)
                return true;
        }

        return false;
    }
}
