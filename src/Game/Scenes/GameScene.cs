﻿#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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

using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.GameObjects.Managers;
using ClassicUO.Game.Gumps.UIGumps;
using ClassicUO.Game.Map;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Scenes
{
    public partial class GameScene : Scene
    {
        private RenderTarget2D _renderTarget;
        private long _timePing;
        private MousePicker _mousePicker;
        private MouseOverList _mouseOverList;
        private WorldViewport _viewPortGump;
        private TopBarGump _topBarGump;
        private StaticManager _staticManager;
        private EffectManager _effectManager;
        private Settings _settings;
        private static GameObject _selectedObject;

        public GameScene() : base(ScenesType.Game)
        {
        }

        public float Scale { get; set; } = 1;

        public Texture2D ViewportTexture => _renderTarget;

        public Point MouseOverWorldPosition => new Point((int) ((Mouse.Position.X - _viewPortGump.ScreenCoordinateX) * Scale), (int) ((Mouse.Position.Y - _viewPortGump.ScreenCoordinateY) * Scale));

        public GameObject SelectedObject
        {
            get => _selectedObject;
            set
            {
                if (_selectedObject == value)
                    return;

                if (value == null)
                {
                    _selectedObject.View.IsSelected = false;
                    _selectedObject = null;
                }
                else
                {
                    if (_selectedObject != null && _selectedObject.View.IsSelected)
                        _selectedObject.View.IsSelected = false;
                    _selectedObject = value;

                    if (Service.Get<Settings>().HighlightGameObjects)
                        _selectedObject.View.IsSelected = true;
                }
            }
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
            Service.Register(_effectManager = new EffectManager());
            Service.Register(_staticManager = new StaticManager());
            _mousePicker = new MousePicker();
            _mouseOverList = new MouseOverList(_mousePicker);
            UIManager.Add(new WorldViewportGump(this));
            UIManager.Add(_topBarGump = new TopBarGump(this));           
            _viewPortGump = Service.Get<WorldViewport>();
            _settings = Service.Get<Settings>();
            GameActions.Initialize(PickupItemBegin);
            InputManager.LeftMouseButtonDown += OnLeftMouseButtonDown;
            InputManager.LeftMouseButtonUp += OnLeftMouseButtonUp;
            InputManager.LeftMouseDoubleClick += OnLeftMouseDoubleClick;
            InputManager.RightMouseButtonDown += OnRightMouseButtonDown;
            InputManager.RightMouseButtonUp += OnRightMouseButtonUp;
            InputManager.RightMouseDoubleClick += OnRightMouseDoubleClick;
            InputManager.DragBegin += OnMouseDragBegin;
            InputManager.MouseDragging += OnMouseDragging;
            InputManager.MouseMoving += OnMouseMoving;
            InputManager.KeyDown += OnKeyDown;
            InputManager.KeyUp += OnKeyUp;

            InputManager.MouseWheel += (sender, e) =>
            {
                if (IsMouseOverWorld)
                {
                    if (!e)
                        Scale += 0.1f;
                    else
                        Scale -= 0.1f;

                    if (Scale < 0.7f)
                        Scale = 0.7f;
                    else if (Scale > 2.3f)
                        Scale = 2.3f;
                }
            };
            UIManager.Add(new OptionsGump1());

            NetClient.Socket.Disconnected += SocketOnDisconnected;
        }

        

        public override void Unload()
        {
            NetClient.Socket.Disconnected -= SocketOnDisconnected;
            NetClient.Socket.Disconnect();
            _renderTarget?.Dispose();            
            UIManager.SaveGumps();
            UIManager.Clear();
            CleaningResources();
            World.Clear();
            Service.Unregister<EffectManager>();
            Service.Unregister<StaticManager>();
            InputManager.LeftMouseButtonDown -= OnLeftMouseButtonDown;
            InputManager.LeftMouseButtonUp -= OnLeftMouseButtonUp;
            InputManager.LeftMouseDoubleClick -= OnLeftMouseDoubleClick;
            InputManager.RightMouseButtonDown -= OnRightMouseButtonDown;
            InputManager.RightMouseButtonUp -= OnRightMouseButtonUp;
            InputManager.RightMouseDoubleClick -= OnRightMouseDoubleClick;
            InputManager.DragBegin -= OnMouseDragBegin;
            InputManager.MouseDragging -= OnMouseDragging;
            InputManager.MouseMoving -= OnMouseMoving;
            InputManager.KeyDown -= OnKeyDown;
            InputManager.KeyUp -= OnKeyUp;

            base.Unload();
        }

        private void SocketOnDisconnected(object sender, EventArgs e)
        {
            UIManager.Add(new MessageBoxGump(_settings.GameWindowX + _settings.GameWindowWidth / 2 - 100, _settings.GameWindowY + _settings.GameWindowHeight / 2 - 125 / 2, 200, 125, "Connection lost", (s) =>
            {
                s.Dispose();
                SceneManager.ChangeScene(ScenesType.Login);
            }));
        }

        public override void FixedUpdate(double totalMS, double frameMS)
        {
            if (!World.InGame)
                return;

            (Point minTile, Point maxTile, Vector2 minPixel, Vector2 maxPixel, Point offset, Point center, Point firstTile, int renderDimensions) = GetViewPort();
            //CheckIfUnderEntity(out int maxItemZ, out bool drawTerrain, out bool underSurface);
            //_maxZ = maxItemZ;
            //_drawTerrain = drawTerrain;
            UpdateMaxDrawZ();
            _renderListCount = 0;
            int minX = minTile.X;
            int minY = minTile.Y;
            int maxX = maxTile.X;
            int maxY = maxTile.Y;
            _offset = offset;
            _minPixel = minPixel;
            _maxPixel = maxPixel;
            _minTile = minTile;
            _maxTile = maxTile;

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
                         Tile tile =  World.Map.GetTile(x, y);

                        if (tile != null)
                        {
                            AddTileToRenderList(tile.ObjectsOnTiles, x, y, false, 150);
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

            CleaningResources();
            base.FixedUpdate(totalMS, frameMS);
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (!World.InGame)
                return;

            if (_renderTarget == null || _renderTarget.Width != (int) (_settings.GameWindowWidth * Scale) || _renderTarget.Height != (int) (_settings.GameWindowHeight * Scale))
            {
                _renderTarget?.Dispose();
                _renderTarget = new RenderTarget2D(Device, (int) (_settings.GameWindowWidth * Scale), (int) (_settings.GameWindowHeight * Scale), false, SurfaceFormat.Bgra5551, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents);
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

            if (IsMouseOverWorld)
            {
                _mouseOverList.MousePosition = _mousePicker.Position = MouseOverWorldPosition;
                _mousePicker.PickOnly = PickerType.PickEverything;
            }
            else if (SelectedObject != null) SelectedObject = null;

            _mouseOverList.Clear();

            if (_rightMousePressed)
                MoveCharacterByInputs();
            // ===================================
            World.Update(totalMS, frameMS);
            _staticManager.Update(totalMS, frameMS);
            _effectManager.Update(totalMS, frameMS);

            if (totalMS > _timePing)
            {
                NetClient.Socket.Send(PPing.Instance.Value);
                _timePing = (long) totalMS + 10000;
            }

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatch3D sb3D, SpriteBatchUI sbUI)
        {
            if (!World.InGame)
                return false;
            DrawWorld(sb3D);
            _mousePicker.UpdateOverObjects(_mouseOverList, _mouseOverList.MousePosition);

            return base.Draw(sb3D, sbUI);
        }

        private void DrawWorld(SpriteBatch3D sb3D)
        {
            sb3D.GraphicsDevice.Clear(Color.Black);
            sb3D.GraphicsDevice.SetRenderTarget(_renderTarget);
            sb3D.Begin();
            sb3D.EnableLight(true);
            sb3D.SetLightIntensity(World.Light.IsometricLevel);
            sb3D.SetLightDirection(World.Light.IsometricDirection);
            RenderedObjectsCount = 0;

            for (int i = 0; i < _renderListCount; i++)
            {
                GameObject obj = _renderList[i];

                if (obj.Z <= _maxGroundZ && obj.View.Draw(sb3D, obj.RealScreenPosition, _mouseOverList))
                    RenderedObjectsCount++;
            }

            // Draw in game overhead text messages
            OverheadManager.Draw(sb3D, _mouseOverList);
            sb3D.End();
            sb3D.EnableLight(false);
            sb3D.GraphicsDevice.SetRenderTarget(null);
        }

        private void CleaningResources()
        {
            Art.ClearUnusedTextures();
            IO.Resources.Gumps.ClearUnusedTextures();
            TextmapTextures.ClearUnusedTextures();
            Animations.ClearUnusedTextures();
            World.Map.ClearUnusedBlocks();
        }
    }
}