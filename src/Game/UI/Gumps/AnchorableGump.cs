﻿#region license

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

using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;

namespace ClassicUO.Game.UI.Gumps
{
    internal class AnchorableGump : Gump
    {
        private GumpPic _lockGumpPic;
        private int _prevX, _prevY;

        public AnchorableGump(Serial local, Serial server) : base(local, server)
        {
        }

        public string AnchorGroupName { get; protected set; }
        public virtual int GroupMatrixWidth { get; protected set; }
        public virtual int GroupMatrixHeight { get; protected set; }
        public int WidthMultiplier { get; protected set; } = 1;
        public int HeightMultiplier { get; protected set; } = 1;

        protected override void OnMove()
        {
            Engine.UI.AnchorManager[this]?.UpdateLocation(this, X - _prevX, Y - _prevY);
            _prevX = X;
            _prevY = Y;

            base.OnMove();
        }

        protected override void OnMouseDown(int x, int y, MouseButton button)
        {
            Engine.UI.AnchorManager[this]?.MakeTopMost();

            _prevX = X;
            _prevY = Y;

            base.OnMouseDown(x, y, button);
        }

        protected override void OnMouseOver(int x, int y)
        {
            if (Engine.UI.IsDragging)
            {
                AnchorableGump ctrl = Engine.UI.AnchorManager.GetAnchorableControlOver(this, x, y);

                if (ctrl != null)
                {
                    Location = Engine.UI.AnchorManager.GetCandidateDropLocation(
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
            AnchorableGump ctrl = Engine.UI.AnchorManager.GetAnchorableControlOver(this, x, y);

            if (ctrl != null)
            {
                Engine.UI.AnchorManager.DropControl(
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

            if (Keyboard.Alt && Engine.UI.AnchorManager[this] != null && _lockGumpPic == null)
            {
                _lockGumpPic = new GumpPic(0, 0, 0x082C, 0);
                _lockGumpPic.Update(totalMS, frameMS);
                _lockGumpPic.AcceptMouseInput = true;
                _lockGumpPic.X = Width - _lockGumpPic.Width;
                _lockGumpPic.Y = 0;
                _lockGumpPic.MouseUp += _lockGumpPic_MouseClick;

                Add(_lockGumpPic);
            }
            else if ((!Keyboard.Alt || Engine.UI.AnchorManager[this] == null) && _lockGumpPic != null)
            {
                Remove(_lockGumpPic);
                _lockGumpPic.Dispose();
                _lockGumpPic = null;
            }
        }

        protected override void CloseWithRightClick()
        {
            if (Engine.UI.AnchorManager[this] == null || Keyboard.Alt || !Engine.Profile.Current.HoldDownKeyAltToCloseAnchored)
            {
                Engine.UI.AnchorManager.DisposeAllControls(this);
                base.CloseWithRightClick();
            }
        }

        private void _lockGumpPic_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButton.Left)
                Engine.UI.AnchorManager.DetachControl(this);
        }

        public override void Dispose()
        {
            Engine.UI.AnchorManager.DetachControl(this);

            base.Dispose();
        }
    }
}