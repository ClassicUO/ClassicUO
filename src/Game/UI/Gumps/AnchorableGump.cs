#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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

using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    internal class AnchorableGump : Gump
    {
        private GumpPic _lockGumpPic;
        private int _prevX, _prevY;
        private AnchorableGump _anchorCandidate;

        public AnchorableGump(uint local, uint server) : base(local, server)
        {
        }

        public string AnchorGroupName { get; protected set; }
        public virtual int GroupMatrixWidth { get; protected set; }
        public virtual int GroupMatrixHeight { get; protected set; }
        public int WidthMultiplier { get; protected set; } = 1;
        public int HeightMultiplier { get; protected set; } = 1;

        protected override void OnMove(int x, int y)
        {
            if (Keyboard.Alt)
            {
                UIManager.AnchorManager.DetachControl(this);
            }
            else
            {
                UIManager.AnchorManager[this]?.UpdateLocation(this, X - _prevX, Y - _prevY);
            }
            _prevX = X;
            _prevY = Y;

            base.OnMove(x, y);
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            UIManager.AnchorManager[this]?.MakeTopMost();

            _prevX = X;
            _prevY = Y;

            base.OnMouseDown(x, y, button);
        }

        protected override void OnMouseOver(int x, int y)
        {

            if (UIManager.IsDragging && UIManager.DraggingControl == this)
                _anchorCandidate = UIManager.AnchorManager.GetAnchorableControlUnder(this);

            base.OnMouseOver(x, y);
        }

        protected override void OnDragEnd(int x, int y)
        {
            if (_anchorCandidate != null)
            {
                Location = UIManager.AnchorManager.GetCandidateDropLocation(this, _anchorCandidate);
                UIManager.AnchorManager.DropControl(this, _anchorCandidate);
                _anchorCandidate = null;
            }

            base.OnDragEnd(x, y);
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (Keyboard.Alt && UIManager.AnchorManager[this] != null)
            {
                if (_lockGumpPic == null)
                {
                    _lockGumpPic = new GumpPic(0, 0, 0x082C, 0);
                    _lockGumpPic.Update(totalMS, frameMS);
                    _lockGumpPic.AcceptMouseInput = true;
                    _lockGumpPic.X = Width - _lockGumpPic.Width;
                    _lockGumpPic.Y = 0;
                    _lockGumpPic.MouseUp += _lockGumpPic_MouseClick;

                    Add(_lockGumpPic);
                }

                if (UIManager.MouseOverControl != null && (UIManager.MouseOverControl == this || UIManager.MouseOverControl.RootParent == this))
                    _lockGumpPic.Hue = 34;
                else
                    _lockGumpPic.Hue = 0;

            }
            else if ((!Keyboard.Alt || UIManager.AnchorManager[this] == null) && _lockGumpPic != null)
            {
                Remove(_lockGumpPic);
                _lockGumpPic.MouseUp += _lockGumpPic_MouseClick;
                _lockGumpPic.Dispose();
                _lockGumpPic = null;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            if (_anchorCandidate != null)
            {
                Point drawLoc = UIManager.AnchorManager.GetCandidateDropLocation(this, _anchorCandidate);

                if (drawLoc != Location)
                {
                    Texture2D previewColor = Texture2DCache.GetTexture(Color.Silver);
                    ResetHueVector();
                    _hueVector.Z = 0.5f;
                    batcher.Draw2D(previewColor, drawLoc.X, drawLoc.Y, Width, Height, ref _hueVector);

                    _hueVector.Z = 0;
                    // double rectangle for thicker "stroke"
                    batcher.DrawRectangle(previewColor, drawLoc.X, drawLoc.Y, Width, Height, ref _hueVector);
                    batcher.DrawRectangle(previewColor, drawLoc.X + 1, drawLoc.Y + 1, Width - 2, Height - 2, ref _hueVector);
                }
            }

            return true;
        }

        protected override void CloseWithRightClick()
        {
            if (UIManager.AnchorManager[this] == null || Keyboard.Alt || !ProfileManager.Current.HoldDownKeyAltToCloseAnchored)
            {
                if (ProfileManager.Current.CloseAllAnchoredGumpsInGroupWithRightClick)
                    UIManager.AnchorManager.DisposeAllControls(this);
                base.CloseWithRightClick();
            }
        }

        private void _lockGumpPic_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtonType.Left)
                UIManager.AnchorManager.DetachControl(this);
        }

        public override void Dispose()
        {
            UIManager.AnchorManager.DetachControl(this);

            base.Dispose();
        }
    }
}