using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Renderer
{
    public enum Techniques
    {
        // drawn effects:
        None = -1,
        Hued = 0,
        MiniMap = 1,
        Grayscale = 2,
        ShadowSet = 3,
        StencilSet = 4,
        Land = 5,

        Default = Hued,
        FirstDrawn = Hued,
        LastDrawn = Land,
        All = Land
    }
}
