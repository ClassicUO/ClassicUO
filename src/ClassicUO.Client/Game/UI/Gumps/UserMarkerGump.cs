using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClassicUO.Utility;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Controls;
using static ClassicUO.Game.UI.Gumps.WorldMapGump;
using ClassicUO.Assets;
using ClassicUO.Resources;

namespace ClassicUO.Game.UI.Gumps
{
    internal sealed class UserMarkersGump : Gump
    {
        private readonly StbTextBox _textBoxX;
        private readonly StbTextBox _textBoxY;
        private readonly StbTextBox _markerName;

        private const ushort HUE_FONT = 0xFFFF;
        private const ushort LABEL_OFFSET = 40;
        private const ushort Y_OFFSET = 30;

        private readonly Combobox _colorsCombo;
        private readonly string[] _colors;

        private readonly Combobox _iconsCombo;
        private readonly string[] _icons;

        private readonly List<WMapMarker> _markers;
        private readonly int _markerIdx;

        private const int MAX_CORD_LEN = 10;
        private const int MAX_NAME_LEN = 25;

        private const int MAP_MIN_CORD = 0;
        private readonly int _mapMaxX;
        private readonly int _mapMaxY;

        private readonly string _userMarkersFilePath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client", $"{USER_MARKERS_FILE}.usr");

        public event EventHandler EditEnd;

        private enum ButtonsOption
        {
            ADD_BTN,
            EDIT_BTN,
            CANCEL_BTN,
        }

        internal UserMarkersGump(World world, int x, int y, List<WMapMarker> markers, string color = "none", string icon = "exit", bool isEdit = false, int markerIdx = -1) : base(world, 0, 0)
        {
            CanMove = true;

            _mapMaxX= MapLoader.Instance.MapsDefaultSize[world.MapIndex, 0];
            _mapMaxY = MapLoader.Instance.MapsDefaultSize[world.MapIndex, 1];

            _markers = markers;
            _markerIdx = markerIdx;

            _colors = new[] { "none", "red", "green", "blue", "purple", "black", "yellow", "white" };
            _icons = _markerIcons.Keys.ToArray();

            var markerName = _markerIdx < 0 ? ResGumps.MarkerDefName : _markers[_markerIdx].Name;

            int selectedIcon = Array.IndexOf(_icons, icon);
            if (selectedIcon < 0)
                selectedIcon = 0;

            int selectedColor = isEdit ? Array.IndexOf(_colors, color) : (_icons.Length == 0 ? 1 : 0);
            if (selectedColor < 0)
                selectedColor = 0;

            AlphaBlendControl markersGumpBackground = new AlphaBlendControl
            {
                Width = 320,
                Height = 220,
                X = Client.Game.Scene.Camera.Bounds.Width / 2 - 125,
                Y = 150,
                Alpha = 0.7f,
                CanMove = true,
                CanCloseWithRightClick = true,
                AcceptMouseInput = true
            };

            Add(markersGumpBackground);

            if (!isEdit)
                Add(new Label(ResGumps.AddMarker, true, HUE_FONT, 0, 255, FontStyle.BlackBorder)
                {
                    X = markersGumpBackground.X + 100,
                    Y = markersGumpBackground.Y + 3,
                });
            else
                Add(new Label(ResGumps.EditMarker, true, HUE_FONT, 0, 255, FontStyle.BlackBorder)
                {
                    X = markersGumpBackground.X + 100,
                    Y = markersGumpBackground.Y + 3,
                });

            // X Field
            var fx = markersGumpBackground.X + 5;
            var fy = markersGumpBackground.Y + 25;
            Add(new ResizePic(0x0BB8)
            {
                X = fx + LABEL_OFFSET,
                Y = fy,
                Width = 90,
                Height = 25
            });

            _textBoxX = new StbTextBox(
                0xFF,
                MAX_CORD_LEN,
                90,
                true,
                FontStyle.BlackBorder | FontStyle.Fixed
            )
            {
                X = fx + LABEL_OFFSET,
                Y = fy,
                Width = 90,
                Height = 25,
                Text = x.ToString()
            };
            Add(_textBoxX);
            Add(new Label(ResGumps.MarkerX, true, HUE_FONT, 0, 255, FontStyle.BlackBorder) { X = fx, Y = fy });

            // Y Field
            fy += Y_OFFSET;
            Add(new ResizePic(0x0BB8)
            {
                X = fx + LABEL_OFFSET,
                Y = fy,
                Width = 90,
                Height = 25
            });

            _textBoxY = new StbTextBox(
                0xFF,
                MAX_CORD_LEN,
                90,
                true,
                FontStyle.BlackBorder | FontStyle.Fixed
            )
            {
                X = fx + LABEL_OFFSET,
                Y = fy,
                Width = 90,
                Height = 25,
                Text = y.ToString()
            };
            Add(_textBoxY);
            Add(new Label(ResGumps.MarkerY, true, HUE_FONT, 0, 255, FontStyle.BlackBorder) { X = fx, Y = fy });

            // Marker Name field
            fy += Y_OFFSET;
            Add(new ResizePic(0x0BB8)
            {
                X = fx + LABEL_OFFSET,
                Y = fy,
                Width = 250,
                Height = 25
            });

            _markerName = new StbTextBox(
                0xFF,
                MAX_NAME_LEN,
                250,
                true,
                FontStyle.BlackBorder | FontStyle.Fixed
            )
            {
                X = fx + LABEL_OFFSET,
                Y = fy,
                Width = 250,
                Height = 25,
                Text = markerName
            };
            Add(_markerName);
            Add(new Label(ResGumps.MarkerName, true, HUE_FONT, 0, 255, FontStyle.BlackBorder) { X = fx, Y = fy });

            // Color Combobox
            fy += Y_OFFSET;
            _colorsCombo = new Combobox
                (
                    fx + LABEL_OFFSET,
                    fy,
                    250,
                    _colors,
                    selectedColor
                );
            Add(_colorsCombo);
            Add(new Label(ResGumps.MarkerColor, true, HUE_FONT, 0, 255, FontStyle.BlackBorder) { X = fx, Y = fy });

            if (_icons.Length > 0)
            {
                // Icon combobox
                fy += Y_OFFSET;
                _iconsCombo = new Combobox
                    (
                        fx + LABEL_OFFSET,
                        fy,
                        250,
                        _icons,
                        selectedIcon
                    );
                Add(_iconsCombo);
                Add(new Label(ResGumps.MarkerIcon, true, HUE_FONT, 0, 255, FontStyle.BlackBorder) { X = fx, Y = fy });
            }

            // Buttons Add and Edit depend of state
            if (!isEdit)
            {
                Add
                (
                    new NiceButton
                        (
                            markersGumpBackground.X + 13,
                            markersGumpBackground.Y + markersGumpBackground.Height - 30,
                            60,
                            25,
                            ButtonAction.Activate,
                            ResGumps.CreateMarker
                        )
                    { ButtonParameter = (int)ButtonsOption.ADD_BTN, IsSelectable = false }
                );
            }
            else
            {
                Add
                (
                    new NiceButton
                        (
                            markersGumpBackground.X + 13,
                            markersGumpBackground.Y + markersGumpBackground.Height - 30,
                            60,
                            25,
                            ButtonAction.Activate,
                            ResGumps.Edit
                        )
                    { ButtonParameter = (int)ButtonsOption.EDIT_BTN, IsSelectable = false }
                );
            }

            Add
            (
                new NiceButton
                    (
                        markersGumpBackground.X + 78,
                        markersGumpBackground.Y + markersGumpBackground.Height - 30,
                        60,
                        25,
                        ButtonAction.Activate,
                        ResGumps.Cancel
                    )
                { ButtonParameter = (int)ButtonsOption.CANCEL_BTN, IsSelectable = false }
            );

            SetInScreen();
        }

