//#region license

////  Copyright (C) 2019 ClassicUO Development Community on Github
////
////	This project is an alternative client for the game Ultima Online.
////	The goal of this is to develop a lightweight client considering 
////	new technologies.  
////      
////  This program is free software: you can redistribute it and/or modify
////  it under the terms of the GNU General Public License as published by
////  the Free Software Foundation, either version 3 of the License, or
////  (at your option) any later version.
////
////  This program is distributed in the hope that it will be useful,
////  but WITHOUT ANY WARRANTY; without even the implied warranty of
////  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
////  GNU General Public License for more details.
////
////  You should have received a copy of the GNU General Public License
////  along with this program.  If not, see <https://www.gnu.org/licenses/>.

//#endregion

//#define DEV_BUILD

//using System;
//using System.Diagnostics;
//using System.IO;
//using System.Reflection;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Threading;

//using ClassicUO.Configuration;
//using ClassicUO.Game;
//using ClassicUO.Game.Managers;
//using ClassicUO.Game.Scenes;
//using ClassicUO.Game.UI.Gumps;
//using ClassicUO.Input;
//using ClassicUO.IO;
//using ClassicUO.Network;
//using ClassicUO.Renderer;
//using ClassicUO.Utility;
//using ClassicUO.Utility.Logging;
//using ClassicUO.Utility.Platforms;

//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;

//using Newtonsoft.Json;

//using SDL2;

//namespace ClassicUO
//{
//    internal class DebugInfo
//    {
//        public int MobilesRendered;
//        public int ItemsRendered;
//        public int StaticsRendered;
//        public int MultiRendered;
//        public int LandsRendered;
//        public int EffectsRendered;

//        public void Reset()
//        {
//            MobilesRendered = ItemsRendered = StaticsRendered = MultiRendered = LandsRendered = EffectsRendered = 0;
//        }
//    }

//    internal class Engine : Microsoft.Xna.Framework.Game
//    {
//        private const int INACTIVE_FPS_DELAY = 217; // 5 fps

//        private static GameWindow _window;
//        private static int _fpsLimit = 30;
//        private static Engine _engine;

//        private readonly GraphicsDeviceManager _graphicDeviceManager;
//        private readonly bool _isHighDPI;
//        private readonly Settings _settings;
//        private UltimaBatcher2D _batcher;
//        private double _currentFpsTime;
//        private DebugInfo _debugInfo;
//        private bool _isRunningSlowly;
//        private double _previous;
//        //private SceneManager _sceneManager;
//        private double _statisticsTimer;
//        private double _totalElapsed;
//        private int _totalFrames;

//        private Engine(string[] args)
//        {
//            Instance = this;

//            string settingsFilepath = Settings.GetSettingsFilepath();

//            Console.WriteLine($"SETTINGS FILE: {settingsFilepath}");

//            // Check settings file for existence
//            if (!Directory.Exists(Path.GetDirectoryName(settingsFilepath)) || !File.Exists(settingsFilepath))
//            {
//                Log.Message(LogTypes.Trace, "Settings file not found");
//                Directory.CreateDirectory(Path.GetDirectoryName(settingsFilepath)); // Make sure we have full path in place
//                _settings = new Settings();
//                _settings.Save();
//                SDL.SDL_ShowSimpleMessageBox(
//                    SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_INFORMATION,
//                    "No settings",
//                    $"A new settings file has been created: {settingsFilepath}\n\n" +
//                        "Please, open it with a text editor and at least set `ultimaonlinedirectory` and `clientversion` options.",
//                    SDL.SDL_GL_GetCurrentWindow()
//                );
//                IsQuitted = true;

//                return;
//            }

//            // Firstly, try to load settings from either main or custom settings file
//            // TODO: Wrap it in the `try..catch` block to catch potential JSON parsing errors
//            _settings = ConfigurationResolver.Load<Settings>(settingsFilepath);

