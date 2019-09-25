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

using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;

namespace ClassicUO.Game.UI.Gumps.CharCreation
{
    internal class CreateCharCityGump : Gump
    {
        private static readonly MapInfo[] _mapInfo =
        {
            new MapInfo(0, "Felucca", 5593, 0x1400, 0x0000, 0x1000, 0x0000),
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

        public CreateCharCityGump(byte profession) : base(0, 0)
        {
            _selectedProfession = profession;
            var loginScene = Engine.SceneManager.GetScene<LoginScene>();

            _maps = loginScene.Cities.GroupBy(city => city.Map)
                              .ToDictionary(group => group.Key,
                                            group => new CityCollection(_mapInfo[group.Key], group.ToArray())
                                            {
                                                X = 57,
                                                Y = 49,
                                                OnSelect = SelectCity
                                            }
                                           );

            SelectedMapIndex = FileManager.ClientVersion >= ClientVersions.CV_70130 ? 0 : 3;

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
            var charCreationGump = Engine.UI.GetGump<CharCreationGump>();

            switch ((Buttons) buttonID)
            {
                case Buttons.PreviousScreen:
                    charCreationGump.StepBack(_selectedProfession > 0 ? 2 : 1);

                    break;

                case Buttons.Finish:

                    if (_selectedCity != default)
                        charCreationGump.SetCity(_selectedCity);

                    charCreationGump.CreateCharacter(_selectedProfession);

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
                var nameWidth = FileManager.Fonts.GetWidthASCII(3, name);

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
            public MapInfo(int mapIndex, string name, Graphic gump, int width, int widthOffset, int height, int heightOffset)
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
            public Graphic Gump { get; set; }

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
                    if (FileManager.Gumps.GetTexture(mapInfo.Gump) == null)
                    {
                        SkipSection = true;

                        return;
                    }
                }

                if (FileManager.ClientVersion == ClientVersions.CV_70130)
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

                    var buttonX = city.IsNewCity ? (city.Position.X - _mapInfo.WidthOffset) * width / mapWidth : city.Position.X - 62;
                    var buttonY = city.IsNewCity ? (city.Position.Y - _mapInfo.HeightOffset) * height / mapHeight : city.Position.Y - 54;

                    var button = new Button(city.Index, 1209, 1210, 1210)
                    {
                        X = buttonX,
                        Y = buttonY,
                        ButtonAction = ButtonAction.Activate
                    };

                    var textX = buttonX;
                    var textY = buttonY - 16;

                    var cityName = city.City;
                    var cityNameWidth = FileManager.Fonts.GetWidthASCII(3, cityName);

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

                bool contains = Children.Contains(Engine.UI.MouseOverControl);

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