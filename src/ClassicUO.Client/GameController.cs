// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Network.Encryption;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using static SDL3.SDL;

namespace ClassicUO
{
    internal unsafe class GameController : Microsoft.Xna.Framework.Game, IGameController
    {
        private SDL_EventFilter _filter;
        private readonly float[] _intervalFixedUpdate = new float[2];
        private double _totalElapsed, _currentFpsTime;
        private uint _totalFrames;
        private UltimaBatcher2D _uoSpriteBatch;
        private RenderTargets _renderTargets = new();
        private readonly RenderLists _renderLists = new();
        private readonly RenderPipeline _renderPipeline = new();
        private Rendering.UIPass _uiPass;
        private readonly Rendering.CompositePass _compositePass = new();
        private bool _suppressedDraw;

        private InputDispatcher _input;

        public GameController(IPluginHost pluginHost)
        {
            GraphicManager = new GraphicsDeviceManager(this);

            GraphicManager.PreparingDeviceSettings += (sender, e) =>
            {
                e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage =
                    RenderTargetUsage.DiscardContents;
            };

            GraphicManager.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
            SetVSync(false);

            Window.AllowUserResizing = true;
            Window.Title = $"ClassicUO - {CUOEnviroment.Version}";
            IsMouseVisible = Settings.GlobalSettings.RunMouseInASeparateThread;

            IsFixedTimeStep = false;
            TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0 / 250.0);
            PluginHost = pluginHost;
        }

        // --- Core properties ---
        public Scene Scene { get; private set; }
        public AudioManager Audio { get; private set; }
        public UOAssets UO { get; } = new UOAssets();
        public IPluginHost PluginHost { get; private set; }
        public GraphicsDeviceManager GraphicManager { get; }
        public WindowManager WindowManager { get; private set; }
        internal IUIManager UI { get; private set; }

        public Rectangle ClientBounds
        {
            get
            {
                var window_rectangle = Window.ClientBounds;
                return new Rectangle(
                    window_rectangle.X,
                    window_rectangle.Y,
                    (int)((float)(window_rectangle.Width) / DpiScale),
                    (int)((float)(window_rectangle.Height) / DpiScale)
                );
            }
        }

        public uint[] FrameDelay { get; } = new uint[2];

        private readonly List<(uint, Action)> _queuedActions = new();

        public void EnqueueAction(uint time, Action action)
        {
            _queuedActions.Add((Time.Ticks + time, action));
        }

        // --- DPI / Scale ---
        private float _screenScale = Settings.GlobalSettings.ScreenScale;
        public float ScreenScale
        {
            get => _screenScale;
            set
            {
                if (value != _screenScale)
                {
                    _screenScale = value;
                    UO.GameCursor?.CreateGraphic(DpiScale);
                }
            }
        }

        public float DpiScale
        {
            get => SDL_GetWindowDisplayScale(Window.Handle) * ScreenScale;
        }

        internal float DisplayScale { get; set; }

        public int ScaleWithDpi(int value, float previousDpi = 1)
        {
            return (int)Math.Round((value / previousDpi) * DpiScale);
        }

        // --- Lifecycle ---
        protected override void Initialize()
        {
            if (GraphicManager.GraphicsDevice.Adapter.IsProfileSupported(GraphicsProfile.HiDef))
            {
                GraphicManager.GraphicsProfile = GraphicsProfile.HiDef;
            }

            GraphicManager.ApplyChanges();

            SetRefreshRate(Settings.GlobalSettings.FPS);
            _uoSpriteBatch = new UltimaBatcher2D(GraphicsDevice);

            _input = new InputDispatcher(this);
            WindowManager = new WindowManager(this);

            _filter = _input.HandleSdlEvent;
            SDL_SetEventFilter(_filter, IntPtr.Zero);

            Microsoft.Xna.Framework.Input.TextInputEXT.StartTextInput();

            DisplayScale = DpiScale;

            _uiPass = new Rendering.UIPass(this);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            Fonts.Initialize(GraphicsDevice);
            SolidColorTextureCache.Initialize(GraphicsDevice);

            var bytes = Loader.GetBackgroundImage().ToArray();
            using var ms = new MemoryStream(bytes);
            _renderTargets.InitializeBackground(Texture2D.FromStream(GraphicsDevice, ms));

            UO.Load(this);
            Audio = new AudioManager(Settings.GlobalSettings, this, UO.World.Profile);
            Audio.Initialize();
            Settings.GlobalSettings.Encryption = (byte)UO.World.Network.Load(UO.FileManager.Version, (EncryptionType)Settings.GlobalSettings.Encryption);

            Log.Trace("Loading plugins...");
            PluginHost?.Initialize();

            foreach (string p in Settings.GlobalSettings.Plugins)
            {
                Plugin.Create(p);
            }
            _input.SetPluginsReady();

            Log.Trace("Done!");

            UI = UO.World.Context.UI;

            SetScene(new LoginScene(UO.World));
            WindowManager.SetPositionBySettings();
        }

