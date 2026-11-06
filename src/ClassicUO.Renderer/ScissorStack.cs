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

        /// <summary>
        ///     Pushes an empty scissor (nothing gets drawn) when the requested one does not intersect the viewport or
        ///     its parent. Keeps the stack balanced for callers that call ClipEnd regardless of ClipBegin's result
        ///     (ScissorControl).
        /// </summary>
        public static void PushEmptyScissors(GraphicsDevice device)
        {
            Rectangle viewport = device.Viewport.Bounds;
            Rectangle empty = new Rectangle(viewport.X, viewport.Y, 0, 0);

            _scissors.Push(empty);
            device.ScissorRectangle = empty;
        }

        private static uint _lastUnderflowLog;

        public static Rectangle PopScissors(GraphicsDevice device)
        {
            if (!_scissors.TryPop(out Rectangle scissors))
            {
                // Unbalanced ClipEnd: log (at most every 5 s) instead of crashing on an empty stack.
                uint now = (uint)Environment.TickCount;

                if (now - _lastUnderflowLog >= 5000)
                {
                    _lastUnderflowLog = now;
                    ClassicUO.Utility.Logging.Log.Warn("ScissorStack: PopScissors without a matching push; ignored.");
                }

                device.ScissorRectangle = device.Viewport.Bounds;

                return device.Viewport.Bounds;
            }

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