// SPDX-License-Identifier: BSD-2-Clause

using System;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Data
{
    [Flags]
    internal enum Direction : byte
    {
        North = 0x00,
        Right = 0x01,
        East = 0x02,
        Down = 0x03,
        South = 0x04,
        Left = 0x05,
        West = 0x06,
        Up = 0x07,
        Mask = 0x7,
        Running = 0x80,
        NONE = 0xED
    }

    internal static class DirectionHelper
    {
        public static Direction DirectionFromPoints(Point from, Point to)
        {
            return DirectionFromVectors(new Vector2(from.X, from.Y), new Vector2(to.X, to.Y));
        }

        public static Direction DirectionFromVectors(Vector2 fromPosition, Vector2 toPosition)
        {
            double Angle = Math.Atan2(toPosition.Y - fromPosition.Y, toPosition.X - fromPosition.X);

            if (Angle < 0)
            {
                Angle = Math.PI + (Math.PI + Angle);
            }

            double piPerSegment = Math.PI * 2f / 8f;
            double segmentValue = Math.PI * 2f / 16f;
            int direction = int.MaxValue;

            for (int i = 0; i < 8; i++)
            {
                if (Angle >= segmentValue && Angle <= segmentValue + piPerSegment)
                {
                    direction = i + 1;

                    break;
                }

                segmentValue += piPerSegment;
            }

            if (direction == int.MaxValue)
            {
                direction = 0;
            }

            direction = direction >= 7 ? direction - 7 : direction + 1;

            return (Direction) direction;
        }

        public static Direction GetDirectionAB(int AAx, int AAy, int BBx, int BBy)
        {
            int dx = AAx - BBx;
            int dy = AAy - BBy;

            int rx = (dx - dy) * 44;
            int ry = (dx + dy) * 44;

            int ax = Math.Abs(rx);
            int ay = Math.Abs(ry);

            Direction ret;

            if ((ay >> 1) - ax >= 0)
            {
                ret = ry > 0 ? Direction.Up : Direction.Down;
            }
            else if ((ax >> 1) - ay >= 0)
            {
                ret = rx > 0 ? Direction.Left : Direction.Right;
            }
            else if (rx >= 0 && ry >= 0)
            {
                ret = Direction.West;
            }
            else if (rx >= 0 && ry < 0)
            {
                ret = Direction.South;
            }
            else if (rx < 0 && ry < 0)
            {
                ret = Direction.East;
            }
            else
            {
                ret = Direction.North;
            }

            return ret;
        }

        public static Direction CalculateDirection(int curX, int curY, int newX, int newY)
        {
            int deltaX = newX - curX;
            int deltaY = newY - curY;

            if (deltaX > 0)
            {
                if (deltaY > 0)
                {
                    return Direction.Down;
                }

                return deltaY == 0 ? Direction.East : Direction.Right;
            }

            if (deltaX == 0)
            {
                if (deltaY > 0)
                {
                    return Direction.South;
                }

                return deltaY == 0 ? Direction.NONE : Direction.North;
            }

            if (deltaY > 0)
            {
                return Direction.Left;
            }

            return deltaY == 0 ? Direction.West : Direction.Up;
        }

        public static Direction DirectionFromKeyboardArrows(bool upPressed, bool downPressed, bool leftPressed, bool rightPressed)
        {
            int direction = (int) Direction.NONE;

            if (upPressed)
            {
                if (leftPressed)
                {
                    direction = 6;
                }
                else if (rightPressed)
                {
                    direction = 0;
                }
                else
                {
                    direction = 7;
                }
            }
            else if (downPressed)
            {
                if (leftPressed)
                {
                    direction = 4;
                }
                else if (rightPressed)
                {
                    direction = 2;
                }
                else
                {
                    direction = 3;
                }
            }
            else if (leftPressed)
            {
                direction = 5;
            }
            else if (rightPressed)
            {
                direction = 1;
            }

            return (Direction) direction;
        }

        public static Direction GetCardinal(Direction inDirection)
        {
            return inDirection & (Direction) 0x6;
        }

        public static Direction Reverse(Direction inDirection)
        {
            return (Direction) ((int) inDirection + 0x04) & Direction.Up;
        }
    }
}