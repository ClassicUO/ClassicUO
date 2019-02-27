using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.UI.Gumps
{
    class AnchorableGump : Gump
    {
        private GumpPic _lockGumpPic;
        private int _prevX, _prevY;
        private bool _isAltPressed = false;

        public AnchorableGump(Serial local, Serial server) : base(local, server)
        {
            Engine.Input.KeyDown += Input_KeyDown;
            Engine.Input.KeyUp += Input_KeyUp;
        }
        
        private void Input_KeyDown(object sender, SDL.SDL_KeyboardEvent e)
        {
            _isAltPressed = (e.keysym.mod & SDL.SDL_Keymod.KMOD_LALT) != 0;
        }

        private void Input_KeyUp(object sender, SDL.SDL_KeyboardEvent e)
        {
            _isAltPressed = (e.keysym.mod & SDL.SDL_Keymod.KMOD_LALT) != 0;
        }

        protected override void OnMove()
        {
            Engine.AnchorManager[this]?.UpdateLocation(this, X - _prevX, Y - _prevY);
            _prevX = X;
            _prevY = Y;

            base.OnMove();
        }
        
        protected override void OnMouseDown(int x, int y, MouseButton button)
        {
            Engine.AnchorManager[this]?.MakeTopMost();

            _prevX = X;
            _prevY = Y;

            base.OnMouseDown(x, y, button);
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

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (_isAltPressed && Engine.AnchorManager[this] != null && _lockGumpPic == null)
            {
                _lockGumpPic = new GumpPic(0, 0, 0x082C, 0);
                _lockGumpPic.Update(totalMS, frameMS);
                _lockGumpPic.AcceptMouseInput = true;
                _lockGumpPic.X = Width - _lockGumpPic.Width;
                _lockGumpPic.Y = 0;
                _lockGumpPic.MouseClick += _lockGumpPic_MouseClick;

                Add(_lockGumpPic);
            } else if ((!_isAltPressed || Engine.AnchorManager[this] == null) && _lockGumpPic != null)
            {
                Remove(_lockGumpPic);
                _lockGumpPic.Dispose();
                _lockGumpPic = null;
            }
        }

        protected override void CloseWithRightClick()
        {
            Engine.AnchorManager.DisposeAllControls(this);

            base.CloseWithRightClick();
        }

        private void _lockGumpPic_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButton.Left)
                Engine.AnchorManager.DetachControl(this);
        }

        public override void Dispose()
        {
            Engine.Input.KeyDown -= Input_KeyDown;
            Engine.Input.KeyUp -= Input_KeyUp;
            Engine.AnchorManager.DetachControl(this);
            
            base.Dispose();
        }
    }
}
