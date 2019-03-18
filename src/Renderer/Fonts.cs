using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Renderer
{
    static class Fonts
    {
        public static void Load()
        {
            Regular = SpriteFont.Create("ClassicUO.Renderer.fonts.regular_font.xnb");
        }


        public static SpriteFont Regular { get; private set; }
    }
}
