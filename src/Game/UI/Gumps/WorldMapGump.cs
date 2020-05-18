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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ClassicUO.Configuration;
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
using Microsoft.Xna.Framework.Graphics;
using SpriteFont = ClassicUO.Renderer.SpriteFont;


namespace ClassicUO.Game.UI.Gumps
{
    internal class WorldMapGump : ResizableGump
    {
        private UOTexture _mapTexture;

        private readonly float[] _zooms = new float[10] { 0.125f, 0.25f, 0.5f, 0.75f, 1f, 1.5f, 2f, 4f, 6f, 8f };
        private int _zoomIndex = 4;
        private Point _center, _lastScroll;
        private bool _isScrolling;
        private int _mapIndex;
        private int _lastX;
        private int _lastY;
        private int _lastZ;
        private int _lastZoom;
        private bool _mapMarkersLoaded = false;
        private Label _coords;
        private readonly string _mapFilesPath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client");
        private readonly string _mapIconsPath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client", "MapIcons");

        private static Point _last_position = new Point(100, 100);



        private bool _flipMap = true;
        private bool _freeView;
        private bool _showPartyMembers = true;
        private bool _isTopMost;
        private bool _showCoordinates;
        private bool _showMobiles = true;
        private bool _showPlayerName = true;
        private bool _showPlayerBar = true;
        private bool _showGroupName = true;
        private bool _showGroupBar = true;
        private bool _showMultis = true;
        private bool _showMarkers = true;
        private bool _showMarkerNames = true;
        private bool _showMarkerIcons = true;

        private SpriteFont _markerFont = Fonts.Map1;
        private int _markerFontIndex = 1;

        private readonly Dictionary<string, ContextMenuItemEntry> _options = new Dictionary<string, ContextMenuItemEntry>();



        private List<WMapMarkerFile> _markerFiles = new List<WMapMarkerFile>();
        private Dictionary<string, Texture2D> _markerIcons = new Dictionary<string, Texture2D>();

        public WorldMapGump() : base(400, 400, 100, 100, 0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = false;
            X = _last_position.X;
            Y = _last_position.Y;

            LoadSettings();

            GameActions.Print("WorldMap loading...", 0x35);
            Load();
            OnResize();

            LoadMarkers();

            World.WMapManager.SetEnable(true);
            
            BuildGump();
        }

        public override GUMP_TYPE GumpType => GUMP_TYPE.GT_WORLDMAP;
        public float Zoom => _zooms[_zoomIndex];
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



        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            BuildGump();
        }

        private void LoadSettings()
        {
            Width = ProfileManager.Current.WorldMapWidth;
            Height = ProfileManager.Current.WorldMapHeight;

            SetFont(ProfileManager.Current.WorldMapFont);

            ResizeWindow(new Point(Width, Height));

            _flipMap = ProfileManager.Current.WorldMapFlipMap;
            TopMost = ProfileManager.Current.WorldMapTopMost;
            FreeView = ProfileManager.Current.WorldMapFreeView;
            _showPartyMembers = ProfileManager.Current.WorldMapShowParty;

            World.WMapManager.SetEnable(_showPartyMembers);

            _zoomIndex = ProfileManager.Current.WorldMapZoomIndex;

            _showCoordinates = ProfileManager.Current.WorldMapShowCoordinates;
            _showMobiles = ProfileManager.Current.WorldMapShowMobiles;

            _showPlayerName = ProfileManager.Current.WorldMapShowPlayerName;
            _showPlayerBar = ProfileManager.Current.WorldMapShowPlayerBar;
            _showGroupName = ProfileManager.Current.WorldMapShowGroupName;
            _showGroupBar = ProfileManager.Current.WorldMapShowGroupBar;
            _showMarkers = ProfileManager.Current.WorldMapShowMarkers;
            _showMultis = ProfileManager.Current.WorldMapShowMultis;
            _showMarkerNames = ProfileManager.Current.WorldMapShowMarkersNames;
        }

