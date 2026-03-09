#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using static SDL3.SDL;

namespace ClassicUO
{
    internal unsafe class GameController : Microsoft.Xna.Framework.Game
    {
        private SDL_EventFilter _filter;

        private readonly Texture2D[] _hueSamplers = new Texture2D[3];
        private bool _ignoreNextTextInput;
        private readonly float[] _intervalFixedUpdate = new float[2];
        private double _totalElapsed, _currentFpsTime, _nextSlowUpdate;
        private uint _totalFrames;
        private uint _lastLegionScriptingUpdate;
        private UltimaBatcher2D _uoSpriteBatch;
        private bool _suppressedDraw;
        private Texture2D _background;

        private static Vector3 bgHueShader = new Vector3(0, 0, 0.3f);

        public GameController()
        {
            GraphicManager = new GraphicsDeviceManager(this);

            GraphicManager.PreparingDeviceSettings += (sender, e) =>
            {
                e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage =
                    RenderTargetUsage.DiscardContents;
            };

            GraphicManager.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
            SetVSync(false);
            GraphicManager.PreparingDeviceSettings += (sender, e) =>
            {
                bool useVSync = ProfileManager.CurrentProfile?.EnableVSync ?? false;
                e.GraphicsDeviceInformation.PresentationParameters.PresentationInterval =
                    useVSync ? PresentInterval.One : PresentInterval.Immediate;
            };

            Window.ClientSizeChanged += WindowOnClientSizeChanged;
            Window.AllowUserResizing = true;
            Window.Title = $"Dust 3.0 - {CUOEnviroment.Version}";
            IsMouseVisible = Settings.GlobalSettings.RunMouseInASeparateThread;

            IsFixedTimeStep = false;
            TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0 / 250.0);
            InactiveSleepTime = TimeSpan.Zero;
        }

        public Scene Scene { get; private set; }
        public GameCursor GameCursor { get; private set; }
        public AudioManager Audio { get; private set; }

        public Renderer.Animations.Animations Animations { get; private set; }
        public Renderer.Arts.Art Arts { get; private set; }
        public Renderer.Gumps.Gump Gumps { get; private set; }
        public Renderer.Texmaps.Texmap Texmaps { get; private set; }
        public Renderer.Lights.Light Lights { get; private set; }
        public Renderer.MultiMaps.MultiMap MultiMaps { get; private set; }
        public Renderer.Sounds.Sound Sounds { get; private set; }

        public GraphicsDeviceManager GraphicManager { get; }
        public readonly uint[] FrameDelay = new uint[2];

