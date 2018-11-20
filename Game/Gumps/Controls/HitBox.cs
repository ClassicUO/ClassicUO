using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Input;

namespace ClassicUO.Game.Gumps.Controls
{
    class HitBox: GumpControl
    {
        int _buttonId;

        public HitBox(int buttonId, int x, int y, int width, int height)
        {
            _buttonId = buttonId;
            X = x;
            Y = y;
            Width = width;
            Height = height;

            AcceptMouseInput = true;
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            OnButtonClick(_buttonId);

            base.OnMouseClick(x, y, button);
        }
    }
}
