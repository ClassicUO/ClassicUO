﻿#region license

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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL2;
using SpriteFont = ClassicUO.Renderer.SpriteFont;

namespace ClassicUO.Game.UI.Gumps
{
    internal class WorldMapGump : ResizableGump
    {
        private static Point _last_position = new Point(100, 100);
        private Point _center, _lastScroll;

        private bool _flipMap = true;
        private bool _freeView;
        private List<string> _hiddenMarkerFiles;
        private bool _isScrolling;
        private bool _isTopMost;
        private readonly string _mapFilesPath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client");
        private readonly string _mapIconsPath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client", "MapIcons");
        private int _mapIndex;
        private bool _mapMarkersLoaded;
        private UOTexture _mapTexture;


        private readonly List<WMapMarkerFile> _markerFiles = new List<WMapMarkerFile>();

        private SpriteFont _markerFont = Fonts.Map1;
        private int _markerFontIndex = 1;
        private readonly Dictionary<string, Texture2D> _markerIcons = new Dictionary<string, Texture2D>();

        private readonly Dictionary<string, ContextMenuItemEntry> _options = new Dictionary<string, ContextMenuItemEntry>();
        private bool _showCoordinates;
        private bool _showGroupBar = true;
        private bool _showGroupName = true;
        private bool _showMarkerIcons = true;
        private bool _showMarkerNames = true;
        private bool _showMarkers = true;
        private bool _showMobiles = true;
        private bool _showMultis = true;
        private bool _showPartyMembers = true;
        private bool _showPlayerBar = true;
        private bool _showPlayerName = true;
        private int _zoomIndex = 4;

        private WMapMarker _gotoMarker;

        private readonly float[] _zooms = new float[10] { 0.125f, 0.25f, 0.5f, 0.75f, 1f, 1.5f, 2f, 4f, 6f, 8f };

        public WorldMapGump() : base
        (
            400,
            400,
            100,
            100,
            0,
            0
        )
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = false;

            X = _last_position.X;
            Y = _last_position.Y;

            LoadSettings();

            GameActions.Print(ResGumps.WorldMapLoading, 0x35);
            Load();
            OnResize();

            LoadMarkers();

            World.WMapManager.SetEnable(true);

            BuildGump();
        }

        public override GumpType GumpType => GumpType.WorldMap;
        public float Zoom => _zooms[_zoomIndex];

