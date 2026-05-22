// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;

namespace ClassicUO.Game.Events.Outgoing
{
    /// <summary>0x12 Emote action text sent.</summary>
    internal readonly record struct EmoteActionSentArgs(string Action);

    /// <summary>0x03 ASCII speech request sent.</summary>
    internal readonly record struct AsciiSpeechRequestSentArgs(string Text, MessageType Type, byte Font, ushort Hue);

    /// <summary>0xAD Unicode speech request sent.</summary>
    internal readonly record struct UnicodeSpeechRequestSentArgs(string Text, MessageType Type, byte Font, ushort Hue, string Lang);

    /// <summary>0xB3 Chat join channel command sent.</summary>
    internal readonly record struct ChatJoinCommandSentArgs(string Name, string Password);

    /// <summary>0xB3 Chat create channel command sent.</summary>
    internal readonly record struct ChatCreateChannelCommandSentArgs(string Name, string Password);

    /// <summary>0xB3 Chat leave channel command sent.</summary>
    internal readonly record struct ChatLeaveChannelCommandSentArgs();

    /// <summary>0xB3 Chat message command sent.</summary>
    internal readonly record struct ChatMessageCommandSentArgs(string Msg);

    /// <summary>0xB5 Open chat window sent.</summary>
    internal readonly record struct OpenChatSentArgs(string Name);

    /// <summary>0x56 Map message (pin add/move/remove etc.) sent.</summary>
    internal readonly record struct MapMessageSentArgs(uint Serial, byte Action, byte Pin, ushort X, ushort Y);

    /// <summary>0xF0 Razor handshake ACK sent.</summary>
    internal readonly record struct RazorAckSentArgs();
}
