#region license
//  Copyright (C) 2019 ClassicUO Development Community on Github
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

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class HitBox : Control
    {
        protected readonly SpriteTexture _texture;

        public HitBox(int x, int y, int w, int h)
        {
            CanMove = false;
            AcceptMouseInput = true;
            Alpha = 0.75f;
            IsTransparent = true;
            _texture = new SpriteTexture(1, 1);

            _texture.SetData(new uint[1]
            {
                0xFFFF_FFFF
            });
            X = x;
            Y = y;
            Width = w;
            Height = h;
            WantUpdateSize = false;
        }


        protected override ClickPriority Priority { get; } = ClickPriority.High;

        public override void Update(double totalMS, double frameMS)
        {
            if (IsDisposed)
                return;
            base.Update(totalMS, frameMS);
            _texture.Ticks = (long) totalMS;
        }

        public override bool Draw(Batcher2D batcher, Point position)
        {
            if (IsDisposed)
                return false;

            if (MouseIsOver)
                batcher.Draw2D(_texture, position, new Rectangle(0, 0, Width, Height), ShaderHuesTraslator.GetHueVector(0, false, IsTransparent ? Alpha : 0, false));
            
            return base.Draw(batcher, position);
        }

        public override void Dispose()
        {
            base.Dispose();
            _texture?.Dispose();
        }
    }
}