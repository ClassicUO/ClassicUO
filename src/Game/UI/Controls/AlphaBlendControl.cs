using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    class AlphaBlendControl : Control
    {
        public AlphaBlendControl(float alpha = 0.5f)
        {
            Alpha = alpha;
            AcceptMouseInput = false;
        }

        public override bool Draw(Batcher2D batcher, int x, int y)
        {
            return batcher.Draw2D(CheckerTrans.TransparentTexture, x, y, Width, Height, ShaderHuesTraslator.GetHueVector(0, false, Alpha, false));
        }
    }
}
