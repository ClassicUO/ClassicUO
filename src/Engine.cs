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

namespace ClassicUO
{
    public class DebugInfo
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

    public class Engine : Microsoft.Xna.Framework.Game
    { 
        private const int MIN_FPS = 15;
        private const int MAX_FPS = 250;
        private const int LOGIN_SCREEN_FPS = 60;

        //private const string FORMATTED_STRING = "FPS: {0}\nObjects: {1}\nCalls: {2}\nMerged: {3}\nFlush: {7}\nPos: {4}\nSelected: {5}\nStats: {6}";
        //private const string FORMAT_1 = "FPS: {0}\nObjects: {1}\nCalls: {2}\nMerged: {3}\n";
        //private const string FORMAT_2 = "Flush: {0}\nPos: {1}\nSelected: {2}\nStats: {3}";

        //private const string DEBUG_STRING_1 = "- FPS: {0}\n- Rendered: {1} mobiles, {2} items, {3} statics, {4} multi, {5} lands, {6} effects\n";
        //private const string DEBUG_STRING_2 = "- CharPos: {0}    Mouse: {1}    InGamePos: {2}\n";
        //private const string DEBUG_STRING_3 = "- Selected: {0}";


        private static int _fpsLimit = MIN_FPS - 1;
        private static Engine _engine;
        private readonly GraphicsDeviceManager _graphicDeviceManager;
        private readonly StringBuilder _sb = new StringBuilder();
        private Batcher2D _batcher;
        private double _currentFpsTime;
        //private RenderedText _infoText;
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

        private Engine()
        {
            TargetElapsedTime = TimeSpan.FromSeconds(1.0f / MAX_FPS);
            _graphicDeviceManager = new GraphicsDeviceManager(this);
            _graphicDeviceManager.PreparingDeviceSettings += (sender, e) => e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;

            if (_graphicDeviceManager.GraphicsDevice.Adapter.IsProfileSupported(GraphicsProfile.HiDef))
                _graphicDeviceManager.GraphicsProfile = GraphicsProfile.HiDef;
            _graphicDeviceManager.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
            _graphicDeviceManager.SynchronizeWithVerticalRetrace = false;
            _graphicDeviceManager.PreferredBackBufferWidth = 640; // should be changed by settings file
            _graphicDeviceManager.PreferredBackBufferHeight = 480; // should be changed by settings file
            _graphicDeviceManager.ApplyChanges();

            Window.ClientSizeChanged += (sender, e) =>
            {
                _graphicDeviceManager.PreferredBackBufferWidth = Window.ClientBounds.Width;
                _graphicDeviceManager.PreferredBackBufferHeight = Window.ClientBounds.Height;
                _graphicDeviceManager.ApplyChanges();
            };
            Window.AllowUserResizing = true;
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
            get => _engine._graphicDeviceManager.IsFullScreen;
            set
            {
                _engine._graphicDeviceManager.IsFullScreen = value;
                _engine._graphicDeviceManager.ApplyChanges();
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

        public static Assembly Assembly { get; private set; }

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

            Assembly = Assembly.GetExecutingAssembly();
            ExePath = Path.GetDirectoryName(Assembly.Location);

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
        }



        protected override void Initialize()
        {
            _settings = ConfigurationResolver.Load<Settings>(Path.Combine(ExePath, "settings.json"));

            if (_settings == null)
            {
                Log.Message(LogTypes.Trace, "settings.json file was not found creating default");
                _settings = new Settings();
                _settings.Save();
                Quit();

                return;
            }

            Log.Message(LogTypes.Trace, "Checking for Ultima Online installation...");

            try
            {
                FileManager.UoFolderPath = _settings.UltimaOnlineDirectory;
            }
            catch (FileNotFoundException e)
            {
                Log.Message(LogTypes.Error, "Wrong Ultima Online installation folder.");

                throw e;
            }

            Log.Message(LogTypes.Trace, "Done!");
            Log.Message(LogTypes.Trace, $"Ultima Online installation folder: {FileManager.UoFolderPath}");
            Log.Message(LogTypes.Trace, "Loading files...");
            FileManager.LoadFiles();
            uint[] hues = FileManager.Hues.CreateShaderColors();
            _batcher = new Batcher2D(GraphicsDevice);
            Texture2D texture0 = new Texture2D(GraphicsDevice, 32, FileManager.Hues.HuesCount);
            texture0.SetData(hues, 0, 32 * FileManager.Hues.HuesCount);
            Texture2D texture1 = new Texture2D(GraphicsDevice, 32, FileManager.Hues.HuesCount);
            texture1.SetData(hues, 32 * FileManager.Hues.HuesCount, 32 * FileManager.Hues.HuesCount);
            GraphicsDevice.Textures[1] = texture0;
            GraphicsDevice.Textures[2] = texture1;
         
            _inputManager = new InputManager();
            _uiManager = new UIManager();
            _profileManager = new ProfileManager();
            _sceneManager = new SceneManager();
            Log.Message(LogTypes.Trace, "Network calibration...");
            PacketHandlers.Load();
            PacketsTable.AdjustPacketSizeByVersion(FileManager.ClientVersion);
            Log.Message(LogTypes.Trace, "Done!");

            FpsLimit = LOGIN_SCREEN_FPS;

            _debugInfo = new DebugInfo();

            //_infoText = new RenderedText
            //{
            //    IsUnicode = true,
            //    Font = 1,
            //    FontStyle = FontStyle.BlackBorder,
            //    Align = TEXT_ALIGN_TYPE.TS_LEFT,
            //    Hue = 0x35,
            //    Cell = 31,
            //    //MaxWidth = 500
            //};

            _uiManager.Add(new DebugGump());
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
            _time += (float) framems;

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
            //_sb.Clear();

            //_sb.AppendFormat(DEBUG_STRING_1, CurrentFPS, _debugInfo.MobilesRendered, _debugInfo.ItemsRendered, _debugInfo.StaticsRendered, _debugInfo.MultiRendered, _debugInfo.LandsRendered, _debugInfo.EffectsRendered);
            //_sb.AppendFormat(DEBUG_STRING_2, World.InGame ? World.Player.Position : Position.Invalid, Mouse.Position, _sceneManager.CurrentScene is GameScene gs ? gs.MouseOverWorldPosition : Point.Zero);
            //_sb.AppendFormat(DEBUG_STRING_3, _sceneManager.CurrentScene is GameScene gs1 && gs1.SelectedObject != null ? gs1.SelectedObject.ToString() : "");

            ////_sb.ConcatFormat(FORMAT_1, CurrentFPS, _sceneManager.CurrentScene.RenderedObjectsCount, totalCalls, totalMerged);
            ////_sb.ConcatFormat(FORMAT_2, totalFlushes, World.Player == null ? string.Empty : World.Player.Position.ToString(), _sceneManager.CurrentScene is GameScene gameScene && gameScene.SelectedObject != null ? gameScene.SelectedObject.ToString() : string.Empty, string.Empty);
            //_infoText.Text = _sb.ToString();
            //_infoText.Draw(_batcher, new Point(20, 0));
            _batcher.End();
            Profiler.ExitContext("RenderFrame");
            Profiler.EnterContext("OutOfContext");
            UpdateWindowCaption(gameTime);
        }

        private void UpdateWindowCaption(GameTime gameTime)
        {
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