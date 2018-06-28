using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO
{
    internal class GameLoop : Microsoft.Xna.Framework.Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        public GameLoop()
        {
            _graphics = new GraphicsDeviceManager(this);

            Log.Message(LogTypes.Trace, "Gameloop initialized.");
        }

        private Game.Facet _facet;

        protected override void Initialize()
        {
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 144.0f);

            IsFixedTimeStep = false;


            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TEST
            Assets.FileManager.UoFolderPath = @"E:\Giochi\Ultima Online Classic ORION";
            Assets.FileManager.LoadFiles();

            _facet = new Game.Facet(0);

            // END TEST

            base.LoadContent();
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();
        }


        const double TIME_RUN_MOUNT = (2d / 20d) * 1000d;
        private DateTime _delay = DateTime.Now;

        private ushort _x = 1443, _y = 1659;
        private ushort _maxX = 1560;
        private ushort _currentX = 1443;
        private Stopwatch _stopwatch;

        protected override void Update(GameTime gameTime)
        {
            //Input.MouseManager.Update();
            //Input.KeyboardManager.Update();

            if (_stopwatch == null)
                _stopwatch = Stopwatch.StartNew();

            if (_stopwatch.ElapsedMilliseconds > TIME_RUN_MOUNT)
            {
                if (_currentX + 1 > _maxX)
                    _currentX = _x;
                _currentX++;

                _facet.LoadChunks(_currentX, _y, 24/8 );

                Log.Message(LogTypes.Trace, _stopwatch.ElapsedMilliseconds.ToString());
                _stopwatch.Restart();
               // _delay = DateTime.Now.AddMilliseconds(TIME_RUN_MOUNT);
            }



            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            base.Draw(gameTime);
        }
    }
}
