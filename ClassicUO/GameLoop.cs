using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO
{
    internal class GameLoop : Microsoft.Xna.Framework.Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private MouseManager _mouseManager;
        private KeyboardManager _keyboardManager;
        private SpriteBatchUI _spriteBatch;

        public GameLoop()
        {
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 144.0f);
            _graphics = new GraphicsDeviceManager(this);

            _graphics.PreparingDeviceSettings += (sender, e) =>
            {
                e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            };

            
            if (_graphics.GraphicsDevice.Adapter.IsProfileSupported(GraphicsProfile.HiDef))
                _graphics.GraphicsProfile = GraphicsProfile.HiDef;

            _graphics.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
            _graphics.SynchronizeWithVerticalRetrace = false;
            _graphics.ApplyChanges();

           

            Log.Message(LogTypes.Trace, "Gameloop initialized.");
        }

        private Game.Map.Facet _facet;

        protected override void Initialize()
        {
            _mouseManager = new MouseManager(this);
            _keyboardManager = new KeyboardManager(this);

            Components.Add(_mouseManager);
            Components.Add(_keyboardManager);

            _spriteBatch = new SpriteBatchUI(this);

            base.Initialize();
        }

        protected override void LoadContent()
        {

            // TEST

           

            AssetsLoader.FileManager.UoFolderPath = @"E:\Giochi\Ultima Online Classic ORION";

            

           // Task.Run(() => 
            //{
                _stopwatch = Stopwatch.StartNew();
                Log.Message(LogTypes.Trace, "Loading UO files...");

                AssetsLoader.FileManager.LoadFiles();

                Log.Message(LogTypes.Trace, "UO files loaded in " + _stopwatch.ElapsedMilliseconds + " ms");

            //});

            PacketHandlers.Load();
            PacketsTable.AdjustPacketSizeByVersion(AssetsLoader.FileManager.ClientVersion);

            _mouseManager.LoadTextures();

            Texture2D textureHue0 = new Texture2D(GraphicsDevice, 32, 2048);
            Texture2D textureHue1 = new Texture2D(GraphicsDevice, 32, 2048);

            AssetsLoader.Hues.CreateHuesPalette();
            AssetsLoader.FloatHues[] huedata = AssetsLoader.Hues.Palette;

            //GraphicsDevice.Textures[1] = textureHue0;
            //GraphicsDevice.Textures[2] = textureHue1;

            //NetClient.Socket.Connect("login.uodemise.com", 2593);

            //_facet = new Game.Map.Facet(0);

            //var data = AssetsLoader.Art.ReadStaticArt(3850, out short w, out short h);

            //_texture = new Texture2D(GraphicsDevice, w, h, false, SurfaceFormat.Bgra5551);
            //_texture.SetData(data);



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
        private Texture2D _texture;

        protected override void Update(GameTime gameTime)
        {
            //Input.MouseManager.Update();
            //Input.KeyboardManager.Update();

            //if (_stopwatch.ElapsedMilliseconds >= TIME_RUN_MOUNT)
            //{
            //    if (_currentX + 1 > _maxX)
            //        _currentX = _x;
            //    _currentX++;

            //    _facet.LoadChunks(_x, _y, 5 );

            //    //Log.Message(LogTypes.Trace, _stopwatch.ElapsedMilliseconds.ToString());
            //    _stopwatch.Restart();
            //   // _delay = DateTime.Now.AddMilliseconds(TIME_RUN_MOUNT);
            //}

            
            NetClient.Socket.Slice();
            


            base.Update(gameTime);
        }

        protected override bool BeginDraw()
        {
            _mouseManager.BeginDraw();

            return base.BeginDraw();
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Transparent);

            _spriteBatch.BeginDraw();

            _mouseManager.Draw(_spriteBatch);


            _spriteBatch.EndDraw();



            base.Draw(gameTime);
        }
    }
}
