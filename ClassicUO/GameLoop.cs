using ClassicUO.AssetsLoader;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.GameObjects.Interfaces;
using ClassicUO.Game.Map;
using ClassicUO.Game.Network;
using ClassicUO.Game.Renderer;
using ClassicUO.Input;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ClassicUO
{
    public class GameLoop : Microsoft.Xna.Framework.Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private FpsCounter _fpsCounter;
        private CursorRenderer _gameCursor;
        private SpriteBatch3D _spriteBatch;
        private Stopwatch _stopwatch;
        private RenderTarget2D _targetRender;
        private Texture2D _texture;
        private Texture2D _crossTexture;
        private Texture2D _gump;
        private readonly Texture2D _textentry;
        private DateTime _timePing;

        private GameText _gameTextTRY;

        public GameLoop()
        {
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 144.0f);
            _graphics = new GraphicsDeviceManager(this);

            //IsFixedTimeStep = false;

            _graphics.PreparingDeviceSettings += (sender, e) => { e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents; };


            if (_graphics.GraphicsDevice.Adapter.IsProfileSupported(GraphicsProfile.HiDef))
                _graphics.GraphicsProfile = GraphicsProfile.HiDef;

            _graphics.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
            _graphics.SynchronizeWithVerticalRetrace = false;
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 600;
            _graphics.ApplyChanges();


            Log.Message(LogTypes.Trace, "Gameloop initialized.");

            Window.ClientSizeChanged += (sender, e) =>
            {
                _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
                _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;

                _graphics.ApplyChanges();
            };

            //TextInputEXT.TextInput += c =>
            //{
            //    Log.Message(LogTypes.Error, c.ToString());
            //};
            //TextInputEXT.StartTextInput();

            //KeyboardManager.KeyPressed += (sender, e) =>
            //{
            //    Log.Message(LogTypes.Info, e.Key.ToString());
            //};
        }

        protected override void Initialize()
        {
            Window.AllowUserResizing = true;


            _spriteBatch = new SpriteBatch3D(this);

            TextureManager.Device = GraphicsDevice;
            TextureManager.Device.DepthStencilState = DepthStencilState.Default;
            _graphics.ApplyChanges();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // TEST


            // uncomment it and fill it to save your first settings
            /*Configuration.Settings settings1 = new Configuration.Settings()
            {
                Username = "",
                Password = "",
                LastCharacterName = "",
                IP = "",
                Port = 2599,
                UltimaOnlineDirectory = "",
                ClientVersion = "7.0.59.8"
            };

            Configuration.ConfigurationResolver.Save(settings1, "settings.json");*/
            

            Settings settings = ConfigurationResolver.Load<Settings>("settings.json");

            string[] parts = settings.ClientVersion.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);


            byte[] clientVersionBuffer = { byte.Parse(parts[0]), byte.Parse(parts[1]), byte.Parse(parts[2]), byte.Parse(parts[3]) };


            FileManager.UoFolderPath = settings.UltimaOnlineDirectory;


            _stopwatch = Stopwatch.StartNew();
            Log.Message(LogTypes.Trace, "Loading UO files...");

            FileManager.LoadFiles();

            Log.Message(LogTypes.Trace, "UO files loaded in " + _stopwatch.ElapsedMilliseconds + " ms");


            PacketHandlers.LoadLoginHandlers();
            PacketsTable.AdjustPacketSizeByVersion(FileManager.ClientVersion);


            Texture2D textureHue0 = new Texture2D(GraphicsDevice, 32, 3000);
            textureHue0.SetData(Hues.CreateShaderColors());
            GraphicsDevice.Textures[1] = textureHue0;

            _fpsCounter = new FpsCounter();

            _crossTexture = new Texture2D(GraphicsDevice, 1, 1);
            _crossTexture.SetData(new[] { Color.Red });

            string username = settings.Username;
            string password = settings.Password;

            NetClient.PacketReceived += (sender, e) =>
            {
                //Log.Message(LogTypes.Trace, string.Format(">> Received \t\tID:   0x{0:X2}\t\t Length:   {1}", e.ID, e.Length));

                switch (e.ID)
                {
                    case 0xA8:
                        NetClient.Socket.Send(new PSelectServer(0));
                        break;
                    case 0x8C:
                        NetClient.Socket.EnableCompression();
                        e.Seek(0);
                        e.MoveToData();
                        e.Skip(6);
                        NetClient.Socket.Send(new PSecondLogin(username, password, e.ReadUInt()));
                        break;
                    case 0xA9:
                        NetClient.Socket.Send(new PSelectCharacter(0, settings.LastCharacterName, NetClient.Socket.ClientAddress));
                        break;
                    case 0xBD:
                        NetClient.Socket.Send(new PClientVersion(clientVersionBuffer));
                        break;
                    case 0xBE:
                        NetClient.Socket.Send(new PAssistVersion(clientVersionBuffer, e.ReadUInt()));
                        break;
                    case 0x55:
                        NetClient.Socket.Send(new PClientViewRange(24));
                        break;
                }
            };

            NetClient.PacketSended += (sender, e) =>
            {
                //Log.Message(LogTypes.Trace, string.Format("<< Sended\t\tID:   0x{0:X2}\t\t Length:   {1}", e.ID, e.Length));
            };

            NetClient.Connected += (sender, e) =>
            {
                Log.Message(LogTypes.Info, "Connected!");
                NetClient.Socket.Send(new PSeed(clientVersionBuffer));
                NetClient.Socket.Send(new PFirstLogin(username, password));
            };

            NetClient.Disconnected += (sender, e) => { Log.Message(LogTypes.Warning, "Disconnected!"); };


            NetClient.Socket.Connect(settings.IP, settings.Port);

            MouseManager.MousePressed += (sender, e) =>
            {
                if (World.Map != null && World.Player != null)
                {
                    if (e.Button == MouseButton.Right)
                    {
                        Point center = new Point(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 2);

                        Direction direction = DirectionHelper.DirectionFromPoints(center, e.Location);

                        World.Player.Walk(direction, true);
                    }
                }
            };


            _gameCursor = new CursorRenderer();

            _gameTextTRY = new GameText()
            {
                IsUnicode = true,
                Align = TEXT_ALIGN_TYPE.TS_LEFT,
                Text = "FPS: 0",
                FontStyle = FontStyle.BlackBorder,
                Font = 0,
                IsPersistent = true,
                IsHTML = false
            };

            _gump = TextureManager.GetOrCreateGumpTexture(0x64);

            _texture = new Texture2D(GraphicsDevice, 1, 1);
            _texture.SetData(new Color[1] { Color.White });

            // END TEST

            base.LoadContent();
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            World.Ticks = (long)gameTime.TotalGameTime.TotalMilliseconds;
            if (IsActive)
            {
                MouseManager.Update();
                KeyboardManager.Update();
            }


            _fpsCounter.Update(gameTime);

            NetClient.Socket.Slice();
            TextureManager.Update();

            Game.Gumps.GumpManager.Update(gameTime.ElapsedGameTime.Milliseconds);
            _gameCursor.Update(gameTime.ElapsedGameTime.Milliseconds);

            if (World.Map != null && World.Player != null)
            {
                World.Update(gameTime.ElapsedGameTime.Milliseconds);

                if (DateTime.Now > _timePing)
                {
                    NetClient.Socket.Send(new PPing());
                    _timePing = DateTime.Now.AddSeconds(10);
                }
            }

            base.Update(gameTime);
        }

        private static void CheckIfUnderEntity(out int maxItemZ, out bool drawTerrain, out bool underSurface)
        {
            maxItemZ = 255;
            drawTerrain = true;
            underSurface = false;

            Tile tile = World.Map.GetTile(World.Map.Center.X, World.Map.Center.Y);
            if (tile != null && tile.IsZUnderObjectOrGround(World.Player.Position.Z, out GameObject underObject, out GameObject underGround))
            {
                drawTerrain = underGround == null;
                if (underObject != null)
                {
                    if (underObject is Item item)
                    {
                        if (TileData.IsRoof((long)item.ItemData.Flags))
                            maxItemZ = World.Player.Position.Z - World.Player.Position.Z % 20 + 20;
                        else if (TileData.IsSurface((long)item.ItemData.Flags) || TileData.IsWall((long)item.ItemData.Flags) && TileData.IsDoor((long)item.ItemData.Flags))
                            maxItemZ = item.Position.Z;
                        else
                        {
                            int z = World.Player.Position.Z + (item.ItemData.Height > 20 ? item.ItemData.Height : 20);
                            maxItemZ = z;
                        }
                    }
                    else if (underObject is Static sta)
                    {
                        if (TileData.IsRoof((long)sta.ItemData.Flags))
                            maxItemZ = World.Player.Position.Z - World.Player.Position.Z % 20 + 20;
                        else if (TileData.IsSurface((long)sta.ItemData.Flags) || TileData.IsWall((long)sta.ItemData.Flags) && TileData.IsDoor((long)sta.ItemData.Flags))
                            maxItemZ = sta.Position.Z;
                        else
                        {
                            int z = World.Player.Position.Z + (sta.ItemData.Height > 20 ? sta.ItemData.Height : 20);
                            maxItemZ = z;
                        }
                    }

                    if (underObject is Item i && TileData.IsRoof((long)i.ItemData.Flags) || underObject is Static s && TileData.IsRoof((long)s.ItemData.Flags))
                    {
                        bool isSE = true;
                        if ((tile = World.Map.GetTile(World.Map.Center.X + 1, World.Map.Center.Y)) != null)
                        {
                            tile.IsZUnderObjectOrGround(World.Player.Position.Z, out underObject, out underGround);
                            isSE = underObject != null;
                        }

                        if (!isSE)
                            maxItemZ = 255;
                    }

                    underSurface = maxItemZ != 255;
                }
            }
        }

        private (Point, Point, Vector2, Vector2, Point, Point, Point, int) GetViewPort()
        {
            int scale = 1;

            int winGamePosX = 0;
            int winGamePosY = 0;

            int winGameWidth = _graphics.PreferredBackBufferWidth;
            int winGameHeight = _graphics.PreferredBackBufferHeight;

            int winGameCenterX = winGamePosX + winGameWidth / 2;
            int winGameCenterY = winGamePosY + winGameHeight / 2 + World.Player.Position.Z * 4;

            winGameCenterX -= (int)World.Player.Offset.X;
            winGameCenterY -= (int)(World.Player.Offset.Y - World.Player.Offset.Z);

            int winDrawOffsetX = (World.Player.Position.X - World.Player.Position.Y) * 22 - winGameCenterX + 22;
            int winDrawOffsetY = (World.Player.Position.X + World.Player.Position.Y) * 22 - winGameCenterY + 22;

            float left = winGamePosX;
            float right = winGameWidth + left;
            float top = winGamePosY;
            float bottom = winGameHeight + top;

            float newRight = right * scale;
            float newBottom = bottom * scale;

            int winGameScaledOffsetX = (int)(left * scale - (newRight - right));
            int winGameScaledOffsetY = (int)(top * scale - (newBottom - bottom));

            int winGameScaledWidth = (int)(newRight - winGameScaledOffsetX);
            int winGameScaledHeight = (int)(newBottom - winGameScaledOffsetY);


            int width = (winGameWidth / 44 + 1) * scale;
            int height = (winGameHeight / 44 + 1) * scale;

            if (width < height)
                width = height;
            else
                height = width;

            int realMinRangeX = World.Player.Position.X - width;
            if (realMinRangeX < 0)
                realMinRangeX = 0;
            int realMaxRangeX = World.Player.Position.X + width;
            if (realMaxRangeX >= Map.MapsDefaultSize[World.Map.Index][0])
                realMaxRangeX = Map.MapsDefaultSize[World.Map.Index][0];

            int realMinRangeY = World.Player.Position.Y - height;
            if (realMinRangeY < 0)
                realMinRangeY = 0;
            int realMaxRangeY = World.Player.Position.Y + height;
            if (realMaxRangeY >= Map.MapsDefaultSize[World.Map.Index][1])
                realMaxRangeY = Map.MapsDefaultSize[World.Map.Index][1];

            int minBlockX = realMinRangeX / 8 - 1;
            int minBlockY = realMinRangeY / 8 - 1;
            int maxBlockX = realMaxRangeX / 8 + 1;
            int maxBlockY = realMaxRangeY / 8 + 1;

            if (minBlockX < 0)
                minBlockX = 0;
            if (minBlockY < 0)
                minBlockY = 0;
            if (maxBlockX >= Map.MapsDefaultSize[World.Map.Index][0])
                maxBlockX = Map.MapsDefaultSize[World.Map.Index][0] - 1;
            if (maxBlockY >= Map.MapsDefaultSize[World.Map.Index][1])
                maxBlockY = Map.MapsDefaultSize[World.Map.Index][1] - 1;

            int drawOffset = scale * 40;

            float maxX = winGamePosX + winGameWidth + drawOffset;
            float maxY = winGamePosY + winGameHeight + drawOffset;
            float newMaxX = maxX * scale;
            float newMaxY = maxY * scale;

            int minPixelsX = (int)((winGamePosX - drawOffset) * scale - (newMaxX - maxX));
            int maxPixelsX = (int)newMaxX;
            int minPixelsY = (int)((winGamePosY - drawOffset) * scale - (newMaxY - maxY));
            int maxPixlesY = (int)newMaxY;

            return (new Point(realMinRangeX, realMinRangeY), new Point(realMaxRangeX, realMaxRangeY), new Vector2(minPixelsX, minPixelsY), new Vector2(maxPixelsX, maxPixlesY), new Point(winDrawOffsetX, winDrawOffsetY), new Point(winGameCenterX, winGameCenterY), new Point(realMinRangeX + width - 1, realMinRangeY - 1), Math.Max(width, height));
        }

        private (Point firstTile, Vector2 renderOffset, Point renderDimensions) GetViewPort2()
        {
            int scale = 1;

            Point renderDimensions = new Point { X = _graphics.PreferredBackBufferWidth / scale / 44 + 5, Y = _graphics.PreferredBackBufferHeight / scale / 44 + 5 };

            int width = renderDimensions.X;
            int height = renderDimensions.Y;

            if (width > height)
                height = width;
            else
                width = height;

            int diff = Math.Max(width, height);

            int renderDimensionDiff = Math.Abs(renderDimensions.X - renderDimensions.Y);
            renderDimensionDiff -= renderDimensionDiff % 2;

            int firstZOffset = World.Player.Position.Z > 0 ? (int)Math.Abs((World.Player.Position.Z + World.Player.Offset.Z / 4) / 11) : 0;

            Point firstTile = new Point { X = World.Player.Position.X - firstZOffset, Y = World.Player.Position.Y - renderDimensions.Y - firstZOffset };

            if (renderDimensions.Y > renderDimensions.X)
            {
                firstTile.X -= renderDimensionDiff / 2;
                firstTile.Y -= renderDimensionDiff / 2;
            }
            else
            {
                firstTile.X += renderDimensionDiff / 2;
                firstTile.Y -= renderDimensionDiff / 2;
            }

            Vector2 renderOffset = new Vector2 { X = (_graphics.PreferredBackBufferWidth / scale + renderDimensions.Y * 44) / 2 - 22f - (int)(World.Player.Offset.X * 1) - (firstTile.X - firstTile.Y) * 22f + renderDimensionDiff * 22f, Y = _graphics.PreferredBackBufferHeight / scale / 2 - renderDimensions.Y * 44 / 2 + (World.Player.Position.Z + World.Player.Offset.Z / 4) * 4 - (int)(World.Player.Offset.Y * 1) - (firstTile.X + firstTile.Y) * 22f - 22f - firstZOffset * 44f };

            return (firstTile, renderOffset, renderDimensions);
        }


        protected override void Draw(GameTime gameTime)
        {
           
            if (World.Player != null && World.Map != null)
            {
                _fpsCounter.IncreaseFrame();

                int scale = 1;

                if (_targetRender == null || _targetRender.Width != _graphics.PreferredBackBufferWidth / scale || _targetRender.Height != _graphics.PreferredBackBufferHeight / scale)
                {
                    _targetRender?.Dispose();

                    _targetRender = new RenderTarget2D(GraphicsDevice, _graphics.PreferredBackBufferWidth / scale, _graphics.PreferredBackBufferHeight / scale, false, SurfaceFormat.Bgra5551, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents);
                }


                (Point minTile, Point maxTile, Vector2 minPixel, Vector2 maxPixel, Point offset, Point center, Point firstTile, int renderDimensions) = GetViewPort();

                CheckIfUnderEntity(out int maxItemZ, out bool drawTerrain, out bool underSurface);

                //(Point firstTile, Vector2 renderOffset, Point renderDimensions) = GetViewPort2();

                _spriteBatch.BeginDraw();
                _spriteBatch.SetLightIntensity(World.Light.IsometricLevel);
                _spriteBatch.SetLightDirection(World.Light.IsometricDirection);

                List<DeferredEntity> toremove = new List<DeferredEntity>();


                int minX = minTile.X;
                int minY = minTile.Y;
                int maxX = maxTile.X;
                int maxY = maxTile.Y;


                for (int y = minY; y < maxY; y++)
                {
                    for (int x = minX; x < maxX; x++)
                    {
                        Tile tile = World.Map.GetTile(x, y);
                        if (tile == null)
                            continue;

                        Vector3 isometricPosition = new Vector3((x - y) * 22 - offset.X, (x + y) * 22 - offset.Y, 0);

                        var objects = tile.ObjectsOnTiles;

                        bool draw = true;

                        for (int k = 0; k < objects.Count; k++)
                        {
                            var obj = objects[k];

                            if (obj is DeferredEntity d)
                                toremove.Add(d);

                            if (!drawTerrain)
                            {
                                if (obj is Tile || obj.Position.Z > tile.Position.Z)
                                    draw = false;
                            }

                            if ((obj.Position.Z >= maxItemZ
                                || maxItemZ != 255 && obj is IDynamicItem dyn && TileData.IsRoof((long)dyn.ItemData.Flags))
                                && !(obj is Tile))
                                continue;

                            if (draw)
                                obj.View.Draw(_spriteBatch, isometricPosition);
                        }

                        foreach (var o in toremove)
                        {
                            o.Reset();
                            tile.RemoveWorldObject(o);
                        }

                        toremove.Clear();
                    }
                }


                //for (int i = 0; i < 2; i++)
                //{
                //    int minValue = minY;
                //    int maxValue = maxY;

                //    if (i > 0)
                //    {
                //        minValue = minX;
                //        maxValue = maxX;
                //    }

                //    for (int lead = minValue; lead < maxValue; lead++)
                //    {
                //        int x = minX;
                //        int y = lead;

                //        if (i > 0)
                //        {
                //            x = lead;
                //            y = maxY;
                //        }

                //        while (true)
                //        {
                //            if (x < minX || x > maxX || y < minY || y > maxY)
                //                break;

                //            Tile tile = World.Map.GetTile(x, y);
                //            if (tile != null)
                //            {
                //                Vector3 isometricPosition = new Vector3(
                //                    tile.IsometricPosition.X - offset.X,
                //                    tile.IsometricPosition.Y - offset.Y,
                //                    0);

                //                var objects = tile.ObjectsOnTiles;

                //                for (int k = 0; k < objects.Count; k++)
                //                {
                //                    var obj = objects[k];

                //                    if (obj is DeferredEntity)
                //                        toremove.Add(obj);

                //                    obj.View.Draw(_spriteBatch, isometricPosition);
                //                }

                //                foreach (var o in toremove)
                //                    tile.RemoveWorldObject(o);

                //                toremove.Clear();

                //            }


                //            x++;
                //            y--;

                //        }
                //    }
                //}


                _spriteBatch.GraphicsDevice.SetRenderTarget(_targetRender);
                _spriteBatch.GraphicsDevice.Clear(ClearOptions.DepthBuffer | ClearOptions.Target, Color.Black, 1, 0);
                _spriteBatch.FlushSprites(true);
                _spriteBatch.GraphicsDevice.SetRenderTarget(null);
            }

            _spriteBatch.GraphicsDevice.Clear(Color.Transparent);
            _spriteBatch.BeginDraw();

            _spriteBatch.Draw2D(_targetRender, new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight), Vector3.Zero);
            GameTextRenderer.Render(_spriteBatch);

            //_spriteBatch.Draw2D(_crossTexture, new Bounds(_graphics.PreferredBackBufferWidth / 2  - 5, _graphics.PreferredBackBufferHeight / 2 - 5, 10, 10), Vector3.Zero);

            //_spriteBatch.Draw2D(_textentry, new Vector3(0, 0, 0), Vector3.Zero);

            //_textRenderer.Draw(_spriteBatch, new Point(100, 150));

            //_mouseManager.Draw(_spriteBatch);

            _gameTextTRY.Text = "FPS: " + _fpsCounter.FPS;
            _gameTextTRY.View.Draw(_spriteBatch, new Vector3(Window.ClientBounds.Width - 100, 20, 0));

            //_spriteBatch.Draw2D(_gump, new Rectangle(100, 100, _gump.Width, _gump.Height), Vector3.Zero);

            //_spriteBatch.DrawLine(_texture, new Vector2(0, 120), new Vector2(Window.ClientBounds.Width, 120), Vector3.Zero);
            //_spriteBatch.DrawRectangle(_texture, new Rectangle(2, 120, 100, 100), Vector3.Zero);

            Game.Gumps.GumpManager.Render(_spriteBatch);
            _gameCursor.Draw(_spriteBatch);
            _spriteBatch.FlushSprites(true);



            base.Draw(gameTime);
        }
    }
}