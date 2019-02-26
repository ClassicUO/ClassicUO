using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    class MapGump : Gump
    {
        private readonly TextureControl _textureControl;
        private readonly List<Control> _container = new List<Control>();

        private readonly Button[] _buttons = new Button[3];

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


            Add(_buttons[0] = new Button((int)ButtonType.PlotCourse, 0x1398, 0x1398) { X = (width - 100) / 2, Y = 5, ButtonAction = ButtonAction.Activate,  });
            Add(_buttons[1] = new Button((int)ButtonType.StopPlotting, 0x1399, 0x1399) { X = (width - 70) / 2, Y = 5, ButtonAction = ButtonAction.Activate, });
            Add(_buttons[2] = new Button((int)ButtonType.ClearCourse, 0x139A, 0x139A) { X = (width - 66) / 2, Y = height + 37, ButtonAction = ButtonAction.Activate, });

            _buttons[0].IsVisible = _buttons[0].IsEnabled = PlotState == 0;
            _buttons[1].IsVisible = _buttons[1].IsEnabled = PlotState == 1;
            _buttons[2].IsVisible = _buttons[2].IsEnabled = PlotState == 1;

            Add(_textureControl = new TextureControl()
            {
                X = 24, Y = 31,
                Width = width,
                Height = height,
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
