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
using System.Text;

using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Gumps;
using ClassicUO.Game.Scenes;
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
    public class Engine : Microsoft.Xna.Framework.Game
    {
        private const int MIN_FPS = 15;
        private const int MAX_FPS = 250;
        private const string FORMATTED_STRING = "FPS: {0}\nObjects: {1}\nCalls: {2}\nMerged: {3}\nFlush: {7}\nPos: {4}\nSelected: {5}\nStats: {6}";
        //private const string FORMATTED_STRING = "FPS: {0}\nObjects: {1}\nCalls: {2}\nMerged: {3}\nFlush: {7}\nPos: {4}\nSelected: {5}\nStats: {6}";
        private const string FORMAT_1 = "FPS: {0}\nObjects: {1}\nCalls: {2}\nMerged: {3}\n";
        private const string FORMAT_2 = "Flush: {0}\nPos: {1}\nSelected: {2}\nStats: {3}";
        private static int _fpsLimit = MIN_FPS;
        private static Engine _engine;
        private readonly GraphicsDeviceManager _graphicDeviceManager;
        private Batcher2D _batcher;
        private double _currentFpsTime;
        private RenderedText _infoText;
        private readonly StringBuilder _sb = new StringBuilder();
        //private SpriteBatch3D _sb3D;
        //private SpriteBatchUI _sbUI;
        private double _statisticsTimer;
        private float _time;
        private int _totalFrames;
        private UIManager _uiManager;

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

        protected float IntervalFixedUpdate => 1000.0f / FpsLimit;

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
                }
            }
        }

        public static int CurrentFPS { get; private set; }

        /// <summary>
        /// Total game time in milliseconds
        /// </summary>
        public static long Ticks { get; private set; }

        /// <summary>
        /// Milliseconds from last frame
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


        public static void Start()
        {
            using (_engine = new Engine())
                _engine.Run();
        }

        protected override void Initialize()
        {
            Settings settings = ConfigurationResolver.Load<Settings>(Path.Combine(Bootstrap.ExeDirectory, "settings.json"));

            if (settings == null)
            {
                Log.Message(LogTypes.Trace, "settings.json file was not found creating default");
                settings = new Settings();
                settings.Save();
                Process.Start("notepad.exe", "settings.json");
                Exit();

                return;
            }

            Service.Register(settings);
            Log.Message(LogTypes.Trace, "Checking for Ultima Online installation...");

            try
            {
                FileManager.UoFolderPath = settings.UltimaOnlineDirectory;
            }
            catch (FileNotFoundException e)
            {
                Log.Message(LogTypes.Error, "Wrong Ultima Online installation folder.");

                throw e;
            }

            Log.Message(LogTypes.Trace, "Done!");
            Log.Message(LogTypes.Trace, $"Ultima Online installation folder: {FileManager.UoFolderPath}");
            Log.Message(LogTypes.Trace, "Loading files...");
            Stopwatch stopwatch = Stopwatch.StartNew();
            FileManager.LoadFiles();
            uint[] hues = Hues.CreateShaderColors();
            _batcher = new Batcher2D(GraphicsDevice);
            Texture2D texture0 = new Texture2D(GraphicsDevice, 32, Hues.HuesCount);
            texture0.SetData(hues, 0, 32 * Hues.HuesCount);
            Texture2D texture1 = new Texture2D(GraphicsDevice, 32, Hues.HuesCount);
            texture1.SetData(hues, 32 * Hues.HuesCount, 32 * Hues.HuesCount);
            GraphicsDevice.Textures[1] = texture0;
            GraphicsDevice.Textures[2] = texture1;
            Log.Message(LogTypes.Trace, $"Files loaded in: {stopwatch.ElapsedMilliseconds} ms!");
            stopwatch.Stop();
            InputManager.Initialize();

            _uiManager = new UIManager();

            //Register Command Stack          
            Log.Message(LogTypes.Trace, "Network calibration...");
            PacketHandlers.Load();
            PacketsTable.AdjustPacketSizeByVersion(FileManager.ClientVersion);
            Log.Message(LogTypes.Trace, "Done!");
            FpsLimit = settings.MaxFPS;

            _infoText = new RenderedText
            {
                IsUnicode = true,
                Font = 3,
                FontStyle = FontStyle.BlackBorder,
                Align = TEXT_ALIGN_TYPE.TS_LEFT,
                MaxWidth = 150
            };
            base.Initialize();
        }

        protected override void LoadContent()
        {
            SceneManager.ChangeScene(ScenesType.Login);
            base.LoadContent();
        }

        protected override void UnloadContent()
        {
            InputManager.Unload();
            SceneManager.CurrentScene?.Unload();
            Service.Get<Settings>().Save();
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
                SuppressDraw();

            Profiler.EnterContext("OutOfContext");
        }

        protected override void Draw(GameTime gameTime)
        {
            Profiler.EndFrame();
            Profiler.BeginFrame();

            if (Profiler.InContext("OutOfContext"))
                Profiler.ExitContext("OutOfContext");
            Profiler.EnterContext("RenderFrame");
            _totalFrames++;
            SceneManager.CurrentScene.Draw(_batcher);
            GraphicsDevice.Clear(Color.Transparent);
            int totalCalls = _batcher.Calls;
            int totalMerged = _batcher.Merged;
            int totalFlushes = _batcher.FlushCount;
            _batcher.Begin();
            UI.Draw(_batcher);
            totalCalls += _batcher.Calls;
            totalMerged += _batcher.Merged;
            totalFlushes += _batcher.FlushCount;
            _sb.Clear();
            _sb.ConcatFormat(FORMAT_1, CurrentFPS, SceneManager.CurrentScene.RenderedObjectsCount, totalCalls, totalMerged);
            _sb.ConcatFormat(FORMAT_2, totalFlushes, World.Player == null ? string.Empty : World.Player.Position.ToString(), SceneManager.CurrentScene is GameScene gameScene && gameScene.SelectedObject != null ? gameScene.SelectedObject.ToString() : string.Empty, string.Empty);
            _infoText.Text = _sb.ToString();
            _infoText.Draw(_batcher, new Point(Window.ClientBounds.Width - 150, 20));
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
            SceneManager.CurrentScene.Update(totalMS, frameMS);
        }

        private void OnFixedUpdate(double totalMS, double frameMS)
        {
            SceneManager.CurrentScene.FixedUpdate(totalMS, frameMS);
        }

        public static void Quit()
        {
            _engine.Exit();
        }
    }
}