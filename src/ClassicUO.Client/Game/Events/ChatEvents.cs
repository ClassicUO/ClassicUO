// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;

namespace ClassicUO.Game.Events
{
    internal readonly record struct ChatMessageArgs(
        uint Serial,
        ushort Graphic,
        MessageType Type,
        ushort Hue,
        byte Font,
        string Name,
        string Text);

    internal readonly record struct UnicodeChatMessageArgs(
        uint Serial,
        ushort Graphic,
        MessageType Type,
        ushort Hue,
        byte Font,
        string Lang,
        string Name,
        string Text);

    internal readonly record struct ClilocMessageArgs(
        uint Serial,
        ushort Graphic,
        MessageType Type,
        ushort Hue,
        byte Font,
        uint Cliloc,
        string Name,
        string Arguments,
        string Affix,
        byte AffixFlags);

    internal readonly record struct AsciiPromptArgs(ulong PromptId);

    internal readonly record struct UnicodePromptArgs(ulong PromptId);

    // ---- Channel chat (0xB2) sub-commands ----
    internal readonly record struct ChatConferenceCreatedArgs(string ChannelName, bool HasPassword);

    internal readonly record struct ChatConferenceDestroyedArgs(string ChannelName);

    internal readonly record struct ChatUsernameRequestArgs();

    internal readonly record struct ChatClosedArgs();

    internal readonly record struct ChatUsernameAcceptedArgs(string Username);

    internal readonly record struct ChatUserAddedArgs(ushort UserType, string Username);

    internal readonly record struct ChatUserRemovedArgs(string Username);

    internal readonly record struct ChatClearAllPlayersArgs();

    internal readonly record struct ChatConferenceJoinedArgs(string ChannelName);

    internal readonly record struct ChatConferenceLeftArgs(string ChannelName);

    internal readonly record struct ChatTextReceivedArgs(ushort Cmd, string Username, string Message);

    internal readonly record struct ChatSystemMessageArgs(ushort Cmd, string Text);
}
