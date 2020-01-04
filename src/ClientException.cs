using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO
{
    class InvalidClientVersion : Exception
    {
        public InvalidClientVersion(string msg) : base(msg)
        {

        }
    }

    class InvalidClientDirectory : Exception
    {
        public InvalidClientDirectory(string msg) : base(msg)
        {

        }
    }
}