        public bool TopMost
        {
            get => _isTopMost;
            set
            {
                _isTopMost = value;

                ShowBorder = !_isTopMost;

                LayerOrder = _isTopMost ? UILayer.Over : UILayer.Under;
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
            Width = ProfileManager.CurrentProfile.WorldMapWidth;
            Height = ProfileManager.CurrentProfile.WorldMapHeight;

            SetFont(ProfileManager.CurrentProfile.WorldMapFont);

            ResizeWindow(new Point(Width, Height));

            _flipMap = ProfileManager.CurrentProfile.WorldMapFlipMap;
            TopMost = ProfileManager.CurrentProfile.WorldMapTopMost;
            FreeView = ProfileManager.CurrentProfile.WorldMapFreeView;
            _showPartyMembers = ProfileManager.CurrentProfile.WorldMapShowParty;

            World.WMapManager.SetEnable(_showPartyMembers);

            _zoomIndex = ProfileManager.CurrentProfile.WorldMapZoomIndex;

            _showCoordinates = ProfileManager.CurrentProfile.WorldMapShowCoordinates;
            _showMobiles = ProfileManager.CurrentProfile.WorldMapShowMobiles;

            _showPlayerName = ProfileManager.CurrentProfile.WorldMapShowPlayerName;
            _showPlayerBar = ProfileManager.CurrentProfile.WorldMapShowPlayerBar;
            _showGroupName = ProfileManager.CurrentProfile.WorldMapShowGroupName;
            _showGroupBar = ProfileManager.CurrentProfile.WorldMapShowGroupBar;
            _showMarkers = ProfileManager.CurrentProfile.WorldMapShowMarkers;
            _showMultis = ProfileManager.CurrentProfile.WorldMapShowMultis;
            _showMarkerNames = ProfileManager.CurrentProfile.WorldMapShowMarkersNames;


            _hiddenMarkerFiles = string.IsNullOrEmpty(ProfileManager.CurrentProfile.WorldMapHiddenMarkerFiles) ? new List<string>() : ProfileManager.CurrentProfile.WorldMapHiddenMarkerFiles.Split(',').ToList();
        }

        public void SaveSettings()
        {
            if (ProfileManager.CurrentProfile == null)
            {
                return;
            }


            ProfileManager.CurrentProfile.WorldMapWidth = Width;
            ProfileManager.CurrentProfile.WorldMapHeight = Height;

            ProfileManager.CurrentProfile.WorldMapFlipMap = _flipMap;
            ProfileManager.CurrentProfile.WorldMapTopMost = TopMost;
            ProfileManager.CurrentProfile.WorldMapFreeView = FreeView;
            ProfileManager.CurrentProfile.WorldMapShowParty = _showPartyMembers;

            ProfileManager.CurrentProfile.WorldMapZoomIndex = _zoomIndex;

            ProfileManager.CurrentProfile.WorldMapShowCoordinates = _showCoordinates;
            ProfileManager.CurrentProfile.WorldMapShowMobiles = _showMobiles;

            ProfileManager.CurrentProfile.WorldMapShowPlayerName = _showPlayerName;
            ProfileManager.CurrentProfile.WorldMapShowPlayerBar = _showPlayerBar;
            ProfileManager.CurrentProfile.WorldMapShowGroupName = _showGroupName;
            ProfileManager.CurrentProfile.WorldMapShowGroupBar = _showGroupBar;
            ProfileManager.CurrentProfile.WorldMapShowMarkers = _showMarkers;
            ProfileManager.CurrentProfile.WorldMapShowMultis = _showMultis;
            ProfileManager.CurrentProfile.WorldMapShowMarkersNames = _showMarkerNames;

            ProfileManager.CurrentProfile.WorldMapHiddenMarkerFiles = string.Join(",", _hiddenMarkerFiles);
        }

        private bool ParseBool(string boolStr)
        {
            return bool.TryParse(boolStr, out bool value) && value;
        }

        private void BuildGump()
        {
            BuildContextMenu();
        }

        private void BuildOptionDictionary()
        {
            _options.Clear();

            _options["show_all_markers"] = new ContextMenuItemEntry(ResGumps.ShowAllMarkers, () => { _showMarkers = !_showMarkers; }, true, _showMarkers);

            _options["show_marker_names"] = new ContextMenuItemEntry(ResGumps.ShowMarkerNames, () => { _showMarkerNames = !_showMarkerNames; }, true, _showMarkerNames);

            _options["show_marker_icons"] = new ContextMenuItemEntry(ResGumps.ShowMarkerIcons, () => { _showMarkerIcons = !_showMarkerIcons; }, true, _showMarkerIcons);

            _options["flip_map"] = new ContextMenuItemEntry(ResGumps.FlipMap, () => { _flipMap = !_flipMap; }, true, _flipMap);

            _options["goto_location"] = new ContextMenuItemEntry
            (
                ResGumps.GotoLocation,
                () =>
                {
                    EntryDialog dialog = new EntryDialog
                    (
                        250,
                        150,
                        ResGumps.EnterLocation,
                        name =>
                        {
                            _gotoMarker = null;

                            if (string.IsNullOrWhiteSpace(name))
                            {
                                GameActions.Print(ResGumps.InvalidLocation, 0x35);

                                return;
                            }

                            int x = -1;
                            int y = -1;

                            string[] coords = name.Split(' ');

                            if (coords.Length < 2)
                            {
                                try
                                {
                                    ConvertCoords(name, ref x, ref y);
                                }
                                catch
                                {
                                    GameActions.Print(ResGumps.InvalidLocation, 0x35);
                                }
                            }
                            else
                            {
                                if (!int.TryParse(coords[0], out x))
                                {
                                    GameActions.Print(ResGumps.InvalidLocation, 0x35);
                                }

                                if (!int.TryParse(coords[1], out y))
                                {
                                    GameActions.Print(ResGumps.InvalidLocation, 0x35);
                                }
                            }

                            if (x != -1 && y != -1)
                            {
                                FreeView = true;

                                _gotoMarker = new WMapMarker
                                {
                                    Color = Color.Aquamarine,
                                    MapId = World.MapIndex,
                                    Name = $"Go to: {x}, {y}",
                                    X = x,
                                    Y = y,
                                    ZoomIndex = 1
                                };

                                _center.X = x;
                                _center.Y = y;
                            }
                        }
                    )
                    {
                        CanCloseWithRightClick = true
                    };

                    UIManager.Add(dialog);
                }
            );

            _options["top_most"] = new ContextMenuItemEntry(ResGumps.TopMost, () => { TopMost = !TopMost; }, true, _isTopMost);

            _options["free_view"] = new ContextMenuItemEntry(ResGumps.FreeView, () => { FreeView = !FreeView; }, true, FreeView);

            _options["show_party_members"] = new ContextMenuItemEntry
            (
                ResGumps.ShowPartyMembers,
                () =>
                {
                    _showPartyMembers = !_showPartyMembers;

                    World.WMapManager.SetEnable(_showPartyMembers);
                },
                true,
                _showPartyMembers
            );

            _options["show_mobiles"] = new ContextMenuItemEntry(ResGumps.ShowMobiles, () => { _showMobiles = !_showMobiles; }, true, _showMobiles);

            _options["show_multis"] = new ContextMenuItemEntry(ResGumps.ShowHousesBoats, () => { _showMultis = !_showMultis; }, true, _showMultis);

            _options["show_your_name"] = new ContextMenuItemEntry(ResGumps.ShowYourName, () => { _showPlayerName = !_showPlayerName; }, true, _showPlayerName);

            _options["show_your_healthbar"] = new ContextMenuItemEntry(ResGumps.ShowYourHealthbar, () => { _showPlayerBar = !_showPlayerBar; }, true, _showPlayerBar);

            _options["show_party_name"] = new ContextMenuItemEntry(ResGumps.ShowGroupName, () => { _showGroupName = !_showGroupName; }, true, _showGroupName);

            _options["show_party_healthbar"] = new ContextMenuItemEntry(ResGumps.ShowGroupHealthbar, () => { _showGroupBar = !_showGroupBar; }, true, _showGroupBar);

            _options["show_coordinates"] = new ContextMenuItemEntry(ResGumps.ShowYourCoordinates, () => { _showCoordinates = !_showCoordinates; }, true, _showCoordinates);

            _options["saveclose"] = new ContextMenuItemEntry(ResGumps.SaveClose, Dispose);
        }

        private void BuildContextMenu()
        {
            BuildOptionDictionary();

            ContextMenu?.Dispose();
            ContextMenu = new ContextMenuControl();

            ContextMenuItemEntry markerFontEntry = new ContextMenuItemEntry(ResGumps.FontStyle);
            markerFontEntry.Add(new ContextMenuItemEntry(string.Format(ResGumps.Style0, 1), () => { SetFont(1); }));
            markerFontEntry.Add(new ContextMenuItemEntry(string.Format(ResGumps.Style0, 2), () => { SetFont(2); }));
            markerFontEntry.Add(new ContextMenuItemEntry(string.Format(ResGumps.Style0, 3), () => { SetFont(3); }));
            markerFontEntry.Add(new ContextMenuItemEntry(string.Format(ResGumps.Style0, 4), () => { SetFont(4); }));
            markerFontEntry.Add(new ContextMenuItemEntry(string.Format(ResGumps.Style0, 5), () => { SetFont(5); }));
            markerFontEntry.Add(new ContextMenuItemEntry(string.Format(ResGumps.Style0, 6), () => { SetFont(6); }));

            ContextMenuItemEntry markersEntry = new ContextMenuItemEntry(ResGumps.MapMarkerOptions);
            markersEntry.Add(new ContextMenuItemEntry(ResGumps.ReloadMarkers, LoadMarkers));

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
                    ContextMenuItemEntry entry = new ContextMenuItemEntry
                    (
                        string.Format(ResGumps.ShowHide0, markerFile.Name),
                        () =>
                        {
                            markerFile.Hidden = !markerFile.Hidden;

                            if (!markerFile.Hidden)
                            {
                                string hiddenFile = _hiddenMarkerFiles.SingleOrDefault(x => x.Equals(markerFile.Name));

                                if (!string.IsNullOrEmpty(hiddenFile))
                                {
                                    _hiddenMarkerFiles.Remove(hiddenFile);
                                }
                            }
                            else
                            {
                                _hiddenMarkerFiles.Add(markerFile.Name);
                            }
                        },
                        true,
                        !markerFile.Hidden
                    );

                    _options[$"show_marker_{markerFile.Name}"] = entry;
                    markersEntry.Add(entry);
                }
            }
            else
            {
                markersEntry.Add(new ContextMenuItemEntry(ResGumps.NoMapFiles));
            }


            ContextMenu.Add(markersEntry);

            ContextMenuItemEntry namesHpBarEntry = new ContextMenuItemEntry(ResGumps.NamesHealthbars);
            namesHpBarEntry.Add(_options["show_your_name"]);
            namesHpBarEntry.Add(_options["show_your_healthbar"]);
            namesHpBarEntry.Add(_options["show_party_name"]);
            namesHpBarEntry.Add(_options["show_party_healthbar"]);

            ContextMenu.Add(namesHpBarEntry);

            ContextMenu.Add("", null);
            ContextMenu.Add(_options["goto_location"]);
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


        #region Update

