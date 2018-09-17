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
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls.InGame;
using ClassicUO.Game.Map;
using ClassicUO.Input;
using ClassicUO.Interfaces;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.Scenes
{
    public class GameScene : Scene
    {
        private RenderTarget2D _renderTarget;
        private DateTime _timePing;
        private readonly List<DeferredEntity> _deferredToRemove = new List<DeferredEntity>();
        private MousePicker<GameObject> _mousePicker;
        private MouseOverList<GameObject> _mouseOverList;      

        private bool _rightMousePressed;
        private WorldViewportGump _viewPortGump;

        private static Hue _savedHue;
        private static GameObject _selectedObject;

        


        public GameScene()
        {
            
        }

        public int Width { get; set; } = 800;
        public int Height { get; set; } = 600;
        public int Scale { get; set; } = 1;
        public Texture2D ViewportTexture => _renderTarget;

        public static GameObject SelectedObject
        {
            get => _selectedObject;
            set
            {
                if (_selectedObject == value)
                    return;

                if (_selectedObject != null)
                {
                    _selectedObject.Hue = _savedHue;
                }

                if (value == null)
                {
                    _selectedObject = null;
                    _savedHue = 0;
                }
                else
                {
                    _selectedObject = value;
                    _savedHue = _selectedObject.Hue;
                    _selectedObject.Hue = 24;
                }
            }
        }

        private bool _ADDED;

        public override void Load()
        {
            base.Load();

            _mousePicker = new MousePicker<GameObject>();
            _mouseOverList = new MouseOverList<GameObject>(_mousePicker);

            UIManager.Add(_viewPortGump = new WorldViewportGump(this));
        }


        public override void Unload()
        {
            _viewPortGump.Dispose();
            CleaningResources();
            base.Unload();
        }


        public override void FixedUpdate(double totalMS, double frameMS)
        {
            CleaningResources();
            base.FixedUpdate(totalMS, frameMS);
        }

        public override void Update(double totalMS, double frameMS)
        {
           
            //if (World.Map != null)
            //{
            //    if (!_ADDED)
            //    {
            //        UIManager.Add(new Gumps.Controls.InGame.MapGump());
            //        _ADDED = true;
            //    }
            //}

            World.Ticks = (long)totalMS;

            if (_renderTarget == null || _renderTarget.Width != Width / Scale || _renderTarget.Height != Height / Scale)
            {
                _renderTarget?.Dispose();
                _renderTarget = new RenderTarget2D(Device, Width / Scale, Height / Scale, false, SurfaceFormat.Bgra5551, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents);
            }

            MouseHandler();
            if (_rightMousePressed)
                MoveCharacterByInputs();

            World.Update(totalMS, frameMS);

            if (DateTime.Now > _timePing)
            {
                NetClient.Socket.Send(new PPing());
                _timePing = DateTime.Now.AddSeconds(10);
            }

            _mouseOverList.MousePosition = _mousePicker.Position = InputManager.MousePosition;
            _mousePicker.PickOnly = PickerType.PickEverything;
            _mouseOverList.Clear();

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatch3D sb3D, SpriteBatchUI sbUI)
        {
            DrawWorld(sb3D);
            //DrawInterfaces(sb3D, sbUI);

            _mousePicker.UpdateOverObjects(_mouseOverList, _mouseOverList.MousePosition);
            SelectedObject = _mousePicker.MouseOverObject;

            return base.Draw(sb3D, sbUI);
        }



        private void CheckIfUnderEntity(out int maxItemZ, out bool drawTerrain, out bool underSurface)
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

        private (Point firstTile, Vector2 renderOffset, Point renderDimensions) GetViewPort(int width, int height, int scale = 1)
        {
            Point renderDimensions = new Point
            {
                X = width / scale / 44 + 3,
                Y = height / scale / 44 + 6
            };

            int renderDimensionDiff = Math.Abs(renderDimensions.X - renderDimensions.Y);
            renderDimensionDiff -= renderDimensionDiff % 2;

            int firstZOffset = World.Player.Position.Z > 0 ? (int)Math.Abs((World.Player.Position.Z + World.Player.Offset.Z / 4) / 11) : 0;

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

            renderOffset.X = (((width / scale) + (renderDimensions.Y * 44)) / 2) - 22f;
            renderOffset.X -= (int)World.Player.Offset.X;
            renderOffset.X -= (firstTile.X - firstTile.Y) * 22f;
            renderOffset.X += renderDimensionDiff * 22f;

            renderOffset.Y = (height / scale) / 2 - (renderDimensions.Y * 44 / 2);
            renderOffset.Y += (World.Player.Position.Z + World.Player.Offset.Z / 4) * 4;
            renderOffset.Y -= (int)World.Player.Offset.Y;
            renderOffset.Y -= (firstTile.X + firstTile.Y) * 22f;
            renderOffset.Y -= 22f;
            renderOffset.Y -= firstZOffset * 44f;

            return (firstTile, renderOffset, renderDimensions);
        }

        private void DrawWorld(SpriteBatch3D sb3D)
        {
            CheckIfUnderEntity(out int maxItemZ, out bool drawTerrain, out bool underSurface);
            (Point firstTile, Vector2 renderOffset, Point renderDimensions) = GetViewPort(Width, Height, Scale);

            sb3D.Begin();
            sb3D.SetLightIntensity(World.Light.IsometricLevel);
            sb3D.SetLightDirection(World.Light.IsometricDirection);

            RenderedObjectsCount = 0;

            ClearDeferredEntities();

            for (int y = 0; y < renderDimensions.Y * 2 + 11; y++)
            {

                Vector3 dp = new Vector3
                {
                    X = (firstTile.X - firstTile.Y + (y % 2)) * 22f + renderOffset.X,
                    Y = (firstTile.X + firstTile.Y + y) * 22f + renderOffset.Y
                };


                Point firstTileInRow = new Point(firstTile.X + ((y + 1) / 2), firstTile.Y + (y / 2));

                for (int x = 0; x < renderDimensions.X + 1; x++, dp.X -= 44f)
                {
                    int tileX = firstTileInRow.X - x;
                    int tileY = firstTileInRow.Y + x;

                    Tile tile = World.Map.GetTile(tileX, tileY);
                    if (tile != null)
                    {
                        var objects = tile.ObjectsOnTiles;
                        bool draw = true;
                        for (int k = 0; k < objects.Count; k++)
                        {
                            var obj = objects[k];

                            if (obj is DeferredEntity d)
                                _deferredToRemove.Add(d);

                            if (!drawTerrain)
                            {
                                if (obj is Tile || obj.Position.Z > tile.Position.Z)
                                    draw = false;
                            }

                            if ((obj.Position.Z >= maxItemZ
                                || maxItemZ != 255 && obj is IDynamicItem dyn && TileData.IsRoof((long)dyn.ItemData.Flags))
                                && !(obj is Tile))
                                continue;

                            var view = obj.View;


                            if (draw && view.Draw(sb3D, dp, _mouseOverList))
                                RenderedObjectsCount++;
                        }

                        ClearDeferredEntities();
                    }
                }
            }

            // Draw in game overhead text messages
            OverheadManager.Draw(sb3D, _mouseOverList);

            sb3D.GraphicsDevice.SetRenderTarget(_renderTarget);
            sb3D.GraphicsDevice.Clear(Color.Black);
            sb3D.End(true);
            sb3D.GraphicsDevice.SetRenderTarget(null);
        }

        //private void DrawInterfaces(SpriteBatch3D sb3D, SpriteBatchUI sbUI)
        //{
        //    sbUI.GraphicsDevice.Clear(Color.Transparent);
        //    sbUI.Begin();

        //    // Draw world
        //    sbUI.Draw2D(_renderTarget, new Rectangle(0, 0, Width, Height), Vector3.Zero);

            

        //    // draw UI
        //    UIManager.Draw(sbUI);


           

          
        //    sbUI.End();
        //}

        private void CleaningResources()
        {
            IO.Resources.Art.ClearUnusedTextures();
            IO.Resources.Gumps.ClearUnusedTextures();
            IO.Resources.TextmapTextures.ClearUnusedTextures();
            IO.Resources.Animations.ClearUnusedTextures();
            World.Map.ClearUnusedBlocks();
        }

        private void ClearDeferredEntities()
        {
            if (_deferredToRemove.Count > 0)
            {
                foreach (var def in _deferredToRemove)
                {
                    def.Reset();
                    def.AssociatedTile.RemoveGameObject(def);
                }
                _deferredToRemove.Clear();
            }
            
        }

        private void MouseHandler()
        {      
            foreach (var e in InputManager.GetMouseEvents())
            {
                if (e.Button == MouseButton.Right)
                    _rightMousePressed = e.EventType == MouseEvent.Down;
            }
        }

        private void MoveCharacterByInputs()
        {
            if (World.InGame)
            {
                Point center = new Point(Width / 2, Height / 2);

                Direction direction = DirectionHelper.DirectionFromPoints(center, InputManager.MousePosition);

                World.Player.Walk(direction, true);
            }
        }
    }
}