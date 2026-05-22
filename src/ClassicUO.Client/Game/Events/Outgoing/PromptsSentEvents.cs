// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Events.Outgoing
{
    /// <summary>0x9A ASCII prompt response sent. <see cref="Cancel"/> = true if the prompt was cancelled rather than answered.</summary>
    internal readonly record struct AsciiPromptResponseSentArgs(string Text, bool Cancel);

    /// <summary>0xC2 Unicode prompt response sent. <see cref="Cancel"/> = true if the prompt was cancelled rather than answered.</summary>
    internal readonly record struct UnicodePromptResponseSentArgs(string Text, string Lang, bool Cancel);
}
