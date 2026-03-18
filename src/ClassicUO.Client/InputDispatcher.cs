// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Utility.Logging;
using System;
using static SDL3.SDL;

namespace ClassicUO
{
    /// <summary>
    /// Handles SDL event dispatch: keyboard, mouse, and window events routed to scenes and UI.
    /// </summary>
    internal sealed unsafe class InputDispatcher
    {
        private readonly GameController _game;
        private bool _ignoreNextTextInput;
        private bool _pluginsReady;

        public InputDispatcher(GameController game)
        {
            _game = game;
        }

        public void SetPluginsReady()
        {
            _pluginsReady = true;
        }

        public bool HandleSdlEvent(IntPtr userData, SDL_Event* sdlEvent)
        {
            if (_pluginsReady && Plugin.ProcessWndProc(sdlEvent) != 0)
            {
                if ((SDL_EventType)sdlEvent->type == SDL_EventType.SDL_EVENT_MOUSE_MOTION)
                {
                    if (_game.UO.GameCursor != null)
                    {
                        _game.UO.GameCursor.AllowDrawSDLCursor = false;
                    }
                }

                return true;
            }

            switch ((SDL_EventType)sdlEvent->type)
            {
                case SDL_EventType.SDL_EVENT_AUDIO_DEVICE_ADDED:
                    Console.WriteLine("AUDIO ADDED: {0}", sdlEvent->adevice.which);
                    break;

                case SDL_EventType.SDL_EVENT_AUDIO_DEVICE_REMOVED:
                    Console.WriteLine("AUDIO REMOVED: {0}", sdlEvent->adevice.which);
                    break;

                case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_ENTER:
                    Mouse.MouseInWindow = true;
                    break;

                case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE:
                    Mouse.MouseInWindow = false;
                    break;

                case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED:
                    Plugin.OnFocusGained();
                    break;

                case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST:
                    Plugin.OnFocusLost();
                    break;

                case SDL_EventType.SDL_EVENT_KEY_DOWN:
                    HandleKeyDown(sdlEvent);
                    break;

                case SDL_EventType.SDL_EVENT_KEY_UP:
                    HandleKeyUp(sdlEvent);
                    break;

                case SDL_EventType.SDL_EVENT_TEXT_INPUT:
                    HandleTextInput(sdlEvent);
                    break;

                case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
                    HandleMouseMotion();
                    break;

                case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
                    HandleMouseWheel(sdlEvent);
                    break;

                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
                    HandleMouseButtonDown(sdlEvent);
                    break;

                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
                    HandleMouseButtonUp(sdlEvent);
                    break;

                case SDL_EventType.SDL_EVENT_WINDOW_DISPLAY_SCALE_CHANGED:
                case SDL_EventType.SDL_EVENT_WINDOW_DISPLAY_CHANGED:
                    HandleDisplayChanged();
                    break;
            }

            return true;
        }

        private void HandleKeyDown(SDL_Event* sdlEvent)
        {
            Keyboard.OnKeyDown(sdlEvent->key);

            if (Plugin.ProcessHotkeys((int)sdlEvent->key.key, (int)sdlEvent->key.mod, true))
            {
                _ignoreNextTextInput = false;

                _game.UI?.KeyboardFocusControl?.InvokeKeyDown(
                    (SDL_Keycode)sdlEvent->key.key,
                    sdlEvent->key.mod
                );

                _game.Scene.OnKeyDown(sdlEvent->key);
            }
            else
            {
                _ignoreNextTextInput = true;
            }
        }

        private void HandleKeyUp(SDL_Event* sdlEvent)
        {
            Keyboard.OnKeyUp(sdlEvent->key);
            _game.UI?.KeyboardFocusControl?.InvokeKeyUp(
                (SDL_Keycode)sdlEvent->key.key,
                sdlEvent->key.mod
            );
            _game.Scene.OnKeyUp(sdlEvent->key);
            Plugin.ProcessHotkeys(0, 0, false);

            if ((SDL_Keycode)sdlEvent->key.key == SDL_Keycode.SDLK_PRINTSCREEN)
            {
                _game.TakeScreenshot();
            }
        }

        private void HandleTextInput(SDL_Event* sdlEvent)
        {
            if (_ignoreNextTextInput)
                return;

            byte* ptr = sdlEvent->text.text;
            while (*ptr != 0)
            {
                ptr++;
            }

            string s = System.Text.Encoding.UTF8.GetString(
                sdlEvent->text.text,
                (int)(ptr - sdlEvent->text.text)
            );

            if (!string.IsNullOrEmpty(s))
            {
                _game.UI?.KeyboardFocusControl?.InvokeTextInput(s);
                _game.Scene.OnTextInput(s);
            }
        }

        private void HandleMouseMotion()
        {
            if (_game.UO.GameCursor != null && !_game.UO.GameCursor.AllowDrawSDLCursor)
            {
                _game.UO.GameCursor.AllowDrawSDLCursor = true;
                _game.UO.GameCursor.Graphic = 0xFFFF;
            }

            UpdateMouse();

            if (Mouse.IsDragging)
            {
                if (!_game.Scene.OnMouseDragging())
                {
                    _game.UI?.OnMouseDragging();
                }
            }
        }

