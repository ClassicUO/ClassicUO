using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 600;
            _graphics.ApplyChanges();

           

            Log.Message(LogTypes.Trace, "Gameloop initialized.");

            this.Window.ClientSizeChanged += (sender, e) =>
            {
                _graphics.PreferredBackBufferWidth = this.Window.ClientBounds.Width;
                _graphics.PreferredBackBufferHeight = this.Window.ClientBounds.Height;
            };
        }


        protected override void Initialize()
        {
            this.Window.AllowUserResizing = true;

            _mouseManager = new MouseManager(this);
            _keyboardManager = new KeyboardManager(this);

            Components.Add(_mouseManager);
            Components.Add(_keyboardManager);

            _spriteBatch = new SpriteBatchUI(this);

            TextureManager.Device = GraphicsDevice;

            

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


            uint[] hues = new uint[32 * 2048 * 2];
            int idx = 0; // 32

            foreach (var range in AssetsLoader.Hues.HuesRange)
            {
                foreach(var entry in range.Entries)
                {
                    foreach (var c in entry.ColorTable)
                    {
                        hues[idx++] = AssetsLoader.Hues.Color16To32(c);
                    }
                }
            }


            textureHue0.SetData(hues, 0, 2048 * 32);
            textureHue1.SetData(hues, 2048 * 32, 2048 * 32);

            GraphicsDevice.Textures[1] = textureHue0;
            GraphicsDevice.Textures[2] = textureHue1;

            //NetClient.Socket.Connect("login.uodemise.com", 2593);

            _facet = new Game.Map.Facet(0);
            Game.World.Map = _facet;

            var data = AssetsLoader.Art.ReadStaticArt(3850, out short w, out short h);

            _texture = new Texture2D(GraphicsDevice, w, h, false, SurfaceFormat.Bgra5551);
            _texture.SetData(data);


            _keyboardManager.KeyPressed += (sender, e) =>
            {
                if (e.KeyState == Microsoft.Xna.Framework.Input.KeyState.Down)
                {
                    switch (e.Key)
                    {
                        case Microsoft.Xna.Framework.Input.Keys.Left:
                            _currentX--;
                            _y++;
                            break;
                        case Microsoft.Xna.Framework.Input.Keys.Up:
                            _y--;
                            _currentX--;
                            break;
                        case Microsoft.Xna.Framework.Input.Keys.Right:
                            _currentX++;
                            _y--;
                            break;
                        case Microsoft.Xna.Framework.Input.Keys.Down:
                            _y++;
                            _currentX++;
                            break;
                    }                
                }
            };

            // END TEST

            base.LoadContent();
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();
        }

        

        const double TIME_RUN_MOUNT = (2d / 20d) * 1000d;
        private DateTime _delay = DateTime.Now;

        private ushort _x = 1336, _y = 1997;
        private ushort _maxX = 5454;
        private ushort _currentX = 1336;
        private Stopwatch _stopwatch;
        private Texture2D _texture;
        private Game.Map.Facet _facet;

        protected override void Update(GameTime gameTime)
        {
            //Input.MouseManager.Update();
            //Input.KeyboardManager.Update();

            _facet.LoadChunks(_currentX, _y, 5);

            //if (_stopwatch.ElapsedMilliseconds >= TIME_RUN_MOUNT / 2)
            //{
            //    //if (_currentX + 1 > _maxX)
            //    //    _currentX = _x;
            //    //_currentX++;

            //    //_facet.LoadChunks(_currentX, _y, 5);

            //    //Log.Message(LogTypes.Trace, _stopwatch.ElapsedMilliseconds.ToString());
            //    _stopwatch.Restart();
            //    // _delay = DateTime.Now.AddMilliseconds(TIME_RUN_MOUNT);
            //}


            NetClient.Socket.Slice();
            


            base.Update(gameTime);
        }

        protected override bool BeginDraw()
        {
            _mouseManager.BeginDraw();

            return base.BeginDraw();
        }

        private (Point, Vector2, Point) CalculateViewport(in Vector3 center, in int overDrawTilesOnSides, in int overDrawTilesOnTopAndBottom)
        {
            int scale = 1;

            int width = (_graphics.PreferredBackBufferWidth / 44) + 5;
            int height = (_graphics.PreferredBackBufferHeight / 44) + 5;

            if (width < height)
                width = height;
            else
                height = width;



            //Point renderDimension = new Point
            //(
            //    (_graphics.PreferredBackBufferWidth / scale / 44) + overDrawTilesOnSides, 
            //   ( _graphics.PreferredBackBufferHeight / scale / 44) + overDrawTilesOnTopAndBottom
            //);

            //int renderDimensionsDiff = Math.Abs(renderDimension.X - renderDimension.Y);
            //renderDimensionsDiff -= renderDimensionsDiff % 2;

            int firstZOffset = center.Z > 0 ? (int)(center.Z / 11) : 0;

            Point firstTile = new Point((int)center.X  - firstZOffset, (int)center.Y - height - firstZOffset);
            //if (renderDimension.Y > renderDimension.X)
            //{
            //    firstTile.X -= renderDimensionsDiff / 2;
            //    firstTile.Y -= renderDimensionsDiff / 2;
            //}
            //else
            //{
            //    firstTile.X += renderDimensionsDiff / 2;
            //    firstTile.Y -= renderDimensionsDiff / 2;
            //}

            Vector2 renderOffset = new Vector2();
            renderOffset.X = (((_graphics.PreferredBackBufferWidth / scale) + ((height) * 44)) / 2) - 22f;
            renderOffset.X -= (firstTile.X - firstTile.Y) * 22f;
            //renderOffset.X += width * 22f;

            renderOffset.Y = ((_graphics.PreferredBackBufferHeight / scale) / 2 - (height * 44 / 2));
            renderOffset.Y += (center.Z * 4);
            renderOffset.Y -= (firstTile.X + firstTile.Y) * 22f;
            renderOffset.Y -= 22f;
            renderOffset.Y -= firstZOffset * 44f;

            return (firstTile, renderOffset, new Point(width, height));
        }

        private RenderTarget2D _targetRender;

        protected override void Draw(GameTime gameTime)
        {
            if (_targetRender == null || _targetRender.Width != _graphics.PreferredBackBufferWidth || _targetRender.Height != _graphics.PreferredBackBufferHeight)
            {
                _targetRender = new RenderTarget2D(GraphicsDevice, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight,
                    false, SurfaceFormat.Bgra5551, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents);

            }

            _spriteBatch.BeginDraw();

            (Point firstTile, Vector2 renderOffset, Point renderDimensions) = CalculateViewport(new Vector3(_currentX, _y, 5), 3, 6);

            for (int y = 0; y < renderDimensions.Y * 2 + 1 + 10; y++)
            {

                Vector3 drawPosition = new Vector3
                {
                    X = (firstTile.X - firstTile.Y + (y % 2)) * 22f + renderOffset.X,
                    Y = (firstTile.X + firstTile.Y + y) * 22f + renderOffset.Y
                };

                Point firstTileInRow = new Point(firstTile.X + ((y + 1) / 2), firstTile.Y + (y / 2));

                for (int x = 0; x < renderDimensions.X + 1; x++)
                {
                    Game.Map.Tile tile = _facet.GetTile((short)(firstTileInRow.X - x), (short)(firstTileInRow.Y + x));
                    if (tile == null)
                    {
                        drawPosition.X -= 44f;
                        continue;
                    }

                    if (tile.ViewObject.Draw(_spriteBatch, drawPosition))
                    {
                       
                    }

                    for (int i = 0; i < tile.ObjectsOnTiles.Count; i++)
                    {
                        tile.ObjectsOnTiles[i].ViewObject?.Draw(_spriteBatch, drawPosition);
                    }

                    drawPosition.X -= 44f;
                }
            }


            _spriteBatch.GraphicsDevice.SetRenderTarget(_targetRender);
            _spriteBatch.GraphicsDevice.Clear(Color.Black);
            _spriteBatch.EndDraw();
            _spriteBatch.GraphicsDevice.SetRenderTarget(null);



            _spriteBatch.GraphicsDevice.Clear(Color.Transparent);
            _spriteBatch.BeginDraw();

            _spriteBatch.Draw2D(_targetRender, new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight), Vector3.Zero);

            _mouseManager.Draw(_spriteBatch);

            _spriteBatch.EndDraw();


            base.Draw(gameTime);
        }
    }
}
