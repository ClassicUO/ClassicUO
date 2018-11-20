using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.IO.Resources;

namespace ClassicUO.Game.Data
{
    struct PopupMenuItem
    {
        public int Cliloc;
        public ushort Index;
        public Hue Hue;
        public Hue ReplacedHue;
        public ushort Flags;
    }
}
