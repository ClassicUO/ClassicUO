﻿#region license

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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;


namespace ClassicUO.Game.UI.Gumps
{
    internal class WorldMapGump : ResizableGump
    {
        private UOTexture _mapTexture;
        private uint _nextQueryPacket;

        private bool _isTopMost;
        private readonly float[] _zooms = new float[10] { 0.125f, 0.25f, 0.5f, 0.75f, 1f, 1.5f, 2f, 4f, 6f, 8f };
        private int _zoomIndex = 4;
        private Point _center, _lastScroll;
        private bool _isScrolling;
        private bool _flipMap = true;
        private bool _freeView;
        private int _mapIndex;
        private bool _showPartyMembers = true;

        private Label _coords;
        private bool _showCoordinates;
        private int _lastX;
        private int _lastY;
        private int _lastZ;
        private bool _showMobiles = true;
        private bool _showPlayerName = true;
        private bool _showPlayerBar = true;

        private bool _hideMarkers = false;

        private string[] _mapFiles;
        private string _mapFilesPath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client");

        private class WMapMarker
        {
            public string Name { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public string MarkerId { get; set; }
            public int MapId { get; set; }
            public Color Color { get; set; }
            public bool Hidden { get; set; }
        }

        private List<WMapMarker> _markers = new List<WMapMarker>();

        public WorldMapGump() : base(400, 400, 100, 100, 0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = false;

            GameActions.Print("WorldMap loading...", 0x35);
            Load();
            OnResize();

            _mapFiles = Directory.GetFiles(_mapFilesPath, "*.map").Union(Directory.GetFiles(_mapFilesPath, "*.xml"))
                .ToArray();
            LoadMarkers();

            World.WMapManager.SetEnable(true);
            BuildGump();
        }

        public override GUMP_TYPE GumpType => GUMP_TYPE.GT_WORLDMAP;
        public float Zoom => _zooms[_zoomIndex];


        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            int width = int.Parse(xml.GetAttribute("width"));
            int height = int.Parse(xml.GetAttribute("height"));

            ResizeWindow(new Point(width, height));

            _flipMap = ParseBool(xml.GetAttribute("flipmap"));
            TopMost = ParseBool(xml.GetAttribute("topmost"));
            FreeView = ParseBool(xml.GetAttribute("freeview"));
            _showPartyMembers = ParseBool(xml.GetAttribute("showpartymembers"));
            if (int.TryParse(xml.GetAttribute("zoomindex"), out int value))
                _zoomIndex = (value >= 0 && value < _zooms.Length) ? value : 4;

            _showCoordinates = ParseBool(xml.GetAttribute("showcoordinates"));
            _showMobiles = ParseBool(xml.GetAttribute("showmobiles"));

            _showPlayerName = ParseBool(xml.GetAttribute("showplayername"));
            _showPlayerBar = ParseBool(xml.GetAttribute("showplayerbar"));
            _hideMarkers = ParseBool(xml.GetAttribute("hidemarkers"));

            BuildGump();
        }

        private bool ParseBool(string boolStr)
        {
            return bool.TryParse(boolStr, out bool value) && value;
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);

            writer.WriteAttributeString("width", Width.ToString());
            writer.WriteAttributeString("height", Height.ToString());

            writer.WriteAttributeString("flipmap", _flipMap.ToString());
            writer.WriteAttributeString("topmost", _isTopMost.ToString());
            writer.WriteAttributeString("freeview", _freeView.ToString());
            writer.WriteAttributeString("showpartymembers", _showPartyMembers.ToString());
            writer.WriteAttributeString("zoomindex", _zoomIndex.ToString());
            writer.WriteAttributeString("showcoordinates", _showCoordinates.ToString());
            writer.WriteAttributeString("showmobiles", _showMobiles.ToString());
            writer.WriteAttributeString("showplayername", _showPlayerName.ToString());
            writer.WriteAttributeString("showplayerbar", _showPlayerBar.ToString());
            writer.WriteAttributeString("hidemarkers", _hideMarkers.ToString());
        }

        private void BuildGump()
        {
            BuildContextMenu();

            Add(_coords = new Label("", true, 1001, font: 1, style: FontStyle.BlackBorder)
            {
                X = 10,
                Y = 5
            });
        }

