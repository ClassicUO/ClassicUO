// SPDX-License-Identifier: BSD-2-Clause

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace ClassicUO.Renderer
{
    internal static class ScissorStack
    {
        private static readonly Stack<Rectangle> _scissors = new Stack<Rectangle>();

        public static bool HasScissors => _scissors.Count - 1 > 0;

        public static bool PushScissors(GraphicsDevice device, Rectangle scissor)
        {
            if (!TryIntersect(ref scissor, device.Viewport.Bounds))
            {
                return false;
            }

            if (_scissors.Count > 0)
            {
                Rectangle parent = _scissors.Peek();
                if (!TryIntersect(ref scissor, parent))
                {
                    return false;
                }
            }

            _scissors.Push(scissor);
            device.ScissorRectangle = scissor;

            return true;
        }

        public static Rectangle PopScissors(GraphicsDevice device)
        {
            Rectangle scissors = _scissors.Pop();

            if (_scissors.Count == 0)
            {
                device.ScissorRectangle = device.Viewport.Bounds;
            }
            else
            {
                Rectangle parent = _scissors.Peek();
                device.ScissorRectangle = TryIntersect(ref parent, device.Viewport.Bounds)
                    ? parent
                    : device.Viewport.Bounds;
            }

            return scissors;
        }

        private static bool TryIntersect(ref Rectangle rectangle, Rectangle bounds)
        {
            int minX = Math.Max(rectangle.X, bounds.X);
            int maxX = Math.Min(rectangle.Right, bounds.Right);
            int minY = Math.Max(rectangle.Y, bounds.Y);
            int maxY = Math.Min(rectangle.Bottom, bounds.Bottom);

            if (maxX <= minX || maxY <= minY)
            {
                return false;
            }

            rectangle = new Rectangle(minX, minY, maxX - minX, maxY - minY);

            return true;
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