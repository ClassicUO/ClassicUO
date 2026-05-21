// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Events
{
    internal readonly record struct SoundPlayArgs(
        ushort Index,
        ushort Audio,
        ushort X,
        ushort Y,
        short Z);

    internal readonly record struct MusicPlayArgs(ushort Index);
}
