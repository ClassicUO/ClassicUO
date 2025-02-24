// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Data
{
    internal enum ConsolePrompt
    {
        None,
        ASCII,
        Unicode
    }

    internal struct PromptData
    {
        public ConsolePrompt Prompt;
        public ulong Data;
    }
}