        private void BuildContextMenu()
        {
            ContextMenu?.Dispose();
            ContextMenu = null;

            ContextMenu = new ContextMenuControl();
            ContextMenu.Add("Flip map", () => _flipMap = !_flipMap, true, _flipMap);
            ContextMenu.Add("Top Most", () => TopMost = !TopMost, true, _isTopMost);
            ContextMenu.Add("Free view", () => { FreeView = !FreeView; }, true, _freeView);
            ContextMenu.Add("", null);
            ContextMenu.Add("Show party members", () => { _showPartyMembers = !_showPartyMembers; }, true,
                _showPartyMembers);
            ContextMenu.Add("Show mobiles", () => { _showMobiles = !_showMobiles; }, true, _showMobiles);
            ContextMenu.Add("Show coordinates", () => { _showCoordinates = !_showCoordinates; }, true,
                _showCoordinates);
            ContextMenu.Add("", null);
            ContextMenu.Add("Show your name", () => { _showPlayerName = !_showPlayerName; }, true, _showPlayerName);
            ContextMenu.Add("Show your healthbar", () => { _showPlayerBar = !_showPlayerBar; }, true, _showPlayerBar);
            ContextMenu.Add("", null);
            ContextMenu.Add("Hide all markers", () => { _hideMarkers = !_hideMarkers; }, true, _hideMarkers);
            ContextMenu.Add("Reload map markers", () => { LoadMarkers(); });

            foreach (string mapFile in _mapFiles)
            {
                string file = Path.GetFileNameWithoutExtension(mapFile);
                ContextMenu.Add($"Show/Hide {file}", () =>
                {
                    foreach (WMapMarker marker in _markers)
                    {
                        if (marker.MarkerId.Equals(file))
                        {
                            marker.Hidden = !marker.Hidden;
                        }
                    }
                });
            }

            ContextMenu.Add("", null);
            ContextMenu.Add("Close", Dispose);

            Add(_coords = new Label("", true, 1001, font: 1, style: FontStyle.BlackBorder)
            {
                X = 10,
                Y = 5
            });
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left || _isScrolling || Keyboard.Alt)
                return base.OnMouseDoubleClick(x, y, button);

            TopMost = !TopMost;

