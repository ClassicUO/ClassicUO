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

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using SDL2;

namespace ClassicUO
{
    internal class DebugInfo
    {
        public int MobilesRendered { get; set; }
        public int ItemsRendered { get; set; }
        public int StaticsRendered { get; set; }
        public int MultiRendered { get; set; }
        public int LandsRendered { get; set; }
        public int EffectsRendered { get; set; }

        public void Reset()
        {
            MobilesRendered = ItemsRendered = StaticsRendered = MultiRendered = LandsRendered = EffectsRendered = 0;
        }
    }

    //internal class Window
    //{
    //    private Engine _engine;

    //    public Window(Engine engine)
    //    {
    //        _engine = engine;
    //    }

    //    public int X
    //    {
    //        get => _engine.Window.ClientBounds.X;
    //        set
    //        {
    //            _engine.Window.
    //        }
    //    }
    //}

    internal class Engine : Microsoft.Xna.Framework.Game
    { 
        private const int MIN_FPS = 15;
        private const int MAX_FPS = 250;
        private const int LOGIN_SCREEN_FPS = 60;

        private static int _fpsLimit = 30;
        private static Engine _engine;
        private readonly GraphicsDeviceManager _graphicDeviceManager;
        private Batcher2D _batcher;
        private double _currentFpsTime;
        private ProfileManager _profileManager;
        private SceneManager _sceneManager;
        private InputManager _inputManager;
        private double _statisticsTimer;
        private float _time;
        private int _totalFrames;
        private UIManager _uiManager;
        private Settings _settings;
        private DebugInfo _debugInfo;
        private bool _isRunningSlowly;
        private bool _isMaximized;

        private Engine()
        {
            //IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromSeconds(1.0f / MAX_FPS);

            _graphicDeviceManager = new GraphicsDeviceManager(this);
            _graphicDeviceManager.PreparingDeviceSettings += (sender, e) => e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;

            if (_graphicDeviceManager.GraphicsDevice.Adapter.IsProfileSupported(GraphicsProfile.HiDef))
                _graphicDeviceManager.GraphicsProfile = GraphicsProfile.HiDef;
            _graphicDeviceManager.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
            _graphicDeviceManager.SynchronizeWithVerticalRetrace = false;
            _graphicDeviceManager.ApplyChanges();

            Window.ClientSizeChanged += (sender, e) =>
            {
                _graphicDeviceManager.PreferredBackBufferWidth = Window.ClientBounds.Width;
                _graphicDeviceManager.PreferredBackBufferHeight = Window.ClientBounds.Height;
                _graphicDeviceManager.ApplyChanges();
            };
            Window.AllowUserResizing = true;
            IsMouseVisible = true;
        }

        public static Batcher2D Batcher => _engine._batcher;

        protected float IntervalFixedUpdate { get; private set; }

        public static int FpsLimit
        {
            get => _fpsLimit;
            set
            {
                if (_fpsLimit != value)
                {
                    _fpsLimit = value;

                    if (_fpsLimit < MIN_FPS)
                        _fpsLimit = MIN_FPS;
                    else if (_fpsLimit > MAX_FPS)
                        _fpsLimit = MAX_FPS;
                    FrameDelay[0] = FrameDelay[1] = (uint) (1000 / _fpsLimit);

                    _engine.IntervalFixedUpdate = 1000.0f / _fpsLimit;
                }
            }
        }

        public static Version Version { get; } = new Version(0, 0, 0, 1);

        public static int CurrentFPS { get; private set; }

        /// <summary>
        ///     Total game time in milliseconds
        /// </summary>
        public static long Ticks { get; private set; }

        /// <summary>
        ///     Milliseconds from last frame
        /// </summary>
        public static long TicksFrame { get; private set; }

        public static uint[] FrameDelay { get; } = new uint[2];

        public static bool IsFullScreen
        {
            get => _engine._isMaximized;
            set
            {
                if (_engine._isMaximized == value)
                    return;

                _engine._isMaximized = value;

                IntPtr wnd = SDL.SDL_GL_GetCurrentWindow();

                if (value)
                    SDL.SDL_MaximizeWindow(wnd);
                else
                    SDL.SDL_RestoreWindow(wnd);
            }
        }

        public static int WindowWidth
        {
            get => _engine._graphicDeviceManager.PreferredBackBufferWidth;
            set
            {
                _engine._graphicDeviceManager.PreferredBackBufferWidth = value;
                _engine._graphicDeviceManager.ApplyChanges();
            }
        }

