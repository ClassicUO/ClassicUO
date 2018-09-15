#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Map;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.Interfaces;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO
{
    public class GameLoop : CoreGame
    {
        private Log _log;
        private UIManager _uiManager;
        private InputManager _inputManager;
        private SceneManager _sceneManager;
        private SpriteBatch3D _sb3D;
        private SpriteBatchUI _sbUI;


        public GameLoop() : base()
        {
            
        }

        protected override void Initialize()
        {
            _log = Service.Get<Log>();
            SettingsModel currentProfile = SettingsController.Load("Default");
            
            _log.Message(LogTypes.Trace, "Checking for Ultima Online installation...", false);
            try
            {
                FileManager.UoFolderPath = currentProfile.UltimaOnlineDirectory;
            }
            catch (FileNotFoundException)
            {
                _log.Message(LogTypes.None, string.Empty);
                _log.Message(LogTypes.Error, "Wrong Ultima Online installation folder.");
                return;
            }
            _log.Message(LogTypes.None, "      Done!");
            _log.Message(LogTypes.Trace, $"Ultima Online installation folder: {FileManager.UoFolderPath}");


            _log.Message(LogTypes.Trace, "Loading files...", false);
            Stopwatch stopwatch = Stopwatch.StartNew();
            FileManager.LoadFiles();

            Texture2D textureHue0 = new Texture2D(GraphicsDevice, 32, 3000);
            textureHue0.SetData(Hues.CreateShaderColors());
            GraphicsDevice.Textures[1] = textureHue0;

            _log.Message(LogTypes.None, $"     Done in: {stopwatch.ElapsedMilliseconds} ms!");
            stopwatch.Stop();

            Service.Register(_uiManager = new UIManager());
            Service.Register(_sceneManager = new SceneManager());

            _inputManager = Service.Get<InputManager>();
            _sb3D = Service.Get<SpriteBatch3D>();
            _sbUI = Service.Get<SpriteBatchUI>();

            _log.Message(LogTypes.Trace, "Network calibration...", false);
            PacketHandlers.Load();
            PacketsTable.AdjustPacketSizeByVersion(FileManager.ClientVersion);
            _log.Message(LogTypes.None, "      Done!");


            _sceneManager.ChangeScene(ScenesType.Loading);



            // ##### START TEST #####
            TEST(currentProfile);
            // #####  END TEST  #####

           

            base.Initialize();
        }


        private void TEST(SettingsModel settings)
        {
            _sceneManager.ChangeScene(ScenesType.Game);


            string[] parts = settings.ClientVersion.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            byte[] clientVersionBuffer = { byte.Parse(parts[0]), byte.Parse(parts[1]), byte.Parse(parts[2]), byte.Parse(parts[3]) };

            NetClient.Connected += (sender, e) =>
            {
                _log.Message(LogTypes.Info, "Connected!");
                NetClient.Socket.Send(new PSeed(clientVersionBuffer));
                NetClient.Socket.Send(new PFirstLogin(settings.Username, settings.Password));
            };

            NetClient.Disconnected += (sender, e) => _log.Message(LogTypes.Warning, "Disconnected!");

            NetClient.PacketReceived += (sender, e) =>
            {
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
                        NetClient.Socket.Send(new PSecondLogin(settings.Username, settings.Password, e.ReadUInt()));
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




            NetClient.Socket.Connect(settings.IP, settings.Port);
        }



        protected override void OnInputUpdate(double totalMS, double frameMS)
        {
            _inputManager.Update(totalMS, frameMS);
        }

        protected override void OnNetworkUpdate(double totalMS, double frameMS)
        {
            NetClient.Socket.Slice();
        }

        protected override void OnUIUpdate(double totalMS, double frameMS)
        {
            _uiManager.Update(totalMS, frameMS);
        }

        protected override void OnUpdate(double totalMS, double frameMS)
        {
            if (World.InGame)
                _sceneManager.CurrentScene.Update(totalMS, frameMS);
        }

        protected override void OnFixedUpdate(double totalMS, double frameMS)
        {
            if (World.InGame)
                _sceneManager.CurrentScene.FixedUpdate(totalMS, frameMS);
        }

        protected override void OnDraw(double frameMS)
        {
            if (World.InGame)
                _sceneManager.CurrentScene.Draw(_sb3D, _sbUI);
        }
    }



    //public class GameLoop : Microsoft.Xna.Framework.Game
    //{
    //    private readonly GraphicsDeviceManager _graphics;
    //    private FpsCounter _fpsCounter;
    //    private CursorRenderer _gameCursor;
    //    private Stopwatch _stopwatch;
    //    private RenderTarget2D _targetRender;
    //    private DateTime _timePing;

    //    private RenderedText _gameTextTRY;

    //    private bool _rightMousePressed;
    //    private const float _interval = 1000.0f / 144.0f;
    //    private float _time;

    //    private readonly Texture2D _texture;

    //    public GameLoop()
    //    {
    //        TargetElapsedTime = TimeSpan.FromSeconds(1.0f / 300.0f);
    //        _graphics = new GraphicsDeviceManager(this);

    //        //IsFixedTimeStep = false;

    //        _graphics.PreparingDeviceSettings += (sender, e) => { e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents; };


    //        if (_graphics.GraphicsDevice.Adapter.IsProfileSupported(GraphicsProfile.HiDef))
    //            _graphics.GraphicsProfile = GraphicsProfile.HiDef;

    //        _graphics.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
    //        _graphics.SynchronizeWithVerticalRetrace = false;
    //        _graphics.PreferredBackBufferWidth = 800;
    //        _graphics.PreferredBackBufferHeight = 600;
    //        _graphics.ApplyChanges();


    //        Window.ClientSizeChanged += (sender, e) =>
    //        {
    //            _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
    //            _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;

    //            _graphics.ApplyChanges();
    //        };

    //    }

    //    protected override void Initialize()
    //    {
    //        Window.AllowUserResizing = true;

    //        _graphics.ApplyChanges();

    //        base.Initialize();
    //    }

    //    protected override void LoadContent()
    //    {
    //        // TEST


    //        //uncomment it and fill it to save your first settings
    //        /*Settings settings1 = new Settings()
    //        {
    //            Username = "",
    //            Password = "",
    //            LastCharacterName = "",
    //            IP = "",
    //            Port = 2599,
    //            UltimaOnlineDirectory = "",
    //            ClientVersion = "7.0.59.8"
    //        };

    //        ConfigurationResolver.Save(settings1, "settings.json");*/

    //        Settings settings = ConfigurationResolver.Load<Settings>(Path.Combine(Environment.CurrentDirectory, "settings.json"));
    //        string[] parts = settings.ClientVersion.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
    //        byte[] clientVersionBuffer = { byte.Parse(parts[0]), byte.Parse(parts[1]), byte.Parse(parts[2]), byte.Parse(parts[3]) };
    //        FileManager.UoFolderPath = settings.UltimaOnlineDirectory;


    //        _stopwatch = Stopwatch.StartNew();
    //        Service.Get<Log>().Message(LogTypes.Trace, "Loading UO files...");

    //        FileManager.LoadFiles();

    //        Service.Get<Log>().Message(LogTypes.Trace, "UO files loaded in " + _stopwatch.ElapsedMilliseconds + " ms");


    //        PacketHandlers.Load();
    //        PacketsTable.AdjustPacketSizeByVersion(FileManager.ClientVersion);


    //        Texture2D textureHue0 = new Texture2D(GraphicsDevice, 32, 3000);
    //        textureHue0.SetData(Hues.CreateShaderColors());
    //        GraphicsDevice.Textures[1] = textureHue0;

    //        _fpsCounter = new FpsCounter();

    //        string username = settings.Username;
    //        string password = settings.Password;

    //        var console = new DevConsole();
    //        //UIManager.Add(console);

    //        NetClient.PacketReceived += (sender, e) =>
    //        {
    //            switch (e.ID)
    //            {
    //                case 0xA8:
    //                    NetClient.Socket.Send(new PSelectServer(0));
    //                    break;
    //                case 0x8C:
    //                    NetClient.Socket.EnableCompression();
    //                    e.Seek(0);
    //                    e.MoveToData();
    //                    e.Skip(6);
    //                    NetClient.Socket.Send(new PSecondLogin(username, password, e.ReadUInt()));
    //                    break;
    //                case 0xA9:
    //                    NetClient.Socket.Send(new PSelectCharacter(0, settings.LastCharacterName, NetClient.Socket.ClientAddress));
    //                    break;
    //                case 0xBD:
    //                    NetClient.Socket.Send(new PClientVersion(clientVersionBuffer));
    //                    break;
    //                case 0xBE:
    //                    NetClient.Socket.Send(new PAssistVersion(clientVersionBuffer, e.ReadUInt()));
    //                    break;
    //                case 0x55:
    //                    NetClient.Socket.Send(new PClientViewRange(24));
    //                    break;
    //            }
    //        };

    //        //NetClient.PacketSended += (sender, e) =>
    //        //{
    //        //    //Service.Get<Log>().Message(LogTypes.Trace, string.Format("<< Sended\t\tID:   0x{0:X2}\t\t Length:   {1}", e.ID, e.Length));
    //        //};

    //        NetClient.Connected += (sender, e) =>
    //        {
    //            Service.Get<Log>().Message(LogTypes.Info, "Connected!");
    //            NetClient.Socket.Send(new PSeed(clientVersionBuffer));
    //            NetClient.Socket.Send(new PFirstLogin(username, password));
    //        };

    //        NetClient.Disconnected += (sender, e) => { Service.Get<Log>().Message(LogTypes.Warning, "Disconnected!"); };


    //        NetClient.Socket.Connect(settings.IP, settings.Port);


    //        _gameCursor = new CursorRenderer();

    //        _gameTextTRY = new RenderedText()
    //        {
    //            IsUnicode = true,
    //            Align = TEXT_ALIGN_TYPE.TS_LEFT,
    //            Text = "FPS: 0",
    //            FontStyle = FontStyle.BlackBorder,
    //            Font = 0,
    //            IsHTML = false,
    //        };


    //        //_texture = new Texture2D(TextureManager.Device, 1, 1);
    //        //_texture.SetData(new Color[1] { Color.White });

    //        // END TEST


    //        base.LoadContent();
    //    }

    //    protected override void UnloadContent()
    //    {
    //        base.UnloadContent();
    //    }

    //    private Point _tileSelected;

    //    public static Point GetTileCoordinates(Vector2 point, Vector2 relativeTo = new Vector2())
    //    {
    //        point -= relativeTo;
    //        point.X += 44 / 2;
    //        point.Y += 44 / 2;
    //        return new Point((int)Math.Floor(point.X / 44), (int)Math.Floor(point.Y / 44));
    //    }
    //    public static Vector2 CartesianToIsometric(Vector2 Cart)
    //    {
    //        return new Vector2(
    //                    Cart.X - Cart.Y,
    //                    ( Cart.X + Cart.Y ) / 2);
    //    }

    //    protected override void Update(GameTime gameTime)
    //    {
    //        World.Ticks = (long)gameTime.TotalGameTime.TotalMilliseconds;
    //        if (IsActive)
    //        {
    //            var inputManager = Service.Get<InputManager>();

    //            //inputManager.Update(World.Ticks);

    //            foreach (var e in inputManager.GetMouseEvents())
    //            {
    //                if (e.Button == MouseButton.Right)
    //                    _rightMousePressed = e.EventType == MouseEvent.Down;

    //            }

    //            if (World.InGame && _rightMousePressed)
    //            {
    //                Point center = new Point(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 2);

    //                Direction direction = DirectionHelper.DirectionFromPoints(center, inputManager.MousePosition);

    //                World.Player.Walk(direction, true);
    //            }
    //        }


    //        _time += (float)gameTime.ElapsedGameTime.TotalMilliseconds;

    //        bool allowBigUpdate = false;

    //        if (_time > _interval)
    //        {
    //            _time = _time % _interval; // or while (time > interval) time -= interval;
    //            allowBigUpdate = true;
    //        }
    //        else
    //            SuppressDraw();


    //        _fpsCounter.Update(gameTime);

    //        NetClient.Socket.Slice();

    //        _gameCursor.Update(gameTime.TotalGameTime.TotalMilliseconds, gameTime.ElapsedGameTime.Milliseconds);

    //        //UIManager.Update(gameTime.TotalGameTime.TotalMilliseconds, gameTime.ElapsedGameTime.Milliseconds);


    //        _picker.Position = _gameCursor.ScreenPosition;
    //        _picker.PickOnly = PickerType.PickEverything;

    //        if (World.InGame)
    //        {
    //            int scale = 1;

    //            if (_targetRender == null || _targetRender.Width != _graphics.PreferredBackBufferWidth / scale || _targetRender.Height != _graphics.PreferredBackBufferHeight / scale)
    //            {
    //                _targetRender?.Dispose();

    //                _targetRender = new RenderTarget2D(GraphicsDevice, _graphics.PreferredBackBufferWidth / scale, _graphics.PreferredBackBufferHeight / scale, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents);
    //            }

    //            World.Update(gameTime.TotalGameTime.TotalMilliseconds, gameTime.ElapsedGameTime.Milliseconds);

    //            if (DateTime.Now > _timePing)
    //            {
    //                NetClient.Socket.Send(new PPing());
    //                _timePing = DateTime.Now.AddSeconds(10);
    //            }


    //            if (!allowBigUpdate)
    //                return;

    //            IO.Resources.Art.ClearUnusedTextures();
    //            IO.Resources.Gumps.ClearUnusedTextures();
    //            IO.Resources.TextmapTextures.ClearUnusedTextures();
    //            IO.Resources.Animations.ClearUnusedTextures();
    //            World.Map.ClearUnusedBlocks();

    //            //var ss = Service.Get<InputManager>();
    //            //_tileSelected = GetTileCoordinates(CartesianToIsometric( new Vector2(ss.MousePosition.X, ss.MousePosition.Y)));
    //            //Console.WriteLine(_tileSelected);

    //            //(Point minTile, Point maxTile, Vector2 minPixel, Vector2 maxPixel, Point offset, Point center, Point firstTile, int renderDimensions) = GetViewPort();

    //            //CheckIfUnderEntity(out int maxItemZ, out bool drawTerrain, out bool underSurface);

    //            //_renderListCount = 0;

    //            //if (_renderList.Count > 0)
    //            //    _renderList.Clear();

    //            //int minX = minTile.X;
    //            //int minY = minTile.Y;
    //            //int maxX = maxTile.X;
    //            //int maxY = maxTile.Y;
    //            //_offset = offset;
    //            //_minPixel = minPixel;
    //            //_maxPixel = maxPixel;

    //            //_minTile = minTile;
    //            //_maxTile = maxTile;

    //            //int n = maxX - minX;

    //            //for (int i = 0; i < n; i++)
    //            //{
    //            //    for(int j = 0; j < i + 1; j++)
    //            //    {
    //            //        int indexX = j;
    //            //        int indexY = i - j;

    //            //        int x = minX + indexX;
    //            //        int y = minY + indexY;

    //            //        Tile tile = World.Map.GetTile(x, y);
    //            //        if (tile != null)
    //            //        {
    //            //            var objects = (List<GameObject>)tile.ObjectsOnTiles;
    //            //            AddTileToRenderList(tile, objects, x, y, false, 150);
    //            //        }

    //            //    }
    //            //}

    //            //for (int i = 1; i < n + 1; i++)
    //            //{
    //            //    for (int j = 0; j < n - i; j++)
    //            //    {
    //            //        int indexX = j + i;
    //            //        int indexY = n - j - 1;

    //            //        int x = minX + indexX;
    //            //        int y = minY + indexY;

    //            //        Tile tile = World.Map.GetTile(x, y);
    //            //        if (tile != null)
    //            //        {
    //            //            var objects = (List<GameObject>)tile.ObjectsOnTiles;
    //            //            AddTileToRenderList(tile, objects, x, y, false, 150);
    //            //        }
    //            //    }
    //            //}


    //            //for (int i = 0; i < 2; i++)
    //            //{
    //            //    int minValue = minY;
    //            //    int maxValue = maxY;

    //            //    if (i > 0)
    //            //    {
    //            //        minValue = minX;
    //            //        maxValue = maxX;
    //            //    }

    //            //    for (int lead = minValue; lead < maxValue; lead++)
    //            //    {
    //            //        int x = minX;
    //            //        int y = lead;

    //            //        if (i > 0)
    //            //        {
    //            //            x = lead;
    //            //            y = maxY;
    //            //        }

    //            //        while (true)
    //            //        {
    //            //            if (x < minX || x > maxX || y < minY || y > maxY)
    //            //                break;
    //            //            Tile tile = World.Map.GetTile(x, y);
    //            //            if (tile != null)
    //            //            {
    //            //                var objects = (List<GameObject>)tile.ObjectsOnTiles;
    //            //                AddTileToRenderList(tile, objects, x, y, false, 150);
    //            //            }

    //            //            x++;
    //            //            y--;

    //            //        }
    //            //    }
    //            //}

    //            //_renderIndex++;

    //            //if (_renderIndex >= 100)
    //            //    _renderIndex = 1;

    //        }
    //    }

    //    private static void CheckIfUnderEntity(out int maxItemZ, out bool drawTerrain, out bool underSurface)
    //    {
    //        maxItemZ = 255;
    //        drawTerrain = true;
    //        underSurface = false;

    //        Tile tile = World.Map.GetTile(World.Map.Center.X, World.Map.Center.Y);
    //        if (tile != null && tile.IsZUnderObjectOrGround(World.Player.Position.Z, out GameObject underObject, out GameObject underGround))
    //        {
    //            drawTerrain = underGround == null;
    //            if (underObject != null)
    //            {
    //                if (underObject is Item item)
    //                {
    //                    if (TileData.IsRoof((long)item.ItemData.Flags))
    //                        maxItemZ = World.Player.Position.Z - World.Player.Position.Z % 20 + 20;
    //                    else if (TileData.IsSurface((long)item.ItemData.Flags) || TileData.IsWall((long)item.ItemData.Flags) && TileData.IsDoor((long)item.ItemData.Flags))
    //                        maxItemZ = item.Position.Z;
    //                    else
    //                    {
    //                        int z = World.Player.Position.Z + ( item.ItemData.Height > 20 ? item.ItemData.Height : 20 );
    //                        maxItemZ = z;
    //                    }
    //                }
    //                else if (underObject is Static sta)
    //                {
    //                    if (TileData.IsRoof((long)sta.ItemData.Flags))
    //                        maxItemZ = World.Player.Position.Z - World.Player.Position.Z % 20 + 20;
    //                    else if (TileData.IsSurface((long)sta.ItemData.Flags) || TileData.IsWall((long)sta.ItemData.Flags) && TileData.IsDoor((long)sta.ItemData.Flags))
    //                        maxItemZ = sta.Position.Z;
    //                    else
    //                    {
    //                        int z = World.Player.Position.Z + ( sta.ItemData.Height > 20 ? sta.ItemData.Height : 20 );
    //                        maxItemZ = z;
    //                    }
    //                }

    //                if (underObject is Item i && TileData.IsRoof((long)i.ItemData.Flags) || underObject is Static s && TileData.IsRoof((long)s.ItemData.Flags))
    //                {
    //                    bool isSE = true;
    //                    if (( tile = World.Map.GetTile(World.Map.Center.X + 1, World.Map.Center.Y) ) != null)
    //                    {
    //                        tile.IsZUnderObjectOrGround(World.Player.Position.Z, out underObject, out underGround);
    //                        isSE = underObject != null;
    //                    }

    //                    if (!isSE)
    //                        maxItemZ = 255;
    //                }

    //                underSurface = maxItemZ != 255;
    //            }
    //        }
    //    }

    //    private (Point, Point, Vector2, Vector2, Point, Point, Point, int) GetViewPort()
    //    {
    //        float scale = 1;

    //        int winGamePosX = 0;
    //        int winGamePosY = 0;

    //        int winGameWidth = _graphics.PreferredBackBufferWidth;
    //        int winGameHeight = _graphics.PreferredBackBufferHeight;

    //        int winGameCenterX = winGamePosX + ( winGameWidth / 2 );
    //        int winGameCenterY = winGamePosY + winGameHeight / 2 + World.Player.Position.Z * 4;

    //        winGameCenterX -= (int)World.Player.Offset.X;
    //        winGameCenterY -= (int)( World.Player.Offset.Y - World.Player.Offset.Z );

    //        int winDrawOffsetX = ( World.Player.Position.X - World.Player.Position.Y ) * 22 - winGameCenterX;
    //        int winDrawOffsetY = ( World.Player.Position.X + World.Player.Position.Y ) * 22 - winGameCenterY;

    //        float left = winGamePosX;
    //        float right = winGameWidth + left;
    //        float top = winGamePosY;
    //        float bottom = winGameHeight + top;

    //        float newRight = right * scale;
    //        float newBottom = bottom * scale;

    //        int winGameScaledOffsetX = (int)( left * scale - ( newRight - right ) );
    //        int winGameScaledOffsetY = (int)( top * scale - ( newBottom - bottom ) );

    //        int winGameScaledWidth = (int)( newRight - winGameScaledOffsetX );
    //        int winGameScaledHeight = (int)( newBottom - winGameScaledOffsetY );


    //        int width = (int)( ( winGameWidth / 44 + 1 ) * scale );
    //        int height = (int)( ( winGameHeight / 44 + 1 ) * scale );

    //        if (width < height)
    //            width = height;
    //        else
    //            height = width;

    //        int realMinRangeX = World.Player.Position.X - width;
    //        if (realMinRangeX < 0)
    //            realMinRangeX = 0;
    //        int realMaxRangeX = World.Player.Position.X + width;
    //        if (realMaxRangeX >= Map.MapsDefaultSize[World.Map.Index][0])
    //            realMaxRangeX = Map.MapsDefaultSize[World.Map.Index][0];

    //        int realMinRangeY = World.Player.Position.Y - height;
    //        if (realMinRangeY < 0)
    //            realMinRangeY = 0;
    //        int realMaxRangeY = World.Player.Position.Y + height;
    //        if (realMaxRangeY >= Map.MapsDefaultSize[World.Map.Index][1])
    //            realMaxRangeY = Map.MapsDefaultSize[World.Map.Index][1];

    //        int minBlockX = realMinRangeX / 8 - 1;
    //        int minBlockY = realMinRangeY / 8 - 1;
    //        int maxBlockX = realMaxRangeX / 8 + 1;
    //        int maxBlockY = realMaxRangeY / 8 + 1;

    //        if (minBlockX < 0)
    //            minBlockX = 0;
    //        if (minBlockY < 0)
    //            minBlockY = 0;
    //        if (maxBlockX >= Map.MapsDefaultSize[World.Map.Index][0])
    //            maxBlockX = Map.MapsDefaultSize[World.Map.Index][0] - 1;
    //        if (maxBlockY >= Map.MapsDefaultSize[World.Map.Index][1])
    //            maxBlockY = Map.MapsDefaultSize[World.Map.Index][1] - 1;

    //        int drawOffset = (int)( scale * 40.0f );

    //        float maxX = winGamePosX + winGameWidth + drawOffset;
    //        float maxY = winGamePosY + winGameHeight + drawOffset;
    //        float newMaxX = maxX * scale;
    //        float newMaxY = maxY * scale;

    //        int minPixelsX = (int)( ( winGamePosX - drawOffset ) * scale - ( newMaxX - maxX ) );
    //        int maxPixelsX = (int)newMaxX;
    //        int minPixelsY = (int)( ( winGamePosY - drawOffset ) * scale - ( newMaxY - maxY ) );
    //        int maxPixlesY = (int)newMaxY;

    //        return (new Point(realMinRangeX, realMinRangeY), new Point(realMaxRangeX, realMaxRangeY), new Vector2(minPixelsX, minPixelsY), new Vector2(maxPixelsX, maxPixlesY), new Point(winDrawOffsetX, winDrawOffsetY), new Point(winGameCenterX, winGameCenterY), new Point(realMinRangeX + width - 1, realMinRangeY - 1), Math.Max(width, height));
    //    }

    //    private (Point firstTile, Vector2 renderOffset, Point renderDimensions) GetViewPort2()
    //    {
    //        int scale = 1;

    //        Point renderDimensions = new Point
    //        {
    //            X = _graphics.PreferredBackBufferWidth / scale / 44 + 3,
    //            Y = _graphics.PreferredBackBufferHeight / scale / 44 + 6
    //        };

    //        int renderDimensionDiff = Math.Abs(renderDimensions.X - renderDimensions.Y);
    //        renderDimensionDiff -= renderDimensionDiff % 2;

    //        int firstZOffset = World.Player.Position.Z > 0 ? (int)Math.Abs(( World.Player.Position.Z + World.Player.Offset.Z / 4 ) / 11) : 0;

    //        Point firstTile = new Point
    //        {
    //            X = World.Player.Position.X - firstZOffset,
    //            Y = World.Player.Position.Y - renderDimensions.Y - firstZOffset
    //        };

    //        if (renderDimensions.Y > renderDimensions.X)
    //        {
    //            firstTile.X -= renderDimensionDiff / 2;
    //            firstTile.Y -= renderDimensionDiff / 2;
    //        }
    //        else
    //        {
    //            firstTile.X += renderDimensionDiff / 2;
    //            firstTile.Y -= renderDimensionDiff / 2;
    //        }

    //        //Vector2 renderOffset = new Vector2
    //        //{
    //        //    X = (_graphics.PreferredBackBufferWidth / scale + renderDimensions.Y * 44) / 2 - 22f - (int)World.Player.Offset.X - (firstTile.X - firstTile.Y) * 22f + renderDimensionDiff * 22f,
    //        //    Y = _graphics.PreferredBackBufferHeight / scale / 2 - renderDimensions.Y * 44 / 2 + (World.Player.Position.Z + World.Player.Offset.Z / 4) * 4 - (int)World.Player.Offset.Y - (firstTile.X + firstTile.Y) * 22f - 22f - firstZOffset * 44f };

    //        Vector2 renderOffset = new Vector2();

    //        renderOffset.X = ( ( ( _graphics.PreferredBackBufferWidth / scale ) + ( renderDimensions.Y * 44 ) ) / 2 ) - 22f;
    //        renderOffset.X -= (int)World.Player.Offset.X;
    //        renderOffset.X -= ( firstTile.X - firstTile.Y ) * 22f;
    //        renderOffset.X += renderDimensionDiff * 22f;

    //        renderOffset.Y = ( _graphics.PreferredBackBufferHeight / scale ) / 2 - ( renderDimensions.Y * 44 / 2 );
    //        renderOffset.Y += ( World.Player.Position.Z + World.Player.Offset.Z / 4 ) * 4;
    //        renderOffset.Y -= (int)World.Player.Offset.Y;
    //        renderOffset.Y -= ( firstTile.X + firstTile.Y ) * 22f;
    //        renderOffset.Y -= 22f;
    //        renderOffset.Y -= firstZOffset * 44f;

    //        return (firstTile, renderOffset, renderDimensions);
    //    }

    //    private MousePicker<GameObject> _picker = new MousePicker<GameObject>();

    //    protected override void Draw(GameTime gameTime)
    //    {
    //        var sb3D = Service.Get<SpriteBatch3D>();
    //        MouseOverList<GameObject> overList = new MouseOverList<GameObject>(_picker);


    //        if (World.InGame)
    //        {
    //            _fpsCounter.IncreaseFrame();

    //            CheckIfUnderEntity(out int maxItemZ, out bool drawTerrain, out bool underSurface);
    //            (Point firstTile, Vector2 renderOffset, Point renderDimensions) = GetViewPort2();

    //            sb3D.Begin();
    //            sb3D.SetLightIntensity(World.Light.IsometricLevel);
    //            sb3D.SetLightDirection(World.Light.IsometricDirection);

    //            List<DeferredEntity> toremove = new List<DeferredEntity>();

    //            _renderListCount = 0;


    //            for (int y = 0; y < renderDimensions.Y * 2 + 11; y++)
    //            {

    //                Vector3 dp = new Vector3
    //                {
    //                    X = ( firstTile.X - firstTile.Y + ( y % 2 ) ) * 22f + renderOffset.X,
    //                    Y = ( firstTile.X + firstTile.Y + y ) * 22f + renderOffset.Y
    //                };


    //                Point firstTileInRow = new Point(firstTile.X + ( ( y + 1 ) / 2 ), firstTile.Y + ( y / 2 ));

    //                for (int x = 0; x < renderDimensions.X + 1; x++, dp.X -= 44f)
    //                {
    //                    int tileX = firstTileInRow.X - x;
    //                    int tileY = firstTileInRow.Y + x;

    //                    Tile tile = World.Map.GetTile(tileX, tileY);
    //                    if (tile != null)
    //                    {
    //                        var objects = tile.ObjectsOnTiles;
    //                        bool draw = true;
    //                        for (int k = 0; k < objects.Count; k++)
    //                        {
    //                            var obj = objects[k];

    //                            if (obj is DeferredEntity d)
    //                                toremove.Add(d);

    //                            if (!drawTerrain)
    //                            {
    //                                if (obj is Tile || obj.Position.Z > tile.Position.Z)
    //                                    draw = false;
    //                            }

    //                            if (( obj.Position.Z >= maxItemZ
    //                                || maxItemZ != 255 && obj is IDynamicItem dyn && TileData.IsRoof((long)dyn.ItemData.Flags) )
    //                                && !( obj is Tile ))
    //                                continue;

    //                            var view = obj.View;

    //                            //if (MouseOverList<SpriteBatch3D>.IsMouseInObjectIsometric(vertices, Service.Get<InputManager>().MousePosition))
    //                            //    vertices[0].Hue = vertices[1].Hue = vertices[2].Hue = vertices[3].Hue = RenderExtentions.GetHueVector(33);

    //                            if (draw && view.Draw(sb3D, dp, overList))
    //                                _renderListCount++;
    //                        }

    //                        foreach (var o in toremove)
    //                        {
    //                            o.Reset();
    //                            tile.RemoveGameObject(o);
    //                        }

    //                        toremove.Clear();
    //                    }
    //                }
    //            }




    //            //int i = 0;
    //            //while (i < _renderList.Count)
    //            //{
    //            //    var obj = _renderList[i++];

    //            //    int x = obj.Position.X;
    //            //    int y = obj.Position.Y;

    //            //    if (obj.Graphic == 0xcee)
    //            //    {
    //            //        if (((Item)obj).Serial == 0x40018e84 )
    //            //        {

    //            //        }
    //            //    }

    //            //    Vector3 isometricPosition = new Vector3((x - y) * 22 - _offset.X - 22, (x + y) * 22 - _offset.Y - 22, 0);

    //            //    obj.View.Draw(sb3D, isometricPosition);

    //            //}

    //            //_renderList.Clear();

    //            sb3D.GraphicsDevice.SetRenderTarget(_targetRender);
    //            sb3D.GraphicsDevice.Clear(Color.Black);
    //            sb3D.End(true);
    //            sb3D.GraphicsDevice.SetRenderTarget(null);
    //        }

    //        var sbUI = Service.Get<SpriteBatchUI>();

    //        sbUI.GraphicsDevice.Clear(Color.Transparent);
    //        sbUI.Begin();

    //        sbUI.Draw2D(_targetRender, new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight), Vector3.Zero);
    //        OverheadManager.Draw(sbUI, overList);
    //        //UIManager.Render(sbUI);

    //        _picker.UpdateOverObjects(overList, overList.MousePosition);

    //        //_spriteBatch.Draw2D(_crossTexture, new Bounds(_graphics.PreferredBackBufferWidth / 2  - 5, _graphics.PreferredBackBufferHeight / 2 - 5, 10, 10), Vector3.Zero);

    //        //_spriteBatch.Draw2D(_textentry, new Vector3(0, 0, 0), Vector3.Zero);

    //        //_textRenderer.Draw(_spriteBatch, new Point(100, 150));

    //        //_Service.Get<MouseManager>().Draw(_spriteBatch);
    //        //GarbageCollectionWatcher.Stop();

    //        StringBuilder sb = new StringBuilder();
    //        sb.Append("FPS: ");
    //        sb.AppendLine(_fpsCounter.FPS.ToString());
    //        sb.Append("Objects: ");
    //        sb.AppendLine(_renderListCount.ToString());
    //        sb.Append("Calls: ");
    //        sb.AppendLine(sb3D.Calls.ToString());
    //        sb.Append("Merged: ");
    //        sb.AppendLine(sb3D.Merged.ToString());
    //        sb.Append("Totals: ");
    //        sb.AppendLine(sb3D.TotalCalls.ToString());
    //        sb.Append("Pos: ");
    //        sb.AppendLine(World.Player == null ? "" : World.Player.Position.ToString());
    //        sb.Append("Selected: ");
    //        sb.AppendLine(SelectedObject == null ? "" : SelectedObject.ToString());

    //        _gameTextTRY.Text = sb.ToString();
    //        _gameTextTRY.Draw(sbUI, new Vector3(/*Window.ClientBounds.Width - 150*/ 20, 20, 0));

    //        //_spriteBatch.Draw2D(_gump, new Rectangle(100, 100, _gump.Width, _gump.Height), Vector3.Zero);

    //        //sbUI.DrawLine(_texture, new Vector2(10, 120), new Vector2(Window.ClientBounds.Width - 10, 120), Vector3.Zero);
    //        //sbUI.DrawRectangle(_texture, new Rectangle(2, 120, 100, 100), Vector3.Zero);


    //        _gameCursor.Draw(sbUI);
    //        sbUI.End();


    //        SelectedObject = _picker.MouseOverObject;
    //    }

    //    private Hue _savedHue;
    //    private GameObject _selectedObject;

    //    private GameObject SelectedObject
    //    {
    //        get => _selectedObject;
    //        set
    //        {
    //            if (_selectedObject == value)
    //                return;

    //            if (_selectedObject != null)
    //            {
    //                _selectedObject.Hue = _savedHue;
    //            }

    //            if (value == null)
    //            {
    //                _selectedObject = null;
    //                _savedHue = 0;
    //            }
    //            else
    //            {
    //                _selectedObject = value;
    //                _savedHue = _selectedObject.Hue;
    //                _selectedObject.Hue = 33;
    //            }
    //        }
    //    }

    //    private readonly int _renderIndex = 1;
    //    private int _renderListCount = 0;
    //    private List<GameObject> _renderList = new List<GameObject>();
    //    private Point _offset, _maxTile, _minTile;
    //    private Vector2 _minPixel, _maxPixel;

    //    private void AddTileToRenderList(Tile tile, List<GameObject> objList, int worldX, int worldY, bool useObjectHandles, int maxZ)
    //    {
    //        for (int i = 0; i < objList.Count; i++)
    //        {
    //            var obj = objList[i];

    //            if (obj.CurrentRenderIndex == _renderIndex || obj.IsDisposed)
    //                continue;

    //            obj.UseInRender = 0xFF;
    //            int drawX = ( obj.Position.X - obj.Position.Y ) * 22 - _offset.X;
    //            int drawY = ( ( obj.Position.X + obj.Position.Y ) * 22 - ( obj.Position.Z * 4 ) ) - _offset.Y;

    //            if (drawX < _minPixel.X || drawX > _maxPixel.X)
    //                break;

    //            int z = obj.Position.Z;
    //            int maxObjectZ = obj.PriorityZ;

    //            if (obj is Mobile)
    //                maxObjectZ += 16;
    //            else if (obj is IDynamicItem dyn)
    //                maxObjectZ += dyn.ItemData.Height;


    //            if (maxObjectZ > maxZ)
    //                break;

    //            obj.CurrentRenderIndex = _renderIndex;

    //            if (obj is IDynamicItem dyn1 && TileData.IsInternal((long)dyn1.ItemData.Flags))
    //                continue;
    //            //else if (!(obj is Tile) && z >= )

    //            int testMinZ = drawY + ( z * 4 );
    //            int testMaxZ = drawY;


    //            if (obj is Tile t && t.IsStretched)
    //                testMinZ -= ( tile.MinZ * 4 );
    //            else
    //                testMinZ = testMaxZ;

    //            if (testMinZ < _minPixel.Y || testMaxZ > _maxPixel.Y)
    //                continue;

    //            if (obj is Mobile mob)
    //                AddOffsetCharacterTileToRenderList(mob, useObjectHandles);
    //            else if (obj is Item item && item.IsCorpse)
    //                AddOffsetCharacterTileToRenderList(item, useObjectHandles);

    //            _renderList.Add(obj);
    //            obj.UseInRender = (byte)_renderIndex;
    //            _renderListCount++;
    //        }
    //    }


    //    private readonly int[,] _coordinates = new int[8, 2];

    //    private void AddOffsetCharacterTileToRenderList(Entity entity, bool useObjectHandles)
    //    {
    //        int charX = entity.Position.X;
    //        int charY = entity.Position.Y;

    //        Mobile mob = entity.Serial.IsMobile ? World.Mobiles.Get(entity) : null;
    //        int dropMaxZIndex = -1;
    //        if (mob != null)
    //        {
    //            if (mob.Steps.Count > 0 && ( mob.Steps.Back().Direction & 7 ) == 2)
    //                dropMaxZIndex = 0;
    //        }

    //        _coordinates[0, 0] = charX + 1;
    //        _coordinates[0, 1] = charY - 1;
    //        _coordinates[1, 0] = charX + 1;
    //        _coordinates[1, 1] = charY - 2;
    //        _coordinates[2, 0] = charX + 2;
    //        _coordinates[2, 1] = charY - 2;
    //        _coordinates[3, 0] = charX - 1;
    //        _coordinates[3, 1] = charY + 2;
    //        _coordinates[4, 0] = charX;
    //        _coordinates[4, 1] = charY + 1;
    //        _coordinates[5, 0] = charX + 1;
    //        _coordinates[5, 1] = charY;
    //        _coordinates[6, 0] = charX + 2;
    //        _coordinates[6, 1] = charY - 1;
    //        _coordinates[7, 0] = charX + 1;
    //        _coordinates[7, 1] = charY + 1;


    //        int maxZ = entity.PriorityZ;

    //        for (int i = 0; i < _coordinates.Length / _coordinates.Rank; i++)
    //        {
    //            int x = _coordinates[i, 0];
    //            int y = _coordinates[i, 1];

    //            if (x < _minTile.X || x > _maxTile.X || y < _minTile.Y || y > _maxTile.Y)
    //                continue;

    //            Tile tile = World.Map.GetTile(x, y);

    //            int currentMaxZ = maxZ;

    //            if (i == dropMaxZIndex)
    //                currentMaxZ += 20;

    //            AddTileToRenderList(tile, (List<GameObject>)tile.ObjectsOnTiles, x, y, useObjectHandles, currentMaxZ);
    //        }
    //    }

    //}
}