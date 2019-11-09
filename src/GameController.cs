using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using ClassicUO.Utility.Platforms;

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
        private readonly float[] _intervalFixedUpdate = new float[2];
        private double _statisticsTimer;

        public GameController()
        {
            _graphicDeviceManager = new GraphicsDeviceManager(this);
            _uoSpriteBatch = new UltimaBatcher2D(GraphicsDevice);
        }

        public Scene Scene => _scene;
        public uint[] FrameDelay { get; } = new uint[2];

        public T GetScene<T>() where T : Scene
        {
            return _scene as T;
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
            IsMouseVisible = Settings.GlobalSettings.RunMouseInASeparateThread;

            IsFixedTimeStep = false; // Settings.GlobalSettings.FixedTimeStep;
            TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0f / 250);

            SetRefreshRate(Settings.GlobalSettings.FPS);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            LoadGameFilesFromFileSystem();
            base.LoadContent();

            SetScene(new LoginScene());
        }

        protected override void UnloadContent()
        {
            _scene?.Unload();
            Settings.GlobalSettings.Save();
            Plugin.OnClosing();
            base.UnloadContent();
        }

        public void SetScene(Scene scene)
        {
            _scene?.Destroy();
            _scene = scene;

            if (scene != null)
            {
                Window.AllowUserResizing = scene.CanResize;

                SetWindowSize(scene.Width, scene.Height);

                if (scene.CanBeMaximized)
                    MaximizeWindow();
                else
                {
                    RestoreWindow();
                    SDL_GetWindowBordersSize(Window.Handle, out int top, out int left, out int bottom, out int right);
                    SetWindowPosition(left + scene.X, top + scene.Y);
                }

                scene.Load();
            }
        }

        public void SetRefreshRate(int rate)
        {
            if (rate < Constants.MIN_FPS)
                rate = Constants.MIN_FPS;
            else if (rate > Constants.MAX_FPS)
                rate = Constants.MAX_FPS;

            FrameDelay[0] = FrameDelay[1] = (uint) (1000 / rate);
            FrameDelay[1] = FrameDelay[1] >> 1;

            Settings.GlobalSettings.FPS = rate;
            //TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0f / 250);

            _intervalFixedUpdate[0] = 1000.0f / rate;
            _intervalFixedUpdate[1] = 217;  // 5 FPS
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
            Log.Message(LogTypes.Trace, "Checking for Ultima Online installation...");
            Log.PushIndent();


            try
            {
                FileManager.UoFolderPath = Settings.GlobalSettings.UltimaOnlineDirectory;
            }
            catch (FileNotFoundException)
            {
                Log.Message(LogTypes.Error, "Wrong Ultima Online installation folder.");

                throw;
            }

            Log.Message(LogTypes.Trace, "Done!");
            Log.Message(LogTypes.Trace, $"Ultima Online installation folder: {FileManager.UoFolderPath}");
            Log.PopIndent();

            Log.Message(LogTypes.Trace, "Loading files...");
            Log.PushIndent();
            FileManager.LoadFiles();
            Log.PopIndent();

            uint[] hues = FileManager.Hues.CreateShaderColors();

            int size = FileManager.Hues.HuesCount;

            Texture2D texture0 = new Texture2D(GraphicsDevice, 32, size * 2);
            texture0.SetData(hues, 0, size * 2);
            Texture2D texture1 = new Texture2D(GraphicsDevice, 32, size);
            texture1.SetData(hues, size, size);
            GraphicsDevice.Textures[1] = texture0;
            GraphicsDevice.Textures[2] = texture1;

            AuraManager.CreateAuraTexture();

            Log.Message(LogTypes.Trace, "Network calibration...");
            Log.PushIndent();
            PacketHandlers.Load();
            //ATTENTION: you will need to enable ALSO ultimalive server-side, or this code will have absolutely no effect!
            UltimaLive.Enable();
            PacketsTable.AdjustPacketSizeByVersion(FileManager.ClientVersion);
            Log.Message(LogTypes.Trace, "Done!");
            Log.PopIndent();

            Log.Message(LogTypes.Trace, "Loading plugins...");
            Log.PushIndent();

            UIManager.InitializeGameCursor();

            foreach (var p in Settings.GlobalSettings.Plugins)
                Plugin.Create(p);
            Log.Message(LogTypes.Trace, "Done!");
            Log.PopIndent();


            UoAssist.Start();
        }


        private float _totalElapsed, _currentFpsTime;
        private uint _totalFrames;

        protected override void Update(GameTime gameTime)
        {
            Time.Ticks = (uint) gameTime.TotalGameTime.TotalMilliseconds;

            OnNetworkUpdate(gameTime.TotalGameTime.TotalMilliseconds, gameTime.ElapsedGameTime.TotalMilliseconds);
            UIManager.Update(gameTime.TotalGameTime.TotalMilliseconds, gameTime.ElapsedGameTime.TotalMilliseconds);

          
            _totalElapsed += (float) gameTime.ElapsedGameTime.TotalMilliseconds;
            _currentFpsTime += (float) gameTime.ElapsedGameTime.TotalMilliseconds;

            if (_currentFpsTime >= 1000)
            {
                CUOEnviroment.CurrentRefreshRate = _totalFrames;

                _totalFrames = 0;
                _currentFpsTime = 0;
            }

            float x = _intervalFixedUpdate[!IsActive && ProfileManager.Current != null && ProfileManager.Current.ReduceFPSWhenInactive ? 1 : 0];

            if (_totalElapsed > x)
            {
                if (_scene != null && _scene.IsLoaded && !_scene.IsDestroyed)
                    _scene.Update(gameTime.TotalGameTime.TotalMilliseconds, gameTime.ElapsedGameTime.TotalMilliseconds);

                _totalElapsed %= x;
            }
            else
            {
                SuppressDraw();

                if (!gameTime.IsRunningSlowly)
                {
                    Thread.Sleep(1);
                }
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            _totalFrames++;

            if (_scene != null && _scene.IsLoaded && !_scene.IsDestroyed)
                _scene.Draw(_uoSpriteBatch);
            UIManager.Draw(_uoSpriteBatch);

            if (ProfileManager.Current != null && ProfileManager.Current.ShowNetworkStats)
            {
                if (!NetClient.Socket.IsConnected)
                    NetClient.LoginSocket.Statistics.Draw(_uoSpriteBatch, 10, 50);
                else if (!NetClient.Socket.IsDisposed)
                    NetClient.Socket.Statistics.Draw(_uoSpriteBatch, 10, 50);
            }


            base.Draw(gameTime);
        }

        private void OnNetworkUpdate(double totalMS, double frameMS)
        {
            if (NetClient.LoginSocket.IsDisposed && NetClient.LoginSocket.IsConnected)
                NetClient.LoginSocket.Disconnect();
            else if (!NetClient.Socket.IsConnected)
            {
                NetClient.LoginSocket.Update();
                UpdateSockeStats(NetClient.LoginSocket, totalMS);
            }
            else if (!NetClient.Socket.IsDisposed)
            {
                NetClient.Socket.Update();
                UpdateSockeStats(NetClient.Socket, totalMS);
            }
        }

        private void UpdateSockeStats(NetClient socket, double totalMS)
        {
            if (_statisticsTimer < totalMS)
            {
                socket.Statistics.Update();
                _statisticsTimer = totalMS + 500;
            }
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
                //TODO:
            }

            uint flags = SDL.SDL_GetWindowFlags(Window.Handle);
            if ((flags & (uint) SDL.SDL_WindowFlags.SDL_WINDOW_MAXIMIZED) == 0)
            {
                // TODO: option set WindowClientBounds
                ProfileManager.Current.WindowClientBounds = new Point(width, height);
            }

            SetWindowSize(width, height);

            var viewport = UIManager.GetGump<WorldViewportGump>();

            if (viewport != null && ProfileManager.Current.GameWindowFullSize)
            {
                viewport.ResizeGameWindow(new Point(width, height));
                viewport.X = -5;
                viewport.Y = -5;
            }
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
                    
                    Keyboard.OnKeyDown(e.key);

                    if (Plugin.ProcessHotkeys((int) e.key.keysym.sym, (int) e.key.keysym.mod, true))
                    {
                        _ignoreNextTextInput = false;

                        //if (UIManager.IsMouseOverUI)
                            UIManager.KeyboardFocusControl?.InvokeKeyDown(e.key.keysym.sym, e.key.keysym.mod);
                        //else 
                            _scene.OnKeyDown(e.key);
                    }
                    else
                        _ignoreNextTextInput = true;

                    break;

                case SDL.SDL_EventType.SDL_KEYUP:
                    
                    Keyboard.OnKeyUp(e.key);

                    //if (UIManager.IsMouseOverUI)
                        UIManager.KeyboardFocusControl?.InvokeKeyUp(e.key.keysym.sym, e.key.keysym.mod);
                   // else
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
                            if (UIManager.IsKeyboardFocusAllowHotkeys)
                                UIManager.KeyboardFocusControl?.InvokeTextInput(s);
                            else
                                _scene.OnTextInput(s);
                        }
                    }

                    break;

                case SDL.SDL_EventType.SDL_MOUSEMOTION:
                    Mouse.Update();

                    if (Mouse.IsDragging)
                    {
                        UIManager.OnMouseDragging();
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

                    UIManager.OnMouseWheel(isup);
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

                                    var res = UIManager.OnLeftMouseDoubleClick() || _scene.OnLeftMouseDoubleClick();

                                    MouseDoubleClickEventArgs arg = new MouseDoubleClickEventArgs(Mouse.Position.X, Mouse.Position.Y, MouseButton.Left);

                                    if (!arg.Result && !res)
                                    {
                                        UIManager.OnLeftMouseButtonDown();
                                        _scene.OnLeftMouseDown();
                                    }
                                    else
                                        Mouse.LastLeftButtonClickTime = 0xFFFF_FFFF;

                                    break;
                                }

                                UIManager.OnLeftMouseButtonDown();
                                _scene.OnLeftMouseDown();

                                Mouse.LastLeftButtonClickTime = Mouse.CancelDoubleClick ? 0 : ticks;
                            }
                            else
                            {
                                if (resetTime)
                                    Mouse.LastLeftButtonClickTime = 0;

                                if (Mouse.LastLeftButtonClickTime != 0xFFFF_FFFF)
                                {
                                    UIManager.OnLeftMouseButtonUp();
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

                                    var res = UIManager.OnRightMouseDoubleClick() || _scene.OnRightMouseDoubleClick();

                                    MouseDoubleClickEventArgs arg = new MouseDoubleClickEventArgs(Mouse.Position.X, Mouse.Position.Y, MouseButton.Right);

                                    if (!arg.Result && !res)
                                    {
                                        UIManager.OnRightMouseButtonDown();
                                        _scene.OnRightMouseDown();
                                    }
                                    else
                                        Mouse.LastRightButtonClickTime = 0xFFFF_FFFF;

                                    break;
                                }

                                UIManager.OnRightMouseButtonDown();
                                _scene.OnRightMouseDown();

                                Mouse.LastRightButtonClickTime = Mouse.CancelDoubleClick ? 0 : ticks;
                            }
                            else
                            {
                                if (resetTime)
                                    Mouse.LastRightButtonClickTime = 0;

                                if (Mouse.LastRightButtonClickTime != 0xFFFF_FFFF)
                                {
                                    UIManager.OnRightMouseButtonUp();
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
