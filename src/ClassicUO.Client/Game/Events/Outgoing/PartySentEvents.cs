// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Events.Outgoing
{
    /// <summary>0xBF/0x06/0x01 Party invite request sent.</summary>
    internal readonly record struct PartyInviteRequestSentArgs();

    /// <summary>0xBF/0x06/0x02 Party remove member request sent.</summary>
    internal readonly record struct PartyRemoveRequestSentArgs(uint Serial);

    /// <summary>0xBF/0x06/0x06 Party change loot type request sent.</summary>
    internal readonly record struct PartyChangeLootTypeRequestSentArgs(bool Type);

    /// <summary>0xBF/0x06/0x08 Party accept invite sent.</summary>
    internal readonly record struct PartyAcceptSentArgs(uint Serial);

    /// <summary>0xBF/0x06/0x09 Party decline invite sent.</summary>
    internal readonly record struct PartyDeclineSentArgs(uint Serial);

    /// <summary>0xBF/0x06/0x03 Party message (whisper to one or broadcast) sent.</summary>
    internal readonly record struct PartyMessageSentArgs(string Text, uint Serial);
}
