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
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Gumps.Controls.InGame;
using ClassicUO.Game.Gumps.UIGumps;
using ClassicUO.Game.Map;
using ClassicUO.Game.Views;
using ClassicUO.Input;
using ClassicUO.Interfaces;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Scenes
{
    public class GameScene : Scene
    {
        private RenderTarget2D _renderTarget;
        private DateTime _timePing;
#if !ORIONSORT
        private readonly List<DeferredEntity> _deferredToRemove = new List<DeferredEntity>();
#endif
        private MousePicker _mousePicker;
        private MouseOverList _mouseOverList;

        private bool _rightMousePressed;
        private WorldViewport _viewPortGump;
        private TopBarGump _topBarGump;
        private StaticManager _staticManager;
        private Settings _settings;

        //private static Hue _savedHue;
        private static GameObject _selectedObject;


        public static Hue MouseOverItemHue => 0x038;


        public GameScene() : base(ScenesType.Game)
        {
        }

        public int Scale { get; set; } = 1;
        public Texture2D ViewportTexture => _renderTarget;
        public Point MouseOverWorldPosition => new Point(InputManager.MousePosition.X - _viewPortGump.ScreenCoordinateX, InputManager.MousePosition.Y - _viewPortGump.ScreenCoordinateY);

        public GameObject SelectedObject
        {
            get => _selectedObject;
            set
            {
                if (_selectedObject == value)
                    return;

                //if (_selectedObject != null)
                //    _selectedObject.Hue = _savedHue;

                if (value == null)
                {
                    _selectedObject.View.IsSelected = false;
                    _selectedObject = null;
                    //_savedHue = 0;
                }
                else
                {
                    _selectedObject = value;
                    //_savedHue = _selectedObject.Hue;

                    if (Service.Get<Settings>().HighlightGameObjects)
                        _selectedObject.View.IsSelected = true;
                }
            }
        }

        public override void Load()
        {
            base.Load();

            _mousePicker = new MousePicker();
            _mouseOverList = new MouseOverList(_mousePicker);
            _staticManager = new StaticManager();

            UIManager.Add(new WorldViewportGump(this));
            UIManager.Add(_topBarGump = new TopBarGump(this));

            _viewPortGump = Service.Get<WorldViewport>();

            _settings = Service.Get<Settings>();

            GameActions.Initialize(PicupItemBegin);
        }


        public override void Unload()
        {
            _topBarGump.Dispose();
            Service.Unregister<GameScene>();

            _viewPortGump.Dispose();
            CleaningResources();
            base.Unload();
        }


        public override void FixedUpdate(double totalMS, double frameMS)
        {
#if ORIONSORT
            (Point minTile, Point maxTile, Vector2 minPixel, Vector2 maxPixel, Point offset, Point center, Point firstTile, int renderDimensions)
 = GetViewPort2();
            _renderListCount = 0;

            //if (_renderList.Count > 0)
            //    _renderList.Clear();

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
                        Tile tile = World.Map.GetTile(x, y);
                        if (tile != null)
                        {
                            var objects = (List<GameObject>)tile.ObjectsOnTiles;
                            AddTileToRenderList(objects, x, y, false, 150);
                        }

                        x++;
                        y--;

                    }
                }
            }

            _renderIndex++;

            if (_renderIndex >= 100)
                _renderIndex = 1;