        public void SaveSettings()
        {
            if (ProfileManager.Current == null)
                return;


            ProfileManager.Current.WorldMapWidth = Width;
            ProfileManager.Current.WorldMapHeight = Height;

            ProfileManager.Current.WorldMapFlipMap = _flipMap;
            ProfileManager.Current.WorldMapTopMost = TopMost;
            ProfileManager.Current.WorldMapFreeView = FreeView;
            ProfileManager.Current.WorldMapShowParty = _showPartyMembers;

            ProfileManager.Current.WorldMapZoomIndex = _zoomIndex;

            ProfileManager.Current.WorldMapShowCoordinates = _showCoordinates;
            ProfileManager.Current.WorldMapShowMobiles = _showMobiles;

            ProfileManager.Current.WorldMapShowPlayerName = _showPlayerName;
            ProfileManager.Current.WorldMapShowPlayerBar = _showPlayerBar;
            ProfileManager.Current.WorldMapShowGroupName = _showGroupName;
            ProfileManager.Current.WorldMapShowGroupBar = _showGroupBar;
            ProfileManager.Current.WorldMapShowMarkers = _showMarkers;
            ProfileManager.Current.WorldMapShowMultis = _showMultis;
            ProfileManager.Current.WorldMapShowMarkersNames = _showMarkerNames;
        }

        private bool ParseBool(string boolStr)
        {
            return bool.TryParse(boolStr, out bool value) && value;
        }

        private void BuildGump()
        {
            BuildContextMenu();
            _coords?.Dispose();
            Add(_coords = new Label("", true, 1001, font: 1, style: FontStyle.BlackBorder)
            {
                X = 10,
                Y = 5
            });
        }

        private void BuildOptionDictionary()
        {
            _options.Clear();

            _options["show_all_markers"] = new ContextMenuItemEntry("Show all markers", () => { _showMarkers = !_showMarkers; }, true, _showMarkers);
            _options["show_marker_names"] = new ContextMenuItemEntry("Show marker names", () => { _showMarkerNames = !_showMarkerNames; }, true, _showMarkerNames);
            _options["show_marker_icons"] = new ContextMenuItemEntry("Show marker icons", () => { _showMarkerIcons = !_showMarkerIcons; }, true, _showMarkerIcons);
            _options["flip_map"] = new ContextMenuItemEntry("Flip map", () => { _flipMap = !_flipMap; }, true, _flipMap);
            _options["top_most"] = new ContextMenuItemEntry("TopMost", () => { TopMost = !TopMost; }, true, _isTopMost);
            _options["free_view"] = new ContextMenuItemEntry("Free view", () => { FreeView = !FreeView; }, true, FreeView);
            _options["show_party_members"] = new ContextMenuItemEntry("Show party members", () =>
            {
                _showPartyMembers = !_showPartyMembers;

                World.WMapManager.SetEnable(_showPartyMembers);

            }, true, _showPartyMembers);
            _options["show_mobiles"] = new ContextMenuItemEntry("Show mobiles", () => { _showMobiles = !_showMobiles; }, true, _showMobiles);
            _options["show_multis"] = new ContextMenuItemEntry("Show houses/boats", () => { _showMultis = !_showMultis; }, true, _showMultis);
            _options["show_your_name"] = new ContextMenuItemEntry("Show your name", () => { _showPlayerName = !_showPlayerName; }, true, _showPlayerName);
            _options["show_your_healthbar"] = new ContextMenuItemEntry("Show your healthbar", () => { _showPlayerBar = !_showPlayerBar; }, true, _showPlayerBar);
            _options["show_party_name"] = new ContextMenuItemEntry("Show group name", () => { _showGroupName = !_showGroupName; }, true, _showGroupName);
            _options["show_party_healthbar"] = new ContextMenuItemEntry("Show group healthbar", () => { _showGroupBar = !_showGroupBar; }, true, _showGroupBar);
            _options["show_coordinates"] = new ContextMenuItemEntry("Show your coordinates", () => { _showCoordinates = !_showCoordinates; }, true, _showCoordinates);

            _options["saveclose"] = new ContextMenuItemEntry("Save & Close", Dispose);
        }

