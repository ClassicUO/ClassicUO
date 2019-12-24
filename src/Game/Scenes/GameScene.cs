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
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

using ClassicUO.Configuration;
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
using SDL2;

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
        private Rectangle _rectangleObj = Rectangle.Empty, _rectanglePlayer;
        private RenderTarget2D _viewportRenderTarget, _lightRenderTarget;
        private int _scale = 5; // 1.0
        private bool _useObjectHandles;


        private long _timePing;
        private UseItemQueue _useItemQueue = new UseItemQueue();
        private Vector4 _vectorClear = new Vector4(Vector3.Zero, 1);
        private WorldViewport _viewPortGump;
        private Weather _weather;


        public GameScene() : base( (int) SceneID.Game,
            true,
            !ProfileManager.Current.RestoreLastGameSize,
            true)
        {

        }


        public bool UpdateDrawPosition { get; set; }

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

        public InfoBarManager InfoBars { get; private set; }

        public Texture2D ViewportTexture => _viewportRenderTarget;

        public Texture2D LightRenderTarget => _lightRenderTarget;

        public Weather Weather => _weather;

        public bool UseLights => ProfileManager.Current != null && ProfileManager.Current.UseCustomLightLevel ? World.Light.Personal < World.Light.Overall : World.Light.RealPersonal < World.Light.RealOverall;
        public bool UseAltLights => ProfileManager.Current != null && ProfileManager.Current.UseAlternativeLights;
       
        public void DoubleClickDelayed(uint serial)
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

            if (!ProfileManager.Current.DebugGumpIsDisabled)
            {
                UIManager.Add(new DebugGump
                {
                    X = ProfileManager.Current.DebugGumpPosition.X,
                    Y = ProfileManager.Current.DebugGumpPosition.Y
                });
                //Engine.DropFpsMinMaxValues();
            }

            HeldItem = new ItemHold();
            Hotkeys = new HotkeysManager();
            Macros = new MacroManager(ProfileManager.Current.Macros);
            InfoBars = new InfoBarManager();
            _healthLinesManager = new HealthLinesManager();
            _weather = new Weather();

            WorldViewportGump viewport = new WorldViewportGump(this);

            UIManager.Add(viewport);

            if (!ProfileManager.Current.TopbarGumpIsDisabled)
                TopBarGump.Create();

            _viewPortGump = viewport.FindControls<WorldViewport>().SingleOrDefault();

            GameActions.Initialize(PickupItemBegin);


            CommandManager.Initialize();
            NetClient.Socket.Disconnected += SocketOnDisconnected;

            Chat.MessageReceived += ChatOnMessageReceived;

            if (!ProfileManager.Current.EnableScaleZoom || !ProfileManager.Current.SaveScaleAfterClose)
                Scale = 1f;
            else
                Scale = ProfileManager.Current.ScaleZoom;

            ProfileManager.Current.RestoreScaleValue = ProfileManager.Current.ScaleZoom = Scale;
            UIManager.ContainerScale = ProfileManager.Current.ContainersScale / 100f;

            if (ProfileManager.Current.WindowBorderless)
            {
                CUOEnviroment.Client.SetWindowBorderless(true);
            }
            else if (Settings.GlobalSettings.IsWindowMaximized)
            {
                CUOEnviroment.Client.MaximizeWindow();
            }
            else if (Settings.GlobalSettings.WindowSize.HasValue)
            {
                int w = Settings.GlobalSettings.WindowSize.Value.X;
                int h = Settings.GlobalSettings.WindowSize.Value.Y;

                w = Math.Max(640, w);
                h = Math.Max(480, h);

                CUOEnviroment.Client.SetWindowSize(w, h);
                //CUOEnviroment.Client.SetWindowPositionBySettings();
            }


            Plugin.OnConnected();
        }

        private void ChatOnMessageReceived(object sender, UOMessageEventArgs e)
        {
            if (e.Type == MessageType.Command)
                return;

            string name;
            string text;

            ushort hue = e.Hue;

            switch (e.Type)
            {
                case MessageType.Regular:

                    if (e.Parent == null || !SerialHelper.IsValid(e.Parent.Serial))
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
                        hue = ProfileManager.Current.EmoteHue;

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
                    hue = ProfileManager.Current.PartyMessageHue;

                    break;

                case MessageType.Alliance:
                    text = e.Text;
                    name = $"[Alliance][{e.Name}]";
                    hue = ProfileManager.Current.AllyMessageHue;

                    break;

                case MessageType.Guild:
                    text = e.Text;
                    name = $"[Guild][{e.Name}]";
                    hue = ProfileManager.Current.GuildMessageHue;

                    break;

                default:
                    text = e.Text;
                    name = e.Name;
                    hue = e.Hue;

                    Log.Warn( $"Unhandled text type {e.Type}  -  text: '{e.Text}'");

                    break;
            }

            World.Journal.Add(text, hue, name, e.IsUnicode);
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

            ProfileManager.Current?.Save(UIManager.Gumps.OfType<Gump>().Where(s => s.CanBeSaved).Reverse().ToList());
            ProfileManager.UnLoadProfile();

            StaticFilters.CleanCaveTextures();
            StaticFilters.CleanTreeTextures();

            NetClient.Socket.Disconnected -= SocketOnDisconnected;
            NetClient.Socket.Disconnect();
            _viewportRenderTarget?.Dispose();
            _lightRenderTarget?.Dispose();
            
            CommandManager.UnRegisterAll();
            _weather.Reset();

            UIManager.Clear();
            World.Clear();
            UOChatManager.Clear();
          
            _useItemQueue?.Clear();
            _useItemQueue = null;
            Hotkeys = null;
            Macros = null;
            Chat.MessageReceived -= ChatOnMessageReceived;


            Settings.GlobalSettings.WindowSize = new Point(CUOEnviroment.Client.Window.ClientBounds.Width, CUOEnviroment.Client.Window.ClientBounds.Height);
            Settings.GlobalSettings.IsWindowMaximized = CUOEnviroment.Client.IsWindowMaximized();
            CUOEnviroment.Client.SetWindowBorderless(false);

            base.Unload();
        }

        private void SocketOnDisconnected(object sender, SocketError e)
        {
            if (Settings.GlobalSettings.Reconnect)
                _forceStopScene = true;
            else
            {
                UIManager.Add(new MessageBoxGump(200, 200, $"Connection lost:\n{e}", s =>
                {
                    if (s)
                        CUOEnviroment.Client.SetScene(new LoginScene());
                }));
            }
        }

        public void RequestQuitGame()
        {
            UIManager.Add(new QuestionGump("Quit\nUltima Online?", s =>
            {
                if (s)
                    CUOEnviroment.Client.SetScene(new LoginScene());
            }));
        }

        public void AddLight(GameObject obj, GameObject lightObject, int x, int y)
        {
            if (_lightCount >= Constants.MAX_LIGHTS_DATA_INDEX_COUNT || (!UseLights && !UseAltLights) || obj == null)
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
                    else if (obj is Mobile _)
                        light.ID = 1;
                    else
                    {
                        ref readonly var data = ref UOFileManager.TileData.StaticData[obj.Graphic];
                        light.ID = data.Layer;
                    }
                    //else if (GameObjectHelper.TryGetStaticData(lightObject, out StaticTiles data))
                    //    light.ID = data.Layer;
                   
                    //else
                    //    return;
                }


                if (light.ID >= Constants.MAX_LIGHTS_DATA_INDEX_COUNT)
                    return;

                light.Color = ProfileManager.Current.UseColoredLights ? LightColors.GetHue(graphic) : (ushort) 0;

                light.DrawX = x;
                light.DrawY = y;
                _lightCount++;
            }
        }

        private void FillGameObjectList()
        {
            _alphaChanged = _alphaTimer < Time.Ticks;

            if (_alphaChanged)
                _alphaTimer = Time.Ticks + Constants.ALPHA_TIME;

            _foliageIndex++;
            if (_foliageIndex >= 100)
            {
                _foliageIndex = 1;
            }
            _foliageCount = 0;

            GetViewPort();

            _renderListCount = 0;
            _objectHandlesCount = 0;

            _useObjectHandles = NameOverHeadManager.IsToggled || _ctrlAndShiftPressed;

            if (_useObjectHandles)
                NameOverHeadManager.Open();
            else
                NameOverHeadManager.Close();

            _rectanglePlayer.X = (int) (World.Player.RealScreenPosition.X - World.Player.FrameInfo.X + 22 + World.Player.Offset.X);
            _rectanglePlayer.Y = (int) (World.Player.RealScreenPosition.Y - World.Player.FrameInfo.Y + 22 + (World.Player.Offset.Y - World.Player.Offset.Z));
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
                            AddTileToRenderList(tile.FirstNode, x, y, _useObjectHandles, 150/*, null*/);
                        x++;
                        y--;
                    }
                }
            }

            if (_alphaChanged)
            {
                for (int i = 0; i < _foliageCount; i++)
                {
                    var f = _foliages[i];

                    if (f.FoliageIndex == _foliageIndex)
                    {
                        f.ProcessAlpha(Constants.FOLIAGE_ALPHA);
                    }
                    else
                    {
                        f.ProcessAlpha(0xFF);
                    }
                }
            }
                

            UpdateTextServerEntities(World.Mobiles);
            UpdateTextServerEntities(World.Items);

            _renderIndex++;

            if (_renderIndex >= 100)
                _renderIndex = 1;
            UpdateDrawPosition = false;
        }

        private void UpdateTextServerEntities(IEnumerable<Entity> entities)
        {
            foreach (Entity e in entities)
            {
                if (e.UseInRender != _renderIndex && e.TextContainer != null)
                {
                    e.UpdateRealScreenPosition(_offset.X, _offset.Y);
                    e.UseInRender = (byte)_renderIndex;
                }
            }
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            PacketHandlers.SendMegaClilocRequests();

            if (_forceStopScene)
            {
                var loginScene = new LoginScene();
                CUOEnviroment.Client.SetScene(loginScene);
                loginScene.Reconnect = true;

                return;
            }

            if (!World.InGame)
                return;

            
            World.Update(totalMS, frameMS);
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
            else if (!ProfileManager.Current.DisableArrowBtn || _isMacroMoveDown)
            {
                if (_arrowKeyPressed)
                    MoveCharacterByKeyboardInput(false);
                else if (_numPadKeyPressed)
                    MoveCharacterByKeyboardInput(true);
            }

            if (_followingMode && SerialHelper.IsMobile(_followingTarget) && !Pathfinder.AutoWalking)
            {
                Mobile follow = World.Mobiles.Get(_followingTarget);

                if (follow != null)
                {
                    int distance = follow.Distance;

                    if (distance > World.ClientViewRange)
                        StopFollowing();
                    else if (distance > 3)
                        Pathfinder.WalkTo(follow.X, follow.Y, follow.Z, 1);
                }
                else
                    StopFollowing();
            }


            if (totalMS > _timePing)
            {
                NetClient.Socket.Statistics.SendPing();
                _timePing = (long) totalMS + 1000;
            }

            Macros.Update();
            _useItemQueue.Update(totalMS, frameMS);

            if (!IsMouseOverViewport)
                SelectedObject.Object = SelectedObject.LastObject = null;
            else
            {
                if (_viewPortGump != null)
                {
                    SelectedObject.TranslatedMousePositionByViewport.X = (int) ((Mouse.Position.X - _viewPortGump.ScreenCoordinateX) * Scale);
                    SelectedObject.TranslatedMousePositionByViewport.Y = (int) ((Mouse.Position.Y - _viewPortGump.ScreenCoordinateY) * Scale);
                }
                else
                    SelectedObject.TranslatedMousePositionByViewport = Point.Zero;
            }

            if (TargetManager.IsTargeting && TargetManager.TargetingState == CursorTarget.MultiPlacement && World.CustomHouseManager == null && TargetManager.MultiTargetInfo != null)
            {
                if (_multi == null)
                    _multi = new Item(0)
                    {
                        Graphic = TargetManager.MultiTargetInfo.Model,
                        Hue = TargetManager.MultiTargetInfo.Hue,
                        IsMulti = true,
                    };

                if (SelectedObject.Object is GameObject gobj)
                {
                    ushort x, y;
                    sbyte z;

                    var o = gobj.Tile?.FirstNode;
                    if (o != null)
                    {
                        x = o.X;
                        y = o.Y;
                        z = o.Z;
                    }
                    else
                    {
                        x = gobj.X;
                        y = gobj.Y;
                        z = gobj.Z;
                    }

                    World.Map.GetMapZ(x, y, out sbyte groundZ, out sbyte _);

                    if (gobj is Static st && st.ItemData.IsWet)
                        groundZ = gobj.Z;

                    x = (ushort) (x - TargetManager.MultiTargetInfo.XOff);
                    y = (ushort) (y - TargetManager.MultiTargetInfo.YOff);
                    z = (sbyte) (groundZ - TargetManager.MultiTargetInfo.ZOff);

                    _multi.X = x;
                    _multi.Y = y;
                    _multi.Z = z;
                    _multi.UpdateScreenPosition();
                    _multi.CheckGraphicChange();
                    _multi.AddToTile();
                    World.HouseManager.TryGetHouse(_multi.Serial, out var house);

                    foreach (Multi s in house.Components)
                    {
                        s.IsFromTarget = true;
                        s.X = (ushort) (_multi.X + s.MultiOffsetX);
                        s.Y = (ushort) (_multi.Y + s.MultiOffsetY);
                        s.Z = (sbyte) (_multi.Z + s.MultiOffsetZ);
                        s.UpdateScreenPosition();
                        s.AddToTile();
                    }
                }
            }
            else if (_multi != null)
            {
                World.HouseManager.RemoveMultiTargetHouse();
                _multi.Destroy();
                _multi = null;
            }


            if (_isMouseLeftDown && !IsHoldingItem && Time.Ticks - _holdMouse2secOverItemTime >= 1000)
            {
                if (PickupItemBegin(SelectedObject.LastObject as Item, 0, 0))
                {
                    _isMouseLeftDown = false;
                    _holdMouse2secOverItemTime = 0;
                }
            }
        }

        public override void FixedUpdate(double totalMS, double frameMS)
        {
            FillGameObjectList();
        }



        public override bool Draw(UltimaBatcher2D batcher)
        {
            if (!World.InGame)
                return false;

            if (ProfileManager.Current.EnableDeathScreen)
            {
                if (_deathScreenLabel == null || _deathScreenLabel.IsDisposed)
                {
                    if (World.Player.IsDead && World.Player.DeathScreenTimer > Time.Ticks)
                    {
                        UIManager.Add(_deathScreenLabel = new Label("You are dead.", false, 999, 200, 3)
                        {
                            X = (CUOEnviroment.Client.Window.ClientBounds.Width >> 1) - 50,
                            Y = (CUOEnviroment.Client.Window.ClientBounds.Height >> 1) - 50
                        });
                        _deathScreenActive = true;
                    }
                }
                else if (World.Player.DeathScreenTimer < Time.Ticks)
                {
                    _deathScreenActive = false;
                    _deathScreenLabel.Dispose();
                }
            }

            DrawWorld(batcher);

            return base.Draw(batcher);
        }



        private void DrawWorld(UltimaBatcher2D batcher)
        {
            SelectedObject.Object = null;

            batcher.GraphicsDevice.Clear(Color.Black);
            batcher.GraphicsDevice.SetRenderTarget(_viewportRenderTarget);

            //if (CircleOfTransparency.Circle == null)
            //    CircleOfTransparency.Create(200);
            //CircleOfTransparency.Circle.Draw(batcher,
            //                                 ((ProfileManager.Current.GameWindowSize.X / 2)),
            //                                 ((ProfileManager.Current.GameWindowSize.Y / 2)));

            //batcher.GraphicsDevice.Clear(ClearOptions.Stencil | ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 0, 0);


            batcher.SetBrightlight(ProfileManager.Current.Brighlight);

            batcher.Begin();

            //batcher.SetStencil(s2);

            if (!_deathScreenActive)
            {
                RenderedObjectsCount = 0;

                int z = World.Player.Z + 5;
                bool usecircle = ProfileManager.Current.UseCircleOfTransparency;

                for (int i = 0; i < _renderListCount; i++)
                {
                    GameObject obj = _renderList[i];

                    if (obj.Z <= _maxGroundZ)
                    {
                        obj.DrawTransparent = usecircle && obj.TransparentTest(z);

                        if (obj.Draw(batcher, obj.RealScreenPosition.X, obj.RealScreenPosition.Y))
                            RenderedObjectsCount++;
                    }
                }

                if (_multi != null && TargetManager.IsTargeting && TargetManager.TargetingState == CursorTarget.MultiPlacement)
                    _multi.Draw(batcher, _multi.RealScreenPosition.X, _multi.RealScreenPosition.Y);
            }

            //batcher.SetStencil(null);

            // draw weather
            _weather.Draw(batcher, 0, 0 /*ProfileManager.Current.GameWindowPosition.X, ProfileManager.Current.GameWindowPosition.Y*/);
            batcher.End();

            DrawLights(batcher);          

            batcher.GraphicsDevice.SetRenderTarget(null);
        }

        private Item _multi;

        private void DrawLights(UltimaBatcher2D batcher)
        {
            if (_deathScreenActive || (!UseLights && !UseAltLights))
                return;

            batcher.GraphicsDevice.SetRenderTarget(_lightRenderTarget);

            if (UseAltLights)
            {
                batcher.GraphicsDevice.Clear(ClearOptions.Target, Color.Black, 0, 0);
            }
            else
            {
                var lightColor = World.Light.IsometricLevel;

                if (ProfileManager.Current.UseDarkNights)
                    lightColor -= 0.02f;

                _vectorClear.X = _vectorClear.Y = _vectorClear.Z = lightColor;

                batcher.GraphicsDevice.Clear(ClearOptions.Target, Color.Black, 0, 0);
                batcher.GraphicsDevice.Clear(ClearOptions.Target, _vectorClear, 0, 0);
            }
            
            batcher.Begin();
            batcher.SetBlendState(BlendState.Additive);

            Vector3 hue = Vector3.Zero;

            for (int i = 0; i < _lightCount; i++)
            {
                ref var l = ref _lights[i];

                UOTexture texture = UOFileManager.Lights.GetTexture(l.ID);

                hue.X = l.Color;
                hue.Y = ShaderHuesTraslator.SHADER_LIGHTS;
                hue.Z = 0;

                batcher.DrawSprite(texture, l.DrawX - (texture.Width >> 1), l.DrawY - (texture.Height >> 1), false, ref hue);
            }

            _lightCount = 0;

            batcher.SetBlendState(null);
            batcher.End();
        }

        public void DrawOverheads(UltimaBatcher2D batcher, int x, int y)
        {
            _healthLinesManager.Draw(batcher, Scale);

            int renderIndex = _renderIndex - 1;

            if (renderIndex < 1)
                renderIndex = 99;


            World.WorldTextManager.ProcessWorldText(true);
            World.WorldTextManager.Draw(batcher, x, y, renderIndex);

            if (!IsMouseOverViewport)
                SelectedObject.Object = null;

            SelectedObject.LastObject = SelectedObject.Object;
        }

        private Vector3 _selectionLines = Vector3.Zero;

        public void DrawSelection(UltimaBatcher2D batcher, int x, int y)
        {
            if (_isSelectionActive)
            {
                _selectionLines.Z = 0.3F;
                batcher.Draw2D(Textures.GetTexture(Color.Black), _selectionStart.Item1, _selectionStart.Item2, Mouse.Position.X - _selectionStart.Item1, Mouse.Position.Y - _selectionStart.Item2, ref _selectionLines);
                _selectionLines.Z = 0.7f;
                batcher.DrawRectangle(Textures.GetTexture(Color.DeepSkyBlue), _selectionStart.Item1, _selectionStart.Item2, Mouse.Position.X - _selectionStart.Item1, Mouse.Position.Y - _selectionStart.Item2, ref _selectionLines);
            }
        }

        private void StopFollowing()
        {
            if (_followingMode)
            {
                _followingMode = false;
                _followingTarget = 0;
                Pathfinder.StopAutoWalk();
                World.Player.AddMessage(MessageType.Regular, "Stopped following.", 3, 1001, false);
            }
        }


        private struct LightData
        {
            public byte ID;
            public ushort Color;
            public int DrawX, DrawY;
        }
    }
}