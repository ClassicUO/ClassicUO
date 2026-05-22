// SPDX-License-Identifier: BSD-2-Clause

using System;

namespace ClassicUO.Game.Events.Outgoing
{
    /// <summary>0x6F Trade response sent (accept/decline/cancel).</summary>
    internal readonly record struct TradeResponseSentArgs(uint Serial, int Code, bool State);

    /// <summary>0x6F Trade gold/platinum amount update sent.</summary>
    internal readonly record struct TradeUpdateGoldSentArgs(uint Serial, uint Gold, uint Platinum);

    /// <summary>0x3B Vendor buy request sent. <see cref="Items"/> is an array of (item serial, amount) tuples.</summary>
    internal readonly record struct BuyRequestSentArgs(uint Serial, Tuple<uint, ushort>[] Items);

    /// <summary>0x9F Vendor sell request sent. <see cref="Items"/> is an array of (item serial, amount) tuples.</summary>
    internal readonly record struct SellRequestSentArgs(uint Serial, Tuple<uint, ushort>[] Items);
}