        private void BuildContextMenu()
        {
            BuildOptionDictionary();

            ContextMenu = new ContextMenuControl();

            ContextMenuItemEntry markerFontEntry = new ContextMenuItemEntry("Font Style");
            markerFontEntry.Add(new ContextMenuItemEntry("Style 1", () => { SetFont(1); }));
            markerFontEntry.Add(new ContextMenuItemEntry("Style 2", () => { SetFont(2); }));
            markerFontEntry.Add(new ContextMenuItemEntry("Style 3", () => { SetFont(3); }));
            markerFontEntry.Add(new ContextMenuItemEntry("Style 4", () => { SetFont(4); }));
            markerFontEntry.Add(new ContextMenuItemEntry("Style 5", () => { SetFont(5); }));
            markerFontEntry.Add(new ContextMenuItemEntry("Style 6", () => { SetFont(6); }));

            ContextMenuItemEntry markersEntry = new ContextMenuItemEntry("Map Marker Options");
            markersEntry.Add(new ContextMenuItemEntry("Reload markers", () => { LoadMarkers(); }));

            markersEntry.Add(markerFontEntry);

            markersEntry.Add(_options["show_all_markers"]);
            markersEntry.Add(new ContextMenuItemEntry(""));
            markersEntry.Add(_options["show_marker_names"]);
            markersEntry.Add(_options["show_marker_icons"]);
            markersEntry.Add(new ContextMenuItemEntry(""));

            if (_markerFiles.Count > 0)
            {
                foreach (WMapMarkerFile markerFile in _markerFiles)
                {
                    var entry = new ContextMenuItemEntry($"Show/Hide '{markerFile.Name}'", () => { markerFile.Hidden = !markerFile.Hidden; }, true, !markerFile.Hidden);
                    _options[$"show_marker_{markerFile.Name}"] = entry;
                    markersEntry.Add(entry);
                }
            }
            else
            {
                markersEntry.Add(new ContextMenuItemEntry("No map files"));
            }


            ContextMenu.Add(markersEntry);

            ContextMenuItemEntry namesHpBarEntry = new ContextMenuItemEntry("Names & Healthbars");
            namesHpBarEntry.Add(_options["show_your_name"]);
            namesHpBarEntry.Add(_options["show_your_healthbar"]);
            namesHpBarEntry.Add(_options["show_party_name"]);
            namesHpBarEntry.Add(_options["show_party_healthbar"]);

            ContextMenu.Add(namesHpBarEntry);

            ContextMenu.Add("", null);
            ContextMenu.Add(_options["flip_map"]);
            ContextMenu.Add(_options["top_most"]);
            ContextMenu.Add(_options["free_view"]);
            ContextMenu.Add("", null);
            ContextMenu.Add(_options["show_party_members"]);
            ContextMenu.Add(_options["show_mobiles"]);
            ContextMenu.Add(_options["show_multis"]);
            ContextMenu.Add(_options["show_coordinates"]);
            ContextMenu.Add("", null);
            ContextMenu.Add(_options["saveclose"]);
        }




        #region Loading

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

                    int bx, by, mapX, mapY, x, y;


