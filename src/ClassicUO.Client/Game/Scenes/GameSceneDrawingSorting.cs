// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Map;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System;
using System.Runtime.CompilerServices;

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
        private float _cotRadiusSq;
        private Vector2 _cotPlayerScreenPos;
        private bool _cotGradientMode;

        private readonly RenderLists _renderLists = new();
        private readonly List<Map.Chunk> _visibleChunks = new();

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
            ref readonly StaticTiles itemData,
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
            else if (_alphaChanged && obj.AlphaHue != 0xFF && !itemData.IsFoliage)
            {
                CalculateAlpha(ref obj.AlphaHue, 0xFF);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte GetGradientCotAlpha(GameObject obj)
        {
            float dx = obj.RealScreenPosition.X - _cotPlayerScreenPos.X;
            float dy = (obj.RealScreenPosition.Y - 44) - _cotPlayerScreenPos.Y;
            float distSq = dx * dx + dy * dy;

            if (distSq >= _cotRadiusSq)
                return 0xFF;

            float ratio = (float)Math.Sqrt(distSq / _cotRadiusSq);
            return (byte)(ratio * ratio * ratio * 255f);
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

        private bool HasSurfaceOverhead(Mobile mob)
        {
            if (
                mob.Serial == _world.Player.Serial /* || _maxZ == _maxGroundZ*/
            )
            {
                return false;
            }

            if (mob._surfaceOverheadCacheX == mob.X && mob._surfaceOverheadCacheY == mob.Y && mob._surfaceOverheadCacheMaxZ == _maxZ)
            {
                return mob._surfaceOverheadCache;
            }

            bool found = false;

            for (int y = -1; y <= 2; ++y)
            {
                for (int x = -1; x <= 2; ++x)
                {
                    GameObject tile = _world.Map.GetTile(mob.X + x, mob.Y + y);

                    found = false;

                    while (tile != null)
                    {
                        var next = tile.TNext;

                        if (tile.Z > mob.Z && (tile is Static || tile is Multi))
                        {
                            ref var itemData = ref Client.Game.UO.FileManager.TileData.StaticData[tile.Graphic];

                            if (itemData.IsNoShoot || itemData.IsWindow)
                            {
                                if (_maxZ - tile.Z + 5 >= tile.Z - mob.Z)
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
                        break;
                    }
                }

                if (!found)
                {
                    break;
                }
            }

            mob._surfaceOverheadCacheX = (ushort)mob.X;
            mob._surfaceOverheadCacheY = (ushort)mob.Y;
            mob._surfaceOverheadCacheMaxZ = _maxZ;
            mob._surfaceOverheadCache = found;

            return found;
        }

        // Returns: 0 = break (handled), 1 = continue (skip), 2 = return retValue from AddTileToRenderList
        private int ProcessStaticLikeTail(
            GameObject obj,
            ref StaticTiles itemData,
            bool allowSelection,
            int screenY,
            ref int maxObjectZ,
            int maxZ,
            out bool retValue,
            ChunkMesh mesh
        )
        {
            retValue = false;

            byte height = 0;

            if (obj.AllowedToDraw)
            {
                height = CalculateObjectHeight(ref maxObjectZ, ref itemData);
            }

            if (maxObjectZ > maxZ)
            {
                retValue = itemData.Height != 0 && maxObjectZ - maxZ < height;
                return 2;
            }

            if (screenY < _minPixel.Y || screenY > _maxPixel.Y)
            {
                return 1;
            }

            // If in chunk mesh, mark visible instead of adding to render list
            if (obj.InChunkMesh && obj.MeshSpriteIndex >= 0)
            {
                bool cot = ProfileManager.CurrentProfile.UseCircleOfTransparency
                    && obj.TransparentTest(_world.Player.Z + 5);

                if (cot && _cotGradientMode)
                {
                    obj.AlphaHue = GetGradientCotAlpha(obj);
                    if (obj.AlphaHue > 0)
                        PushToRenderQueue(obj, true, allowSelection);
                    return 0;
                }

                mesh.Statics.SetVisible(obj.MeshSpriteIndex, obj.AlphaHue, cot);
                ApplyMeshHue(obj, mesh.Statics);

                if (itemData.IsLight)
                {
                    AddLight(obj, obj, obj.RealScreenPosition.X + 22, obj.RealScreenPosition.Y + 22);
                }

                if (allowSelection && !(cot && IsMouseInsideCotCircle()) && obj.AllowedToDraw && obj.CheckMouseSelection())
                {
                    if (SelectedObject.Object is GameObject prev)
                    {
                        if (obj.CalculateDepthZ() >= prev.CalculateDepthZ())
                            SelectedObject.Object = obj;
                    }
                    else
                        SelectedObject.Object = obj;
                }
                return 0;
            }

            CheckIfBehindATree(obj, ref itemData);

            // Gradient CoT for non-mesh objects (trees, foliage, animated statics)
            if (_cotGradientMode && ProfileManager.CurrentProfile.UseCircleOfTransparency
                && obj.TransparentTest(_world.Player.Z + 5))
            {
                obj.AlphaHue = GetGradientCotAlpha(obj);
                if (obj.AlphaHue > 0)
                    PushToRenderQueue(obj, true, allowSelection);
                return 0;
            }

            // hacky way to render shadows without z-fight
            bool isShadow =
                ProfileManager.CurrentProfile.ShadowsEnabled
                && ProfileManager.CurrentProfile.ShadowsStatics
                && (
                    StaticFilters.IsTree(obj.Graphic, out _)
                    || itemData.IsFoliage
                    || StaticFilters.IsRock(obj.Graphic)
                );

            PushToRenderQueue(obj, isShadow, allowSelection);
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ApplyMeshHue(GameObject obj, MeshLayer layer)
        {
            var profile = ProfileManager.CurrentProfile;
            int hue = obj.Hue;
            bool partial = false;

            if (profile.NoColorObjectsOutOfRange && obj.Distance > _world.ClientViewRange)
            {
                hue = Constants.OUT_RANGE_COLOR;
            }
            else if (_world.Player.IsDead && profile.EnableBlackWhiteEffect)
            {
                hue = Constants.DEAD_RANGE_COLOR;
            }
            else if (obj is Static s)
            {
                partial = s.ItemData.IsPartialHue;
            }
            else if (obj is Multi m)
            {
                partial = m.ItemData.IsPartialHue;
            }

            float hueX, hueY;
            if (hue != 0)
            {
                hueX = hue - 1;
                if (obj is Land land && land.IsStretched)
                    hueY = ShaderHueTranslator.SHADER_LAND_HUED;
                else
                    hueY = partial ? ShaderHueTranslator.SHADER_PARTIAL_HUED : ShaderHueTranslator.SHADER_HUED;
            }
            else
            {
                hueX = 0;
                if (obj is Land land && land.IsStretched)
                    hueY = ShaderHueTranslator.SHADER_LAND;
                else
                    hueY = ShaderHueTranslator.SHADER_NONE;
            }

            layer.SetHue(obj.MeshSpriteIndex, hueX, hueY);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsMouseInsideCotCircle()
        {
            if (_cotRadiusSq <= 0)
                return false;
            float dx = SelectedObject.TranslatedMousePositionByViewport.X - _cotPlayerScreenPos.X;
            float dy = SelectedObject.TranslatedMousePositionByViewport.Y - _cotPlayerScreenPos.Y;
            return (dx * dx + dy * dy) < _cotRadiusSq;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void TrySelectObject(GameObject obj, bool allowSelection)
        {
            if (allowSelection && obj.AllowedToDraw && obj.CheckMouseSelection())
            {
                if (SelectedObject.Object is GameObject prev)
                {
                    if (obj.CalculateDepthZ() >= prev.CalculateDepthZ())
                        SelectedObject.Object = obj;
                }
                else
                    SelectedObject.Object = obj;
            }
        }

        private void PushToRenderQueue(
            GameObject obj,
            bool isTransparent,
            bool allowSelection
        )
        {
            if (obj.AlphaHue == 0)
            {
                return;
            }

            if (
                allowSelection
                && obj.Z <= _maxGroundZ
                && obj.AllowedToDraw
                && !(ProfileManager.CurrentProfile.UseCircleOfTransparency
                    && obj.TransparentTest(_world.Player.Z + 5)
                    && IsMouseInsideCotCircle())
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

            _renderLists.Add(obj, isTransparent || obj.AlphaHue != byte.MaxValue);
        }

        private unsafe bool AddTileToRenderList(
            GameObject obj,
            bool useObjectHandles,
            int maxZ,
            Chunk chunk
        )
        {
            var profile = ProfileManager.CurrentProfile;
            var mesh = chunk.Mesh;

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

                // Fast path: meshed objects (statics, multis, land) skip type-switch entirely
                if (obj.InChunkMesh)
                {
                    if (obj is Land meshLand)
                    {
                        // For stretched tiles, the visible area extends below screenY
                        // based on MinZ, so use adjustedY for the top-of-screen cull.
                        if (meshLand.IsStretched)
                        {
                            int adjustedY = screenY + (meshLand.Z << 2) - (meshLand.MinZ << 2);
                            if (adjustedY < _minPixel.Y || screenY > _maxPixel.Y)
                                continue;
                        }
                        else if (screenY < _minPixel.Y || screenY > _maxPixel.Y)
                        {
                            continue;
                        }

                        if (maxObjectZ > maxZ)
                            return false;

                        // Simplified alpha for land (no itemData needed)
                        if (obj.Z > _maxGroundZ)
                        {
                            bool changed = _alphaChanged
                                ? CalculateAlpha(ref obj.AlphaHue, 0)
                                : obj.AlphaHue != 0;

                            if (!changed)
                                break;
                        }
                        else if (_alphaChanged && obj.AlphaHue != 0xFF)
                        {
                            CalculateAlpha(ref obj.AlphaHue, 0xFF);
                        }

                        if (obj.AlphaHue != 0)
                        {
                            mesh.Land.SetVisible(obj.MeshSpriteIndex, obj.AlphaHue);
                            ApplyMeshHue(obj, mesh.Land);
                            TrySelectObject(obj, true);
                        }
                        continue;
                    }

                    if (screenY < _minPixel.Y || screenY > _maxPixel.Y)
                        continue;

                    // Static or Multi — meshed objects are never foliage/trees/internal/animated
                    ref StaticTiles meshItemData = ref (obj is Static meshStatic
                        ? ref meshStatic.ItemData
                        : ref Unsafe.As<Multi>(obj).ItemData);

                    // Simplified ProcessAlpha for meshed statics: skip IsFoliage branch (never true)
                    bool meshAllowSelection = true;
                    bool meshFadingOut = false;
                    if (obj.Z >= _maxZ)
                    {
                        bool changed = _alphaChanged
                            ? CalculateAlpha(ref obj.AlphaHue, 0)
                            : obj.AlphaHue != 0;

                        if (!changed)
                            continue;

                        meshFadingOut = true;
                        meshAllowSelection = false;
                    }
                    else if (_noDrawRoofs && meshItemData.IsRoof)
                    {
                        if (_alphaChanged && !CalculateAlpha(ref obj.AlphaHue, 0))
                            continue;
                        if (obj.AlphaHue == 0)
                            continue;

                        meshFadingOut = true;
                        meshAllowSelection = false;
                    }
                    else if (meshItemData.IsTranslucent)
                    {
                        if (_alphaChanged)
                            CalculateAlpha(ref obj.AlphaHue, 178);
                    }
                    else if (_alphaChanged && obj.AlphaHue != 0xFF)
                    {
                        CalculateAlpha(ref obj.AlphaHue, 0xFF);
                    }

                    if (obj.AlphaHue == 0)
                        continue;

                    // Z-height culling
                    if (obj.AllowedToDraw)
                        CalculateObjectHeight(ref maxObjectZ, ref meshItemData);

                    if (maxObjectZ > maxZ)
                        continue;

                    // Fading statics must not be drawn from the mesh GPU buffer because
                    // they write to the depth buffer and block objects underneath (mobiles, items).
                    // Instead, draw them via the CPU transparent list (rendered after mobiles).
                    if (meshFadingOut)
                    {
                        PushToRenderQueue(obj, true, false);
                        continue;
                    }

                    bool meshCot = ProfileManager.CurrentProfile.UseCircleOfTransparency
                        && obj.TransparentTest(_world.Player.Z + 5);

                    // Gradient CoT: set alpha on CPU and route to transparent list
                    // so depth buffer doesn't block mobiles underneath.
                    if (meshCot && _cotGradientMode)
                    {
                        obj.AlphaHue = GetGradientCotAlpha(obj);
                        if (obj.AlphaHue > 0)
                            PushToRenderQueue(obj, true, meshAllowSelection);
                        continue;
                    }

                    mesh.Statics.SetVisible(obj.MeshSpriteIndex, obj.AlphaHue, meshCot);
                    ApplyMeshHue(obj, mesh.Statics);

                    if (meshItemData.IsLight)
                    {
                        AddLight(obj, obj, obj.RealScreenPosition.X + 22, obj.RealScreenPosition.Y + 22);
                    }

                    TrySelectObject(obj, meshAllowSelection && !(meshCot && IsMouseInsideCotCircle()));
                    continue;
                }

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

                        PushToRenderQueue(
                            obj,
                            false,
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
                                    out bool allowSelection
                                )
                            )
                            {
                                continue;
                            }

                            if (itemData.IsFoliage && profile.TreeToStumps)
                            {
                                continue;
                            }

                            if (
                                !itemData.IsMultiMovable
                                && staticc.IsVegetation
                                && profile.HideVegetation
                            )
                            {
                                continue;
                            }

                            int cf = ProcessStaticLikeTail(obj, ref itemData, allowSelection, screenY, ref maxObjectZ, maxZ, out bool retVal, mesh);
                            if (cf == 1) continue;
                            if (cf == 2) return retVal;
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
                                    out bool allowSelection
                                )
                            )
                            {
                                continue;
                            }

                            if (!itemData.IsMultiMovable)
                            {
                                if (itemData.IsFoliage && profile.TreeToStumps)
                                {
                                    continue;
                                }

                                if (multi.IsVegetation && profile.HideVegetation)
                                {
                                    continue;
                                }
                            }

                            int cf = ProcessStaticLikeTail(obj, ref itemData, allowSelection, screenY, ref maxObjectZ, maxZ, out bool retVal, mesh);
                            if (cf == 1) continue;
                            if (cf == 2) return retVal;
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

                            PushToRenderQueue(
                                obj,
                                false,
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
                                    out bool allowSelection
                                )
                            )
                            {
                                continue;
                            }

                            if (
                                !itemData.IsMultiMovable
                                && itemData.IsFoliage
                                && profile.TreeToStumps
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
                                PushToRenderQueue(
                                    obj,
                                    false,
                                    allowSelection
                                );
                            }
                            else
                            {
                                PushToRenderQueue(
                                    obj,
                                    false,
                                    true
                                );
                            }

                            break;
                        }

                    case GameEffect effect:
                        if (effect is not LightningEffect &&
                            !ProcessAlpha(
                                obj,
                                ref Client.Game.UO.FileManager.TileData.StaticData[effect.Graphic],
                                out _
                            ))
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

                        PushToRenderQueue(
                            obj,
                            false,
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
