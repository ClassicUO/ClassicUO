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
        public byte[] Data;
    }
}