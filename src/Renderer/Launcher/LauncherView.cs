using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer.UI;

namespace ClassicUO.Renderer.Launcher
{
    class LauncherView : Control
    {
        public LauncherView()
        {
            Width = 400;
            Height = 400;
            WantUpdateSize = false;

            //Add(new Button());
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            return base.Draw(batcher, x, y);
        }
    }
}
