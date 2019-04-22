using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
