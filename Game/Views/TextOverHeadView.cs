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

using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Views
{
    public class TextOverheadView : View
    {
        private RenderedText _text;

        public TextOverheadView(TextOverhead parent, int maxwidth = 0, ushort hue = 0xFFFF, byte font = 0, bool isunicode = false, FontStyle style = FontStyle.None) : base(parent)
        {
            _text = new RenderedText
            {
                MaxWidth = maxwidth,
                Hue = hue,
                Font = font,
                IsUnicode = isunicode,
                FontStyle = style,
                Text = parent.Text
            };
            Texture = _text.Texture;

            if (parent.TimeToLive <= 0.0f)
                parent.TimeToLive = (((4000 * _text.LinesCount) * Service.Get<Settings>().SpeechDelay) / 100);

            parent.Initialized = true;
        }

        public override bool Draw(SpriteBatch3D spriteBatch, Vector3 position, MouseOverList objectList)
        {
            if (!AllowedToDraw || GameObject.IsDisposed)
            {
                _text?.Dispose();
                _text = null;
                return false;
            }

            Texture.Ticks = CoreGame.Ticks;
            TextOverhead overhead = (TextOverhead) GameObject;
            if (!overhead.IsPersistent && overhead.Alpha < 1.0f)
                HueVector = RenderExtentions.GetHueVector(0, false, overhead.Alpha, true);
            Settings settings = Service.Get<Settings>();
            int width = Texture.Width - Bounds.X;
            int height = Texture.Height - Bounds.Y;

            if (position.X < Bounds.X)
                position.X = Bounds.X;
            else if (position.X > settings.GameWindowWidth - width)
                position.X = settings.GameWindowWidth - width;

            if (position.Y - Bounds.Y < 0)
                position.Y = Bounds.Y;
            else if (position.Y > settings.GameWindowHeight - height)
                position.Y = settings.GameWindowHeight - height;

            return base.Draw(spriteBatch, position, objectList);
        }

        protected override void MousePick(MouseOverList list, SpriteVertex[] vertex)
        {
        }
    }

    public class DamageOverheadView : TextOverheadView
    {
        public DamageOverheadView(DamageOverhead parent, int maxwidth = 0, ushort hue = 0xFFFF, byte font = 0, bool isunicode = false, FontStyle style = FontStyle.None) : base(parent, maxwidth, hue, font, isunicode, style)
        {

        }

        public override bool Draw(SpriteBatch3D spriteBatch, Vector3 position, MouseOverList objectList)
        {

            DamageOverhead dmg = (DamageOverhead) GameObject;

            if (dmg.MovingTime >= 50)
            {
                dmg.MovingTime = 0;
                dmg.OffsetY -= 2;
            }

            return base.Draw(spriteBatch, position, objectList);
        }
    }
}