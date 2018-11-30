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
using Microsoft.Xna.Framework;

using SDL2;

namespace ClassicUO.Input
{
    internal static class Mouse
    {
        public const int MOUSE_DELAY_DOUBLE_CLICK = 350;
        private static Point _position;

        public static uint LastLeftButtonClickTime { get; set; }

        public static uint LastMidButtonClickTime { get; set; }

        public static uint LastRightButtonClickTime { get; set; }

        public static bool CancelDoubleClick { get; set; }

        public static bool LButtonPressed { get; set; }

        public static bool RButtonPressed { get; set; }

        public static bool MButtonPressed { get; set; }

        public static bool IsDragging { get; set; }

        public static Point Position => _position;

        public static Point RealPosition { get; private set; }

        public static Point LDropPosition { get; set; }

        public static Point RDropPosition { get; set; }

        public static Point MDropPosition { get; set; }

        public static Point LDroppedOffset => LButtonPressed ? RealPosition - LDropPosition : Point.Zero;

        public static Point RDroppedOffset => RButtonPressed ? RealPosition - RDropPosition : Point.Zero;

        public static Point MDroppedOffset => MButtonPressed ? RealPosition - MDropPosition : Point.Zero;

        public static bool MouseInWindow { get; set; }

        public static void Begin()
        {
            SDL.SDL_CaptureMouse(SDL.SDL_bool.SDL_TRUE);
        }

        public static void End()
        {
            if (!(LButtonPressed || RButtonPressed || MButtonPressed))
                SDL.SDL_CaptureMouse(SDL.SDL_bool.SDL_FALSE);
        }

        public static void Update()
        {
            if (!MouseInWindow)
            {
                SDL.SDL_GetGlobalMouseState(out int x, out int y);
                SDL.SDL_GetWindowPosition(Microsoft.Xna.Framework.Input.Mouse.WindowHandle, out int winX, out int winY);
                _position.X = x - winX;
                _position.Y = y - winY;
            }
            else
                SDL.SDL_GetMouseState(out _position.X, out _position.Y);

            IsDragging = LButtonPressed || RButtonPressed || MButtonPressed;
            RealPosition = Position;
        }
    }
}