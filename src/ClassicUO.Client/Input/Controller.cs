using ClassicUO.Game;
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
    }
}