        protected override void Initialize()
        {
            if (GraphicManager.GraphicsDevice.Adapter.IsProfileSupported(GraphicsProfile.HiDef))
            {
                GraphicManager.GraphicsProfile = GraphicsProfile.HiDef;
            }

            GraphicManager.ApplyChanges();

            SetRefreshRate(Settings.GlobalSettings.FPS);
            _uoSpriteBatch = new UltimaBatcher2D(GraphicsDevice);
            
            // Aplicar configurações de performance
            ClassicUO.Game.Scenes.PerformanceOptimizer.ApplyGraphicsQualitySettings();
            ClassicUO.Game.Scenes.PerformanceOptimizer.OptimizeForPerformance();
            
            // Aplicar configurações de VSync após GraphicsDevice estar inicializado
            SetVSync(ProfileManager.CurrentProfile?.EnableVSync ?? false);

            _filter = HandleSdlEvent;
            SDL_SetEventFilter(_filter, IntPtr.Zero);

            // SDL optimizations for high FPS
            OptimizeSDLForHighFPS();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            const int TEXTURE_WIDTH = 32;
            const int TEXTURE_HEIGHT = 2048;

            const int LIGHTS_TEXTURE_WIDTH = 32;
            const int LIGHTS_TEXTURE_HEIGHT = 63;

            _hueSamplers[0] = new Texture2D(GraphicsDevice, TEXTURE_WIDTH, TEXTURE_HEIGHT);
            _hueSamplers[1] = new Texture2D(GraphicsDevice, TEXTURE_WIDTH, TEXTURE_HEIGHT);
            _hueSamplers[2] = new Texture2D(
                GraphicsDevice,
                LIGHTS_TEXTURE_WIDTH,
                LIGHTS_TEXTURE_HEIGHT
            );

            uint[] buffer = System.Buffers.ArrayPool<uint>.Shared.Rent(
                Math.Max(
                    LIGHTS_TEXTURE_WIDTH * LIGHTS_TEXTURE_HEIGHT,
                    TEXTURE_WIDTH * TEXTURE_HEIGHT * 2
                )
            );

            fixed (uint* ptr = buffer)
            {
                HuesLoader.Instance.CreateShaderColors(buffer);
                _hueSamplers[0].SetDataPointerEXT(
                    0,
                    null,
                    (IntPtr)ptr,
                    TEXTURE_WIDTH * TEXTURE_HEIGHT * sizeof(uint)
                );
                _hueSamplers[1].SetDataPointerEXT(
                    0,
                    null,
                    (IntPtr)ptr + TEXTURE_WIDTH * TEXTURE_HEIGHT * sizeof(uint),
                    TEXTURE_WIDTH * TEXTURE_HEIGHT * sizeof(uint)
                );

                LightColors.CreateLightTextures(buffer, LIGHTS_TEXTURE_HEIGHT);
                _hueSamplers[2].SetDataPointerEXT(
                    0,
                    null,
                    (IntPtr)ptr,
                    LIGHTS_TEXTURE_WIDTH * LIGHTS_TEXTURE_HEIGHT * sizeof(uint)
                );
            }

            System.Buffers.ArrayPool<uint>.Shared.Return(buffer, true);

            GraphicsDevice.Textures[1] = _hueSamplers[0];
            GraphicsDevice.Textures[2] = _hueSamplers[1];
            GraphicsDevice.Textures[3] = _hueSamplers[2];

            MapLoader.MapsLayouts = Settings.GlobalSettings.MapsLayouts;

            Fonts.Initialize(GraphicsDevice);
            SolidColorTextureCache.Initialize(GraphicsDevice);
            PNGLoader.Instance.GraphicsDevice = GraphicsDevice;
            System.Threading.Tasks.Task loadResourceAssets = PNGLoader.Instance.LoadResourceAssets();

            Animations = new Renderer.Animations.Animations(GraphicsDevice);
            Arts = new Renderer.Arts.Art(GraphicsDevice);
            Gumps = new Renderer.Gumps.Gump(GraphicsDevice);
            Texmaps = new Renderer.Texmaps.Texmap(GraphicsDevice);
            Lights = new Renderer.Lights.Light(GraphicsDevice);
            MultiMaps = new Renderer.MultiMaps.MultiMap(GraphicsDevice);
            Sounds = new Renderer.Sounds.Sound();

            LightColors.LoadLights();

            GameCursor = new GameCursor();
            Audio = new AudioManager();
            Audio.Initialize();

            var bytes = Loader.GetBackgroundImage().ToArray();
            using var ms = new MemoryStream(bytes);
            _background = Texture2D.FromStream(GraphicsDevice, ms);

            loadResourceAssets.Wait(10000);

            SetScene(new LoginScene());
            SetWindowPositionBySettings();
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
            if (Scene is GameScene && World.Player != null)
            {
                try
                {
                    PaperdollSelectCharManager.Instance.Save();
                    PaperdollSelectCharManager.Instance.SaveJson();
                }
                catch { }
            }
            Plugin.OnClosing();

            ArtLoader.Instance.Dispose();
            GumpsLoader.Instance.Dispose();
            TexmapsLoader.Instance.Dispose();
            AnimationsLoader.Instance.Dispose();
            LightsLoader.Instance.Dispose();
            TileDataLoader.Instance.Dispose();
            AnimDataLoader.Instance.Dispose();
            ClilocLoader.Instance.Dispose();
            FontsLoader.Instance.Dispose();
            HuesLoader.Instance.Dispose();
            MapLoader.Instance.Dispose();
            MultiLoader.Instance.Dispose();
            MultiMapLoader.Instance.Dispose();
            ProfessionLoader.Instance.Dispose();
            SkillsLoader.Instance.Dispose();
            SoundsLoader.Instance.Dispose();
            SpeechesLoader.Instance.Dispose();
            Verdata.File?.Dispose();
            World.Map?.Destroy();

            base.UnloadContent();
        }