        protected override void UnloadContent()
        {
            SDL_GetWindowBordersSize(Window.Handle, out int top, out int left, out _, out _);

            Settings.GlobalSettings.WindowPosition = new Point(
                Math.Max(0, Window.ClientBounds.X - left),
                Math.Max(0, Window.ClientBounds.Y - top)
            );

            Audio?.StopMusic();
            Settings.GlobalSettings.Save();
            Plugin.OnClosing();

            UO.Unload();

            base.UnloadContent();
        }

        // --- Scene management ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetScene<T>() where T : Scene
        {
            return Scene as T;
        }

        public void SetScene(Scene scene)
        {
            Scene?.Dispose();
            Scene = scene;
            Scene?.Load();
        }

        // --- Window management (delegated) ---
        public void SetWindowTitle(string title) => WindowManager.SetTitle(title);
        public void SetWindowSize(int width, int height) => WindowManager.SetSize(width, height);
        public void SetWindowBorderless(bool borderless) => WindowManager.SetBorderless(borderless, UI);
        public void MaximizeWindow() => WindowManager.Maximize();
        public bool IsWindowMaximized() => WindowManager.IsMaximized();
        public void RestoreWindow() => WindowManager.Restore();
        public void SetWindowPositionBySettings() => WindowManager.SetPositionBySettings();

        // --- Refresh rate ---
        public void SetVSync(bool value)
        {
            GraphicManager.SynchronizeWithVerticalRetrace = value;
        }

        public void SetRefreshRate(int rate)
        {
            if (rate < Constants.MIN_FPS)
                rate = Constants.MIN_FPS;
            else if (rate > Constants.MAX_FPS)
                rate = Constants.MAX_FPS;

            float frameDelay;

            if (rate == Constants.MIN_FPS)
            {
                // The "real" UO framerate is 12.5. Treat "12" as "12.5" to match.
                frameDelay = 80;
            }
            else
            {
                frameDelay = 1000.0f / rate;
            }

            FrameDelay[0] = FrameDelay[1] = (uint)frameDelay;
            FrameDelay[1] = FrameDelay[1] >> 1;

            Settings.GlobalSettings.FPS = rate;

            _intervalFixedUpdate[0] = frameDelay;
            _intervalFixedUpdate[1] = 217; // 5 FPS
        }