        public override void Update(double totalTime, double frameTime)
        {
            base.Update(totalTime, frameTime);

            if (_mapIndex != World.MapIndex)
            {
                Load();
            }

            World.WMapManager.RequestServerPartyGuildInfo();
        }

        #endregion


        private (int, int) RotatePoint(int x, int y, float zoom, int dist, float angle = 45f)
        {
            x = (int) (x * zoom);
            y = (int) (y * zoom);

            if (angle == 0.0f)
            {
                return (x, y);
            }

            return ((int) Math.Round(Math.Cos(dist * Math.PI / 4.0) * x - Math.Sin(dist * Math.PI / 4.0) * y), (int) Math.Round(Math.Sin(dist * Math.PI / 4.0) * x + Math.Cos(dist * Math.PI / 4.0) * y));
        }

        private void AdjustPosition
        (
            int x,
            int y,
            int centerX,
            int centerY,
            out int newX,
            out int newY
        )
        {
            int offset = GetOffset(x, y, centerX, centerY);
            int currX = x;
            int currY = y;

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
            {
                return 1;
            }

            if (y < -centerY)
            {
                return 2;
            }

            if (x > centerX)
            {
                return offset + 4;
            }

            if (x >= -centerX)
            {
                return offset;
            }

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
            {
                return Color.Red;
            }

            if (name.Equals("green", StringComparison.OrdinalIgnoreCase))
            {
                return Color.Green;
            }

            if (name.Equals("blue", StringComparison.OrdinalIgnoreCase))
            {
                return Color.Blue;
            }

            if (name.Equals("purple", StringComparison.OrdinalIgnoreCase))
            {
                return Color.Purple;
            }

            if (name.Equals("black", StringComparison.OrdinalIgnoreCase))
            {
                return Color.Black;
            }

            if (name.Equals("yellow", StringComparison.OrdinalIgnoreCase))
            {
                return Color.Yellow;
            }

            if (name.Equals("white", StringComparison.OrdinalIgnoreCase))
            {
                return Color.White;
            }

            if (name.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                return Color.Transparent;
            }

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
            _options.TryGetValue(key, out ContextMenuItemEntry v);

            return v != null && v.IsSelected;
        }

        public void SetOptionValue(string key, bool v)
        {
            if (_options.TryGetValue(key, out ContextMenuItemEntry entry) && entry != null)
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

        private class CurLoader
        {
            public static unsafe Texture2D CreateTextureFromICO_Cur(Stream stream)
            {
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);

                DataReader reader = new DataReader();
                reader.SetData(buffer, stream.Length);

                bool was_error;
                long fp_offset;
                int bmp_pitch;
                int i, pad;
                SDL.SDL_Surface* surface;
                uint r_mask, g_mask, b_mask;
                byte* bits;
                int expand_bmp;
                int max_col = 0;
                uint ico_of_s = 0;
                uint* palette = stackalloc uint[256];

                ushort bf_reserved, bf_type, bf_count;
                uint bi_size, bi_width, bi_height;
                ushort bi_planes, bi_bit_count;

                uint bi_compression, bi_size_image, bi_x_perls_per_meter, bi_y_perls_per_meter, bi_clr_used, bi_clr_important;

                bf_reserved = reader.ReadUShort();
                bf_type = reader.ReadUShort();
                bf_count = reader.ReadUShort();

                for (i = 0; i < bf_count; i++)
                {
                    int b_width = reader.ReadByte();
                    int b_height = reader.ReadByte();
                    int b_color_count = reader.ReadByte();
                    byte b_reserver = reader.ReadByte();
                    ushort w_planes = reader.ReadUShort();
                    ushort w_bit_count = reader.ReadUShort();
                    uint dw_bytes_in_res = reader.ReadUInt();
                    uint dw_image_offse = reader.ReadUInt();

                    if (b_width == 0)
                    {
                        b_width = 256;
                    }

                    if (b_height == 0)
                    {
                        b_height = 256;
                    }

                    if (b_color_count == 0)
                    {
                        b_color_count = 256;
                    }

                    if (b_color_count > max_col)
                    {
                        max_col = b_color_count;
                        ico_of_s = dw_image_offse;
                    }
                }

                reader.Seek(ico_of_s);

                bi_size = reader.ReadUInt();

                if (bi_size == 40)
                {
                    bi_width = reader.ReadUInt();
                    bi_height = reader.ReadUInt();
                    bi_planes = reader.ReadUShort();
                    bi_bit_count = reader.ReadUShort();
                    bi_compression = reader.ReadUInt();
                    bi_size_image = reader.ReadUInt();
                    bi_x_perls_per_meter = reader.ReadUInt();
                    bi_y_perls_per_meter = reader.ReadUInt();
                    bi_clr_used = reader.ReadUInt();
                    bi_clr_important = reader.ReadUInt();
                }
                else
                {
                    return null;
                }

                const int BI_RGB = 0;
                const int BI_RLE = 1;
                const int BI_RLE4 = 2;
                const int BI_BITFIELDS = 3;

                switch (bi_compression)
                {
                    case BI_RGB:

                        switch (bi_bit_count)
                        {
                            case 1:
                            case 4:
                                expand_bmp = bi_bit_count;
                                bi_bit_count = 8;

                                break;

                            case 8:
                                expand_bmp = 8;

                                break;

                            case 32:
                                r_mask = 0x00FF0000;
                                g_mask = 0x0000FF00;
                                b_mask = 0x000000FF;
                                expand_bmp = 0;

                                break;

                            default: return null;
                        }

                        break;

                    default: return null;
                }


                bi_height >>= 1;

                surface = (SDL.SDL_Surface*) SDL.SDL_CreateRGBSurface
                (
                    0,
                    (int) bi_width,
                    (int) bi_height,
                    32,
                    0x00FF0000,
                    0x0000FF00,
                    0x000000FF,
                    0xFF000000
                );

                if (bi_bit_count <= 8)
                {
                    if (bi_clr_used == 0)
                    {
                        bi_clr_used = (uint) (1 << bi_bit_count);
                    }

                    for (i = 0; i < bi_clr_used; i++)
                    {
                        palette[i] = reader.ReadUInt();
                    }
                }

                bits = (byte*) (surface->pixels + surface->h * surface->pitch);

                switch (expand_bmp)
                {
                    case 1:
                        bmp_pitch = (int) (bi_width + 7) >> 3;
                        pad = bmp_pitch % 4 != 0 ? 4 - bmp_pitch % 4 : 0;

                        break;

                    case 4:
                        bmp_pitch = (int) (bi_width + 1) >> 1;
                        pad = bmp_pitch % 4 != 0 ? 4 - bmp_pitch % 4 : 0;

                        break;

                    case 8:
                        bmp_pitch = (int) bi_width;
                        pad = bmp_pitch % 4 != 0 ? 4 - bmp_pitch % 4 : 0;

                        break;

                    default:
                        bmp_pitch = (int) bi_width * 4;
                        pad = 0;

                        break;
                }


                while (bits > (byte*) surface->pixels)
                {
                    bits -= surface->pitch;

                    switch (expand_bmp)
                    {
                        case 1:
                        case 4:
                        case 8:
                        {
                            byte pixel = 0;
                            int shift = 8 - expand_bmp;

                            for (i = 0; i < surface->w; i++)
                            {
                                if (i % (8 / expand_bmp) == 0)
                                {
                                    pixel = reader.ReadByte();
                                }

                                *((uint*) bits + i) = palette[pixel >> shift];
                                pixel <<= expand_bmp;
                            }
                        }

                            break;

                        default:

                            for (int k = 0; k < surface->pitch; k++)
                            {
                                bits[k] = reader.ReadByte();
                            }

                            break;
                    }

                    if (pad != 0)
                    {
                        for (i = 0; i < pad; i++)
                        {
                            reader.ReadByte();
                        }
                    }
                }


                bits = (byte*) (surface->pixels + surface->h * surface->pitch);
                expand_bmp = 1;
                bmp_pitch = (int) (bi_width + 7) >> 3;
                pad = bmp_pitch % 4 != 0 ? 4 - bmp_pitch % 4 : 0;

                while (bits > (byte*) surface->pixels)
                {
                    byte pixel = 0;
                    int shift = 8 - expand_bmp;

                    bits -= surface->pitch;

                    for (i = 0; i < surface->w; i++)
                    {
                        if (i % (8 / expand_bmp) == 0)
                        {
                            pixel = reader.ReadByte();
                        }

                        *((uint*) bits + i) |= pixel >> shift != 0 ? 0 : 0xFF000000;

                        pixel <<= expand_bmp;
                    }

                    if (pad != 0)
                    {
                        for (i = 0; i < pad; i++)
                        {
                            reader.ReadByte();
                        }
                    }
                }

                surface = (SDL.SDL_Surface*) INTERNAL_convertSurfaceFormat((IntPtr) surface);

                int len = surface->w * surface->h * 4;
                byte* pixels = (byte*) surface->pixels;

                for (i = 0; i < len; i += 4, pixels += 4)
                {
                    if (pixels[3] == 0)
                    {
                        pixels[0] = 0;
                        pixels[1] = 0;
                        pixels[2] = 0;
                    }
                }

                Texture2D texture = new Texture2D(Client.Game.GraphicsDevice, surface->w, surface->h);
                texture.SetDataPointerEXT(0, new Rectangle(0, 0, surface->w, surface->h), surface->pixels, len);

                SDL.SDL_FreeSurface((IntPtr) surface);

                reader.ReleaseData();

                return texture;
            }

            private static unsafe IntPtr INTERNAL_convertSurfaceFormat(IntPtr surface)
            {
                IntPtr result = surface;
                SDL.SDL_Surface* surPtr = (SDL.SDL_Surface*) surface;
                SDL.SDL_PixelFormat* pixelFormatPtr = (SDL.SDL_PixelFormat*) surPtr->format;

                // SurfaceFormat.Color is SDL_PIXELFORMAT_ABGR8888
                if (pixelFormatPtr->format != SDL.SDL_PIXELFORMAT_ABGR8888)
                {
                    // Create a properly formatted copy, free the old surface
                    result = SDL.SDL_ConvertSurfaceFormat(surface, SDL.SDL_PIXELFORMAT_ABGR8888, 0);
                    SDL.SDL_FreeSurface(surface);
                }

                return result;
            }
        }


