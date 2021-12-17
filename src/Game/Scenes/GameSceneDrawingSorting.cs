#region license

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
        private int _foliageCount;
      

        // statics
        private GameObject _renderListStaticsHead, _renderList;
        private int _renderListStaticsCount;

        // lands
        private GameObject _renderListTransparentObjectsHead, _renderListTransparentObjects;
        private int _renderListTransparentObjectsCount;

        // animations
        private GameObject _renderListAnimationsHead, _renderListAnimations;
        private int _renderListAnimationCount;





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
                        var rect = ArtLoader.Instance.GetRealArtBounds(obj.Graphic);

                        rect.X = obj.RealScreenPosition.X - (rect.Width >> 1) + rect.X;
                        rect.Y = obj.RealScreenPosition.Y - rect.Height + rect.Y;

                        check = Exstentions.InRect(ref rect, ref _rectanglePlayer);

                        if (check)
                        {
                            index = FoliageIndex;
                            IsFoliageUnion(obj.Graphic, obj.X, obj.Y, obj.Z);
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

        private bool ProcessAlpha(GameObject obj, ref StaticTiles itemData, bool useCoT, ref Vector2 playerPos, int cotZ, out bool allowSelection)
        {
            allowSelection = true;

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
            else if (!itemData.IsFoliage)
            {
                if (useCoT && CheckCircleOfTransparencyRadius(obj, cotZ, ref playerPos, ref allowSelection))
                {
                }
                else if (_alphaChanged && obj.AlphaHue != 0xFF)
                {
                    CalculateAlpha(ref obj.AlphaHue, 0xFF);
                }
            }

            return true;
        }

        private bool CheckCircleOfTransparencyRadius(GameObject obj, int maxZ, ref Vector2 playerPos, ref bool allowSelection)
        {
            if (ProfileManager.CurrentProfile.UseCircleOfTransparency && obj.TransparentTest(maxZ))
            {
                int maxDist = ProfileManager.CurrentProfile.CircleOfTransparencyRadius + 0;
                Vector2 pos = new Vector2(obj.RealScreenPosition.X, obj.RealScreenPosition.Y - 44);
                Vector2.Distance(ref playerPos, ref pos, out float dist);

                if (dist <= maxDist)
                {
                    float delta = (maxDist - 44) * 0.5f;
                    float fraction = (dist - delta) / (maxDist - delta);

                    obj.AlphaHue = (byte)Microsoft.Xna.Framework.MathHelper.Clamp(fraction * 255f, byte.MinValue, byte.MaxValue);

                    //const byte ALPHA_ERROR = 44;

                    //if (obj.AlphaHue > ALPHA_ERROR && obj.AlphaHue >= byte.MaxValue - ALPHA_ERROR)
                    //{
                    //    obj.AlphaHue = 255;

                    //    return false;
                    //}

                    allowSelection = obj.AlphaHue >= 127;

                    return true;
                }
            }

            return false;
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

        private bool HasSurfaceOverhead(Entity obj)
        {
            if (obj.Serial == World.Player.Serial/* || _maxZ == _maxGroundZ*/)
            {
                return false;
            }
            
            bool found = false;
            
            for (int y = -1; y <= 2; ++y)
            {
                for (int x = -1; x <= 2; ++x)
                {
                    GameObject tile = World.Map.GetTile(obj.X + x, obj.Y + y);

                    found = false;

                    while (tile != null)
                    {
                        var next = tile.TNext;

                        if (tile.Z > obj.Z && (tile is Static || tile is Multi))
                        {
                            ref var itemData = ref TileDataLoader.Instance.StaticData[tile.Graphic];

                            if (itemData.IsNoShoot || itemData.IsWindow)
                            {
                                if (_maxZ - tile.Z + 5 >= tile.Z - obj.Z)
                                {
                                    found = true;

                                    break;
                                }
                            }
                        }

                        tile = next;
                    }

                    if (!found)
                    {
                        return false;
                    }
                }
            }

            return found;
        }

        private void PushToRenderList(GameObject obj, ref GameObject renderList, ref GameObject first, ref int renderListCount, bool allowSelection)
        {
            if (obj.AlphaHue == 0)
            {
                return;
            }

            // slow as fuck
            if (allowSelection && obj.Z <= _maxGroundZ && obj.AllowedToDraw && obj.CheckMouseSelection())
            {
                SelectedObject.Object = obj;
            }

            if (obj.AlphaHue != byte.MaxValue)
            {
                if (_renderListTransparentObjectsHead == null)
                {
                    _renderListTransparentObjectsHead = _renderListTransparentObjects = obj;
                }
                else
                {
                    _renderListTransparentObjects.RenderListNext = obj;
                    _renderListTransparentObjects = obj;
                }

                obj.RenderListNext = null;

                ++_renderListTransparentObjectsCount;
            }
            else
            {
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

                ++renderListCount;
            }


            obj.UseInRender = (byte)_renderIndex;
        }

        private unsafe bool AddTileToRenderList
        (
            GameObject obj, 
            int worldX,
            int worldY,
            bool useObjectHandles, 
            int maxZ,
            int cotZ, 
            ref Vector2 playerScreePos
        )
        {
            for (; obj != null; obj = obj.TNext)
            {
                // i think we can remove this property. It's used to the "odd sorting system"
                //if (obj.CurrentRenderIndex == _renderIndex)
                //{
                //    continue;
                //}

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

                    PushToRenderList(obj, ref _renderList, ref _renderListStaticsHead, ref _renderListStaticsCount, true);
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

                    if (!ProcessAlpha(obj, ref itemData, true, ref playerScreePos, cotZ, out bool allowSelection))
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

                    // hacky way to render shadows without z-fight
                    if (ProfileManager.CurrentProfile.ShadowsEnabled && ProfileManager.CurrentProfile.ShadowsStatics && (StaticFilters.IsTree(obj.Graphic, out _) || itemData.IsFoliage || StaticFilters.IsRock(obj.Graphic)))
                    {
                        PushToRenderList(obj, ref _renderListTransparentObjects, ref _renderListTransparentObjectsHead, ref _renderListTransparentObjectsCount, allowSelection);
                    }
                    else
                    {
                        var alpha = obj.AlphaHue;

                        // hack to fix transparent objects at the same level of a opaque one
                        if (itemData.IsTranslucent || itemData.IsTransparent)
                        {
                            obj.AlphaHue = 0xFF;
                        }

                        PushToRenderList(obj, ref _renderList, ref _renderListStaticsHead, ref _renderListStaticsCount, allowSelection);

                        obj.AlphaHue = alpha;
                    } 
                }
                else if (obj is Multi multi)
                {
                    ref StaticTiles itemData = ref multi.ItemData;

                    if (itemData.IsInternal)
                    {
                        continue;
                    }

                    if (!ProcessAlpha(obj, ref itemData, true, ref playerScreePos, cotZ, out bool allowSelection))
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

                    CheckIfBehindATree(obj, worldX, worldY, ref itemData);

                    // hacky way to render shadows without z-fight
                    if (ProfileManager.CurrentProfile.ShadowsEnabled && ProfileManager.CurrentProfile.ShadowsStatics && (StaticFilters.IsTree(obj.Graphic, out _) || itemData.IsFoliage || StaticFilters.IsRock(obj.Graphic)))
                    {
                        PushToRenderList(obj, ref _renderListTransparentObjects, ref _renderListTransparentObjectsHead, ref _renderListTransparentObjectsCount, allowSelection);
                    }
                    else
                    {
                        var alpha = obj.AlphaHue;

                        // hack to fix transparent objects at the same level of a opaque one
                        if (itemData.IsTranslucent || itemData.IsTransparent)
                        {
                            obj.AlphaHue = 0xFF;
                        }

                        PushToRenderList(obj, ref _renderList, ref _renderListStaticsHead, ref _renderListStaticsCount, allowSelection);

                        obj.AlphaHue = alpha;
                    }
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

                    if (!ProcessAlpha(obj, ref empty, false, ref playerScreePos, cotZ, out bool allowSelection))
                    {
                        continue;
                    }

                    obj.CurrentRenderIndex = _renderIndex;

                    if (screenY < _minPixel.Y || screenY > _maxPixel.Y)
                    {
                        continue;
                    }

                    obj.AllowedToDraw = !HasSurfaceOverhead(mobile);

                    PushToRenderList(obj, ref _renderListAnimations, ref _renderListAnimationsHead, ref _renderListAnimationCount, allowSelection);
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

                    if (!ProcessAlpha(obj, ref itemData, false, ref playerScreePos, cotZ, out bool allowSelection))
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
                    }
                    else if (itemData.IsMultiMovable)
                    {
                    }

                    if (!item.IsCorpse)
                    {
                        CheckIfBehindATree(obj, worldX, worldY, ref itemData);
                    }

                    if (item.IsCorpse)
                    {
                        PushToRenderList(obj, ref _renderListAnimations, ref _renderListAnimationsHead, ref _renderListAnimationCount, allowSelection);
                    }
                    else
                    {
                        PushToRenderList(obj, ref _renderList, ref _renderListStaticsHead, ref _renderListStaticsCount, true);
                    }         
                }
                else if (obj is GameEffect effect)
                {
                    if (!ProcessAlpha(obj, ref TileDataLoader.Instance.StaticData[effect.Graphic], false, ref playerScreePos, cotZ, out _))
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
                    }

                    PushToRenderList(obj, ref _renderList, ref _renderListStaticsHead, ref _renderListStaticsCount, false);
                }
            }

            return false;
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