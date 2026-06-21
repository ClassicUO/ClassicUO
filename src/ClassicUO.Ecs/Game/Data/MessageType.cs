// SPDX-License-Identifier: BSD-2-Clause

using System;

namespace ClassicUO.Game.Data
{
    [Flags]
    internal enum MessageType : byte
    {
        Regular = 0,
        System = 1,
        Emote = 2,
        Limit3Spell = 3, // Sphere style shards use this to limit to 3 of these message types showing overhead.
        Label = 6,
        Focus = 7,
        Whisper = 8,
        Yell = 9,
        Spell = 10,
        Guild = 13,
        Alliance = 14,
        Command = 15,
        Encoded = 0xC0,
        Party = 0xFF // This is a CUO assigned type, value is unimportant
    }
}