using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using SDL2;
using static SDL2.SDL;

namespace ClassicUO
{
    class GameController : Microsoft.Xna.Framework.Game
    {
        private Scene _scene;
        private bool _dragStarted;
        private bool _ignoreNextTextInput;
        private readonly GraphicsDeviceManager _graphicDeviceManager;
        private readonly UltimaBatcher2D _uoSpriteBatch;

        public GameController(string[] args)
        {
            _graphicDeviceManager = new GraphicsDeviceManager(this);
            _uoSpriteBatch = new UltimaBatcher2D(GraphicsDevice);
        }

        protected override void Initialize()
        {
            Log.Trace("Setup GraphicDeviceManager");

            _graphicDeviceManager.PreparingDeviceSettings += (sender, e) => e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.DiscardContents;
            if (_graphicDeviceManager.GraphicsDevice.Adapter.IsProfileSupported(GraphicsProfile.HiDef))
                _graphicDeviceManager.GraphicsProfile = GraphicsProfile.HiDef;

            _graphicDeviceManager.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
            _graphicDeviceManager.SynchronizeWithVerticalRetrace = false; // TODO: V-Sync option
            _graphicDeviceManager.ApplyChanges();

            Window.ClientSizeChanged += WindowOnClientSizeChanged;
            Window.AllowUserResizing = true;
            Window.Title = $"ClassicUO - {CUOEnviroment.Version}";


            SetRefreshRate(CUOEnviroment.RefreshRate);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            LoadGameFilesFromFileSystem();
            base.LoadContent();
        }


        public Scene Scene => _scene;
        
        public void SetScene(Scene scene)
        {
            _scene?.Destroy();
            _scene = scene;

            if (scene != null)
            {
                if (scene.IsMaximized)
                    MaximizeWindow();
                else 
                    RestoreWindow();

                SetWindowSize(scene.Width, scene.Height);
                Window.AllowUserResizing = scene.CanResize;

                scene.Load();
            }
        }

        public void SetRefreshRate(uint rate)
        {
            TargetElapsedTime = TimeSpan.FromMilliseconds(1.0f / rate);
        }

        public void SetWindowPosition(int x, int y)
        {
            SDL.SDL_SetWindowPosition(Window.Handle, x, y);
        }

        public void SetWindowSize(int width, int height)
        {
            _graphicDeviceManager.PreferredBackBufferWidth = width;
            _graphicDeviceManager.PreferredBackBufferHeight = height;
            _graphicDeviceManager.ApplyChanges();
        }

        public void MaximizeWindow()
        {
            SDL.SDL_MaximizeWindow(Window.Handle);
        }

        public void RestoreWindow()
        {
            SDL.SDL_RestoreWindow(Window.Handle);
        }

        public void LoadGameFilesFromFileSystem()
        {

        }

        protected override void Update(GameTime gameTime)
        {
            Time.Ticks = (uint) gameTime.TotalGameTime.TotalMilliseconds;

            _scene?.Update(gameTime.TotalGameTime.TotalMilliseconds, gameTime.ElapsedGameTime.TotalMilliseconds);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            _scene?.Draw(_uoSpriteBatch);
            base.Draw(gameTime);
        }

        public override void OnSDLEvent(ref SDL_Event ev)
        {
            HandleSDLEvent(ref ev);
            base.OnSDLEvent(ref ev);
        }

        private void WindowOnClientSizeChanged(object sender, EventArgs e)
        {
            int width = Window.ClientBounds.Width;
            int height = Window.ClientBounds.Height;

            if (CUOEnviroment.IsHighDPI)
            {

            }

            uint flags = SDL.SDL_GetWindowFlags(Window.Handle);
            if ((flags & (uint) SDL.SDL_WindowFlags.SDL_WINDOW_MAXIMIZED) == 0)
            {
                // TODO: option set WindowClientBounds
            }

            SetWindowSize(width, height);
        }

