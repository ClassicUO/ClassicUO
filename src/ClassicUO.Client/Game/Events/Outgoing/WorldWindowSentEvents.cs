// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Events.Outgoing
{
    /// <summary>0xBF 0x05 Game window size changed.</summary>
    internal readonly record struct GameWindowSizeSentArgs(uint Width, uint Height);

    /// <summary>0xC8 Client view range update.</summary>
    internal readonly record struct ClientViewRangeSentArgs(byte Range);

    /// <summary>0xFA Request to open the UO store.</summary>
    internal readonly record struct OpenUoStoreSentArgs();

    /// <summary>0xFB Toggle public house content visibility.</summary>
    internal readonly record struct ShowPublicHouseContentSentArgs(bool Show);

    /// <summary>0x2C Death screen request.</summary>
    internal readonly record struct DeathScreenSentArgs();
}