//            // Secondly, Try to apply any settings passed from the command-line/shortcut options over those
//            // we loaded from the settings file
//            // NOTE: If nothing was loaded from settings file (file doesn't exist), then it will create
//            //   a new settings object and populate it with the passed settings
//            ArgsParser(args, _settings);

//            // If no settings loaded from file, no settings parsed from command-line options,
//            //   or if settings are invalid, then show an error and exit
//            if (_settings == null || !_settings.IsValid())
//            {
//                Log.Message(LogTypes.Trace, "Invalid settings");
//                SDL.SDL_ShowSimpleMessageBox(
//                    SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_INFORMATION,
//                    "Invalid settings",
//                    "Please, check your settings.\n\n" +
//                        "You should at least set `ultimaonlinedirectory` and `clientversion`.",
//                    SDL.SDL_GL_GetCurrentWindow()
//                );
//                IsQuitted = true;

//                return;
//            }


//            TargetElapsedTime = TimeSpan.FromSeconds(1.0f / Constants.MAX_FPS);
//            IsFixedTimeStep = _settings.FixedTimeStep;

//            _graphicDeviceManager = new GraphicsDeviceManager(this);
//            _graphicDeviceManager.PreparingDeviceSettings += (sender, e) => e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.DiscardContents;

//            if (_graphicDeviceManager.GraphicsDevice.Adapter.IsProfileSupported(GraphicsProfile.HiDef))
//                _graphicDeviceManager.GraphicsProfile = GraphicsProfile.HiDef;

//            _graphicDeviceManager.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
//            _graphicDeviceManager.SynchronizeWithVerticalRetrace = false;
//            _graphicDeviceManager.ApplyChanges();

//            _isHighDPI = Environment.GetEnvironmentVariable("FNA_GRAPHICS_ENABLE_HIGHDPI") == "1";
//            _window = Window;

//            //Window.ClientSizeChanged += (sender, e) =>
//            //{
//            //    int width = Window.ClientBounds.Width;
//            //    int height = Window.ClientBounds.Height;

//            //    if (_isHighDPI)
//            //    {
//            //        width *= 2;
//            //        height *= 2;
//            //    }

//            //    if (!IsMaximized)
//            //        _engine._profileManager.Current.WindowClientBounds = new Point(width, height);

//            //    SetPreferredBackBufferSize(width, height);

//            //    WorldViewportGump gump = UIManager.GetGump<WorldViewportGump>();

//            //    if (gump != null && _profileManager.Current.GameWindowFullSize)
//            //    {
//            //        gump.ResizeWindow(new Point(WindowWidth, WindowHeight));
//            //        gump.X = -5;
//            //        gump.Y = -5;
//            //    }
//            //};

//            Window.AllowUserResizing = true;
//            IsMouseVisible = _settings.RunMouseInASeparateThread;

//            Window.Title = $"ClassicUO - {Version}";

//            if (Bootstrap.StartMinimized)
//                SDL.SDL_MinimizeWindow(Window.Handle);
//        }

//        public bool IsQuitted { get; private set; }

//        public bool DisableUpdateWindowCaption { get; set; }

//        public static int FpsLimit
//        {
//            get => _fpsLimit;
//            set
//            {
//                if (_fpsLimit != value)
//                {
//                    _fpsLimit = value;

//                    if (_fpsLimit < Constants.MIN_FPS)
//                        _fpsLimit = Constants.MIN_FPS;
//                    else if (_fpsLimit > Constants.MAX_FPS)
//                        _fpsLimit = Constants.MAX_FPS;
//                    FrameDelay[0] = FrameDelay[1] = (uint) (1000 / _fpsLimit);
//                    FrameDelay[1] = FrameDelay[1] >> 1;

//                    _engine.IntervalFixedUpdate[0] = 1000.0 / _fpsLimit;
//                    _engine.IntervalFixedUpdate[1] = INACTIVE_FPS_DELAY;
//                    //_engine.TargetElapsedTime = TimeSpan.FromSeconds(1.0 / _fpsLimit);
//                }
//            }
//        }

