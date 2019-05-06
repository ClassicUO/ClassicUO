﻿#region license

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

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Map;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Scenes
{
    internal partial class GameScene
    {
        private sbyte _maxGroundZ;
        private int _maxZ;
        private Vector2 _minPixel, _maxPixel;
        private bool _noDrawRoofs;
        private int _objectHandlesCount;
        //private WeakReference<GameObject>[] _renderList = new WeakReference<GameObject>[2000];


        private Point _offset, _maxTile, _minTile;
        private int _oldPlayerX, _oldPlayerY, _oldPlayerZ;

        private int _renderIndex = 1;

        private GameObject[] _renderList = new GameObject[2000];
        private int _renderListCount;
        private bool _updateDrawPosition;

        public void UpdateMaxDrawZ(bool force = false)
        {
            int playerX = World.Player.X;
            int playerY = World.Player.Y;
            int playerZ = World.Player.Z;

            if (playerX == _oldPlayerX && playerY == _oldPlayerY && playerZ == _oldPlayerZ && !force)
                return;

            _oldPlayerX = playerX;
            _oldPlayerY = playerY;
            _oldPlayerZ = playerZ;

            sbyte maxGroundZ = 127;
            _maxGroundZ = 127;
            _maxZ = 127;
            _noDrawRoofs = !Engine.Profile.Current.DrawRoofs;
            int bx = playerX;
            int by = playerY;
            Tile tile = World.Map.GetTile(bx, by, false);

            if (tile != null)
            {
                int pz14 = playerZ + 14;
                int pz16 = playerZ + 16;

                for (GameObject obj = tile.FirstNode; obj != null; obj = obj.Right)
                {
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


                    //if (obj is Item it && !it.ItemData.IsRoof || !(obj is Static) && !(obj is Multi))
                    //    continue;

                    if (tileZ > pz14 && _maxZ > tileZ)
                    {
                        if (GameObjectHelper.TryGetStaticData(obj, out var itemdata) && ((ulong) itemdata.Flags & 0x20004) == 0 && (!itemdata.IsRoof || itemdata.IsSurface))
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
                tile = World.Map.GetTile(bx, by, false);

                if (tile != null)
                {
                    for (GameObject obj = tile.FirstNode; obj != null; obj = obj.Right)
                    {
                        //if (obj is Item it && !it.ItemData.IsRoof || !(obj is Static) && !(obj is Multi))
                        //    continue;

                        if (obj is Mobile)
                            continue;

                        sbyte tileZ = obj.Z;

                        if (tileZ > pz14 && _maxZ > tileZ)
                        {
                            if (GameObjectHelper.TryGetStaticData(obj, out var itemdata) && ((ulong) itemdata.Flags & 0x204) == 0 && itemdata.IsRoof)
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

        private void AddTileToRenderList(GameObject obj, int worldX, int worldY, bool useObjectHandles, int maxZ)
        {
            for (; obj != null; obj = obj.Right)
            {
                if (obj.CurrentRenderIndex == _renderIndex || obj.IsDestroyed || !obj.AllowedToDraw) continue;

                if (_updateDrawPosition && obj.CurrentRenderIndex != _renderIndex || obj.IsPositionChanged)
                    obj.UpdateRealScreenPosition(_offset);

                //obj.UseInRender = 0xFF;

                int drawX = obj.RealScreenPosition.X;
                int drawY = obj.RealScreenPosition.Y;

                if (drawX < _minPixel.X || drawX > _maxPixel.X)
                    break;

                int z = obj.Z;
                int maxObjectZ = obj.PriorityZ;

                bool ismobile = false;

                StaticTiles itemData = default;
                bool changinAlpha = false;

                switch (obj)
                {
                    case Mobile _:
                        maxObjectZ += Constants.DEFAULT_CHARACTER_HEIGHT;
                        ismobile = true;

                        break;
                    default:

                        if (GameObjectHelper.TryGetStaticData(obj, out itemData))
                        {
                            if (obj is Static st)
                            {
                                if (StaticFilters.IsTree(st.OriginalGraphic))
                                {
                                    if (Engine.Profile.Current.TreeToStumps && st.Graphic != Constants.TREE_REPLACE_GRAPHIC)
                                        st.SetGraphic(Constants.TREE_REPLACE_GRAPHIC);
                                    else if (st.OriginalGraphic != st.Graphic && !Engine.Profile.Current.TreeToStumps)
                                        st.RestoreOriginalGraphic();
                                }
                            }

                            if (_noDrawRoofs && itemData.IsRoof)
                            {
                                if (_alphaChanged)
                                    changinAlpha = obj.ProcessAlpha(0);
                                else
                                    changinAlpha = obj.AlphaHue != 0;


                                if (!changinAlpha)
                                    continue;
                            }

                            if (Engine.Profile.Current.TreeToStumps && itemData.IsFoliage || Engine.Profile.Current.HideVegetation && StaticFilters.IsVegetation(obj.Graphic))
                                continue;

                            maxObjectZ += itemData.Height;
                        }

                        break;
                }

                if (maxObjectZ > maxZ) break;

                obj.CurrentRenderIndex = _renderIndex;

                bool iscorpse = !ismobile && obj is Item item && item.IsCorpse;

                if (!ismobile && !iscorpse && itemData.IsInternal) continue;

                bool island = !ismobile && !iscorpse && obj is Land;

                if (!island && z >= _maxZ)
                {
                    if (!changinAlpha)
                    {
                        if (_alphaChanged)
                            changinAlpha = obj.ProcessAlpha(0);
                        else
                            changinAlpha = obj.AlphaHue != 0;

                        if (!changinAlpha)
                            continue;
                    }
                }

                int testMinZ = drawY + z * 4;
                int testMaxZ = drawY;

                if (island)
                {
                    Land t = obj as Land;

                    if (t.IsStretched)
                        testMinZ -= t.MinZ * 4;
                    else
                        testMinZ = testMaxZ;
                }
                else
                    testMinZ = testMaxZ;

                if (testMinZ < _minPixel.Y || testMaxZ > _maxPixel.Y)
                    continue;


                if (obj.OverheadMessageContainer != null && !obj.OverheadMessageContainer.IsEmpty) Overheads.AddOverhead(obj.OverheadMessageContainer);

                if (ismobile || iscorpse)
                    AddOffsetCharacterTileToRenderList(obj, useObjectHandles);
                else if (itemData.IsFoliage)
                {
                    bool check = World.Player.X <= worldX && World.Player.Y <= worldY;

                    if (!check)
                    {
                        check = World.Player.Y <= worldY && World.Player.Position.X <= worldX + 1;

                        if (!check)
                            check = World.Player.X <= worldX && World.Player.Y <= worldY + 1;
                    }

                    if (check)
                    {
                        Rectangle rect = new Rectangle(drawX - obj.FrameInfo.X,
                                                       drawY - obj.FrameInfo.Y,
                                                       obj.FrameInfo.Width,
                                                       obj.FrameInfo.Height);

                        Rectangle r = World.Player.GetOnScreenRectangle();
                        check = Exstentions.InRect(ref rect, ref r);
                    }

                    if (obj is Static st)
                        st.CharacterIsBehindFoliage = check;
                    else if (obj is Multi m)
                        m.CharacterIsBehindFoliage = check;
                    else if (obj is Item it)
                        it.CharacterIsBehindFoliage = check;
                }

                if (_alphaChanged && !changinAlpha)
                {
                    if (itemData.IsTranslucent)
                        obj.ProcessAlpha(178);
                    else if (!itemData.IsFoliage && obj.AlphaHue != 0xFF)
                        obj.ProcessAlpha(0xFF);
                }

                if (_renderListCount >= _renderList.Length)
                {
                    int newsize = _renderList.Length + 1000;
                    //_renderList.Resize(newsize);
                    Array.Resize(ref _renderList, newsize);
                }


                if (useObjectHandles && NameOverHeadManager.IsAllowed(obj as Entity))
                {
                    obj.UseObjectHandles = (ismobile || iscorpse || obj is Item it /*&& !it.IsLocked */&& !it.IsMulti) && !obj.ClosedObjectHandles;
                    _objectHandlesCount++;
                }
                else if (obj.ClosedObjectHandles)
                {
                    obj.ClosedObjectHandles = false;
                    obj.ObjectHandlesOpened = false;
                }
                else if (obj.UseObjectHandles)
                {
                    obj.ObjectHandlesOpened = false;
                    obj.UseObjectHandles = false;
                }

                //ref var weak = ref _renderList[_renderListCount];

                //if (weak == null)
                //    weak = new WeakReference<GameObject>(obj);
                //else
                //    weak.SetTarget(obj);

                _renderList[_renderListCount] = obj;
                //_renderList.Enqueue(obj);
                //obj.UseInRender = (byte) _renderIndex;
                _renderListCount++;
            }
        }



        private void AddOffsetCharacterTileToRenderList(GameObject entity, bool useObjectHandles)
        {
            int charX = entity.X;
            int charY = entity.Y;
            int dropMaxZIndex = -1;


            if (entity is Mobile mob && mob.IsMoving && mob.Steps.Back().Direction == 2)
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

                AddTileToRenderList(tile.FirstNode, x, y, useObjectHandles, currentMaxZ);
            }
        }

        private void GetViewPort()
        {
            int oldDrawOffsetX = _offset.X;
            int oldDrawOffsetY = _offset.Y;
            int winGamePosX = 0;
            int winGamePosY = 0;
            int winGameWidth = Engine.Profile.Current.GameWindowSize.X;
            int winGameHeight = Engine.Profile.Current.GameWindowSize.Y;
            int winGameCenterX = winGamePosX + (winGameWidth >> 1);
            int winGameCenterY = winGamePosY + (winGameHeight >> 1) + World.Player.Z * 4;
            winGameCenterX -= (int) World.Player.Offset.X;
            winGameCenterY -= (int) (World.Player.Offset.Y - World.Player.Offset.Z);
            int winDrawOffsetX = (World.Player.X - World.Player.Y) * 22 - winGameCenterX;
            int winDrawOffsetY = (World.Player.X + World.Player.Y) * 22 - winGameCenterY;
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

            winDrawOffsetX += winGameScaledOffsetX >> 1;
            winDrawOffsetY += winGameScaledOffsetY >> 1;

            const int MAX = 70;

            if (width > MAX)
                width = MAX;

            if (height > MAX)
                height = MAX;

            int size = Math.Max(width, height);

            int realMinRangeX = World.Player.Position.X - size;

            if (realMinRangeX < 0)
                realMinRangeX = 0;
            int realMaxRangeX = World.Player.Position.X + size;

            //if (realMaxRangeX >= FileManager.Map.MapsDefaultSize[World.Map.Index][0])
            //    realMaxRangeX = FileManager.Map.MapsDefaultSize[World.Map.Index][0];
            int realMinRangeY = World.Player.Position.Y - size;

            if (realMinRangeY < 0)
                realMinRangeY = 0;
            int realMaxRangeY = World.Player.Position.Y + size;

            //if (realMaxRangeY >= FileManager.Map.MapsDefaultSize[World.Map.Index][1])
            //    realMaxRangeY = FileManager.Map.MapsDefaultSize[World.Map.Index][1];
            int minBlockX = (realMinRangeX >> 3) - 1;
            int minBlockY = (realMinRangeY >> 3) - 1;
            int maxBlockX = (realMaxRangeX >> 3) + 1;
            int maxBlockY = (realMaxRangeY >> 3) + 1;

            if (minBlockX < 0)
                minBlockX = 0;

            if (minBlockY < 0)
                minBlockY = 0;

            if (maxBlockX >= FileManager.Map.MapsDefaultSize[World.Map.Index, 0])
                maxBlockX = FileManager.Map.MapsDefaultSize[World.Map.Index, 0] - 1;

            if (maxBlockY >= FileManager.Map.MapsDefaultSize[World.Map.Index, 1])
                maxBlockY = FileManager.Map.MapsDefaultSize[World.Map.Index, 1] - 1;
            int drawOffset = (int) (Scale * 40.0);
            float maxX = winGamePosX + winGameWidth + drawOffset;
            float maxY = winGamePosY + winGameHeight + drawOffset;
            float newMaxX = maxX * Scale;
            float newMaxY = maxY * Scale;
            int minPixelsX = (int) ((winGamePosX - drawOffset) * Scale - (newMaxX + maxX));
            int maxPixelsX = (int) newMaxX;
            int minPixelsY = (int) ((winGamePosY - drawOffset) * Scale - (newMaxY + maxY));
            int maxPixlesY = (int) newMaxY;
            if (_updateDrawPosition || oldDrawOffsetX != winDrawOffsetX || oldDrawOffsetY != winDrawOffsetY) _updateDrawPosition = true;


            _minTile.X = realMinRangeX;
            _minTile.Y = realMinRangeY;
            _maxTile.X = realMaxRangeX;
            _maxTile.Y = realMaxRangeY;

            _minPixel.X = minPixelsX;
            _minPixel.Y = minPixelsY;
            _maxPixel.X = maxPixelsX;
            _maxPixel.Y = maxPixlesY;

            _offset.X = winDrawOffsetX;
            _offset.Y = winDrawOffsetY;
        }
    }
}