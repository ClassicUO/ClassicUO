#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
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

#define DEV_BUILD

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Renderer.UI;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using ClassicUO.Utility.Platforms;

using CUO_API;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Newtonsoft.Json;

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

    internal class Engine : Microsoft.Xna.Framework.Game
    {
        private const int MIN_FPS = 15;
        private const int MAX_FPS = 250;
        private const int LOGIN_SCREEN_FPS = 60;
        private static GameWindow _window;
        private static int _fpsLimit = 30;
        private static Engine _engine;

        public static bool DebugFocus = false;
        public static string SettingsFile = "settings.json";
        private readonly GraphicsDeviceManager _graphicDeviceManager;
        private readonly bool _isHighDPI;
        private readonly Settings _settings;
        private AuraManager _auraManager;
        private UltimaBatcher2D _batcher;
        private double _currentFpsTime;
        private DebugInfo _debugInfo;
        private InputManager _inputManager;
        private bool _isRunningSlowly;
        private ProfileManager _profileManager;
        private SceneManager _sceneManager;
        private double _statisticsTimer;
        private double _time;
        private int _totalFrames;
        private UIManager _uiManager;

        private Engine(string[] args)
        {
            Instance = this;

            // By default try to load settings from main settings file
            _settings = ConfigurationResolver.Load<Settings>(Path.Combine(ExePath, SettingsFile));

            // Try to apply any settings passed from the command-line/shortcut to what we loaded from file
            // NOTE: If nothing was loaded from settings file (file doesn't exist), then it will create
            //   a new settings object and populate it with the passed settings
            _settings = ArgsParser(args, _settings);

            // If no still no settings after loading a file and parsing command-line settings,
            //   then show an error, generate default settings file and exit
            if (_settings == null)
            {
                SDL.SDL_ShowSimpleMessageBox(SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_INFORMATION, "No `" + SettingsFile + "`", "A `" + SettingsFile + "` has been created into ClassicUO main folder.\nPlease fill it!", SDL.SDL_GL_GetCurrentWindow());
                Log.Message(LogTypes.Trace, SettingsFile + " file not found");
                _settings = new Settings();
                _settings.Save();
                IsQuitted = true;

                return;
            }

            // Validate resulting settings object
            // NOTE: Check that at least `UltimaOnlineDirectory` and `ClientVersion` properties are set
            //   to some other than default values
            Settings defaultSettings = new Settings();
            bool settingsAreValid = _settings.UltimaOnlineDirectory != defaultSettings.UltimaOnlineDirectory;

            if (_settings.ClientVersion == defaultSettings.ClientVersion)
                settingsAreValid = false;

            // If settings are invalid, then show an error and exit
            if (!settingsAreValid)
            {
                SDL.SDL_ShowSimpleMessageBox(
                                             SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_INFORMATION,
                                             "Invalid settings",
                                             "Please, check your settings.\nYou should at least set `ultimaonlinedirectory` and `clientversion`.",
                                             SDL.SDL_GL_GetCurrentWindow()
                                            );
                Log.Message(LogTypes.Trace, "Invalid settings");
                IsQuitted = true;

                return;
            }


            TargetElapsedTime = TimeSpan.FromSeconds(1.0f / MAX_FPS);
            IsFixedTimeStep = _settings.FixedTimeStep;

            _graphicDeviceManager = new GraphicsDeviceManager(this);
            _graphicDeviceManager.PreparingDeviceSettings += (sender, e) => e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.DiscardContents;

            if (_graphicDeviceManager.GraphicsDevice.Adapter.IsProfileSupported(GraphicsProfile.HiDef))
                _graphicDeviceManager.GraphicsProfile = GraphicsProfile.HiDef;

            _graphicDeviceManager.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
            _graphicDeviceManager.SynchronizeWithVerticalRetrace = false;
            _graphicDeviceManager.ApplyChanges();

            _isHighDPI = Environment.GetEnvironmentVariable("FNA_GRAPHICS_ENABLE_HIGHDPI") == "1";
            _window = Window;

            Window.ClientSizeChanged += (sender, e) =>
            {
                int width = Window.ClientBounds.Width;
                int height = Window.ClientBounds.Height;

                if (_isHighDPI)
                {
                    width *= 2;
                    height *= 2;
                }

                if (! IsMaximized)
                    _engine._profileManager.Current.WindowClientBounds = new Point(width, height);

                SetPreferredBackBufferSize(width, height);

                WorldViewportGump gump = _uiManager.GetControl<WorldViewportGump>();

                if (gump != null && _profileManager.Current.GameWindowFullSize)
                {
                    gump.ResizeWindow(new Point(WindowWidth, WindowHeight));
                    gump.X = -5;
                    gump.Y = -5;
                }
            };

            Window.AllowUserResizing = true;
            IsMouseVisible = true;

            Window.Title = $"ClassicUO - {Version}";
        }

        public bool IsQuitted { get; private set; }

        public bool DisableUpdateWindowCaption { get; set; }

        public static UltimaBatcher2D Batcher => _engine._batcher;

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

                    _engine.IntervalFixedUpdate = 1000.0 / _fpsLimit;

                    //_engine.TargetElapsedTime = TimeSpan.FromSeconds(1.0 / _fpsLimit);
                }
            }
        }

        public static Version Version { get; } = Assembly.GetExecutingAssembly().GetName().Version;

        public static int CurrentFPS { get; private set; }
        public static int FPSMin { get; private set; } = int.MaxValue;
        public static int FPSMax { get; private set; }

        public static bool AllowWindowResizing
        {
            get => _window.AllowUserResizing;
            set => _window.AllowUserResizing = value;
        }

        /// <summary>
        ///     Total game time in milliseconds
        /// </summary>
        public static uint Ticks { get; private set; }

        public static uint[] FrameDelay { get; } = new uint[2];

        public static bool IsMaximized
        {
            get
            {
                IntPtr wnd = SDL.SDL_GL_GetCurrentWindow();
                uint flags = SDL.SDL_GetWindowFlags(wnd);

                return ((flags & (uint) SDL.SDL_WindowFlags.SDL_WINDOW_MAXIMIZED) != 0);
            }
            set
            {
                if (IsMaximized == value)
                    return;

                IntPtr wnd = SDL.SDL_GL_GetCurrentWindow();

                if (value)
                    SDL.SDL_MaximizeWindow(wnd);
                else
                    SDL.SDL_RestoreWindow(wnd);
            }
        }

        public static Engine Instance { get; private set; }

        public static int ThreadID { get; private set; }

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

        public static void SetPreferredBackBufferSize(int width, int height)
        {
            _engine._graphicDeviceManager.PreferredBackBufferWidth = width;
            _engine._graphicDeviceManager.PreferredBackBufferHeight = height;
            _engine._graphicDeviceManager.ApplyChanges();
        }

        public static UIManager UI => _engine._uiManager;

        public static InputManager Input => _engine._inputManager;

        public static ProfileManager Profile => _engine._profileManager;

        public static Settings GlobalSettings => _engine._settings;

        public static SceneManager SceneManager => _engine._sceneManager;

        public static AuraManager AuraManager => _engine._auraManager;

        public static string ExePath { get; private set; }

        public static DebugInfo DebugInfo => _engine._debugInfo;

        public static bool IsRunningSlowly => _engine._isRunningSlowly;

        public static void DropFpsMinMaxValues()
        {
            FPSMax = 0;
            FPSMin = int.MaxValue;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetDllDirectory(string lpPathName);

        private static void Main(string[] args)
        {
            Configure();

            //new Updater()
            //   .Check();

            if (CheckUpdate(args))
                return;

            Thread.CurrentThread.Name = "CUO_MAIN_THREAD";
            ThreadID = Thread.CurrentThread.ManagedThreadId;

            using (_engine = new Engine(args))
                if (!_engine.IsQuitted)
                    _engine.Run();
        }

        private static bool CheckUpdate(string[] args)
        {
            string path = string.Empty;
            string action = string.Empty;
            int pid = -1;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--source" && i < args.Length - 1)
                    path = args[i + 1];
                else if (args[i] == "--action" && i < args.Length - 1)
                    action = args[i + 1];
                else if (args[i] == "--pid" && i < args.Length - 1) pid = int.Parse(args[i + 1]);
            }

            if (action == "update")
            {
                Log.Message(LogTypes.Trace, "ClassicUO Updating...", ConsoleColor.Yellow);

                try
                {
                    Process proc = Process.GetProcessById(pid);
                    proc.Kill();
                    proc.WaitForExit(5000);
                }
                catch
                {
                }


                //File.SetAttributes(Path.GetDirectoryName(path), FileAttributes.Normal);

                foreach (string file in Directory.EnumerateFiles(ExePath, "*", SearchOption.AllDirectories))
                {
                    string sub = Path.Combine(file, file.Replace(ExePath, path));
                    File.Copy(file, sub, true);
                    Console.WriteLine("COPIED {0} over {1}", file, sub);
                }

                string prefix = Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix ? "mono " : string.Empty;

                new Process
                {
                    StartInfo =
                    {
                        WorkingDirectory = path,
                        FileName = prefix + Path.Combine(path, "ClassicUO.exe"),
                        UseShellExecute = false,
                        Arguments =
                            $"--source \"{ExePath}\" --pid {Process.GetCurrentProcess().Id} --action cleanup"
                    }
                }.Start();

                return true;
            }

            if (action == "cleanup")
            {
                try
                {
                    Process.GetProcessById(pid);
                    Thread.Sleep(1000);
                    Process.GetProcessById(pid).Kill();
                }
                catch
                {
                }

                try
                {
                    Directory.Delete(path, true);
                }
                catch (Exception e)
                {
                }

                Log.Message(LogTypes.Trace, "ClassicUO updated successful!", ConsoleColor.Green);
            }

            return false;
        }

        private static Settings ArgsParser(string[] args, Settings settings = null)
        {
            if (args.Length > 1)
            {
                if (settings == null) settings = new Settings();

                for (int i = 0; i < args.Length - 1; i += 2)
                {
                    string cmd = args[i].ToLower();

                    if (cmd.Length <= 1 && cmd[0] != '-')
                        continue;

                    cmd = cmd.Remove(0, 1);
                    string value = args[i + 1];

                    Log.Message(LogTypes.Trace, $"ARG: {cmd}, VALUE: {value}");

                    switch (cmd)
                    {
                        case "username":
                            settings.Username = value;

                            break;

                        case "password":
                            settings.Password = Crypter.Encrypt(value);

                            break;

                        case "password_enc": // Non-standard setting, similar to `password` but for already encrypted password
                            settings.Password = value;

                            break;

                        case "ip":
                            settings.IP = value;

                            break;

                        case "port":
                            settings.Port = ushort.Parse(value);

                            break;

                        case "ultimaonlinedirectory":
                        case "uopath":
                            settings.UltimaOnlineDirectory = value; // Required

                            break;

                        case "clientversion":
                            settings.ClientVersion = value; // Required

                            break;

                        case "lastcharactername":
                        case "lastcharname":
                            settings.LastCharacterName = value;

                            break;

                        case "lastservernum":
                            settings.LastServerNum = ushort.Parse(value);

                            break;

                        case "login_fps":
                        case "fps":
                            settings.MaxLoginFPS = int.Parse(value);

                            break;

                        case "debug":
                            settings.Debug = bool.Parse(value);

                            break;

                        case "profiler":
                            settings.Profiler = bool.Parse(value);

                            break;

                        //case "preload_maps":
                        //    settings.PreloadMaps = bool.Parse(value);

                            break;

                        case "saveaccount":
                            settings.SaveAccount = bool.Parse(value);

                            break;

                        case "autologin":
                            settings.AutoLogin = bool.Parse(value);

                            break;

                        case "reconnect":
                            settings.Reconnect = bool.Parse(value);

                            break;

                        case "reconnect_time":
                            settings.ReconnectTime = int.Parse(value);

                            break;

                        case "login_music":
                        case "music":
                            settings.LoginMusic = bool.Parse(value);

                            break;

                        case "login_music_volume":
                        case "music_volume":
                            settings.LoginMusicVolume = int.Parse(value);

                            break;

                        case "shard_type":
                        case "shard":
                            settings.ShardType = int.Parse(value);

                            break;

                        case "fixed_time_step":
                            settings.FixedTimeStep = bool.Parse(value);

                            break;

                        // FIXME: This is bad idea since the filename stored in `Engine.SettingsFile` is
                        //   used not only for main settings file, but for character profile settings file
                        //   as well. The "-settings" option should also have lower priority than other 
                        //   and should be processed before we overwrite options one-by-one from 
                        //   command-line arguments or from a shortcut.
                        //case "settings":
                        //    Engine.SettingsFile = value;
                        //    break;
                    }
                }
            }

            return settings;
        }

        public static void Quit()
        {
            _engine?.Exit();
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            IsQuitted = true;
            base.OnExiting(sender, args);
        }


        private static void Configure()
        {
            Log.Start(LogTypes.All);
            ExePath = Environment.CurrentDirectory; //Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                StringBuilder sb = new StringBuilder();
#if DEV_BUILD
                sb.AppendFormat("ClassicUO [dev] - v{0}\nOS: {1} {2}\nThread: {3}\n\n", Version, Environment.OSVersion.Platform, Environment.Is64BitOperatingSystem ? "x64" : "x86", Thread.CurrentThread.Name);
#else
                sb.AppendFormat("ClassicUO - v{0}\nOS: {1} {2}\nThread: {3}\n\n", Version, Environment.OSVersion.Platform, Environment.Is64BitOperatingSystem ? "x64" : "x86", Thread.CurrentThread.Name);
#endif
                sb.AppendFormat("Exception:\n{0}", e.ExceptionObject);

                Log.Message(LogTypes.Panic, e.ExceptionObject.ToString());
                string path = Path.Combine(ExePath, "Logs");

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                using (LogFile crashfile = new LogFile(path, "crash.txt"))
                {
                    crashfile.WriteAsync(sb.ToString()).RunSynchronously();

                    //SDL.SDL_ShowSimpleMessageBox(
                    //                             SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_INFORMATION,
                    //                             "An error occurred",
                    //                             $"{crashfile}\ncreated in /Logs.",
                    //                             SDL.SDL_GL_GetCurrentWindow()
                    //                            );
                }

             
            };
