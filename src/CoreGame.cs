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

using ClassicUO.Utility;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO
{
    public abstract class CoreGame : Microsoft.Xna.Framework.Game
    {
        private const int MIN_FPS = 15;
        private const int MAX_FPS = 250;
        private int _maxFPS = MIN_FPS;
        private float _time;
        private int _totalFrames;
        private int _fps;
        private double _currentFpsTime;

        protected CoreGame()
        {
            TargetElapsedTime = TimeSpan.FromSeconds(1.0f / MAX_FPS);
            GraphicsDeviceManager = new GraphicsDeviceManager(this);
            GraphicsDeviceManager.PreparingDeviceSettings += (sender, e) => e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;

            if (GraphicsDeviceManager.GraphicsDevice.Adapter.IsProfileSupported(GraphicsProfile.HiDef))
                GraphicsDeviceManager.GraphicsProfile = GraphicsProfile.HiDef;
            GraphicsDeviceManager.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
            GraphicsDeviceManager.SynchronizeWithVerticalRetrace = false;
            GraphicsDeviceManager.PreferredBackBufferWidth = 640; // should be changed by settings file
            GraphicsDeviceManager.PreferredBackBufferHeight = 480; // should be changed by settings file
            GraphicsDeviceManager.ApplyChanges();

            Window.ClientSizeChanged += (sender, e) =>
            {
                GraphicsDeviceManager.PreferredBackBufferWidth = Window.ClientBounds.Width;
                GraphicsDeviceManager.PreferredBackBufferHeight = Window.ClientBounds.Height;
                GraphicsDeviceManager.ApplyChanges();
            };
            Window.AllowUserResizing = true;
        }

        protected GraphicsDeviceManager GraphicsDeviceManager { get; }

        protected float IntervalFixedUpdate => 1000.0f / MaxFPS;

        public int MaxFPS
        {
            get => _maxFPS;
            set
            {
                if (_maxFPS != value)
                {
                    _maxFPS = value;

                    if (_maxFPS < MIN_FPS)
                        _maxFPS = MIN_FPS;
                    else if (_maxFPS > MAX_FPS)
                        _maxFPS = MAX_FPS;
                    FrameDelay[0] = FrameDelay[1] = (uint) (1000 / _maxFPS);
                }
            }
        }

        public int CurrentFPS => _fps;

        public static long Ticks { get; private set; }

        public static uint[] FrameDelay { get; } = new uint[2];

        public bool IsFullScreen
        {
            get => GraphicsDeviceManager.IsFullScreen;
            set
            {
                GraphicsDeviceManager.IsFullScreen = value;
                GraphicsDeviceManager.ApplyChanges();
            }
        }

        public int WindowWidth
        {
            get => GraphicsDeviceManager.PreferredBackBufferWidth;
            set
            {
                GraphicsDeviceManager.PreferredBackBufferWidth = value;
                GraphicsDeviceManager.ApplyChanges();
            }
        }

        public int WindowHeight
        {
            get => GraphicsDeviceManager.PreferredBackBufferHeight;
            set
            {
                GraphicsDeviceManager.PreferredBackBufferHeight = value;
                GraphicsDeviceManager.ApplyChanges();
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (Profiler.InContext("OutOfContext"))
                Profiler.ExitContext("OutOfContext");
            Profiler.EnterContext("Update");
            double totalms = gameTime.TotalGameTime.TotalMilliseconds;
            double framems = gameTime.ElapsedGameTime.TotalMilliseconds;
            Ticks = (long) totalms;

            _currentFpsTime += gameTime.ElapsedGameTime.TotalSeconds;

            if (_currentFpsTime >= 1.0)
            {
                _fps = _totalFrames;
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
            OnDraw(gameTime.ElapsedGameTime.TotalMilliseconds);
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

        protected abstract void OnUpdate(double totalMS, double frameMS);
        protected abstract void OnFixedUpdate(double totalMS, double frameMS);
        protected abstract void OnDraw(double frameMS);
        protected abstract void OnNetworkUpdate(double totalMS, double frameMS);
        protected abstract void OnInputUpdate(double totalMS, double frameMS);
        protected abstract void OnUIUpdate(double totalMS, double frameMS);
    }
}