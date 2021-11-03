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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Map;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Scenes
{
    internal partial class GameScene
    {      
        private static GameObject[] _foliages = new GameObject[100];
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

        private sbyte _maxGroundZ;
        private int _maxZ;
        private Vector2 _minPixel, _maxPixel;
        private bool _noDrawRoofs;
        private Point _offset, _maxTile, _minTile, _last_scaled_offset;
        private int _oldPlayerX, _oldPlayerY, _oldPlayerZ;
        private int _renderIndex = 1;


#if RENDER_LIST_LINKED_LIST
        private int _renderListCount, _foliageCount, _renderListLandCount;
        private GameObject _first, _renderList;
        private GameObject _firstLand, _renderListLand;
        private bool _useLandList = false;
#else
        struct DrawingInfo
        {
            public GameObject Object;
            public ushort Hue;
        }

        private static DrawingInfo[] _renderList = new DrawingInfo[10000];
        private int _renderListCount, _foliageCount;
#endif


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

        private void UpdateObjectHandles(Entity obj, bool useObjectHandles)
        {
            if (useObjectHandles && NameOverHeadManager.IsAllowed(obj))
            {
                if (obj.ObjectHandlesStatus != ObjectHandlesStatus.CLOSED)
                {
                    if (obj.ObjectHandlesStatus == ObjectHandlesStatus.NONE)
                    {
                        obj.ObjectHandlesStatus = ObjectHandlesStatus.OPEN;
                    }

                    obj.UpdateTextCoordsV();
                }
            }
            else if (obj.ObjectHandlesStatus != ObjectHandlesStatus.NONE)
            {
                obj.ObjectHandlesStatus = ObjectHandlesStatus.NONE;
                obj.UpdateTextCoordsV();
            }
        }
     
        private void CheckIfBehindATree(GameObject obj, int worldX, int worldY, ref StaticTiles itemData)
        {
            if (itemData.IsFoliage)
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
                        ArtTexture texture = ArtLoader.Instance.GetTexture(obj.Graphic);

                        if (texture != null)
                        {
                            Rectangle rect = texture.ImageRectangle;

                            rect.X = obj.RealScreenPosition.X - (rect.Width >> 1) + rect.X;
                            rect.Y = obj.RealScreenPosition.Y - rect.Height + rect.Y;

                            check = Exstentions.InRect(ref rect, ref _rectanglePlayer);

                            if (check)
                            {
                                index = FoliageIndex;
                                IsFoliageUnion(obj.Graphic, obj.X, obj.Y, obj.Z);
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
            }
        }

        private bool ProcessAlpha(GameObject obj, ref StaticTiles itemData)
        {
            if (obj.Z >= _maxZ)
            {
                bool changed;

                if (_alphaChanged)
                {
                    changed = CalculateAlpha(ref obj.AlphaHue, 0);
                }
                else
                {
                    changed = obj.AlphaHue != 0;
                }

                if (!changed)
                {
                    obj.UseInRender = (byte)_renderIndex;

                    return false;
                }
            }
            else if (_noDrawRoofs && itemData.IsRoof)
            {
                if (_alphaChanged)
                {
                    if (!CalculateAlpha(ref obj.AlphaHue, 0))
                    {
                        return false;
                    }
                }

                return obj.AlphaHue != 0;
            }
            else if (itemData.IsTranslucent)
            {
                if (_alphaChanged)
                {
                    CalculateAlpha(ref obj.AlphaHue, 178);
                }
            }
            else if (!itemData.IsFoliage && obj.AlphaHue != 0xFF)
            {
                if (_alphaChanged)
                {
                    CalculateAlpha(ref obj.AlphaHue, 0xFF);
                }
            }

            return true;
        }

        private static bool CalculateAlpha(ref byte alphaHue, int maxAlpha)
        {
            if (ProfileManager.CurrentProfile != null && !ProfileManager.CurrentProfile.UseObjectsFading)
            {
                alphaHue = (byte)maxAlpha;

                return maxAlpha != 0;
            }

            bool result = false;

            int alpha = alphaHue;

            if (alpha > maxAlpha)
            {
                alpha -= 25;

                if (alpha < maxAlpha)
                {
                    alpha = maxAlpha;
                }

                result = true;
            }
            else if (alpha < maxAlpha)
            {
                alpha += 25;

                if (alpha > maxAlpha)
                {
                    alpha = maxAlpha;
                }

                result = true;
            }

            alphaHue = (byte)alpha;

            return result;
        }

        private static byte CalculateObjectHeight(ref int maxObjectZ, ref StaticTiles itemData)
        {
            if (itemData.Height != 0xFF /*&& itemData.Flags != 0*/)
            {
                byte height = itemData.Height;

                if (itemData.Height == 0)
                {
                    if (!itemData.IsBackground && !itemData.IsSurface)
                    {
                        height = 10;
                    }
                }

                if ((itemData.Flags & TileFlag.Bridge) != 0)
                {
                    height /= 2;
                }

                maxObjectZ += height;

                return height;
            }

            return 0xFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsFoliageVisibleAtSeason(ref StaticTiles itemData, Season season)
        {
            return !(itemData.IsFoliage && !itemData.IsMultiMovable && season >= Season.Winter);
        }

        private void PushToRenderList(GameObject obj, GameObject parent, bool island = false)
        {
#if RENDER_LIST_LINKED_LIST
            if (!_useLandList && island)
            {
                island = false;
            }

            ref GameObject first = ref (island ? ref _firstLand : ref _first);
            ref GameObject renderList = ref (island ? ref _renderListLand : ref _renderList);

            if (first == null)
            {
                first = renderList = obj;
            }
            else
            {
                renderList.RenderListNext = obj;
                renderList = obj;
            }

            obj.RenderListNext = null;

            if (island)
            {
                ++_renderListLandCount;
            }
            else
            {
                ++_renderListCount;
            }
#else
            if (_renderListCount >= _renderList.Length)
            {
                Array.Resize(ref _renderList, _renderList.Length + 1000);
            }

            ref var r = ref _renderList[_renderListCount++];

            r.Object = obj;
            r.Hue = CUOEnviroment.Debug ? (ushort)(parent != null ? 0x0044 : 300 + obj.PriorityZ) : obj.Hue;
#endif

            obj.UseInRender = (byte)_renderIndex;
        }

        private unsafe bool AddTileToRenderList(GameObject obj, int worldX, int worldY, bool useObjectHandles, int maxZ, GameObject parent = null)
        {
            for (; obj != null; obj = obj.TNext)
            {
                if (obj.CurrentRenderIndex == _renderIndex)
                {
                    continue;
                }

                if (UpdateDrawPosition && obj.CurrentRenderIndex != _renderIndex || obj.IsPositionChanged)
                {
                    obj.UpdateRealScreenPosition(_offset.X, _offset.Y);
                }

                obj.UseInRender = 0xFF;

                int screenX = obj.RealScreenPosition.X;

                if (screenX < _minPixel.X || screenX > _maxPixel.X)
                {
                    break;
                }

                int screenY = obj.RealScreenPosition.Y;
                int maxObjectZ = obj.PriorityZ;

#if !OK
                if (obj is Land land)
                {
                    if (maxObjectZ > maxZ)
                    {
                        return false;
                    }

                    obj.CurrentRenderIndex = _renderIndex;

                    if (screenY > _maxPixel.Y)
                    {
                        continue;
                    }

                    if (land.IsStretched)
                    {
                        screenY += (land.Z << 2);
                        screenY -= (land.MinZ << 2);
                    }

                    if (screenY < _minPixel.Y)
                    {
                        continue;
                    }

                    PushToRenderList(obj, parent, true);
                }
                else if (obj is Static staticc)
                {
                    ref var itemData = ref staticc.ItemData;

                    if (itemData.IsInternal)
                    {
                        continue;
                    }

                    if (!IsFoliageVisibleAtSeason(ref itemData, World.Season))
                    {
                        continue;
                    }

                    if (!ProcessAlpha(obj, ref itemData))
                    {
                        continue;
                    }

                    //we avoid to hide impassable foliage or bushes, if present...
                    if (itemData.IsFoliage && ProfileManager.CurrentProfile.TreeToStumps)
                    {
                        continue;
                    }

                    if (!itemData.IsMultiMovable && staticc.IsVegetation && ProfileManager.CurrentProfile.HideVegetation)
                    {
                        continue;
                    }

                    byte height = 0;

                    if (obj.AllowedToDraw)
                    {
                        height = CalculateObjectHeight(ref maxObjectZ, ref itemData);
                    }

                    if (maxObjectZ > maxZ)
                    {
                        return itemData.Height != 0 && maxObjectZ - maxZ < height;
                    }

                    obj.CurrentRenderIndex = _renderIndex;

                    if (screenY < _minPixel.Y || screenY > _maxPixel.Y)
                    {
                        continue;
                    }

                    CheckIfBehindATree(obj, worldX, worldY, ref itemData);

                    PushToRenderList(obj, parent);
                }
                else if (obj is Multi multi)
                {
                    ref StaticTiles itemData = ref multi.ItemData;

                    if (itemData.IsInternal)
                    {
                        continue;
                    }

                    if (!ProcessAlpha(obj, ref itemData))
                    {
                        continue;
                    }

                    //we avoid to hide impassable foliage or bushes, if present...

                    if (!itemData.IsMultiMovable)
                    {
                        if (itemData.IsFoliage && ProfileManager.CurrentProfile.TreeToStumps)
                        {
                            continue;
                        }

                        if (multi.IsVegetation && ProfileManager.CurrentProfile.HideVegetation)
                        {
                            continue;
                        }
                    }          

                    byte height = 0;

                    if (obj.AllowedToDraw)
                    {
                        height = CalculateObjectHeight(ref maxObjectZ, ref itemData);
                    }

                    if (maxObjectZ > maxZ)
                    {
                        return itemData.Height != 0 && maxObjectZ - maxZ < height;
                    }

                    obj.CurrentRenderIndex = _renderIndex;

                    if (screenY < _minPixel.Y || screenY > _maxPixel.Y)
                    {
                        continue;
                    }

                    if (multi.IsMovable)
                    {
                        AddOffsetCharacterTileToRenderList(obj, useObjectHandles, true);
                    }

                    CheckIfBehindATree(obj, worldX, worldY, ref itemData);

                    PushToRenderList(obj, parent);
                }
                else if (obj is Mobile mobile)
                {
                    UpdateObjectHandles(mobile, useObjectHandles);

                    maxObjectZ += Constants.DEFAULT_CHARACTER_HEIGHT;

                    if (maxObjectZ > maxZ)
                    {
                        return false;
                    }

                    StaticTiles empty = default;

                    if (!ProcessAlpha(obj, ref empty))
                    {
                        continue;
                    }

                    obj.CurrentRenderIndex = _renderIndex;

                    if (screenY < _minPixel.Y || screenY > _maxPixel.Y)
                    {
                        continue;
                    }

                    AddOffsetCharacterTileToRenderList(obj, useObjectHandles, false);

                    PushToRenderList(obj, parent);
                }
                else if (obj is Item item)
                {
                    ref StaticTiles itemData = ref (item.IsMulti ? ref TileDataLoader.Instance.StaticData[item.MultiGraphic] : ref item.ItemData);

                    if (!item.IsCorpse && itemData.IsInternal)
                    {
                        continue;
                    }

                    if (item.IsCorpse || (!item.IsMulti && (!item.IsLocked || item.IsLocked && itemData.IsContainer)))
                    {
                        UpdateObjectHandles(item, useObjectHandles);
                    }

                    if (!item.IsMulti && !IsFoliageVisibleAtSeason(ref itemData, World.Season))
                    {
                        continue;
                    }

                    if (!ProcessAlpha(obj, ref itemData))
                    {
                        continue;
                    }

                    if (!itemData.IsMultiMovable && itemData.IsFoliage && ProfileManager.CurrentProfile.TreeToStumps)
                    {
                        continue;
                    }

                    byte height = 0;

                    if (obj.AllowedToDraw)
                    {
                        height = CalculateObjectHeight(ref maxObjectZ, ref itemData);
                    }

                    if (maxObjectZ > maxZ)
                    {
                        return itemData.Height != 0 && maxObjectZ - maxZ < height;
                    }

                    obj.CurrentRenderIndex = _renderIndex;

                    if (screenY < _minPixel.Y || screenY > _maxPixel.Y)
                    {
                        continue;
                    }

                    if (item.IsCorpse)
                    {
                        AddOffsetCharacterTileToRenderList(obj, useObjectHandles, false);
                    }
                    else if (itemData.IsMultiMovable)
                    {
                        AddOffsetCharacterTileToRenderList(obj, useObjectHandles, true);
                    }

                    if (!item.IsCorpse)
                    {
                        CheckIfBehindATree(obj, worldX, worldY, ref itemData);
                    }

                    PushToRenderList(obj, parent);
                }
                else if (obj is GameEffect effect)
                {
                    if (!ProcessAlpha(obj, ref TileDataLoader.Instance.StaticData[effect.Graphic]))
                    {
                        continue;
                    }

                    obj.CurrentRenderIndex = _renderIndex;

                    if (screenY < _minPixel.Y || screenY > _maxPixel.Y)
                    {
                        continue;
                    }

                    if (effect.IsMoving) // TODO: check for typeof(MovingEffect) ?
                    {
                        AddOffsetCharacterTileToRenderList(obj, useObjectHandles, true);
                    }

                    PushToRenderList(obj, parent);
                }
#else

                switch (obj)
                {
                    case Land land:

                        if (maxObjectZ > maxZ)
                        {
                            return false;
                        }

                        obj.CurrentRenderIndex = _renderIndex;

                        if (screenY > _maxPixel.Y)
                        {
                            continue;
                        }

                        if (land.IsStretched)
                        {
                            screenY += (land.Z << 2);
                            screenY -= (land.MinZ << 2);
                        }

                        if (screenY < _minPixel.Y)
                        {
                            continue;
                        }

                        PushToRenderList(obj, parent, true);

                        break;

                    case Static staticc:

                        ref StaticTiles itemData = ref staticc.ItemData;

                        if (itemData.IsInternal)
                        {
                            continue;
                        }

                        if (!IsFoliageVisibleAtSeason(ref itemData, World.Season))
                        {
                            continue;
                        }

                        if (!ProcessAlpha(obj, ref itemData))
                        {
                            continue;
                        }

                        //we avoid to hide impassable foliage or bushes, if present...
                        if (itemData.IsFoliage && ProfileManager.CurrentProfile.TreeToStumps)
                        {
                            continue;
                        }

                        if (!itemData.IsMultiMovable && staticc.IsVegetation && ProfileManager.CurrentProfile.HideVegetation)
                        {
                            continue;
                        }

                        byte height = 0;
                        if (obj.AllowedToDraw)
                        {
                            height = CalculateObjectHeight(ref maxObjectZ, ref itemData);
                        }

                        if (maxObjectZ > maxZ)
                        {
                            return itemData.Height != 0 && maxObjectZ - maxZ < height;
                        }

                        obj.CurrentRenderIndex = _renderIndex;

                        if (screenY < _minPixel.Y || screenY > _maxPixel.Y)
                        {
                            continue;
                        }

                        CheckIfBehindATree(obj, worldX, worldY, ref itemData);

                        PushToRenderList(obj, parent);

                        break;

                    case Multi multi:

                        itemData = ref multi.ItemData;

                        if (itemData.IsInternal)
                        {
                            continue;
                        }

                        if (!ProcessAlpha(obj, ref itemData))
                        {
                            continue;
                        }

                        //we avoid to hide impassable foliage or bushes, if present...
                        if (itemData.IsFoliage && ProfileManager.CurrentProfile.TreeToStumps)
                        {
                            continue;
                        }

                        if (!itemData.IsMultiMovable && multi.IsVegetation && ProfileManager.CurrentProfile.HideVegetation)
                        {
                            continue;
                        }

                        if (obj.AllowedToDraw)
                        {
                            height = CalculateObjectHeight(ref maxObjectZ, ref itemData);
                        }
                        else
                        {
                            height = 0;
                        }

                        if (maxObjectZ > maxZ)
                        {
                            return itemData.Height != 0 && maxObjectZ - maxZ < height;
                        }

                        obj.CurrentRenderIndex = _renderIndex;

                        if (screenY < _minPixel.Y || screenY > _maxPixel.Y)
                        {
                            continue;
                        }

                        if (multi.IsMovable)
                        {
                            AddOffsetCharacterTileToRenderList(obj, useObjectHandles, true);
                        }

                        CheckIfBehindATree(obj, worldX, worldY, ref itemData);

                        PushToRenderList(obj, parent);

                        break;

                    case Mobile mobile:

                        UpdateObjectHandles(mobile, useObjectHandles);

                        maxObjectZ += Constants.DEFAULT_CHARACTER_HEIGHT;

                        if (maxObjectZ > maxZ)
                        {
                            return false;
                        }

                        StaticTiles empty = default;

                        if (!ProcessAlpha(obj, ref empty))
                        {
                            continue;
                        }

                        obj.CurrentRenderIndex = _renderIndex;

                        if (screenY < _minPixel.Y || screenY > _maxPixel.Y)
                        {
                            continue;
                        }

                        AddOffsetCharacterTileToRenderList(obj, useObjectHandles, false);

                        PushToRenderList(obj, parent);

                        break;

                    case Item item:

                        itemData = ref (item.IsMulti ? ref TileDataLoader.Instance.StaticData[item.MultiGraphic] : ref item.ItemData);

                        if (!item.IsCorpse && itemData.IsInternal)
                        {
                            continue;
                        }

                        if (item.IsCorpse || (!item.IsMulti && (!item.IsLocked || item.IsLocked && itemData.IsContainer)))
                        {
                            UpdateObjectHandles(item, useObjectHandles);
                        }

                        if (!IsFoliageVisibleAtSeason(ref itemData, World.Season))
                        {
                            continue;
                        }

                        if (!ProcessAlpha(obj, ref itemData))
                        {
                            continue;
                        }

                        if (itemData.IsFoliage && ProfileManager.CurrentProfile.TreeToStumps)
                        {
                            continue;
                        }

                        if (obj.AllowedToDraw)
                        {
                            height = CalculateObjectHeight(ref maxObjectZ, ref itemData);
                        }
                        else
                        {
                            height = 0;
                        }

                        if (maxObjectZ > maxZ)
                        {
                            return itemData.Height != 0 && maxObjectZ - maxZ < height;
                        }

                        obj.CurrentRenderIndex = _renderIndex;

                        if (screenY < _minPixel.Y || screenY > _maxPixel.Y)
                        {
                            continue;
                        }

                        if (item.IsCorpse)
                        {
                            AddOffsetCharacterTileToRenderList(obj, useObjectHandles, false);
                        }
                        else if (itemData.IsMultiMovable)
                        {
                            AddOffsetCharacterTileToRenderList(obj, useObjectHandles, true);
                        }

                        if (!item.IsCorpse)
                        {
                            CheckIfBehindATree(obj, worldX, worldY, ref itemData);
                        }

                        PushToRenderList(obj, parent);

                        break;

                    case GameEffect effect:

                        if (!ProcessAlpha(obj, ref TileDataLoader.Instance.StaticData[effect.Graphic]))
                        {
                            continue;
                        }

                        obj.CurrentRenderIndex = _renderIndex;

                        if (screenY < _minPixel.Y || screenY > _maxPixel.Y)
                        {
                            continue;
                        }

                        if (effect.IsMoving) // TODO: check for typeof(MovingEffect) ?
                        {
                            AddOffsetCharacterTileToRenderList(obj, useObjectHandles, true);
                        }

                        PushToRenderList(obj, parent);

                        break;

                    default:

                        Log.Error("OBJECT NOT RENDERED --> " + obj.GetType());

                        break;
                }

#endif
            }

            return false;
        }

        private static readonly sbyte[,] _offets = new sbyte[8, 2]
        {
            { 1, -1 },
            { 1, -2 },
            { 0, 1 },
            { -1, 2 },
            { 1, 0 },
            { 1, 1 },
            { 2, -2 },
            { 2, -1 },
        };


        private void AddOffsetCharacterTileToRenderList(GameObject entity, bool useObjectHandles, bool ignoreDefaultHeightOffset)
        {
            short maxZ = entity.PriorityZ;

            /*  Rotation 45° side: --->
             *
             *      [ ][ ][ ][ ][ ][ ][ ]
             *      [ ][ ][ ][ ][1][6][ ]
             *      [ ][ ][ ][ ][0][7][ ]
             *      [ ][ ][ ][+][4][ ][ ]
             *      [ ][ ][ ][2][5][ ][ ]
             *      [ ][ ][3][ ][ ][ ][ ]
             *      [ ][ ][ ][ ][ ][ ][ ]
             *
             */

            for (int i = 0; i < 8; ++i)
            {
                int charX = entity.X + _offets[i, 0];
                int charY = entity.Y + _offets[i, 1];

                if (charX < _minTile.X || charX > _maxTile.X ||
                    charY < _minTile.Y || charY > _maxTile.Y)
                {
                    continue;
                }

                int currentMaxZ = maxZ;

                if (!ignoreDefaultHeightOffset && i <= 1)
                {
                    currentMaxZ += 20;
                }

                GameObject tile = World.Map.GetTile(charX, charY);

                if (tile != null)
                {
                    if (AddTileToRenderList
                    (
                        tile,
                        charX,
                        charY,
                        useObjectHandles,
                        currentMaxZ,
                        entity
                    ) && i >= 4)
                    {
                        break;
                    }
                }
            }
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