//        public static Version Version { get; } = Assembly.GetExecutingAssembly().GetName().Version;

//        public static int CurrentFPS { get; private set; }
//        public static int FPSMin { get; private set; } = int.MaxValue;
//        public static int FPSMax { get; private set; }
//        public static bool AllowWindowResizing
//        {
//            get => _window.AllowUserResizing;
//            set => _window.AllowUserResizing = value;
//        }
//        public static uint Ticks { get; private set; }

//        public static uint[] FrameDelay { get; } = new uint[2];

//        public static bool IsMaximized
//        {
//            get
//            {
//                IntPtr wnd = SDL.SDL_GL_GetCurrentWindow();
//                uint flags = SDL.SDL_GetWindowFlags(wnd);

//                return (flags & (uint) SDL.SDL_WindowFlags.SDL_WINDOW_MAXIMIZED) != 0;
//            }
//            set
//            {
//                if (IsMaximized == value)
//                    return;

//                IntPtr wnd = SDL.SDL_GL_GetCurrentWindow();

//                if (value)
//                    SDL.SDL_MaximizeWindow(wnd);
//                else
//                    SDL.SDL_RestoreWindow(wnd);
//            }
//        }

//        public static Engine Instance { get; private set; }

//        public static int ThreadID { get; private set; }

//        public static int WindowWidth
//        {
//            get => _engine._graphicDeviceManager.PreferredBackBufferWidth;
//            set
//            {
//                _engine._graphicDeviceManager.PreferredBackBufferWidth = value;
//                _engine._graphicDeviceManager.ApplyChanges();
//            }
//        }

//        public static int WindowHeight
//        {
//            get => _engine._graphicDeviceManager.PreferredBackBufferHeight;
//            set
//            {
//                _engine._graphicDeviceManager.PreferredBackBufferHeight = value;
//                _engine._graphicDeviceManager.ApplyChanges();
//            }
//        }

//        public static Point WindowPosition
//        {
//            get
//            {
//                SDL.SDL_GetWindowPosition(_window.Handle, out var x, out var y);
//                return new Point(x, y);
//            }

//            set => SDL.SDL_SetWindowPosition(_window.Handle, value.X, value.Y);
//        }



//        //public static SceneManager SceneManager => _engine._sceneManager;

//        public static string ExePath { get; private set; }

//        public static DebugInfo DebugInfo => _engine._debugInfo;

//        public double[] IntervalFixedUpdate { get; } = new double[2];

//        public double FPSTime => IntervalFixedUpdate[/*!IsActive && _profileManager?.Current != null && _profileManager.Current.ReduceFPSWhenInactive ? 1 :*/ 0];

//        public static void SetPreferredBackBufferSize(int width, int height)
//        {
//            _engine._graphicDeviceManager.PreferredBackBufferWidth = width;
//            _engine._graphicDeviceManager.PreferredBackBufferHeight = height;
//            _engine._graphicDeviceManager.ApplyChanges();
//        }

//        public static void DropFpsMinMaxValues()
//        {
//            FPSMax = 0;
//            FPSMin = int.MaxValue;
//        }

//        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
//        [return: MarshalAs(UnmanagedType.Bool)]
//        private static extern bool SetDllDirectory(string lpPathName);


//        internal static void Run(string[] args)
//        {
//            using (_engine = new Engine(args))
//            {
//                if (!_engine.IsQuitted)
//                    _engine.Run();
//            }
//        }

//        public static void Quit()
//        {
//            _engine?.Exit();
//        }

//        private static void ArgsParser(string[] args, Settings settings = null)
//        {
//            // NOTE: If no command-line options given, this method will NOT create a new setting object
//            if (args.Length == 0)
//                return;

//            if (args.Length > 1)
//            {
//                if (settings == null)
//                    settings = new Settings();

