#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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

using System.IO;
using System.Linq;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;
using ClassicUO.Game.Scenes;
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
        private static MiniMapGump _self;
        private SpriteTexture _gumpTexture, _mapTexture;
        private bool _forceUpdate;
        private Texture2D _playerIndicator, _mobilesIndicator;
        private long _timeMS;
        private bool _useLargeMap;
        private ushort _x, _y;
        private int _lastMap = -1;
        private bool _draw;

        public MiniMapGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanBeSaved = true;
        }


        public static void Toggle(GameScene scene)
        {
            if (Engine.UI.GetByLocalSerial<MiniMapGump>() == null)
            {
                Engine.UI.Add(_self = new MiniMapGump());
            }
            else
            {
                _self.Dispose();
            }

        }

        public override void Save(BinaryWriter writer)
        {
            base.Save(writer);
            writer.Write(_useLargeMap);
        }

        public override void Restore(BinaryReader reader)
        {
            base.Restore(reader);
            _self = this;
            _useLargeMap = reader.ReadBoolean();
            _forceUpdate = true;
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (_gumpTexture == null || _gumpTexture.IsDisposed || _forceUpdate)
            {
                _gumpTexture = FileManager.Gumps.GetTexture(_useLargeMap ? (ushort) 5011 : (ushort) 5010);
                Width = _gumpTexture.Width;
                Height = _gumpTexture.Height;
                CreateMiniMapTexture();

                if (_forceUpdate)
                    _forceUpdate = false;
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


        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {
            if (_gumpTexture == null || _gumpTexture.IsDisposed || IsDisposed)
                return false;

            batcher.Draw2D(_gumpTexture, position, Vector3.Zero);
            CreateMiniMapTexture();
            batcher.Draw2D(_mapTexture, position, Vector3.Zero);      

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
                    _mobilesIndicator.SetData( new [] { Color.White } );
                }

                int w = Width >> 1;
                int h = Height >> 1;

                foreach (Mobile mob in World.Mobiles.Where(s => s != World.Player))
                {
                    int x = mob.X - World.Player.X;
                    int y = mob.Y - World.Player.Y;

                    int gx = x - y;
                    int gy = x + y;

                    batcher.Draw2D(_mobilesIndicator, new Rectangle(position.X + w + gx, position.Y + h + gy, 2, 2), ShaderHuesTraslator.GetHueVector(Notoriety.GetHue(mob.NotorietyFlag)) );
                }

                //DRAW DOT OF PLAYER
                batcher.Draw2D(_playerIndicator, new Rectangle(position.X + w, position.Y + h, 2, 2), Vector3.Zero);
            }

            return base.Draw(batcher, position, hue);
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                _useLargeMap = !_useLargeMap;
                _forceUpdate = true;

                return true;
            }

            return false;
        }

        public void ForceUpdate() => _forceUpdate = true;

        private void CreateMiniMapTexture()
        {
            if (_gumpTexture == null || _gumpTexture.IsDisposed)
                return;
            ushort lastX = World.Player.Position.X;
            ushort lastY = World.Player.Position.Y;

            if (_lastMap != World.MapIndex)
            {
                _forceUpdate = true;
                _lastMap = World.MapIndex;
            }

            if (_x != lastX || _y != lastY)
            {
                _x = lastX;
                _y = lastY;
            }
            else if (!_forceUpdate)
                return;

            if (_mapTexture != null && !_mapTexture.IsDisposed)
                _mapTexture.Dispose();
            int blockOffsetX = Width >> 2;
            int blockOffsetY = Height >> 2;
            int gumpCenterX = Width >> 1;
            int gumpCenterY = Height >> 1;

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
            int maxBlockIndex = World.Map.MapBlockIndex;
            int mapBlockHeight = FileManager.Map.MapBlocksSize[World.MapIndex, 1];
            ushort[] data = FileManager.Gumps.GetGumpPixels(_useLargeMap ? (uint) 5011 : 5010, out _, out _);

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
                    RadarMapBlock? mbbv = FileManager.Map.GetRadarMapBlock(World.MapIndex, i, j);

                    if (!mbbv.HasValue)
                        break;
                    RadarMapBlock mb = mbbv.Value;
                    Chunk block = World.Map.Chunks[blockIndex];
                    int realBlockX = i * 8;
                    int realBlockY = j * 8;

                    for (int x = 0; x < 8; x++)
                    {
                        int px = realBlockX + x - lastX + gumpCenterX;

                        for (int y = 0; y < 8; y++)
                        {
                            int py = realBlockY + y - lastY;
                            int gx = px - py;
                            int gy = px + py;
                            uint color = mb.Cells[x, y].Graphic;
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
                                        color = obj.Graphic;
                                        island = false;
                                        break;
                                    }
                                }
                            }

                            if (!island)
                                color += 0x4000;
                            int tableSize = 2;
                            color = (uint) (0x8000 | FileManager.Hues.GetRadarColorData((int) color));
                            CreatePixels(data, (int) color, gx, gy, Width, Height, table, tableSize);
                        }
                    }
                }
            }

            _mapTexture = new SpriteTexture(Width, Height, false);
            _mapTexture.SetDataHitMap16(data);
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

        protected override bool Contains(int x, int y)
        {
            return _mapTexture.Contains(x, y);
            //return FileManager.Gumps.Contains(_useLargeMap ? (ushort) 5011 : (ushort) 5010, x, y);
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