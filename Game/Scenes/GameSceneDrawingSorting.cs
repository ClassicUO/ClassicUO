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
        private static void CheckIfUnderEntity(out int maxItemZ, out bool drawTerrain, out bool underSurface)
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
                    if (underObject is IDynamicItem item)
                    {
                        if (TileData.IsRoof((long) item.ItemData.Flags))
                        {
                            maxItemZ = World.Player.Position.Z - World.Player.Position.Z % 20 + 20;
                        }
                        else if (TileData.IsSurface((long) item.ItemData.Flags) || TileData.IsWall((long) item.ItemData.Flags) && !TileData.IsDoor((long) item.ItemData.Flags))
                        {
                            maxItemZ = item.Position.Z;
                        }
                        else
                        {
                            int z = World.Player.Position.Z + (item.ItemData.Height > 20 ? item.ItemData.Height : 20);
                            maxItemZ = z;
                        }
                    }

                    if (underObject is IDynamicItem sta && TileData.IsRoof((long) sta.ItemData.Flags))
                    {
                        bool isRoofSouthEast = true;

                        if ((tile = World.Map.GetTile(World.Map.Center.X + 1, World.Map.Center.Y)) != null)
                        {
                            tile.IsZUnderObjectOrGround(World.Player.Position.Z, out underObject, out underGround);
                            isRoofSouthEast = underObject != null;
                        }

                        if (!isRoofSouthEast)
                            maxItemZ = 255;
                    }

                    underSurface = maxItemZ != 255;
                }
            }
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
#if ORIONSORT
        private int _renderIndex = 1;
        private int _renderListCount;
        private GameObject[] _renderList = new GameObject[2000];
        private Point _offset, _maxTile, _minTile;
        private Vector2 _minPixel, _maxPixel;
        private int _maxZ;
        private bool _drawTerrain;

        private void AddTileToRenderList(IReadOnlyList<GameObject> objList, int worldX, int worldY, bool useObjectHandles, int maxZ)
        {
            for (int i = 0; i < objList.Count; i++)
            {
                GameObject obj = objList[i];

                if (obj.CurrentRenderIndex == _renderIndex || obj.IsDisposed)
                    continue;
                obj.UseInRender = 0xFF;
                int drawX = (obj.Position.X - obj.Position.Y) * 22 - _offset.X;
                int drawY = (obj.Position.X + obj.Position.Y) * 22 - obj.Position.Z * 4 - _offset.Y;

                if (drawX < _minPixel.X || drawX > _maxPixel.X)
                    break;
                int z = obj.Position.Z;
                int maxObjectZ = obj.PriorityZ;

                switch (obj)
                {
                    case Mobile _:
                        maxObjectZ += 16;

                        break;
                    case IDynamicItem dyn:
                        maxObjectZ += dyn.ItemData.Height;

                        break;
                }

                if (maxObjectZ > maxZ)
                    break;
                obj.CurrentRenderIndex = _renderIndex;

                if (!(obj is Tile) && (z >= _maxZ || obj is IDynamicItem dyn2 && (TileData.IsInternal((long) dyn2.ItemData.Flags) || _maxZ != 255 && TileData.IsRoof((long) dyn2.ItemData.Flags))))
                    continue;
                int testMinZ = drawY + z * 4;
                int testMaxZ = drawY;

                if (obj is Tile t && t.IsStretched)
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
            int charX = entity.Position.X;
            int charY = entity.Position.Y;
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
                Tile tile = World.Map.GetTile(x, y);
                int currentMaxZ = maxZ;

                if (i == dropMaxZIndex)
                    currentMaxZ += 20;
                AddTileToRenderList(tile.ObjectsOnTiles, x, y, useObjectHandles, currentMaxZ);
            }
        }

        private (Point, Point, Vector2, Vector2, Point, Point, Point, int) GetViewPort()
        {
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
            float left = winGamePosX;
            float right = winGameWidth + left;
            float top = winGamePosY;
            float bottom = winGameHeight + top;
            float newRight = right * Scale;
            float newBottom = bottom * Scale;
            int winGameScaledOffsetX = (int) (left * Scale - (newRight - right));
            int winGameScaledOffsetY = (int) (top * Scale - (newBottom - bottom));
            int winGameScaledWidth = (int) (newRight - winGameScaledOffsetX);
            int winGameScaledHeight = (int) (newBottom - winGameScaledOffsetY);
            int width = (int) ((winGameWidth / 44 + 1) * Scale);
            int height = (int) ((winGameHeight / 44 + 1) * Scale);

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
            int drawOffset = (int) (Scale * 40.0f);
            float maxX = winGamePosX + winGameWidth + drawOffset;
            float maxY = winGamePosY + winGameHeight + drawOffset;
            float newMaxX = maxX * Scale;
            float newMaxY = maxY * Scale;
            int minPixelsX = (int) ((winGamePosX - drawOffset) * Scale - (newMaxX - maxX));
            int maxPixelsX = (int) newMaxX;
            int minPixelsY = (int) ((winGamePosY - drawOffset) * Scale - (newMaxY - maxY));
            int maxPixlesY = (int) newMaxY;

            return (new Point(realMinRangeX, realMinRangeY), new Point(realMaxRangeX, realMaxRangeY), new Vector2(minPixelsX, minPixelsY), new Vector2(maxPixelsX, maxPixlesY), new Point(winDrawOffsetX, winDrawOffsetY), new Point(winGameCenterX, winGameCenterY), new Point(realMinRangeX + width - 1, realMinRangeY - 1), Math.Max(width, height));
        }
#else
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

#endif
    }
}