using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.UI
{
    [Flags]
    enum TEXT_ENTRY_RULES : uint
    {
        NUMERIC = 0x00000001,
        SYMBOL = 0x00000002,
        LETTER = 0x00000004,
        SPACE = 0x00000008,
        UNUMERIC = 0x00000010 // unsigned
    }

}