#endif

            // We can use the mono's dllmap feature, but 99% of people use VS to compile.
            if (Environment.OSVersion.Platform != PlatformID.MacOSX && Environment.OSVersion.Platform != PlatformID.Unix)
            {
                string libsPath = Path.Combine(ExePath, "libs", Environment.Is64BitProcess ? "x64" : "x86");
                SetDllDirectory(libsPath);
            }

            //Environment.SetEnvironmentVariable("FNA_GRAPHICS_ENABLE_HIGHDPI", "1");
            Environment.SetEnvironmentVariable("FNA_OPENGL_BACKBUFFER_SCALE_NEAREST", "1");
            Environment.SetEnvironmentVariable(SDL.SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");
            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + Path.Combine(ExePath, "Data", "Plugins"));
        }

        protected override void Initialize()
        {
            Log.NewLine();
            Log.NewLine();

            Log.Message(LogTypes.Trace, $"Starting ClassicUO - {Version}", ConsoleColor.Cyan);

            Log.NewLine();
            Log.NewLine();


            _batcher = new UltimaBatcher2D(GraphicsDevice);
            _inputManager = new InputManager();
            _uiManager = new UIManager();
            _profileManager = new ProfileManager();
            _sceneManager = new SceneManager();
            _debugInfo = new DebugInfo();

            FpsLimit = LOGIN_SCREEN_FPS;


            Log.Message(LogTypes.Trace, "Loading UI Fonts...");
            Log.PushIndent();
            Fonts.Load();
            Log.PopIndent();


            Log.Message(LogTypes.Trace, "Checking for Ultima Online installation...");
            Log.PushIndent();


            //_uiSystem = new UISystemManager(this);
            //Components.Add(_uiSystem);


            try
            {
                FileManager.UoFolderPath = GlobalSettings.UltimaOnlineDirectory;
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

            _auraManager = new AuraManager();
            _auraManager.CreateAuraTexture();

            Log.Message(LogTypes.Trace, "Network calibration...");
            Log.PushIndent();
            PacketHandlers.Load();
            //ATTENTION: you will need to enable ALSO ultimalive server-side, or this code will have absolutely no effect!
            UltimaLive.Enable();
            PacketsTable.AdjustPacketSizeByVersion(FileManager.ClientVersion);
            Log.Message(LogTypes.Trace, "Done!");
            Log.PopIndent();

            _uiManager.InitializeGameCursor();

            Log.Message(LogTypes.Trace, "Loading plugins...");
            Log.PushIndent();

            foreach (var p in GlobalSettings.Plugins)
                Plugin.Create(p);
            Log.Message(LogTypes.Trace, "Done!");
            Log.PopIndent();


            UoAssist.Start();

            PoolsManager.Initialize();

            base.Initialize();
        }


        protected override void LoadContent()
        {
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

        //protected override void Update(GameTime gameTime)
        //{
        //    if (Profiler.InContext("OutOfContext"))
        //        Profiler.ExitContext("OutOfContext");

        //    Profiler.EnterContext("Update");

        //    uint last = Ticks;
        //    Ticks = SDL.SDL_GetTicks();

        //    double totalms = Ticks; //gameTime.TotalGameTime.TotalMilliseconds;
        //    double framems = Ticks - last;  //gameTime.ElapsedGameTime.TotalMilliseconds;
 
        //    //Ticks = (long) totalms;
        //    //TicksFrame = (long) framems;

        //    _currentFpsTime += gameTime.ElapsedGameTime.TotalSeconds;

        //    if (_currentFpsTime >= 1.0)
        //    {
        //        CurrentFPS = _totalFrames;

        //        FPSMax = CurrentFPS > FPSMax || FPSMax > FpsLimit ? CurrentFPS : FPSMax;
        //        FPSMin = CurrentFPS < FPSMin && CurrentFPS != 0 ? CurrentFPS : FPSMin;

        //        _totalFrames = 0;
        //        _currentFpsTime = 0;
        //    }

        //    Profiler.ExitContext("Update");

        //    Profiler.EnterContext("FixedUpdate");
        //    OnFixedUpdate(totalms, framems);
        //    Profiler.ExitContext("FixedUpdate");



        //    //_time += framems;

        //    //if (_time >= IntervalFixedUpdate)
        //    //{
        //    //    _time %= IntervalFixedUpdate;
        //    //}
        //    //else
        //    //    SuppressDraw();


        //    base.Update(gameTime);
        //    Profiler.EnterContext("OutOfContext");
        //}

        private double _previous;
        private double _totalElapsed;


        protected override void BeginRun()
        {
            base.BeginRun();
            _previous = SDL.SDL_GetTicks();
        }


        private double _lag;

        protected override void Update(GameTime gameTime)
        {
            Profiler.EnterContext("Update");

            OnUpdate(Ticks, gameTime.ElapsedGameTime.TotalMilliseconds);
            OnFixedUpdate(Ticks, gameTime.ElapsedGameTime.TotalMilliseconds);

            base.Update(gameTime);

            
            _time += gameTime.ElapsedGameTime.TotalMilliseconds;

            if (_time > IntervalFixedUpdate)
            {
                _time %= IntervalFixedUpdate;
            }
            else
                SuppressDraw();

            Profiler.ExitContext("Update");
        }

        protected override void Draw(GameTime gameTime)
        {
            Render();
            base.Draw(gameTime);
        }


        public override void Tick()
        {
            if (Profiler.InContext("OutOfContext"))
                Profiler.ExitContext("OutOfContext");


            double current = Ticks = SDL.SDL_GetTicks();
            double elapsed = current - _previous;
            _previous = current;

            // ###############################
            // This should be the right order
            OnNetworkUpdate(current, elapsed);
            Mouse.Update();
            Plugin.Tick();
            OnUIUpdate(current, elapsed);
            // ###############################



            //OnUpdate(current, elapsed);
            //FrameworkDispatcher.Update();
            //OnFixedUpdate(current, elapsed);

            //Profiler.ExitContext("Update");
       

            
            _currentFpsTime += elapsed;

            if (_currentFpsTime >= 1000)
            {
                CurrentFPS = _totalFrames;

                FPSMax = CurrentFPS > FPSMax || FPSMax > FpsLimit ? CurrentFPS : FPSMax;
                FPSMin = CurrentFPS < FPSMin && CurrentFPS != 0 ? CurrentFPS : FPSMin;

                _totalFrames = 0;
                _currentFpsTime = 0;
            }

            base.Tick();

            //_totalElapsed += elapsed;

            //if (_totalElapsed > IntervalFixedUpdate)
            //{
            //    Render();
            //    _totalElapsed -= IntervalFixedUpdate;
            //    _isRunningSlowly = _totalElapsed > IntervalFixedUpdate;


        }

        public double IntervalFixedUpdate { get; private set; }


        private void Render()
        {
            _debugInfo.Reset();

            Profiler.EndFrame();
            Profiler.BeginFrame();

            if (Profiler.InContext("OutOfContext"))
                Profiler.ExitContext("OutOfContext");
            Profiler.EnterContext("RenderFrame");

            _totalFrames++;

            if (_sceneManager.CurrentScene != null && _sceneManager.CurrentScene.IsLoaded && !_sceneManager.CurrentScene.IsDestroyed)
                _sceneManager.CurrentScene.Draw(_batcher);

            _uiManager.Draw(_batcher);

            if (_profileManager.Current != null && _profileManager.Current.ShowNetworkStats)
            {
                if (!NetClient.Socket.IsConnected)
                {
                    NetClient.LoginSocket.Statistics.Draw(_batcher, 10, 50);
                }
                else if (!NetClient.Socket.IsDisposed)
                {
                    NetClient.Socket.Statistics.Draw(_batcher, 10, 50);
                }
            }


            Profiler.ExitContext("RenderFrame");
            Profiler.EnterContext("OutOfContext");
            UpdateWindowCaption();


            //GraphicsDevice?.Present();
        }

        public override void OnSDLEvent(ref SDL.SDL_Event ev)
        {
            _inputManager.EventHandler(ref ev);
        }

        //protected override void Draw(GameTime gameTime)
        //{
        //    //_isRunningSlowly = gameTime.IsRunningSlowly;
        //    _debugInfo.Reset();

        //    Profiler.EndFrame();
        //    Profiler.BeginFrame();

        //    if (Profiler.InContext("OutOfContext"))
        //        Profiler.ExitContext("OutOfContext");
        //    Profiler.EnterContext("RenderFrame");

        //    _totalFrames++;

        //    if (_sceneManager.CurrentScene != null && _sceneManager.CurrentScene.IsLoaded && !_sceneManager.CurrentScene.IsDestroyed)
        //        _sceneManager.CurrentScene.Draw(_batcher);

        //    _uiManager.Draw(_batcher);

        //    if (_profileManager.Current != null && _profileManager.Current.ShowNetworkStats)
        //    {
        //        if (!NetClient.Socket.IsConnected)
        //        {
        //            NetClient.LoginSocket.Statistics.Draw(_batcher, 10, 50);
        //        }
        //        else if (!NetClient.Socket.IsDisposed)
        //        {
        //            NetClient.Socket.Statistics.Draw(_batcher, 10, 50);
        //        }
        //    }

        //    Profiler.ExitContext("RenderFrame");
        //    Profiler.EnterContext("OutOfContext");
        //    UpdateWindowCaption();

        //    base.Draw(gameTime);
        //}

        private void UpdateWindowCaption()
        {
            if (!_settings.Profiler || DisableUpdateWindowCaption)
                return;

            double timeDraw = Profiler.GetContext("RenderFrame").TimeInContext;
            double timeUpdate = Profiler.GetContext("Update").TimeInContext;
            double timeOutOfContext = Profiler.GetContext("OutOfContext").TimeInContext;
            double timeTotalCheck = timeOutOfContext + timeDraw + timeUpdate;
            double timeTotal = Profiler.TrackedTime;
            double avgDrawMs = Profiler.GetContext("RenderFrame").AverageTime;

#if DEV_BUILD
            Window.Title = string.Format("ClassicUO [dev] {5} - Draw:{0:0.0}% Update:{1:0.0}% AvgDraw:{2:0.0}ms {3} - FPS: {4}", 100d * (timeDraw / timeTotal), 100d * (timeUpdate / timeTotal), avgDrawMs, _isRunningSlowly ? "*" : string.Empty, CurrentFPS, Version);
#else
            Window.Title = string.Format("ClassicUO {5} - Draw:{0:0.0}% Update:{1:0.0}% Fixed:{2:0.0}% AvgDraw:{3:0.0}ms {4} - FPS: {4}", 100d * (timeDraw / timeTotal), 100d * (timeUpdate / timeTotal), avgDrawMs, gameTime.IsRunningSlowly ? "*" : string.Empty, CurrentFPS, Version);
#endif
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
            Scene scene = _sceneManager.CurrentScene;

            if (scene != null && scene.IsLoaded)
            {
                if (scene.IsDestroyed)
                    _sceneManager.Switch();
                else
                    scene.Update(totalMS, frameMS);
            }
        }

        private void OnFixedUpdate(double totalMS, double frameMS)
        {
            if (_sceneManager.CurrentScene != null && _sceneManager.CurrentScene.IsLoaded && !_sceneManager.CurrentScene.IsDestroyed)
                _sceneManager.CurrentScene.FixedUpdate(totalMS, frameMS);
        }
    }
}