            return true;
        }

        public bool TopMost
        {
            get => _isTopMost;
            set
            {
                _isTopMost = value;

                ShowBorder = !_isTopMost;

                ControlInfo.Layer = _isTopMost ? UILayer.Over : UILayer.Default;
            }
        }

        public bool FreeView
        {
            get => _freeView;
            set
            {
                _freeView = value;

                if (!_freeView)
                {
                    _isScrolling = false;
                    CanMove = true;
                }
            }
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                _isScrolling = false;
                CanMove = true;
            }

            UIManager.GameCursor.IsDraggingCursorForced = false;

            base.OnMouseUp(x, y, button);
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left && (Keyboard.Alt || _freeView))
            {
                if (x > 4 && x < Width - 8 && y > 4 && y < Height - 8)
                {
                    _lastScroll.X = x;
                    _lastScroll.Y = y;
                    _isScrolling = true;
                    CanMove = false;

                    UIManager.GameCursor.IsDraggingCursorForced = true;
                }
            }

            base.OnMouseDown(x, y, button);
        }

        protected override void OnMouseOver(int x, int y)
        {
            Point offset = Mouse.LDroppedOffset;

            if (_isScrolling && offset != Point.Zero)
            {
                int scrollX = _lastScroll.X - x;
                int scrollY = _lastScroll.Y - y;

                (scrollX, scrollY) = RotatePoint(scrollX, scrollY, 1f, -1, _flipMap ? 45f : 0f);

                _center.X += (int)(scrollX / Zoom);
                _center.Y += (int)(scrollY / Zoom);

                if (_center.X < 0)
                    _center.X = 0;

                if (_center.Y < 0)
                    _center.Y = 0;

                if (_center.X > MapLoader.Instance.MapsDefaultSize[World.MapIndex, 0])
                    _center.X = MapLoader.Instance.MapsDefaultSize[World.MapIndex, 0];

                if (_center.Y > MapLoader.Instance.MapsDefaultSize[World.MapIndex, 1])
                    _center.Y = MapLoader.Instance.MapsDefaultSize[World.MapIndex, 1];

                _lastScroll.X = x;
                _lastScroll.Y = y;
            }
            else
            {
                base.OnMouseOver(x, y);
            }
        }


        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (_mapIndex != World.MapIndex)
            {
                Load();
            }

            World.WMapManager.RequestServerPartyGuildInfo();
        }

        private unsafe Task Load()
        {
            _mapIndex = World.MapIndex;
            _mapTexture?.Dispose();
            _mapTexture = null;

            return Task.Run(() =>
            {
                if (World.InGame)
                {
                    const int OFFSET_PIX = 2;
                    const int OFFSET_PIX_HALF = OFFSET_PIX / 2;

                    int realWidth = MapLoader.Instance.MapsDefaultSize[World.MapIndex, 0];
                    int realHeight = MapLoader.Instance.MapsDefaultSize[World.MapIndex, 1];

                    int fixedWidth = MapLoader.Instance.MapBlocksSize[World.MapIndex, 0];
                    int fixedHeight = MapLoader.Instance.MapBlocksSize[World.MapIndex, 1];

                    int size = (realWidth + OFFSET_PIX) * (realHeight + OFFSET_PIX);
                    Color[] buffer = new Color[size];
                    sbyte[] allZ = new sbyte[size];


                    for (int bx = 0; bx < fixedWidth; bx++)
                    {
                        int mapX = bx << 3;

                        for (int by = 0; by < fixedHeight; by++)
                        {
                            int mapY = by << 3;

                            ref IndexMap indexMap = ref World.Map.GetIndex(bx, by);

                            if (indexMap.MapAddress == 0)
                                continue;

                            MapBlock* mapBlock = (MapBlock*)indexMap.MapAddress;
                            MapCells* cells = (MapCells*)&mapBlock->Cells;

                            int pos = 0;

                            for (int y = 0; y < 8; y++)
                            {
                                int block = (mapY + y + OFFSET_PIX_HALF) * (realWidth + OFFSET_PIX) + mapX +
                                            OFFSET_PIX_HALF;

                                for (int x = 0; x < 8; x++)
                                {
                                    ref MapCells cell = ref cells[pos];

                                    var color =
                                        (ushort)(0x8000 | HuesLoader.Instance.GetRadarColorData(cell.TileID));

                                    buffer[block] = new Color((((color >> 10) & 31) / 31f),
                                        (((color >> 5) & 31) / 31f),
                                        ((color & 31) / 31f));
                                    allZ[block] = cell.Z;

                                    block++;
                                    pos++;
                                }
                            }


                            StaticsBlock* sb = (StaticsBlock*)indexMap.StaticAddress;
                            if (sb != null)
                            {
                                int count = (int)indexMap.StaticCount;

                                for (int c = 0; c < count; c++)
                                {
                                    ref readonly StaticsBlock staticBlock = ref sb[c];

                                    if (staticBlock.Color != 0 && staticBlock.Color != 0xFFFF &&
                                        !GameObjectHelper.IsNoDrawable(staticBlock.Color))
                                    {
                                        pos = (staticBlock.Y << 3) + staticBlock.X;
                                        ref MapCells cell = ref cells[pos];

                                        if (cell.Z <= staticBlock.Z)
                                        {
                                            var color = (ushort)(0x8000 | (staticBlock.Hue > 0
                                                                      ? HuesLoader.Instance.GetColor16(16384,
                                                                          staticBlock.Hue)
                                                                      : HuesLoader.Instance.GetRadarColorData(
                                                                          staticBlock.Color + 0x4000)));

                                            int block = (mapY + staticBlock.Y + OFFSET_PIX_HALF) *
                                                        (realWidth + OFFSET_PIX) + (mapX + staticBlock.X) +
                                                        OFFSET_PIX_HALF;
                                            buffer[block] = new Color((((color >> 10) & 31) / 31f),
                                                (((color >> 5) & 31) / 31f),
                                                ((color & 31) / 31f));
                                            allZ[block] = staticBlock.Z;
                                        }
                                    }
                                }
                            }
                        }
                    }


                    for (int mapY = 1; mapY < realHeight - 1; mapY++)
                    {
                        for (int mapX = 1; mapX < realWidth - 1; mapX++)
                        {
                            int blockCurrent = ((mapY) + OFFSET_PIX_HALF) * (realWidth + OFFSET_PIX) + (mapX) +
                                               OFFSET_PIX_HALF;
                            int blockNext = ((mapY + 1) + OFFSET_PIX_HALF) * (realWidth + OFFSET_PIX) + (mapX - 1) +
                                            OFFSET_PIX_HALF;

                            sbyte z0 = allZ[blockCurrent];
                            sbyte z1 = allZ[blockNext];

                            int block = ((mapY + 1) + OFFSET_PIX_HALF) * (realWidth + OFFSET_PIX) + (mapX + 1) +
                                        OFFSET_PIX_HALF;
                            ref Color cc = ref buffer[block];

                            if (z0 < z1)
                            {
                                cc.R = (byte)(cc.R * 80 / 100);
                                cc.G = (byte)(cc.G * 80 / 100);
                                cc.B = (byte)(cc.B * 80 / 100);
                            }
                            else if (z0 > z1)
                            {
                                cc.R = (byte)(cc.R * 100 / 80);
                                cc.G = (byte)(cc.G * 100 / 80);
                                cc.B = (byte)(cc.B * 100 / 80);
                            }
                        }
                    }

                    if (OFFSET_PIX > 0)
                    {
                        realWidth += OFFSET_PIX;
                        realHeight += OFFSET_PIX;
                    }

                    _mapTexture = new UOTexture32(realWidth, realHeight);
                    _mapTexture.SetData(buffer);

                    GameActions.Print("WorldMap loaded!", 0x48);
                }
            }
            );
        }

        private unsafe Task LoadMarkers()
        {
            return Task.Run(() =>
            {
                if (World.InGame)
                {
                    GameActions.Print("Loading WorldMap labels..", 0x48);

                    _mapFiles = Directory.GetFiles(_mapFilesPath, "*.map")
                        .Union(Directory.GetFiles(_mapFilesPath, "*.xml")).ToArray();

                    _markers.Clear();

                    foreach (string mapFile in _mapFiles)
                    {
                        if (File.Exists(mapFile))
                        {
                            GameActions.Print($"..{Path.GetFileName(mapFile)}", 0x48);

                            if (mapFile != null && Path.GetExtension(mapFile).Equals(".xml"))
                            {
                                XmlTextReader reader = new XmlTextReader(mapFile);

                                while (reader.Read())
                                {
                                    if (reader.Name.Equals("Marker"))
                                    {
                                        WMapMarker marker = new WMapMarker
                                        {
                                            X = int.Parse(reader.GetAttribute("X")),
                                            Y = int.Parse(reader.GetAttribute("Y")),
                                            Name = reader.GetAttribute("Name"),
                                            MapId = int.Parse(reader.GetAttribute("Facet")),
                                            MarkerId = Path.GetFileNameWithoutExtension(mapFile),
                                            Color = Color.White
                                        };

                                        _markers.Add(marker);
                                    }
                                }
                            }
                            else
                            {
                                using (StreamReader reader = new StreamReader(mapFile))
                                {
                                    while (!reader.EndOfStream)
                                    {
                                        string line = reader.ReadLine();

                                        // ignore empty lines, and if UOAM, ignore the first line that always has a 3
                                        if (string.IsNullOrEmpty(line) || line.Equals("3")) continue;

                                        // Check for UOAM file
                                        if (line.Substring(0, 1).Equals("+") || line.Substring(0, 1).Equals("-"))
                                        {
                                            line = line.Substring(line.IndexOf(':') + 2);

                                            string[] splits = line.Split(' ');

                                            if (splits.Length <= 1) continue;

                                            WMapMarker marker = new WMapMarker
                                            {
                                                X = int.Parse(splits[0]),
                                                Y = int.Parse(splits[1]),
                                                MapId = int.Parse(splits[2]),
                                                Name = string.Join(" ", splits, 3, splits.Length - 3),
                                                MarkerId = Path.GetFileNameWithoutExtension(mapFile),
                                                Color = Color.White
                                            };

                                            _markers.Add(marker);
                                        }
                                        else // go go CSV
                                        {
                                            string[] splits = line.Split(',');

                                            if (splits.Length <= 1) continue;

                                            WMapMarker marker = new WMapMarker
                                            {
                                                X = int.Parse(splits[0]),
                                                Y = int.Parse(splits[1]),
                                                MapId = int.Parse(splits[2]),
                                                Name = splits[3],
                                                MarkerId = Path.GetFileNameWithoutExtension(mapFile),
                                                Color = splits.Length == 5 ? GetColor(splits[4]) : Color.White
                                            };

                                            _markers.Add(marker);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    GameActions.Print("WorldMap labels loaded", 0x48);
                }
            }
            );
        }

        protected override void OnMouseWheel(MouseEventType delta)
        {
            if (delta == MouseEventType.WheelScrollUp)
            {
                _zoomIndex++;

                if (_zoomIndex >= _zooms.Length)
                    _zoomIndex = _zooms.Length - 1;
            }
            else
            {
                _zoomIndex--;

                if (_zoomIndex < 0)
                    _zoomIndex = 0;
            }


            base.OnMouseWheel(delta);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed || !World.InGame)
                return false;

            if (!_isScrolling && !_freeView)
            {
                _center.X = World.Player.X;
                _center.Y = World.Player.Y;
            }


            int gX = x + 4;
            int gY = y + 4;
            int gWidth = Width - 8;
            int gHeight = Height - 8;

            int sx = _center.X + 1;
            int sy = _center.Y + 1;

            int size = (int)Math.Max(gWidth * 1.75f, gHeight * 1.75f);

            int size_zoom = (int)(size / Zoom);
            int size_zoom_half = size_zoom >> 1;

            int halfWidth = gWidth >> 1;
            int halfHeight = gHeight >> 1;

            ResetHueVector();


            batcher.Draw2D(Texture2DCache.GetTexture(Color.Black), gX, gY, gWidth, gHeight, ref _hueVector);

            if (_mapTexture != null)
            {
                var rect = ScissorStack.CalculateScissors(Matrix.Identity, gX, gY, gWidth, gHeight);

                if (ScissorStack.PushScissors(rect))
                {
                    batcher.EnableScissorTest(true);

                    int offset = size >> 1;

                    batcher.Draw2D(_mapTexture, (gX - offset) + halfWidth, (gY - offset) + halfHeight,
                        size, size,
                        sx - size_zoom_half,
                        sy - size_zoom_half,
                        size_zoom,
                        size_zoom,
                        ref _hueVector, _flipMap ? 45 : 0);

                    DrawAll(batcher, gX, gY, halfWidth, halfHeight);

                    batcher.EnableScissorTest(false);
                    ScissorStack.PopScissors();
                }
            }

            //foreach (House house in World.HouseManager.Houses)
            //{
            //    foreach (Multi multi in house.Components)
            //    {
            //        batcher.Draw2D(Textures.GetTexture())
            //    }
            //}


            return base.Draw(batcher, x, y);
        }

        private void DrawAll(UltimaBatcher2D batcher, int gX, int gY, int halfWidth, int halfHeight)
        {
            if (_showCoordinates)
            {
                if (World.Player.X != _lastX || World.Player.Y != _lastY || World.Player.Z != _lastZ)
                {
                    _coords.Text = $"{World.Player.X}, {World.Player.Y} ({World.Player.Z})";
                    _lastX = World.Player.X;
                    _lastY = World.Player.Y;
                    _lastZ = World.Player.Z;
                }
            }
            else
            {
                _coords.Text = string.Empty;
            }

            if (!_hideMarkers)
            {
                foreach (WMapMarker marker in _markers)
                {
                    DrawMarker(batcher, marker, gX, gY, halfWidth, halfHeight, Zoom);
                }
            }

            if (_showMobiles)
            {
                foreach (Mobile mob in World.Mobiles)
                {
                    if (mob == World.Player)
                        continue;

                    if (mob.NotorietyFlag != NotorietyFlag.Ally)
                        DrawMobile(batcher, mob, gX, gY, halfWidth, halfHeight, Zoom, Color.Red);
                    else
                    {
                        if (mob != null && mob.Distance <= World.ClientViewRange)
                        {
                            var wme = World.WMapManager.GetEntity(mob);
                            if (wme != null)
                                wme.Name = mob.Name;
                            else
                                DrawMobile(batcher, mob, gX, gY, halfWidth, halfHeight, Zoom, Color.Lime, true, true,
                                    true);
                        }
                        else
                        {
                            var wme = World.WMapManager.GetEntity(mob.Serial);
                            if (wme != null && wme.IsGuild)
                            {
                                DrawWMEntity(batcher, wme, gX, gY, halfWidth, halfHeight, Zoom);
                            }
                        }
                    }
                }
            }

            foreach (var wme in World.WMapManager.Entities.Values)
            {
                if (wme.IsGuild && !World.Party.Contains(wme.Serial))
                {
                    DrawWMEntity(batcher, wme, gX, gY, halfWidth, halfHeight, Zoom);
                }
            }

            if (_showPartyMembers)
            {
                for (int i = 0; i < 10; i++)
                {
                    var partyMember = World.Party.Members[i];

                    if (partyMember != null && SerialHelper.IsValid(partyMember.Serial))
                    {
                        var mob = World.Mobiles.Get(partyMember.Serial);

                        if (mob != null && mob.Distance <= World.ClientViewRange)
                        {
                            var wme = World.WMapManager.GetEntity(mob);
                            if (wme != null)
                                wme.Name = partyMember.Name;

                            DrawMobile(batcher, mob, gX, gY, halfWidth, halfHeight, Zoom, Color.Yellow, true, true,
                                true);
                        }
                        else
                        {
                            var wme = World.WMapManager.GetEntity(partyMember.Serial);
                            if (wme != null && !wme.IsGuild)
                            {
                                DrawWMEntity(batcher, wme, gX, gY, halfWidth, halfHeight, Zoom);
                            }
                        }
                    }
                }
            }

            DrawMobile(batcher, World.Player, gX, gY, halfWidth, halfHeight, Zoom, Color.White, _showPlayerName, false,
                _showPlayerBar);
        }

        private void DrawMobile(UltimaBatcher2D batcher, Mobile mobile, int x, int y, int width, int height, float zoom,
            Color color, bool drawName = false, bool isparty = false, bool drawHpBar = false)
        {
            ResetHueVector();

            int sx = mobile.X - _center.X;
            int sy = mobile.Y - _center.Y;

            (int rotX, int rotY) = RotatePoint(sx, sy, zoom, 1, _flipMap ? 45f : 0f);
            AdjustPosition(rotX, rotY, width - 4, height - 4, out rotX, out rotY);

            rotX += x + width;
            rotY += y + height;

            const int DOT_SIZE = 4;
            const int DOT_SIZE_HALF = DOT_SIZE >> 1;

            if (rotX < x)
                rotX = x;

            if (rotX > x + Width - 8 - DOT_SIZE)
                rotX = x + Width - 8 - DOT_SIZE;

            if (rotY < y)
                rotY = y;

            if (rotY > y + Height - 8 - DOT_SIZE)
                rotY = y + Height - 8 - DOT_SIZE;

            batcher.Draw2D(Texture2DCache.GetTexture(color), rotX - DOT_SIZE_HALF, rotY - DOT_SIZE_HALF, DOT_SIZE,
                DOT_SIZE, ref _hueVector);

            if (drawName && !string.IsNullOrEmpty(mobile.Name))
            {
                Vector2 size = Fonts.Regular.MeasureString(mobile.Name);

                if (rotX + size.X / 2 > x + Width - 8)
                {
                    rotX = x + Width - 8 - (int)(size.X / 2);
                }
                else if (rotX - size.X / 2 < x)
                {
                    rotX = x + (int)(size.X / 2);
                }

                if (rotY + size.Y > y + Height)
                {
                    rotY = y + Height - (int)(size.Y);
                }
                else if (rotY - size.Y < y)
                {
                    rotY = y + (int)size.Y;
                }

                int xx = (int)(rotX - size.X / 2);
                int yy = (int)(rotY - size.Y);

                _hueVector.X = 0;
                _hueVector.Y = 1;
                batcher.DrawString(Fonts.Regular, mobile.Name, xx + 1, yy + 1, ref _hueVector);
                ResetHueVector();
                _hueVector.X = isparty ? 0x0034 : Notoriety.GetHue(mobile.NotorietyFlag);
                _hueVector.Y = 1;
                batcher.DrawString(Fonts.Regular, mobile.Name, xx, yy, ref _hueVector);
            }

            if (drawHpBar)
            {
                int ww = mobile.HitsMax;

                if (ww > 0)
                {
                    ww = mobile.Hits * 100 / ww;

                    if (ww > 100)
                        ww = 100;
                    else if (ww < 1)
                        ww = 0;
                }

                rotY += DOT_SIZE + 1;

                DrawHpBar(batcher, rotX, rotY, ww);
            }
        }

        private void DrawMarker(UltimaBatcher2D batcher, WMapMarker marker, int x, int y, int width, int height,
            float zoom, bool drawName = true)
        {
            if (marker.Hidden)
                return;

            if (marker.MapId != World.MapIndex)
                return;

            ResetHueVector();

            int sx = marker.X - _center.X;
            int sy = marker.Y - _center.Y;

            (int rotX, int rotY) = RotatePoint(sx, sy, zoom, 1, _flipMap ? 45f : 0f);

            rotX += x + width;
            rotY += y + height;

            const int DOT_SIZE = 4;
            const int DOT_SIZE_HALF = DOT_SIZE >> 1;

            if (rotX < x ||
                rotX > x + Width - 8 - DOT_SIZE ||
                rotY < y ||
                rotY > y + Height - 8 - DOT_SIZE)
                return;

            batcher.Draw2D(Texture2DCache.GetTexture(marker.Color), rotX - DOT_SIZE_HALF, rotY - DOT_SIZE_HALF,
                DOT_SIZE,
                DOT_SIZE, ref _hueVector);

            if (drawName && !string.IsNullOrEmpty(marker.Name))
            {
                Vector2 size = Fonts.Regular.MeasureString(marker.Name);

                if (rotX + size.X / 2 > x + Width - 8)
                {
                    rotX = x + Width - 8 - (int)(size.X / 2);
                }
                else if (rotX - size.X / 2 < x)
                {
                    rotX = x + (int)(size.X / 2);
                }

                if (rotY + size.Y > y + Height)
                {
                    rotY = y + Height - (int)(size.Y);
                }
                else if (rotY - size.Y < y)
                {
                    rotY = y + (int)size.Y;
                }

                if (_zoomIndex < 6) return;
                //if (_currentMarkerCount > 50) return;

                int xx = (int)(rotX - size.X / 2);
                int yy = (int)(rotY - size.Y);

                _hueVector.X = 0;
                _hueVector.Y = 1;
                batcher.DrawString(Fonts.Regular, marker.Name, xx + 1, yy + 1, ref _hueVector);
                ResetHueVector();
                _hueVector.X = 0x0034;
                _hueVector.Y = 1;
                batcher.DrawString(Fonts.Regular, marker.Name, xx, yy, ref _hueVector);
            }
        }


        private void DrawWMEntity(UltimaBatcher2D batcher, WMapEntity entity, int x, int y, int width, int height,
            float zoom)
        {
            ResetHueVector();

            ushort uohue;
            Color color;

            if (entity.IsGuild)
            {
                uohue = 0x0044;
                color = Color.LimeGreen;
            }
            else
            {
                uohue = 0x0034;
                color = Color.Yellow;
            }

            if (entity.Map != World.MapIndex)
            {
                uohue = 992;
                color = Color.DarkGray;
            }

            int sx = entity.X - _center.X;
            int sy = entity.Y - _center.Y;

            (int rotX, int rotY) = RotatePoint(sx, sy, zoom, 1, _flipMap ? 45f : 0f);
            AdjustPosition(rotX, rotY, width - 4, height - 4, out rotX, out rotY);

            rotX += x + width;
            rotY += y + height;

            const int DOT_SIZE = 4;
            const int DOT_SIZE_HALF = DOT_SIZE >> 1;

            if (rotX < x)
                rotX = x;

            if (rotX > x + Width - 8 - DOT_SIZE)
                rotX = x + Width - 8 - DOT_SIZE;

            if (rotY < y)
                rotY = y;

            if (rotY > y + Height - 8 - DOT_SIZE)
                rotY = y + Height - 8 - DOT_SIZE;

            batcher.Draw2D(Texture2DCache.GetTexture(color), rotX - DOT_SIZE_HALF, rotY - DOT_SIZE_HALF, DOT_SIZE,
                DOT_SIZE, ref _hueVector);

            //string name = entity.GetName();
            string name = entity.Name ?? "<out of range>";
            Vector2 size = Fonts.Regular.MeasureString(entity.Name ?? name);

            if (rotX + size.X / 2 > x + Width - 8)
            {
                rotX = x + Width - 8 - (int)(size.X / 2);
            }
            else if (rotX - size.X / 2 < x)
            {
                rotX = x + (int)(size.X / 2);
            }

            if (rotY + size.Y > y + Height)
            {
                rotY = y + Height - (int)(size.Y);
            }
            else if (rotY - size.Y < y)
            {
                rotY = y + (int)size.Y;
            }

            int xx = (int)(rotX - size.X / 2);
            int yy = (int)(rotY - size.Y);

            _hueVector.X = 0;
            _hueVector.Y = 1;
            batcher.DrawString(Fonts.Regular, name, xx + 1, yy + 1, ref _hueVector);
            ResetHueVector();
            _hueVector.X = uohue;
            _hueVector.Y = 1;
            batcher.DrawString(Fonts.Regular, name, xx, yy, ref _hueVector);

            rotY += DOT_SIZE + 1;

            DrawHpBar(batcher, rotX, rotY, entity.HP);
        }

        private void DrawHpBar(UltimaBatcher2D batcher, int x, int y, int hp)
        {
            ResetHueVector();

            const int BAR_MAX_WIDTH = 25;
            const int BAR_MAX_WIDTH_HALF = BAR_MAX_WIDTH / 2;

            const int BAR_MAX_HEIGHT = 3;
            const int BAR_MAX_HEIGHT_HALF = BAR_MAX_HEIGHT / 2;


            batcher.Draw2D(Texture2DCache.GetTexture(Color.Black), x - BAR_MAX_WIDTH_HALF - 1,
                y - BAR_MAX_HEIGHT_HALF - 1, BAR_MAX_WIDTH + 2, BAR_MAX_HEIGHT + 2, ref _hueVector);
            batcher.Draw2D(Texture2DCache.GetTexture(Color.Red), x - BAR_MAX_WIDTH_HALF, y - BAR_MAX_HEIGHT_HALF,
                BAR_MAX_WIDTH, BAR_MAX_HEIGHT, ref _hueVector);

            int max = 100;
            int current = hp;

            if (max > 0)
            {
                max = current * 100 / max;

                if (max > 100)
                    max = 100;

                if (max > 1)
                    max = BAR_MAX_WIDTH * max / 100;
            }

            batcher.Draw2D(Texture2DCache.GetTexture(Color.CornflowerBlue), x - BAR_MAX_WIDTH_HALF,
                y - BAR_MAX_HEIGHT_HALF, max, BAR_MAX_HEIGHT, ref _hueVector);
        }

        private (int, int) RotatePoint(int x, int y, float zoom, int dist, float angle = 45f)
        {
            x = (int)(x * zoom);
            y = (int)(y * zoom);

            if (angle == 0.0f)
                return (x, y);

            return ((int)Math.Round(Math.Cos(dist * Math.PI / 4.0) * x - Math.Sin(dist * Math.PI / 4.0) * y),
                (int)Math.Round(Math.Sin(dist * Math.PI / 4.0) * x + Math.Cos(dist * Math.PI / 4.0) * y));
        }

        private void AdjustPosition(int x, int y, int centerX, int centerY, out int newX, out int newY)
        {
            var offset = GetOffset(x, y, centerX, centerY);
            var currX = x;
            var currY = y;

            while (offset != 0)
            {
                if ((offset & 1) != 0)
                {
                    currY = centerY;
                    currX = x * currY / y;
                }
                else if ((offset & 2) != 0)
                {
                    currY = -centerY;
                    currX = x * currY / y;
                }
                else if ((offset & 4) != 0)
                {
                    currX = centerX;
                    currY = y * currX / x;
                }
                else if ((offset & 8) != 0)
                {
                    currX = -centerX;
                    currY = y * currX / x;
                }

                x = currX;
                y = currY;
                offset = GetOffset(x, y, centerX, centerY);
            }

            newX = x;
            newY = y;
        }

        private int GetOffset(int x, int y, int centerX, int centerY)
        {
            const int offset = 0;
            if (y > centerY)
                return 1;
            if (y < -centerY)
                return 2;
            if (x > centerX)
                return offset + 4;
            if (x >= -centerX)
                return offset;
            return offset + 8;
        }

        public override void Dispose()
        {
            World.WMapManager.SetEnable(false);

            UIManager.GameCursor.IsDraggingCursorForced = false;

            _mapTexture?.Dispose();
            base.Dispose();
        }

        private Color GetColor(string name)
        {
            if (name.Equals("red", StringComparison.OrdinalIgnoreCase))
                return Color.Red;

            if (name.Equals("green", StringComparison.OrdinalIgnoreCase))
                return Color.Green;

            if (name.Equals("blue", StringComparison.OrdinalIgnoreCase))
                return Color.Blue;

            if (name.Equals("purple", StringComparison.OrdinalIgnoreCase))
                return Color.Purple;

            if (name.Equals("black", StringComparison.OrdinalIgnoreCase))
                return Color.Black;

            if (name.Equals("yellow", StringComparison.OrdinalIgnoreCase))
                return Color.Yellow;

            return Color.White;
        }
    }
}