//                for (int i = 0; i < args.Length - 1; i++)
//                {
//                    string cmd = args[i].ToLower();

//                    // NOTE: Command-line option name should start with "-" character
//                    if (cmd.Length == 0 || cmd[0] != '-')
//                        continue;

//                    cmd = cmd.Remove(0, 1);
//                    string value = (i < args.Length - 1) ? args[i + 1] : null;

//                    // NOTE: This is duplicated output
//                    //Log.Message(LogTypes.Trace, $"ARG: {cmd}, VALUE: {value}");

//                    switch (cmd)
//                    {
//                        case "username":
//                            settings.Username = value;

//                            break;

//                        case "password":
//                            settings.Password = Crypter.Encrypt(value);

//                            break;

//                        case "password_enc": // Non-standard setting, similar to `password` but for already encrypted password
//                            settings.Password = value;

//                            break;

//                        case "ip":
//                            settings.IP = value;

//                            break;

//                        case "port":
//                            settings.Port = ushort.Parse(value);

//                            break;

//                        case "ultimaonlinedirectory":
//                        case "uopath":
//                            settings.UltimaOnlineDirectory = value; // Required

//                            break;

//                        case "clientversion":
//                            settings.ClientVersion = value; // Required

//                            break;

//                        case "lastcharactername":
//                        case "lastcharname":
//                            settings.LastCharacterName = value;

//                            break;

//                        case "lastservernum":
//                            settings.LastServerNum = ushort.Parse(value);

//                            break;

//                        case "login_fps":
//                        case "fps":
//                            settings.MaxLoginFPS = int.Parse(value);

//                            break;

//                        case "debug":
//                            settings.Debug = bool.Parse(value);

//                            break;

//                        case "profiler":
//                            settings.Profiler = bool.Parse(value);

//                            break;
                        
//                        case "saveaccount":
//                            settings.SaveAccount = bool.Parse(value);

//                            break;

//                        case "autologin":
//                            settings.AutoLogin = bool.Parse(value);

//                            break;

//                        case "reconnect":
//                            settings.Reconnect = bool.Parse(value);

//                            break;

//                        case "reconnect_time":
//                            settings.ReconnectTime = int.Parse(value);

//                            break;

//                        case "login_music":
//                        case "music":
//                            settings.LoginMusic = bool.Parse(value);

//                            break;

//                        case "login_music_volume":
//                        case "music_volume":
//                            settings.LoginMusicVolume = int.Parse(value);

//                            break;

//                        case "shard_type":
//                        case "shard":
//                            settings.ShardType = int.Parse(value);

//                            break;

//                        case "fixed_time_step":
//                            settings.FixedTimeStep = bool.Parse(value);

//                            break;
//                    }
//                }
//            }
//        }

//        protected override void OnExiting(object sender, EventArgs args)
//        {
//            IsQuitted = true;
//            base.OnExiting(sender, args);
//        }
         

//        internal static void Configure()
//        {
//            Thread.CurrentThread.Name = "CUO_MAIN_THREAD";
//            ThreadID = Thread.CurrentThread.ManagedThreadId;

//            Log.Start(LogTypes.All);

//            // NOTE: This is very important to get correct path of the assembly to properly run in the mono environment
//            // @see: https://stackoverflow.com/questions/39601742/mono-executes-program-with-wrong-current-directory
//            // @see: https://stackoverflow.com/questions/1658518/getting-the-absolute-path-of-the-executable-using-c/17836681
//            ExePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location); // Environment.CurrentDirectory;
//            Console.WriteLine("EXE PATH: {0}", ExePath);

