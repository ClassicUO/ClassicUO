#region license

// Copyright (c) 2024, andreakarasho
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
using System.IO;
using System.Xml;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    internal class MiniMapGump : Gump
    {
        private bool _draw;
        private int _lastMap = -1;
        private long _timeMS;
        private bool _useLargeMap;
        private ushort _x,
            _y;
        private static readonly uint[][] _blankGumpsPixels = new uint[4][];

        const ushort SMALL_MAP_GRAPHIC = 5010;
        const ushort BIG_MAP_GRAPHIC = 5011;

        public MiniMapGump(World world) : base(world, 0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
        }

        public override GumpType GumpType => GumpType.MiniMap;

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            writer.WriteAttributeString("isminimized", _useLargeMap.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);
            _useLargeMap = bool.Parse(xml.GetAttribute("isminimized"));
            CreateMap();
        }

        private void CreateMap()
        {
            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(
                _useLargeMap ? BIG_MAP_GRAPHIC : SMALL_MAP_GRAPHIC
            );

            int index = _useLargeMap ? 1 : 0;

            if (_blankGumpsPixels[index] == null)
            {
                int size = gumpInfo.UV.Width * gumpInfo.UV.Height;
                _blankGumpsPixels[index] = new uint[size];
                _blankGumpsPixels[index + 2] = new uint[size];
                gumpInfo.Texture.GetData(0, gumpInfo.UV, _blankGumpsPixels[index], 0, size);

                Array.Copy(_blankGumpsPixels[index], 0, _blankGumpsPixels[index + 2], 0, size);
            }

            Width = gumpInfo.UV.Width;
            Height = gumpInfo.UV.Height;
            CreateMiniMapTexture(gumpInfo.Texture, gumpInfo.UV, true);
        }

        public override void Update()
        {
            if (!World.InGame)
            {
                return;
            }

            if (_lastMap != World.MapIndex)
            {
                CreateMap();
                _lastMap = World.MapIndex;
            }

            if (_timeMS < Time.Ticks)
            {
                _draw = !_draw;
                _timeMS = (long)Time.Ticks + 500;
            }
        }

        public bool ToggleSize(bool? large = null)
        {
            if (large.HasValue)
            {
                _useLargeMap = large.Value;
            }
            else
            {
                _useLargeMap = !_useLargeMap;
            }

            CreateMap();

            return _useLargeMap;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(
                _useLargeMap ? BIG_MAP_GRAPHIC : SMALL_MAP_GRAPHIC
            );

            if (gumpInfo.Texture == null)
            {
                Dispose();

                return false;
            }

            batcher.Draw(gumpInfo.Texture, new Vector2(x, y), gumpInfo.UV, hueVector);

            CreateMiniMapTexture(gumpInfo.Texture, gumpInfo.UV);

            batcher.Draw(gumpInfo.Texture, new Vector2(x, y), gumpInfo.UV, hueVector);

            if (_draw)
            {
                int w = Width >> 1;
                int h = Height >> 1;

                Texture2D mobilesTextureDot = SolidColorTextureCache.GetTexture(Color.Red);

                foreach (Mobile mob in World.Mobiles.Values)
                {
                    if (mob == World.Player)
                    {
                        continue;
                    }

                    int xx = mob.X - World.Player.X;
                    int yy = mob.Y - World.Player.Y;

                    int gx = xx - yy;
                    int gy = xx + yy;

                    hueVector = ShaderHueTranslator.GetHueVector(
                        Notoriety.GetHue(mob.NotorietyFlag)
                    );

                    batcher.Draw(
                        mobilesTextureDot,
                        new Rectangle(x + w + gx, y + h + gy, 2, 2),
                        hueVector
                    );
                }

                //DRAW PLAYER DOT
                hueVector = ShaderHueTranslator.GetHueVector(0);

                batcher.Draw(
                    SolidColorTextureCache.GetTexture(Color.White),
                    new Rectangle(x + w, y + h, 2, 2),
                    hueVector
                );
            }

            return base.Draw(batcher, x, y);
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                ToggleSize();

                return true;
            }

            return false;
        }

        protected override void UpdateContents()
        {
            CreateMap();
        }

        private unsafe void CreateMiniMapTexture(
            Texture2D texture,
            Rectangle bounds,
            bool force = false
        )
        {
            ushort lastX = World.Player.X;
            ushort lastY = World.Player.Y;

            if (_x != lastX || _y != lastY)
            {
                _x = lastX;
                _y = lastY;
            }
            else if (!force)
            {
                return;
            }

            int blockOffsetX = Width >> 2;
            int blockOffsetY = Height >> 2;
            int gumpCenterX = Width >> 1;
            //int gumpCenterY = Height >> 1;

            //0xFF080808 - pixel32
            //0x8421 - pixel16
            int minBlockX = ((lastX - blockOffsetX) >> 3) - 1;
            int minBlockY = ((lastY - blockOffsetY) >> 3) - 1;
            int maxBlockX = ((lastX + blockOffsetX) >> 3) + 1;
            int maxBlockY = ((lastY + blockOffsetY) >> 3) + 1;

            if (minBlockX < 0)
            {
                minBlockX = 0;
            }

            if (minBlockY < 0)
            {
                minBlockY = 0;
            }

            int maxBlockIndex = World.Map.BlocksCount;
            int mapBlockHeight = MapLoader.Instance.MapBlocksSize[World.MapIndex, 1];
            int index = _useLargeMap ? 1 : 0;

            _blankGumpsPixels[index].CopyTo(_blankGumpsPixels[index + 2], 0);

            uint[] data = _blankGumpsPixels[index + 2];

            Point* table = stackalloc Point[2];
            table[0].X = 0;
            table[0].Y = 0;
            table[1].X = 0;
            table[1].Y = 1;

            for (int i = minBlockX; i <= maxBlockX; i++)
            {
                int blockIndexOffset = i * mapBlockHeight;

                for (int j = minBlockY; j <= maxBlockY; j++)
                {
                    int blockIndex = blockIndexOffset + j;

                    if (blockIndex >= maxBlockIndex)
                    {
                        break;
                    }

                    ref IndexMap indexMap = ref World.Map.GetIndex(i, j);

                    if (indexMap.MapAddress == 0)
                    {
                        break;
                    }

                    MapBlock* mp = (MapBlock*)indexMap.MapAddress;
                    MapCells* cells = (MapCells*)&mp->Cells;
                    StaticsBlock* sb = (StaticsBlock*)indexMap.StaticAddress;
                    uint staticCount = indexMap.StaticCount;

                    Chunk block = World.Map.GetChunk(blockIndex);
                    int realBlockX = i << 3;
                    int realBlockY = j << 3;

                    for (int x = 0; x < 8; x++)
                    {
                        int px = realBlockX + x - lastX + gumpCenterX;

                        for (int y = 0; y < 8; y++)
                        {
                            ref MapCells cell = ref cells[(y << 3) + x];
                            int color = cell.TileID;
                            bool isLand = true;
                            int z = cell.Z;

                            for (int c = 0; c < staticCount; ++c)
                            {
                                ref StaticsBlock stblock = ref sb[c];

                                if (
                                    stblock.X == x
                                    && stblock.Y == y
                                    && stblock.Color > 0
                                    && stblock.Color != 0xFFFF
                                    && GameObject.CanBeDrawn(World, stblock.Color)
                                )
                                {
                                    if (stblock.Z >= z)
                                    {
                                        color =
                                            stblock.Hue > 0
                                                ? (ushort)(stblock.Hue + 0x4000)
                                                : stblock.Color;
                                        isLand = stblock.Hue > 0;

                                        z = stblock.Z;
                                    }
                                }
                            }

                            if (block != null)
                            {
                                GameObject obj = block.Tiles[x, y];

                                while (obj?.TNext != null)
                                {
                                    obj = obj.TNext;
                                }

                                for (; obj != null; obj = obj.TPrevious)
                                {
                                    if (obj is Multi)
                                    {
                                        if (obj.Hue == 0)
                                        {
                                            color = obj.Graphic;
                                            isLand = false;
                                        }
                                        else
                                        {
                                            color = obj.Hue + 0x4000;
                                        }

                                        break;
                                    }
                                }
                            }

                            if (!isLand)
                            {
                                color += 0x4000;
                            }

                            int tableSize = 2;

                            if (isLand && color > 0x4000)
                            {
                                color = HuesLoader.Instance.GetColor16(
                                    16384,
                                    (ushort)(color - 0x4000)
                                ); //28672 is an arbitrary position in hues.mul, is the 14 position in the range
                            }
                            else
                            {
                                color = HuesLoader.Instance.GetRadarColorData(color);
                            }

                            int py = realBlockY + y - lastY;
                            int gx = px - py;
                            int gy = px + py;

                            CreatePixels(
                                data,
                                0x8000 | color,
                                gx,
                                gy,
                                Width,
                                Height,
                                table,
                                tableSize
                            );
                        }
                    }
                }
            }

            fixed (uint* ptr = data)
            {
                texture.SetDataPointerEXT(0, bounds, (IntPtr)ptr, data.Length * sizeof(uint));
            }
        }

        private unsafe void CreatePixels(
            uint[] data,
            int color,
            int x,
            int y,
            int w,
            int h,
            Point* table,
            int count
        )
        {
            int px = x;
            int py = y;

            for (int i = 0; i < count; i++)
            {
                px += table[i].X;
                py += table[i].Y;

                int gx = px;

                if (gx < 0 || gx >= w)
                {
                    continue;
                }

                int gy = py;

                if (gy < 0 || gy >= h)
                {
                    break;
                }

                int block = gy * w + gx;

                if (data[block] == 0xFF080808)
                {
                    data[block] = HuesHelper.Color16To32((ushort)color) | 0xFF_00_00_00;
                }
            }
        }

        public override bool Contains(int x, int y)
        {
            x -= Offset.X;
            y -= Offset.Y;

            if (x >= 0 && y >= 0 && x < Width && y < Height)
            {
                int index = (_useLargeMap ? 1 : 0) + 2;
                int pos = (y * Width) + x;

                if (pos < _blankGumpsPixels[index].Length)
                {
                    return _blankGumpsPixels[index][pos] != 0;
                }
            }

            return false;
        }
    }
}
