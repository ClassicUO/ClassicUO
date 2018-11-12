using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.GameObjects;

namespace ClassicUO.Interfaces
{
    interface IMobilePaperdollOwner
    {
        Mobile Mobile { get; set; }
    }
}
