// SPDX-License-Identifier: BSD-2-Clause

using System;

namespace ClassicUO.Utility.Logging
{
    [Flags]
    public enum LogTypes : byte
    {
        None = 0x00,
        Info = 0x01,
        Debug = 0x02,
        Trace = 0x04,
        Warning = 0x08,
        Error = 0x10,
        Panic = 0x20,
        Table = 0x30,
        All = byte.MaxValue
    }
}