        public void SetWindowTitle(string title)
        {
            if (string.IsNullOrEmpty(title))
            {
#if DEV_BUILD
                Window.Title = $"ClassicUO [dev] - {CUOEnviroment.Version}";
#else
                Window.Title = $"[Dust765.3.0 {CUOEnviroment.Version}]";
#endif
            }
            else
            {
#if DEV_BUILD
                Window.Title = $"{title} - ClassicUO [dev] - {CUOEnviroment.Version}";
#else
                Window.Title = $"{title} - [Dust765.3.0 {CUOEnviroment.Version}]";
#endif
            }
        }

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

        public void SetVSync(bool value)
        {
            GraphicManager.SynchronizeWithVerticalRetrace = value;
            if (GraphicManager.GraphicsDevice != null)
            {
                GraphicManager.GraphicsDevice.PresentationParameters.PresentationInterval =
                    value ? PresentInterval.One : PresentInterval.Immediate;
                if (!value)
                {
                    SDL_SetHint("SDL_RENDER_VSYNC", "0");
                    PresentationParameters pp = GraphicManager.GraphicsDevice.PresentationParameters.Clone();
                    pp.PresentationInterval = PresentInterval.Immediate;
                    GraphicManager.GraphicsDevice.Reset(pp, GraphicManager.GraphicsDevice.Adapter);
                }
            }
            GraphicManager.ApplyChanges();
        }

        public void SetRefreshRate(int rate)
        {
            if (rate < Constants.MIN_FPS)
            {
                rate = Constants.MIN_FPS;
            }
            else if (rate > Constants.MAX_FPS)
            {
                rate = Constants.MAX_FPS;
            }

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

            bool unlimitedFps = ProfileManager.CurrentProfile?.DisableFrameLimiting == true;
            TargetElapsedTime = unlimitedFps ? TimeSpan.FromMilliseconds(1000.0 / 250.0) : TimeSpan.FromMilliseconds(frameDelay);
        }

        private void SetWindowPosition(int x, int y)
        {
            SDL_SetWindowPosition(Window.Handle, x, y);
        }

        public void SetWindowSize(int width, int height)
        {
            //width = (int) ((double) width * Client.Game.GraphicManager.PreferredBackBufferWidth / Client.Game.Window.ClientBounds.Width);
            //height = (int) ((double) height * Client.Game.GraphicManager.PreferredBackBufferHeight / Client.Game.Window.ClientBounds.Height);

            /*if (CUOEnviroment.IsHighDPI)
            {
                width *= 2;
                height *= 2;
            }
            */

            GraphicManager.PreferredBackBufferWidth = width;
            GraphicManager.PreferredBackBufferHeight = height;
            GraphicManager.ApplyChanges();
        }

        private void OptimizeSDLForHighFPS()
        {
            SDL_SetHint("SDL_RENDER_VSYNC", "0");
            SDL_SetHint("SDL_VIDEO_MINIMIZE_ON_FOCUS_LOSS", "0"); // Não minimizar ao perder foco
            SDL_SetHint("SDL_TIMER_RESOLUTION", "1"); // Resolução de timer de 1ms para FPS alto
            
            // Otimizações específicas para Windows
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                SDL_SetHint("SDL_WINDOWS_DISABLE_THREAD_NAMING", "1");
                SDL_SetHint("SDL_VIDEO_WINDOW_SHARE_PIXEL_FORMAT", "1");
            }
            
            // Configurar timer de alta resolução
            SDL_SetHint("SDL_HINT_TIMER_RESOLUTION", "1");
            
