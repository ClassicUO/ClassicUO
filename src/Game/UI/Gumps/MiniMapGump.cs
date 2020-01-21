#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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

using System.IO;
using System.Linq;
using System.Xml;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    internal class MiniMapGump : Gump
    {
        private bool _draw;
        //private bool _forceUpdate;
        private UOTexture16 _gumpTexture, _mapTexture;
        private int _lastMap = -1;
        private Texture2D _playerIndicator, _mobilesIndicator;
        private long _timeMS;
        private bool _useLargeMap;
        private ushort _x, _y;

        public MiniMapGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
        }


        public override GUMP_TYPE GumpType => GUMP_TYPE.GT_MINIMAP;

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

        public override void Update(double totalMS, double frameMS)
        {
            if (!World.InGame)
                return;

            if (_gumpTexture == null || _gumpTexture.IsDisposed) CreateMap();

            if (_lastMap != World.MapIndex)
            {
                CreateMap();
                _lastMap = World.MapIndex;
            }

            if (_gumpTexture != null)
                _gumpTexture.Ticks = (long) totalMS;

            if (_mapTexture != null)
                _mapTexture.Ticks = (long) totalMS;

            if (_timeMS < totalMS)
            {
                _draw = !_draw;
                _timeMS = (long) totalMS + 500;
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
            if (_gumpTexture == null || _gumpTexture.IsDisposed || IsDisposed)
                return false;

            ResetHueVector();

            batcher.Draw2D(_gumpTexture, x, y, ref _hueVector);
            CreateMiniMapTexture();
            batcher.Draw2D(_mapTexture, x, y, ref _hueVector);

            if (_draw)
            {
                if (_playerIndicator == null)
                {
                    _playerIndicator = new Texture2D(batcher.GraphicsDevice, 1, 1);

                    _playerIndicator.SetData(new uint[1]
                    {
                        0xFFFFFFFF
                    });

                    _mobilesIndicator = new Texture2D(batcher.GraphicsDevice, 1, 1);
                    _mobilesIndicator.SetData(new[] {Color.White});
                }

                int w = Width >> 1;
                int h = Height >> 1;

                foreach (Mobile mob in World.Mobiles.Where(s => s != World.Player))
                {
                    int xx = mob.X - World.Player.X;
                    int yy = mob.Y - World.Player.Y;

                    int gx = xx - yy;
                    int gy = xx + yy;

                    _hueVector.Z = 0;

                    ShaderHuesTraslator.GetHueVector(ref _hueVector, Notoriety.GetHue(mob.NotorietyFlag));

                    batcher.Draw2D(_mobilesIndicator, x + w + gx, y + h + gy, 2, 2, ref _hueVector);
                }

                //DRAW DOT OF PLAYER
                ResetHueVector();
                batcher.Draw2D(_playerIndicator, x + w, y + h, 2, 2, ref _hueVector);
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

        public void ForceUpdate()
        {
            CreateMap();
        }

        private void CreateMiniMapTexture(bool force = false)
        {
            if (_gumpTexture == null || _gumpTexture.IsDisposed)
                return;

            ushort lastX = World.Player.X;
            ushort lastY = World.Player.Y;


            if (_x != lastX || _y != lastY)
            {
                _x = lastX;
                _y = lastY;
            }
            else if (!force)
                return;

            if (_mapTexture != null && !_mapTexture.IsDisposed)
                _mapTexture.Dispose();
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
                minBlockX = 0;

            if (minBlockY < 0)
                minBlockY = 0;
            int maxBlockIndex = World.Map.BlocksCount;
            int mapBlockHeight = MapLoader.Instance.MapBlocksSize[World.MapIndex, 1];
            ushort[] data = GumpsLoader.Instance.GetGumpPixels(_useLargeMap ? (uint) 5011 : 5010, out _, out _);

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
                        break;

                    RadarMapBlock? mbbv = MapLoader.Instance.GetRadarMapBlock(World.MapIndex, i, j);

                    if (!mbbv.HasValue)
                        break;

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
                                GameObject obj = block.Tiles[x, y].FirstNode;

                                while (obj?.Right != null)
                                    obj = obj.Right;

                                for (; obj != null; obj = obj.Left)
                                {
                                    if (obj is Multi)
                                    {
                                        if (obj.Hue == 0)
                                        {
                                            color = obj.Graphic;
                                            island = false;
                                        }
                                        else
                                            color = obj.Hue + 0x4000;
                                        break;
                                    }
                                }
                            }

                            if (!island)
                                color += 0x4000;
                            int tableSize = 2;
                            if(island && color > 0x4000)
                                color = HuesLoader.Instance.GetColor16(16384, (ushort)(color - 0x4000));//28672 is an arbitrary position in hues.mul, is the 14 position in the range
                            else
                                color = HuesLoader.Instance.GetRadarColorData(color);
                            CreatePixels(data, 0x8000 | color, gx, gy, Width, Height, table, tableSize);
                        }
                    }
                }
            }

            _mapTexture = new UOTexture16(Width, Height);
            _mapTexture.PushData(data);
        }

        private void CreatePixels(ushort[] data, int color, int x, int y, int w, int h, Point[] table, int count)
        {
            int px = x;
            int py = y;

            for (int i = 0; i < count; i++)
            {
                px += table[i].X;
                py += table[i].Y;
                int gx = px;

                if (gx < 0 || gx >= w)
                    continue;

                int gy = py;

                if (gy < 0 || gy >= h)
                    break;

                int block = gy * w + gx;

                if (data[block] == 0x8421)
                    data[block] = (ushort) color;
            }
        }

        public override bool Contains(int x, int y)
        {
            return _mapTexture.Contains(x, y);
        }

        public override void Dispose()
        {
            _playerIndicator?.Dispose();
            _mapTexture?.Dispose();
            _mobilesIndicator?.Dispose();
            base.Dispose();
        }
    }
}