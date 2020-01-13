#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    internal static class ScissorStack
    {
        private static readonly Stack<Rectangle> _scissors = new Stack<Rectangle>();

        public static bool HasScissors => _scissors.Count - 1 > 0;

        public static bool PushScissors(Rectangle scissor)
        {
            if (_scissors.Count > 0)
            {
                Rectangle parent = _scissors.Peek();
                int minX = Math.Max(parent.X, scissor.X);
                int maxX = Math.Min(parent.X + parent.Width, scissor.X + scissor.Width);

                if (maxX - minX < 1)
                    return false;

                int minY = Math.Max(parent.Y, scissor.Y);
                int maxY = Math.Min(parent.Y + parent.Height, scissor.Y + scissor.Height);

                if (maxY - minY < 1)
                    return false;

                scissor.X = minX;
                scissor.Y = minY;
                scissor.Width = maxX - minX;
                scissor.Height = Math.Max(1, maxY - minY);
            }

            _scissors.Push(scissor);
            Client.Game.GraphicsDevice.ScissorRectangle = scissor;

            return true;
        }

        public static Rectangle PopScissors()
        {
            Rectangle scissors = _scissors.Pop();
            GraphicsDevice gd = Client.Game.GraphicsDevice;

            if (_scissors.Count == 0)
                gd.ScissorRectangle = gd.Viewport.Bounds;
            else
                gd.ScissorRectangle = _scissors.Peek();

            return scissors;
        }

        public static Rectangle CalculateScissors(Matrix batchTransform, int sx, int sy, int sw, int sh)
        {
            Vector2 tmp = new Vector2(sx, sy);
            Vector2.Transform(ref tmp, ref batchTransform, out tmp);

            Rectangle newScissor = new Rectangle
            {
                X = (int) tmp.X, Y = (int) tmp.Y
            };
            tmp.X = sx + sw;
            tmp.Y = sy + sh;
            Vector2.Transform(ref tmp, ref batchTransform, out tmp);
            newScissor.Width = (int) tmp.X - newScissor.X;
            newScissor.Height = (int) tmp.Y - newScissor.Y;

            return newScissor;
        }
    }
}