//#if !DEBUG
//            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
//            {
//                StringBuilder sb = new StringBuilder();
//#if DEV_BUILD
//                sb.AppendFormat("ClassicUO [dev] - v{0}\nOS: {1} {2}\nThread: {3}\n\n", Version, Environment.OSVersion.Platform, Environment.Is64BitOperatingSystem ? "x64" : "x86", Thread.CurrentThread.Name);
//#else
//                sb.AppendFormat("ClassicUO - v{0}\nOS: {1} {2}\nThread: {3}\n\n", Version, Environment.OSVersion.Platform, Environment.Is64BitOperatingSystem ? "x64" : "x86", Thread.CurrentThread.Name);
//#endif
//                sb.AppendFormat("Exception:\n{0}", e.ExceptionObject);

//                Log.Message(LogTypes.Panic, e.ExceptionObject.ToString());
//                string path = Path.Combine(ExePath, "Logs");

//                if (!Directory.Exists(path))
//                    Directory.CreateDirectory(path);

//                using (LogFile crashfile = new LogFile(path, "crash.txt"))
//                {
//                    crashfile.WriteAsync(sb.ToString()).RunSynchronously();

//                    //SDL.SDL_ShowSimpleMessageBox(
//                    //                             SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_INFORMATION,
//                    //                             "An error occurred",
//                    //                             $"{crashfile}\ncreated in /Logs.",
//                    //                             SDL.SDL_GL_GetCurrentWindow()
//                    //                            );
//                }

             
//            };
//#endif

//            // We can use the mono's dllmap feature, but 99% of people use VS to compile.
//            if (Environment.OSVersion.Platform != PlatformID.MacOSX && Environment.OSVersion.Platform != PlatformID.Unix)
//            {
//                string libsPath = Path.Combine(ExePath, "libs", Environment.Is64BitProcess ? "x64" : "x86");
//                SetDllDirectory(libsPath);
//            }

//            Environment.SetEnvironmentVariable("FNA_GRAPHICS_ENABLE_HIGHDPI", "1");
//            Environment.SetEnvironmentVariable("FNA_OPENGL_BACKBUFFER_SCALE_NEAREST", "1");
//            Environment.SetEnvironmentVariable("FNA_OPENGL_FORCE_COMPATIBILITY_PROFILE", "1");
//            Environment.SetEnvironmentVariable(SDL.SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");
//            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + Path.Combine(ExePath, "Data", "Plugins"));
//        }

//        protected override void Initialize()
//        {
//            Log.NewLine();
//            Log.NewLine();

//            Log.Message(LogTypes.Trace, $"Starting ClassicUO - {Version}", ConsoleColor.Cyan);

//            Log.NewLine();
//            Log.NewLine();

//            _batcher = new UltimaBatcher2D(GraphicsDevice);
//            //_sceneManager = new SceneManager();
//            _debugInfo = new DebugInfo();

//            FpsLimit = Constants.LOGIN_SCREEN_FPS;


//            Log.Message(LogTypes.Trace, "Loading UI Fonts...");
//            Log.PushIndent();
//            Fonts.Load();
//            Log.PopIndent();


//            Log.Message(LogTypes.Trace, "Checking for Ultima Online installation...");
//            Log.PushIndent();


//            try
//            {
//                //FileManager.UoFolderPath = GlobalSettings.UltimaOnlineDirectory;
//            }
//            catch (FileNotFoundException)
//            {
//                Log.Message(LogTypes.Error, "Wrong Ultima Online installation folder.");

//                throw;
//            }

//            Log.Message(LogTypes.Trace, "Done!");
//            Log.Message(LogTypes.Trace, $"Ultima Online installation folder: {FileManager.UoFolderPath}");
//            Log.PopIndent();

//            Log.Message(LogTypes.Trace, "Loading files...");
//            Log.PushIndent();
//            FileManager.LoadFiles();
//            Log.PopIndent();

//            uint[] hues = FileManager.Hues.CreateShaderColors();

//            int size = FileManager.Hues.HuesCount;

//            Texture2D texture0 = new Texture2D(GraphicsDevice, 32, size * 2);
//            texture0.SetData(hues, 0, size * 2);
//            Texture2D texture1 = new Texture2D(GraphicsDevice, 32, size);
//            texture1.SetData(hues, size, size);
//            GraphicsDevice.Textures[1] = texture0;
//            GraphicsDevice.Textures[2] = texture1;