        #region Loading

        private unsafe Task Load()
        {
            _mapIndex = World.MapIndex;
            _mapTexture?.Dispose();
            _mapTexture = null;

            return Task.Run
            (
                () =>
                {
                    if (World.InGame)
                    {
                        try
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


                            for (bx = 0; bx < fixedWidth; ++bx)
                            {
                                mapX = bx << 3;

                                for (by = 0; by < fixedHeight; ++by)
                                {
                                    ref IndexMap indexMap = ref World.Map.GetIndex(bx, by);

                                    if (indexMap.MapAddress == 0)
                                    {
                                        continue;
                                    }

                                    MapBlock* mapBlock = (MapBlock*) indexMap.MapAddress;
                                    MapCells* cells = (MapCells*) &mapBlock->Cells;

                                    mapY = by << 3;

                                    for (y = 0; y < 8; ++y)
                                    {
                                        int block = (mapY + y + OFFSET_PIX_HALF) * (realWidth + OFFSET_PIX) + mapX + OFFSET_PIX_HALF;

                                        int pos = y << 3;

                                        for (x = 0; x < 8; ++x, ++pos, ++block)
                                        {
                                            ushort color = (ushort) (0x8000 | HuesLoader.Instance.GetRadarColorData(cells[pos].TileID & 0x3FFF));

                                            ref Color cc = ref buffer[block];
                                            cc.PackedValue = HuesHelper.Color16To32(color) | 0xFF_00_00_00;

                                            allZ[block] = cells[pos].Z;
                                        }
                                    }


                                    StaticsBlock* sb = (StaticsBlock*) indexMap.StaticAddress;

                                    if (sb != null)
                                    {
                                        int count = (int) indexMap.StaticCount;

                                        for (int c = 0; c < count; ++c, ++sb)
                                        {
                                            if (sb->Color != 0 && sb->Color != 0xFFFF && !GameObjectHelper.IsNoDrawable(sb->Color))
                                            {
                                                int block = (mapY + sb->Y + OFFSET_PIX_HALF) * (realWidth + OFFSET_PIX) + mapX + sb->X + OFFSET_PIX_HALF;

                                                if (sb->Z >= allZ[block])
                                                {
                                                    ushort color = (ushort) (0x8000 | (sb->Hue != 0 ? HuesLoader.Instance.GetColor16(16384, sb->Hue) : HuesLoader.Instance.GetRadarColorData(sb->Color + 0x4000)));

                                                    ref Color cc = ref buffer[block];
                                                    cc.PackedValue = HuesHelper.Color16To32(color) | 0xFF_00_00_00;

                                                    allZ[block] = sb->Z;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            int real_width_less_one = realWidth - 1;
                            int real_height_less_one = realHeight - 1;
                            const float MAG_0 = 80f / 100f;
                            const float MAG_1 = 100f / 80f;

                            int mapY_plus_one;
                            int r, g, b;

                            for (mapY = 1; mapY < real_height_less_one; ++mapY)
                            {
                                mapY_plus_one = mapY + 1;

                                for (mapX = 1; mapX < real_width_less_one; ++mapX)
                                {
                                    int blockCurrent = (mapY + OFFSET_PIX_HALF) * (realWidth + OFFSET_PIX) + mapX + OFFSET_PIX_HALF;

                                    int blockNext = (mapY_plus_one + OFFSET_PIX_HALF) * (realWidth + OFFSET_PIX) + (mapX - 1) + OFFSET_PIX_HALF;

                                    ref sbyte z0 = ref allZ[blockCurrent];
                                    ref sbyte z1 = ref allZ[blockNext];

                                    if (z0 == z1)
                                    {
                                        continue;
                                    }

                                    ref Color cc = ref buffer[blockCurrent];

                                    if (cc.R != 0 || cc.G != 0 || cc.B != 0)
                                    {
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
                                        {
                                            r = 255;
                                        }

                                        if (g > 255)
                                        {
                                            g = 255;
                                        }

                                        if (b > 255)
                                        {
                                            b = 255;
                                        }

                                        cc.R = (byte) r;
                                        cc.G = (byte) g;
                                        cc.B = (byte) b;
                                    }
                                }
                            }

                            if (OFFSET_PIX > 0)
                            {
                                realWidth += OFFSET_PIX;
                                realHeight += OFFSET_PIX;
                            }

                            _mapTexture = new UOTexture(realWidth, realHeight);
                            _mapTexture.SetData(buffer);
                        }
                        catch (Exception ex)
                        {
                        }


                        GameActions.Print(ResGumps.WorldMapLoaded, 0x48);
                    }
                }
            );
        }


        private void LoadMarkers()
        {
            //return Task.Run(() =>
            {
                if (World.InGame)
                {
                    _mapMarkersLoaded = false;

                    GameActions.Print(ResGumps.LoadingWorldMapMarkers, 0x2A);

                    foreach (Texture2D t in _markerIcons.Values)
                    {
                        if (t != null && !t.IsDisposed)
                        {
                            t.Dispose();
                        }
                    }

                    _markerIcons.Clear();

                    if (!Directory.Exists(_mapIconsPath))
                    {
                        Directory.CreateDirectory(_mapIconsPath);
                    }

                    foreach (string icon in Directory.GetFiles(_mapIconsPath, "*.cur").Union(Directory.GetFiles(_mapIconsPath, "*.ico")))
                    {
                        FileStream fs = new FileStream(icon, FileMode.Open, FileAccess.Read);
                        MemoryStream ms = new MemoryStream();
                        fs.CopyTo(ms);
                        ms.Seek(0, SeekOrigin.Begin);

                        try
                        {
                            Texture2D texture = CurLoader.CreateTextureFromICO_Cur(ms);

                            _markerIcons.Add(Path.GetFileNameWithoutExtension(icon).ToLower(), texture);
                        }
                        catch (Exception ee)
                        {
                            Log.Error($"{ee}");
                        }
                        finally
                        {
                            ms.Dispose();
                            fs.Dispose();
                        }
                    }

                    foreach (string icon in Directory.GetFiles(_mapIconsPath, "*.png").Union(Directory.GetFiles(_mapIconsPath, "*.jpg")))
                    {
                        FileStream fs = new FileStream(icon, FileMode.Open, FileAccess.Read);
                        MemoryStream ms = new MemoryStream();
                        fs.CopyTo(ms);
                        ms.Seek(0, SeekOrigin.Begin);

                        try
                        {
                            Texture2D texture = Texture2D.FromStream(Client.Game.GraphicsDevice, ms);

                            _markerIcons.Add(Path.GetFileNameWithoutExtension(icon).ToLower(), texture);
                        }
                        catch (Exception ee)
                        {
                            Log.Error($"{ee}");
                        }
                        finally
                        {
                            ms.Dispose();
                            fs.Dispose();
                        }
                    }

                    string[] mapFiles = Directory.GetFiles(_mapFilesPath, "*.map").Union(Directory.GetFiles(_mapFilesPath, "*.csv")).Union(Directory.GetFiles(_mapFilesPath, "*.xml")).ToArray();

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

                            string hiddenFile = _hiddenMarkerFiles.FirstOrDefault(x => x.Contains(markerFile.Name));

                            if (!string.IsNullOrEmpty(hiddenFile))
                            {
                                markerFile.Hidden = true;
                            }

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
                                        {
                                            continue;
                                        }

                                        // Check for UOAM file
                                        if (line.Substring(0, 1).Equals("+") || line.Substring(0, 1).Equals("-"))
                                        {
                                            string icon = line.Substring(1, line.IndexOf(':') - 1);

                                            line = line.Substring(line.IndexOf(':') + 2);

                                            string[] splits = line.Split(' ');

                                            if (splits.Length <= 1)
                                            {
                                                continue;
                                            }

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
                                            {
                                                marker.MarkerIcon = value;
                                            }

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
                                        {
                                            return;
                                        }

                                        string[] splits = line.Split(',');

                                        if (splits.Length <= 1)
                                        {
                                            continue;
                                        }

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
                                        {
                                            marker.MarkerIcon = value;
                                        }

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

                    GameActions.Print(string.Format(ResGumps.WorldMapMarkersLoaded0, count), 0x2A);
                }
            }

            //);
        }

        #endregion


        #region Draw

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed || !World.InGame)
            {
                return false;
            }

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


            batcher.Draw2D
            (
                SolidColorTextureCache.GetTexture(Color.Black),
                gX,
                gY,
                gWidth,
                gHeight,
                ref HueVector
            );

            if (_mapTexture != null)
            {
                Rectangle rect = ScissorStack.CalculateScissors
                (
                    Matrix.Identity,
                    gX,
                    gY,
                    gWidth,
                    gHeight
                );

                if (ScissorStack.PushScissors(batcher.GraphicsDevice, rect))
                {
                    batcher.EnableScissorTest(true);

                    int offset = size >> 1;

                    batcher.Draw2D
                    (
                        _mapTexture,
                        gX - offset + halfWidth,
                        gY - offset + halfHeight,
                        size,
                        size,
                        sx - size_zoom_half,
                        sy - size_zoom_half,
                        size_zoom,
                        size_zoom,
                        ref HueVector,
                        _flipMap ? 45 : 0
                    );

                    DrawAll
                    (
                        batcher,
                        gX,
                        gY,
                        halfWidth,
                        halfHeight
                    );

                    batcher.EnableScissorTest(false);
                    ScissorStack.PopScissors(batcher.GraphicsDevice);
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
            if (_showMarkers && _mapMarkersLoaded)
            {
                WMapMarker lastMarker = null;

                foreach (WMapMarkerFile file in _markerFiles)
                {
                    if (file.Hidden)
                    {
                        continue;
                    }

                    foreach (WMapMarker marker in file.Markers)
                    {
                        if (DrawMarker
                        (
                            batcher,
                            marker,
                            gX,
                            gY,
                            halfWidth,
                            halfHeight,
                            Zoom
                        ))
                        {
                            lastMarker = marker;
                        }
                    }
                }

                if (lastMarker != null)
                {
                    DrawMarkerString(batcher, lastMarker, gX, gY, halfWidth, halfHeight);
                }
            }

            if (_gotoMarker != null)
            {
                DrawMarker
                (
                    batcher,
                    _gotoMarker,
                    gX,
                    gY,
                    halfWidth,
                    halfHeight,
                    Zoom
                );
            }

            if (_showMultis)
            {
                foreach (House house in World.HouseManager.Houses)
                {
                    Item item = World.Items.Get(house.Serial);

                    if (item != null)
                    {
                        DrawMulti
                        (
                            batcher,
                            house,
                            item.X,
                            item.Y,
                            gX,
                            gY,
                            halfWidth,
                            halfHeight,
                            Zoom
                        );
                    }
                }
            }

            if (_showMobiles)
            {
                foreach (Mobile mob in World.Mobiles)
                {
                    if (mob == World.Player)
                    {
                        continue;
                    }

                    if (mob.NotorietyFlag != NotorietyFlag.Ally)
                    {
                        DrawMobile
                        (
                            batcher,
                            mob,
                            gX,
                            gY,
                            halfWidth,
                            halfHeight,
                            Zoom,
                            Color.Red
                        );
                    }
                    else
                    {
                        if (mob != null && mob.Distance <= World.ClientViewRange)
                        {
                            WMapEntity wme = World.WMapManager.GetEntity(mob);

                            if (wme != null)
                            {
                                if (string.IsNullOrEmpty(wme.Name) && !string.IsNullOrEmpty(mob.Name))
                                {
                                    wme.Name = mob.Name;
                                }
                            }
                            else
                            {
                                DrawMobile
                                (
                                    batcher,
                                    mob,
                                    gX,
                                    gY,
                                    halfWidth,
                                    halfHeight,
                                    Zoom,
                                    Color.Lime,
                                    true,
                                    true,
                                    _showGroupBar
                                );
                            }
                        }
                        else
                        {
                            WMapEntity wme = World.WMapManager.GetEntity(mob.Serial);

                            if (wme != null && wme.IsGuild)
                            {
                                DrawWMEntity
                                (
                                    batcher,
                                    wme,
                                    gX,
                                    gY,
                                    halfWidth,
                                    halfHeight,
                                    Zoom
                                );
                            }
                        }
                    }
                }
            }

            foreach (WMapEntity wme in World.WMapManager.Entities.Values)
            {
                if (wme.IsGuild && !World.Party.Contains(wme.Serial))
                {
                    DrawWMEntity
                    (
                        batcher,
                        wme,
                        gX,
                        gY,
                        halfWidth,
                        halfHeight,
                        Zoom
                    );
                }
            }

            if (_showPartyMembers)
            {
                for (int i = 0; i < 10; i++)
                {
                    PartyMember partyMember = World.Party.Members[i];

                    if (partyMember != null && SerialHelper.IsValid(partyMember.Serial))
                    {
                        Mobile mob = World.Mobiles.Get(partyMember.Serial);

                        if (mob != null && mob.Distance <= World.ClientViewRange)
                        {
                            WMapEntity wme = World.WMapManager.GetEntity(mob);

                            if (wme != null)
                            {
                                if (string.IsNullOrEmpty(wme.Name) && !string.IsNullOrEmpty(partyMember.Name))
                                {
                                    wme.Name = partyMember.Name;
                                }
                            }

                            DrawMobile
                            (
                                batcher,
                                mob,
                                gX,
                                gY,
                                halfWidth,
                                halfHeight,
                                Zoom,
                                Color.Yellow,
                                _showGroupName,
                                true,
                                _showGroupBar
                            );
                        }
                        else
                        {
                            WMapEntity wme = World.WMapManager.GetEntity(partyMember.Serial);

                            if (wme != null && !wme.IsGuild)
                            {
                                DrawWMEntity
                                (
                                    batcher,
                                    wme,
                                    gX,
                                    gY,
                                    halfWidth,
                                    halfHeight,
                                    Zoom
                                );
                            }
                        }
                    }
                }
            }

            DrawMobile
            (
                batcher,
                World.Player,
                gX,
                gY,
                halfWidth,
                halfHeight,
                Zoom,
                Color.White,
                _showPlayerName,
                false,
                _showPlayerBar
            );


            if (_showCoordinates)
            {
                ResetHueVector();

                HueVector.Y = 1;

                batcher.DrawString
                (
                    Fonts.Bold,
                    $"{World.Player.X}, {World.Player.Y} ({World.Player.Z}) [{_zoomIndex}]",
                    gX + 6,
                    gY + 6,
                    ref HueVector
                );

                ResetHueVector();

                batcher.DrawString
                (
                    Fonts.Bold,
                    $"{World.Player.X}, {World.Player.Y} ({World.Player.Z}) [{_zoomIndex}]",
                    gX + 5,
                    gY + 5,
                    ref HueVector
                );
            }
        }

        private void DrawMobile
        (
            UltimaBatcher2D batcher,
            Mobile mobile,
            int x,
            int y,
            int width,
            int height,
            float zoom,
            Color color,
            bool drawName = false,
            bool isparty = false,
            bool drawHpBar = false
        )
        {
            ResetHueVector();

            int sx = mobile.X - _center.X;
            int sy = mobile.Y - _center.Y;

            (int rotX, int rotY) = RotatePoint
            (
                sx,
                sy,
                zoom,
                1,
                _flipMap ? 45f : 0f
            );

            AdjustPosition
            (
                rotX,
                rotY,
                width - 4,
                height - 4,
                out rotX,
                out rotY
            );

            rotX += x + width;
            rotY += y + height;

            const int DOT_SIZE = 4;
            const int DOT_SIZE_HALF = DOT_SIZE >> 1;

            if (rotX < x)
            {
                rotX = x;
            }

            if (rotX > x + Width - 8 - DOT_SIZE)
            {
                rotX = x + Width - 8 - DOT_SIZE;
            }

            if (rotY < y)
            {
                rotY = y;
            }

            if (rotY > y + Height - 8 - DOT_SIZE)
            {
                rotY = y + Height - 8 - DOT_SIZE;
            }

            batcher.Draw2D
            (
                SolidColorTextureCache.GetTexture(color),
                rotX - DOT_SIZE_HALF,
                rotY - DOT_SIZE_HALF,
                DOT_SIZE,
                DOT_SIZE,
                ref HueVector
            );

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
                    rotY = y + Height - (int) size.Y;
                }
                else if (rotY - size.Y < y)
                {
                    rotY = y + (int) size.Y;
                }

                int xx = (int) (rotX - size.X / 2);
                int yy = (int) (rotY - size.Y);

                HueVector.X = 0;
                HueVector.Y = 1;

                batcher.DrawString
                (
                    Fonts.Regular,
                    mobile.Name,
                    xx + 1,
                    yy + 1,
                    ref HueVector
                );

                ResetHueVector();
                HueVector.X = isparty ? 0x0034 : Notoriety.GetHue(mobile.NotorietyFlag);
                HueVector.Y = 1;

                batcher.DrawString
                (
                    Fonts.Regular,
                    mobile.Name,
                    xx,
                    yy,
                    ref HueVector
                );
            }

            if (drawHpBar)
            {
                int ww = mobile.HitsMax;

                if (ww > 0)
                {
                    ww = mobile.Hits * 100 / ww;

                    if (ww > 100)
                    {
                        ww = 100;
                    }
                    else if (ww < 1)
                    {
                        ww = 0;
                    }
                }

                rotY += DOT_SIZE + 1;

                DrawHpBar(batcher, rotX, rotY, ww);
            }
        }

        private bool DrawMarker
        (
            UltimaBatcher2D batcher,
            WMapMarker marker,
            int x,
            int y,
            int width,
            int height,
            float zoom
        )
        {
            if (marker.MapId != World.MapIndex)
            {
                return false;
            }

            if (_zoomIndex < marker.ZoomIndex && marker.Color == Color.Transparent)
            {
                return false;
            }

            ResetHueVector();

            int sx = marker.X - _center.X;
            int sy = marker.Y - _center.Y;

            (int rotX, int rotY) = RotatePoint
            (
                sx,
                sy,
                zoom,
                1,
                _flipMap ? 45f : 0f
            );

            rotX += x + width;
            rotY += y + height;

            const int DOT_SIZE = 4;
            const int DOT_SIZE_HALF = DOT_SIZE >> 1;

            if (rotX < x || rotX > x + Width - 8 - DOT_SIZE || rotY < y || rotY > y + Height - 8 - DOT_SIZE)
            {
                return false;
            }

            bool showMarkerName = _showMarkerNames && !string.IsNullOrEmpty(marker.Name) && _zoomIndex > 5;
            bool drawSingleName = false;

            if (_zoomIndex < marker.ZoomIndex || !_showMarkerIcons || marker.MarkerIcon == null)
            {
                batcher.Draw2D
                (
                    SolidColorTextureCache.GetTexture(marker.Color),
                    rotX - DOT_SIZE_HALF,
                    rotY - DOT_SIZE_HALF,
                    DOT_SIZE,
                    DOT_SIZE,
                    ref HueVector
                );

                if (Mouse.Position.X >= rotX - DOT_SIZE && Mouse.Position.X <= rotX + DOT_SIZE_HALF &&
                    Mouse.Position.Y >= rotY - DOT_SIZE && Mouse.Position.Y <= rotY + DOT_SIZE_HALF)
                {
                    drawSingleName = true;
                }
            }
            else
            {
                batcher.Draw2D(marker.MarkerIcon, rotX - (marker.MarkerIcon.Width >> 1), rotY - (marker.MarkerIcon.Height >> 1), ref HueVector);
               
                if (!showMarkerName)
                {
                    if (Mouse.Position.X >= rotX - (marker.MarkerIcon.Width >> 1) &&
                        Mouse.Position.X <= rotX + (marker.MarkerIcon.Width >> 1) &&
                        Mouse.Position.Y >= rotY - (marker.MarkerIcon.Height >> 1) &&
                        Mouse.Position.Y <= rotY + (marker.MarkerIcon.Height >> 1))
                    {
                        drawSingleName = true;
                    }
                }
            }

            if (showMarkerName)
            {
                DrawMarkerString(batcher, marker, x, y, width, height);

                drawSingleName = false;
            }

            return drawSingleName;
        }

        private void DrawMarkerString(UltimaBatcher2D batcher, WMapMarker marker, int x, int y, int width, int height)
        {
            int sx = marker.X - _center.X;
            int sy = marker.Y - _center.Y;

            (int rotX, int rotY) = RotatePoint
            (
                sx,
                sy,
                Zoom,
                1,
                _flipMap ? 45f : 0f
            );

            rotX += x + width;
            rotY += y + height;

            Vector2 size = _markerFont.MeasureString(marker.Name);

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
                rotY = y + Height - (int)size.Y;
            }
            else if (rotY - size.Y < y)
            {
                rotY = y + (int)size.Y;
            }

            int xx = (int)(rotX - size.X / 2);
            int yy = (int)(rotY - size.Y - 5);

            ResetHueVector();

            HueVector.X = 0;
            HueVector.Y = 1;
            HueVector.Z = 0.5f;

            batcher.Draw2D
            (
                SolidColorTextureCache.GetTexture(Color.Black),
                xx - 2,
                yy - 2,
                size.X + 4,
                size.Y + 4,
                ref HueVector
            );

            ResetHueVector();

            HueVector.X = 0;
            HueVector.Y = 1;

            batcher.DrawString
            (
                _markerFont,
                marker.Name,
                xx + 1,
                yy + 1,
                ref HueVector
            );

            ResetHueVector();

            batcher.DrawString
            (
                _markerFont,
                marker.Name,
                xx,
                yy,
                ref HueVector
            );
        }

        private void DrawMulti
        (
            UltimaBatcher2D batcher,
            House house,
            int multiX,
            int multiY,
            int x,
            int y,
            int width,
            int height,
            float zoom
        )
        {
            int sx = multiX - _center.X;
            int sy = multiY - _center.Y;
            int sW = Math.Abs(house.Bounds.Width - house.Bounds.X);
            int sH = Math.Abs(house.Bounds.Height - house.Bounds.Y);

            (int rotX, int rotY) = RotatePoint
            (
                sx,
                sy,
                zoom,
                1,
                _flipMap ? 45f : 0f
            );

          
            rotX += x + width;
            rotY += y + height;

            const int DOT_SIZE = 4;
            const int DOT_SIZE_HALF = DOT_SIZE >> 1;

            if (rotX < x || rotX > x + Width - 8 - DOT_SIZE || rotY < y || rotY > y + Height - 8 - DOT_SIZE)
            {
                return;
            }

            ResetHueVector();

            Texture2D texture = SolidColorTextureCache.GetTexture(Color.DarkGray);

            batcher.Draw2D
            (
                texture,
                rotX - sW / 2f * zoom,
                rotY - sH / 2f * zoom,
                sW * zoom,
                sH * zoom,
                0,
                0,
                sW,
                sH,
                ref HueVector,
                _flipMap ? 45f : 0f
            );
        }

        private void DrawWMEntity
        (
            UltimaBatcher2D batcher,
            WMapEntity entity,
            int x,
            int y,
            int width,
            int height,
            float zoom
        )
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

            (int rotX, int rotY) = RotatePoint
            (
                sx,
                sy,
                zoom,
                1,
                _flipMap ? 45f : 0f
            );

            AdjustPosition
            (
                rotX,
                rotY,
                width - 4,
                height - 4,
                out rotX,
                out rotY
            );

            rotX += x + width;
            rotY += y + height;

            const int DOT_SIZE = 4;
            const int DOT_SIZE_HALF = DOT_SIZE >> 1;

            if (rotX < x)
            {
                rotX = x;
            }

            if (rotX > x + Width - 8 - DOT_SIZE)
            {
                rotX = x + Width - 8 - DOT_SIZE;
            }

            if (rotY < y)
            {
                rotY = y;
            }

            if (rotY > y + Height - 8 - DOT_SIZE)
            {
                rotY = y + Height - 8 - DOT_SIZE;
            }

            batcher.Draw2D
            (
                SolidColorTextureCache.GetTexture(color),
                rotX - DOT_SIZE_HALF,
                rotY - DOT_SIZE_HALF,
                DOT_SIZE,
                DOT_SIZE,
                ref HueVector
            );

            if (_showGroupName)
            {
                string name = entity.Name ?? ResGumps.OutOfRange;
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
                    rotY = y + Height - (int) size.Y;
                }
                else if (rotY - size.Y < y)
                {
                    rotY = y + (int) size.Y;
                }

                int xx = (int) (rotX - size.X / 2);
                int yy = (int) (rotY - size.Y);

                HueVector.X = 0;
                HueVector.Y = 1;

                batcher.DrawString
                (
                    Fonts.Regular,
                    name,
                    xx + 1,
                    yy + 1,
                    ref HueVector
                );

                ResetHueVector();
                HueVector.X = uohue;
                HueVector.Y = 1;

                batcher.DrawString
                (
                    Fonts.Regular,
                    name,
                    xx,
                    yy,
                    ref HueVector
                );
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


            batcher.Draw2D
            (
                SolidColorTextureCache.GetTexture(Color.Black),
                x - BAR_MAX_WIDTH_HALF - 1,
                y - BAR_MAX_HEIGHT_HALF - 1,
                BAR_MAX_WIDTH + 2,
                BAR_MAX_HEIGHT + 2,
                ref HueVector
            );

            batcher.Draw2D
            (
                SolidColorTextureCache.GetTexture(Color.Red),
                x - BAR_MAX_WIDTH_HALF,
                y - BAR_MAX_HEIGHT_HALF,
                BAR_MAX_WIDTH,
                BAR_MAX_HEIGHT,
                ref HueVector
            );

            int max = 100;
            int current = hp;

            if (max > 0)
            {
                max = current * 100 / max;

                if (max > 100)
                {
                    max = 100;
                }

                if (max > 1)
                {
                    max = BAR_MAX_WIDTH * max / 100;
                }
            }

            batcher.Draw2D
            (
                SolidColorTextureCache.GetTexture(Color.CornflowerBlue),
                x - BAR_MAX_WIDTH_HALF,
                y - BAR_MAX_HEIGHT_HALF,
                max,
                BAR_MAX_HEIGHT,
                ref HueVector
            );
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
            if (!ItemHold.Enabled)
            {
                if (button == MouseButtonType.Left && (Keyboard.Alt || _freeView) || button == MouseButtonType.Middle)
                {
                    if (x > 4 && x < Width - 8 && y > 4 && y < Height - 8)
                    {
                        if (button == MouseButtonType.Middle)
                        {
                            FreeView = true;
                        }

                        _lastScroll.X = x;
                        _lastScroll.Y = y;
                        _isScrolling = true;
                        CanMove = false;

                        UIManager.GameCursor.IsDraggingCursorForced = true;
                    }
                }
            }

            base.OnMouseDown(x, y, button);
        }

        protected override void OnMouseOver(int x, int y)
        {
            Point offset = Mouse.LButtonPressed ? Mouse.LDragOffset : Mouse.MButtonPressed ? Mouse.MDragOffset : Point.Zero;

            if (_isScrolling && offset != Point.Zero)
            {
                int scrollX = _lastScroll.X - x;
                int scrollY = _lastScroll.Y - y;

                (scrollX, scrollY) = RotatePoint
                (
                    scrollX,
                    scrollY,
                    1f,
                    -1,
                    _flipMap ? 45f : 0f
                );

                _center.X += (int) (scrollX / Zoom);
                _center.Y += (int) (scrollY / Zoom);

                if (_center.X < 0)
                {
                    _center.X = 0;
                }

                if (_center.Y < 0)
                {
                    _center.Y = 0;
                }

                if (_center.X > MapLoader.Instance.MapsDefaultSize[World.MapIndex, 0])
                {
                    _center.X = MapLoader.Instance.MapsDefaultSize[World.MapIndex, 0];
                }

                if (_center.Y > MapLoader.Instance.MapsDefaultSize[World.MapIndex, 1])
                {
                    _center.Y = MapLoader.Instance.MapsDefaultSize[World.MapIndex, 1];
                }

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
                {
                    _zoomIndex = _zooms.Length - 1;
                }
            }
            else
            {
                _zoomIndex--;

                if (_zoomIndex < 0)
                {
                    _zoomIndex = 0;
                }
            }


            base.OnMouseWheel(delta);
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left || _isScrolling || Keyboard.Alt)
            {
                return base.OnMouseDoubleClick(x, y, button);
            }

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

        #region Helpers

        /// <summary>
        /// Converts latitudes and longitudes to X and Y locations based on Lord British's throne is located at 1323.1624 or 0° 0'N 0° 0'E
        /// </summary>
        /// <param name="coords"></param>
        /// <param name="xAxis"></param>
        /// <param name="yAxis"></param>
        private static void ConvertCoords(string coords, ref int xAxis, ref int yAxis)
        {
            string[] coordsSplit = coords.Split(',');

            string yCoord = coordsSplit[0];
            string xCoord = coordsSplit[1];

            // Calc Y first
            string[] ySplit = yCoord.Split('°', 'o');
            double yDegree = Convert.ToDouble(ySplit[0]);
            double yMinute = Convert.ToDouble(ySplit[1].Substring(0, ySplit[1].IndexOf("'", StringComparison.Ordinal)));

            if (yCoord.Substring(yCoord.Length - 1).Equals("N"))
            {
                yAxis = (int) (1624 - (yMinute / 60) * (4096.0 / 360) - yDegree * (4096.0 / 360));
            }
            else
            {
                yAxis = (int) (1624 + (yMinute / 60) * (4096.0 / 360) + yDegree * (4096.0 / 360));
            }

            // Calc X next
            string[] xSplit = xCoord.Split('°', 'o');
            double xDegree = Convert.ToDouble(xSplit[0]);
            double xMinute = Convert.ToDouble(xSplit[1].Substring(0, xSplit[1].IndexOf("'", StringComparison.Ordinal)));

            if (xCoord.Substring(xCoord.Length - 1).Equals("W"))
            {
                xAxis = (int) (1323 - (xMinute / 60) * (5120.0 / 360) - xDegree * (5120.0 / 360));
            }
            else
            {
                xAxis = (int) (1323 + (xMinute / 60) * (5120.0 / 360) + xDegree * (5120.0 / 360));
            }

            // Normalize values outside of map range.
            if (xAxis < 0)
            {
                xAxis += 5120;
            }
            else if (xAxis > 5120)
            {
                xAxis -= 5120;
            }

            if (yAxis < 0)
            {
                yAxis += 4096;
            }
            else if (yAxis > 4096)
            {
                yAxis -= 4096;
            }
        }
    }

    #endregion
}