namespace ClassicUO.Game.Data
{
    public enum ConsolePrompt
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