//            Log.Message(LogTypes.Trace, "Network calibration...");
//            Log.PushIndent();
//            PacketHandlers.Load();
//            //ATTENTION: you will need to enable ALSO ultimalive server-side, or this code will have absolutely no effect!
//            UltimaLive.Enable();
//            PacketsTable.AdjustPacketSizeByVersion(FileManager.ClientVersion);
//            Log.Message(LogTypes.Trace, "Done!");
//            Log.PopIndent();


//            Log.Message(LogTypes.Trace, "Loading plugins...");
//            Log.PushIndent();

//            //foreach (var p in GlobalSettings.Plugins)
//            //    Plugin.Create(p);
//            Log.Message(LogTypes.Trace, "Done!");
//            Log.PopIndent();


//            UoAssist.Start();

//            base.Initialize();
//        }


//        protected override void LoadContent()
//        {
//            //_sceneManager.ChangeScene(ScenesType.Login);
//            base.LoadContent();
//        }

//        protected override void UnloadContent()
//        {
//            //_sceneManager.CurrentScene?.Unload();
//            _settings.Save();
//            Plugin.OnClosing();
//            base.UnloadContent();
//        }


//        protected override void BeginRun()
//        {
//            base.BeginRun();
//            //SDL.SDL_GetWindowBordersSize(Window.Handle, out int top, out _, out _, out _);
//            //SDL.SDL_SetWindowPosition(Window.Handle, 0, top);
//            _previous = SDL.SDL_GetTicks();
//        }

  
//        public override void Tick()
//        {
//            if (Profiler.InContext("OutOfContext"))
//                Profiler.ExitContext("OutOfContext");


//            double current = Ticks = SDL.SDL_GetTicks();
//            double elapsed = current - _previous;
//            _previous = current;

//            // ###############################
//            // This should be the right order
//            OnNetworkUpdate(current, elapsed);
//            Mouse.Update();
//            //_uiManager.Update(current, elapsed);
//            Plugin.Tick();
//            // ###############################


//            Profiler.EnterContext("Update");

//            //Scene scene = _sceneManager.CurrentScene;
//            //if (scene != null && scene.IsLoaded)
//            //    scene.Update(current, elapsed);
//            FrameworkDispatcher.Update();

//            Profiler.ExitContext("Update");


//            _currentFpsTime += elapsed;

//            if (_currentFpsTime >= 1000)
//            {
//                CurrentFPS = _totalFrames;

//                FPSMax = CurrentFPS > FPSMax || FPSMax > FpsLimit ? CurrentFPS : FPSMax;
//                FPSMin = CurrentFPS < FPSMin && CurrentFPS != 0 ? CurrentFPS : FPSMin;

//                _totalFrames = 0;
//                _currentFpsTime = 0;
//            }

//            //base.Tick();


//            _totalElapsed += elapsed;

//            double fpsTime = FPSTime;

//            if (_totalElapsed > fpsTime)
//            {
//                Render();
//                GraphicsDevice?.Present();
//                _totalElapsed -= fpsTime;
//                _isRunningSlowly = _totalElapsed > fpsTime;

//                if (_isRunningSlowly && _totalElapsed > fpsTime * 2)
//                {
//                    _totalElapsed %= fpsTime;
//                }
//            }

//            if (!_isRunningSlowly)
//            {
//                uint sleep = SDL.SDL_GetTicks() - Ticks;

//                if (fpsTime - sleep >= FrameDelay[1])
//                {
//                    Thread.Sleep(1);
//                }
//            }
//        }


//        private void Render()
//        {
//            _debugInfo.Reset();

//            Profiler.EndFrame();
//            Profiler.BeginFrame();

