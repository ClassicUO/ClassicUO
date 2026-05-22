// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Events.Outgoing
{
    /// <summary>0x71 0x03 Request the body of a single bulletin board message.</summary>
    internal readonly record struct BulletinBoardRequestMessageSentArgs(uint Serial, uint MsgSerial);

    /// <summary>0x71 0x04 Request the summary of a single bulletin board message.</summary>
    internal readonly record struct BulletinBoardRequestMessageSummarySentArgs(uint Serial, uint MsgSerial);

    /// <summary>0x71 0x05 Post a bulletin board message.</summary>
    internal readonly record struct BulletinBoardPostMessageSentArgs(uint Serial, uint MsgSerial, string Subject, string Text);

    /// <summary>0x71 0x06 Remove a bulletin board message.</summary>
    internal readonly record struct BulletinBoardRemoveMessageSentArgs(uint Serial, uint MsgSerial);
}
