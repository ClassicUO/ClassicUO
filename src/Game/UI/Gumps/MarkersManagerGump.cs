using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClassicUO.Resources;
using Microsoft.Xna.Framework.Graphics;
using static ClassicUO.Game.UI.Gumps.WorldMapGump;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    internal sealed class MarkersManagerGump : Gump
    {
        private const int WIDTH = 620;
        private const int HEIGHT = 500;
        private const ushort HUE_FONT = 0xFFFF;

        private bool _isMarkerListModified;

        private ScrollArea _scrollArea;
        private readonly SearchTextBoxControl _searchTextBox;

        private string _searchText = "";
        private int _categoryId = 0;

        private static List<WMapMarker> _markers = new List<WMapMarker>();

        private static readonly List<WMapMarkerFile> _markerFiles = WorldMapGump._markerFiles;

        private readonly string _userMarkersFilePath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client", $"{USER_MARKERS_FILE}.usr");

        private readonly int MARKERS_CATEGORY_GROUP_INDEX = 10;

        private enum ButtonsOption
        {
            SEARCH_BTN = 100,
            CLEAR_SEARCH_BTN
        }

        internal MarkersManagerGump() : base(0, 0)
        {
            X = 50;
            Y = 50;
            CanMove = true;
            AcceptMouseInput = true;

            var button_width = 50;
            if (_markerFiles.Count > 0)
            {
                _markers = _markerFiles[0].Markers;
                button_width = WIDTH / _markerFiles.Count;
            }

            Add
            (
                new AlphaBlendControl(0.05f)
                {
                    X = 1,
                    Y = 1,
                    Width = WIDTH,
                    Height = HEIGHT,
                    Hue = 999,
                    AcceptMouseInput = true,
                    CanCloseWithRightClick = true,
                    CanMove = true,
                }
            );

            #region Boarder
            Add
            (
                new Line
                (
                    0,
                    0,
                    WIDTH,
                    1,
                    Color.Gray.PackedValue
                )
            );

            Add
            (
                new Line
                (
                    0,
                    0,
                    1,
                    HEIGHT,
                    Color.Gray.PackedValue
                )
            );

            Add
            (
                new Line
                (
                    0,
                    HEIGHT,
                    WIDTH,
                    1,
                    Color.Gray.PackedValue
                )
            );

            Add
            (
                new Line
                (
                    WIDTH,
                    0,
                    1,
                    HEIGHT,
                    Color.Gray.PackedValue
                )
            );
            #endregion

            var initY = 10;

            #region Legend

            Add(new Label(ResGumps.MarkerIcon, true, HUE_FONT, 185, 255, FontStyle.BlackBorder) { X = 5, Y = initY });

            Add(new Label(ResGumps.MarkerName, true, HUE_FONT, 185, 255, FontStyle.BlackBorder) { X = 50, Y = initY });

            Add(new Label(ResGumps.MarkerX, true, HUE_FONT, 35, 255, FontStyle.BlackBorder) { X = 315, Y = initY });

            Add(new Label(ResGumps.MarkerY, true, HUE_FONT, 35, 255, FontStyle.BlackBorder) { X = 380, Y = initY });

            Add(new Label(ResGumps.MarkerColor, true, HUE_FONT, 35, 255, FontStyle.BlackBorder) { X = 420, Y = initY });

            Add(new Label(ResGumps.Edit, true, HUE_FONT, 35, 255, FontStyle.BlackBorder) { X = 475, Y = initY });

            Add(new Label(ResGumps.Remove, true, HUE_FONT, 40, 255, FontStyle.BlackBorder) { X = 505, Y = initY });

            Add(new Label(ResGumps.MarkerGoTo, true, HUE_FONT, 40, 255, FontStyle.BlackBorder) { X = 550, Y = initY });

            #endregion

            Add
            (
                new Line
                (
                    0,
                    initY + 20,
                    WIDTH,
                    1,
                    Color.Gray.PackedValue
                )
            );

            // Search Field
            Add(_searchTextBox = new SearchTextBoxControl(WIDTH / 2 - 150, 40));

            DrawArea(_markerFiles[_categoryId].IsEditable);

            var initX = 0;
            foreach (var file in _markerFiles)
            {
                var b = new NiceButton(
                        button_width * initX,
                        HEIGHT - 40,
                        button_width,
                        40,
                        ButtonAction.Activate,
                        file.Name,
                        MARKERS_CATEGORY_GROUP_INDEX
                        )
                {
                    ButtonParameter = initX,
                    IsSelectable = true,
                };

                b.SetTooltip(file.Name);
                if (initX == 0) b.IsSelected = true;

                Add(b);

                Add
                (
                    new Line
                    (
                        b.X,
                        b.Y,
                        1,
                        b.Height,
                        Color.Gray.PackedValue
                    )
                );
                initX++;
            }

            Add
            (
                new Line
                (
                    0,
                    HEIGHT - 40,
                    WIDTH,
                    1,
                    Color.Gray.PackedValue
                )
            );

            SetInScreen();
        }

        private void DrawArea(bool isEditable)
        {
            _scrollArea = new ScrollArea
            (
                10,
                80,
                WIDTH - 20,
                370,
                true
            );

            int i = 0;

            foreach (var marker in _markers.Select((value, idx) => new { idx, value }))
            {
                if (!string.IsNullOrEmpty(_searchText) && !marker.value.Name.ToLower().Contains(_searchText.ToLower()))
                {
                    continue;
                }

                var newElement = new MakerManagerControl(marker.value, i, marker.idx, isEditable);
                newElement.RemoveMarkerEvent += MarkerRemoveEventHandler;
                newElement.EditMarkerEvent += MarkerEditEventHandler;

                _scrollArea.Add(newElement);
                i += 25;
            }
            Add(_scrollArea);
        }

        private void MarkerRemoveEventHandler(object sender, EventArgs e)
        {
            if (sender is int idx)
            {
                _markers.RemoveAt(idx);
                // Clear area
                Remove(_scrollArea);
                //Redraw List
                DrawArea(_markerFiles[_categoryId].IsEditable);
                //Mark list as Modified
                _isMarkerListModified = true;
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case (int)ButtonsOption.SEARCH_BTN:
                    if (_searchText.Equals(_searchTextBox.SearchText))
                        return;
                    _searchText = _searchTextBox.SearchText;
                    break;
                case (int)ButtonsOption.CLEAR_SEARCH_BTN:
                    _searchTextBox.ClearText();
                    _searchText = "";
                    break;
                default:
                    _categoryId = buttonID;
                    _markers = _markerFiles[buttonID].Markers;
                    break;
            }

            _scrollArea.Clear();
            DrawArea(_markerFiles[_categoryId].IsEditable);
        }

        public override void OnKeyboardReturn(int textID, string text)
        {
            if (_searchText.Equals(_searchTextBox.SearchText))
                return;

            _scrollArea.Clear();
            _searchText = _searchTextBox.SearchText;
            DrawArea(_markerFiles[_categoryId].IsEditable);
        }

        private void MarkerEditEventHandler(object sender, EventArgs e)
        {
            _isMarkerListModified = true;
        }

        public override void Dispose()
        {
            if (_isMarkerListModified)
            {
                using (StreamWriter writer = new StreamWriter(_userMarkersFilePath, false))
                {
                    foreach (var marker in _markers)
                    {
                        var newLine = $"{marker.X},{marker.Y},{marker.MapId},{marker.Name},{marker.MarkerIconName},{marker.ColorName},4";

                        writer.WriteLine(newLine);
                    }
                }
                _isMarkerListModified = false;
                ReloadUserMarkers();
            }
            base.Dispose();
        }

        internal class DrawTexture : Control
        {
            public Texture2D Texture;

            public DrawTexture(Texture2D texture)
            {
                Texture = texture;
            }
        }

        private sealed class MakerManagerControl : Control
        {
            private readonly WMapMarker _marker;
            private readonly int _y;
            private readonly int _idx;
            private readonly bool _isEditable;

            private Label _labelName;
            private Label _labelX;
            private Label _labelY;
            private Label _labelColor;

            private DrawTexture _iconTexture;

            public event EventHandler RemoveMarkerEvent;
            public event EventHandler EditMarkerEvent;

            private enum ButtonsOption
            {
                EDIT_MARKER_BTN,
                REMOVE_MARKER_BTN,
                GOTO_MARKER_BTN
            }

            public MakerManagerControl(WMapMarker marker, int y, int idx, bool isEditable)
            {
                CanMove = true;

                _idx = idx;
                _marker = marker;
                _y = y;
                _isEditable = isEditable;

                DrawData();
            }

            private void DrawData()
            {
                if (_marker.MarkerIcon != null)
                {
                    _iconTexture = new DrawTexture(_marker.MarkerIcon) { X = 0, Y = _y - 5 };
                    Add(_iconTexture);
                }

                _labelName = new Label($"{_marker.Name}", true, HUE_FONT, 280) { X = 30, Y = _y };
                Add(_labelName);

                _labelX = new Label($"{_marker.X}", true, HUE_FONT, 35) { X = 305, Y = _y };
                Add(_labelX);

                _labelY = new Label($"{_marker.Y}", true, HUE_FONT, 35) { X = 350, Y = _y };
                Add(_labelY);

                _labelColor = new Label($"{_marker.ColorName}", true, HUE_FONT, 35) { X = 410, Y = _y };
                Add(_labelColor);

                if (_isEditable)
                {
                    Add(
                        new Button((int)ButtonsOption.EDIT_MARKER_BTN, 0xFAB, 0xFAC)
                        {
                            X = 470,
                            Y = _y,
                            ButtonAction = ButtonAction.Activate,
                        }
                    );

                    Add(
                        new Button((int)ButtonsOption.REMOVE_MARKER_BTN, 0xFB1, 0xFB2)
                        {
                            X = 505,
                            Y = _y,
                            ButtonAction = ButtonAction.Activate,
                        }
                    );
                }

                Add(
                    new Button((int)ButtonsOption.GOTO_MARKER_BTN, 0xFA5, 0xFA7)
                    {
                        X = 540,
                        Y = _y,
                        ButtonAction = ButtonAction.Activate,
                    }
                );
            }

            private void OnEditEnd(object sender, EventArgs e)
            {
                if (sender is WMapMarker editedMarker)
                {
                    _labelName.Text = editedMarker.Name;
                    _labelColor.Text = editedMarker.ColorName;
                    _labelX.Text = editedMarker.X.ToString();
                    _labelY.Text = editedMarker.Y.ToString();
                    if (editedMarker.MarkerIcon != null)
                        _iconTexture.Texture = editedMarker.MarkerIcon;

                    EditMarkerEvent.Raise();
                }
            }

            public override void OnButtonClick(int buttonId)
            {
                switch (buttonId)
                {
                    case (int)ButtonsOption.EDIT_MARKER_BTN:
                        UserMarkersGump existingGump = UIManager.GetGump<UserMarkersGump>();

                        existingGump?.Dispose();

                        var editUserMarkerGump = new UserMarkersGump(_marker.X, _marker.Y, _markers, _marker.ColorName, _marker.MarkerIconName, true, _idx);
                        editUserMarkerGump.EditEnd += OnEditEnd;

                        UIManager.Add(editUserMarkerGump);

                        break;
                    case (int)ButtonsOption.REMOVE_MARKER_BTN:
                        RemoveMarkerEvent.Raise(_idx);
                        break;
                    case (int)ButtonsOption.GOTO_MARKER_BTN:
                        var wmGump = UIManager.GetGump<WorldMapGump>();
                        if (wmGump != null)
                        {
                            wmGump.GoToMarker(_marker.X, _marker.Y, false);
                        }
                        break;
                }
            }
        }

        private sealed class SearchTextBoxControl : Control
        {
            private readonly StbTextBox _textBox;
            public string SearchText { get => _textBox.Text; }

            public SearchTextBoxControl(int x, int y)
            {
                AcceptMouseInput = true;
                AcceptKeyboardInput = true;

                Add
                (
                    new Label
                    (
                        ResGumps.MarkerSearch,
                        true,
                        HUE_FONT,
                        50,
                        1
                    )
                    {
                        X = x,
                        Y = y
                    }
                );

                Add
                (
                    new ResizePic(0x0BB8)
                    {
                        X = x + 50,
                        Y = y,
                        Width = 200,
                        Height = 25
                    }
                );

                _textBox = new StbTextBox
                (
                    0xFF,
                    30,
                    200,
                    true,
                    FontStyle.BlackBorder | FontStyle.Fixed
                )
                {
                    X = x + 53,
                    Y = y + 3,
                    Width = 200,
                    Height = 25
                };

                Add(_textBox);

                Add(
                    new Button((int)ButtonsOption.SEARCH_BTN, 0xFB7, 0xFB9)
                    {
                        X = x + 250,
                        Y = y + 1,
                        ButtonAction = ButtonAction.Activate,
                    }
                );

                Add(
                    new Button((int)ButtonsOption.CLEAR_SEARCH_BTN, 0xFB1, 0xFB2)
                    {
                        X = x + 285,
                        Y = y + 1,
                        ButtonAction = ButtonAction.Activate,
                    }
                );
            }

            public void ClearText()
            {
                _textBox.SetText("");
            }
        }
    }

}