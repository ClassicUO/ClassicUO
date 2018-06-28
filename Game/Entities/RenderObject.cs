using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.Entities
{
    public abstract class RenderObject
    {
        public abstract Position Position { get; set; }
        public int Facet { get; set; }
    }
}