        // --- Game loop ---
        protected override void Update(GameTime gameTime)
        {
            if (Profiler.InContext(Profiler.ProfilerContext.OUT_OF_CONTEXT))
            {
                Profiler.ExitContext(Profiler.ProfilerContext.OUT_OF_CONTEXT);
            }

            Time.Ticks = (uint)gameTime.TotalGameTime.TotalMilliseconds;
            Time.Delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Mouse.Update(Window.Handle, GraphicManager.PreferredBackBufferWidth, GraphicManager.PreferredBackBufferHeight, Window.ClientBounds.Width, Window.ClientBounds.Height, DpiScale);

            var network = UO.World.Network;

            ArraySegment<byte> data;
            while ((data = network.CollectAvailableData()).Count > 0)
            {
                var packetsCount = PacketHandlers.Handler.ParsePackets(network, UO.World, data);
                network.Statistics.TotalPacketsReceived += (uint)packetsCount;
            }
           
            network.Flush();

            Plugin.Tick();

            if (Scene != null && Scene.IsLoaded && !Scene.IsDestroyed)
            {
                Profiler.EnterContext(Profiler.ProfilerContext.UPDATE_WORLD);
                Scene.Update();
                Profiler.ExitContext(Profiler.ProfilerContext.UPDATE_WORLD);
            }

            UI.Update();

            _totalElapsed += gameTime.ElapsedGameTime.TotalMilliseconds;
            _currentFpsTime += gameTime.ElapsedGameTime.TotalMilliseconds;

            if (_currentFpsTime >= 1000)
            {
                CUOEnviroment.CurrentRefreshRate = _totalFrames;
                _totalFrames = 0;
                _currentFpsTime = 0;
            }

            double x = _intervalFixedUpdate[
                !IsActive
                && UO.World?.Profile?.CurrentProfile is { ReduceFPSWhenInactive: true }
                    ? 1
                    : 0
            ];
            _suppressedDraw = false;

            if (_totalElapsed > x)
            {
                _totalElapsed %= x;
            }
            else
            {
                _suppressedDraw = true;
                SuppressDraw();

                if (!gameTime.IsRunningSlowly)
                {
                    Thread.Sleep(1);
                }
            }

            UO.GameCursor?.Update();
            Audio?.Update();

            for (var i = _queuedActions.Count - 1; i >= 0; i--)
            {
                (var time, var fn) = _queuedActions[i];

                if (Time.Ticks > time)
                {
                    fn();
                    _queuedActions.RemoveAt(i);
                    break;
                }
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            _renderTargets.EnsureSizes(
                GraphicsDevice,
                new Rectangle(0, 0, GraphicManager.PreferredBackBufferWidth, GraphicManager.PreferredBackBufferHeight),
                Scene.Camera.Bounds,
                DpiScale
            );

            Profiler.EndFrame();
            Profiler.BeginFrame();

            if (Profiler.InContext(Profiler.ProfilerContext.OUT_OF_CONTEXT))
            {
                Profiler.ExitContext(Profiler.ProfilerContext.OUT_OF_CONTEXT);
            }

            Profiler.EnterContext(Profiler.ProfilerContext.RENDER_FRAME);

            _totalFrames++;

            // Scene-specific passes (Lights, World, etc.)
            if (Scene != null && Scene.IsLoaded && !Scene.IsDestroyed)
            {
                Scene.BuildRenderPasses(_renderPipeline, _renderTargets);
            }

            _renderPipeline.Add(_uiPass);
            _renderPipeline.Add(_compositePass);

            _renderPipeline.Execute(_uoSpriteBatch, _renderTargets);

            Profiler.ExitContext(Profiler.ProfilerContext.RENDER_FRAME);
            Profiler.EnterContext(Profiler.ProfilerContext.OUT_OF_CONTEXT);

            Plugin.ProcessDrawCmdList(GraphicsDevice);

            base.Draw(gameTime);
        }

        protected override bool BeginDraw()
        {
            return !_suppressedDraw && base.BeginDraw();
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            Scene?.Dispose();
            base.OnExiting(sender, args);
        }

        // --- Utility ---
        internal void TakeScreenshot()
        {
            string screenshotsFolder = FileSystemHelper.CreateFolderIfNotExists(
                CUOEnviroment.ExecutablePath,
                "Data",
                "Client",
                "Screenshots"
            );

            string path = Path.Combine(
                screenshotsFolder,
                $"screenshot_{DateTime.Now:yyyy-MM-dd_hh-mm-ss}.png"
            );

            Color[] colors = new Color[
                GraphicManager.PreferredBackBufferWidth * GraphicManager.PreferredBackBufferHeight
            ];

            GraphicsDevice.GetBackBufferData(colors);

            using (
                Texture2D texture = new Texture2D(
                    GraphicsDevice,
                    GraphicManager.PreferredBackBufferWidth,
                    GraphicManager.PreferredBackBufferHeight,
                    false,
                    SurfaceFormat.Color
                )
            )
            using (FileStream fileStream = File.Create(path))
            {
                texture.SetData(colors);
                texture.SaveAsPng(fileStream, texture.Width, texture.Height);
                string message = string.Format(ResGeneral.ScreenshotStoredIn0, path);

                if (
                    UO.World?.Profile?.CurrentProfile is not { } screenshotProfile
                    || screenshotProfile.HideScreenshotStoredInMessage
                )
                {
                    Log.Info(message);
                }
                else
                {
                    GameActions.Print(UO.World, message, 0x44, MessageType.System);
                }
            }
        }
    }
}
