// SPDX-License-Identifier: BSD-2-Clause

using Microsoft.Xna.Framework;
using SDL3;

namespace ClassicUO.Input
{
    internal static class Mouse
    {
        public const int MOUSE_DELAY_DOUBLE_CLICK = 350;

        /* Log a button press event at the given time. */
        public static void ButtonPress(MouseButtonType type)
        {
            CancelDoubleClick = false;

            switch (type)
            {
                case MouseButtonType.Left:
                    LButtonPressed = true;
                    LClickPosition = Position;

                    break;

                case MouseButtonType.Middle:
                    MButtonPressed = true;
                    MClickPosition = Position;

                    break;

                case MouseButtonType.Right:
                    RButtonPressed = true;
                    RClickPosition = Position;

                    break;

                case MouseButtonType.XButton1:
                case MouseButtonType.XButton2:
                    XButtonPressed = true;

                    break;
            }

            SDL.SDL_CaptureMouse(true);
        }

        /* Log a button release event at the given time */
        public static void ButtonRelease(MouseButtonType type)
        {
            switch (type)
            {
                case MouseButtonType.Left:
                    LButtonPressed = false;

                    break;

                case MouseButtonType.Middle:
                    MButtonPressed = false;

                    break;

                case MouseButtonType.Right:
                    RButtonPressed = false;

                    break;

                case MouseButtonType.XButton1:
                case MouseButtonType.XButton2:
                    XButtonPressed = false;

                    break;
            }

            if (!(LButtonPressed || RButtonPressed || MButtonPressed))
            {
                SDL.SDL_CaptureMouse(false);
            }
        }

        public static Point Position;

        public static Point LClickPosition;

        public static Point RClickPosition;

        public static Point MClickPosition;

        public static uint LastLeftButtonClickTime { get; set; }

        public static uint LastMidButtonClickTime { get; set; }

        public static uint LastRightButtonClickTime { get; set; }

        public static bool CancelDoubleClick { get; set; }

        public static bool LButtonPressed { get; set; }

        public static bool RButtonPressed { get; set; }

        public static bool MButtonPressed { get; set; }

        public static bool XButtonPressed { get; set; }

        public static bool IsDragging { get; set; }

        public static Point LDragOffset => LButtonPressed ? Position - LClickPosition : Point.Zero;

        public static Point RDragOffset => RButtonPressed ? Position - RClickPosition : Point.Zero;

        public static Point MDragOffset => MButtonPressed ? Position - MClickPosition : Point.Zero;

        public static bool MouseInWindow { get; set; }

#if AGENT_BUILD
        // Agent dev-loop input synthesis. When enabled, Update() skips
        // the SDL polling path and uses the synthetic coordinates instead.
        // Button state is set directly on LButtonPressed / RButtonPressed /
        // MButtonPressed via AgentSetSyntheticButtons before the agent's
        // per-frame drain runs Mouse.Update().
        private static bool _agentSynthEnabled;
        private static int _agentSynthX, _agentSynthY;

        internal static void AgentSetSyntheticPosition(int x, int y)
        {
            _agentSynthEnabled = true;
            _agentSynthX = x;
            _agentSynthY = y;
        }

        internal static void AgentSetSyntheticButtons(bool left, bool middle, bool right)
        {
            _agentSynthEnabled = true;
            LButtonPressed = left;
            MButtonPressed = middle;
            RButtonPressed = right;
        }

        internal static void AgentClearSynthetic() => _agentSynthEnabled = false;

        internal static bool AgentSyntheticActive => _agentSynthEnabled;
#endif

        public static void Update()
        {
#if AGENT_BUILD
            if (_agentSynthEnabled)
            {
                Position.X = _agentSynthX;
                Position.Y = _agentSynthY;
                IsDragging = LButtonPressed || RButtonPressed || MButtonPressed;
                return;
            }
#endif

            if (!MouseInWindow)
            {
                SDL.SDL_GetGlobalMouseState(out float x, out float y);
                SDL.SDL_GetWindowPosition(Client.Game.Window.Handle, out int winX, out int winY);
                Position.X = (int)x - winX;
                Position.Y = (int)y - winY;
            }
            else
            {
                SDL.SDL_GetMouseState(out float x, out float y);
                Position.X = (int)x;
                Position.Y = (int)y;
            }

            // Scale the mouse coordinates for the faux-backbuffer and DPI settings
            Position.X = (int) ((double) Position.X * (Client.Game.GraphicManager.PreferredBackBufferWidth / Client.Game.Window.ClientBounds.Width) / Client.Game.DpiScale);

            Position.Y = (int) ((double) Position.Y * (Client.Game.GraphicManager.PreferredBackBufferHeight / Client.Game.Window.ClientBounds.Height) / Client.Game.DpiScale);

            IsDragging = LButtonPressed || RButtonPressed || MButtonPressed;
        }
    }
}