//            if (Profiler.InContext("OutOfContext"))
//                Profiler.ExitContext("OutOfContext");
//            Profiler.EnterContext("RenderFrame");

//            _totalFrames++;

//            //var scene = _sceneManager.CurrentScene;
//            //if (scene != null && scene.IsLoaded && !scene.IsDestroyed)
//            //    scene.Draw(_batcher);

//            //_uiManager.Draw(_batcher);

//            //if (_profileManager.Current != null && _profileManager.Current.ShowNetworkStats)
//            //{
//            //    if (!NetClient.Socket.IsConnected)
//            //        NetClient.LoginSocket.Statistics.Draw(_batcher, 10, 50);
//            //    else if (!NetClient.Socket.IsDisposed) NetClient.Socket.Statistics.Draw(_batcher, 10, 50);
//            //}


//            Profiler.ExitContext("RenderFrame");
//            Profiler.EnterContext("OutOfContext");
//            UpdateWindowCaption();


//            //GraphicsDevice?.Present();
//        }


//        private void UpdateWindowCaption()
//        {
//            if (!_settings.Profiler || DisableUpdateWindowCaption)
//                return;

//            double timeDraw = Profiler.GetContext("RenderFrame").TimeInContext;
//            double timeUpdate = Profiler.GetContext("Update").TimeInContext;
//            double timeOutOfContext = Profiler.GetContext("OutOfContext").TimeInContext;
//            double timeTotalCheck = timeOutOfContext + timeDraw + timeUpdate;
//            double timeTotal = Profiler.TrackedTime;
//            double avgDrawMs = Profiler.GetContext("RenderFrame").AverageTime;

//#if DEV_BUILD
//            Window.Title = string.Format("ClassicUO [dev] {5} - Draw:{0:0.0}% Update:{1:0.0}% AvgDraw:{2:0.0}ms {3} - FPS: {4}", 100d * (timeDraw / timeTotal), 100d * (timeUpdate / timeTotal), avgDrawMs, _isRunningSlowly ? "*" : string.Empty, CurrentFPS, Version);
//#else
//            Window.Title = string.Format("ClassicUO {5} - Draw:{0:0.0}% Update:{1:0.0}% Fixed:{2:0.0}% AvgDraw:{3:0.0}ms {4} - FPS: {4}", 100d * (timeDraw / timeTotal), 100d * (timeUpdate / timeTotal), avgDrawMs, _isRunningSlowly ? "*" : string.Empty, CurrentFPS, Version);
//#endif
//        }


//        private void OnNetworkUpdate(double totalMS, double frameMS)
//        {
//            if (NetClient.LoginSocket.IsDisposed && NetClient.LoginSocket.IsConnected)
//                NetClient.LoginSocket.Disconnect();
//            else if (!NetClient.Socket.IsConnected)
//            {
//                NetClient.LoginSocket.Update();
//                UpdateSockeStats(NetClient.LoginSocket, totalMS);
//            }
//            else if (!NetClient.Socket.IsDisposed)
//            {
//                NetClient.Socket.Update();
//                UpdateSockeStats(NetClient.Socket, totalMS);
//            }
//        }

//        private void UpdateSockeStats(NetClient socket, double totalMS)
//        {
//            if (_statisticsTimer < totalMS)
//            {
//                socket.Statistics.Update();
//                _statisticsTimer = totalMS + 500;
//            }
//        }

//        private static int _previousHour = -1;
//        private static int _Differential;

//        private static int Differential //to use in all cases where you rectify normal clocks obtained with utctimer!
//        {
//            get
//            {
//                if (_previousHour != DateTime.UtcNow.Hour)
//                {
//                    _previousHour = DateTime.UtcNow.Hour;
//                    _Differential = DateTimeOffset.Now.Offset.Hours;
//                }

//                return _Differential;
//            }
//        }

//        public static DateTime CurrDateTime
//        {
//            get { return DateTime.UtcNow.AddHours(Differential); }
//        }
//    }
//}