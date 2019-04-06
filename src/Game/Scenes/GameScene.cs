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
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

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
using ClassicUO.Utility.Coroutines;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Scenes
{
    internal partial class GameScene : Scene
    {
        private RenderTarget2D _renderTarget, _darkness;
        private long _timePing;
        private MousePicker _mousePicker;
        private MouseOverList _mouseOverList;
        private WorldViewport _viewPortGump;
        private JournalManager _journalManager;
        private OverheadManager _overheadManager;
        private HotkeysManager _hotkeysManager;
        private MacroManager _macroManager;
        private GameObject _selectedObject;
        private UseItemQueue _useItemQueue = new UseItemQueue();
        private bool _alphaChanged;
        private long _alphaTimer;
        private bool _forceStopScene = false;
        private readonly float[] _scaleArray = Enumerable.Range(5, 21).Select(i => i / 10.0f).ToArray(); // 0.5 => 2.5
        private int _scale = 5; // 1.0
        private readonly BlendState _blendText = new BlendState()
        {
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            ColorBlendFunction = BlendFunction.Add
        };
        private int _lightCount;
        private readonly LightData[] _lights = new LightData[Constants.MAX_LIGHTS_DATA_INDEX_COUNT];
        private bool _deathScreenActive;
        private Label _deathScreenLabel;
        private Vector4 _vectorClear = new Vector4(Vector3.Zero, 1);

        public GameScene() : base()
        {
        }

        private int ScalePos
        {
            get => _scale;
            set
            {
                if (value < 0)
                    value = 0;
                else if (value >= (_scaleArray.Length - 1))
                    value = _scaleArray.Length - 1;

                _scale = value;
            }
        }

        public float Scale
        {
            get => _scaleArray[_scale];
            set => ScalePos = (int)(value * 10) - 5;
        }

        public HotkeysManager Hotkeys => _hotkeysManager;

        public MacroManager Macros => _macroManager;

        public Texture2D ViewportTexture => _renderTarget;

        public Texture2D Darkness => _darkness;

        public Point MouseOverWorldPosition => _viewPortGump == null ? Point.Zero : new Point((int) ((Mouse.Position.X - _viewPortGump.ScreenCoordinateX) * Scale), (int) ((Mouse.Position.Y - _viewPortGump.ScreenCoordinateY) * Scale));

        public GameObject SelectedObject
        {
            get => _selectedObject;
            set
            {
                if (_selectedObject == value)
                    return;

                if (value == null)
                {
                    _selectedObject.IsSelected = false;
                    _selectedObject = null;
                }
                else
                {
                    if (_selectedObject != null && _selectedObject.IsSelected)
                        _selectedObject.IsSelected = false;
                    _selectedObject = value;

                    
                    _selectedObject.IsSelected = true;
                }
            }
        }

        public JournalManager Journal => _journalManager;

        public OverheadManager Overheads => _overheadManager;

        private bool UseLights => Engine.Profile.Current != null && Engine.Profile.Current.UseCustomLightLevel ? World.Light.Personal < World.Light.Overall : World.Light.RealPersonal < World.Light.RealOverall;

        public void DoubleClickDelayed(Serial serial)
            => _useItemQueue.Add(serial);

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
                Engine.UI.Add(new DebugGump()
                {
                    X = Engine.Profile.Current.DebugGumpPosition.X,
                    Y = Engine.Profile.Current.DebugGumpPosition.Y,
                });
                Engine.DropFpsMinMaxValues();
            }

            HeldItem = new ItemHold();
            _journalManager = new JournalManager();
            _overheadManager = new OverheadManager();
            _hotkeysManager = new HotkeysManager();
            _macroManager = new MacroManager(Engine.Profile.Current.Macros);
            _mousePicker = new MousePicker();
            _mouseOverList = new MouseOverList(_mousePicker);

            WorldViewportGump viewport = new WorldViewportGump(this);
            
            Engine.UI.Add(viewport);

            if (! Engine.Profile.Current.TopbarGumpIsDisabled)
                TopBarGump.Create();

            _viewPortGump = viewport.FindControls<WorldViewport>().SingleOrDefault();

            GameActions.Initialize(PickupItemBegin);

            _viewPortGump.MouseDown += OnMouseDown;
            _viewPortGump.MouseUp += OnMouseUp;
            _viewPortGump.MouseDoubleClick += OnMouseDoubleClick;
            _viewPortGump.MouseOver += OnMouseMove;
            _viewPortGump.MouseWheel += (sender, e) =>
            {
                if (!Engine.Profile.Current.EnableScaleZoom || !Input.Keyboard.Ctrl)
                    return;

                if (e.Direction == MouseEvent.WheelScrollDown)
                    ScalePos++;
                else
                    ScalePos--;

                if (Engine.Profile.Current.SaveScaleAfterClose)
                    Engine.Profile.Current.ScaleZoom = Scale;
            };

            Engine.Input.KeyDown += OnKeyDown;
            Engine.Input.KeyUp += OnKeyUp;

            CommandManager.Initialize();
            NetClient.Socket.Disconnected += SocketOnDisconnected;

            Chat.Message += ChatOnMessage;

            if (!Engine.Profile.Current.EnableScaleZoom || !Engine.Profile.Current.SaveScaleAfterClose)
                Scale = 1f;
            else
                Scale = Engine.Profile.Current.ScaleZoom;

            Engine.Profile.Current.RestoreScaleValue = Engine.Profile.Current.ScaleZoom = Scale;

            Plugin.OnConnected();
        }

        private void ChatOnMessage(object sender, UOMessageEventArgs e)
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
            
            _journalManager.Add(text, hue, name, e.IsUnicode);
        }

        public override void Unload()
        {
            HeldItem?.Clear();

            try
            {
                Plugin.OnDisconnected();
            }
            catch { }

            _renderList = null;

            TargetManager.ClearTargetingWithoutTargetCancelPacket();

            Engine.Profile.Current?.Save(Engine.UI.Gumps.OfType<Gump>().Where(s => s.CanBeSaved).Reverse().ToList());
            Engine.Profile.UnLoadProfile();

            NetClient.Socket.Disconnected -= SocketOnDisconnected;
            NetClient.Socket.Disconnect();
            _renderTarget?.Dispose();
            CommandManager.UnRegisterAll();

            Engine.UI?.Clear();
            World.Clear();

            _viewPortGump.MouseDown -= OnMouseDown;
            _viewPortGump.MouseUp -= OnMouseUp;
            _viewPortGump.MouseDoubleClick -= OnMouseDoubleClick;
            _viewPortGump.DragBegin -= OnMouseDragBegin;

            Engine.Input.KeyDown -= OnKeyDown;
            Engine.Input.KeyUp -= OnKeyUp;

            _overheadManager?.Clear();
            _overheadManager = null;
            _journalManager?.Clear();
            _journalManager = null;
            _overheadManager = null;
            _useItemQueue?.Clear();
            _useItemQueue = null;
            _hotkeysManager = null;
            _macroManager = null;
            Chat.Message -= ChatOnMessage;
            _blendText?.Dispose();

            base.Unload();
        }

        private void SocketOnDisconnected(object sender, SocketError e)
        {
            if (Engine.GlobalSettings.Reconnect)
                _forceStopScene = true;
            else
            {
                Engine.UI.Add(new MessageBoxGump(200, 200, $"Connection lost:\n{e}", (s) =>
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
                sbyte z5 = (sbyte)(obj.Z + 5);

                for (GameObject o = tile.FirstNode; o != null; o = o.Right)
                {
                    if ((  (!(o is Static s) || s.ItemData.IsTransparent)   && 
                            (!(o is Multi m) || m.ItemData.IsTransparent)) || !o.AllowedToDraw)
                    {
                        continue;
                    }

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

                if ((graphic >= 0x3E02 && graphic <= 0x3E0B) ||
                    (graphic >= 0x3914 && graphic <= 0x3929) || graphic == 0x0B1D)
                {
                    light.ID = 2;
                }
                else
                {
                    if (obj == lightObject && obj is Item item)
                    {
                        light.ID = item.LightID;
                    }
                    else if (GameObjectHelper.TryGetStaticData(lightObject, out StaticTiles data))
                        light.ID = data.Layer;
                    else
                    {
                        // not handled
                    }
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
                        {
                            AddTileToRenderList(tile.FirstNode, x, y, _useObjectHandles, 150);
                        }
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

                _renderTarget = new RenderTarget2D(Engine.Batcher.GraphicsDevice, (int)(Engine.Profile.Current.GameWindowSize.X * Scale), (int)(Engine.Profile.Current.GameWindowSize.Y * Scale), false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents);
                _darkness = new RenderTarget2D(Engine.Batcher.GraphicsDevice, (int)(Engine.Profile.Current.GameWindowSize.X * Scale), (int)(Engine.Profile.Current.GameWindowSize.Y * Scale), false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents);
            }

            Pathfinder.ProcessAutoWalk();
            SelectedObject = _mousePicker.MouseOverObject;

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

            if (Engine.UI.IsMouseOverWorld)
            {
                _mouseOverList.MousePosition = _mousePicker.Position = MouseOverWorldPosition;
                _mousePicker.PickOnly = PickerType.PickEverything;
            }
            else if (SelectedObject != null) SelectedObject = null;

            _mouseOverList?.Clear();

            if (_rightMousePressed || _continueRunning)
                MoveCharacterByInputs();

            World.Update(totalMS, frameMS);
            _overheadManager.Update(totalMS, frameMS);

            if (totalMS > _timePing)
            {
                NetClient.Socket.Send(new PPing());
                _timePing = (long) totalMS + 10000;
            }

            _useItemQueue.Update(totalMS, frameMS);
        }
        
        public override bool Draw(Batcher2D batcher)
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
                            //X = (Engine.Profile.Current.GameWindowSize.X - Engine.Profile.Current.GameWindowPosition.X) / 2 - 50,
                            //Y = (Engine.Profile.Current.GameWindowSize.Y - Engine.Profile.Current.GameWindowPosition.Y) / 2 - 50,
                            X = Engine.WindowWidth / 2 - 50,
                            Y = Engine.WindowHeight / 2 - 50
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


            return base.Draw(batcher);
        }


        
        private void DrawWorld(Batcher2D batcher)
        {
            batcher.GraphicsDevice.Clear(Color.Black);
            batcher.GraphicsDevice.SetRenderTarget(_renderTarget);

            batcher.Begin();
            batcher.SetLightDirection(World.Light.IsometricDirection);
            if (!_deathScreenActive)
            {
                RenderedObjectsCount = 0;

                int z = World.Player.Z + 5;
                bool usecircle = Engine.Profile.Current.UseCircleOfTransparency;

                for (int i = 0; i < _renderListCount; i++)
                {
                    //if (!_renderList[i].TryGetTarget(out var obj))
                    //    continue;

                    var obj = _renderList[i];

                    if (obj.Z <= _maxGroundZ)
                    {
                        obj.DrawTransparent = usecircle && obj.TransparentTest(z);

                        if (obj.Draw(batcher, obj.RealScreenPosition, _mouseOverList))
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

                    if (SelectedObject != null && (SelectedObject is Land || SelectedObject is Static))
                    {                        
                        multiTarget.Position = SelectedObject.Position + TargetManager.MultiTargetInfo.Offset;
                        multiTarget.CheckGraphicChange();
                    }
                    multiTarget.Draw(batcher, multiTarget.ScreenPosition, _mouseOverList);
                }

            }
            batcher.End();
          
            DrawLights(batcher);

            batcher.GraphicsDevice.SetRenderTarget(null);
        }


        private void DrawLights(Batcher2D batcher)
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

            for (int i = 0; i < _lightCount; i++)
            {
                ref var l = ref _lights[i];

                SpriteTexture texture = FileManager.Lights.GetTexture(l.ID);

                SpriteVertex[] vertex = SpriteVertex.PolyBuffer;
                vertex[0].Position.X = l.DrawX - texture.Width / 2;
                vertex[0].Position.Y = l.DrawY - texture.Height / 2;
                vertex[0].TextureCoordinate.Y = 0;
                vertex[1].Position = vertex[0].Position;
                vertex[1].Position.X += texture.Width;
                vertex[1].TextureCoordinate.Y = 0;
                vertex[2].Position = vertex[0].Position;
                vertex[2].Position.Y += texture.Height;
                vertex[3].Position = vertex[1].Position;
                vertex[3].Position.Y += texture.Height;

                for (int j = 0; j < 4; j++)
                {
                    ref SpriteVertex v = ref vertex[j];
                    v.Hue.X = l.Color;
                    v.Hue.Y = 13;
                    v.Hue.Z = 0f;
                }
                batcher.DrawSprite(texture, vertex);
            }
            _lightCount = 0;

            batcher.SetBlendState(null);
            batcher.End();
        }

        public void DrawOverheads(Batcher2D batcher, int x, int y)
        {
            batcher.SetBlendState(_blendText);
            _overheadManager.Draw(batcher, _mouseOverList, _offset, x, y);
            batcher.SetBlendState(null);
            // workaround to set overheads clickable
            _mousePicker.UpdateOverObjects(_mouseOverList, _mouseOverList.MousePosition);
        }


        private struct LightData
        {
            public byte ID;
            public ushort Color;
            public int DrawX, DrawY;
        }
    }
}