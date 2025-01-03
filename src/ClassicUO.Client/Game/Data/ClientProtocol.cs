// SPDX-License-Identifier: BSD-2-Clause

using System;

namespace ClassicUO.Game.Data
{
    [Flags]
    internal enum ClientFlags : uint
    {
        CF_T2A = 0x00,
        CF_RE = 0x01,
        CF_TD = 0x02,
        CF_LBR = 0x04,
        CF_AOS = 0x08,
        CF_SE = 0x10,
        CF_SA = 0x20,
        CF_UO3D = 0x40,
        CF_RESERVED = 0x80,
        CF_3D = 0x100,
        CF_UNDEFINED = 0xFFFF
    }
}