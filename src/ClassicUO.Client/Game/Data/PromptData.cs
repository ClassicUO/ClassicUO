// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Data
{
    internal enum ConsolePrompt
    {
        None,
        ASCII,
        Unicode
    }

    internal readonly struct PromptData(ConsolePrompt prompt, ulong data)
    {
        public readonly ConsolePrompt Prompt = prompt;
        public readonly ulong Data = data;
    }
}