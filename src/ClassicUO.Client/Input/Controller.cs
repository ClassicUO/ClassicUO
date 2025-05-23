using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SDL2;

namespace ClassicUO.Input
{
    internal static class Controller
    {
        public static bool Button_A { get; private set; }
        public static bool Button_B { get; private set; }
        public static bool Button_X { get; private set; }
        public static bool Button_Y { get; private set; }

        public static bool Button_Left { get; private set; }
        public static bool Button_Right { get; private set; }
        public static bool Button_Up { get; private set; }
        public static bool Button_Down { get; private set; }

        public static bool Button_LeftTrigger { get; private set; }
        public static bool Button_LeftBumper { get; private set; }

        public static bool Button_RightTrigger { get; private set; }
        public static bool Button_RightBumper { get; private set; }

        public static bool Button_LeftStick { get; private set; }
        public static bool Button_RightStick { get; private set; }

        public static Dictionary<SDL.SDL_GameControllerButton, bool> ButtonStates = new Dictionary<SDL.SDL_GameControllerButton, bool>();

        public static void OnButtonDown(SDL.SDL_ControllerButtonEvent e)
        {
            SetButtonState((SDL.SDL_GameControllerButton)e.button, true);
        }

        public static void OnButtonUp(SDL.SDL_ControllerButtonEvent e)
        {
            SetButtonState((SDL.SDL_GameControllerButton)e.button, false);
        }

        private static void SetButtonState(SDL.SDL_GameControllerButton button, bool state)
        {
            ButtonStates[button] = state;

            switch (button)
            {
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A:
                    Button_A = state;
                    break;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B:
                    Button_B = state;
                    break;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X:
                    Button_X = state;
                    break;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y:
                    Button_Y = state;
                    break;

                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT:
                    Button_Left = state;
                    break;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT:
                    Button_Right = state;
                    break;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP:
                    Button_Up = state;
                    break;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN:
                    Button_Down = state;
                    break;

                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER:
                    Button_LeftBumper = state;
                    break;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER:
                    Button_RightBumper = state;
                    break;

                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK:
                    Button_LeftTrigger = state;
                    break;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_GUIDE:
                    Button_RightTrigger = state;
                    break;

                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK:
                    Button_LeftStick = state;
                    break;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK:
                    Button_RightStick = state;
                    break;
            }
        }

        public static bool IsButtonPressed(SDL.SDL_GameControllerButton button)
        {
            return ButtonStates.ContainsKey(button) && ButtonStates[button];
        }

        /// <summary>
        /// Check is the supplied list of buttons are currently pressed.
        /// </summary>
        /// <param name="buttons"></param>
        /// <param name="exact">If true, any other buttons pressed will make this return false</param>
        /// <returns></returns>
        public static bool AreButtonsPressed(SDL.SDL_GameControllerButton[] buttons, bool exact = true)
        {
            bool finalstatus = true;

            foreach (var button in buttons)
            {
                if (!IsButtonPressed(button))
                {
                    finalstatus = false;
                    break;
                }
            }

            if (exact)
            {
                var allPressed = PressedButtons();

                if (allPressed.Length > buttons.Length)
                {
                    finalstatus = false;
                }
            }

            return finalstatus;
        }

        public static SDL.SDL_GameControllerButton[] PressedButtons() => ButtonStates.Where(x => x.Value).Select(x => x.Key).ToArray();

        public static string GetButtonNames(SDL.SDL_GameControllerButton[] buttons)
        {
            string keys = string.Empty;

            foreach (var button in buttons)
            {
                switch (button)
                {
                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A:
                        keys += "A";
                        break;
                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B:
                        keys += "B";
                        break;
                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X:
                        keys += "X";
                        break;
                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y:
                        keys += "Y";
                        break;

                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT:
                        keys += "Left";
                        break;
                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT:
                        keys += "Right";
                        break;
                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP:
                        keys += "Up";
                        break;
                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN:
                        keys += "Down";
                        break;


                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER:
                        keys += "LB";
                        break;
                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER:
                        keys += "RB";
                        break;


                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK:
                        keys += "LT";
                        break;
                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_GUIDE:
                        keys += "RT";
                        break;

                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK:
                        keys += "LS";
                        break;
                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK:
                        keys += "RS";
                        break;
                }

                keys += ", ";
            }

            if (keys.EndsWith(", "))
            {
                keys = keys.Substring(0, keys.Length - 2);
            }

            return keys;
        }

        public static GamePadState GetGamePadState => GamePad.GetState(PlayerIndex.One);
    }
}
