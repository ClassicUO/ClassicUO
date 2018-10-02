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
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps
{
    public class Label : GumpControl
    {
        private readonly RenderedText _gText;
        private float _timeToLive;
        private float _timeCreated;
        private float _alpha;

        public Label(string text, bool isunicode, ushort hue, int maxwidth = 0, FontStyle style = FontStyle.None, TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT)
        {
            _gText = new RenderedText
            {
                IsUnicode = isunicode,
                Font = (byte) (FileManager.ClientVersion >= ClientVersions.CV_305D ? 1 : 0),
                FontStyle = style,
                Hue = hue++,
                Align = align,
                MaxWidth = maxwidth,
                Text = text
            };
            AcceptMouseInput = false;

            Width = _gText.Width;
            Height = _gText.Height;
        }

        public Label(string[] parts, string[] lines) : this(lines[int.Parse(parts[4])], true, TransformHue(Hue.Parse(parts[3])), 0, FontStyle.BlackBorder, TEXT_ALIGN_TYPE.TS_LEFT)
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
        }


        private static Hue TransformHue(Hue hue)
        {
            if (hue > 1)
                hue -= 2;
            if (hue < 2)
                hue = 1;
            return hue;
        }

        public byte Font
        {
            get => _gText.Font;
            set => _gText.Font = value;
        }

        public string Text
        {
            get => _gText.Text;
            set => _gText.Text = value;
        }

        public bool FadeOut { get; set; }

        public override void Update(double totalMS, double frameMS)
        {
            if (IsDisposed)
                return;

            if (FadeOut)
            {
                float time = (float) totalMS - _timeCreated;
                if (time > _timeToLive)
                    Dispose();
                else if (time > _timeToLive - 2000)
                {
                    _alpha = (time - (_timeToLive - 2000)) / 2000;
                }
            }

            base.Update(totalMS, frameMS);

        }


        protected override void OnInitialize()
        {
            if (FadeOut)
            {
                _timeToLive = 2500 + Text.Substring(Text.IndexOf('>') + 1).Length * 100;
                if (_timeToLive > 10000.0f)
                    _timeToLive = 10000.0f;

                _timeCreated = World.Ticks;
            }
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            if (FadeOut)
                hue = RenderExtentions.GetHueVector(hue.HasValue ? (int) hue.Value.X : 0, false, _alpha, false);
        
            _gText.Draw(spriteBatch, position, hue);
            return base.Draw(spriteBatch, position, hue);
        }
    }
}