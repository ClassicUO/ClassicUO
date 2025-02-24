// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Map;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

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
        private Vector2 _minPixel,
            _maxPixel,
            _lastCamOffset;
        private bool _noDrawRoofs;
        private Point _offset,
            _maxTile,
            _minTile,
            _last_scaled_offset;
        private int _oldPlayerX,
            _oldPlayerY,
            _oldPlayerZ;
        private int _foliageCount;


        private readonly List<GameObject> _renderListStatics = new List<GameObject>();
        private readonly List<GameObject> _renderListTransparentObjects = new List<GameObject>();
        private readonly List<GameObject> _renderListAnimations = new List<GameObject>();
        private readonly List<GameObject> _renderListEffects = new List<GameObject>();

        //// statics
        //private GameObject _renderListStaticsHead, _renderList;
        //private int _renderListStaticsCount;

        //// lands
        //private GameObject _renderListTransparentObjectsHead, _renderListTransparentObjects;
        //private int _renderListTransparentObjectsCount;

        //// animations
        //private GameObject _renderListAnimationsHead, _renderListAnimations;
        //private int _renderListAnimationCount;

        //private GameObject _renderListEffectsHead, _renderListEffects;
        //private int _renderListEffectCount;

        public sbyte FoliageIndex { get; private set; }

        public void UpdateMaxDrawZ(bool force = false)
        {
            int playerX = _world.Player.X;
            int playerY = _world.Player.Y;
            int playerZ = _world.Player.Z;

            if (
                playerX == _oldPlayerX && playerY == _oldPlayerY && playerZ == _oldPlayerZ && !force
            )
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
            Chunk chunk = _world.Map.GetChunk(bx, by, false);

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
                            maxGroundZ = (sbyte)pz16;
                            _maxGroundZ = (sbyte)pz16;
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
                        ref StaticTiles itemdata = ref Client.Game.UO.FileManager.TileData.StaticData[
                            obj.Graphic
                        ];

                        //if (GameObjectHelper.TryGetStaticData(obj, out var itemdata) && ((ulong) itemdata.Flags & 0x20004) == 0 && (!itemdata.IsRoof || itemdata.IsSurface))
                        if (
                            ((ulong)itemdata.Flags & 0x20004) == 0
                            && (!itemdata.IsRoof || itemdata.IsSurface)
                        )
                        {
                            _maxZ = tileZ;
                            _noDrawRoofs = true;
                        }
                    }
                }

                int tempZ = _maxZ;
                _maxGroundZ = (sbyte)_maxZ;
                playerX++;
                playerY++;
                bx = playerX;
                by = playerY;
                chunk = _world.Map.GetChunk(bx, by, false);

                if (chunk != null)
                {
                    x = playerX % 8;
                    y = playerY % 8;

                    for (
                        GameObject obj2 = chunk.GetHeadObject(x, y);
                        obj2 != null;
                        obj2 = obj2.TNext
                    )
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
                                ref StaticTiles itemdata = ref Client.Game.UO.FileManager.TileData.StaticData[
                                    obj2.Graphic
                                ];

                                if (((ulong)itemdata.Flags & 0x204) == 0 && itemdata.IsRoof)
                                {
                                    _maxZ = tileZ;
                                    _world.Map.ClearBockAccess();
                                    _maxGroundZ = _world.Map.CalculateNearZ(
                                        tileZ,
                                        playerX,
                                        playerY,
                                        tileZ
                                    );
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
                    _maxGroundZ = (sbyte)pz16;
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
            GameObject tile = _world.Map.GetTile(x, y);

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
            if (useObjectHandles && _world.NameOverHeadManager.IsAllowed(obj))
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

        private void CheckIfBehindATree(
            GameObject obj,
            ref StaticTiles itemData
        )
        {
            if (obj.Z < _maxZ && itemData.IsFoliage)
            {
                if (obj.FoliageIndex != FoliageIndex)
                {
                    sbyte index = 0;

                    bool check = _world.Player.X <= obj.X && _world.Player.Y <= obj.Y;

                    if (!check)
                    {
                        check = _world.Player.Y <= obj.Y && _world.Player.X <= obj.X + 1;

                        if (!check)
                        {
                            check = _world.Player.X <= obj.X && _world.Player.Y <= obj.Y + 1;
                        }
                    }

                    if (check)
                    {
                        var rect = Client.Game.UO.Arts.GetRealArtBounds(obj.Graphic);

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

        private bool ProcessAlpha(
            GameObject obj,
            ref StaticTiles itemData,
            bool useCoT,
            ref Vector2 playerPos,
            int cotZ,
            out bool allowSelection
        )
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
                if (
                    useCoT
                    && CheckCircleOfTransparencyRadius(obj, cotZ, ref playerPos, ref allowSelection)
                ) { }
                else if (_alphaChanged && obj.AlphaHue != 0xFF)
                {
                    CalculateAlpha(ref obj.AlphaHue, 0xFF);
                }
            }

            return true;
        }

        private bool CheckCircleOfTransparencyRadius(
            GameObject obj,
            int maxZ,
            ref Vector2 playerPos,
            ref bool allowSelection
        )
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

                    obj.AlphaHue = (byte)
                        Microsoft.Xna.Framework.MathHelper.Clamp(
                            fraction * 255f,
                            byte.MinValue,
                            byte.MaxValue
                        );

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
            if (
                ProfileManager.CurrentProfile != null
                && !ProfileManager.CurrentProfile.UseObjectsFading
            )
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
            if (
                itemData.Height != 0xFF /*&& itemData.Flags != 0*/
            )
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
            if (
                obj.Serial == _world.Player.Serial /* || _maxZ == _maxGroundZ*/
            )
            {
                return false;
            }

            bool found = false;

            for (int y = -1; y <= 2; ++y)
            {
                for (int x = -1; x <= 2; ++x)
                {
                    GameObject tile = _world.Map.GetTile(obj.X + x, obj.Y + y);

                    found = false;

                    while (tile != null)
                    {
                        var next = tile.TNext;

                        if (tile.Z > obj.Z && (tile is Static || tile is Multi))
                        {
                            ref var itemData = ref Client.Game.UO.FileManager.TileData.StaticData[tile.Graphic];

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

        private void PushToRenderList(
            GameObject obj,
            List<GameObject> renderList,
            bool allowSelection
        )
        {
            if (obj.AlphaHue == 0)
            {
                return;
            }

            // slow as fuck
            if (
                allowSelection
                && obj.Z <= _maxGroundZ
                && obj.AllowedToDraw
                && obj.CheckMouseSelection()
            )
            {
                if (SelectedObject.Object is GameObject prev)
                {
                    if (obj.CalculateDepthZ() >= prev.CalculateDepthZ())
                    {
                        SelectedObject.Object = obj;
                    }
                }
                else
                {
                    SelectedObject.Object = obj;
                }
            }

            if (obj.AlphaHue != byte.MaxValue)
            {
                _renderListTransparentObjects.Add(obj);
            }
            else
            {
                renderList.Add(obj);
            }
        }

        private unsafe bool AddTileToRenderList(
            GameObject obj,
            bool useObjectHandles,
            int maxZ,
            int cotZ,
            ref Vector2 playerScreePos
        )
        {
            for (; obj != null; obj = obj.TNext)
            {
                if (UpdateDrawPosition || obj.IsPositionChanged)
                {
                    obj.UpdateRealScreenPosition(_offset.X, _offset.Y);
                }

                int screenX = obj.RealScreenPosition.X;

                if (screenX < _minPixel.X || screenX > _maxPixel.X)
                {
                    break;
                }

                int screenY = obj.RealScreenPosition.Y;
                int maxObjectZ = obj.PriorityZ;

                switch (obj)
                {
                    case Land land:
                        if (maxObjectZ > maxZ)
                        {
                            return false;
                        }

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

                        PushToRenderList(
                            obj,
                            _renderListStatics,
                            true
                        );
                        break;
                    case Static staticc:
                        {
                            ref var itemData = ref staticc.ItemData;

                            if (itemData.IsInternal)
                            {
                                continue;
                            }

                            if (!IsFoliageVisibleAtSeason(ref itemData, _world.Season))
                            {
                                continue;
                            }

                            if (
                                !ProcessAlpha(
                                    obj,
                                    ref itemData,
                                    true,
                                    ref playerScreePos,
                                    cotZ,
                                    out bool allowSelection
                                )
                            )
                            {
                                continue;
                            }

                            //we avoid to hide impassable foliage or bushes, if present...
                            if (itemData.IsFoliage && ProfileManager.CurrentProfile.TreeToStumps)
                            {
                                continue;
                            }

                            if (
                                !itemData.IsMultiMovable
                                && staticc.IsVegetation
                                && ProfileManager.CurrentProfile.HideVegetation
                            )
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

                            if (screenY < _minPixel.Y || screenY > _maxPixel.Y)
                            {
                                continue;
                            }

                            CheckIfBehindATree(obj, ref itemData);

                            // hacky way to render shadows without z-fight
                            if (
                                ProfileManager.CurrentProfile.ShadowsEnabled
                                && ProfileManager.CurrentProfile.ShadowsStatics
                                && (
                                    StaticFilters.IsTree(obj.Graphic, out _)
                                    || itemData.IsFoliage
                                    || StaticFilters.IsRock(obj.Graphic)
                                )
                            )
                            {
                                PushToRenderList(
                                    obj,
                                    _renderListTransparentObjects,
                                    allowSelection
                                );
                            }
                            else
                            {
                                PushToRenderList(
                                    obj,
                                    _renderListStatics,
                                    allowSelection
                                );
                            }

                            break;
                        }

                    case Multi multi:
                        {
                            ref StaticTiles itemData = ref multi.ItemData;

                            if (itemData.IsInternal)
                            {
                                continue;
                            }

                            if (
                                !ProcessAlpha(
                                    obj,
                                    ref itemData,
                                    true,
                                    ref playerScreePos,
                                    cotZ,
                                    out bool allowSelection
                                )
                            )
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

                            if (screenY < _minPixel.Y || screenY > _maxPixel.Y)
                            {
                                continue;
                            }

                            CheckIfBehindATree(obj, ref itemData);

                            // hacky way to render shadows without z-fight
                            if (
                                ProfileManager.CurrentProfile.ShadowsEnabled
                                && ProfileManager.CurrentProfile.ShadowsStatics
                                && (
                                    StaticFilters.IsTree(obj.Graphic, out _)
                                    || itemData.IsFoliage
                                    || StaticFilters.IsRock(obj.Graphic)
                                )
                            )
                            {
                                PushToRenderList(
                                    obj,
                                    _renderListTransparentObjects,
                                    allowSelection
                                );
                            }
                            else
                            {
                                PushToRenderList(
                                    obj,
                                    _renderListStatics,
                                    allowSelection
                                );
                            }

                            break;
                        }

                    case Mobile mobile:
                        {
                            UpdateObjectHandles(mobile, useObjectHandles);

                            maxObjectZ += Constants.DEFAULT_CHARACTER_HEIGHT;

                            if (maxObjectZ > maxZ)
                            {
                                return false;
                            }

                            StaticTiles empty = default;

                            if (
                                !ProcessAlpha(
                                    obj,
                                    ref empty,
                                    false,
                                    ref playerScreePos,
                                    cotZ,
                                    out bool allowSelection
                                )
                            )
                            {
                                continue;
                            }

                            if (screenY < _minPixel.Y || screenY > _maxPixel.Y)
                            {
                                continue;
                            }

                            obj.AllowedToDraw = !HasSurfaceOverhead(mobile);

                            PushToRenderList(
                                obj,
                                _renderListAnimations,
                                allowSelection
                            );
                            break;
                        }

                    case Item item:
                        {
                            ref StaticTiles itemData = ref (
                                item.IsMulti
                                    ? ref Client.Game.UO.FileManager.TileData.StaticData[item.MultiGraphic]
                                    : ref item.ItemData
                            );

                            if (!item.IsCorpse && itemData.IsInternal)
                            {
                                continue;
                            }

                            if (
                                item.IsCorpse
                                || (
                                    !item.IsMulti
                                    && (!item.IsLocked || item.IsLocked && itemData.IsContainer)
                                )
                            )
                            {
                                UpdateObjectHandles(item, useObjectHandles);
                            }

                            if (!item.IsMulti && !IsFoliageVisibleAtSeason(ref itemData, _world.Season))
                            {
                                continue;
                            }

                            if (
                                !ProcessAlpha(
                                    obj,
                                    ref itemData,
                                    false,
                                    ref playerScreePos,
                                    cotZ,
                                    out bool allowSelection
                                )
                            )
                            {
                                continue;
                            }

                            if (
                                !itemData.IsMultiMovable
                                && itemData.IsFoliage
                                && ProfileManager.CurrentProfile.TreeToStumps
                            )
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

                            if (screenY < _minPixel.Y || screenY > _maxPixel.Y)
                            {
                                continue;
                            }

                            if (!item.IsCorpse)
                            {
                                CheckIfBehindATree(obj, ref itemData);
                            }

                            if (item.IsCorpse)
                            {
                                PushToRenderList(
                                    obj,
                                    _renderListAnimations,
                                    allowSelection
                                );
                            }
                            else
                            {
                                PushToRenderList(
                                    obj,
                                    _renderListStatics,
                                    true
                                );
                            }

                            break;
                        }

                    case GameEffect effect:
                        if (
                                            !ProcessAlpha(
                                                obj,
                                                ref Client.Game.UO.FileManager.TileData.StaticData[effect.Graphic],
                                                false,
                                                ref playerScreePos,
                                                cotZ,
                                                out _
                                            )
                                        )
                        {
                            continue;
                        }

                        if (screenY < _minPixel.Y || screenY > _maxPixel.Y)
                        {
                            continue;
                        }

                        if (effect.IsMoving) // TODO: check for typeof(MovingEffect) ?
                        { }

                        //PushToRenderList(obj, ref _renderList, ref _renderListStaticsHead, ref _renderListStaticsCount, false);

                        PushToRenderList(
                            obj,
                            _renderListEffects,
                            false
                        );
                        break;
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
            int winGameWidth = Camera.Bounds.Width;
            int winGameHeight = Camera.Bounds.Height;
            int winGameCenterX = winGamePosX + (winGameWidth >> 1);
            int winGameCenterY = winGamePosY + (winGameHeight >> 1) + (_world.Player.Z << 2);
            winGameCenterX -= (int)_world.Player.Offset.X;
            winGameCenterY -= (int)(_world.Player.Offset.Y - _world.Player.Offset.Z);

            int tileOffX = _world.Player.X;
            int tileOffY = _world.Player.Y;

            int winDrawOffsetX = (tileOffX - tileOffY) * 22 - winGameCenterX;
            int winDrawOffsetY = (tileOffX + tileOffY) * 22 - winGameCenterY;

            int winGameScaledOffsetX;
            int winGameScaledOffsetY;
            int winGameScaledWidth;
            int winGameScaledHeight;

            if (zoom != 1f)
            {
                float left = winGamePosX;
                float right = winGameWidth + left;
                float top = winGamePosY;
                float bottom = winGameHeight + top;
                float newRight = right * zoom;
                float newBottom = bottom * zoom;

                winGameScaledOffsetX = (int)(left * zoom - (newRight - right));
                winGameScaledOffsetY = (int)(top * zoom - (newBottom - bottom));
                winGameScaledWidth = (int)(newRight - winGameScaledOffsetX);
                winGameScaledHeight = (int)(newBottom - winGameScaledOffsetY);
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

            int size = (int)(Math.Max(winGameWidth / 44f + 1, winGameHeight / 44f + 1) * zoom);

            if (Camera.Offset.X != 0 || Camera.Offset.Y != 0)
            {
                tileOffX += (int)(zoom * (Camera.Offset.X + Camera.Offset.Y) / 44);
                tileOffY += (int)(zoom * (Camera.Offset.Y - Camera.Offset.X) / 44);
            }
            ;

            int realMinRangeX = Math.Max(0, tileOffX - size);
            int realMaxRangeX = tileOffX + size;
            int realMinRangeY = Math.Max(0, tileOffY - size);
            int realMaxRangeY = tileOffY + size;

            int drawOffset = (int)(44 / zoom);

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

            if (
                UpdateDrawPosition
                || oldDrawOffsetX != winDrawOffsetX
                || oldDrawOffsetY != winDrawOffsetY
                || old_scaled_offset.X != winGameScaledOffsetX
                || old_scaled_offset.Y != winGameScaledOffsetY
                || _lastCamOffset != Camera.Offset
            )
            {
                UpdateDrawPosition = true;
                _lastCamOffset = Camera.Offset;

                if (
                    _use_render_target
                    && (
                        _world_render_target == null
                        || _world_render_target.Width != (int)(winGameWidth * zoom)
                        || _world_render_target.Height != (int)(winGameHeight * zoom)
                    )
                )
                {
                    _world_render_target?.Dispose();

                    PresentationParameters pp = Client.Game.GraphicsDevice.PresentationParameters;

                    _world_render_target = new RenderTarget2D(
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

                if (
                    _lightRenderTarget == null
                    || _lightRenderTarget.Width != winGameWidth
                    || _lightRenderTarget.Height != winGameHeight
                )
                {
                    _lightRenderTarget?.Dispose();

                    PresentationParameters pp = Client.Game.GraphicsDevice.PresentationParameters;

                    _lightRenderTarget = new RenderTarget2D(
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
