using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.WorldObjects.Interfaces
{
    public interface IDeferreable
    {
        DeferredEntity DeferredObject { get; set; }
    }
}
