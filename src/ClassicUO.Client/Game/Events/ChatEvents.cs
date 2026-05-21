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
}
