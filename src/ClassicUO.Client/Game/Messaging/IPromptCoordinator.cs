// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;

namespace ClassicUO.Game.Messaging
{
    /// <summary>
    /// Owns the server-prompt state (<see cref="PromptData"/>) and the
    /// <c>AsciiPrompt</c> / <c>UnicodePrompt</c> EventSink handlers. Single
    /// responsibility: track the most recent server-prompt request and notify
    /// listeners when it changes.
    /// </summary>
    internal interface IPromptCoordinator
    {
        PromptData PromptData { get; set; }
        event EventHandler<PromptData> ServerPromptChanged;

        void OnAsciiPrompt(AsciiPromptArgs e);
        void OnUnicodePrompt(UnicodePromptArgs e);
    }
}
