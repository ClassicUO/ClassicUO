using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.Renderer
{
    // N.B. Techniques must be numbered sequentially! Any missing numbers might cause the shader to crash.
    public enum Techniques
    {
        // drawn effects:
        Hued = 0,
        MiniMap = 1,
        Grayscale = 2,
        ShadowSet = 3,
        StencilSet = 4,

        Default = Hued,
        FirstDrawn = Hued,
        LastDrawn = StencilSet,
        All = StencilSet
    }
}
