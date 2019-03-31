using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    class FadeOutLabel : Label
    {
        private float _timeToLive;

        public FadeOutLabel(string text, bool isunicode, ushort hue, float timeToLive, int maxwidth = 0, byte font = 0xFF, FontStyle style = FontStyle.None, TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT) 
            : base(text, isunicode, hue, maxwidth, font, style, align)
        {
            _timeToLive = timeToLive;
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            _timeToLive -= (float)frameMS;

            if (_timeToLive > 0 && _timeToLive <= Constants.TIME_FADEOUT_TEXT)
                Alpha = 1.0f - (_timeToLive / Constants.TIME_FADEOUT_TEXT);

            if (_timeToLive <= 0.0f)
                Dispose();
        }
    }
}
