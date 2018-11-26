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

using ClassicUO.Game.Map;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class MiniMapGump : Gump
    {
        private const float ReticleBlinkMS = 250f;
        private static MiniMapGump _self;
        private double _frameMS;
        private SpriteTexture _gumpTexture, _mapTexture;
        private bool _miniMap_LargeFormat, _forceUpdate;
        private Texture2D _playerIndicator;
        private GameScene _scene;
        private float _timeMS;
        private bool _useLargeMap;
        private ushort _x, _y;

        public MiniMapGump(GameScene scene) : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            X = 600;
            Y = 50;
            _scene = scene;
            _useLargeMap = _miniMap_LargeFormat;
        }

        public static bool MiniMap_LargeFormat { get; set; }

        public static void Toggle(GameScene scene)
        {
            UIManager ui = Service.Get<UIManager>();

            if (ui.GetByLocalSerial<MiniMapGump>() == null)
                ui.Add(_self = new MiniMapGump(scene));
            else
                _self.Dispose();
        }

        public override void Update(double totalMS, double frameMS)
        {
            _frameMS = frameMS;

            if (_gumpTexture == null || _gumpTexture.IsDisposed || _useLargeMap != _miniMap_LargeFormat || _forceUpdate)
            {
                _useLargeMap = _miniMap_LargeFormat;
                _gumpTexture = IO.Resources.Gumps.GetGumpTexture(_useLargeMap ? (ushort) 5011 : (ushort) 5010);
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
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Point position, Vector3? hue = null)
        {
            if (_gumpTexture == null || _gumpTexture.IsDisposed)
                return false;
            spriteBatch.Draw2D(_gumpTexture, position, Vector3.Zero);
            CreateMiniMapTexture();
            spriteBatch.Draw2D(_mapTexture, position, Vector3.Zero);
            _timeMS += (float) _frameMS;

            if (_timeMS >= ReticleBlinkMS)
            {
                if (_playerIndicator == null)
                {
                    _playerIndicator = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);

                    _playerIndicator.SetData(new uint[1]
                    {
                        0xFFFFFFFF
                    });
                }

                //DRAW DOT OF PLAYER
                spriteBatch.Draw2D(_playerIndicator, new Point(position.X + Width / 2, position.Y + Height / 2), Vector3.Zero);
            }

            if (_timeMS >= ReticleBlinkMS * 2)
                _timeMS -= ReticleBlinkMS * 2;

            return base.Draw(spriteBatch, position, hue);
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                MiniMap_LargeFormat = !MiniMap_LargeFormat;
                _miniMap_LargeFormat = MiniMap_LargeFormat;
                _forceUpdate = true;

                return true;
            }

            return false;
        }

        private void CreateMiniMapTexture()
        {
            if (_gumpTexture == null || _gumpTexture.IsDisposed)
                return;
            ushort lastX = World.Player.Position.X;
            ushort lastY = World.Player.Position.Y;

            if (_x != lastX || _y != lastY)
            {
                _x = lastX;
                _y = lastY;
            }
            else if (!_forceUpdate) return;

            if (_mapTexture != null && !_mapTexture.IsDisposed)
                _mapTexture.Dispose();
            int blockOffsetX = Width / 4;
            int blockOffsetY = Height / 4;
            int gumpCenterX = Width / 2;
            int gumpCenterY = Height / 2;

            //0xFF080808 - pixel32
            //0x8421 - pixel16
            int minBlockX = (lastX - blockOffsetX) / 8 - 1;
            int minBlockY = (lastY - blockOffsetY) / 8 - 1;
            int maxBlockX = (lastX + blockOffsetX) / 8 + 1;
            int maxBlockY = (lastY + blockOffsetY) / 8 + 1;

            if (minBlockX < 0)
                minBlockX = 0;

            if (minBlockY < 0)
                minBlockY = 0;
            int maxBlockIndex = World.Map.MapBlockIndex;
            int mapBlockHeight = IO.Resources.Map.MapBlocksSize[World.MapIndex][1];
            ushort[] data = IO.Resources.Gumps.GetGumpPixels(_useLargeMap ? 5011 : 5010, out _, out _);

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
                    RadarMapBlock? mbbv = IO.Resources.Map.GetRadarMapBlock(World.MapIndex, i, j);

                    if (!mbbv.HasValue)
                        break;
                    RadarMapBlock mb = mbbv.Value;
                    MapChunk mapBlock = World.Map.Chunks[blockIndex];
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

                            //if (mapBlock != null)
                            //{
                            //    ushort multicolor = mapBlock.get
                            //}

                            if (!island)
                                color += 0x4000;
                            int tableSize = 2;
                            color = (uint) (0x8000 | Hues.GetRadarColorData((int) color));
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
            //return IO.Resources.Gumps.Contains(_useLargeMap ? (ushort) 5011 : (ushort) 5010, x, y);
        }
    }
}