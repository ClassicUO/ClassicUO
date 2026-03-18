// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Data
{
    public enum ConsolePrompt
    {
        None,
        ASCII,
        Unicode
    }

    public readonly struct PromptData(ConsolePrompt prompt, ulong data)
    {
        public readonly ConsolePrompt Prompt = prompt;
        public readonly ulong Data = data;
    }
}