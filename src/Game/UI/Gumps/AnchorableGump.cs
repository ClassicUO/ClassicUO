using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.UI.Gumps
{
    class AnchorableGump : Gump
    {
        //public AnchorManager.AnchorGroup AnchorGroup { get; set; }
        private int prevX, prevY;

        public AnchorableGump(Serial local, Serial server) : base(local, server)
        {
        }

        protected override void OnMove()
        {
            Engine.AnchorManager[this]?.UpdateLocation(this, X - prevX, Y - prevY);
            prevX = X;
            prevY = Y;

            base.OnMove();
        }

        protected override void OnMouseDown(int x, int y, MouseButton button)
        {
            prevX = X;
            prevY = Y;

            base.OnMouseDown(x, y, button);
        }

        public override void Dispose()
        {
            Engine.AnchorManager.DisposeAllControls(this);

            base.Dispose();
        }

        protected override void OnMouseOver(int x, int y)
        {
            if (Engine.UI.IsDragging)
            {
                AnchorableGump ctrl = Engine.AnchorManager.GetAnchorableControlOver(this, x, y);

                if (ctrl != null)
                {
                    Location = Engine.AnchorManager.GetCandidateDropLocation(
                        this, 
                        ctrl, 
                        ScreenCoordinateX + x - ctrl.ScreenCoordinateX,
                        ScreenCoordinateY + y - ctrl.ScreenCoordinateY);
                }
            }

            base.OnMouseOver(x, y);
        }

        protected override void OnDragEnd(int x, int y)
        {
            AnchorableGump ctrl = Engine.AnchorManager.GetAnchorableControlOver(this, x, y);

            if (ctrl != null)
            {
                Engine.AnchorManager.DropControl(
                    this,
                    ctrl,
                    ScreenCoordinateX + x - ctrl.ScreenCoordinateX,
                    ScreenCoordinateY + y - ctrl.ScreenCoordinateY);
            }

            base.OnDragEnd(x, y);
        }
    }
}
