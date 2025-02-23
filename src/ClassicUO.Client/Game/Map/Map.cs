// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ClassicUO.Game.GameObjects;
using ClassicUO.Assets;

namespace ClassicUO.Game.Map
{
    internal sealed class Map
    {
        private static Chunk[] _terrainChunks;
        private static readonly bool[] _blockAccessList = new bool[0x1000];
        private readonly LinkedList<int> _usedIndices = new LinkedList<int>();
        private readonly World _world;


        public Map(World world, int index)
        {
            _world = world;
            Index = index;
            BlocksCount = Client.Game.UO.FileManager.Maps.MapBlocksSize[Index, 0] * Client.Game.UO.FileManager.Maps.MapBlocksSize[Index, 1];

            if (_terrainChunks == null || BlocksCount > _terrainChunks.Length)
                _terrainChunks = new Chunk[BlocksCount];

            ClearBockAccess();
        }

        public readonly int BlocksCount;
        public readonly int Index;


        public Chunk GetChunk(int block)
        {
            if (block >= 0 && block < BlocksCount)
            {
                return _terrainChunks[block];
            }

            return null;
        }

        public Chunk GetChunk(int x, int y, bool load = true)
        {
            if (x < 0 || y < 0)
            {
                return null;
            }

            int cellX = x >> 3;
            int cellY = y >> 3;

            return GetChunk2(cellX, cellY, load);
        }

        public Chunk GetChunk2(int chunkX, int chunkY, bool load = true)
        {
            int block = GetBlock(chunkX, chunkY);

            if (block >= BlocksCount || block >= _terrainChunks.Length)
            {
                return null;
            }

            ref Chunk chunk = ref _terrainChunks[block];

            if (chunk == null)
            {
                if (!load)
                {
                    return null;
                }

                LinkedListNode<int> node = _usedIndices.AddLast(block);
                chunk = Chunk.Create(_world, chunkX, chunkY);
                chunk.Load(Index);
                chunk.Node = node;
            }
            else if (chunk.IsDestroyed)
            {
                // make sure node is clear
                if (chunk.Node != null && (chunk.Node.Previous != null || chunk.Node.Next != null))
                {
                    chunk.Node.List?.Remove(chunk.Node);
                }

                LinkedListNode<int> node = _usedIndices.AddLast(block);
                chunk.X = chunkX;
                chunk.Y = chunkY;
                chunk.Load(Index);
                chunk.Node = node;
            }

            chunk.LastAccessTime = Time.Ticks;

            return chunk;
        }


        public GameObject GetTile(int x, int y, bool load = true)
        {
            return GetChunk(x, y, load)?.GetHeadObject(x % 8, y % 8);
        }

        public sbyte GetTileZ(int x, int y)
        {
            if (x < 0 || y < 0)
            {
                return -125;
            }

            ref var blockIndex = ref GetIndex(x >> 3, y >> 3);

            if (!blockIndex.IsValid())
            {
                return -125;
            }

            int mx = x % 8;
            int my = y % 8;

            unsafe
            {
                blockIndex.MapFile.Seek((long)blockIndex.MapAddress, System.IO.SeekOrigin.Begin);
                return blockIndex.MapFile.Read<MapBlock>().Cells[(my << 3) + mx].Z;
            }
        }

        public void GetMapZ(int x, int y, out sbyte groundZ, out sbyte staticZ)
        {
            Chunk chunk = GetChunk(x, y);
            //var obj = GetTile(x, y);
            groundZ = staticZ = 0;

            if (chunk == null)
            {
                return;
            }

            GameObject obj = chunk.Tiles[x % 8, y % 8];

            while (obj != null)
            {
                if (obj is Land)
                {
                    groundZ = obj.Z;
                }
                else if (staticZ < obj.Z)
                {
                    staticZ = obj.Z;
                }

                obj = obj.TNext;
            }
        }

        public void ClearBockAccess()
        {
            _blockAccessList.AsSpan().Fill(false);
        }

        public sbyte CalculateNearZ(sbyte defaultZ, int x, int y, int z)
        {
            ref bool access = ref _blockAccessList[(x & 0x3F) + ((y & 0x3F) << 6)];

            if (access)
            {
                return defaultZ;
            }

            access = true;
            Chunk chunk = GetChunk(x, y, false);

            if (chunk != null)
            {
                GameObject obj = chunk.Tiles[x % 8, y % 8];

                for (; obj != null; obj = obj.TNext)
                {
                    if (!(obj is Static) && !(obj is Multi))
                    {
                        continue;
                    }

                    if (obj.Graphic >= Client.Game.UO.FileManager.TileData.StaticData.Length)
                    {
                        continue;
                    }

                    if (!Client.Game.UO.FileManager.TileData.StaticData[obj.Graphic].IsRoof || Math.Abs(z - obj.Z) > 6)
                    {
                        continue;
                    }

                    break;
                }

                if (obj == null)
                {
                    return defaultZ;
                }

                sbyte tileZ = obj.Z;

                if (tileZ < defaultZ)
                {
                    defaultZ = tileZ;
                }

                defaultZ = CalculateNearZ(defaultZ, x - 1, y, tileZ);
                defaultZ = CalculateNearZ(defaultZ, x + 1, y, tileZ);
                defaultZ = CalculateNearZ(defaultZ, x, y - 1, tileZ);
                defaultZ = CalculateNearZ(defaultZ, x, y + 1, tileZ);
            }

            return defaultZ;
        }


        public ref IndexMap GetIndex(int blockX, int blockY)
        {
            int block = GetBlock(blockX, blockY);
            int map = Index;
            Client.Game.UO.FileManager.Maps.SanitizeMapIndex(ref map);
            IndexMap[] list = Client.Game.UO.FileManager.Maps.BlockData[map];

            return ref block >= list.Length ? ref IndexMap.Invalid : ref list[block];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetBlock(int blockX, int blockY)
        {
            return blockX * Client.Game.UO.FileManager.Maps.MapBlocksSize[Index, 1] + blockY;
        }

        public IEnumerable<Chunk> GetUsedChunks()
        {
            foreach (int i in _usedIndices)
            {
                yield return GetChunk(i);
            }
        }


        public void ClearUnusedBlocks()
        {
            int count = 0;
            long ticks = Time.Ticks - Constants.CLEAR_TEXTURES_DELAY;

            LinkedListNode<int> first = _usedIndices.First;

            while (first != null)
            {
                LinkedListNode<int> next = first.Next;

                ref Chunk block = ref _terrainChunks[first.Value];

                if (block != null && block.LastAccessTime < ticks && block.HasNoExternalData())
                {
                    block.Destroy();
                    block = null;

                    if (++count >= Constants.MAX_MAP_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR)
                    {
                        break;
                    }
                }

                first = next;
            }
        }

        public void Destroy()
        {
            LinkedListNode<int> first = _usedIndices.First;

            while (first != null)
            {
                LinkedListNode<int> next = first.Next;
                ref Chunk c = ref _terrainChunks[first.Value];
                c?.Destroy();
                c = null;
                first = next;
            }

            _usedIndices.Clear();
        }
    }
}