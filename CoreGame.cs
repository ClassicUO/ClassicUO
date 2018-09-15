using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO
{
    public abstract class CoreGame : Microsoft.Xna.Framework.Game
    {
        private float _time;
        private FpsCounter _fpsCounter;

        protected CoreGame()
        {
            TargetElapsedTime = TimeSpan.FromSeconds(1.0f / 300.0f);
            GraphicsDeviceManager = new GraphicsDeviceManager(this);

            GraphicsDeviceManager.PreparingDeviceSettings += (sender, e) => e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = Microsoft.Xna.Framework.Graphics.RenderTargetUsage.PreserveContents;

            if (GraphicsDeviceManager.GraphicsDevice.Adapter.IsProfileSupported(GraphicsProfile.HiDef))
                GraphicsDeviceManager.GraphicsProfile = GraphicsProfile.HiDef;

            GraphicsDeviceManager.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
            GraphicsDeviceManager.SynchronizeWithVerticalRetrace = false;
            GraphicsDeviceManager.PreferredBackBufferWidth = 800;   // should be changed by settings file
            GraphicsDeviceManager.PreferredBackBufferHeight = 600;  // should be changed by settings file
            GraphicsDeviceManager.ApplyChanges();


            Window.ClientSizeChanged += (sender, e) =>
            {
                GraphicsDeviceManager.PreferredBackBufferWidth = Window.ClientBounds.Width;
                GraphicsDeviceManager.PreferredBackBufferHeight = Window.ClientBounds.Height;
                GraphicsDeviceManager.ApplyChanges();
            };

            Window.AllowUserResizing = true;


            MaxFPS = 144;
            _fpsCounter = new FpsCounter();
        }


        protected GraphicsDeviceManager GraphicsDeviceManager { get; }
        protected float IntervalFixedUpdate => 1000.0f / MaxFPS;


        public int MaxFPS { get; set; }
        public int CurrentFPS => _fpsCounter.FPS;


        protected override void Update(GameTime gameTime)
        {
            if (Profiler.InContext("OutOfContext"))
                Profiler.ExitContext("OutOfContext");
            Profiler.EnterContext("Update");

            var totalms = gameTime.TotalGameTime.TotalMilliseconds;
            var framems = gameTime.ElapsedGameTime.TotalMilliseconds;

            _fpsCounter.Update(gameTime);

            base.Update(gameTime);

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

            Profiler.EnterContext("OutOfContext");
        }

        protected override void Draw(GameTime gameTime)
        {
            Profiler.EndFrame();
            Profiler.BeginFrame();
            if (Profiler.InContext("OutOfContext"))
                Profiler.ExitContext("OutOfContext");
            Profiler.EnterContext("RenderFrame");
            _fpsCounter.IncreaseFrame();
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

            Window.Title = string.Format("ClassicUO - Draw:{0:0.0}% Update:{1:0.0}% Fixed:{2:0.0}% AvgDraw:{3:0.0}ms {4} - FPS: {5}",
                100d * (timeDraw / timeTotal),
                100d * (timeUpdate / timeTotal),
                100d * (timeFixedUpdate / timeTotal),
                avgDrawMs,
                gameTime.IsRunningSlowly ? "*" : string.Empty, CurrentFPS);
        }

        protected abstract void OnUpdate(double totalMS, double frameMS);
        protected abstract void OnFixedUpdate(double totalMS, double frameMS);
        protected abstract void OnDraw(double frameMS);

        protected abstract void OnNetworkUpdate(double totalMS, double frameMS);
        protected abstract void OnInputUpdate(double totalMS, double frameMS);
        protected abstract void OnUIUpdate(double totalMS, double frameMS);


    }
}
