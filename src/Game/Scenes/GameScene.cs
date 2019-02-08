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
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Map;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility.Coroutines;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using System.Diagnostics;

namespace ClassicUO.Game.Scenes
{
    internal partial class GameScene : Scene
    {
        private RenderTarget2D _renderTarget;
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
        private float _scale = 1;
        private bool _forceStopScene = false;

        public GameScene() : base()
        {
        }

        public float Scale
        {
            get => _scale;
            set
            {

                if (value < 0.7f)
                    value = 0.7f;
                else if (value > 2.3f)
                    value = 2.3f;
                _scale = value;              
            }
        }

        public HotkeysManager Hotkeys => _hotkeysManager;

        public MacroManager Macros => _macroManager;

        public Texture2D ViewportTexture => _renderTarget;

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

            Engine.UI.Add(new DebugGump());

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
                if (!Engine.Profile.Current.EnableScaleZoom)
                    return;

                if (e.Direction == MouseEvent.WheelScrollDown)
                    Scale += 0.1f;
                else
                    Scale -= 0.1f;

                if (Scale < 0.7f)
                    Scale = 0.7f;
                else if (Scale > 2.3f)
                    Scale = 2.3f;

                if (Engine.Profile.Current.SaveScaleAfterClose)
                    Engine.Profile.Current.ScaleZoom = Scale;
            };

            Engine.Input.KeyDown += OnKeyDown;
            Engine.Input.KeyUp += OnKeyUp;

            CommandManager.Initialize();
            NetClient.Socket.Disconnected += SocketOnDisconnected;

            Chat.Message += ChatOnMessage;

            if (Engine.Profile.Current.SaveScaleAfterClose)
                Scale = Engine.Profile.Current.ScaleZoom;
            else
                Scale = 1f; // hard return to 1.0f


            Plugin.OnConnected();
            //Coroutine.Start(this, CastSpell());
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

            _journalManager.Add(text, e.Font, hue, name);
        }

        private IEnumerable<IWaitCondition> CastSpell()
        {
            while (true)
            {
                yield return new WaitTime(TimeSpan.FromMilliseconds(1));

                foreach (Mobile mobile in World.Mobiles)
                {
                    mobile.AddOverhead(MessageType.Regular, "AAAAAAAAAAAAAAAAAAAAA", 1, 0x45, true);
                }
            }

        }

        public override void Unload()
        {
            try
            {
                HeldItem.Clear();

                Engine.UI.Clear();
                World.Clear();

                Plugin.OnDisconnected();

                _renderList = null;

                TargetManager.ClearTargetingWithoutTargetCancelPacket();

                Engine.Profile.Current?.Save(Engine.UI.Gumps.OfType<Gump>().Where(s => s.CanBeSaved).Reverse().ToList());
                Engine.Profile.UnLoadProfile();

                NetClient.Socket.Disconnected -= SocketOnDisconnected;
                NetClient.Socket.Disconnect();
                _renderTarget?.Dispose();
                CommandManager.UnRegisterAll();

                _viewPortGump.MouseDown -= OnMouseDown;
                _viewPortGump.MouseUp -= OnMouseUp;
                _viewPortGump.MouseDoubleClick -= OnMouseDoubleClick;
                _viewPortGump.DragBegin -= OnMouseDragBegin;

                Engine.Input.KeyDown -= OnKeyDown;
                Engine.Input.KeyUp -= OnKeyUp;

                _overheadManager.Dispose();
                _overheadManager = null;
                _journalManager.Clear();
                _journalManager = null;
                _overheadManager = null;
                _useItemQueue.Clear();
                _useItemQueue = null;
                _hotkeysManager = null;
                _macroManager = null;
                Chat.Message -= ChatOnMessage;

            } catch
            {

            }

            base.Unload();
        }

        private void SocketOnDisconnected(object sender, SocketError e)
        {
            _forceStopScene = true;
            /*
            Engine.UI.Add(new MessageBoxGump(200, 200, $"Connection lost:\n{e}", (s) =>
            {         
                if (s)
                    Engine.SceneManager.ChangeScene(ScenesType.Login);
            }));
            */
        }

        private bool _alphaChanged;
        private long _alphaTimer;

        public void RequestQuitGame()
        {
            Engine.UI.Add(new QuestionGump("Quit\nUltima Online?", s =>
            {
                if (s)
                    Engine.SceneManager.ChangeScene(ScenesType.Login);
            }));
        }

        public override void FixedUpdate(double totalMS, double frameMS)
        {
            base.FixedUpdate(totalMS, frameMS);

            if (_forceStopScene)
            {
                Engine.SceneManager.ChangeScene(ScenesType.Login);
                return;
            }

            if (!World.InGame)
                return;

            _alphaChanged = _alphaTimer < Engine.Ticks;

            if (_alphaChanged)
                _alphaTimer = Engine.Ticks + 20;

            GetViewPort();

            UpdateMaxDrawZ();
            _renderListCount = 0;

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
                            AddTileToRenderList(tile.FirstNode, x, y, false, 150);
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
                _renderTarget = new RenderTarget2D(Engine.Batcher.GraphicsDevice, (int)(Engine.Profile.Current.GameWindowSize.X * Scale), (int)(Engine.Profile.Current.GameWindowSize.Y * Scale), false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents);
            }

            Pathfinder.ProcessAutoWalk();
            SelectedObject = _mousePicker.MouseOverObject;

            if (_inqueue)
            {
                _dequeueAt -= frameMS;

                if (_dequeueAt <= 0)
                {
                    _inqueue = false;

                    if (_queuedObject != null && !_queuedObject.IsDisposed)
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

            _mouseOverList.Clear();

            if (_rightMousePressed || _continueRunning)
                MoveCharacterByInputs();
            // ===================================
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
            DrawWorld(batcher);
            _mousePicker.UpdateOverObjects(_mouseOverList, _mouseOverList.MousePosition);

            return base.Draw(batcher);
        }

        private void DrawWorld(Batcher2D batcher)
        {
            batcher.GraphicsDevice.Clear(Color.Black);
            batcher.GraphicsDevice.SetRenderTarget(_renderTarget);
            batcher.Begin();
            batcher.EnableLight(true);
            batcher.SetLightIntensity(World.Light.IsometricLevel);
            batcher.SetLightDirection(World.Light.IsometricDirection);
            RenderedObjectsCount = 0;



            //int drawX = (Engine.Profile.Current.GameWindowSize.X >> 1);
            //int drawY = (Engine.Profile.Current.GameWindowSize.Y >> 1) - 22;

            //if (CircleOfTransparency.Circle == null)
            //    CircleOfTransparency.Create(100);
            //CircleOfTransparency.Circle.Draw(batcher, drawX, drawY);

            int z = World.Player.Z + 5;
            bool usecircle = Engine.Profile.Current.UseCircleOfTransparency;

            for (int i = 0; i < _renderListCount; i++)
            {
                GameObject obj = _renderList[i];
                if (obj.Z <= _maxGroundZ)
                {
                    obj.DrawTransparent = usecircle && obj.TransparentTest(z);

                    if (obj.Draw(batcher, obj.RealScreenPosition, _mouseOverList))
                    {
                        RenderedObjectsCount++;
                    }

                }
            }

            // Draw in game overhead text messages
            _overheadManager.Draw(batcher, _mouseOverList, _offset);



            batcher.End();
            batcher.EnableLight(false);
            batcher.GraphicsDevice.SetRenderTarget(null);
        }
    }
}