        private void HandleMouseWheel(SDL_Event* sdlEvent)
        {
            UpdateMouse();
            bool isScrolledUp = sdlEvent->wheel.y > 0;

            Plugin.ProcessMouse(0, (int)sdlEvent->wheel.y);

            if (!_game.Scene.OnMouseWheel(isScrolledUp))
            {
                _game.UI?.OnMouseWheel(isScrolledUp);
            }
        }

        private void HandleMouseButtonDown(SDL_Event* sdlEvent)
        {
            SDL_MouseButtonEvent mouse = sdlEvent->button;
            MouseButtonType buttonType = (MouseButtonType)mouse.button;

            uint lastClickTime = buttonType switch
            {
                MouseButtonType.Left => Mouse.LastLeftButtonClickTime,
                MouseButtonType.Middle => Mouse.LastMidButtonClickTime,
                MouseButtonType.Right => Mouse.LastRightButtonClickTime,
                MouseButtonType.XButton1 or MouseButtonType.XButton2 => 0,
                _ => 0
            };

            if (buttonType != MouseButtonType.Left && buttonType != MouseButtonType.Middle && buttonType != MouseButtonType.Right
                && buttonType != MouseButtonType.XButton1 && buttonType != MouseButtonType.XButton2)
            {
                Log.Warn($"No mouse button handled: {mouse.button}");
            }

            Mouse.ButtonPress(buttonType);
            UpdateMouse();

            uint ticks = Time.Ticks;

            if (lastClickTime + Mouse.MOUSE_DELAY_DOUBLE_CLICK >= ticks)
            {
                lastClickTime = 0;

                bool res =
                    _game.Scene.OnMouseDoubleClick(buttonType)
                    || (_game.UI?.OnMouseDoubleClick(buttonType) ?? false);

                if (!res)
                {
                    if (!_game.Scene.OnMouseDown(buttonType))
                    {
                        _game.UI?.OnMouseButtonDown(buttonType);
                    }
                }
                else
                {
                    lastClickTime = 0xFFFF_FFFF;
                }
            }
            else
            {
                if (buttonType != MouseButtonType.Left && buttonType != MouseButtonType.Right)
                {
                    Plugin.ProcessMouse(sdlEvent->button.button, 0);
                }

                if (!_game.Scene.OnMouseDown(buttonType))
                {
                    _game.UI?.OnMouseButtonDown(buttonType);
                }

                lastClickTime = Mouse.CancelDoubleClick ? 0 : ticks;
            }

            switch (buttonType)
            {
                case MouseButtonType.Left:
                    Mouse.LastLeftButtonClickTime = lastClickTime;
                    break;
                case MouseButtonType.Middle:
                    Mouse.LastMidButtonClickTime = lastClickTime;
                    break;
                case MouseButtonType.Right:
                    Mouse.LastRightButtonClickTime = lastClickTime;
                    break;
            }
        }

        private void HandleMouseButtonUp(SDL_Event* sdlEvent)
        {
            SDL_MouseButtonEvent mouse = sdlEvent->button;
            MouseButtonType buttonType = (MouseButtonType)mouse.button;

            uint lastClickTime = buttonType switch
            {
                MouseButtonType.Left => Mouse.LastLeftButtonClickTime,
                MouseButtonType.Middle => Mouse.LastMidButtonClickTime,
                MouseButtonType.Right => Mouse.LastRightButtonClickTime,
                _ => 0
            };

            if (buttonType != MouseButtonType.Left && buttonType != MouseButtonType.Middle && buttonType != MouseButtonType.Right)
            {
                Log.Warn($"No mouse button handled: {mouse.button}");
            }

            if (lastClickTime != 0xFFFF_FFFF)
            {
                if (!_game.Scene.OnMouseUp(buttonType)
                    || _game.UI?.LastControlMouseDown(buttonType) != null)
                {
                    _game.UI?.OnMouseButtonUp(buttonType);
                }
            }

            Mouse.ButtonRelease(buttonType);
            UpdateMouse();
        }

        private void HandleDisplayChanged()
        {
            float displayScale = _game.DisplayScale;
            if (displayScale != 0 && displayScale != _game.DpiScale)
            {
                _game.WindowManager.OnClientSizeChanged(
                    _game.ScaleWithDpi(_game.Window.ClientBounds.Width, previousDpi: displayScale),
                    _game.ScaleWithDpi(_game.Window.ClientBounds.Height, previousDpi: displayScale)
                );

                SDL_GetWindowMinimumSize(_game.Window.Handle, out int previousMinWidth, out int previousMinHeight);

                SDL_SetWindowMinimumSize(
                    _game.Window.Handle,
                    _game.ScaleWithDpi(previousMinWidth, previousDpi: displayScale),
                    _game.ScaleWithDpi(previousMinHeight, previousDpi: displayScale)
                );

                _game.DisplayScale = _game.DpiScale;
            }
        }

        private void UpdateMouse()
        {
            Mouse.Update(
                _game.Window.Handle,
                _game.GraphicManager.PreferredBackBufferWidth,
                _game.GraphicManager.PreferredBackBufferHeight,
                _game.Window.ClientBounds.Width,
                _game.Window.ClientBounds.Height,
                _game.DpiScale
            );
        }
    }
}
