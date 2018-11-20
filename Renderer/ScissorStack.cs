using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    internal static class ScissorStack
    {
        private static readonly Stack<Rectangle> _scissors = new Stack<Rectangle>();

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
            Service.Get<SpriteBatchUI>().GraphicsDevice.ScissorRectangle = scissor;

            return true;
        }

        public static Rectangle PopScissors()
        {
            Rectangle scissors = _scissors.Pop();
            GraphicsDevice gd = Service.Get<SpriteBatchUI>().GraphicsDevice;

            if (_scissors.Count == 0)
                gd.ScissorRectangle = gd.Viewport.Bounds;
            else
                gd.ScissorRectangle = _scissors.Peek();

            return scissors;
        }

        public static Rectangle CalculateScissors(Matrix batchTransform, Rectangle scissors)
        {
            Vector2 tmp = new Vector2(scissors.X, scissors.Y);
            tmp = Vector2.Transform(tmp, batchTransform);

            Rectangle newScissor = new Rectangle
            {
                X = (int) tmp.X, Y = (int) tmp.Y
            };
            tmp.X = scissors.X + scissors.Width;
            tmp.Y = scissors.Y + scissors.Height;
            tmp = Vector2.Transform(tmp, batchTransform);
            newScissor.Width = (int) tmp.X - newScissor.X;
            newScissor.Height = (int) tmp.Y - newScissor.Y;

            return newScissor;
        }
    }
}