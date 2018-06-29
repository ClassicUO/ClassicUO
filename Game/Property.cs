using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game
{
    public struct Property
    {
        public Property(in uint cliloc, in string args) : this()
        {
            Cliloc = cliloc;
            Args = args;
        }

        public uint Cliloc { get; private set; }
        public string Args { get; private set; }
    }
}