        public static int WindowHeight
        {
            get => _engine._graphicDeviceManager.PreferredBackBufferHeight;
            set
            {
                _engine._graphicDeviceManager.PreferredBackBufferHeight = value;
                _engine._graphicDeviceManager.ApplyChanges();
            }
        }

        public static UIManager UI => _engine._uiManager;

        public static InputManager Input => _engine._inputManager;

        public static ProfileManager Profile => _engine._profileManager;

        public static Settings GlobalSettings => _engine._settings;

        public static SceneManager SceneManager => _engine._sceneManager;

        public static string ExePath { get; private set; }

        public static DebugInfo DebugInfo => _engine._debugInfo;

        public static bool IsRunningSlowly => _engine._isRunningSlowly;


        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetDllDirectory(string lpPathName);



        private static void Main(string[] args)
        {
            Configure();

            using (_engine = new Engine())
                _engine.Run();
        }

        public static void Quit()
        {
            _engine.Exit();
        }



        private static void Configure()
        {
            Log.Start(LogTypes.All);
            ExePath = Directory.GetCurrentDirectory();

#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += async (sender, e) =>
            {
                string msg = e.ExceptionObject.ToString();
                Log.Message(LogTypes.Panic, msg);
                string path = Path.Combine(ExePath, "Logs");

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                using (LogFile crashfile = new LogFile(path, "crash.txt"))
                    await crashfile.WriteAsync(msg);
            };
#endif
            // We can use the mono's dllmap feature, but 99% of people use VS to compile.
            if (Environment.OSVersion.Platform != PlatformID.MacOSX && Environment.OSVersion.Platform != PlatformID.Unix)
            {
                string libsPath = Path.Combine(ExePath, "libs", Environment.Is64BitProcess ? "x64" : "x86");

                SetDllDirectory(libsPath);
            }

            Environment.SetEnvironmentVariable("FNA_GRAPHICS_ENABLE_HIGHDPI", "1");
            Environment.SetEnvironmentVariable("FNA_OPENGL_BACKBUFFER_SCALE_NEAREST", "1");
            Environment.SetEnvironmentVariable(SDL.SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");
        }



        protected override void Initialize()
        {
            Log.NewLine();
            Log.NewLine();

            Log.Message(LogTypes.Trace, $"Starting ClassicUO - {Version}", ConsoleColor.Cyan);

            Log.NewLine();
            Log.NewLine();

            _settings = ConfigurationResolver.Load<Settings>(Path.Combine(ExePath, "settings.json"));

            if (_settings == null)
            {
                SDL.SDL_ShowSimpleMessageBox(SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR, "No `setting.json`", "A `settings.json` has been created into ClassicUO main folder.\nPlease fill it!", SDL.SDL_GL_GetCurrentWindow());
                Log.Message(LogTypes.Trace, "settings.json file was not found creating default");
                _settings = new Settings();
                _settings.Save();
                Quit();

                return;
            }

            Log.Message(LogTypes.Trace, "Checking for Ultima Online installation...");
            Log.PushIndent();

            try
            {
                FileManager.UoFolderPath = _settings.UltimaOnlineDirectory;
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

            _batcher = new Batcher2D(GraphicsDevice);

            int size = FileManager.Hues.HuesCount;

            Texture2D texture0 = new Texture2D(GraphicsDevice, 32, size);
            texture0.SetData(hues, 0, size);
            Texture2D texture1 = new Texture2D(GraphicsDevice, 32, size);
            texture1.SetData(hues, size, size);
            GraphicsDevice.Textures[1] = texture0;
            GraphicsDevice.Textures[2] = texture1;


            _inputManager = new InputManager();
            _uiManager = new UIManager();
            _profileManager = new ProfileManager();
            _sceneManager = new SceneManager();
            Log.Message(LogTypes.Trace, "Network calibration...");
            Log.PushIndent();
            PacketHandlers.Load();
            //ATTENTION: you will need to enable ALSO ultimalive server-side, or this code will have absolutely no effect!
            UltimaLive.Load();
            PacketsTable.AdjustPacketSizeByVersion(FileManager.ClientVersion);
            Log.Message(LogTypes.Trace, "Done!");
            Log.PopIndent();

            FpsLimit = LOGIN_SCREEN_FPS;

            _debugInfo = new DebugInfo();
            _uiManager.Add(new DebugGump());          

            base.Initialize();
        }


        protected override void LoadContent()
        {
            Log.Message(LogTypes.Trace, "Loading plugins...");
            Log.PushIndent();
            Plugin.Create(@".\Assistant\Razor.dll");
            Log.Message(LogTypes.Trace, "Done!");
            Log.PopIndent();

            _sceneManager.ChangeScene(ScenesType.Login);
            base.LoadContent();           
        }

        protected override void UnloadContent()
        {   
            _inputManager.Dispose();
            _sceneManager.CurrentScene?.Unload();
            _settings.Save();
            Plugin.OnClosing();
            base.UnloadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            if (Profiler.InContext("OutOfContext"))
                Profiler.ExitContext("OutOfContext");
            Profiler.EnterContext("Update");
            double totalms = gameTime.TotalGameTime.TotalMilliseconds;
            double framems = gameTime.ElapsedGameTime.TotalMilliseconds;
            Ticks = (long) totalms;
            TicksFrame = (long) framems;
            _currentFpsTime += gameTime.ElapsedGameTime.TotalSeconds;

            if (_currentFpsTime >= 1.0)
            {
                CurrentFPS = _totalFrames;
                _totalFrames = 0;
                _currentFpsTime = 0;
            }

            // ###############################
            // This should be the right order
            OnNetworkUpdate(totalms, framems);
            OnInputUpdate(totalms, framems);
            OnUIUpdate(totalms, framems);
            OnUpdate(totalms, framems);
            // ###############################
            Profiler.ExitContext("Update");

            _time += (float)framems;

            if (_time > IntervalFixedUpdate)
            {
                _time = _time % IntervalFixedUpdate;
                Profiler.EnterContext("FixedUpdate");
                OnFixedUpdate(totalms, framems);
                Profiler.ExitContext("FixedUpdate");
            }
            else
            {
                SuppressDraw();
            }

            base.Update(gameTime);
            Profiler.EnterContext("OutOfContext");
        }

        protected override void Draw(GameTime gameTime)
        {
            _isRunningSlowly = gameTime.IsRunningSlowly;
            _debugInfo.Reset();

            Profiler.EndFrame();
            Profiler.BeginFrame();

            if (Profiler.InContext("OutOfContext"))
                Profiler.ExitContext("OutOfContext");
            Profiler.EnterContext("RenderFrame");

            _totalFrames++;

            if (_sceneManager.CurrentScene.IsLoaded)
                _sceneManager.CurrentScene.Draw(_batcher);

            GraphicsDevice.Clear(Color.Transparent);
            _batcher.Begin();
            UI.Draw(_batcher);
            _batcher.End();

            Profiler.ExitContext("RenderFrame");
            Profiler.EnterContext("OutOfContext");
            UpdateWindowCaption(gameTime);

            base.Draw(gameTime);
        }

        private void UpdateWindowCaption(GameTime gameTime)
        {
            if (!_settings.Profiler)
                return;

            double timeDraw = Profiler.GetContext("RenderFrame").TimeInContext;
            double timeUpdate = Profiler.GetContext("Update").TimeInContext;
            double timeFixedUpdate = Profiler.GetContext("FixedUpdate").TimeInContext;
            double timeOutOfContext = Profiler.GetContext("OutOfContext").TimeInContext;
            double timeTotalCheck = timeOutOfContext + timeDraw + timeUpdate + timeFixedUpdate;
            double timeTotal = Profiler.TrackedTime;
            double avgDrawMs = Profiler.GetContext("RenderFrame").AverageTime;
            Window.Title = string.Format("ClassicUO - Draw:{0:0.0}% Update:{1:0.0}% Fixed:{2:0.0}% AvgDraw:{3:0.0}ms {4} - FPS: {5}", 100d * (timeDraw / timeTotal), 100d * (timeUpdate / timeTotal), 100d * (timeFixedUpdate / timeTotal), avgDrawMs, gameTime.IsRunningSlowly ? "*" : string.Empty, CurrentFPS);
        }

        private void OnInputUpdate(double totalMS, double frameMS)
        {
            Mouse.Update();
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

        private void OnUIUpdate(double totalMS, double frameMS)
        {
            UI.Update(totalMS, frameMS);
        }

        private void OnUpdate(double totalMS, double frameMS)
        {
            if (_sceneManager.CurrentScene.IsLoaded)
                _sceneManager.CurrentScene.Update(totalMS, frameMS);
        }

        private void OnFixedUpdate(double totalMS, double frameMS)
        {
            if (_sceneManager.CurrentScene.IsLoaded)
                _sceneManager.CurrentScene.FixedUpdate(totalMS, frameMS);
        }
    }
}