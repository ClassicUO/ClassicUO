using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.Renderer;
using ClassicUO.Game.Renderer.Views;

namespace ClassicUO.Game.GameObjects
{
    public class TextOverhead : GameObject
    {
        private readonly int _maxWidth;
        private readonly ushort _hue;
        private readonly byte _font;
        private readonly bool _isUnicode;
        private readonly FontStyle _style;

        public TextOverhead(in GameObject parent, string text = "", int maxwidth = 0, ushort hue = 0xFFFF, byte font = 0, bool isunicode = true, FontStyle style = FontStyle.None) : base(parent.Map)
        {
            Text = text;
            Parent = parent;
            _maxWidth = maxwidth; _hue = hue; _font = font; _isUnicode = isunicode; _style = style;


            TimeToLive = 2500 + (text.Substring(text.IndexOf('>') + 1).Length * 100);
            if (TimeToLive > 10000)
            {
                TimeToLive = 10000;
            }
        }

        public string Text { get; private set; }
        public GameObject Parent { get; }
        public bool IsPersistent { get; set; }
        public int TimeToLive { get; set; }
        public MessageType MessageType { get; set; }

        protected override View CreateView()
        {
            return new TextOverheadView(this, _maxWidth, _hue, _font, _isUnicode, _style);
        }

        public override void Update(double frameMS)
        {
            base.Update(frameMS);

            if (IsPersistent || IsDisposed)
                return;

            TimeToLive -= (int)frameMS;

            if (TimeToLive <= 0)
                Dispose();
        }

    }
}