        private void EditMarker()
        {
            var editedMarker = PrepareMarker();
            if (editedMarker == null)
                return;

            _markers[_markerIdx] = editedMarker;

            EditEnd.Raise(editedMarker);

            Dispose();
        }

        private void AddNewMarker()
        {
            if (!File.Exists(_userMarkersFilePath))
            {
                return;
            }

            var newMarker = PrepareMarker();
            if (newMarker == null)
            {
                return;
            }

            var newLine = $"{newMarker.X},{newMarker.Y},{newMarker.MapId},{newMarker.Name},{newMarker.MarkerIconName},{newMarker.ColorName},4\r";

            File.AppendAllText(_userMarkersFilePath, newLine);

            _markers.Add(newMarker);

            Dispose();
        }

        private WMapMarker PrepareMarker()
        {
            if (!int.TryParse(_textBoxX.Text, out var x))
                return null;
            if (!int.TryParse(_textBoxY.Text, out var y))
                return null;

            // Validate User Enter Data
            if (x > _mapMaxX || x < MAP_MIN_CORD)
            {
                return null;
            }

            if (y > _mapMaxY || y < MAP_MIN_CORD)
            {
                return null;
            }

            var markerName = _markerName.Text;
            if (string.IsNullOrEmpty(markerName))
                return null;

            if (markerName.Contains(","))
                markerName = markerName.Replace(",", "");

            var mapIdx = World.MapIndex;
            var color = _colors[_colorsCombo.SelectedIndex];
            var icon = _iconsCombo == null ? string.Empty : _icons[_iconsCombo.SelectedIndex];

            var marker = new WMapMarker
            {
                Name = markerName,
                X = x,
                Y = y,
                MapId = mapIdx,
                ColorName = color,
                Color = GetColor(color),
            };

            if (!_markerIcons.TryGetValue(icon, out var iconTexture))
            {
                return marker;
            }

            marker.MarkerIcon = iconTexture;
            marker.MarkerIconName = icon;

            return marker;
        }

        public override void OnButtonClick(int buttonId)
        {
            switch (buttonId)
            {
                case (int)ButtonsOption.ADD_BTN:
                    AddNewMarker();
                    break;
                case (int)ButtonsOption.EDIT_BTN:
                    EditMarker();
                    break;
                case (int)ButtonsOption.CANCEL_BTN:
                    Dispose();
                    break;
            }
        }
    }
}