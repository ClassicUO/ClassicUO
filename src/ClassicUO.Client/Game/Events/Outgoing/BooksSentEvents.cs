// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Events.Outgoing
{
    /// <summary>0x93 Pre-AOS book header change.</summary>
    internal readonly record struct BookHeaderChangedOldSentArgs(uint Serial, string Title, string Author);

    /// <summary>0xD4 Post-AOS book header change.</summary>
    internal readonly record struct BookHeaderChangedSentArgs(uint Serial, string Title, string Author);

    /// <summary>0x66 Book page data sent to server.</summary>
    internal readonly record struct BookPageDataSentArgs(uint Serial, string[] Text, int Page);

    /// <summary>0x66 Book page data request.</summary>
    internal readonly record struct BookPageDataRequestSentArgs(uint Serial, ushort Page);
}
