using System;
using System.Diagnostics;
using ClassicUO.AssetsLoader;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Map;
using ClassicUO.Game.Network;
using ClassicUO.Game.Renderer;
using ClassicUO.Game.WorldObjects;
using ClassicUO.Input;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO
{
    public class GameLoop : Microsoft.Xna.Framework.Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private DateTime _delay = DateTime.Now;
        private CursorRenderer _gameCursor;
        private SpriteBatchUI _spriteBatch;

        private Stopwatch _stopwatch;

        private RenderTarget2D _targetRender;

        private TextRenderer _textRenderer = new TextRenderer("Select which shard to play on:")
        {
            IsUnicode = true,
            Color = 847
        };

        private Texture2D _texture, _crossTexture, _gump, _textentry;

        private DateTime _timePing;

        public GameLoop()
        {
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 144.0f);
            _graphics = new GraphicsDeviceManager(this);

            _graphics.PreparingDeviceSettings += (sender, e) =>
            {
                e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage =
                    RenderTargetUsage.PreserveContents;
            };


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
        }

        private (Point, Point, Vector2, Vector2, Point, Point) GetViewPort()
        {
            var scale = 1;

            var winGamePosX = 0;
            var winGamePosY = 0;

            var winGameWidth = _graphics.PreferredBackBufferWidth;
            var winGameHeight = _graphics.PreferredBackBufferHeight;

            var winGameCenterX = winGamePosX + winGameWidth / 2;
            var winGameCenterY = winGamePosY + winGameHeight / 2 + World.Player.Position.Z * 4;

            winGameCenterX -= (int) World.Player.Offset.X;
            winGameCenterY -= (int) (World.Player.Offset.Y - World.Player.Offset.Z);

            var winDrawOffsetX = (World.Player.Position.X - World.Player.Position.Y) * 22 - winGameCenterX + 22;
            var winDrawOffsetY = (World.Player.Position.X + World.Player.Position.Y) * 22 - winGameCenterY + 22;

            float left = winGamePosX;
            var right = winGameWidth + left;
            float top = winGamePosY;
            var bottom = winGameHeight + top;

            var newRight = right * scale;
            var newBottom = bottom * scale;

            var winGameScaledOffsetX = (int) (left * scale - (newRight - right));
            var winGameScaledOffsetY = (int) (top * scale - (newBottom - bottom));

            var winGameScaledWidth = (int) (newRight - winGameScaledOffsetX);
            var winGameScaledHeight = (int) (newBottom - winGameScaledOffsetY);


            var width = (winGameWidth / 44 + 1) * scale;
            var height = (winGameHeight / 44 + 1) * scale;

            if (width < height)
                width = height;
            else
                height = width;

            var realMinRangeX = World.Player.Position.X - width;
            if (realMinRangeX < 0)
                realMinRangeX = 0;
            var realMaxRangeX = World.Player.Position.X + width;
            if (realMaxRangeX >= Map.MapsDefaultSize[World.Map.Index][0])
                realMaxRangeX = Map.MapsDefaultSize[World.Map.Index][0];

            var realMinRangeY = World.Player.Position.Y - height;
            if (realMinRangeY < 0)
                realMinRangeY = 0;
            var realMaxRangeY = World.Player.Position.Y + height;
            if (realMaxRangeY >= Map.MapsDefaultSize[World.Map.Index][1])
                realMaxRangeY = Map.MapsDefaultSize[World.Map.Index][1];

            //int minBlockX = (realMinRangeX / 8) - 1;
            //int minBlockY = (realMinRangeY / 8) - 1;
            //int maxBlockX = (realMaxRangeX / 8) + 1;
            //int maxBlockY = (realMaxRangeY / 8) + 1;

            //if (minBlockX < 0)
            //    minBlockX = 0;
            //if (minBlockY < 0)
            //    minBlockY = 0;
            //if (maxBlockX >= AssetsLoader.Map.MapsDefaultSize[Game.World.Map.Index][0])
            //    maxBlockX = AssetsLoader.Map.MapsDefaultSize[Game.World.Map.Index][0] - 1;
            //if (maxBlockY >= AssetsLoader.Map.MapsDefaultSize[Game.World.Map.Index][1])
            //    maxBlockY = AssetsLoader.Map.MapsDefaultSize[Game.World.Map.Index][1] - 1;

            var drawOffset = scale * 40;

            float maxX = winGamePosX + winGameWidth + drawOffset;
            float maxY = winGamePosY + winGameHeight + drawOffset;
            var newMaxX = maxX * scale;
            var newMaxY = maxY * scale;

            var minPixelsX = (int) ((winGamePosX - drawOffset) * scale - (newMaxX - maxX));
            var maxPixelsX = (int) newMaxX;
            var minPixelsY = (int) ((winGamePosY - drawOffset) * scale - (newMaxY - maxY));
            var maxPixlesY = (int) newMaxY;

            return (new Point(realMinRangeX, realMinRangeY), new Point(realMaxRangeX, realMaxRangeY),
                new Vector2(minPixelsX, minPixelsY), new Vector2(maxPixelsX, maxPixlesY),
                new Point(winDrawOffsetX, winDrawOffsetY), new Point(winGameCenterX, winGameCenterY));
        }

        protected override void Initialize()
        {
            Window.AllowUserResizing = true;


            _spriteBatch = new SpriteBatchUI(this);

            TextureManager.Device = GraphicsDevice;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // TEST


            /* uncomment it and fill it to save your first settings
            Configuration.Settings settings1 = new Configuration.Settings()
            {
                Username = "",
                Password = "",
                LastCharacterName = "",
                IP = "",
                Port = 2599,
                UltimaOnlineDirectory = "",
                ClientVersion = "7.0.59.8"
            };

            Configuration.ConfigurationResolver.Save(settings1, "settings.json");
            */

            var settings = ConfigurationResolver.Load<Settings>("settings.json");

            var parts = settings.ClientVersion.Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries);


            byte[] clientVersionBuffer =
            {
                byte.Parse(parts[0]),
                byte.Parse(parts[1]),
                byte.Parse(parts[2]),
                byte.Parse(parts[3])
            };


            FileManager.UoFolderPath = settings.UltimaOnlineDirectory;


            _stopwatch = Stopwatch.StartNew();
            Log.Message(LogTypes.Trace, "Loading UO files...");

            FileManager.LoadFiles();

            Log.Message(LogTypes.Trace, "UO files loaded in " + _stopwatch.ElapsedMilliseconds + " ms");


            PacketHandlers.LoadLoginHandlers();
            PacketsTable.AdjustPacketSizeByVersion(FileManager.ClientVersion);


            var textureHue0 = new Texture2D(GraphicsDevice, 32, 3000);
            textureHue0.SetData(Hues.CreateShaderColors());
            GraphicsDevice.Textures[1] = textureHue0;


            _crossTexture = new Texture2D(GraphicsDevice, 1, 1);
            _crossTexture.SetData(new[] {Color.Red});

            var username = settings.Username;
            var password = settings.Password;

            NetClient.PacketReceived += (sender, e) =>
            {
                //Log.Message(LogTypes.Trace, string.Format(">> Received\t\tID:   0x{0:X2}\t\t Length:   {1}", e.ID, e.Length));

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
                        NetClient.Socket.Send(new PSelectCharacter(0, settings.LastCharacterName,
                            BitConverter.ToUInt32(new byte[] {127, 0, 0, 1}, 0)));
                        break;
                    case 0xBD:
                        NetClient.Socket.Send(new PClientVersion(clientVersionBuffer));
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
                    if (e.Button == MouseButton.Right)
                    {
                        var center = new Point(_graphics.PreferredBackBufferWidth / 2,
                            _graphics.PreferredBackBufferHeight / 2);

                        var direction = DirectionHelper.DirectionFromPoints(center, e.Location);

                        World.Player.Walk(direction, true);
                    }
            };


            _gameCursor = new CursorRenderer();

            // END TEST

            base.LoadContent();
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            World.Ticks = (long) gameTime.TotalGameTime.TotalMilliseconds;

            NetClient.Socket.Slice();
            TextureManager.Update();
            MouseManager.Update();
            _gameCursor.Update(gameTime.TotalGameTime.Ticks);

            if (World.Map != null && World.Player != null)
            {
                World.Update(gameTime.TotalGameTime.Ticks);

                if (DateTime.Now > _timePing)
                {
                    NetClient.Socket.Send(new PPing());
                    _timePing = DateTime.Now.AddSeconds(10);
                }
            }

            base.Update(gameTime);
        }

        private void CheckIfUnderEntity(out int maxItemZ, out bool drawTerrain, out bool underSurface)
        {
            maxItemZ = 255;
            drawTerrain = true;
            underSurface = false;

            var tile = World.Map.GetTile(World.Map.Center.X, World.Map.Center.Y);
            if (tile != null &&
                tile.IsZUnderObjectOrGround(World.Player.Position.Z, out var underObject, out var underGround))
            {
                drawTerrain = underGround == null;
                if (underObject != null)
                {
                    if (underObject is Item item)
                    {
                        if (TileData.IsRoof((long) item.ItemData.Flags))
                        {
                            maxItemZ = World.Player.Position.Z - World.Player.Position.Z % 20 + 20;
                        }
                        else if (TileData.IsSurface((long) item.ItemData.Flags) ||
                                 TileData.IsWall((long) item.ItemData.Flags) &&
                                 TileData.IsDoor((long) item.ItemData.Flags))
                        {
                            maxItemZ = item.Position.Z;
                        }
                        else
                        {
                            var z = World.Player.Position.Z + (item.ItemData.Height > 20 ? item.ItemData.Height : 20);
                            maxItemZ = z;
                        }
                    }
                    else if (underObject is Static sta)
                    {
                        if (TileData.IsRoof((long) sta.ItemData.Flags))
                        {
                            maxItemZ = World.Player.Position.Z - World.Player.Position.Z % 20 + 20;
                        }
                        else if (TileData.IsSurface((long) sta.ItemData.Flags) ||
                                 TileData.IsWall((long) sta.ItemData.Flags) &&
                                 TileData.IsDoor((long) sta.ItemData.Flags))
                        {
                            maxItemZ = sta.Position.Z;
                        }
                        else
                        {
                            var z = World.Player.Position.Z + (sta.ItemData.Height > 20 ? sta.ItemData.Height : 20);
                            maxItemZ = z;
                        }
                    }

                    if (underObject is Item i && TileData.IsRoof((long) i.ItemData.Flags) ||
                        underObject is Static s && TileData.IsRoof((long) s.ItemData.Flags))
                    {
                        var isSE = true;
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

        protected override void Draw(GameTime gameTime)
        {
            if (World.Player != null && World.Map != null)
            {
                var scale = 1;

                if (_targetRender == null || _targetRender.Width != _graphics.PreferredBackBufferWidth / scale ||
                    _targetRender.Height != _graphics.PreferredBackBufferHeight / scale)
                {
                    if (_targetRender != null)
                        _targetRender.Dispose();

                    _targetRender = new RenderTarget2D(GraphicsDevice, _graphics.PreferredBackBufferWidth / scale,
                        _graphics.PreferredBackBufferHeight / scale,
                        false, SurfaceFormat.Bgra5551, DepthFormat.Depth24Stencil8, 0,
                        RenderTargetUsage.DiscardContents);
                }


                var (minChunkTile, maxChunkTile, minPixel, maxPixel, offset, center) = GetViewPort();

                CheckIfUnderEntity(out var maxItemZ, out var drawTerrain, out var underSurface);

                _spriteBatch.BeginDraw();
                _spriteBatch.SetLightIntensity(World.Light.IsometricLevel);
                _spriteBatch.SetLightDirection(World.Light.IsometricDirection);

                var minX = minChunkTile.X;
                var minY = minChunkTile.Y;
                var maxX = maxChunkTile.X;
                var maxY = maxChunkTile.Y;

                var mapBlockHeight = Map.MapBlocksSize[World.Map.Index][1];

                for (var i = 0; i < 2; i++)
                {
                    var minValue = minY;
                    var maxValue = maxY;

                    if (i > 0)
                    {
                        minValue = minX;
                        maxValue = maxX;
                    }

                    for (var lead = minValue; lead < maxValue; lead++)
                    {
                        var x = minX;
                        var y = lead;

                        if (i > 0)
                        {
                            x = lead;
                            y = maxY;
                        }

                        while (true)
                        {
                            if (x < minX || x > maxX || y < minY || y > maxY)
                                break;

                            var draw = true;

                            var tile = World.Map.GetTile((short) x, (short) y);

                            if (tile != null)
                            {
                                var position = new Vector3(
                                    (x - y) * 22f - offset.X,
                                    (x + y) * 22f - offset.Y, 0);


                                for (var k = 0; k < tile.ObjectsOnTiles.Count; k++)
                                {
                                    var o = tile.ObjectsOnTiles[k];


                                    if (!drawTerrain)
                                        if (o is Tile || o.Position.Z > tile.Position.Z)
                                            draw = false;

                                    if ((o.Position.Z >= maxItemZ ||
                                         maxItemZ != 255 &&
                                         (o is Item item && TileData.IsRoof((long) item.ItemData.Flags) ||
                                          o is Static st && TileData.IsRoof((long) st.ItemData.Flags)))
                                        && !(o is Tile))
                                        continue;


                                    if (draw) o.ViewObject.Draw(_spriteBatch, position);
                                }
                            }

                            x++;
                            y--;
                        }
                    }
                }

                //foreach (var obj in worldObjects)
                //    obj.Item2.ViewObject.Draw(_spriteBatch, obj.Item1);

                _spriteBatch.GraphicsDevice.SetRenderTarget(_targetRender);
                _spriteBatch.GraphicsDevice.Clear(Color.Black);
                _spriteBatch.EndDraw(true);
                _spriteBatch.GraphicsDevice.SetRenderTarget(null);
            }

            _spriteBatch.GraphicsDevice.Clear(Color.Transparent);
            _spriteBatch.BeginDraw();

            _spriteBatch.Draw2D(_targetRender,
                new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight),
                Vector3.Zero);

            //_spriteBatch.Draw2D(_crossTexture, new Rectangle(_graphics.PreferredBackBufferWidth / 2  - 5, _graphics.PreferredBackBufferHeight / 2 - 5, 10, 10), Vector3.Zero);

            //_spriteBatch.Draw2D(_textentry, new Vector3(0, 0, 0), Vector3.Zero);

            //_textRenderer.Draw(_spriteBatch, new Point(100, 150));

            //_mouseManager.Draw(_spriteBatch);

            _gameCursor.Draw(_spriteBatch);

            _spriteBatch.EndDraw();


            base.Draw(gameTime);
        }
    }
}