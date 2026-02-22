#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.

#endregion

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps.Login
{
    internal static class LoginLayoutHelper
    {
        public const int ContentWidth = 1024;
        public const int ContentHeight = 768;

        public static int WindowWidth => ContentWidth;
        public static int WindowHeight => ContentHeight;

        public static int ContentOffsetX => 0;
        public static int ContentOffsetY => 0;

        public static int CenterX => ContentWidth >> 1;
        public static int CenterY => ContentHeight >> 1;

        public static int X(int refX) => refX;
        public static int Y(int refY) => refY;
        public static Point Pos(int refX, int refY) => new Point(refX, refY);

        public static int W(int refW) => refW;
        public static int H(int refH) => refH;
        public static Point Size(int refW, int refH) => new Point(refW, refH);

        public static int CenterOffsetX(int controlWidth) => CenterX - (controlWidth >> 1);
        public static int CenterOffsetY(int controlHeight) => CenterY - (controlHeight >> 1);
    }
}
