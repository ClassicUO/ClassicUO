using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Controls
{
    class ContextMenuControl : Control
    {
        private List<ContextMenuItem> _items;

        private AlphaBlendControl _background;

        public ContextMenuControl()
        {
            CanMove = false;
            AcceptMouseInput = true;

            WantUpdateSize = false;


            _background = new AlphaBlendControl();
            Add(_background);
        }


        public IReadOnlyList<ContextMenuItem> Items => _items;



        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
        }


        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            return base.Draw(batcher, x, y);
        }

        public override void Add(Control c, int page = 0)
        {
            base.Add(c, page);
        }
    }


    class ContextMenuItem : Control
    {

    }
}
