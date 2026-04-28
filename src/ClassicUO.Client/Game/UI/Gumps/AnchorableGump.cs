// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    internal enum ANCHOR_TYPE
    {
        NONE,
        SPELL,
        HEALTHBAR
    }

    internal abstract class AnchorableGump : Gump
    {
        private AnchorableGump _anchorCandidate;

        //private GumpPic _lockGumpPic;
        private int _prevX,
            _prevY;

        const ushort LOCK_GRAPHIC = 0x082C;

        protected AnchorableGump(World world, uint local, uint server) : base(world, local, server) { }

        public ANCHOR_TYPE AnchorType { get; protected set; }
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
            if (!IsDisposed && UIManager.IsDragging && UIManager.DraggingControl == this)
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

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
            float layerDepth = layerDepthRef;

            if (ShowLock)
            {
                Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);
                ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(LOCK_GRAPHIC);

                var texture = gumpInfo.Texture;
                if (texture != null)
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

                    var sourceRectangle = gumpInfo.UV;
                    renderLists.AddGumpSprite(
                        texture,
                        sourceRectangle,
                        new Rectangle(
                            x + (Width - sourceRectangle.Width),
                            y,
                            sourceRectangle.Width,
                            sourceRectangle.Height
                        ),
                        hueVector,
                        layerDepth
                    );
                }
            }

            if (_anchorCandidate != null)
            {
                Point drawLoc = UIManager.AnchorManager.GetCandidateDropLocation(
                    this,
                    _anchorCandidate
                );

                if (drawLoc != Location)
                {
                    Texture2D previewColor = SolidColorTextureCache.GetTexture(Color.Silver);
                    Vector3 fillVector = ShaderHueTranslator.GetHueVector(0, false, 0.5f);
                    Vector3 strokeVector = fillVector;
                    strokeVector.Z = 1f;

                    renderLists.AddGumpSprite(
                        previewColor,
                        new Rectangle(drawLoc.X, drawLoc.Y, Width, Height),
                        fillVector,
                        layerDepth
                    );

                    // double rectangle for thicker "stroke" — outer
                    renderLists.AddGumpSprite(previewColor, new Rectangle(drawLoc.X, drawLoc.Y, Width, 1), strokeVector, layerDepth); // top
                    renderLists.AddGumpSprite(previewColor, new Rectangle(drawLoc.X + Width - 1, drawLoc.Y, 1, Height), strokeVector, layerDepth); // right
                    renderLists.AddGumpSprite(previewColor, new Rectangle(drawLoc.X, drawLoc.Y + Height - 1, Width, 1), strokeVector, layerDepth); // bottom
                    renderLists.AddGumpSprite(previewColor, new Rectangle(drawLoc.X, drawLoc.Y, 1, Height), strokeVector, layerDepth); // left

                    // inner stroke (offset 1, shrunk 2)
                    renderLists.AddGumpSprite(previewColor, new Rectangle(drawLoc.X + 1, drawLoc.Y + 1, Width - 2, 1), strokeVector, layerDepth); // top
                    renderLists.AddGumpSprite(previewColor, new Rectangle(drawLoc.X + Width - 2, drawLoc.Y + 1, 1, Height - 2), strokeVector, layerDepth); // right
                    renderLists.AddGumpSprite(previewColor, new Rectangle(drawLoc.X + 1, drawLoc.Y + Height - 2, Width - 2, 1), strokeVector, layerDepth); // bottom
                    renderLists.AddGumpSprite(previewColor, new Rectangle(drawLoc.X + 1, drawLoc.Y + 1, 1, Height - 2), strokeVector, layerDepth); // left
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
