#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using ClassicUO.Game.Views;
using ClassicUO.Renderer;

namespace ClassicUO.Game.GameObjects
{
    public class TextOverhead : GameObject
    {

        public TextOverhead(GameObject parent, string text = "", int maxwidth = 0, ushort hue = 0xFFFF, byte font = 0, bool isunicode = true, FontStyle style = FontStyle.None, float timeToLive = 0.0f)
        {
            Text = text;
            Parent = parent;
            MaxWidth = maxwidth;
            Hue = hue;
            Font = font;
            IsUnicode = isunicode;
            Style = style;
            TimeToLive = timeToLive;
        }

        public string Text { get; }

        public GameObject Parent { get; }

        public bool IsPersistent { get; set; }

        public float TimeToLive { get; set; }

        public MessageType MessageType { get; set; }

        public float Alpha { get; private set; }

        public bool IsUnicode { get; }

        public byte Font { get; }

        public int MaxWidth { get; }

        public FontStyle Style { get; }

        public bool Initialized { get; set; }

        private bool _isOverlapped;

        public bool IsOverlapped
        {
            get => _isOverlapped;
            set
            {
                _isOverlapped = value;

                Alpha = _isOverlapped ? .5f : 0;
            }
        }

        protected override View CreateView()
        {
            return new TextOverheadView(this, MaxWidth, Hue, Font, IsUnicode, Style);
        }

        public override void Update(double totalMS, double frameMS)
        {
            //base.Update(totalMS, frameMS);

            if (IsPersistent || IsDisposed)
                return;

            if (Initialized)
            {
                TimeToLive -= (float) frameMS;

                if (TimeToLive > 0 && TimeToLive <= Constants.TIME_FADEOUT_TEXT)
                {
                    // start alpha decreasing
                    float alpha = 1 - (TimeToLive / Constants.TIME_FADEOUT_TEXT);

                    if (!_isOverlapped || (_isOverlapped && alpha > Alpha))
                        Alpha = alpha;
                }
                else if (TimeToLive <= 0.0f)
                    Dispose();
            }
        }
    }

    public class DamageOverhead : TextOverhead
    {
        public DamageOverhead(GameObject parent, string text = "", int maxwidth = 0, ushort hue = 0xFFFF, byte font = 0, bool isunicode = true, FontStyle style = FontStyle.None, float timeToLive = 0.0f) : base(parent, text, maxwidth, hue, font, isunicode, style, timeToLive)
        {
        }

        public float MovingTime { get; set; }

        public int OffsetY { get; set; }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
            if (Initialized) MovingTime += (float) frameMS;
        }

        protected override View CreateView()
        {
            return new DamageOverheadView(this, MaxWidth, Hue, Font, IsUnicode, Style);
        }
    }
}