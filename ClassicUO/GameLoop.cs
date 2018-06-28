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

        protected override void Initialize()
        {
            // TEST
            Assets.FileManager.UoFolderPath = @"E:\Giochi\Ultima Online Classic ORION";
            Assets.FileManager.LoadFiles();

            Game.Facet facet = new Game.Facet(0);

            Stopwatch t = Stopwatch.StartNew();

            facet.LoadChunks(1511, 1894, 24 / 8);

            long elapsed = t.ElapsedMilliseconds;
            Log.Message(LogTypes.Trace, elapsed.ToString());
            // END TEST

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            base.LoadContent();
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();
        }


        protected override void Update(GameTime gameTime)
        {
            Input.MouseManager.Update();
            Input.KeyboardManager.Update();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            base.Draw(gameTime);
        }
    }
}
