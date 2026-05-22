// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;

namespace ClassicUO.Game.Messaging
{
    /// <summary>
    /// Concrete <see cref="IPromptCoordinator"/>. Holds the current
    /// <see cref="PromptData"/> and fires <see cref="ServerPromptChanged"/>
    /// whenever it is overwritten (including resets to <c>default</c>).
    /// </summary>
    internal sealed class PromptCoordinator : IPromptCoordinator
    {
        public event EventHandler<PromptData> ServerPromptChanged;

        public PromptData PromptData
        {
            get => field;
            set
            {
                field = value;
                ServerPromptChanged?.Invoke(this, value);
            }
        }

        public void OnAsciiPrompt(AsciiPromptArgs e)
        {
            PromptData = new PromptData(ConsolePrompt.ASCII, e.PromptId);
        }

        public void OnUnicodePrompt(UnicodePromptArgs e)
        {
            PromptData = new PromptData(ConsolePrompt.Unicode, e.PromptId);
        }
    }
}