        private unsafe void HandleSDLEvent(ref SDL.SDL_Event e)
        {
            switch (e.type)
            {
                case SDL.SDL_EventType.SDL_AUDIODEVICEADDED:
                    Console.WriteLine("AUDIO ADDED: {0}", e.adevice.which);

                    break;

                case SDL.SDL_EventType.SDL_AUDIODEVICEREMOVED:
                    Console.WriteLine("AUDIO REMOVED: {0}", e.adevice.which);

                    break;


                case SDL.SDL_EventType.SDL_WINDOWEVENT:

                    switch (e.window.windowEvent)
                    {
                        case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_ENTER:
                            Mouse.MouseInWindow = true;

                            break;

                        case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE:
                            Mouse.MouseInWindow = false;

                            break;

                        case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED:
                            Plugin.OnFocusGained();

                            // SDL_CaptureMouse(SDL_bool.SDL_TRUE);
                            //Log.Message(LogTypes.Debug, "FOCUS");
                            break;

                        case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST:
                            Plugin.OnFocusLost();
                            //Log.Message(LogTypes.Debug, "NO FOCUS");
                            //SDL_CaptureMouse(SDL_bool.SDL_FALSE);

                            break;

                        case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_TAKE_FOCUS:

                            //Log.Message(LogTypes.Debug, "TAKE FOCUS");
                            break;

                        case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_HIT_TEST:

                            break;
                    }

                    break;

                case SDL.SDL_EventType.SDL_SYSWMEVENT:

                    break;

                case SDL.SDL_EventType.SDL_KEYDOWN:

                    if (Plugin.ProcessHotkeys((int) e.key.keysym.sym, (int) e.key.keysym.mod, true))
                    {
                        _ignoreNextTextInput = false;
                        _scene.OnKeyDown(e.key);
                    }
                    else
                        _ignoreNextTextInput = true;

                    break;

                case SDL.SDL_EventType.SDL_KEYUP:

                    _scene.OnKeyUp(e.key);

                    break;

                case SDL.SDL_EventType.SDL_TEXTINPUT:

                    if (_ignoreNextTextInput)
                        break;

                    fixed (SDL.SDL_Event* ev = &e)
                    {
                        string s = StringHelper.ReadUTF8(ev->text.text);

                        if (!string.IsNullOrEmpty(s))
                        {
                            _scene.OnTextInput(s);
                        }

                    }

                    break;

                case SDL.SDL_EventType.SDL_MOUSEMOTION:
                    Mouse.Update();

                    if (Mouse.IsDragging)
                    {
                        _scene.OnMouseDragging();
                    }

                    if (Mouse.IsDragging && !_dragStarted)
                    {
                        _dragStarted = true;
                    }

                    break;

                case SDL.SDL_EventType.SDL_MOUSEWHEEL:
                    Mouse.Update();
                    bool isup = e.wheel.y > 0;

                    Plugin.ProcessMouse(0, e.wheel.y);
                    _scene.OnMouseWheel(isup);

                    break;

                case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    Mouse.Update();
                    bool isDown = e.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN;
                    bool resetTime = false;

                    if (_dragStarted && !isDown)
                    {
                        _dragStarted = false;
                        resetTime = true;
                    }

                    SDL.SDL_MouseButtonEvent mouse = e.button;

                    switch ((uint) mouse.button)
                    {
                        case SDL_BUTTON_LEFT:

                            if (isDown)
                            {
                                Mouse.Begin();
                                Mouse.LButtonPressed = true;
                                Mouse.LDropPosition = Mouse.Position;
                                Mouse.CancelDoubleClick = false;
                                uint ticks = SDL_GetTicks();

                                if (Mouse.LastLeftButtonClickTime + Mouse.MOUSE_DELAY_DOUBLE_CLICK >= ticks)
                                {
                                    Mouse.LastLeftButtonClickTime = 0;

                                    var res = _scene.OnLeftMouseDoubleClick();

                                    MouseDoubleClickEventArgs arg = new MouseDoubleClickEventArgs(Mouse.Position.X, Mouse.Position.Y, MouseButton.Left);

                                    if (!arg.Result && !res)
                                    {
                                        _scene.OnLeftMouseDown();
                                    }
                                    else
                                        Mouse.LastLeftButtonClickTime = 0xFFFF_FFFF;

                                    break;
                                }

                                _scene.OnLeftMouseDown();
                                Mouse.LastLeftButtonClickTime = Mouse.CancelDoubleClick ? 0 : ticks;
                            }
                            else
                            {
                                if (resetTime)
                                    Mouse.LastLeftButtonClickTime = 0;

                                if (Mouse.LastLeftButtonClickTime != 0xFFFF_FFFF)
                                {
                                    _scene.OnLeftMouseUp();
                                }
                                Mouse.LButtonPressed = false;
                                Mouse.End();

                                Mouse.LastClickPosition = Mouse.Position;
                            }

                            break;

                        case SDL_BUTTON_MIDDLE:

                            if (isDown)
                            {
                                Mouse.Begin();
                                Mouse.MButtonPressed = true;
                                Mouse.MDropPosition = Mouse.Position;
                                Mouse.CancelDoubleClick = false;
                                uint ticks = SDL_GetTicks();

                                if (Mouse.LastMidButtonClickTime + Mouse.MOUSE_DELAY_DOUBLE_CLICK >= ticks)
                                {
                                    Mouse.LastMidButtonClickTime = 0;
                                    var res = _scene.OnMiddleMouseDoubleClick();

                                    MouseDoubleClickEventArgs arg = new MouseDoubleClickEventArgs(Mouse.Position.X, Mouse.Position.Y, MouseButton.Middle);

                                    if (!arg.Result && !res)
                                    {
                                        _scene.OnMiddleMouseDown();
                                    }

                                    break;
                                }

                                Plugin.ProcessMouse(e.button.button, 0);

                                _scene.OnMiddleMouseDown();
                                Mouse.LastMidButtonClickTime = Mouse.CancelDoubleClick ? 0 : ticks;
                            }
                            else
                            {
                                Mouse.MButtonPressed = false;
                                Mouse.End();
                            }

                            break;

                        case SDL_BUTTON_RIGHT:

                            if (isDown)
                            {
                                Mouse.Begin();
                                Mouse.RButtonPressed = true;
                                Mouse.RDropPosition = Mouse.Position;
                                Mouse.CancelDoubleClick = false;
                                uint ticks = SDL_GetTicks();

                                if (Mouse.LastRightButtonClickTime + Mouse.MOUSE_DELAY_DOUBLE_CLICK >= ticks)
                                {
                                    Mouse.LastRightButtonClickTime = 0;

                                    var res = _scene.OnRightMouseDoubleClick();

                                    MouseDoubleClickEventArgs arg = new MouseDoubleClickEventArgs(Mouse.Position.X, Mouse.Position.Y, MouseButton.Right);


                                    if (!arg.Result && !res)
                                    {
                                        _scene.OnRightMouseDown();
                                    }
                                    else
                                        Mouse.LastRightButtonClickTime = 0xFFFF_FFFF;

                                    break;
                                }

                                _scene.OnRightMouseDown();
                                Mouse.LastRightButtonClickTime = Mouse.CancelDoubleClick ? 0 : ticks;
                            }
                            else
                            {
                                if (resetTime)
                                    Mouse.LastRightButtonClickTime = 0;

                                if (Mouse.LastRightButtonClickTime != 0xFFFF_FFFF)
                                {
                                    _scene.OnRightMouseUp();
                                }
                                Mouse.RButtonPressed = false;
                                Mouse.End();
                            }

                            break;

                        case SDL_BUTTON_X1:

                            if (isDown)
                                Plugin.ProcessMouse(e.button.button, 0);

                            break;

                        case SDL_BUTTON_X2:

                            if (isDown)
                                Plugin.ProcessMouse(e.button.button, 0);

                            break;
                    }

                    break;
            }
        }
    }
}