            Log.Trace("SDL optimized for high FPS");
        }

        public void SetWindowBorderless(bool borderless)
        {
            SDL_WindowFlags flags = (SDL_WindowFlags)SDL_GetWindowFlags(Window.Handle);

            if ((flags & SDL_WindowFlags.SDL_WINDOW_BORDERLESS) != 0 && borderless)
            {
                return;
            }

            if ((flags & SDL_WindowFlags.SDL_WINDOW_BORDERLESS) == 0 && !borderless)
            {
                return;
            }

            SDL_SetWindowBordered(Window.Handle, !borderless);

            uint displayId = SDL_GetDisplayForWindow(Window.Handle);
            SDL_DisplayMode* displayModePtr = (SDL_DisplayMode*)SDL_GetCurrentDisplayMode(displayId);
            int width = displayModePtr->w;
            int height = displayModePtr->h;

            if (borderless)
            {
                SetWindowSize(width, height);
                SDL_GetDisplayUsableBounds(
                    SDL_GetDisplayForWindow(Window.Handle),
                    out SDL_Rect rect
                );
                SDL_SetWindowPosition(Window.Handle, rect.x, rect.y);
            }
            else
            {
                SDL_GetWindowBordersSize(Window.Handle, out int top, out _, out int bottom, out _);

                SetWindowSize(width, height - (top - bottom));
                SetWindowPositionBySettings();
            }

            WorldViewportGump viewport = UIManager.GetGump<WorldViewportGump>();

            if (viewport != null && ProfileManager.CurrentProfile.GameWindowFullSize)
            {
                viewport.ResizeGameWindow(new Point(width, height));
                viewport.X = -5;
                viewport.Y = -5;
            }
        }

        public void MaximizeWindow()
        {
            SDL_MaximizeWindow(Window.Handle);

            GraphicManager.PreferredBackBufferWidth = Client.Game.Window.ClientBounds.Width;
            GraphicManager.PreferredBackBufferHeight = Client.Game.Window.ClientBounds.Height;
            GraphicManager.ApplyChanges();
        }

        public bool IsWindowMaximized()
        {
            SDL_WindowFlags flags = (SDL_WindowFlags)SDL_GetWindowFlags(Window.Handle);

            return (flags & SDL_WindowFlags.SDL_WINDOW_MAXIMIZED) != 0;
        }

        public void RestoreWindow()
        {
            SDL_RestoreWindow(Window.Handle);
        }

        public void SetWindowPositionBySettings()
        {
            SDL_GetWindowBordersSize(Window.Handle, out int top, out int left, out _, out _);

            if (Settings.GlobalSettings.WindowPosition.HasValue)
            {
                int x = left + Settings.GlobalSettings.WindowPosition.Value.X;
                int y = top + Settings.GlobalSettings.WindowPosition.Value.Y;
                x = Math.Max(0, x);
                y = Math.Max(0, y);

                SetWindowPosition(x, y);
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (Profiler.InContext("OutOfContext"))
            {
                Profiler.ExitContext("OutOfContext");
            }

            Time.Ticks = (uint)gameTime.TotalGameTime.TotalMilliseconds;
            Time.Delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Mouse.Update();

            var data = NetClient.Socket.CollectAvailableData();
            var packetsCount = PacketHandlers.Handler.ParsePackets(data);
            NetClient.Socket.Statistics.TotalPacketsReceived += (uint)packetsCount;
            NetClient.Socket.Flush();

            Plugin.Tick();

            if (Scene != null && Scene.IsLoaded && !Scene.IsDestroyed)
            {
                if (EventSink.GameUpdate != null)
                {
                    EventSink.GameUpdate();
                }
                Profiler.EnterContext("Update");
                Scene.Update();
                Profiler.ExitContext("Update");
            }

            UIManager.Update();
            if (Time.Ticks - _lastLegionScriptingUpdate >= 50)
            {
                _lastLegionScriptingUpdate = Time.Ticks;
                LegionScripting.LegionScripting.OnUpdate();
            }

            if (World.InGame)
                PerformanceOptimizer.UpdatePvPMode();

            if (Time.Ticks >= _nextSlowUpdate)
            {
                _nextSlowUpdate = Time.Ticks + 500;
                UIManager.SlowUpdate();
            }

            _totalElapsed += gameTime.ElapsedGameTime.TotalMilliseconds;
            _currentFpsTime += gameTime.ElapsedGameTime.TotalMilliseconds;

            if (_currentFpsTime >= 1000)
            {
                CUOEnviroment.CurrentRefreshRate = _totalFrames;
                if (_totalFrames < CUOEnviroment.CurrentRefreshRateMin)
                    CUOEnviroment.CurrentRefreshRateMin = _totalFrames;
                if (_totalFrames > CUOEnviroment.CurrentRefreshRateMax)
                    CUOEnviroment.CurrentRefreshRateMax = _totalFrames;

                _totalFrames = 0;
                _currentFpsTime = 0;
            }

            // Verificar se o frame limiting deve ser desabilitado
            bool disableFrameLimiting = ProfileManager.CurrentProfile?.DisableFrameLimiting ?? false;
            
            if (!disableFrameLimiting)
            {
                double x = _intervalFixedUpdate[
                    !IsActive
                    && ProfileManager.CurrentProfile != null
                    && ProfileManager.CurrentProfile.ReduceFPSWhenInactive
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
                }
            }
            else
            {
                // Frame limiting completamente desabilitado - renderizar sempre
                _suppressedDraw = false;
                _totalElapsed = 0; // Reset para evitar acúmulo
            }

            GameCursor?.Update();
            Audio?.Update();

            base.Update(gameTime);
        }

        public static void UpdateBackgroundHueShader()
        {
            if (ProfileManager.CurrentProfile != null)
                bgHueShader = ShaderHueTranslator.GetHueVector(ProfileManager.CurrentProfile.MainWindowBackgroundHue, false, bgHueShader.Z);
        }

        protected override void Draw(GameTime gameTime)
        {
            Profiler.EndFrame();
            Profiler.BeginFrame();

            if (Profiler.InContext("OutOfContext"))
            {
                Profiler.ExitContext("OutOfContext");
            }

            Profiler.EnterContext("RenderFrame");

            _totalFrames++;

            GraphicsDevice.Clear(Color.Black);

            _uoSpriteBatch.Begin();
            var rect = new Rectangle(0, 0, GraphicManager.PreferredBackBufferWidth, GraphicManager.PreferredBackBufferHeight);
            _uoSpriteBatch.Draw(SolidColorTextureCache.GetTexture(Color.Black), rect, Vector3.UnitZ);

            _uoSpriteBatch.DrawTiled(_background, rect, _background.Bounds, bgHueShader);
            _uoSpriteBatch.End();

            if (Scene != null && Scene.IsLoaded && !Scene.IsDestroyed)
            {
                Scene.Draw(_uoSpriteBatch);
            }

            UIManager.Draw(_uoSpriteBatch);

            if (World.InGame && SelectedObject.Object is TextObject t)
            {
                if (t.IsTextGump)
                {
                    t.ToTopD();
                }
                else
                {
                    World.WorldTextManager?.MoveToTop(t);
                }
            }

            SelectedObject.HealthbarObject = null;
            SelectedObject.SelectedContainer = null;

            _uoSpriteBatch.Begin();
            GameCursor.Draw(_uoSpriteBatch);
            _uoSpriteBatch.End();

            base.Draw(gameTime);

            Profiler.ExitContext("RenderFrame");
            Profiler.EnterContext("OutOfContext");

            Plugin.ProcessDrawCmdList(GraphicsDevice);
        }

        protected override bool BeginDraw()
        {
            return !_suppressedDraw && base.BeginDraw();
        }

        private void WindowOnClientSizeChanged(object sender, EventArgs e)
        {
            int width = Window.ClientBounds.Width;
            int height = Window.ClientBounds.Height;

            if (!IsWindowMaximized())
            {
                ProfileManager.CurrentProfile.WindowClientBounds = new Point(width, height);
            }

            SetWindowSize(width, height);

            WorldViewportGump viewport = UIManager.GetGump<WorldViewportGump>();

            if (viewport != null && ProfileManager.CurrentProfile.GameWindowFullSize)
            {
                viewport.ResizeGameWindow(new Point(width, height));
                viewport.X = -5;
                viewport.Y = -5;
            }
        }

        private bool HandleSdlEvent(IntPtr userData, SDL_Event* sdlEvent)
        {
            SDL_EventType eventType = (SDL_EventType)sdlEvent->type;
            bool isKeyEvent =
                eventType == SDL_EventType.SDL_EVENT_KEY_DOWN
                || eventType == SDL_EventType.SDL_EVENT_KEY_UP;

            int wndProcResult = 0;
            if (!isKeyEvent)
            {
                wndProcResult = Plugin.ProcessWndProc(sdlEvent);
            }

            if (wndProcResult != 0)
            {
                if (eventType == SDL_EventType.SDL_EVENT_MOUSE_MOTION)
                {
                    if (GameCursor != null)
                    {
                        GameCursor.AllowDrawSDLCursor = false;
                    }
                }

                return true;
            }

            switch (eventType)
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

                    Keyboard.OnKeyDown(sdlEvent->key);

                    if (
                        Plugin.ProcessHotkeys(
                            (int)(SDL_Keycode)sdlEvent->key.key,
                            (int)sdlEvent->key.mod,
                            true
                        )
                    )
                    {
                        _ignoreNextTextInput = false;

                        UIManager.KeyboardFocusControl?.InvokeKeyDown(
                            (SDL_Keycode)sdlEvent->key.key,
                            sdlEvent->key.mod
                        );

                        Scene.OnKeyDown(sdlEvent->key);
                    }
                    else
                    {
                        _ignoreNextTextInput = true;
                    }

                    break;

                case SDL_EventType.SDL_EVENT_KEY_UP:

                    Keyboard.OnKeyUp(sdlEvent->key);
                    UIManager.KeyboardFocusControl?.InvokeKeyUp(
                        (SDL_Keycode)sdlEvent->key.key,
                        sdlEvent->key.mod
                    );
                    Scene.OnKeyUp(sdlEvent->key);
                    Plugin.ProcessHotkeys(0, 0, false);

                    if ((SDL_Keycode)sdlEvent->key.key == SDL_Keycode.SDLK_PRINTSCREEN)
                    {
                        if (Keyboard.Ctrl)
                        {
                            if (Tooltip.IsEnabled)
                            {
                                ClipboardScreenshot(new Rectangle(Tooltip.X, Tooltip.Y, Tooltip.Width, Tooltip.Height), GraphicsDevice);
                            }
                            else if (MultipleToolTipGump.SSIsEnabled)
                            {
                                ClipboardScreenshot(new Rectangle(MultipleToolTipGump.SSX, MultipleToolTipGump.SSY, MultipleToolTipGump.SSWidth, MultipleToolTipGump.SSHeight), GraphicsDevice);
                            }
                            else if (UIManager.MouseOverControl != null && UIManager.MouseOverControl.IsVisible)
                            {
                                Control c = UIManager.MouseOverControl.RootParent;
                                if (c != null)
                                {
                                    ClipboardScreenshot(c.Bounds, GraphicsDevice);
                                }
                                else
                                {
                                    ClipboardScreenshot(UIManager.MouseOverControl.Bounds, GraphicsDevice);
                                }
                            }
                        }
                        else
                        {
                            TakeScreenshot();
                        }
                    }

                    break;

                case SDL_EventType.SDL_EVENT_TEXT_INPUT:

                    if (_ignoreNextTextInput)
                    {
                        break;
                    }

                    string s = Utf8PtrToString((IntPtr)sdlEvent->text.text);

                    if (!string.IsNullOrEmpty(s))
                    {
                        UIManager.KeyboardFocusControl?.InvokeTextInput(s);
                        Scene.OnTextInput(s);
                    }

                    break;

                case SDL_EventType.SDL_EVENT_MOUSE_MOTION:

                    if (GameCursor != null && !GameCursor.AllowDrawSDLCursor)
                    {
                        GameCursor.AllowDrawSDLCursor = true;
                        GameCursor.Graphic = 0xFFFF;
                    }

                    Mouse.Update();

                    if (Mouse.IsDragging)
                    {
                        if (!Scene.OnMouseDragging())
                        {
                            UIManager.OnMouseDragging();
                        }
                    }

                    break;

                case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
                    Mouse.Update();
                    bool isScrolledUp = sdlEvent->wheel.y > 0;

                    Plugin.ProcessMouse(0, (int)sdlEvent->wheel.y);

                    if (!Scene.OnMouseWheel(isScrolledUp))
                    {
                        UIManager.OnMouseWheel(isScrolledUp);
                    }

                    break;

                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
                    {
                        SDL_MouseButtonEvent mouse = sdlEvent->button;

                        // The values in MouseButtonType are chosen to exactly match the SDL values
                        MouseButtonType buttonType = (MouseButtonType)mouse.button;

                        uint lastClickTime = 0;

                        switch (buttonType)
                        {
                            case MouseButtonType.Left:
                                lastClickTime = Mouse.LastLeftButtonClickTime;

                                break;

                            case MouseButtonType.Middle:
                                lastClickTime = Mouse.LastMidButtonClickTime;

                                break;

                            case MouseButtonType.Right:
                                lastClickTime = Mouse.LastRightButtonClickTime;

                                break;

                            case MouseButtonType.XButton1:
                            case MouseButtonType.XButton2:
                                break;

                            default:
                                Log.Warn($"No mouse button handled: {mouse.button}");

                                break;
                        }

                        Mouse.ButtonPress(buttonType);
                        Mouse.Update();

                        uint ticks = Time.Ticks;

                        if (lastClickTime + Mouse.MOUSE_DELAY_DOUBLE_CLICK >= ticks)
                        {
                            lastClickTime = 0;

                            bool res =
                                Scene.OnMouseDoubleClick(buttonType)
                                || UIManager.OnMouseDoubleClick(buttonType);

                            if (!res)
                            {
                                if (!Scene.OnMouseDown(buttonType))
                                {
                                    UIManager.OnMouseButtonDown(buttonType);
                                }
                            }
                            else
                            {
                                lastClickTime = 0xFFFF_FFFF;
                            }
                        }
                        else
                        {
                            if (
                                buttonType != MouseButtonType.Left
                                && buttonType != MouseButtonType.Right
                            )
                            {
                                Plugin.ProcessMouse(sdlEvent->button.button, 0);
                            }

                            if (!Scene.OnMouseDown(buttonType))
                            {
                                UIManager.OnMouseButtonDown(buttonType);
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

                        break;
                    }

                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
                    {
                        SDL_MouseButtonEvent mouse = sdlEvent->button;

                        // The values in MouseButtonType are chosen to exactly match the SDL values
                        MouseButtonType buttonType = (MouseButtonType)mouse.button;

                        uint lastClickTime = 0;

                        switch (buttonType)
                        {
                            case MouseButtonType.Left:
                                lastClickTime = Mouse.LastLeftButtonClickTime;

                                break;

                            case MouseButtonType.Middle:
                                lastClickTime = Mouse.LastMidButtonClickTime;

                                break;

                            case MouseButtonType.Right:
                                lastClickTime = Mouse.LastRightButtonClickTime;

                                break;

                            default:
                                Log.Warn($"No mouse button handled: {mouse.button}");

                                break;
                        }

                        if (lastClickTime != 0xFFFF_FFFF)
                        {
                            if (
                                !Scene.OnMouseUp(buttonType)
                                || UIManager.LastControlMouseDown(buttonType) != null
                            )
                            {
                                UIManager.OnMouseButtonUp(buttonType);
                            }
                        }

                        Mouse.ButtonRelease(buttonType);
                        Mouse.Update();

                        break;
                    }

                case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_DOWN:
                    if (!IsActive)
                    {
                        break;
                    }
                    Controller.OnButtonDown(sdlEvent->gbutton);
                    UIManager.KeyboardFocusControl?.InvokeControllerButtonDown((SDL_GamepadButton)sdlEvent->gbutton.button);
                    Scene.OnControllerButtonDown(sdlEvent->gbutton);

                    if (sdlEvent->gbutton.button == (byte)SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_DOWN)
                    {
                        UIManager.OnMouseWheel(false);
                    }
                    else if (sdlEvent->gbutton.button == (byte)SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_UP)
                    {
                        UIManager.OnMouseWheel(true);
                    }

                    if (sdlEvent->gbutton.button == (byte)SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_STICK)
                    {
                        SDL_Event e = new SDL_Event();
                        e.type = (uint)SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN;
                        e.button.button = (byte)MouseButtonType.Left;
                        SDL3.SDL.SDL_PushEvent(ref e);
                    }
                    else if (sdlEvent->gbutton.button == (byte)SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_STICK)
                    {
                        SDL_Event e = new SDL_Event();
                        e.type = (uint)SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN;
                        e.button.button = (byte)MouseButtonType.Right;
                        SDL3.SDL.SDL_PushEvent(ref e);
                    }
                    else if (sdlEvent->gbutton.button == (byte)SDL_GamepadButton.SDL_GAMEPAD_BUTTON_START && World.InGame)
                    {
                        Gump g = UIManager.GetGump<OptionsGump>();
                        if (g == null)
                        {
                            UIManager.Add(new OptionsGump());
                        }
                        else
                        {
                            g.Dispose();
                        }
                    }
                    break;

                case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_UP:
                    if (!IsActive)
                    {
                        break;
                    }
                    Controller.OnButtonUp(sdlEvent->gbutton);
                    UIManager.KeyboardFocusControl?.InvokeControllerButtonUp((SDL_GamepadButton)sdlEvent->gbutton.button);
                    Scene.OnControllerButtonUp(sdlEvent->gbutton);

                    if (sdlEvent->gbutton.button == (byte)SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_STICK)
                    {
                        SDL_Event e = new SDL_Event();
                        e.type = (uint)SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP;
                        e.button.button = (byte)MouseButtonType.Left;
                        SDL3.SDL.SDL_PushEvent(ref e);
                    }
                    else if (sdlEvent->gbutton.button == (byte)SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_STICK)
                    {
                        SDL_Event e = new SDL_Event();
                        e.type = (uint)SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP;
                        e.button.button = (byte)MouseButtonType.Right;
                        SDL3.SDL.SDL_PushEvent(ref e);
                    }
                    break;

                case SDL_EventType.SDL_EVENT_GAMEPAD_AXIS_MOTION:
                    if (!IsActive)
                    {
                        break;
                    }
                    if (sdlEvent->gbutton.button == (byte)SDL_GamepadButton.SDL_GAMEPAD_BUTTON_BACK || sdlEvent->gbutton.button == (byte)SDL_GamepadButton.SDL_GAMEPAD_BUTTON_GUIDE)
                    {
                        if (sdlEvent->gaxis.value > 32000)
                        {
                            if (
                                ((SDL_GamepadButton)sdlEvent->gbutton.button == SDL_GamepadButton.SDL_GAMEPAD_BUTTON_BACK && !Controller.Button_LeftTrigger)
                                || ((SDL_GamepadButton)sdlEvent->gbutton.button == SDL_GamepadButton.SDL_GAMEPAD_BUTTON_GUIDE && !Controller.Button_RightTrigger)
                                )
                            {
                                Controller.OnButtonDown(sdlEvent->gbutton);
                                UIManager.KeyboardFocusControl?.InvokeControllerButtonDown((SDL_GamepadButton)sdlEvent->gbutton.button);
                                Scene.OnControllerButtonDown(sdlEvent->gbutton);
                            }
                        }
                        else if (sdlEvent->gaxis.value < 5000)
                        {
                            Controller.OnButtonUp(sdlEvent->gbutton);
                            UIManager.KeyboardFocusControl?.InvokeControllerButtonUp((SDL_GamepadButton)sdlEvent->gbutton.button);
                            Scene.OnControllerButtonUp(sdlEvent->gbutton);
                        }
                    }
                    break;
            }

            return true;
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            Scene?.Dispose();

            base.OnExiting(sender, args);
        }

        private void TakeScreenshot()
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
                    ProfileManager.CurrentProfile == null
                    || ProfileManager.CurrentProfile.HideScreenshotStoredInMessage
                )
                {
                    Log.Info(message);
                }
                else
                {
                    GameActions.Print(message, 0x44, MessageType.System);
                }
            }
        }

        public void ClipboardScreenshot(Rectangle position, GraphicsDevice graphicDevice)
        {
            Color[] colors = new Color[position.Width * position.Height];

            graphicDevice.GetBackBufferData(position, colors, 0, colors.Length);

            using (Texture2D texture = new Texture2D(GraphicsDevice, position.Width, position.Height, false, SurfaceFormat.Color))
            {
                texture.SetData(colors);
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
                using FileStream fileStream = File.Create(path);
                texture.SaveAsPng(fileStream, texture.Width, texture.Height);
                string message = string.Format(ResGeneral.ScreenshotStoredIn0, path);
                if (ProfileManager.CurrentProfile == null || ProfileManager.CurrentProfile.HideScreenshotStoredInMessage)
                {
                    Log.Info(message);
                }
                else
                {
                    GameActions.Print(message, 0x44, MessageType.System);
                }
            }
        }

        private static unsafe string Utf8PtrToString(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return null;
            byte* p = (byte*)ptr;
            int len = 0;
            while (p[len] != 0) len++;
            if (len == 0) return string.Empty;
            return System.Text.Encoding.UTF8.GetString(p, len);
        }
    }
}
