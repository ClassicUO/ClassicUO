using SDL2;

namespace ClassicUO.Input
{
    internal static class Controller
    {
        public static bool Button_A { get; private set; } = false;
        public static bool Button_B { get; private set; } = false;
        public static bool Button_X { get; private set; } = false;
        public static bool Button_Y { get; private set; } = false;

        public static bool Button_Left { get; private set; } = false;
        public static bool Button_Right { get; private set; } = false;
        public static bool Button_Up { get; private set; } = false;
        public static bool Button_Down { get; private set; } = false;

        public static bool Button_LeftTrigger { get; private set; } = false;
        public static bool Button_LeftBumper { get; private set; } = false;

        public static bool Button_RightTrigger { get; private set; } = false;
        public static bool Button_RightBumper { get; private set; } = false;

        public static bool Button_LeftStick { get; private set; } = false;
        public static bool Button_RightStick { get; private set; } = false;



        public static void OnButtonDown(SDL.SDL_ControllerButtonEvent e)
        {
            //GameActions.Print(typeof(SDL.SDL_GameControllerButton).GetEnumName((SDL.SDL_GameControllerButton)e.button));

            switch (e.button)
            {
                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A:
                    Button_A = true;
                    break;
                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B:
                    Button_B = true;
                    break;
                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X:
                    Button_X = true;
                    break;
                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y:
                    Button_Y = true;
                    break;

                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT:
                    Button_Left = true;
                    break;
                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT:
                    Button_Right = true;
                    break;
                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP:
                    Button_Up = true;
                    break;
                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN:
                    Button_Down = true;
                    break;


                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER:
                    Button_LeftBumper = true;
                    break;
                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER:
                    Button_RightBumper = true;
                    break;


                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK:
                    Button_LeftTrigger = true;
                    break;

                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_GUIDE:
                    Button_RightTrigger = true;
                    break;

                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK:
                    Button_LeftStick = true;
                    break;
                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK:
                    Button_RightStick = true;
                    break;
            }
        }

        public static void OnButtonUp(SDL.SDL_ControllerButtonEvent e)
        {
            switch (e.button)
            {
                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A:
                    Button_A = false;
                    break;
                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B:
                    Button_B = false;
                    break;
                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X:
                    Button_X = false;
                    break;
                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y:
                    Button_Y = false;
                    break;

                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT:
                    Button_Left = false;
                    break;
                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT:
                    Button_Right = false;
                    break;
                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP:
                    Button_Up = false;
                    break;
                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN:
                    Button_Down = false;
                    break;


                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER:
                    Button_LeftBumper = false;
                    break;
                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER:
                    Button_RightBumper = false;
                    break;


                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK:
                    Button_LeftTrigger = false;
                    break;

                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_GUIDE:
                    Button_RightTrigger = false;
                    break;

                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK:
                    Button_LeftStick = false;
                    break;
                case (byte)SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK:
                    Button_RightStick = false;
                    break;
            }
        }

        public static bool IsButtonPressed(SDL.SDL_GameControllerButton button)
        {
            switch (button)
            {
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A:
                    return Button_A;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B:
                    return Button_B;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X:
                    return Button_X;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y:
                    return Button_Y;

                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT:
                    return Button_Left;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT:
                    return Button_Right;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP:
                    return Button_Up;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN:
                    return Button_Down;


                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER:
                    return Button_LeftBumper;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER:
                    return Button_RightBumper;


                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK:
                    return Button_LeftTrigger;

                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_GUIDE:
                    return Button_RightTrigger;

                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK:
                    return Button_LeftStick;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK:
                    return Button_RightStick;
            }

            return false;
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

            var allPressed = PressedButtons();

            if (exact && allPressed.Length > buttons.Length)
            {
                finalstatus = false;
            }

            return finalstatus;
        }

        public static SDL.SDL_GameControllerButton[] PressedButtons()
        {
            System.Collections.Generic.List<SDL.SDL_GameControllerButton> buttons = new System.Collections.Generic.List<SDL.SDL_GameControllerButton>();

            if (Button_A)
            {
                buttons.Add(SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A);
            }
            if (Button_B)
            {
                buttons.Add(SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B);
            }
            if (Button_X)
            {
                buttons.Add(SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X);
            }
            if (Button_Y)
            {
                buttons.Add(SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y);
            }


            if (Button_Left)
            {
                buttons.Add(SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT);
            }
            if (Button_Right)
            {
                buttons.Add(SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT);
            }
            if (Button_Up)
            {
                buttons.Add(SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP);
            }
            if (Button_Down)
            {
                buttons.Add(SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN);
            }


            if (Button_LeftTrigger)
            {
                buttons.Add(SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK);
            }
            if (Button_LeftBumper)
            {
                buttons.Add(SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER);
            }

            if (Button_RightTrigger)
            {
                buttons.Add(SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_GUIDE);
            }
            if (Button_RightBumper)
            {
                buttons.Add(SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER);
            }

            if (Button_LeftStick)
            {
                buttons.Add(SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK);
            }
            if (Button_RightStick)
            {
                buttons.Add(SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK);
            }

            return buttons.ToArray();
        }

        public static string GetButtonNames(SDL.SDL_GameControllerButton[] buttons)
        {
            string keys = string.Empty;

            foreach (var button in buttons)
            {
                switch (button)
                {
                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A:
                        keys += "A, ";
                        break;
                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B:
                        keys += "B, ";
                        break;
                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X:
                        keys += "X, ";
                        break;
                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y:
                        keys += "Y, ";
                        break;

                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT:
                        keys += "Left, ";
                        break;
                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT:
                        keys += "Right, ";
                        break;
                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP:
                        keys += "Up, ";
                        break;
                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN:
                        keys += "Down, ";
                        break;


                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER:
                        keys += "LB, ";
                        break;
                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER:
                        keys += "RB, ";
                        break;


                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK:
                        keys += "LT, ";
                        break;
                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_GUIDE:
                        keys += "RT, ";
                        break;

                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK:
                        keys += "LS, ";
                        break;
                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK:
                        keys += "RS, ";
                        break;
                }
            }

            if (keys.EndsWith(", "))
            {
                keys = keys.Substring(0, keys.Length - 2);
            }

            return keys;
        }
    }
}
