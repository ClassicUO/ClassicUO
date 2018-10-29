using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

using SDL2;

namespace ClassicUO.Input
{
    static class Mouse
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


        public static void Begin()
        {
            SDL2.SDL.SDL_CaptureMouse(SDL.SDL_bool.SDL_TRUE);
        }

        public static void End()
        {
            if (!(LButtonPressed || RButtonPressed || MButtonPressed))
                SDL2.SDL.SDL_CaptureMouse(SDL.SDL_bool.SDL_FALSE);
        }

        public static void Update()
        {
            SDL.SDL_GetMouseState(out _position.X, out _position.Y);

            IsDragging = LButtonPressed || RButtonPressed || MButtonPressed;
            RealPosition = Position;
        }
    }
}