                    for (bx = 0; bx < fixedWidth; bx++)
                    {
                        mapX = bx << 3;

                        for (by = 0; by < fixedHeight; by++)
                        {
                            ref IndexMap indexMap = ref World.Map.GetIndex(bx, by);

                            if (indexMap.MapAddress == 0)
                                continue;

                            MapBlock* mapBlock = (MapBlock*) indexMap.MapAddress;
                            MapCells* cells = (MapCells*) &mapBlock->Cells;

                            int pos = 0;
                            mapY = by << 3;

                            for (y = 0; y < 8; y++)
                            {
                                int block = (mapY + y + OFFSET_PIX_HALF) * (realWidth + OFFSET_PIX) + mapX + OFFSET_PIX_HALF;

                                for (x = 0; x < 8; x++)
                                {
                                    ref MapCells cell = ref cells[pos];

                                    ushort color = (ushort) (0x8000 | HuesLoader.Instance.GetRadarColorData(cell.TileID));

                                    ref var cc = ref buffer[block];
                                    cc.PackedValue = HuesHelper.Color16To32(color) | 0xFF_00_00_00;

                                    allZ[block] = cell.Z;

                                    block++;
                                    pos++;
                                }
                            }


                            StaticsBlock* sb = (StaticsBlock*) indexMap.StaticAddress;
                            if (sb != null)
                            {
                                int count = (int) indexMap.StaticCount;

                                for (int c = 0; c < count; c++)
                                {
                                    ref readonly StaticsBlock staticBlock = ref sb[c];

                                    if (staticBlock.Color != 0 && staticBlock.Color != 0xFFFF && !GameObjectHelper.IsNoDrawable(staticBlock.Color))
                                    {
                                        pos = (staticBlock.Y << 3) + staticBlock.X;
                                        ref MapCells cell = ref cells[pos];

                                        if (cell.Z <= staticBlock.Z)
                                        {
                                            ushort color = (ushort) (0x8000 | (staticBlock.Hue > 0
                                                                                  ? HuesLoader.Instance.GetColor16(16384,
                                                                                                                   staticBlock.Hue)
                                                                                  : HuesLoader.Instance.GetRadarColorData(
                                                                                                                          staticBlock.Color + 0x4000)));

                                            int block = (mapY + staticBlock.Y + OFFSET_PIX_HALF) *
                                                        (realWidth + OFFSET_PIX) + (mapX + staticBlock.X) +
                                                        OFFSET_PIX_HALF;

                                            ref var cc = ref buffer[block];
                                            cc.PackedValue = HuesHelper.Color16To32(color) | 0xFF_00_00_00;

                                            allZ[block] = staticBlock.Z;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    int real_width_less_one = realHeight - 1;
                    int real_height_less_one = realHeight - 1;
                    const float MAG_0 = 80f / 100f;
                    const float MAG_1 = 100f / 80f;

                    int mapY_plus_one;
                    int r, g, b;

                    for (mapY = 1; mapY < real_height_less_one; mapY++)
                    {
                        mapY_plus_one = mapY + 1;

                        for (mapX = 1; mapX < real_width_less_one; mapX++)
                        {
                            int blockCurrent = (mapY + OFFSET_PIX_HALF) * (realWidth + OFFSET_PIX) + mapX + OFFSET_PIX_HALF;
                            int blockNext = (mapY_plus_one + OFFSET_PIX_HALF) * (realWidth + OFFSET_PIX) + (mapX - 1) + OFFSET_PIX_HALF;

                            ref sbyte z0 = ref allZ[blockCurrent];
                            ref sbyte z1 = ref allZ[blockNext];

                            if (z0 == z1)
                                continue;

                            ref Color cc = ref buffer[blockCurrent];

                            if (z0 < z1)
                            {
                                r = (int) (cc.R * MAG_0);
                                g = (int) (cc.G * MAG_0);
                                b = (int) (cc.B * MAG_0);
                            }
                            else
                            {
                                r = (int) (cc.R * MAG_1);
                                g = (int) (cc.G * MAG_1);
                                b = (int) (cc.B * MAG_1);
                            }

                            if (r > 255)
                                r = 255;

                            if (g > 255)
                                g = 255;

                            if (b > 255)
                                b = 255;

                            cc.R = (byte) (r);
                            cc.G = (byte) (g);
                            cc.B = (byte) (b);
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
                    _mapMarkersLoaded = false;

                    GameActions.Print("Loading WorldMap markers..", 0x2A);

                    _markerIcons.Clear();

                    if (!Directory.Exists(_mapIconsPath))
                        Directory.CreateDirectory(_mapIconsPath);

                    foreach (string icon in Directory.GetFiles(_mapIconsPath, "*.cur")
                        .Union(Directory.GetFiles(_mapIconsPath, "*.png"))
                        .Union(Directory.GetFiles(_mapIconsPath, "*.jpg"))
                        .Union(Directory.GetFiles(_mapIconsPath, "*.ico")))
                    {
                        FileStream fs = new FileStream(icon, FileMode.Open, FileAccess.Read);
                        MemoryStream ms = new MemoryStream();

                        fs.CopyTo(ms);

                        _markerIcons.Add(Path.GetFileNameWithoutExtension(icon).ToLower(), Texture2D.FromStream(Client.Game.GraphicsDevice, ms));

                        ms.Dispose();
                        fs.Dispose();
                    }

                    string[] mapFiles = Directory.GetFiles(_mapFilesPath, "*.map").Union(Directory.GetFiles(_mapFilesPath, "*.csv"))
                        .Union(Directory.GetFiles(_mapFilesPath, "*.xml")).ToArray();

                    _markerFiles.Clear();

                    foreach (string mapFile in mapFiles)
                    {
                        if (File.Exists(mapFile))
                        {
                            WMapMarkerFile markerFile = new WMapMarkerFile
                            {
                                Hidden = false,
                                Name = Path.GetFileNameWithoutExtension(mapFile),
                                FullPath = mapFile,
                                Markers = new List<WMapMarker>()
                            };

                            if (mapFile != null && Path.GetExtension(mapFile).ToLower().Equals(".xml")) // Ultima Mapper
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
                                            Color = Color.White,
                                            ZoomIndex = 3
                                        };

                                        if (_markerIcons.TryGetValue(reader.GetAttribute("Icon").ToLower(), out Texture2D value))
                                        {
                                            marker.MarkerIcon = value;
                                            marker.MarkerIconName = reader.GetAttribute("Icon").ToLower();
                                        }

                                        markerFile.Markers.Add(marker);
                                    }
                                }
                            }
                            else if (mapFile != null && Path.GetExtension(mapFile).ToLower().Equals(".map")) //UOAM
                            {
                                using (StreamReader reader = new StreamReader(mapFile))
                                {
                                    while (!reader.EndOfStream)
                                    {
                                        string line = reader.ReadLine();

                                        // ignore empty lines, and if UOAM, ignore the first line that always has a 3
                                        if (string.IsNullOrEmpty(line) || line.Equals("3"))
                                            continue;

                                        // Check for UOAM file
                                        if (line.Substring(0, 1).Equals("+") || line.Substring(0, 1).Equals("-"))
                                        {
                                            string icon = line.Substring(1, line.IndexOf(':') - 1);

                                            line = line.Substring(line.IndexOf(':') + 2);

                                            string[] splits = line.Split(' ');

                                            if (splits.Length <= 1)
                                                continue;

                                            WMapMarker marker = new WMapMarker
                                            {
                                                X = int.Parse(splits[0]),
                                                Y = int.Parse(splits[1]),
                                                MapId = int.Parse(splits[2]),
                                                Name = string.Join(" ", splits, 3, splits.Length - 3),
                                                Color = Color.White,
                                                ZoomIndex = 3
                                            };

                                            string[] iconSplits = icon.Split(' ');
                                            marker.MarkerIconName = iconSplits[0].ToLower();

                                            if (_markerIcons.TryGetValue(iconSplits[0].ToLower(), out Texture2D value))
                                                marker.MarkerIcon = value;

                                            markerFile.Markers.Add(marker);
                                        }
                                    }
                                }
                            }
                            else if (mapFile != null) //CSV x,y,mapindex,name of marker,iconname,color,zoom
                            {
                                using (StreamReader reader = new StreamReader(mapFile))
                                {
                                    while (!reader.EndOfStream)
                                    {
                                        string line = reader.ReadLine();

                                        if (string.IsNullOrEmpty(line))
                                            return;

                                        string[] splits = line.Split(',');

                                        if (splits.Length <= 1)
                                            continue;

                                        WMapMarker marker = new WMapMarker
                                        {
                                            X = int.Parse(splits[0]),
                                            Y = int.Parse(splits[1]),
                                            MapId = int.Parse(splits[2]),
                                            Name = splits[3],
                                            MarkerIconName = splits[4].ToLower(),
                                            Color = GetColor(splits[5]),
                                            ZoomIndex = splits.Length == 7 ? int.Parse(splits[6]) : 3
                                        };

                                        if (_markerIcons.TryGetValue(splits[4].ToLower(), out Texture2D value))
                                            marker.MarkerIcon = value;

                                        markerFile.Markers.Add(marker);
                                    }
                                }
                            }

                            if (markerFile.Markers.Count > 0)
                            {
                                GameActions.Print($"..{Path.GetFileName(mapFile)} ({markerFile.Markers.Count})", 0x2B);
                                _markerFiles.Add(markerFile);
                            }
                        }
                    }

                    BuildContextMenu();

                    int count = 0;
                    foreach (WMapMarkerFile file in _markerFiles)
                    {
                        count += file.Markers.Count;
                    }

                    _mapMarkersLoaded = true;

                    GameActions.Print($"WorldMap markers loaded ({count})", 0x2A);
                }
            });
        }

