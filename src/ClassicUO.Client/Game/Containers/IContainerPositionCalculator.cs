// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Containers
{
    /// <summary>
    /// Computes the screen-space top-left position for a newly-opened container
    /// gump. Honours the cached gump position, profile overrides
    /// (near-object / top-right / fixed) and a wrap-around grid for the
    /// default case. Stateful: the last calculated position is exposed via
    /// <see cref="X"/> / <see cref="Y"/> for the caller to consume.
    /// </summary>
    internal interface IContainerPositionCalculator
    {
        int DefaultX { get; }
        int DefaultY { get; }
        int X { get; }
        int Y { get; }

        void CalculateContainerPosition(uint serial, ushort graphic);
    }
}
