#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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
        private const float TIME_FADEOUT = 2000.0f;

        private readonly int _maxWidth;
        private readonly ushort _hue;
        private readonly byte _font;
        private readonly bool _isUnicode;
        private readonly FontStyle _style;

        public TextOverhead(in GameObject parent, string text = "", int maxwidth = 0, ushort hue = 0xFFFF,
            byte font = 0, bool isunicode = true, FontStyle style = FontStyle.None) : base(parent.Map)
        {
            Text = text;
            Parent = parent;
            _maxWidth = maxwidth;
            _hue = hue;
            _font = font;
            _isUnicode = isunicode;
            _style = style;


            TimeToLive = 2500 + text.Substring(text.IndexOf('>') + 1).Length * 100;
            if (TimeToLive > 10000.0f) TimeToLive = 10000.0f;

            TimeCreated = World.Ticks;
        }

        public string Text { get; }
        public GameObject Parent { get; }
        public bool IsPersistent { get; set; }
        public float TimeToLive { get; }
        public MessageType MessageType { get; set; }
        public float TimeCreated { get; }
        public float Alpha { get; private set; }

        protected override View CreateView() => new TextOverheadView(this, _maxWidth, _hue, _font, _isUnicode, _style);

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (IsPersistent || IsDisposed)
                return;

            float time = (float) totalMS - TimeCreated;

            if (time > TimeToLive)
                Dispose();
            else if (time > TimeToLive - TIME_FADEOUT)
            {
                Alpha = (time - (TimeToLive - TIME_FADEOUT)) / TIME_FADEOUT;
            }
        }
    }
}