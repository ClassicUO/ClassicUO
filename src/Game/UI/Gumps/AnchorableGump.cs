
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;

namespace ClassicUO.Game.UI.Gumps
{
    internal class AnchorableGump : Gump
    {
        public string AnchorGroupName { get; protected set; }
        public virtual int GroupMatrixWidth { get; protected set; }
        public virtual int GroupMatrixHeight { get; protected set; }
        public int WidthMultiplier { get; protected set; } = 1;
        public int HeightMultiplier { get; protected set; } = 1;
        
        private GumpPic _lockGumpPic;
        private int _prevX, _prevY;

        public AnchorableGump(Serial local, Serial server) : base(local, server)
        {

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

            if (Input.Keyboard.Alt && Engine.AnchorManager[this] != null && _lockGumpPic == null)
            {
                _lockGumpPic = new GumpPic(0, 0, 0x082C, 0);
                _lockGumpPic.Update(totalMS, frameMS);
                _lockGumpPic.AcceptMouseInput = true;
                _lockGumpPic.X = Width - _lockGumpPic.Width;
                _lockGumpPic.Y = 0;
                _lockGumpPic.MouseClick += _lockGumpPic_MouseClick;

                Add(_lockGumpPic);
            } else if ((!Input.Keyboard.Alt || Engine.AnchorManager[this] == null) && _lockGumpPic != null)
            {
                Remove(_lockGumpPic);
                _lockGumpPic.Dispose();
                _lockGumpPic = null;
            }
        }

        protected override void CloseWithRightClick()
        {
            if (Engine.AnchorManager[this] == null || Input.Keyboard.Alt || !Engine.Profile.Current.HoldDownKeyAltToCloseAnchored)
            {
                Engine.AnchorManager.DisposeAllControls(this);
                base.CloseWithRightClick();
            }
        }

        private void _lockGumpPic_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButton.Left)
                Engine.AnchorManager.DetachControl(this);
        }

        public override void Dispose()
        {
            Engine.AnchorManager.DetachControl(this);
            
            base.Dispose();
        }
    }
}
