// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Runtime.CompilerServices;

namespace ClassicUO.Game.Map
{
    /// <summary>
    /// Helper class for coordinate conversions between different coordinate systems.
    /// </summary>
    public static class CoordinateHelper
    {
        /// <summary>
        /// Converts absolute isometric world coordinates to tile coordinates.
        /// This is used in Weather, SplashEffect, and RippleEffect for position calculations.
        /// </summary>
        /// <param name="worldX">Absolute isometric X coordinate</param>
        /// <param name="worldY">Absolute isometric Y coordinate</param>
        /// <returns>A tuple containing (tileX, tileY)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int tileX, int tileY) IsometricToTile(float worldX, float worldY)
        {
            int targetTileX = (int)Math.Round((worldX + worldY) / 44f);
            int targetTileY = (int)Math.Round((worldY - worldX) / 44f);
            return (targetTileX, targetTileY);
        }
    }
}
