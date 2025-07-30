using ClassicUO.Game.Managers;
using Microsoft.Xna.Framework;
using System.Linq;

namespace ClassicUO.Game.UI.Gumps
{
    internal partial class CounterBarGump
    {
        private class DraggableGump : Gump
        {
            public DraggableGump(World world) : base(world, 0, 0)
            {
                CanMove = true;
            }

            protected override void OnDragBegin(int x, int y)
            {
                base.OnDragBegin(x, y);
            }

            protected override void OnDragEnd(int x, int y)
            {
                if (UIManager.MouseOverControl == this || UIManager.MouseOverControl?.RootParent == this)
                {
                    Children.First()?.InvokeDragEnd(new Point(x, y));
                }

                base.OnDragEnd(x, y);
            }
        }
    }
}