#endif

            CleaningResources();
            base.FixedUpdate(totalMS, frameMS);
        }

        public override void Update(double totalMS, double frameMS)
        {
            World.Ticks = (long) totalMS;

            if (_renderTarget == null || _renderTarget.Width != _settings.GameWindowWidth / Scale || _renderTarget.Height != _settings.GameWindowHeight / Scale)
            {
                _renderTarget?.Dispose();
                _renderTarget = new RenderTarget2D(Device, _settings.GameWindowWidth / Scale, _settings.GameWindowHeight / Scale, false, SurfaceFormat.Bgra5551,
                    DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents);
            }


            // ============== INPUT ==============          
            HandleMouseActions();
            MouseHandler(frameMS);


            if (IsMouseOverWorld)
            {
                _mouseOverList.MousePosition = _mousePicker.Position = MouseOverWorldPosition;
                _mousePicker.PickOnly = PickerType.PickEverything;
            }
            else if (SelectedObject != null)
                SelectedObject = null;

            _mouseOverList.Clear();


            if (_rightMousePressed)
                MoveCharacterByInputs();
            // ===================================


            World.Update(totalMS, frameMS);
            _staticManager.Update(totalMS, frameMS);


            if (DateTime.Now > _timePing)
            {
                NetClient.Socket.Send(new PPing());
                _timePing = DateTime.Now.AddSeconds(10);
            }

            base.Update(totalMS, frameMS);
        }


        public override bool Draw(SpriteBatch3D sb3D, SpriteBatchUI sbUI)
        {
            DrawWorld(sb3D);

            _mousePicker.UpdateOverObjects(_mouseOverList, _mouseOverList.MousePosition);

            return base.Draw(sb3D, sbUI);
        }


        private static void CheckIfUnderEntity(out int maxItemZ, out bool drawTerrain, out bool underSurface)
        {
            maxItemZ = 255;
            drawTerrain = true;
            underSurface = false;

            Tile tile = World.Map.GetTile(World.Map.Center.X, World.Map.Center.Y);
            if (tile != null && tile.IsZUnderObjectOrGround(World.Player.Position.Z, out GameObject underObject,
                    out GameObject underGround))
            {
                drawTerrain = underGround == null;
                if (underObject != null)
                {
                    if (underObject is Item item)
                    {
                        if (TileData.IsRoof((long) item.ItemData.Flags))
                            maxItemZ = World.Player.Position.Z - World.Player.Position.Z % 20 + 20;
                        else if (TileData.IsSurface((long) item.ItemData.Flags) ||
                                 TileData.IsWall((long) item.ItemData.Flags) &&
                                 TileData.IsDoor((long) item.ItemData.Flags))
                            maxItemZ = item.Position.Z;
                        else
                        {
                            int z = World.Player.Position.Z + (item.ItemData.Height > 20 ? item.ItemData.Height : 20);
                            maxItemZ = z;
                        }
                    }
                    else if (underObject is Static sta)
                    {
                        if (TileData.IsRoof((long) sta.ItemData.Flags))
                            maxItemZ = World.Player.Position.Z - World.Player.Position.Z % 20 + 20;
                        else if (TileData.IsSurface((long) sta.ItemData.Flags) ||
                                 TileData.IsWall((long) sta.ItemData.Flags) &&
                                 TileData.IsDoor((long) sta.ItemData.Flags))
                            maxItemZ = sta.Position.Z;
                        else
                        {
                            int z = World.Player.Position.Z + (sta.ItemData.Height > 20 ? sta.ItemData.Height : 20);
                            maxItemZ = z;
                        }
                    }

                    if (underObject is Item i && TileData.IsRoof((long) i.ItemData.Flags) ||
                        underObject is Static s && TileData.IsRoof((long) s.ItemData.Flags))
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

        private static (Point firstTile, Vector2 renderOffset, Point renderDimensions) GetViewPort(int width, int height, int scale)
        {
            int off = Math.Abs(width / 44 - height / 44) % 3;


            Point renderDimensions = new Point
            {
                X = width / scale / 44 + 3,
                Y = height / scale / 44 + 6
            };

            int renderDimensionDiff = Math.Abs(renderDimensions.X - renderDimensions.Y);
            renderDimensionDiff -= renderDimensionDiff % 2;

            int firstZOffset = World.Player.Position.Z > 0
                ? (int) Math.Abs((World.Player.Position.Z + World.Player.Offset.Z / 4) / 11)
                : 0;

            Point firstTile = new Point
            {
                X = World.Player.Position.X - firstZOffset,
                Y = World.Player.Position.Y - renderDimensions.Y - firstZOffset
            };

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

            //Vector2 renderOffset = new Vector2
            //{
            //    X = (_graphics.PreferredBackBufferWidth / scale + renderDimensions.Y * 44) / 2 - 22f - (int)World.Player.Offset.X - (firstTile.X - firstTile.Y) * 22f + renderDimensionDiff * 22f,
            //    Y = _graphics.PreferredBackBufferHeight / scale / 2 - renderDimensions.Y * 44 / 2 + (World.Player.Position.Z + World.Player.Offset.Z / 4) * 4 - (int)World.Player.Offset.Y - (firstTile.X + firstTile.Y) * 22f - 22f - firstZOffset * 44f };

            Vector2 renderOffset = new Vector2();

            renderOffset.X = (width / scale + renderDimensions.Y * 44) / 2 - 22f;
            renderOffset.X -= (int) World.Player.Offset.X;
            renderOffset.X -= (firstTile.X - firstTile.Y) * 22f;
            renderOffset.X += renderDimensionDiff * 22f;

            renderOffset.Y = height / scale / 2 - renderDimensions.Y * 44 / 2;
            renderOffset.Y += (World.Player.Position.Z + World.Player.Offset.Z / 4) * 4;
            renderOffset.Y -= (int) World.Player.Offset.Y;
            renderOffset.Y -= (firstTile.X + firstTile.Y) * 22f;
            renderOffset.Y -= 22f;
            renderOffset.Y -= firstZOffset * 44f;

            return (firstTile, renderOffset, renderDimensions);
        }

        private void DrawWorld(SpriteBatch3D sb3D)
        {
            sb3D.Begin();
            sb3D.SetLightIntensity(World.Light.IsometricLevel);
            sb3D.SetLightDirection(World.Light.IsometricDirection);

            RenderedObjectsCount = 0;


#if ORIONSORT
            for (int i = 0; i < _renderListCount; i++)
            {
                var obj = _renderList[i];
                if (obj == null)
                    continue;

                int x = obj.Position.X;
                int y = obj.Position.Y;

                Vector3 isometricPosition = new Vector3((x - y) * 22 - _offset.X - 22, (x + y) * 22 - _offset.Y - 22, 0);

                obj.View.Draw(sb3D, isometricPosition, _mouseOverList);

            }

            //_renderList.Clear();
#else
            CheckIfUnderEntity(out int maxItemZ, out bool drawTerrain, out bool underSurface);
            (Point firstTile, Vector2 renderOffset, Point renderDimensions) = GetViewPort(_settings.GameWindowWidth, _settings.GameWindowHeight, Scale);

            ClearDeferredEntities();

            for (int y = 0; y < renderDimensions.Y * 2 + 11; y++)
            {
                Vector3 dp = new Vector3
                {
                    X = (firstTile.X - firstTile.Y + y % 2) * 22f + renderOffset.X,
                    Y = (firstTile.X + firstTile.Y + y) * 22f + renderOffset.Y
                };


                Point firstTileInRow = new Point(firstTile.X + (y + 1) / 2, firstTile.Y + y / 2);

                for (int x = 0; x < renderDimensions.X + 1; x++, dp.X -= 44f)
                {
                    int tileX = firstTileInRow.X - x;
                    int tileY = firstTileInRow.Y + x;

                    Tile tile = World.Map.GetTile(tileX, tileY);
                    if (tile != null)
                    {
                        IReadOnlyList<GameObject> objects = tile.ObjectsOnTiles;
                        bool draw = true;

                        for (int k = 0; k < objects.Count; k++)
                        {
                            GameObject obj = objects[k];

                            if (obj is DeferredEntity d)
                                _deferredToRemove.Add(d);

                            if (!drawTerrain)
                            {
                                if (obj is Tile || obj.Position.Z > tile.Position.Z)
                                    draw = false;
                            }

                            if ((obj.Position.Z >= maxItemZ
                                 || maxItemZ != 255 && obj is IDynamicItem dyn &&
                                 TileData.IsRoof((long) dyn.ItemData.Flags))
                                && !(obj is Tile))
                                continue;

                            if (draw && obj.View.Draw(sb3D, dp, _mouseOverList))
                                RenderedObjectsCount++;
                        }

                        ClearDeferredEntities();
                    }
                }
            }
#endif
            // Draw in game overhead text messages
            OverheadManager.Draw(sb3D, _mouseOverList);

            sb3D.GraphicsDevice.SetRenderTarget(_renderTarget);
            sb3D.GraphicsDevice.Clear(Color.Black);
            sb3D.End(true);
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

#if !ORIONSORT
        private void ClearDeferredEntities()
        {
            if (_deferredToRemove.Count > 0)
            {
                foreach (DeferredEntity def in _deferredToRemove)
                {
                    def.Reset();
                    def.AssociatedTile.RemoveGameObject(def);
                }

                _deferredToRemove.Clear();
            }
        }
#endif

        public bool IsMouseOverUI => UIManager.IsMouseOverUI && !(UIManager.MouseOverControl is WorldViewport);
        public bool IsMouseOverWorld => UIManager.IsMouseOverUI && UIManager.MouseOverControl is WorldViewport;


        private void HandleMouseActions()
        {
            SelectedObject = null;

            if (IsHoldingItem && InputManager.HandleMouseEvent(MouseEvent.Up, MouseButton.Left))
            {
                if (IsMouseOverUI)
                {
                    GumpControl target = UIManager.MouseOverControl;

                    // TODO: ITEMGUMPLING
                }
                else if (IsMouseOverWorld)
                {
                    GameObject obj = _mousePicker.MouseOverObject;

                    if (obj != null)
                    {
                        switch (obj)
                        {
                            case Mobile mobile:
                                MergeHeldItem(mobile);
                                break;
                            case IDynamicItem dyn:
                                if (dyn is Item item)
                                {
                                    if (item.IsCorpse)
                                        MergeHeldItem(item);
                                    else
                                    {
                                        SelectedObject = item;

                                        if (item.Graphic == HeldItem.Graphic && HeldItem is IDynamicItem dyn1 &&
                                            TileData.IsStackable((long) dyn1.ItemData.Flags))
                                            MergeHeldItem(item);
                                        else
                                        {
                                            DropHeldItemToWorld(obj.Position.X, obj.Position.Y,
                                                (sbyte) (obj.Position.Z + dyn.ItemData.Height));
                                        }
                                    }
                                }
                                else
                                {
                                    DropHeldItemToWorld(obj.Position.X, obj.Position.Y,
                                        (sbyte) (obj.Position.Z + dyn.ItemData.Height));
                                }

                                break;
                            case Tile tile:
                                DropHeldItemToWorld(obj.Position);
                                break;
                            default:
                                return;
                        }
                    }
                }
            }


            if (SelectedObject == null) SelectedObject = _mousePicker.MouseOverObject;
        }

        private void MouseHandler(double frameMS)
        {
            if (!IsMouseOverWorld)
            {
                if (_rightMousePressed)
                    _rightMousePressed = false;
                return;
            }

            foreach (InputMouseEvent e in InputManager.GetMouseEvents())
            {
                switch (e.Button)
                {
                    case MouseButton.Right:
                        _rightMousePressed = e.EventType == MouseEvent.Down;
                        e.IsHandled = true;
                        break;
                    case MouseButton.Left:

                        if (e.EventType == MouseEvent.Click)
                        {
                            EnqueueSingleClick(e, _mousePicker.MouseOverObject, _mousePicker.MouseOverObjectPoint);
                            continue;
                        }

                        if (e.EventType == MouseEvent.DoubleClick)
                            ClearQueuedClicks();

                        DoMouseButton(e, _mousePicker.MouseOverObject, _mousePicker.MouseOverObjectPoint);
                        break;
                }
            }

            CheckForQueuedClicks(frameMS);
        }

        private void DoMouseButton(InputMouseEvent e, GameObject obj, Point point)
        {
            switch (e.EventType)
            {
                case MouseEvent.Down:
                {
                    _dragginObject = obj;
                    _dragOffset = point;
                }
                    break;
                case MouseEvent.Click:
                {
                    if (obj is Static st)
                    {
                        if (string.IsNullOrEmpty(st.Name))
                            TileData.StaticData[st.Graphic].Name = Cliloc.GetString(1020000 + st.Graphic);

                        obj.AddGameText(MessageType.Label, st.Name, 3, 0, false);

                        _staticManager.Add(st);
                    }
                    else if (obj is Entity entity) GameActions.SingleClick(entity);
                }
                    break;
                case MouseEvent.DoubleClick:
                {
                    if (obj is Item item)
                        GameActions.DoubleClick(item);
                    else if (obj is Mobile mob)
                    {
                        //TODO: attack request also
                        if (World.Player.InWarMode)
                        {
                        }
                        else
                            GameActions.DoubleClick(mob);
                    }
                }
                    break;
                case MouseEvent.DragBegin:
                {
                    if (obj is Mobile mobile)
                    {
                        // get the lifebar
                    }
                    else if (obj is Item item) PicupItemBegin(item, _dragOffset.X, _dragOffset.Y);
                }
                    break;
            }

            e.IsHandled = true;
        }

        private GameObject _dragginObject;
        private Point _dragOffset, _heldOffset;
        private Item _heldItem;

        public Item HeldItem
        {
            get => _heldItem;
            set
            {
                if (value == null && _heldItem != null)
                    UIManager.RemoveInputBlocker(this);
                else if (value != null && _heldItem == null) UIManager.RemoveInputBlocker(this);

                _heldItem = value;
            }
        }

        public bool IsHoldingItem => HeldItem != null;

        private void MergeHeldItem(Entity entity)
        {
            GameActions.DropDown(HeldItem, Position.Invalid, entity.Serial);
            ClearHolding();
        }

        private void PicupItemBegin(Item item, int x, int y, int? amount = null)
        {
            // TODO: AMOUNT CHECK

            PickupItemDirectly(item, x, y, amount ?? item.Amount);
        }

        private void PickupItemDirectly(Item item, int x, int y, int amount)
        {
            if (!item.IsPickable)
                return;

            if (item.Container.IsValid)
            {
                Entity entity = World.Get(item.Container);
                item.Position = entity.Position;
                entity.Items.Remove(item);
            }

            item.Amount = (ushort) amount;
            HeldItem = item;
            _heldOffset = new Point(x, y);

            NetClient.Socket.Send(new PPickUpRequest(item, (ushort) amount));
        }

        private void DropHeldItemToWorld(Position position)
            => DropHeldItemToWorld(position.X, position.Y, position.Z);

        private void DropHeldItemToWorld(ushort x, ushort y, sbyte z)
        {
            GameObject obj = SelectedObject;
            Serial serial;
            if (obj is Item item && TileData.IsContainer((long) item.ItemData.Flags))
            {
                serial = item;
                x = y = 0xFFFF;
                z = 0;
            }
            else
                serial = Serial.MinusOne;

            GameActions.DropDown(HeldItem.Serial, x, y, z, serial);
            ClearHolding();
        }

        private void DropHeldItemToContainer(Item container)
        {
            DropHeldItemToContainer(container, 0, 0);
        }

        private void DropHeldItemToContainer(Item container, ushort x, ushort y)
        {
            GameActions.DropDown(HeldItem.Serial, x, y, 0, container);
            ClearHolding();
        }

        private void WearHeldItem()
        {
            GameActions.Equip(HeldItem, Layer.Invalid);
            ClearHolding();
        }

        private void ClearHolding() => HeldItem = null;


        private GameObject _queuedObject;
        private Point _queuedPosition;
        private InputMouseEvent _queuedEvent;
        private double _dequeueAt;
        private bool _inqueue;

        private void EnqueueSingleClick(InputMouseEvent e, GameObject obj, Point point)
        {
            _inqueue = true;
            _queuedObject = obj;
            _queuedPosition = point;
            _dequeueAt = 200f;
            _queuedEvent = e;
        }

        private void CheckForQueuedClicks(double framMS)
        {
            if (_inqueue)
            {
                _dequeueAt -= framMS;
                if (_dequeueAt <= 0d)
                {
                    DoMouseButton(_queuedEvent, _queuedObject, _queuedPosition);
                    ClearQueuedClicks();
                }
            }
        }

        private void ClearQueuedClicks()
        {
            _inqueue = false;
            _queuedEvent = null;
            _queuedObject = null;
        }


        private void MoveCharacterByInputs()
        {
            if (World.InGame)
            {
                Point center = new Point(_settings.GameWindowX + _settings.GameWindowWidth / 2,
                    _settings.GameWindowY + _settings.GameWindowHeight / 2);

                Direction direction = DirectionHelper.DirectionFromPoints(center, InputManager.MousePosition);

                World.Player.Walk(direction, true);
            }
        }


#if ORIONSORT
        private int _renderIndex = 1;
        private int _renderListCount = 0;
        private GameObject[] _renderList = new GameObject[2000];
        private Point _offset, _maxTile, _minTile;
        private Vector2 _minPixel, _maxPixel;

        private void AddTileToRenderList(List<GameObject> objList, int worldX, int worldY, bool useObjectHandles, int maxZ)
        {
            for (int i = 0; i < objList.Count; i++)
            {
                var obj = objList[i];

                if (obj.CurrentRenderIndex == _renderIndex || obj.IsDisposed)
                    continue;

                obj.UseInRender = 0xFF;
                int drawX = (obj.Position.X - obj.Position.Y) * 22 - _offset.X;
                int drawY = ((obj.Position.X + obj.Position.Y) * 22 - (obj.Position.Z * 4)) - _offset.Y;

                if (drawX < _minPixel.X || drawX > _maxPixel.X)
                    break;

                int z = obj.Position.Z;
                int maxObjectZ = obj.PriorityZ;

                if (obj is Mobile)
                    maxObjectZ += 16;
                else if (obj is IDynamicItem dyn)
                    maxObjectZ += dyn.ItemData.Height;


                if (maxObjectZ > maxZ)
                    break;

                obj.CurrentRenderIndex = _renderIndex;

                //if (obj is IDynamicItem dyn1 && TileData.IsInternal((long)dyn1.ItemData.Flags))
                //    continue;
                //else if (!(obj is Tile) && z >= )

                int testMinZ = drawY + (z * 4);
                int testMaxZ = drawY;


                if (obj is Tile t && t.IsStretched)
                    testMinZ -= (t.MinZ * 4);
                else
                    testMinZ = testMaxZ;

                if (testMinZ < _minPixel.Y || testMaxZ > _maxPixel.Y)
                    continue;

                if (obj is Mobile mob)
                    AddOffsetCharacterTileToRenderList(mob, useObjectHandles);
                else if (obj is Item item && item.IsCorpse)
                    AddOffsetCharacterTileToRenderList(item, useObjectHandles);


                if (_renderListCount >= _renderList.Length)
                {
                    int newsize = _renderList.Length + 1000;

                    Array.Resize(ref _renderList, newsize);
                }


                _renderList[_renderListCount] = obj;

                obj.UseInRender = (byte)_renderIndex;
                _renderListCount++;
            }
        }


        private readonly int[,] _coordinates = new int[8, 2];

        private void AddOffsetCharacterTileToRenderList(Entity entity, bool useObjectHandles)
        {
            int charX = entity.Position.X;
            int charY = entity.Position.Y;

            Mobile mob = entity.Serial.IsMobile ? World.Mobiles.Get(entity) : null;
            int dropMaxZIndex = -1;
            if (mob != null)
            {
                if (mob.Steps.Count > 0 && (mob.Steps.Back().Direction & 7) == 2)
                    dropMaxZIndex = 0;
            }

            _coordinates[0, 0] = charX + 1;
            _coordinates[0, 1] = charY - 1;
            _coordinates[1, 0] = charX + 1;
            _coordinates[1, 1] = charY - 2;
            _coordinates[2, 0] = charX + 2;
            _coordinates[2, 1] = charY - 2;
            _coordinates[3, 0] = charX - 1;
            _coordinates[3, 1] = charY + 2;
            _coordinates[4, 0] = charX;
            _coordinates[4, 1] = charY + 1;
            _coordinates[5, 0] = charX + 1;
            _coordinates[5, 1] = charY;
            _coordinates[6, 0] = charX + 2;
            _coordinates[6, 1] = charY - 1;
            _coordinates[7, 0] = charX + 1;
            _coordinates[7, 1] = charY + 1;


            int maxZ = entity.PriorityZ;

            for (int i = 0; i < 8; i++)
            {
                int x = _coordinates[i, 0];
                int y = _coordinates[i, 1];

                if (x < _minTile.X || x > _maxTile.X || y < _minTile.Y || y > _maxTile.Y)
                    continue;

                Tile tile = World.Map.GetTile(x, y);

                int currentMaxZ = maxZ;

                if (i == dropMaxZIndex)
                    currentMaxZ += 20;

                var list = (List<GameObject>)tile.ObjectsOnTiles;
                AddTileToRenderList(list, x, y, useObjectHandles, currentMaxZ);
            }
        }


        private (Point, Point, Vector2, Vector2, Point, Point, Point, int) GetViewPort2()
        {
            float scale = 1;

            int winGamePosX = 0;
            int winGamePosY = 0;

            int winGameWidth = _settings.GameWindowWidth;
            int winGameHeight = _settings.GameWindowHeight;

            int winGameCenterX = winGamePosX + (winGameWidth / 2);
            int winGameCenterY = winGamePosY + winGameHeight / 2 + World.Player.Position.Z * 4;

            winGameCenterX -= (int)World.Player.Offset.X;
            winGameCenterY -= (int)(World.Player.Offset.Y - World.Player.Offset.Z);

            int winDrawOffsetX = (World.Player.Position.X - World.Player.Position.Y) * 22 - winGameCenterX;
            int winDrawOffsetY = (World.Player.Position.X + World.Player.Position.Y) * 22 - winGameCenterY;

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


            int width = (int)((winGameWidth / 44 + 1) * scale);
            int height = (int)((winGameHeight / 44 + 1) * scale);

            if (width < height)
                width = height;
            else
                height = width;

            int realMinRangeX = World.Player.Position.X - width;
            if (realMinRangeX < 0)
                realMinRangeX = 0;
            int realMaxRangeX = World.Player.Position.X + width;
            if (realMaxRangeX >= IO.Resources.Map.MapsDefaultSize[World.Map.Index][0])
                realMaxRangeX = IO.Resources.Map.MapsDefaultSize[World.Map.Index][0];

            int realMinRangeY = World.Player.Position.Y - height;
            if (realMinRangeY < 0)
                realMinRangeY = 0;
            int realMaxRangeY = World.Player.Position.Y + height;
            if (realMaxRangeY >= IO.Resources.Map.MapsDefaultSize[World.Map.Index][1])
                realMaxRangeY = IO.Resources.Map.MapsDefaultSize[World.Map.Index][1];

            int minBlockX = realMinRangeX / 8 - 1;
            int minBlockY = realMinRangeY / 8 - 1;
            int maxBlockX = realMaxRangeX / 8 + 1;
            int maxBlockY = realMaxRangeY / 8 + 1;

            if (minBlockX < 0)
                minBlockX = 0;
            if (minBlockY < 0)
                minBlockY = 0;
            if (maxBlockX >= IO.Resources.Map.MapsDefaultSize[World.Map.Index][0])
                maxBlockX = IO.Resources.Map.MapsDefaultSize[World.Map.Index][0] - 1;
            if (maxBlockY >= IO.Resources.Map.MapsDefaultSize[World.Map.Index][1])
                maxBlockY = IO.Resources.Map.MapsDefaultSize[World.Map.Index][1] - 1;

            int drawOffset = (int)(scale * 40.0f);

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
#endif
    }
}