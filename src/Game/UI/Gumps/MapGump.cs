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

using System.Collections.Generic;

using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    internal class MapGump : Gump
    {
        private readonly Button[] _buttons = new Button[3];
        private readonly List<Control> _container = new List<Control>();
        private readonly TextureControl _textureControl;

        public MapGump(Serial serial, ushort gumpid, int width, int height) : base(serial, 0)
        {
            AcceptMouseInput = false;
            CanMove = true;

            Width = width;
            Height = height;

            Add(new ResizePic(0x1432)
            {
                Width = width + 44, Height = height + 61
            });


            Add(_buttons[0] = new Button((int) ButtonType.PlotCourse, 0x1398, 0x1398) {X = (width - 100) >> 1, Y = 5, ButtonAction = ButtonAction.Activate});
            Add(_buttons[1] = new Button((int) ButtonType.StopPlotting, 0x1399, 0x1399) {X = (width - 70) >> 1, Y = 5, ButtonAction = ButtonAction.Activate});
            Add(_buttons[2] = new Button((int) ButtonType.ClearCourse, 0x139A, 0x139A) {X = (width - 66) >> 1, Y = height + 37, ButtonAction = ButtonAction.Activate});

            _buttons[0].IsVisible = _buttons[0].IsEnabled = PlotState == 0;
            _buttons[1].IsVisible = _buttons[1].IsEnabled = PlotState == 1;
            _buttons[2].IsVisible = _buttons[2].IsEnabled = PlotState == 1;

            Add(_textureControl = new TextureControl
            {
                X = 24, Y = 31,
                Width = width,
                Height = height
            });

            Add(new GumpPic(width - 20, height - 20, 0x0139D, 0));


            //WantUpdateSize = false;
        }

        public int PlotState { get; private set; }

        public void SetMapTexture(SpriteTexture texture)
        {
            _textureControl.Texture?.Dispose();
            _textureControl.WantUpdateSize = true;
            _textureControl.Texture = texture;

            WantUpdateSize = true;
        }

        public void AddToContainer(Control c)
        {
            _container.Add(c);
            Add(c);
        }

        public void ClearContainer()
        {
            _container.ForEach(s => s.Dispose());
            _container.Clear();
        }

        public void SetPlotState(int s)
        {
            PlotState = s;

            _buttons[0].IsVisible = _buttons[0].IsEnabled = PlotState == 0;
            _buttons[1].IsVisible = _buttons[1].IsEnabled = PlotState == 1;
            _buttons[2].IsVisible = _buttons[2].IsEnabled = PlotState == 1;
        }

        private enum ButtonType
        {
            PlotCourse,
            StopPlotting,
            ClearCourse
        }
    }
}