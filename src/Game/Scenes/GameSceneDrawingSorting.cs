#region license
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
using System.Collections.Generic;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;
using ClassicUO.Interfaces;
using ClassicUO.IO.Resources;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Scenes
{
    partial class GameScene
    {
        private int _oldPlayerX, _oldPlayerY, _oldPlayerZ;
        private sbyte _maxGroundZ;
        private bool _noDrawRoofs;

        private void UpdateMaxDrawZ()
        {
            int playerX = World.Player.X;
            int playerY = World.Player.Y;
            int playerZ = World.Player.Z;

            if (playerX == _oldPlayerX && playerY == _oldPlayerY && playerZ == _oldPlayerZ)
                return;
            _oldPlayerX = playerX;
            _oldPlayerY = playerY;
            _oldPlayerZ = playerZ;

            sbyte maxGroundZ = 127;
            _maxGroundZ = 127;
            _maxZ = 127;
            _noDrawRoofs = false;
            int bx = playerX;
            int by = playerY;
            Tile tile = World.Map.GetTile(bx, by);

            if (tile != null)
            {
                int pz14 = playerZ + 14;
                int pz16 = playerZ + 16;
                var objects = tile.ObjectsOnTiles;

                for (int i = 0; i < objects.Count; i++)
                {
                    GameObject obj = objects[i];
                    sbyte tileZ = obj.Z;

                    if (obj is Land)
                    {
                        if (pz16 <= tileZ)
                        {
                            maxGroundZ = (sbyte) pz16;
                            _maxGroundZ = (sbyte) pz16;
                            _maxZ = _maxGroundZ;

                            break;
                        }

                        continue;
                    }

                    if (obj is Mobile)
                        continue;

                    if (!(obj is Static) && obj is Item item && !item.IsMulti)
                        continue;

                    if (tileZ > pz14 && _maxZ > tileZ)
                    {
                        if (obj is IDynamicItem st && (st.ItemData.Flags & 0x20004) == 0 && (!TileData.IsRoof(st.ItemData.Flags) || TileData.IsSurface( st.ItemData.Flags)))
                        {
                            _maxZ = tileZ;
                            _noDrawRoofs = true;
                        }
                    }
                }

                int tempZ = _maxZ;
                _maxGroundZ = (sbyte) _maxZ;
                playerX++;
                playerY++;
                bx = playerX;
                by = playerY;
                tile = World.Map.GetTile(bx, by);

                if (tile !=null)
                {
                    objects = tile.ObjectsOnTiles;

                    for (int i = 0; i < objects.Count; i++)
                    {
                        GameObject obj = objects[i];

                        if (!(obj is Static) && obj is Item it && !it.IsMulti)
                            continue;

                        if (obj is Mobile)
                            continue;
                        sbyte tileZ = obj.Z;

                        if (tileZ > pz14 && _maxZ > tileZ)
                        {
                            if (obj is IDynamicItem dyn2 && (dyn2.ItemData.Flags & 0x204) == 0 && TileData.IsRoof( dyn2.ItemData.Flags))
                            {
                                _maxZ = tileZ;
                                World.Map.ClearBockAccess();
                                _maxGroundZ = World.Map.CalculateNearZ(tileZ, playerX, playerY, tileZ);
                                _noDrawRoofs = true;
                            }
                        }
                    }

                    tempZ = _maxGroundZ;
                }

                _maxZ = _maxGroundZ;

                if (tempZ < pz16)
                {
                    _maxZ = pz16;
                    _maxGroundZ = (sbyte) pz16;
                }

                _maxGroundZ = maxGroundZ;
            }
        }

        private int _renderIndex = 1;
        private int _renderListCount;
        private GameObject[] _renderList = new GameObject[2000];
        private Point _offset, _maxTile, _minTile;
        private Vector2 _minPixel, _maxPixel;
        private int _maxZ;
        private bool _updateDrawPosition;

        private void AddTileToRenderList(List<GameObject> objList, int worldX, int worldY, bool useObjectHandles, int maxZ)
        {
            for (int i = 0; i < objList.Count; i++)
            {
                GameObject obj = objList[i];

                if (obj.CurrentRenderIndex == _renderIndex || obj.IsDisposed)
                    continue;

                if (_updateDrawPosition && obj.CurrentRenderIndex != _renderIndex || obj.IsPositionChanged)
                    obj.UpdateRealScreenPosition(_offset);
                obj.UseInRender = 0xFF;
                int drawX = (int) obj.RealScreenPosition.X;
                int drawY = (int) obj.RealScreenPosition.Y;

                if (drawX < _minPixel.X || drawX > _maxPixel.X)
                    break;
                int z = obj.Z;
                int maxObjectZ = obj.PriorityZ;

                switch (obj)
                {
                    case Mobile _:
                        maxObjectZ += 16;

                        break;
                    case IDynamicItem dyn:

                        if (_noDrawRoofs && TileData.IsRoof( dyn.ItemData.Flags)) continue;
                        maxObjectZ += dyn.ItemData.Height;

                        break;
                }

                if (maxObjectZ > maxZ)
                    break;
                obj.CurrentRenderIndex = _renderIndex;

                if (obj.Graphic != 0x2006 && obj is IDynamicItem dyn2 && TileData.IsInternal( dyn2.ItemData.Flags))
                    continue;

                if (!(obj is Land) && z >= _maxZ)
                    continue;
                int testMinZ = drawY + z * 4;
                int testMaxZ = drawY;

                if (obj is Land t && t.IsStretched)
                    testMinZ -= t.MinZ * 4;
                else
                    testMinZ = testMaxZ;

                if (testMinZ < _minPixel.Y || testMaxZ > _maxPixel.Y)
                    continue;

                switch (obj)
                {
                    case Mobile mob:
                        AddOffsetCharacterTileToRenderList(mob, useObjectHandles);

                        break;
                    case Item item when item.IsCorpse:
                        AddOffsetCharacterTileToRenderList(item, useObjectHandles);

                        break;
                }

                if (_renderListCount >= _renderList.Length)
                {
                    int newsize = _renderList.Length + 1000;
                    Array.Resize(ref _renderList, newsize);
                }

                _renderList[_renderListCount] = obj;
                obj.UseInRender = (byte) _renderIndex;
                _renderListCount++;
            }
        }

        private void AddOffsetCharacterTileToRenderList(Entity entity, bool useObjectHandles)
        {
            int charX = entity.X;
            int charY = entity.Y;
            Mobile mob = World.Mobiles.Get(entity);
            int dropMaxZIndex = -1;

            if (mob != null && mob.IsMoving && mob.Steps.Back().Direction == 2)
                dropMaxZIndex = 0;
            int[,] coordinates = new int[8, 2];
            coordinates[0, 0] = charX + 1;
            coordinates[0, 1] = charY - 1;
            coordinates[1, 0] = charX + 1;
            coordinates[1, 1] = charY - 2;
            coordinates[2, 0] = charX + 2;
            coordinates[2, 1] = charY - 2;
            coordinates[3, 0] = charX - 1;
            coordinates[3, 1] = charY + 2;
            coordinates[4, 0] = charX;
            coordinates[4, 1] = charY + 1;
            coordinates[5, 0] = charX + 1;
            coordinates[5, 1] = charY;
            coordinates[6, 0] = charX + 2;
            coordinates[6, 1] = charY - 1;
            coordinates[7, 0] = charX + 1;
            coordinates[7, 1] = charY + 1;
            int maxZ = entity.PriorityZ;

            for (int i = 0; i < 8; i++)
            {
                int x = coordinates[i, 0];
                int y = coordinates[i, 1];

                if (x < _minTile.X || x > _maxTile.X || y < _minTile.Y || y > _maxTile.Y)
                    continue;
                 Tile tile =  World.Map.GetTile(x, y);

                if (tile == null)
                    continue;
                int currentMaxZ = maxZ;

                if (i == dropMaxZIndex)
                    currentMaxZ += 20;
                AddTileToRenderList(tile.ObjectsOnTiles, x, y, useObjectHandles, currentMaxZ);
            }
        }

        public int ScaledOffsetX { get; private set; }

        public int ScaledOffsetY { get; private set; }

        public int ScaledOffsetW { get; private set; }

        public int ScaledOffsetH { get; private set; }

        private (Point, Point, Vector2, Vector2, Point, Point, Point, int) GetViewPort()
        {
            int oldDrawOffsetX = _offset.X;
            int oldDrawOffsetY = _offset.Y;
            int winGamePosX = 0;
            int winGamePosY = 0;
            int winGameWidth = _settings.GameWindowWidth;
            int winGameHeight = _settings.GameWindowHeight;
            int winGameCenterX = winGamePosX + winGameWidth / 2;
            int winGameCenterY = winGamePosY + winGameHeight / 2 + World.Player.Position.Z * 4;
            winGameCenterX -= (int) World.Player.Offset.X;
            winGameCenterY -= (int) (World.Player.Offset.Y - World.Player.Offset.Z);
            int winDrawOffsetX = (World.Player.Position.X - World.Player.Position.Y) * 22 - winGameCenterX;
            int winDrawOffsetY = (World.Player.Position.X + World.Player.Position.Y) * 22 - winGameCenterY;
            double left = winGamePosX;
            double right = winGameWidth + left;
            double top = winGamePosY;
            double bottom = winGameHeight + top;
            double newRight = right * Scale;
            double newBottom = bottom * Scale;
            int winGameScaledOffsetX = (int) (left * Scale - (newRight - right));
            int winGameScaledOffsetY = (int) (top * Scale - (newBottom - bottom));
            int winGameScaledWidth = (int) (newRight - winGameScaledOffsetX);
            int winGameScaledHeight = (int) (newBottom - winGameScaledOffsetY);
            int width = (int) ((winGameWidth / 44 + 1) * Scale);
            int height = (int) ((winGameHeight / 44 + 1) * Scale);
            ScaledOffsetX = winGameScaledOffsetX;
            ScaledOffsetY = winGameScaledOffsetY;
            ScaledOffsetW = winGameScaledWidth;
            ScaledOffsetH = winGameScaledHeight;
            winDrawOffsetX += winGameScaledOffsetX / 2;
            winDrawOffsetY += winGameScaledOffsetY / 2;

            if (width < height)
                width = height;
            else
                height = width;
            int realMinRangeX = World.Player.Position.X - width;

            if (realMinRangeX < 0)
                realMinRangeX = 0;
            int realMaxRangeX = World.Player.Position.X + width;

            //if (realMaxRangeX >= IO.Resources.Map.MapsDefaultSize[World.Map.Index][0])
            //    realMaxRangeX = IO.Resources.Map.MapsDefaultSize[World.Map.Index][0];
            int realMinRangeY = World.Player.Position.Y - height;

            if (realMinRangeY < 0)
                realMinRangeY = 0;
            int realMaxRangeY = World.Player.Position.Y + height;

            //if (realMaxRangeY >= IO.Resources.Map.MapsDefaultSize[World.Map.Index][1])
            //    realMaxRangeY = IO.Resources.Map.MapsDefaultSize[World.Map.Index][1];
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
            int drawOffset = (int) (Scale * 40.0);
            double maxX = winGamePosX + winGameWidth + drawOffset;
            double maxY = winGamePosY + winGameHeight + drawOffset;
            double newMaxX = maxX * Scale;
            double newMaxY = maxY * Scale;
            int minPixelsX = (int) ((winGamePosX - drawOffset) * Scale - (newMaxX + maxX));
            int maxPixelsX = (int) newMaxX;
            int minPixelsY = (int) ((winGamePosY - drawOffset) * Scale - (newMaxY + maxY));
            int maxPixlesY = (int) newMaxY;
            if (_updateDrawPosition || oldDrawOffsetX != winDrawOffsetX || oldDrawOffsetY != winDrawOffsetY) _updateDrawPosition = true;

            return (new Point(realMinRangeX, realMinRangeY), new Point(realMaxRangeX, realMaxRangeY), new Vector2(minPixelsX, minPixelsY), new Vector2(maxPixelsX, maxPixlesY), new Point(winDrawOffsetX, winDrawOffsetY), new Point(winGameCenterX, winGameCenterY), new Point(realMinRangeX + width - 1, realMinRangeY - 1), Math.Max(width, height));
        }
    }
}