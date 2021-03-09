﻿#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Map;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Scenes
{
    internal partial class GameScene
    {
        private static GameObject[] _renderList = new GameObject[10000];
        private static GameObject[] _foliages = new GameObject[100];
        private static readonly GameObject[] _objectHandles = new GameObject[Constants.MAX_OBJECT_HANDLES];
        private static readonly TreeUnion[] _treeInfos =
        {
            new TreeUnion(0x0D45, 0x0D4C),
            new TreeUnion(0x0D5C, 0x0D62),
            new TreeUnion(0x0D73, 0x0D79),
            new TreeUnion(0x0D87, 0x0D8B),
            new TreeUnion(0x12BE, 0x12C7),
            new TreeUnion(0x0D4D, 0x0D53),
            new TreeUnion(0x0D63, 0x0D69),
            new TreeUnion(0x0D7A, 0x0D7F),
            new TreeUnion(0x0D8C, 0x0D90)
        };
        private StaticTiles _empty;


        private sbyte _maxGroundZ;
        private int _maxZ;
        private Vector2 _minPixel, _maxPixel;
        private bool _noDrawRoofs;
        private int _objectHandlesCount;
        private Point _offset, _maxTile, _minTile, _last_scaled_offset;
        private int _oldPlayerX, _oldPlayerY, _oldPlayerZ;
        private int _renderIndex = 1;
        private int _renderListCount, _foliageCount;


        public Point ScreenOffset => _offset;
        public sbyte FoliageIndex { get; private set; }


        public void UpdateMaxDrawZ(bool force = false)
        {
            int playerX = World.Player.X;
            int playerY = World.Player.Y;
            int playerZ = World.Player.Z;

            if (playerX == _oldPlayerX && playerY == _oldPlayerY && playerZ == _oldPlayerZ && !force)
            {
                return;
            }

            _oldPlayerX = playerX;
            _oldPlayerY = playerY;
            _oldPlayerZ = playerZ;

            sbyte maxGroundZ = 127;
            _maxGroundZ = 127;
            _maxZ = 127;
            _noDrawRoofs = !ProfileManager.CurrentProfile.DrawRoofs;
            int bx = playerX;
            int by = playerY;
            Chunk chunk = World.Map.GetChunk(bx, by, false);

            if (chunk != null)
            {
                int x = playerX % 8;
                int y = playerY % 8;

                int pz14 = playerZ + 14;
                int pz16 = playerZ + 16;

                for (GameObject obj = chunk.GetHeadObject(x, y); obj != null; obj = obj.TNext)
                {
                    sbyte tileZ = obj.Z;

                    if (obj is Land l)
                    {
                        if (l.IsStretched)
                        {
                            tileZ = l.AverageZ;
                        }

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
                    {
                        continue;
                    }


                    //if (obj is Item it && !it.ItemData.IsRoof || !(obj is Static) && !(obj is Multi))
                    //    continue;

                    if (tileZ > pz14 && _maxZ > tileZ)
                    {
                        ref StaticTiles itemdata = ref TileDataLoader.Instance.StaticData[obj.Graphic];

                        //if (GameObjectHelper.TryGetStaticData(obj, out var itemdata) && ((ulong) itemdata.Flags & 0x20004) == 0 && (!itemdata.IsRoof || itemdata.IsSurface))
                        if (((ulong) itemdata.Flags & 0x20004) == 0 && (!itemdata.IsRoof || itemdata.IsSurface))
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
                chunk = World.Map.GetChunk(bx, by, false);

                if (chunk != null)
                {
                    x = playerX % 8;
                    y = playerY % 8;

                    for (GameObject obj2 = chunk.GetHeadObject(x, y); obj2 != null; obj2 = obj2.TNext)
                    {
                        //if (obj is Item it && !it.ItemData.IsRoof || !(obj is Static) && !(obj is Multi))
                        //    continue;

                        if (obj2 is Mobile)
                        {
                            continue;
                        }

                        sbyte tileZ = obj2.Z;

                        if (tileZ > pz14 && _maxZ > tileZ)
                        {
                            if (!(obj2 is Land))
                            {
                                ref StaticTiles itemdata = ref TileDataLoader.Instance.StaticData[obj2.Graphic];

                                if (((ulong) itemdata.Flags & 0x204) == 0 && itemdata.IsRoof)
                                {
                                    _maxZ = tileZ;
                                    World.Map.ClearBockAccess();
                                    _maxGroundZ = World.Map.CalculateNearZ(tileZ, playerX, playerY, tileZ);
                                    _noDrawRoofs = true;
                                }
                            }

                            //if (GameObjectHelper.TryGetStaticData(obj2, out var itemdata) && ((ulong) itemdata.Flags & 0x204) == 0 && itemdata.IsRoof)
                            //{
                            //    _maxZ = tileZ;
                            //    World.Map.ClearBockAccess();
                            //    _maxGroundZ = World.Map.CalculateNearZ(tileZ, playerX, playerY, tileZ);
                            //    _noDrawRoofs = true;
                            //}
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

        private void IsFoliageUnion(ushort graphic, int x, int y, int z)
        {
            for (int i = 0; i < _treeInfos.Length; i++)
            {
                ref TreeUnion info = ref _treeInfos[i];

                if (info.Start <= graphic && graphic <= info.End)
                {
                    while (graphic > info.Start)
                    {
                        graphic--;
                        x--;
                        y++;
                    }

                    for (graphic = info.Start; graphic <= info.End; graphic++, x++, y--)
                    {
                        ApplyFoliageTransparency(graphic, x, y, z);
                    }

                    break;
                }
            }
        }

        private void ApplyFoliageTransparency(ushort graphic, int x, int y, int z)
        {
            GameObject tile = World.Map.GetTile(x, y);

            if (tile != null)
            {
                for (GameObject obj = tile; obj != null; obj = obj.TNext)
                {
                    ushort testGraphic = obj.Graphic;

                    if (testGraphic == graphic && obj.Z == z)
                    {
                        obj.FoliageIndex = FoliageIndex;
                    }
                }
            }
        }

        private void AddTileToRenderList(GameObject obj, int worldX, int worldY, bool useObjectHandles, int maxZ /*, GameObject entity*/)
        {
            /*sbyte HeightChecks = 0;
            if(entity != null)
            {
                if(entity.X < worldX && entity.Y > worldY)
                {
                    HeightChecks = 1;
                }
                else if (entity.Y < worldY && entity.X > worldX)
                {
                    HeightChecks = -1;
                }
            }*/

            TileDataLoader loader = TileDataLoader.Instance;

            for (; obj != null; obj = obj.TNext)
            {
                if (obj.CurrentRenderIndex == _renderIndex || !obj.AllowedToDraw)
                {
                    continue;
                }

                if (UpdateDrawPosition && obj.CurrentRenderIndex != _renderIndex || obj.IsPositionChanged)
                {
                    obj.UpdateRealScreenPosition(_offset.X, _offset.Y);
                }

                obj.UseInRender = 0xFF;

                int drawX = obj.RealScreenPosition.X;
                int drawY = obj.RealScreenPosition.Y;

                if (drawX < _minPixel.X || drawX > _maxPixel.X)
                {
                    break;
                }

                int maxObjectZ = obj.PriorityZ;

                ref StaticTiles itemData = ref _empty;

                bool changinAlpha = false;
                bool island = false;
                bool iscorpse = false;
                bool ismobile = false;
                bool push_with_priority = false;

                ref ushort graphic = ref obj.Graphic;

                switch (obj)
                {
                    case Mobile _:
                        maxObjectZ += Constants.DEFAULT_CHARACTER_HEIGHT;
                        ismobile = true;
                        push_with_priority = true;

                        break;

                    case Land _:
                        island = true;

                        goto SKIP_HANDLES_CHECK;

                    case Item it:

                        if (it.IsCorpse)
                        {
                            iscorpse = true;
                            push_with_priority = true;
                            goto default;
                        }
                        else if (it.IsMulti)
                        {
                            graphic = it.MultiGraphic;
                        }

                        push_with_priority = it.Offset != Vector3.Zero;

                        //goto default;

                        //push_with_priority = it.Offset != Vector3.Zero && ((
                        //                                                       it.BoatDirection != Direction.West &&
                        //                                                       it.BoatDirection != Direction.Up &&
                        //                                                       it.BoatDirection != Direction.North));
                        goto default;

                    case MovingEffect moveEff:
                        push_with_priority = true;
                        goto default;

                    case Multi multi:
                        push_with_priority = (multi.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_PREVIEW) != 0 && multi.Offset != Vector3.Zero;

                        //push_with_priority = multi.IsMovable;

                        goto default;

                    default:

                        itemData = ref loader.StaticData[graphic];

                        if (itemData.IsFoliage && !itemData.IsMultiMovable && World.Season >= Season.Winter && !(obj is Multi))
                        {
                            continue;
                        }

                        if (_noDrawRoofs && itemData.IsRoof)
                        {
                            if (_alphaChanged)
                            {
                                changinAlpha = obj.ProcessAlpha(0);
                            }
                            else
                            {
                                changinAlpha = obj.AlphaHue != 0;
                            }

                            if (!changinAlpha)
                            {
                                continue;
                            }
                        }

                        //we avoid to hide impassable foliage or bushes, if present...
                        if (ProfileManager.CurrentProfile.TreeToStumps && itemData.IsFoliage && !itemData.IsMultiMovable && !(obj is Multi) || ProfileManager.CurrentProfile.HideVegetation && (obj is Multi mm && mm.IsVegetation || obj is Static st && st.IsVegetation))
                        {
                            continue;
                        }

                        //if (HeightChecks <= 0 && (!itemData.IsBridge || ((itemData.Flags & TileFlag.StairBack | TileFlag.StairRight) != 0) || itemData.IsWall))
                    {
                        maxObjectZ += itemData.Height == 0xFF ? 0 : itemData.Height;
                    }

                        break;
                }


                if (useObjectHandles && NameOverHeadManager.IsAllowed(obj as Entity))
                {
                    if ((ismobile || iscorpse || obj is Item it && (!it.IsLocked || it.IsLocked && itemData.IsContainer) && !it.IsMulti) && !obj.ClosedObjectHandles)
                    {
                        int index = _objectHandlesCount % Constants.MAX_OBJECT_HANDLES;

                        if (_objectHandles[index] != null && !_objectHandles[index].ObjectHandlesOpened)
                        {
                            _objectHandles[index].UseObjectHandles = false;

                            //_objectHandles[index].ObjectHandlesOpened = false;
                        }

                        _objectHandles[index] = obj;
                        obj.UseObjectHandles = true;
                        _objectHandlesCount++;
                        obj.UpdateTextCoordsV();
                    }
                }
                else if (obj.ClosedObjectHandles)
                {
                    obj.ClosedObjectHandles = false;
                    obj.ObjectHandlesOpened = false;
                    obj.UpdateTextCoordsV();
                }
                else if (obj.UseObjectHandles)
                {
                    obj.ObjectHandlesOpened = false;
                    obj.UseObjectHandles = false;
                    obj.UpdateTextCoordsV();
                }


                SKIP_HANDLES_CHECK:

                if (maxObjectZ > maxZ)
                {
                    break;
                }

                obj.CurrentRenderIndex = _renderIndex;

                if (!island)
                {
                    //obj.UpdateTextCoordsV();
                }
                else
                {
                    goto SKIP_INTERNAL_CHECK;
                }

                if (!ismobile && !iscorpse && !island && itemData.IsInternal)
                {
                    continue;
                }

                SKIP_INTERNAL_CHECK:

                int z = obj.Z;

                if (!island && z >= _maxZ)
                {
                    if (!changinAlpha)
                    {
                        if (_alphaChanged)
                        {
                            changinAlpha = obj.ProcessAlpha(0);
                        }
                        else
                        {
                            changinAlpha = obj.AlphaHue != 0;
                        }

                        if (!changinAlpha)
                        {
                            obj.UseInRender = (byte) _renderIndex;

                            continue;
                        }
                    }
                }

                int testMaxZ = drawY;

                if (testMaxZ > _maxPixel.Y)
                {
                    continue;
                }

                int testMinZ = drawY + (z << 2);

                if (island)
                {
                    Land t = obj as Land;

                    if (t.IsStretched)
                    {
                        testMinZ -= t.MinZ << 2;
                    }
                    else
                    {
                        testMinZ = testMaxZ;
                    }
                }
                else
                {
                    testMinZ = testMaxZ;
                }

                if (testMinZ < _minPixel.Y)
                {
                    continue;
                }

                if (push_with_priority)
                {
                    AddOffsetCharacterTileToRenderList(obj, useObjectHandles);
                }
                else if (!island && itemData.IsFoliage)
                {
                    if (obj.FoliageIndex != FoliageIndex)
                    {
                        sbyte index = 0;

                        bool check = World.Player.X <= worldX && World.Player.Y <= worldY;

                        if (!check)
                        {
                            check = World.Player.Y <= worldY && World.Player.X <= worldX + 1;

                            if (!check)
                            {
                                check = World.Player.X <= worldX && World.Player.Y <= worldY + 1;
                            }
                        }

                        if (check)
                        {
                            ArtTexture texture = ArtLoader.Instance.GetTexture(graphic);

                            if (texture != null)
                            {
                                _rectangleObj.X = drawX - (texture.Width >> 1) + texture.ImageRectangle.X;
                                _rectangleObj.Y = drawY - texture.Height + texture.ImageRectangle.Y;
                                _rectangleObj.Width = texture.ImageRectangle.Width;
                                _rectangleObj.Height = texture.ImageRectangle.Height;

                                check = Exstentions.InRect(ref _rectangleObj, ref _rectanglePlayer);

                                if (check)
                                {
                                    index = FoliageIndex;
                                    IsFoliageUnion(obj.Graphic, obj.X, obj.Y, z);
                                }
                            }
                        }

                        obj.FoliageIndex = index;
                    }

                    if (_foliageCount >= _foliages.Length)
                    {
                        int newsize = _foliages.Length + 50;
                        Array.Resize(ref _foliages, newsize);
                    }

                    _foliages[_foliageCount++] = obj;

                    goto FOLIAGE_SKIP;
                }

                if (!island && _alphaChanged && !changinAlpha)
                {
                    if (itemData.IsTranslucent)
                    {
                        obj.ProcessAlpha(178);
                    }
                    else if (!itemData.IsFoliage && obj.AlphaHue != 0xFF)
                    {
                        obj.ProcessAlpha(0xFF);
                    }
                }

                FOLIAGE_SKIP:

                if (_renderListCount >= _renderList.Length)
                {
                    int newsize = _renderList.Length + 1000;
                    Array.Resize(ref _renderList, newsize);
                }

                _renderList[_renderListCount++] = obj;
                obj.UseInRender = (byte) _renderIndex;
            }
        }

        private void AddOffsetCharacterTileToRenderList(GameObject entity, bool useObjectHandles)
        {
            int charX = entity.X;
            int charY = entity.Y;
            int maxZ = entity.PriorityZ;

            int dropMaxZIndex = -1;

            if (entity is Mobile mob)
            {
                if (mob.Steps.Count != 0)
                {
                    ref Mobile.Step step = ref mob.Steps.Back();

                    if ((step.Direction & 7) == 2 || (step.Direction & 7) == 6)
                    {
                        dropMaxZIndex = 0;
                    }
                }
                else if (mob.Direction == Direction.East || mob.Direction == Direction.West)
                {
                    dropMaxZIndex = 0;
                }
            }

            for (int i = 0; i < 8; i++)
            {
                int x = charX;
                int y = charY;

                switch (i)
                {
                    case 0:
                        x++;
                        y--;

                        break;

                    case 1:
                        x++;
                        y -= 2;

                        break;

                    case 2:
                        x += 2;
                        y -= 2;

                        break;

                    case 3:
                        x--;
                        y += 2;

                        break;

                    case 4:
                        y++;

                        break;

                    case 5:
                        x++;

                        break;

                    case 6:
                        x += 2;
                        y--;

                        break;

                    case 7:
                        x++;
                        y++;

                        break;
                }

                if (x < _minTile.X || x > _maxTile.X)
                {
                    continue;
                }

                if (y < _minTile.Y || y > _maxTile.Y)
                {
                    continue;
                }

                int currentMaxZ = maxZ;

                if (i == dropMaxZIndex)
                {
                    currentMaxZ += 20;
                }

                GameObject tile = World.Map.GetTile(x, y);

                if (tile != null)
                {
                    AddTileToRenderList
                    (
                        tile,
                        x,
                        y,
                        useObjectHandles,
                        currentMaxZ
                    );
                }
            }

            /*int area = 2;

            if (entity is Mobile mob)
            {
                byte dir;
                if (mob.IsMoving)
                {
                    Mobile.Step s = mob.Steps.Back();
                    dir = s.Direction;
                    if (dir > 0 && dir < 6)
                    {
                        if (s.Z > entity.Z)
                            maxZ += s.Z + 5 - entity.Z;
                    }
                }
                else
                    dir = (byte)mob.Direction;

                if (mob.Texture != null)
                {
                    Rectangle r = mob.Texture.Bounds;
                    //this is a raw optimization, since every object is at least 44*44, we consider the minimum 4096 (optimized to avoid division and use bit shift operands)
                    //so we can calculate an approximated area occupied by the animation in tiles, and use an area of 2 for priority drawing at minimum
                    
                    //area = Math.Max(4096, r.Width * r.Height) >> 11;
                    //area >>= 2;

                    //if (area > 5)
                    //    area = 5;
                    //else if (area < 2)
                    //    area = 2;

                    
                    area = Math.Max(r.Width, r.Height);

                    if (area < 32)
                        area = 44;

                    area >>= 5;

                    if (area > 2)
                        area >>= 1;
                    else if (area < 1)
                        area = 1;

                    //if (area > 3)
                    //    area = 3;
                    //if (area >= 2)
                    //{
                    //    if (dir % 2 != 0)
                    //        area--;
                    //}
                }
                else
                    area = 0;
            }
            if (area == 0)
                return;

            int minX = charX - area;
            int minY = charY + area;
            int maxX = charX + area;
            int maxY = charY - area;

            for (int leadx = minX; leadx <= maxX; leadx++)
            {
                int x = leadx, y = minY;
                while (x <= maxX && y >= maxY)
                {
                    if (x != charX || y != charY)
                    {
                        Tile tile = World.Map.GetTile(x, y);

                        if (tile != null)
                        {
                            AddTileToRenderList(tile.FirstNode, x, y, useObjectHandles, maxZ, entity);
                        }
                    }
                    x++;
                    y--;
                }
            }
            */
        }

        private void GetViewPort()
        {
            int oldDrawOffsetX = _offset.X;
            int oldDrawOffsetY = _offset.Y;
            Point old_scaled_offset = _last_scaled_offset;

            float zoom = Camera.Zoom;

            int winGamePosX = 0;
            int winGamePosY = 0;
            int winGameWidth = ProfileManager.CurrentProfile.GameWindowSize.X;
            int winGameHeight = ProfileManager.CurrentProfile.GameWindowSize.Y;
            int winGameCenterX = winGamePosX + (winGameWidth >> 1);
            int winGameCenterY = winGamePosY + (winGameHeight >> 1) + (World.Player.Z << 2);
            winGameCenterX -= (int) World.Player.Offset.X;
            winGameCenterY -= (int) (World.Player.Offset.Y - World.Player.Offset.Z);

            int tileOffX = World.Player.X;
            int tileOffY = World.Player.Y;

            int winDrawOffsetX = (tileOffX - tileOffY) * 22 - winGameCenterX;
            int winDrawOffsetY = (tileOffX + tileOffY) * 22 - winGameCenterY;

            int winGameScaledOffsetX;
            int winGameScaledOffsetY;
            int winGameScaledWidth;
            int winGameScaledHeight;

            if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.EnableMousewheelScaleZoom)
            {
                float left = winGamePosX;
                float right = winGameWidth + left;
                float top = winGamePosY;
                float bottom = winGameHeight + top;
                float newRight = right * zoom;
                float newBottom = bottom * zoom;

                winGameScaledOffsetX = (int) (left * zoom - (newRight - right));
                winGameScaledOffsetY = (int) (top * zoom - (newBottom - bottom));
                winGameScaledWidth = (int) (newRight - winGameScaledOffsetX);
                winGameScaledHeight = (int) (newBottom - winGameScaledOffsetY);
            }
            else
            {
                winGameScaledOffsetX = 0;
                winGameScaledOffsetY = 0;
                winGameScaledWidth = 0;
                winGameScaledHeight = 0;
            }


            //if (_use_render_target)
            //{
            //    winDrawOffsetX += winGameScaledOffsetX >> 1;
            //    winDrawOffsetY += winGameScaledOffsetY >> 1;
            //}

            int width = (int) ((winGameWidth / 44 + 1) * zoom);
            int height = (int) ((winGameHeight / 44 + 1) * zoom);


            if (width < height)
            {
                width = height;
            }
            else
            {
                height = width;
            }

            int realMinRangeX = tileOffX - width;

            if (realMinRangeX < 0)
            {
                realMinRangeX = 0;
            }

            int realMaxRangeX = tileOffX + width;
            //if (realMaxRangeX >= FileManager.Map.MapsDefaultSize[World.Map.Index][0])
            //    realMaxRangeX = FileManager.Map.MapsDefaultSize[World.Map.Index][0];

            int realMinRangeY = tileOffY - height;

            if (realMinRangeY < 0)
            {
                realMinRangeY = 0;
            }

            int realMaxRangeY = tileOffY + height;
            //if (realMaxRangeY >= FileManager.Map.MapsDefaultSize[World.Map.Index][1])
            //    realMaxRangeY = FileManager.Map.MapsDefaultSize[World.Map.Index][1];

            int minBlockX = (realMinRangeX >> 3) - 1;
            int minBlockY = (realMinRangeY >> 3) - 1;
            int maxBlockX = (realMaxRangeX >> 3) + 1;
            int maxBlockY = (realMaxRangeY >> 3) + 1;

            if (minBlockX < 0)
            {
                minBlockX = 0;
            }

            if (minBlockY < 0)
            {
                minBlockY = 0;
            }

            if (maxBlockX >= MapLoader.Instance.MapsDefaultSize[World.Map.Index, 0])
            {
                maxBlockX = MapLoader.Instance.MapsDefaultSize[World.Map.Index, 0] - 1;
            }

            if (maxBlockY >= MapLoader.Instance.MapsDefaultSize[World.Map.Index, 1])
            {
                maxBlockY = MapLoader.Instance.MapsDefaultSize[World.Map.Index, 1] - 1;
            }

            int drawOffset = (int) (44 / zoom);

            Point p = Point.Zero;
            p.X -= drawOffset;
            p.Y -= drawOffset;
            p = Camera.ScreenToWorld(p);
            int minPixelsX = p.X;
            int minPixelsY = p.Y;

            p.X = Camera.Bounds.Width + drawOffset;
            p.Y = Camera.Bounds.Height + drawOffset;
            p = Camera.ScreenToWorld(p);
            int maxPixelsX = p.X;
            int maxPixelsY = p.Y;


            if (UpdateDrawPosition || oldDrawOffsetX != winDrawOffsetX || oldDrawOffsetY != winDrawOffsetY || old_scaled_offset.X != winGameScaledOffsetX || old_scaled_offset.Y != winGameScaledOffsetY)
            {
                UpdateDrawPosition = true;


                if (_use_render_target && (_world_render_target == null || _world_render_target.Width != (int) (winGameWidth * zoom) || _world_render_target.Height != (int) (winGameHeight * zoom)))
                {
                    _world_render_target?.Dispose();

                    PresentationParameters pp = Client.Game.GraphicsDevice.PresentationParameters;

                    _world_render_target = new RenderTarget2D
                    (
                        Client.Game.GraphicsDevice,
                        winGameWidth * 1,
                        winGameHeight * 1,
                        false,
                        pp.BackBufferFormat,
                        pp.DepthStencilFormat,
                        pp.MultiSampleCount,
                        pp.RenderTargetUsage
                    );
                }

                if (_lightRenderTarget == null || _lightRenderTarget.Width != winGameWidth || _lightRenderTarget.Height != winGameHeight)
                {
                    _lightRenderTarget?.Dispose();

                    PresentationParameters pp = Client.Game.GraphicsDevice.PresentationParameters;


                    _lightRenderTarget = new RenderTarget2D
                    (
                        Client.Game.GraphicsDevice,
                        winGameWidth,
                        winGameHeight,
                        false,
                        pp.BackBufferFormat,
                        pp.DepthStencilFormat,
                        pp.MultiSampleCount,
                        pp.RenderTargetUsage
                    );
                }
            }

            _minTile.X = realMinRangeX;
            _minTile.Y = realMinRangeY;
            _maxTile.X = realMaxRangeX;
            _maxTile.Y = realMaxRangeY;

            _minPixel.X = minPixelsX;
            _minPixel.Y = minPixelsY;
            _maxPixel.X = maxPixelsX;
            _maxPixel.Y = maxPixelsY;

            _offset.X = winDrawOffsetX;
            _offset.Y = winDrawOffsetY;

            _last_scaled_offset.X = winGameScaledOffsetX;
            _last_scaled_offset.Y = winGameScaledOffsetY;


            UpdateMaxDrawZ();
        }

        private struct TreeUnion
        {
            public TreeUnion(ushort start, ushort end)
            {
                Start = start;
                End = end;
            }

            public readonly ushort Start;
            public readonly ushort End;
        }
    }
}