using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace ClassicUO.Ecs;

static class Isometric
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 IsoToScreen(ushort isoX, ushort isoY, sbyte isoZ)
    {
        return new Vector2(
            (isoX - isoY) * 22,
            (isoX + isoY) * 22 - (isoZ << 2)
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetDepthZ(int x, int y, int priorityZ)
    {
        const float FACTOR = 0.7071f;
        return (x + y) * FACTOR + priorityZ * 0.01f;
    }
}