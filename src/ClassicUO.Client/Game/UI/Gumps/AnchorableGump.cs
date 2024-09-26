#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    public enum ANCHOR_TYPE
    {
        NONE,
        SPELL,
        HEALTHBAR,
        DISABLED
    }

    public abstract class AnchorableGump : Gump
    {
        private AnchorableGump _anchorCandidate;

        //private GumpPic _lockGumpPic;
        private int _prevX,
            _prevY;

        const ushort LOCK_GRAPHIC = 0x082C;

        protected AnchorableGump(World world, uint local, uint server) : base(world, local, server) { }

        public ANCHOR_TYPE AnchorType { get; set; }
        public virtual int GroupMatrixWidth { get; protected set; }
        public virtual int GroupMatrixHeight { get; protected set; }
        public int WidthMultiplier { get; protected set; } = 1;
        public int HeightMultiplier { get; protected set; } = 1;

        public bool ShowLock => Keyboard.Alt && UIManager.AnchorManager[this] != null;

        protected override void OnMove(int x, int y)
        {
            if (Keyboard.Alt && !ProfileManager.CurrentProfile.HoldAltToMoveGumps)
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
            if (!IsDisposed && UIManager.IsDragging && UIManager.DraggingControl == this && AnchorType != ANCHOR_TYPE.DISABLED)
            {
                _anchorCandidate = UIManager.AnchorManager.GetAnchorableControlUnder(this);
            }

            base.OnMouseOver(x, y);
        }

        protected override void OnDragEnd(int x, int y)
        {
            Attache();

            base.OnDragEnd(x, y);
        }

        public void TryAttacheToExist()
        {
            _anchorCandidate = UIManager.AnchorManager.GetAnchorableControlUnder(this);

            Attache();
        }

        private void Attache()
        {
            if (_anchorCandidate != null)
            {
                Location = UIManager.AnchorManager.GetCandidateDropLocation(this, _anchorCandidate);
                UIManager.AnchorManager.DropControl(this, _anchorCandidate);
                _anchorCandidate = null;
            }
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left && ShowLock)
            {
                ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(LOCK_GRAPHIC);
                if (gumpInfo.Texture != null)
                {
                    if (
                        x >= Width - gumpInfo.UV.Width
                        && x < Width
                        && y >= 0
                        && y <= gumpInfo.UV.Height
                    )
                    {
                        UIManager.AnchorManager.DetachControl(this);
                    }
                }
            }

            base.OnMouseUp(x, y, button);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            Vector3 hueVector;

            if (ShowLock)
            {
                hueVector = ShaderHueTranslator.GetHueVector(0);
                ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(LOCK_GRAPHIC);

                if (gumpInfo.Texture != null)
                {
                    if (
                        UIManager.MouseOverControl != null
                        && (
                            UIManager.MouseOverControl == this
                            || UIManager.MouseOverControl.RootParent == this
                        )
                    )
                    {
                        hueVector.X = 34;
                        hueVector.Y = 1;
                    }

                    batcher.Draw(
                        gumpInfo.Texture,
                        new Vector2(x + (Width - gumpInfo.UV.Width), y),
                        gumpInfo.UV,
                        hueVector
                    );
                }
            }

            hueVector = ShaderHueTranslator.GetHueVector(0);

            if (_anchorCandidate != null)
            {
                Point drawLoc = UIManager.AnchorManager.GetCandidateDropLocation(
                    this,
                    _anchorCandidate
                );

                if (drawLoc != Location)
                {
                    Texture2D previewColor = SolidColorTextureCache.GetTexture(Color.Silver);
                    hueVector = ShaderHueTranslator.GetHueVector(0, false, 0.5f);

                    batcher.Draw(
                        previewColor,
                        new Rectangle(drawLoc.X, drawLoc.Y, Width, Height),
                        hueVector
                    );

                    hueVector.Z = 1f;

                    // double rectangle for thicker "stroke"
                    batcher.DrawRectangle(
                        previewColor,
                        drawLoc.X,
                        drawLoc.Y,
                        Width,
                        Height,
                        hueVector
                    );

                    batcher.DrawRectangle(
                        previewColor,
                        drawLoc.X + 1,
                        drawLoc.Y + 1,
                        Width - 2,
                        Height - 2,
                        hueVector
                    );
                }
            }

            return true;
        }

        protected override void CloseWithRightClick()
        {
            if (
                UIManager.AnchorManager[this] == null
                || Keyboard.Alt
                || !ProfileManager.CurrentProfile.HoldDownKeyAltToCloseAnchored
            )
            {
                if (ProfileManager.CurrentProfile.CloseAllAnchoredGumpsInGroupWithRightClick)
                {
                    UIManager.AnchorManager.DisposeAllControls(this);
                }

                base.CloseWithRightClick();
            }
        }

        public override void Dispose()
        {
            UIManager.AnchorManager.DetachControl(this);

            base.Dispose();
        }
    }
}
