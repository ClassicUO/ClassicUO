using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Game.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO
{
    public class GameLoop : Microsoft.Xna.Framework.Game
    {
        private readonly GraphicsDeviceManager _graphics;
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

            var settings = Configuration.ConfigurationResolver.Load<Configuration.Settings>("settings.json");

            var parts = settings.ClientVersion.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);



            byte[] clientVersionBuffer =
            {
                byte.Parse(parts[0]),
                byte.Parse(parts[1]),
                byte.Parse(parts[2]),
                byte.Parse(parts[3]),
            };



            AssetsLoader.FileManager.UoFolderPath = settings.UltimaOnlineDirectory;




            _stopwatch = Stopwatch.StartNew();
            Log.Message(LogTypes.Trace, "Loading UO files...");

            AssetsLoader.FileManager.LoadFiles();

            Log.Message(LogTypes.Trace, "UO files loaded in " + _stopwatch.ElapsedMilliseconds + " ms");



            PacketHandlers.LoadLoginHandlers();
            PacketsTable.AdjustPacketSizeByVersion(AssetsLoader.FileManager.ClientVersion);



            Texture2D textureHue0 = new Texture2D(GraphicsDevice, 32, 3000);

            uint[] hues = new uint[32 * 2 * 3000];
            int index = 0; // 32

            foreach (var range in AssetsLoader.Hues.HuesRange)
            {
                foreach (var entry in range.Entries)
                {
                    foreach (var c in entry.ColorTable)
                    {
                        hues[index++] = AssetsLoader.Hues.Color16To32(c);
                    }
                }
            }

            textureHue0.SetData(hues);

            GraphicsDevice.Textures[1] = textureHue0;



            var data = AssetsLoader.Art.ReadStaticArt(3850, out short w, out short h);

            _texture = new Texture2D(GraphicsDevice, w, h, false, SurfaceFormat.Bgra5551);
            _texture.SetData(data);

            _crossTexture = new Texture2D(GraphicsDevice, 1, 1);
            _crossTexture.SetData(new Color[] { Color.Red });

            KeyboardManager.KeyPressed += (sender, e) =>
            {
                //if (e.KeyState == Microsoft.Xna.Framework.Input.KeyState.Down)
                //{
                //    switch (e.Key)
                //    {
                //        case Microsoft.Xna.Framework.Input.Keys.Left:
                //            _currentX--;
                //            _y++;
                //            break;
                //        case Microsoft.Xna.Framework.Input.Keys.Up:
                //            _y--;
                //            _currentX--;
                //            break;
                //        case Microsoft.Xna.Framework.Input.Keys.Right:
                //            _currentX++;
                //            _y--;
                //            break;
                //        case Microsoft.Xna.Framework.Input.Keys.Down:
                //            _y++;
                //            _currentX++;
                //            break;
                //    }
                //}
            };

            string username = settings.Username;
            string password = settings.Password;

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
                        NetClient.Socket.Send(new PSelectCharacter(0, settings.LastCharacterName, BitConverter.ToUInt32(new byte[] { 127, 0, 0, 1 }, 0)));
                        break;
                    case 0xBD:
                        NetClient.Socket.Send(new PClientVersion(clientVersionBuffer));
                        break;
                }
            };

            NetClient.PacketSended += (sender, e) =>
            {
                Log.Message(LogTypes.Trace, string.Format("<< Sended\t\tID:   0x{0:X2}\t\t Length:   {1}", e.ID, e.Length));
            };

            NetClient.Connected += (sender, e) =>
            {
                Log.Message(LogTypes.Trace, "Connected!");
                NetClient.Socket.Send(new PSeed(clientVersionBuffer));
                NetClient.Socket.Send(new PFirstLogin(username, password));
            };

            NetClient.Disconnected += (sender, e) =>
            {
                Log.Message(LogTypes.Warning, "Disconnected!");
            };


            AssetsLoader.Fonts.SetUseHTML(true);
            AssetsLoader.Fonts.RecalculateWidthByInfo = true;
            //_textRenderer.GenerateTexture(0, 0, AssetsLoader.TEXT_ALIGN_TYPE.TS_LEFT, 0, 30);

            NetClient.Socket.Connect(settings.IP, settings.Port);

            int font = 0;
            MouseManager.MouseMove += (sender, e) =>
            {
                //if (font + 1 > 20)
                //    font = 0;
                //_textRenderer.Text = string.Format("Mouse screen location: {0},{1}   FONT: {2}", e.Location.X, e.Location.Y, font);
                //_textRenderer.GenerateTexture(0, 0, AssetsLoader.TEXT_ALIGN_TYPE.TS_LEFT, font, 30);
                //font++;
                //_textRenderer.Text = string.Format("Mouse screen location: {0},{1}   FONT: {2}", e.Location.X, e.Location.Y, font);
                //_textRenderer.GenerateTexture(0, 0, AssetsLoader.TEXT_ALIGN_TYPE.TS_LEFT, font++);

            };


            MouseManager.MousePressed += (sender, e) =>
            {
                if (Game.World.Map != null && Game.World.Player != null)
                {
                    if (e.Button == MouseButton.Right)
                    {

                        Point center = new Point(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 2);

                        Game.Direction direction = Game.DirectionHelper.DirectionFromPoints(center, e.Location);

                        Game.World.Player.Walk(direction, true);
                    }
                }
            };



            var datagump = AssetsLoader.Gumps.GetGump(0x2329, out int gw, out int gh);
            _gump = new Texture2D(GraphicsDevice, gw, gh, false, SurfaceFormat.Bgra5551);
            _gump.SetData(datagump);

            var datatextentry = AssetsLoader.Gumps.GetGump(9350 + 4, out gw, out gh);
            _textentry = new Texture2D(GraphicsDevice, gw, gh, false, SurfaceFormat.Bgra5551);
            _textentry.SetData(datatextentry);


            _gameCursor = new Game.Renderer.CursorRenderer();

            // END TEST

            base.LoadContent();
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();
        }



        const double TIME_RUN_MOUNT = (2d / 20d) * 1000d;
        private DateTime _delay = DateTime.Now;

        //private ushort _x = 1446, _y = 1665;
        //private sbyte _z = 0;
        //private ushort _maxX = 5454;
        //private ushort _currentX = 1446;
        private Stopwatch _stopwatch;
        private Texture2D _texture, _crossTexture, _gump, _textentry;
        private Game.Renderer.CursorRenderer _gameCursor;

        private TextRenderer _textRenderer = new TextRenderer("Select which shard to play on:")
        {
            IsUnicode = true,
            Color = 847
        };

        private DateTime _timePing;


        protected override void Update(GameTime gameTime)
        {

            Game.World.Ticks = (long)gameTime.TotalGameTime.TotalMilliseconds;



            NetClient.Socket.Slice();

            TextureManager.UpdateTicks();

            MouseManager.Update();
            _gameCursor.Update(gameTime.TotalGameTime.Ticks);


            if (Game.World.Map != null && Game.World.Player != null)
            {

                //if (Game.World.Player.Position.X != _currentX || Game.World.Player.Position.Y != _y)
                //{
                //    _currentX = (ushort)Game.World.Map.Center.X;
                //    _y = (ushort)Game.World.Map.Center.Y;
                //    _z = Game.World.Player.Position.Z;
                //}


                Game.World.Update(gameTime.TotalGameTime.Ticks);



                if (DateTime.Now > _timePing)
                {
                    NetClient.Socket.Send(new PPing());
                    _timePing = DateTime.Now.AddSeconds(10);
                }
            }

            base.Update(gameTime);
        }


        private (Point, Point, Vector2, Vector2, Point, Point) GetViewPort()
        {
            int scale = 1;

            int winGamePosX = 0;
            int winGamePosY = 0;

            int winGameWidth = _graphics.PreferredBackBufferWidth;
            int winGameHeight = _graphics.PreferredBackBufferHeight;

            int winGameCenterX = winGamePosX + (winGameWidth / 2);
            int winGameCenterY = (winGamePosY + winGameHeight / 2) + (Game.World.Player.Position.Z * 4);

            winGameCenterX -= (int)Game.World.Player.Offset.X;
            winGameCenterY -= (int)(Game.World.Player.Offset.Y - Game.World.Player.Offset.Z);

            int winDrawOffsetX = ((Game.World.Player.Position.X - Game.World.Player.Position.Y) * 22) - winGameCenterX + 22;
            int winDrawOffsetY = ((Game.World.Player.Position.X + Game.World.Player.Position.Y) * 22) - winGameCenterY + 22;

            float left = winGamePosX;
            float right = winGameWidth + left;
            float top = winGamePosY;
            float bottom = winGameHeight + top;

            float newRight = right * scale;
            float newBottom = bottom * scale;

            int winGameScaledOffsetX = (int)((left * scale) - (newRight - right));
            int winGameScaledOffsetY = (int)((top * scale) - (newBottom - bottom));

            int winGameScaledWidth = (int)(newRight - winGameScaledOffsetX);
            int winGameScaledHeight = (int)(newBottom - winGameScaledOffsetY);


            int width = ((winGameWidth / 44) + 1) * scale;
            int height = ((winGameHeight / 44) + 1) * scale;

            if (width < height)
                width = height;
            else
                height = width;

            int realMinRangeX = Game.World.Player.Position.X - width;
            if (realMinRangeX < 0)
                realMinRangeX = 0;
            int realMaxRangeX = Game.World.Player.Position.X + width;
            if (realMaxRangeX >= AssetsLoader.Map.MapsDefaultSize[Game.World.Map.Index][0])
                realMaxRangeX = AssetsLoader.Map.MapsDefaultSize[Game.World.Map.Index][0];

            int realMinRangeY = Game.World.Player.Position.Y - height;
            if (realMinRangeY < 0)
                realMinRangeY = 0;
            int realMaxRangeY = Game.World.Player.Position.Y + height;
            if (realMaxRangeY >= AssetsLoader.Map.MapsDefaultSize[Game.World.Map.Index][1])
                realMaxRangeY = AssetsLoader.Map.MapsDefaultSize[Game.World.Map.Index][1];

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

            int drawOffset = scale * 40;

            float maxX = winGamePosX + winGameWidth + drawOffset;
            float maxY = winGamePosY + winGameHeight + drawOffset;
            float newMaxX = maxX * scale;
            float newMaxY = maxY * scale;

            int minPixelsX = (int)(((winGamePosX - drawOffset) * scale) - (newMaxX - maxX));
            int maxPixelsX = (int)newMaxX;
            int minPixelsY = (int)(((winGamePosY - drawOffset) * scale) - (newMaxY - maxY));
            int maxPixlesY = (int)newMaxY;

            return (new Point(realMinRangeX, realMinRangeY), new Point(realMaxRangeX, realMaxRangeY),
                new Vector2(minPixelsX, minPixelsY), new Vector2(maxPixelsX, maxPixlesY),
                new Point(winDrawOffsetX, winDrawOffsetY), new Point(winGameCenterX, winGameCenterY));
        }

        private RenderTarget2D _targetRender;

        protected override void Draw(GameTime gameTime)
        {
            if (Game.World.Player != null && Game.World.Map !=  null)
            {
                int scale = 1;

                if (_targetRender == null || _targetRender.Width != _graphics.PreferredBackBufferWidth / scale || _targetRender.Height != _graphics.PreferredBackBufferHeight / scale)
                {
                    if (_targetRender != null)
                        _targetRender.Dispose();

                    _targetRender = new RenderTarget2D(GraphicsDevice, _graphics.PreferredBackBufferWidth / scale, _graphics.PreferredBackBufferHeight / scale,
                        false, SurfaceFormat.Bgra5551, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents);

                }

                _spriteBatch.BeginDraw();


                (Point minChunkTile, Point maxChunkTile, Vector2 minPixel, Vector2 maxPixel, Point offset, Point center) = GetViewPort();

                //if (Game.World.Map.Center != center)
                //{
                //    Game.World.Map.Center = center;
                //}

                int minX = minChunkTile.X;
                int minY = minChunkTile.Y;
                int maxX = maxChunkTile.X;
                int maxY = maxChunkTile.Y;

                int mapBlockHeight = AssetsLoader.Map.MapBlocksSize[Game.World.Map.Index][1];


                for (int i = 0; i < 2; i++)
                {
                    int minValue = minY;
                    int maxValue = maxY;

                    if (i > 0)
                    {
                        minValue = minX;
                        maxValue = maxX;
                    }

                    for (int lead = minValue; lead < maxValue; lead++)
                    {
                        int x = minX;
                        int y = lead;

                        if (i > 0)
                        {
                            x = lead;
                            y = maxY;
                        }



                        while (true)
                        {
                            if (x < minX || x > maxX || y < minY || y > maxY)
                                break;




                            Game.Map.Tile tile = Game.World.Map.GetTile((short)x, (short)y);
                            if (tile != null)
                            {

                                Vector3 position = new Vector3(
                               ((x - y) * 22f) - offset.X ,
                               ((x + y) * 22f /*- (Game.World.Player.Position.Z * 4)*/) - offset.Y, 0);


                                for (int k = 0; k < tile.ObjectsOnTiles.Count; k++)
                                {
                                    tile.ObjectsOnTiles[k].ViewObject.Draw(_spriteBatch, position);
                                }

                            }

                            x++;
                            y--;
                        }
                    }
                }


                _spriteBatch.GraphicsDevice.SetRenderTarget(_targetRender);
                _spriteBatch.GraphicsDevice.Clear(Color.Black);
                _spriteBatch.EndDraw();
                _spriteBatch.GraphicsDevice.SetRenderTarget(null);

            }

            _spriteBatch.GraphicsDevice.Clear(Color.Transparent);
            _spriteBatch.BeginDraw();

            _spriteBatch.Draw2D(_targetRender, new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight), Vector3.Zero);

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
