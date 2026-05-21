// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Events
{
    internal readonly record struct GumpOpenedArgs(uint Sender, uint GumpId, int X, int Y);

    internal readonly record struct GumpClosedArgs(uint Sender, uint GumpId, int ButtonId);
}