        #endregion


        #region Update

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (_mapIndex != World.MapIndex)
            {
                Load();
            }

            World.WMapManager.RequestServerPartyGuildInfo();
        }


        #endregion


        #region Draw

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

            int size = (int) Math.Max(gWidth * 1.75f, gHeight * 1.75f);

            int size_zoom = (int) (size / Zoom);
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
                if (World.Player.X != _lastX || World.Player.Y != _lastY || World.Player.Z != _lastZ || _zoomIndex != _lastZoom)
                {
                    _coords.Text = $"{World.Player.X}, {World.Player.Y} ({World.Player.Z}) [{_zoomIndex}]";
                    _lastX = World.Player.X;
                    _lastY = World.Player.Y;
                    _lastZ = World.Player.Z;
                    _lastZoom = _zoomIndex;
                }
            }
            else
            {
                _coords.Text = string.Empty;
            }

            if (_showMarkers && _mapMarkersLoaded)
            {
                foreach (WMapMarkerFile file in _markerFiles)
                {
                    if (file.Hidden)
                        continue;

                    foreach (WMapMarker marker in file.Markers)
                    {
                        DrawMarker(batcher, marker, gX, gY, halfWidth, halfHeight, Zoom);
                    }
                }
            }

            if (_showMultis)
            {
                foreach (House house in World.HouseManager.Houses)
                {
                    Item item = World.Items.Get(house.Serial);

                    if (item != null)
                        DrawMulti(batcher, item.X, item.Y, gX, gY, halfWidth, halfHeight, Zoom);
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
                                    _showGroupBar);
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

                            DrawMobile(batcher, mob, gX, gY, halfWidth, halfHeight, Zoom, Color.Yellow, _showGroupName, true,
                                _showGroupBar);
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
                    rotX = x + Width - 8 - (int) (size.X / 2);
                }
                else if (rotX - size.X / 2 < x)
                {
                    rotX = x + (int) (size.X / 2);
                }

                if (rotY + size.Y > y + Height)
                {
                    rotY = y + Height - (int) (size.Y);
                }
                else if (rotY - size.Y < y)
                {
                    rotY = y + (int) size.Y;
                }

                int xx = (int) (rotX - size.X / 2);
                int yy = (int) (rotY - size.Y);

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

        private void DrawMarker(UltimaBatcher2D batcher, WMapMarker marker, int x, int y, int width, int height, float zoom)
        {
            if (marker.MapId != World.MapIndex)
                return;

            if (_zoomIndex < marker.ZoomIndex && marker.Color == Color.Transparent)
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

            bool showMarkerName = _showMarkerNames && !string.IsNullOrEmpty(marker.Name) && _zoomIndex > 5;

            if (_zoomIndex < marker.ZoomIndex || !_showMarkerIcons || marker.MarkerIcon == null)
            {
                batcher.Draw2D(Texture2DCache.GetTexture(marker.Color), rotX - DOT_SIZE_HALF, rotY - DOT_SIZE_HALF,
                    DOT_SIZE,
                    DOT_SIZE, ref _hueVector);

                if (Mouse.Position.X >= rotX - DOT_SIZE && Mouse.Position.X <= rotX + DOT_SIZE_HALF &&
                    Mouse.Position.Y >= rotY - DOT_SIZE && Mouse.Position.Y <= rotY + DOT_SIZE_HALF)
                {
                    showMarkerName = true;
                }
            }
            else
            {
                batcher.Draw2D(marker.MarkerIcon, rotX - (marker.MarkerIcon.Width >> 1), rotY - (marker.MarkerIcon.Height >> 1), ref _hueVector);

                if (!showMarkerName)
                {
                    if (Mouse.Position.X >= rotX - (marker.MarkerIcon.Width >> 1) && Mouse.Position.X <= rotX + (marker.MarkerIcon.Width >> 1) &&
                        Mouse.Position.Y >= rotY - (marker.MarkerIcon.Height >> 1) && Mouse.Position.Y <= rotY + (marker.MarkerIcon.Height >> 1))
                    {
                        showMarkerName = true;
                    }
                }
            }

            if (showMarkerName)
            {
                Vector2 size = _markerFont.MeasureString(marker.Name);

                if (rotX + size.X / 2 > x + Width - 8)
                {
                    rotX = x + Width - 8 - (int) (size.X / 2);
                }
                else if (rotX - size.X / 2 < x)
                {
                    rotX = x + (int) (size.X / 2);
                }

                if (rotY + size.Y > y + Height)
                {
                    rotY = y + Height - (int) (size.Y);
                }
                else if (rotY - size.Y < y)
                {
                    rotY = y + (int) size.Y;
                }
                int xx = (int) (rotX - size.X / 2);
                int yy = (int) (rotY - size.Y - 5);

                _hueVector.X = 0;
                _hueVector.Y = 1;
                batcher.DrawString(_markerFont, marker.Name, xx + 1, yy + 1, ref _hueVector);
                ResetHueVector();
                batcher.DrawString(_markerFont, marker.Name, xx, yy, ref _hueVector);
            }
        }

        private void DrawMulti(UltimaBatcher2D batcher, int multiX, int multiY, int x, int y, int width, int height, float zoom)
        {
            ResetHueVector();

            int sx = multiX - _center.X;
            int sy = multiY - _center.Y;

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

            batcher.Draw2D(Texture2DCache.GetTexture(Color.Aquamarine), rotX - DOT_SIZE_HALF, rotY - DOT_SIZE_HALF,
                DOT_SIZE,
                DOT_SIZE, ref _hueVector);
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

            if (_showGroupName)
            {
                string name = entity.Name ?? "<out of range>";
                Vector2 size = Fonts.Regular.MeasureString(entity.Name ?? name);

                if (rotX + size.X / 2 > x + Width - 8)
                {
                    rotX = x + Width - 8 - (int) (size.X / 2);
                }
                else if (rotX - size.X / 2 < x)
                {
                    rotX = x + (int) (size.X / 2);
                }

                if (rotY + size.Y > y + Height)
                {
                    rotY = y + Height - (int) (size.Y);
                }
                else if (rotY - size.Y < y)
                {
                    rotY = y + (int) size.Y;
                }

                int xx = (int) (rotX - size.X / 2);
                int yy = (int) (rotY - size.Y);

                _hueVector.X = 0;
                _hueVector.Y = 1;
                batcher.DrawString(Fonts.Regular, name, xx + 1, yy + 1, ref _hueVector);
                ResetHueVector();
                _hueVector.X = uohue;
                _hueVector.Y = 1;
                batcher.DrawString(Fonts.Regular, name, xx, yy, ref _hueVector);
            }

            if (_showGroupBar)
            {
                rotY += DOT_SIZE + 1;
                DrawHpBar(batcher, rotX, rotY, entity.HP);
            }
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


        #endregion


        #region I/O

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left && !Keyboard.Alt)
            {
                _isScrolling = false;
                CanMove = true;
            }

            UIManager.GameCursor.IsDraggingCursorForced = false;

            base.OnMouseUp(x, y, button);
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if ((button == MouseButtonType.Left && (Keyboard.Alt || _freeView)) || (button == MouseButtonType.Middle))
            {
                if (x > 4 && x < Width - 8 && y > 4 && y < Height - 8)
                {
                    if (button == MouseButtonType.Middle)
                        FreeView = true;

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
            Point offset = Mouse.LButtonPressed ? Mouse.LDroppedOffset : Mouse.MButtonPressed ? Mouse.MDroppedOffset : Point.Zero;

            if (_isScrolling && offset != Point.Zero)
            {
                int scrollX = _lastScroll.X - x;
                int scrollY = _lastScroll.Y - y;

                (scrollX, scrollY) = RotatePoint(scrollX, scrollY, 1f, -1, _flipMap ? 45f : 0f);

                _center.X += (int) (scrollX / Zoom);
                _center.Y += (int) (scrollY / Zoom);

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

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left || _isScrolling || Keyboard.Alt)
                return base.OnMouseDoubleClick(x, y, button);

            TopMost = !TopMost;

            return true;
        }

        protected override void OnMove(int x, int y)
        {
            base.OnMove(x, y);
            _last_position.X = ScreenCoordinateX;
            _last_position.Y = ScreenCoordinateY;
        }

        #endregion







        private (int, int) RotatePoint(int x, int y, float zoom, int dist, float angle = 45f)
        {
            x = (int) (x * zoom);
            y = (int) (y * zoom);

            if (angle == 0.0f)
                return (x, y);

            return ((int) Math.Round(Math.Cos(dist * Math.PI / 4.0) * x - Math.Sin(dist * Math.PI / 4.0) * y),
                (int) Math.Round(Math.Sin(dist * Math.PI / 4.0) * x + Math.Cos(dist * Math.PI / 4.0) * y));
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
            SaveSettings();
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

            if (name.Equals("white", StringComparison.OrdinalIgnoreCase))
                return Color.White;

            if (name.Equals("none", StringComparison.OrdinalIgnoreCase))
                return Color.Transparent;

            return Color.White;
        }

        private void SetFont(int fontIndex)
        {
            _markerFontIndex = fontIndex;

            switch (fontIndex)
            {
                case 1:
                    _markerFont = Fonts.Map1;
                    break;
                case 2:
                    _markerFont = Fonts.Map2;
                    break;
                case 3:
                    _markerFont = Fonts.Map3;
                    break;
                case 4:
                    _markerFont = Fonts.Map4;
                    break;
                case 5:
                    _markerFont = Fonts.Map5;
                    break;
                case 6:
                    _markerFont = Fonts.Map6;
                    break;
                default:
                    _markerFontIndex = 1;
                    _markerFont = Fonts.Map1;
                    break;
            }
        }

        private bool GetOptionValue(string key)
        {
            _options.TryGetValue(key, out var v);

            return v != null && v.IsSelected;
        }

        public void SetOptionValue(string key, bool v)
        {
            if (_options.TryGetValue(key, out var entry) && entry != null)
            {
                entry.IsSelected = v;
            }
        }


        private class WMapMarker
        {
            public string Name { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public int MapId { get; set; }
            public Color Color { get; set; }
            public Texture2D MarkerIcon { get; set; }
            public string MarkerIconName { get; set; }
            public int ZoomIndex { get; set; }
        }

        private class WMapMarkerFile
        {
            public string Name { get; set; }
            public string FullPath { get; set; }
            public List<WMapMarker> Markers { get; set; }
            public bool Hidden { get; set; }
        }

    }
}