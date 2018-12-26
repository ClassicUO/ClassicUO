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

using System;

using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Views
{
    public class TextOverheadView : View
    {
        private readonly RenderedText _text;

        protected bool EdgeDetection { get; set; }

        public TextOverheadView(TextOverhead parent, int maxwidth = 0, ushort hue = 0xFFFF, byte font = 0, bool isunicode = false, FontStyle style = FontStyle.None) : base(parent)
        {
            _text = new RenderedText
            {
                MaxWidth = maxwidth,
                Hue = hue,
                Font = font,
                IsUnicode = isunicode,
                FontStyle = style,
                SaveHitMap = true,
                Text = parent.Text
            };
            Texture = _text.Texture;

            //Bounds.X = (Texture.Width >> 1) - 22;
            //Bounds.Y = Texture.Height;
            //Bounds.Width = Texture.Width;
            //Bounds.Height = Texture.Height;

            if (Engine.Profile.Current.ScaleSpeechDelay)
            {
                int delay = Engine.Profile.Current.SpeechDelay;

                if (delay < 10)
                    delay = 10;

                if (parent.TimeToLive <= 0.0f)
                    parent.TimeToLive = 4000 * _text.LinesCount * delay / 100.0f;
            }
            else
            {
                long delay = ((5497558140000 * Engine.Profile.Current.SpeechDelay) >> 32) >> 5;

                if (parent.TimeToLive <= 0.0f)
                    parent.TimeToLive = (delay >> 31) + delay;
            }


            parent.Initialized = true;

            parent.Disposed += ParentOnDisposed;
            EdgeDetection = true;
        }

        private void ParentOnDisposed(object sender, EventArgs e)
        {
            GameObject.Disposed -= ParentOnDisposed;
            _text?.Dispose();
        }

        public override bool Draw(Batcher2D batcher, Vector3 position, MouseOverList objectList)
        {
            if (!AllowedToDraw || GameObject.IsDisposed)
            {
                return false;
            }

            Texture.Ticks = Engine.Ticks;
            TextOverhead overhead = (TextOverhead) GameObject;

            HueVector = ShaderHuesTraslator.GetHueVector(0, false, overhead.Alpha, true);

            if (EdgeDetection)
            {
                GameScene gs = Engine.SceneManager.GetScene<GameScene>();

                int width = Texture.Width - Bounds.X;
                int height = Texture.Height - Bounds.Y;

                if (position.X < Bounds.X)
                    position.X = Bounds.X;
                else if (position.X > Engine.Profile.Current.GameWindowSize.X * gs.Scale - width)
                    position.X = Engine.Profile.Current.GameWindowSize.X * gs.Scale - width;

                if (position.Y - Bounds.Y < 0)
                    position.Y = Bounds.Y;
                else if (position.Y > Engine.Profile.Current.GameWindowSize.Y * gs.Scale - height)
                    position.Y = Engine.Profile.Current.GameWindowSize.Y * gs.Scale - height;
            }

            bool ok = base.Draw(batcher, position, objectList);


            //if (_edge == null)
            //{
            //    _edge = new Texture2D(batcher.GraphicsDevice, 1, 1);
            //    _edge.SetData(new Color[] { Color.LightBlue });
            //}

            //batcher.DrawRectangle(_edge, new Rectangle((int) position.X - Bounds.X, (int) position.Y - Bounds.Y, _text.Width, _text.Height) , Vector3.Zero);

            return ok;
        }

        //private static Texture2D _edge;

        protected override void MousePick(MouseOverList list, SpriteVertex[] vertex)
        {
            int x = list.MousePosition.X - (int)vertex[0].Position.X;
            int y = list.MousePosition.Y - (int)vertex[0].Position.Y;

            if (Texture.Contains(x, y))
                list.Add(GameObject, vertex[0].Position);
        }

    }

    public class DamageOverheadView : TextOverheadView
    {
        public DamageOverheadView(DamageOverhead parent, int maxwidth = 0, ushort hue = 0xFFFF, byte font = 0, bool isunicode = false, FontStyle style = FontStyle.None) : base(parent, maxwidth, hue, font, isunicode, style)
        {
            EdgeDetection = false;
        }
    }
}