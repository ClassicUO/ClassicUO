#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
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
using System.Linq;
using System.Net.Sockets;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Map;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Scenes
{
    internal partial class GameScene : Scene
    {
        private readonly LightData[] _lights = new LightData[Constants.MAX_LIGHTS_DATA_INDEX_COUNT];
        private readonly float[] _scaleArray = Enumerable.Range(5, 21).Select(i => i / 10.0f).ToArray(); // 0.5 => 2.5
        private bool _alphaChanged;
        private long _alphaTimer;
        private bool _deathScreenActive;
        private Label _deathScreenLabel;
        private bool _forceStopScene;
        private HealthLinesManager _healthLinesManager;
        private int _lightCount;
        private RenderTarget2D _renderTarget, _darkness;
        private int _scale = 5; // 1.0
        private Rectangle _rectangleObj = Rectangle.Empty, _rectanglePlayer;


        private IGameEntity _selectedObject;
        private long _timePing;
        private UseItemQueue _useItemQueue = new UseItemQueue();
        private Vector4 _vectorClear = new Vector4(Vector3.Zero, 1);
        private WorldViewport _viewPortGump;

        private int ScalePos
        {
            get => _scale;
            set
            {
                if (value < 0)
                    value = 0;
                else if (value >= _scaleArray.Length - 1)
                    value = _scaleArray.Length - 1;

                _scale = value;
            }
        }

        public float Scale
        {
            get => _scaleArray[_scale];
            set => ScalePos = (int) (value * 10) - 5;
        }

        public HotkeysManager Hotkeys { get; private set; }

        public MacroManager Macros { get; private set; }

        public Texture2D ViewportTexture => _renderTarget;

        public Texture2D Darkness => _darkness;

        //public IGameEntity SelectedObject
        //{
        //    get => _selectedObject;
        //    set
        //    {
        //        _selectedObject = Game.SelectedObject.Object = value;
        //        //if (_selectedObject == value)
        //        //    return;

        //        //if (_selectedObject != null && _selectedObject.IsSelected)
        //        //    _selectedObject.IsSelected = false;

        //        //_selectedObject = value;

        //        //if (_selectedObject != null)
        //        //    _selectedObject.IsSelected = true;
        //    }
        //}

        public JournalManager Journal { get; private set; }

        public OverheadManager Overheads { get; private set; }

        public bool UseLights => Engine.Profile.Current != null && Engine.Profile.Current.UseCustomLightLevel ? World.Light.Personal < World.Light.Overall : World.Light.RealPersonal < World.Light.RealOverall;

        public void DoubleClickDelayed(Serial serial)
        {
            _useItemQueue.Add(serial);
        }

        private void ClearDequeued()
        {
            if (_inqueue)
            {
                _inqueue = false;
                _queuedObject = null;
                _queuedAction = null;
                _dequeueAt = 0;
            }
        }

        public override void Load()
        {
            base.Load();

            if (!Engine.Profile.Current.DebugGumpIsDisabled)
            {
                Engine.UI.Add(new DebugGump
                {
                    X = Engine.Profile.Current.DebugGumpPosition.X,
                    Y = Engine.Profile.Current.DebugGumpPosition.Y
                });
                Engine.DropFpsMinMaxValues();
            }

            HeldItem = new ItemHold();
            Journal = new JournalManager();
            Overheads = new OverheadManager();
            Hotkeys = new HotkeysManager();
            Macros = new MacroManager(Engine.Profile.Current.Macros);
            _healthLinesManager = new HealthLinesManager();

            WorldViewportGump viewport = new WorldViewportGump(this);

            Engine.UI.Add(viewport);

            if (!Engine.Profile.Current.TopbarGumpIsDisabled)
                TopBarGump.Create();

            _viewPortGump = viewport.FindControls<WorldViewport>().SingleOrDefault();

            GameActions.Initialize(PickupItemBegin);


            // LEFT
            Engine.Input.LeftMouseButtonDown += OnLeftMouseDown;
            Engine.Input.LeftMouseButtonUp += OnLeftMouseUp;
            Engine.Input.LeftMouseDoubleClick += OnLeftMouseDoubleClick;

            // RIGHT
            Engine.Input.RightMouseButtonDown += OnRightMouseDown;
            Engine.Input.RightMouseButtonUp += OnRightMouseUp;
            Engine.Input.RightMouseDoubleClick += OnRightMouseDoubleClick;

            // MOUSE MOVING
            Engine.Input.MouseMoving += OnMouseMoving;

            // MOUSE WHEEL
            Engine.Input.MouseWheel += OnMouseWheel;

            // MOUSE DRAG
            Engine.Input.DragBegin += OnMouseDragBegin;

            // KEYBOARD
            Engine.Input.KeyDown += OnKeyDown;
            Engine.Input.KeyUp += OnKeyUp;


            CommandManager.Initialize();
            NetClient.Socket.Disconnected += SocketOnDisconnected;

            Chat.MessageReceived += ChatOnMessageReceived;

            if (!Engine.Profile.Current.EnableScaleZoom || !Engine.Profile.Current.SaveScaleAfterClose)
                Scale = 1f;
            else
                Scale = Engine.Profile.Current.ScaleZoom;

            Engine.Profile.Current.RestoreScaleValue = Engine.Profile.Current.ScaleZoom = Scale;

            Plugin.OnConnected();
        }

        private void ChatOnMessageReceived(object sender, UOMessageEventArgs e)
        {
            if (e.Type == MessageType.Command)
                return;

            string name;
            string text;

            Hue hue = e.Hue;

            switch (e.Type)
            {
                case MessageType.Regular:

                    if (e.Parent == null || e.Parent.Serial == Serial.INVALID)
                        name = "System";
                    else
                        name = e.Name;

                    text = e.Text;

                    break;

                case MessageType.System:
                    name = "System";
                    text = e.Text;

                    break;

                case MessageType.Emote:
                    name = e.Name;
                    text = $"*{e.Text}*";

                    if (e.Hue == 0)
                        hue = Engine.Profile.Current.EmoteHue;

                    break;
                case MessageType.Label:
                    name = "You see";
                    text = e.Text;

                    break;

                case MessageType.Spell:
                    name = e.Name;
                    text = e.Text;

                    break;
                case MessageType.Party:
                    text = e.Text;
                    name = $"[Party][{e.Name}]";
                    hue = Engine.Profile.Current.PartyMessageHue;

                    break;
                case MessageType.Alliance:
                    text = e.Text;
                    name = $"[Alliance][{e.Name}]";
                    hue = Engine.Profile.Current.AllyMessageHue;

                    break;
                case MessageType.Guild:
                    text = e.Text;
                    name = $"[Guild][{e.Name}]";
                    hue = Engine.Profile.Current.GuildMessageHue;

                    break;
                default:
                    text = e.Text;
                    name = e.Name;
                    hue = e.Hue;

                    Log.Message(LogTypes.Warning, $"Unhandled text type {e.Type}  -  text: '{e.Text}'");

                    break;
            }

            Journal.Add(text, hue, name, e.IsUnicode);
        }

        public override void Unload()
        {
            HeldItem?.Clear();

            try
            {
                Plugin.OnDisconnected();
            }
            catch
            {
            }

            _renderList = null;

            TargetManager.ClearTargetingWithoutTargetCancelPacket();

            Engine.Profile.Current?.Save(Engine.UI.Gumps.OfType<Gump>().Where(s => s.CanBeSaved).Reverse().ToList());
            Engine.Profile.UnLoadProfile();

            NetClient.Socket.Disconnected -= SocketOnDisconnected;
            NetClient.Socket.Disconnect();
            _renderTarget?.Dispose();
            _darkness?.Dispose();
            CommandManager.UnRegisterAll();

            Engine.UI?.Clear();
            World.Clear();

            // LEFT
            Engine.Input.LeftMouseButtonDown -= OnLeftMouseDown;
            Engine.Input.LeftMouseButtonUp -= OnLeftMouseUp;
            Engine.Input.LeftMouseDoubleClick -= OnLeftMouseDoubleClick;

            // RIGHT
            Engine.Input.RightMouseButtonDown -= OnRightMouseDown;
            Engine.Input.RightMouseButtonUp -= OnRightMouseUp;
            Engine.Input.RightMouseDoubleClick -= OnRightMouseDoubleClick;

            // MOUSE MOVING
            Engine.Input.MouseMoving -= OnMouseMoving;

            // MOUSE WHEEL
            Engine.Input.MouseWheel -= OnMouseWheel;

            // MOUSE DRAG
            Engine.Input.DragBegin -= OnMouseDragBegin;

            Engine.Input.KeyDown -= OnKeyDown;
            Engine.Input.KeyUp -= OnKeyUp;

            Overheads?.Clear();
            Overheads = null;
            Journal?.Clear();
            Journal = null;
            Overheads = null;
            _useItemQueue?.Clear();
            _useItemQueue = null;
            Hotkeys = null;
            Macros = null;
            Chat.MessageReceived -= ChatOnMessageReceived;

            base.Unload();
        }

        private void SocketOnDisconnected(object sender, SocketError e)
        {
            if (Engine.GlobalSettings.Reconnect)
                _forceStopScene = true;
            else
            {
                Engine.UI.Add(new MessageBoxGump(200, 200, $"Connection lost:\n{e}", s =>
                {
                    if (s)
                        Engine.SceneManager.ChangeScene(ScenesType.Login);
                }));
            }
        }

        public void RequestQuitGame()
        {
            Engine.UI.Add(new QuestionGump("Quit\nUltima Online?", s =>
            {
                if (s)
                    Engine.SceneManager.ChangeScene(ScenesType.Login);
            }));
        }

        public void AddLight(GameObject obj, GameObject lightObject, int x, int y)
        {
            if (_lightCount >= Constants.MAX_LIGHTS_DATA_INDEX_COUNT || !UseLights)
                return;

            bool canBeAdded = true;

            int testX = obj.X + 1;
            int testY = obj.Y + 1;

            Tile tile = World.Map.GetTile(testX, testY);

            if (tile != null)
            {
                sbyte z5 = (sbyte) (obj.Z + 5);

                for (GameObject o = tile.FirstNode; o != null; o = o.Right)
                {
                    if ((!(o is Static s) || s.ItemData.IsTransparent) &&
                        (!(o is Multi m) || m.ItemData.IsTransparent) || !o.AllowedToDraw)
                        continue;

                    if (o.Z < _maxZ && o.Z >= z5)
                    {
                        canBeAdded = false;

                        break;
                    }
                }
            }


            if (canBeAdded)
            {
                ref var light = ref _lights[_lightCount];

                ushort graphic = lightObject.Graphic;

                if (graphic >= 0x3E02 && graphic <= 0x3E0B ||
                    graphic >= 0x3914 && graphic <= 0x3929 || 
                    graphic == 0x0B1D)
                    light.ID = 2;
                else
                {
                    if (obj == lightObject && obj is Item item)
                        light.ID = item.LightID;
                    else if (lightObject is Item it)
                        light.ID = (byte) it.ItemData.LightIndex;
                    else if (GameObjectHelper.TryGetStaticData(lightObject, out StaticTiles data))
                        light.ID = data.Layer;
                    else if (obj is Mobile _)
                        light.ID = 1;
                    else
                        return;
                }


                if (light.ID >= Constants.MAX_LIGHTS_DATA_INDEX_COUNT)
                    return;

                light.Color = LightColors.GetHue(graphic);

                light.DrawX = x;
                light.DrawY = y;
                _lightCount++;
            }
        }

        public override void FixedUpdate(double totalMS, double frameMS)
        {
            base.FixedUpdate(totalMS, frameMS);

            if (!World.InGame)
                return;

            if (_forceStopScene)
            {
                Engine.SceneManager.ChangeScene(ScenesType.Login);

                LoginScene loginScene = Engine.SceneManager.GetScene<LoginScene>();

                if (loginScene != null)
                    loginScene.Reconnect = true;

                return;
            }

            _alphaChanged = _alphaTimer < Engine.Ticks;

            if (_alphaChanged)
                _alphaTimer = Engine.Ticks + 20;

            GetViewPort();

            UpdateMaxDrawZ();
            _renderListCount = 0;
            _objectHandlesCount = 0;



            _rectanglePlayer.X = (int)(World.Player.RealScreenPosition.X - World.Player.FrameInfo.X + 22 + World.Player.Offset.X);
            _rectanglePlayer.Y = (int)(World.Player.RealScreenPosition.Y - World.Player.FrameInfo.Y + 22 + (World.Player.Offset.Y - World.Player.Offset.Z));
            _rectanglePlayer.Width = World.Player.FrameInfo.Width;
            _rectanglePlayer.Height = World.Player.FrameInfo.Height;


            int minX = _minTile.X;
            int minY = _minTile.Y;
            int maxX = _maxTile.X;
            int maxY = _maxTile.Y;

            for (int i = 0; i < 2; i++)
            {
                int minValue = minY;
                int maxValue = maxY;

                if (i != 0)
                {
                    minValue = minX;
                    maxValue = maxX;
                }

                for (int lead = minValue; lead < maxValue; lead++)
                {
                    int x = minX;
                    int y = lead;

                    if (i != 0)
                    {
                        x = lead;
                        y = maxY;
                    }

                    while (true)
                    {
                        if (x < minX || x > maxX || y < minY || y > maxY)
                            break;

                        Tile tile = World.Map.GetTile(x, y);

                        if (tile != null)
                            AddTileToRenderList(tile.FirstNode, x, y, _useObjectHandles, 150);
                        x++;
                        y--;
                    }
                }
            }

            _renderIndex++;

            if (_renderIndex >= 100)
                _renderIndex = 1;
            _updateDrawPosition = false;


            //if (_renderList.Length - _renderListCount != 0)
            //{
            //    if (_renderList[_renderListCount] != null)
            //        Array.Clear(_renderList, _renderListCount, _renderList.Length - _renderListCount);
            //}
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (_forceStopScene)
                return;

            if (!World.InGame)
                return;

            if (_renderTarget == null || _renderTarget.Width != (int) (Engine.Profile.Current.GameWindowSize.X * Scale) || _renderTarget.Height != (int) (Engine.Profile.Current.GameWindowSize.Y * Scale))
            {
                _renderTarget?.Dispose();
                _darkness?.Dispose();

                _renderTarget = new RenderTarget2D(Engine.Batcher.GraphicsDevice, (int) (Engine.Profile.Current.GameWindowSize.X * Scale), (int) (Engine.Profile.Current.GameWindowSize.Y * Scale), false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents);
                _darkness = new RenderTarget2D(Engine.Batcher.GraphicsDevice, (int) (Engine.Profile.Current.GameWindowSize.X * Scale), (int) (Engine.Profile.Current.GameWindowSize.Y * Scale), false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents);
            }

            Pathfinder.ProcessAutoWalk();


            if (_inqueue)
            {
                _dequeueAt -= frameMS;

                if (_dequeueAt <= 0)
                {
                    _inqueue = false;

                    if (_queuedObject != null && !_queuedObject.IsDestroyed)
                    {
                        _queuedAction();
                        _queuedObject = null;
                        _queuedAction = null;
                        _dequeueAt = 0;
                    }
                }
            }

            if (_rightMousePressed || _continueRunning)
                MoveCharacterByMouseInput();
            else if (!Engine.Profile.Current.DisableArrowBtn || _isMacroMoveDown)
            {
                if (_arrowKeyPressed)
                    MoveCharacterByKeyboardInput(false);
                else if (_numPadKeyPressed)
                    MoveCharacterByKeyboardInput(true);
            }

            if (_followingMode && _followingTarget.IsMobile && !Pathfinder.AutoWalking)
            {
                Mobile follow = World.Mobiles.Get(_followingTarget);
                
                if (follow != null)
                {
                    int distance = follow.Distance;

                    if (distance > World.ViewRange)
                        StopFollowing();
                    else if (distance > 3)
                        Pathfinder.WalkTo(follow.X, follow.Y, follow.Z, 1);
                }
                else
                {
                    StopFollowing();
                }
            }

            World.Update(totalMS, frameMS);
            Overheads.Update(totalMS, frameMS);

            if (totalMS > _timePing)
            {
                //NetClient.Socket.Send(new PPing());

                NetClient.Socket.Statistics.SendPing();

                _timePing = (long) totalMS + 1000;
            }

            _useItemQueue.Update(totalMS, frameMS);


            if (!IsMouseOverViewport)
                Game.SelectedObject.Object = Game.SelectedObject.LastObject = null;
            else
            {
                if (_viewPortGump != null)
                {
                    Game.SelectedObject.TranslatedMousePositionByViewport.X = (int) ((Mouse.Position.X - _viewPortGump.ScreenCoordinateX) * Scale);
                    Game.SelectedObject.TranslatedMousePositionByViewport.Y = (int) ((Mouse.Position.Y - _viewPortGump.ScreenCoordinateY) * Scale);
                }
                else
                    Game.SelectedObject.TranslatedMousePositionByViewport = Point.Zero;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher)
        {
            if (!World.InGame)
                return false;

            if (Engine.Profile.Current.EnableDeathScreen)
            {
                if (_deathScreenLabel == null || _deathScreenLabel.IsDisposed)
                {
                    if (World.Player.IsDead && World.Player.DeathScreenTimer > Engine.Ticks)
                    {
                        Engine.UI.Add(_deathScreenLabel = new Label("You are dead.", false, 999, 200, 3)
                        {
                            //X = ((Engine.Profile.Current.GameWindowSize.X - Engine.Profile.Current.GameWindowPosition.X) >> 1) - 50,
                            //Y = ((Engine.Profile.Current.GameWindowSize.Y - Engine.Profile.Current.GameWindowPosition.Y) >> 1) - 50,
                            X = (Engine.WindowWidth >> 1) - 50,
                            Y = (Engine.WindowHeight >> 1) - 50
                        });
                        _deathScreenActive = true;
                    }
                }
                else if (World.Player.DeathScreenTimer < Engine.Ticks)
                {
                    _deathScreenActive = false;
                    _deathScreenLabel.Dispose();
                }
            }

            DrawWorld(batcher);


            Game.SelectedObject.LastObject = Game.SelectedObject.Object;

            return base.Draw(batcher);
        }


        //DepthStencilState s2 = new DepthStencilState
        //{
        //    StencilEnable = true,
        //    StencilFunction = CompareFunction.NotEqual,
        //    StencilPass = StencilOperation.Keep,
        //    StencilFail = StencilOperation.Keep,
        //    StencilDepthBufferFail = StencilOperation.Keep,
        //    ReferenceStencil = 0,
        //    DepthBufferEnable = false,
        //};

        private void DrawWorld(UltimaBatcher2D batcher)
        {
            Game.SelectedObject.Object = null;

            batcher.GraphicsDevice.Clear(Color.Black);
            batcher.GraphicsDevice.SetRenderTarget(_renderTarget);

            //if (CircleOfTransparency.Circle == null)
            //    CircleOfTransparency.Create(200);
            //CircleOfTransparency.Circle.Draw(batcher, Engine.WindowWidth >> 1, Engine.WindowHeight >> 1);

            //batcher.GraphicsDevice.Clear(ClearOptions.Stencil, new Vector4(0, 0, 0, 1), 0, 0);


            batcher.Begin();

            //batcher.SetStencil(s2);

            if (!_deathScreenActive)
            {
                RenderedObjectsCount = 0;

                int z = World.Player.Z + 5;
                bool usecircle = Engine.Profile.Current.UseCircleOfTransparency;

                for (int i = 0; i < _renderListCount; i++)
                {
                    //if (!_renderList[i].TryGetTarget(out var obj))
                    //    continue;

                    GameObject obj = _renderList[i];

                    if (obj.Z <= _maxGroundZ)
                    {
                        obj.DrawTransparent = usecircle && obj.TransparentTest(z);

                        if (obj.Draw(batcher, obj.RealScreenPosition.X, obj.RealScreenPosition.Y))
                        {
                            RenderedObjectsCount++;
                        }
                    }

                }

                if (TargetManager.IsTargeting && TargetManager.TargetingState == CursorTarget.MultiPlacement)
                {
                    Item multiTarget = new Item(Serial.INVALID)
                    {
                        Graphic = TargetManager.MultiTargetInfo.Model,
                        IsMulti = true
                    };

                    if (Game.SelectedObject.Object != null && Game.SelectedObject.Object is GameObject gobj && (gobj is Land || gobj is Static))
                    {
                        multiTarget.Position = gobj.Position + TargetManager.MultiTargetInfo.Offset;
                        multiTarget.CheckGraphicChange();
                    }

                    multiTarget.Draw(batcher, multiTarget.RealScreenPosition.X, multiTarget.RealScreenPosition.Y);
                }
            }

            //batcher.SetStencil(null);

            batcher.End();


            DrawLights(batcher);

            batcher.GraphicsDevice.SetRenderTarget(null);
        }


        private void DrawLights(UltimaBatcher2D batcher)
        {
            batcher.GraphicsDevice.SetRenderTarget(null);
            batcher.GraphicsDevice.SetRenderTarget(_darkness);

            _vectorClear.X = _vectorClear.Y = _vectorClear.Z = World.Light.IsometricLevel;

            batcher.GraphicsDevice.Clear(Color.Black);
            batcher.GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, _vectorClear, 0, 0);

            if (_deathScreenActive || !UseLights)
                return;

            batcher.Begin();
            batcher.SetBlendState(BlendState.Additive);

            Vector3 hue = Vector3.Zero;

            for (int i = 0; i < _lightCount; i++)
            {
                ref var l = ref _lights[i];

                SpriteTexture texture = FileManager.Lights.GetTexture(l.ID);

                hue.X = l.Color;
                hue.Y = ShaderHuesTraslator.SHADER_LIGHTS;
                hue.Z = 0;

                batcher.DrawSprite(texture, l.DrawX, l.DrawY, texture.Width, texture.Height, texture.Width >> 1, texture.Height >> 1, ref hue);
            }

            _lightCount = 0;

            batcher.SetBlendState(null);
            batcher.End();
        }

        public void DrawOverheads(UltimaBatcher2D batcher, int x, int y)
        {
            _healthLinesManager.Draw(batcher, Scale);

            //batcher.SetBlendState(_blendText);
            Overheads.Draw(batcher, x, y);

            // batcher.SetBlendState(null);
            // workaround to set overheads clickable
            //_mousePicker.UpdateOverObjects(_mouseOverList, _mouseOverList.MousePosition);
        }


        private struct LightData
        {
            public byte ID;
            public ushort Color;
            public int DrawX, DrawY;
        }
    }
}