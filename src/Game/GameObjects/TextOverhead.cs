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
using ClassicUO.Renderer;
using System;
using System.Diagnostics;

namespace ClassicUO.Game.GameObjects
{
    [DebuggerDisplay("Text = {Text}")]
    internal partial class TextOverhead : GameObject
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


            _text = new RenderedText
            {
                MaxWidth = maxwidth,
                Hue = hue,
                Font = font,
                IsUnicode = isunicode,
                FontStyle = style,
                SaveHitMap = true,
                Text = Text
            };
            Texture = _text.Texture;

            if (Engine.Profile.Current.ScaleSpeechDelay)
            {
                int delay = Engine.Profile.Current.SpeechDelay;

                if (delay < 10)
                    delay = 10;

                if (TimeToLive <= 0.0f)
                    TimeToLive = 4000 * _text.LinesCount * delay / 100.0f;
            }
            else
            {
                long delay = ((5497558140000 * Engine.Profile.Current.SpeechDelay) >> 32) >> 5;

                if (TimeToLive <= 0.0f)
                    TimeToLive = (delay >> 31) + delay;
            }

            EdgeDetection = true;
        }

        public string Text { get; }

        public GameObject Parent { get; }

        public float TimeToLive { get; set; }

        public MessageType MessageType { get; set; }

        public bool IsUnicode { get; }

        public byte Font { get; }

        public int MaxWidth { get; }

        public FontStyle Style { get; }

        public bool IsOverlapped { get; set; }

        public override void Dispose()
        {
            _text?.Dispose();
            base.Dispose();
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (IsDisposed)
                return;
         

            TimeToLive -= (float)frameMS;

            if (TimeToLive > 0 && TimeToLive <= Constants.TIME_FADEOUT_TEXT)
            {
                // start alpha decreasing

                //if (Engine.Ticks > _fadeOut)
                //{
                //    _fadeOut = Engine.Ticks + 55;
                //    ProcessAlpha(0);
                //}
                //if (!IsOverlapped || (IsOverlapped && alpha > Alpha))
                //    Alpha = alpha;
            }
            else if (TimeToLive <= 0.0f)
            {
                Dispose();
            }
            else if (IsOverlapped && AlphaHue != 75)
                AlphaHue = 75;
            //else if (!IsOverlapped && AlphaHue != 0xFF)
            //    AlphaHue = 0xFF;                  
        }
    }

    internal class DamageOverhead : TextOverhead
    {
        private const int DAMAGE_Y_MOVING_TIME = 50;

        private uint _movingTime;
        public DamageOverhead(GameObject parent, string text = "", int maxwidth = 0, ushort hue = 0xFFFF, byte font = 0, bool isunicode = true, FontStyle style = FontStyle.None, float timeToLive = 0.0f) : base(parent, text, maxwidth, hue, font, isunicode, style, timeToLive)
        {
            EdgeDetection = false;
        }

        public int OffsetY { get; private set; }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
      
            if (_movingTime < totalMS)
            {
                _movingTime = (uint) totalMS + DAMAGE_Y_MOVING_TIME;
                OffsetY -= 2;
            }          
        }
    }
}