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

using System.IO;
using System.Xml;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    internal class MiniMapGump : Gump
    {
        private bool _draw;
        //private bool _forceUpdate;
        private UOTexture _gumpTexture, _mapTexture;
        private int _lastMap = -1;
        private long _timeMS;
        private bool _useLargeMap;
        private ushort _x, _y;

        public MiniMapGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
        }


        public override GumpType GumpType => GumpType.MiniMap;

        public override void Save(BinaryWriter writer)
        {
            base.Save(writer);
            writer.Write(_useLargeMap);
        }

        public override void Restore(BinaryReader reader)
        {
            base.Restore(reader);
            _useLargeMap = reader.ReadBoolean();
            CreateMap();
        }

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
            _gumpTexture = GumpsLoader.Instance.GetTexture(_useLargeMap ? (ushort) 5011 : (ushort) 5010);
            Width = _gumpTexture.Width;
            Height = _gumpTexture.Height;
            CreateMiniMapTexture(true);
        }

        public override void Update(double totalTime, double frameTime)
        {
            if (!World.InGame)
            {
                return;
            }

            if (_gumpTexture == null || _gumpTexture.IsDisposed)
            {
                CreateMap();
            }

            if (_lastMap != World.MapIndex)
            {
                CreateMap();
                _lastMap = World.MapIndex;
            }

            if (_gumpTexture != null)
            {
                _gumpTexture.Ticks = (long) totalTime;
            }

            if (_mapTexture != null)
            {
                _mapTexture.Ticks = (long) totalTime;
            }

            if (_timeMS < totalTime)
            {
                _draw = !_draw;
                _timeMS = (long) totalTime + 500;
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

            if (_mapTexture != null && !_mapTexture.IsDisposed)
            {
                _mapTexture.Dispose();
            }

            CreateMap();

            return _useLargeMap;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (_gumpTexture == null || _gumpTexture.IsDisposed || IsDisposed)
            {
                return false;
            }

            ResetHueVector();

            batcher.Draw2D(_gumpTexture, x, y, ref HueVector);
            CreateMiniMapTexture();
            batcher.Draw2D(_mapTexture, x, y, ref HueVector);

            if (_draw)
            {
                int w = Width >> 1;
                int h = Height >> 1;

                Texture2D mobilesTextureDot = SolidColorTextureCache.GetTexture(Color.Red);

                foreach (Mobile mob in World.Mobiles)
                {
                    if (mob == World.Player)
                    {
                        continue;
                    }

                    int xx = mob.X - World.Player.X;
                    int yy = mob.Y - World.Player.Y;

                    int gx = xx - yy;
                    int gy = xx + yy;

                    HueVector.Z = 0;

                    ShaderHueTranslator.GetHueVector(ref HueVector, Notoriety.GetHue(mob.NotorietyFlag));

                    batcher.Draw2D
                    (
                        mobilesTextureDot,
                        x + w + gx,
                        y + h + gy,
                        2,
                        2,
                        ref HueVector
                    );
                }

                //DRAW DOT OF PLAYER
                ResetHueVector();

                batcher.Draw2D
                (
                    SolidColorTextureCache.GetTexture(Color.White),
                    x + w,
                    y + h,
                    2,
                    2,
                    ref HueVector
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

        private void CreateMiniMapTexture(bool force = false)
        {
            if (_gumpTexture == null || _gumpTexture.IsDisposed)
            {
                return;
            }

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
            uint[] data = GumpsLoader.Instance.GetGumpPixels(_useLargeMap ? (uint) 5011 : 5010, out _, out _);

            Point[] table = new Point[2]
            {
                new Point(0, 0), new Point(0, 1)
            };

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

                    RadarMapBlock? mbbv = MapLoader.Instance.GetRadarMapBlock(World.MapIndex, i, j);

                    if (!mbbv.HasValue)
                    {
                        break;
                    }

                    RadarMapBlock mb = mbbv.Value;
                    Chunk block = World.Map.Chunks[blockIndex];
                    int realBlockX = i << 3;
                    int realBlockY = j << 3;

                    for (int x = 0; x < 8; x++)
                    {
                        int px = realBlockX + x - lastX + gumpCenterX;

                        for (int y = 0; y < 8; y++)
                        {
                            int py = realBlockY + y - lastY;
                            int gx = px - py;
                            int gy = px + py;

                            int color = mb.Cells[x, y].Graphic;

                            bool island = mb.Cells[x, y].IsLand;

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
                                            island = false;
                                        }
                                        else
                                        {
                                            color = obj.Hue + 0x4000;
                                        }

                                        break;
                                    }
                                }
                            }

                            if (!island)
                            {
                                color += 0x4000;
                            }

                            int tableSize = 2;

                            if (island && color > 0x4000)
                            {
                                color = HuesLoader.Instance.GetColor16(16384, (ushort) (color - 0x4000)); //28672 is an arbitrary position in hues.mul, is the 14 position in the range
                            }
                            else
                            {
                                color = HuesLoader.Instance.GetRadarColorData(color);
                            }

                            CreatePixels
                            (
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

            if (_mapTexture == null || _mapTexture.IsDisposed)
            {
                _mapTexture = new UOTexture(Width, Height);
            }

            _mapTexture.PushData(data);
        }

        private void CreatePixels
        (
            uint[] data,
            int color,
            int x,
            int y,
            int w,
            int h,
            Point[] table,
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
                    data[block] = HuesHelper.Color16To32((ushort) color) | 0xFF_00_00_00;
                }
            }
        }

        public override bool Contains(int x, int y)
        {
            return _mapTexture.Contains(x - Offset.X, y - Offset.Y);
        }

        public override void Dispose()
        {
            _mapTexture?.Dispose();
            base.Dispose();
        }
    }
}