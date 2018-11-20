using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

namespace ClassicUO.Game.Gumps.Controls
{
    internal class HoveredLabel : Label
    {
        private readonly ushort _normalHue;
        private readonly ushort _overHue;

        public HoveredLabel(string text, bool isunicode, ushort hue, ushort overHue, int maxwidth = 0, byte font = 255, FontStyle style = FontStyle.None, TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT, float timeToLive = 0) : base(text, isunicode, hue, maxwidth, font, style, align, timeToLive)
        {
            _overHue = overHue;
            _normalHue = hue;
            AcceptMouseInput = true;
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (MouseIsOver)
                Hue = _overHue;
            else
                Hue = _normalHue;
            base.Update(totalMS, frameMS);
        }
    }
}