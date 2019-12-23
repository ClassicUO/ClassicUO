#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps.CharCreation
{
    class CreateCharSelectionCityGump : Gump
    {
        private enum Buttons
        {
            PreviousScreen,
            Finish,

            PreviousMap,
            NextMap
        }

        private readonly Point[] _townButtonsText =
        {
            new Point(105, 130),
            new Point(245, 90),
            new Point(165, 200),
            new Point(395, 160),
            new Point(200, 305),
            new Point(335, 250),
            new Point(160, 395),
            new Point(100, 250),
            new Point(270, 130),
        };
        private readonly string[] _cityNames = { "Felucca", "Trammel", "Ilshenar", "Malas", "Tokuno", "Ter Mur" };
        private int _selectedCityIndex;
        private readonly Label _facetName;
        private readonly HtmlControl _htmlControl;
        private readonly LoginScene _scene;
        private readonly List<CityControl>[] _cityTable;

        public CreateCharSelectionCityGump(byte profession, LoginScene scene) : base(0, 0)
        {
            CanMove = false;
            CanCloseWithRightClick = false;
            CanCloseWithEsc = false;

            _scene = scene;


            CityInfo city;

            if (UOFileManager.ClientVersion >= ClientVersions.CV_70130)
            {
                city = scene.GetCity(0);
            }
            else
            {
                city = scene.GetCity(3);

                if (city == null)
                {
                    city = scene.GetCity(0);
                }
            }

            if (city == null)
            {
                Log.Error("No city found. Something wrong with the received cities.");
                Dispose();
                return;
            }

            uint map = 0;

            if (city.IsNewCity)
            {
                map = city.Map;
            }

            _facetName = new Label("", true, 0x0481, font: 0, style: FontStyle.BlackBorder, align: TEXT_ALIGN_TYPE.TS_LEFT)
            {
                X = 240,
                Y = 440
            };

            int totalMaps = 1;

            if (UOFileManager.ClientVersion >= ClientVersions.CV_70130)
            {
                Add(new GumpPic(62, 54, (ushort) (0x15D9 + map), 0));
                Add(new GumpPic(57, 49, 0x15DF, 0));
                _facetName.Text = _cityNames[map];
                totalMaps = _cityNames.Length;
            }
            else
            {
                Add(new GumpPic(57, 49, 0x1598, 0));
                _facetName.IsVisible = false;
            }

            Add(_facetName);


            Add(new Button((int) Buttons.PreviousScreen, 0x15A1, 0x15A3, 0x15A2)
            {
                X = 586,
                Y = 445,
                ButtonAction = ButtonAction.Activate
            });

            Add(new Button((int) Buttons.Finish, 0x15A4, 0x15A6, 0x15A5)
            {
                X = 610,
                Y = 445,
                ButtonAction = ButtonAction.Activate
            });

            Add(new Button((int) Buttons.PreviousMap, 0x15A1, 0x15A3, 0x15A2)
            {
                X = 586,
                Y = 435,
                ButtonAction = ButtonAction.Activate
            });

            Add(new Button((int) Buttons.NextMap, 0x15A4, 0x15A6, 0x15A5)
            {
                X = 610,
                Y = 435,
                ButtonAction = ButtonAction.Activate
            });


            _htmlControl = new HtmlControl(452, 60, 175, 367, true, true, ishtml: true);
            Add(_htmlControl);



            _cityTable = new List<CityControl>[totalMaps];

            for (int i = 0; i < scene.Cities.Length; i++)
            {
                var c = scene.GetCity( UOFileManager.ClientVersion >= ClientVersions.CV_70130 ? i : i + 1);

                if (c == null)
                    continue;

                uint cityFacet = 0;

                int x = 0;
                int y = 0;

                if (c.IsNewCity)
                {
                    x = 62 + Utility.MathHelper.PercetangeOf(UOFileManager.Map.MapsDefaultSize[map, 0] - 2048, c.X, 383);
                    y = 54 + Utility.MathHelper.PercetangeOf(UOFileManager.Map.MapsDefaultSize[map, 1], c.Y, 384);
                    cityFacet = c.Map;
                }
                else if ( i < _townButtonsText.Length)
                {
                    x = _townButtonsText[i].X;
                    y = _townButtonsText[i].Y;
                }

                if (cityFacet > 5)
                    cityFacet = 5;

                if (_cityTable[cityFacet] == null)
                {
                    _cityTable[cityFacet] = new List<CityControl>();
                }

                var control = new CityControl(cityFacet, c, x, y);
                _cityTable[cityFacet].Add(control);
            }

        }


        private void SetCity(int index)
        {
            SetCity(_scene.GetCity(index));
        }

        private void SetCity(CityInfo city)
        {
            if (city == null)
                return;

            _htmlControl.Text = city.Description;
        }

        private void SetFacet(int index)
        {
            if (UOFileManager.ClientVersion < ClientVersions.CV_70130)
                return;

            if (index < 0)
                index = 0;
            else if (index >= _cityNames.Length)
                index = _cityNames.Length - 1;

            _facetName.Text = _cityNames[index];
        }


        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons) buttonID)
            {
                case Buttons.PreviousScreen:
                    break;
                case Buttons.Finish:
                    break;
                case Buttons.PreviousMap:
                    break;
                case Buttons.NextMap: 
                    break;
            }
        }


        class CityControl : Control
        {
            private readonly Label _label;
            
            public CityControl(uint facet, CityInfo city, int x, int y)
            {
                CanMove = false;

                Facet = facet;
                City = city;

                Add(new Button(0, 0x04B9, 0x04BA, 0x04BA)
                {
                    ButtonAction = ButtonAction.Activate,
                    X = x,
                    Y = y
                });
                x -= 20;

                if (city.Index == 3)
                {
                    x -= 60;
                }

                _label = new HoveredLabel(city.City, false, 0x0058, 0x0481, font: 3)
                {
                    X = x, Y = y
                };

                Add(_label);
            }

            public readonly uint Facet;
            public readonly CityInfo City;


            public override void OnButtonClick(int buttonID)
            {
                if (buttonID == 0)
                {
                    // TODO:
                }
            }
        }
    }

    internal class CreateCharCityGump : Gump
    {
        private readonly MapInfo[] _mapInfo =
        {
            new MapInfo(0, "Felucca", (ushort) (UOFileManager.ClientVersion >= ClientVersions.CV_70130 ? 5593 : 0x1598), 0x1400, 0x0000, 0x1000, 0x0000),
            new MapInfo(1, "Trammel", 5594, 0x1400, 0x0000, 0x1000, 0x0000),
            new MapInfo(2, "Ilshenar", 5595, 0x0900, 0x0200, 0x0640, 0x0000),
            new MapInfo(3, "Malas", 5596, 0x0A00, 0x0000, 0x0800, 0x0000),
            new MapInfo(4, "Tokuno", 5597, 0x05A8, 0x0000, 0x05A8, 0x0000),
            new MapInfo(5, "Ter Mur", 5598, 0x0500, 0x0100, 0x1000, 0x0AC0)
        };

        private readonly Dictionary<uint, CityCollection> _maps;

        private readonly byte _selectedProfession;
        private HtmlControl _description;

        private Label _mapName;
        private CityInfo _selectedCity;
        private CityCollection _selectedMap;

        private int _selectedMapIndex;

        public CreateCharCityGump(byte profession, LoginScene scene) : base(0, 0)
        {
            _selectedProfession = profession;

            _maps = scene.Cities.GroupBy(city => city.Map)
                         .ToDictionary(group => group.Key,
                                       group => new CityCollection(_mapInfo[group.Key], group.ToArray())
                                       {
                                           X = 57,
                                           Y = 49,
                                           OnSelect = SelectCity
                                       }
                                      );

            SelectedMapIndex = UOFileManager.ClientVersion >= ClientVersions.CV_70130 ? 0 : 3;

            if (_selectedMap.SkipSection)
            {
                SelectCity(_selectedMap.FirstOrDefault());
                OnButtonClick((int) Buttons.Finish);
                Dispose();

                return;
            }

            //why we make calculations on program if it could be done outside?
            var mapCenterX = 253; //393 / 2 + 57;

            Add(new Button((int) Buttons.PreviousCollection, 0x15A1, 0x15A3, 0x15A2)
            {
                X = mapCenterX - 65,
                Y = 440,
                ButtonAction = ButtonAction.Activate
            });

            Add(new Button((int) Buttons.NextCollection, 0x15A4, 0x15A6, 0x15A5)
            {
                X = mapCenterX + 50,
                Y = 440,
                ButtonAction = ButtonAction.Activate
            });

            Add(new Button((int) Buttons.PreviousScreen, 0x15A1, 0x15A3, 0x15A2)
            {
                X = 586,
                Y = 435,
                ButtonAction = ButtonAction.Activate
            });

            Add(new Button((int) Buttons.Finish, 0x15A4, 0x15A6, 0x15A5)
            {
                X = 610,
                Y = 435,
                ButtonAction = ButtonAction.Activate
            });
        }

        public int SelectedMapIndex
        {
            get => _selectedMapIndex;
            set
            {
                _selectedMapIndex = value;

                if (_selectedMapIndex < 0)
                    _selectedMapIndex = _maps.Count - 1;

                if (_selectedMapIndex >= _maps.Count)
                    _selectedMapIndex = 0;

                SelectMap(_maps.ElementAt(_selectedMapIndex).Value);
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            var charCreationGump = UIManager.GetGump<CharCreationGump>();

            switch ((Buttons) buttonID)
            {
                case Buttons.PreviousScreen:
                    charCreationGump?.StepBack(_selectedProfession > 0 ? 2 : 1);

                    break;

                case Buttons.Finish:

                    if (_selectedCity != default)
                        charCreationGump?.SetCity(_selectedCity);

                    charCreationGump?.CreateCharacter(_selectedProfession);

                    break;

                case Buttons.PreviousCollection:
                    SelectedMapIndex--;

                    break;

                case Buttons.NextCollection:
                    SelectedMapIndex++;

                    break;
            }

            base.OnButtonClick(buttonID);
        }

        public void SelectCity(CityInfo city)
        {
            SelectCity(city.Index);
        }

        public void SelectCity(CitySelector selector)
        {
            SelectCity(selector.ButtonID);
        }

        public void SelectCity(int index)
        {
            var city = _selectedMap?.FirstOrDefault(c => c.Index == index);

            if (city != null && _selectedMap.Index == city.Map)
            {
                var selectors = GetSelectors();

                foreach (var s in selectors)
                    s.IsSelected = false;

                var citySelector = selectors.FirstOrDefault(s => s.ButtonID == city.Index);

                if (citySelector != null)
                    citySelector.IsSelected = true;

                _selectedCity = city;

                SetDescription(city);
            }
        }

        private void SelectMap(CityCollection map)
        {
            if (_selectedMap != null)
                Remove(_selectedMap);

            _selectedMap = map;

            if (_selectedMap != null)
            {
                Add(_selectedMap);

                if (_mapName != null)
                    Remove(_mapName);

                var name = map.Name;
                var nameWidth = UOFileManager.Fonts.GetWidthASCII(3, name);

                Add(_mapName = new Label(name, false, 1153, font: 3)
                {
                    X = 57 + ((393 - nameWidth) >> 1),
                    Y = 440
                });

                SelectCity(_selectedMap.FirstOrDefault());
            }
        }

        private void SetDescription(CityInfo info)
        {
            if (_description != null)
                Remove(_description);

            Add(_description = new HtmlControl(452, 60, 173, 367, true, true, false,
                                               info.Description, 0x000000, true));
        }

        private IEnumerable<CitySelector> GetSelectors()
        {
            foreach (var map in _maps.Values)
            {
                foreach (var selector in map.FindControls<CitySelector>())
                    yield return selector;
            }
        }

        internal class MapInfo
        {
            public MapInfo(int mapIndex, string name, ushort gump, int width, int widthOffset, int height, int heightOffset)
            {
                Index = mapIndex;
                Name = name;
                Gump = gump;

                Width = width;
                Height = height;

                WidthOffset = widthOffset;
                HeightOffset = heightOffset;
            }

            public int Index { get; set; }
            public string Name { get; set; }
            public ushort Gump { get; set; }

            public int Width { get; set; }
            public int Height { get; set; }

            public int WidthOffset { get; set; }
            public int HeightOffset { get; set; }
        }

        private enum Buttons
        {
            PreviousScreen,
            Finish,

            PreviousCollection,
            NextCollection
        }


        internal class CityCollection : Gump, IEnumerable<CityInfo>
        {
            private readonly CityInfo[] _cities;
            private readonly MapInfo _mapInfo;
            internal bool SkipSection;

            public CityCollection(MapInfo mapInfo, CityInfo[] cities) : base(0, 0)
            {
                _mapInfo = mapInfo;
                _cities = cities;

                if (mapInfo.Index == 0 || mapInfo.Index == 3)
                {
                    if (UOFileManager.Gumps.GetTexture(mapInfo.Gump) == null)
                    {
                        SkipSection = true;

                        return;
                    }
                }

                if (UOFileManager.ClientVersion == ClientVersions.CV_70130)
                {
                    Add(new GumpPic(5, 5, _mapInfo.Gump, 0));
                    Add(new GumpPic(0, 0, 0x15DF, 0));
                }
                else
                    Add(new GumpPic(0, 0, 0x1598, 0));


                var width = 393;
                var height = 393;

                var mapWidth = _mapInfo.Width - _mapInfo.WidthOffset;
                var mapHeight = _mapInfo.Height - _mapInfo.HeightOffset;

                var cityCount = cities.Length;

                for (int i = 0; i < cityCount; i++)
                {
                    var city = cities[i];

                    var buttonX = city.IsNewCity ? (city.X - _mapInfo.WidthOffset) * width / mapWidth : city.X - 62;
                    var buttonY = city.IsNewCity ? (city.Y - _mapInfo.HeightOffset) * height / mapHeight : city.Y - 54;

                    var button = new Button(city.Index, 1209, 1210, 1210)
                    {
                        X = buttonX,
                        Y = buttonY,
                        ButtonAction = ButtonAction.Activate
                    };

                    var textX = buttonX;
                    var textY = buttonY - 16;

                    var cityName = city.City;
                    var cityNameWidth = UOFileManager.Fonts.GetWidthASCII(3, cityName);

                    var right = textX + cityNameWidth;
                    var mapRight = width - 20;

                    if (right > mapRight)
                        textX -= right - mapRight;

                    var label = new Label(cityName, false, 88, font: 3)
                    {
                        X = textX,
                        Y = textY
                    };

                    Add(new CitySelector(button, label)
                    {
                        OnClick = selector => OnSelect(selector)
                    });
                }
            }

            public int Index => _mapInfo.Index;
            public string Name => _mapInfo.Name;

            public Action<CitySelector> OnSelect { get; set; }

            public IEnumerator<CityInfo> GetEnumerator()
            {
                foreach (var element in _cities)
                    yield return element;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _cities.GetEnumerator();
            }
        }

        public class CitySelector : Gump
        {
            private readonly Button _button;

            private readonly Label _label;
            private bool _isSelected;

            public CitySelector(Button button, Label label) : base(0, 0)
            {
                Add(_button = button);
                Add(_label = label);
                _label.AcceptMouseInput = true;
                _label.MouseUp += LabelOnMouseUp;
                button.MouseDoubleClick += ButtonOnMouseDoubleClick;
            }

            private void ButtonOnMouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
            {
                if (e.Button == MouseButton.Left)
                {
                    
                    e.Result = true;
                }
            }

            public int ButtonID => _button.ButtonID;

            public Action<CitySelector> OnClick { get; set; }

            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    if (_isSelected != value)
                    {
                        _label.Hue = (ushort) (value ? 1153 : 88);
                        _button.ButtonGraphicNormal = (ushort) (value ? 1210 : 1209);

                        _isSelected = value;
                    }
                }
            }

            private void LabelOnMouseUp(object sender, MouseEventArgs e)
            {
                OnClick(this);
            }

            public override void Update(double totalMS, double frameMS)
            {
                base.Update(totalMS, frameMS);

                if (IsSelected)
                    return;

                bool contains = Children.Contains(UIManager.MouseOverControl);

                if (contains && _label.Hue != 153)
                    _label.Hue = 153;
                else if (!contains && _label.Hue != 88)
                    _label.Hue = 88;
            }

            public override void OnButtonClick(int buttonID)
            {
                if (_button.ButtonID == buttonID)
                    OnClick?.Invoke(this);
            }
        }
    }
}