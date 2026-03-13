using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class TopStatusBarGump : Gump
    {
        private const int BAR_HEIGHT = 28;

        private readonly TopStatusBarControl _statusBarControl;
        private bool _windowDragging;
        private Point _dragStartWindow;
        private Point _dragStartGlobalMouse;

        public TopStatusBarGump() : base(0, 0)
        {
            X = 0;
            Y = 0;
            Width = 800;
            Height = BAR_HEIGHT;
            CanMove = false;
            CanCloseWithRightClick = false;
            AcceptMouseInput = true;
            LayerOrder = UILayer.Over;

            _statusBarControl = new TopStatusBarControl(Width, Height);
            Add(_statusBarControl);
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left && x < Width - TopStatusBarControl.TITLEBAR_BUTTONS_WIDTH)
            {
                Client.Game.MaximizeOrRestoreWindow();
                return true;
            }
            return base.OnMouseDoubleClick(x, y, button);
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left && x < Width - TopStatusBarControl.TITLEBAR_BUTTONS_WIDTH)
            {
                _windowDragging = true;
                _dragStartWindow = Client.Game.GetWindowPosition();
                _dragStartGlobalMouse = Client.Game.GetGlobalMousePosition();
            }
            base.OnMouseDown(x, y, button);
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
                _windowDragging = false;
            if (button != MouseButtonType.Left)
            {
                base.OnMouseUp(x, y, button);
                return;
            }
            if (x >= Width - TopStatusBarControl.TITLEBAR_BUTTONS_WIDTH && x < Width)
            {
                int btnIndex = (x - (Width - TopStatusBarControl.TITLEBAR_BUTTONS_WIDTH)) / TopStatusBarControl.TITLEBAR_BUTTON_SIZE;
                if (btnIndex == 0)
                    Client.Game.MinimizeWindow();
                else if (btnIndex == 1)
                    Client.Game.MaximizeOrRestoreWindow();
                else if (btnIndex == 2)
                    Client.Game.Exit();
                return;
            }
            base.OnMouseUp(x, y, button);
        }

        protected override void OnMouseExit(int x, int y)
        {
            _statusBarControl.HoveredButtonIndex = -1;
            base.OnMouseExit(x, y);
        }

        public static void Create()
        {
            if (UIManager.GetGump<TopStatusBarGump>() != null)
                return;

            UIManager.Add(new TopStatusBarGump());
        }

        public override void Update()
        {
            base.Update();
            int w = Client.Game.Window.ClientBounds.Width;
            if (w > 0 && Width != w)
            {
                Width = w;
                _statusBarControl.Width = w;
            }
            if (_windowDragging && Mouse.LButtonPressed)
            {
                Point cur = Client.Game.GetGlobalMousePosition();
                Point newPos = _dragStartWindow + (cur - _dragStartGlobalMouse);
                Client.Game.SetWindowPosition(newPos.X, newPos.Y);
            }
            Point mouse = Mouse.Position;
            if (mouse.X >= 0 && mouse.X < Width && mouse.Y >= 0 && mouse.Y < Height)
            {
                if (mouse.X >= Width - TopStatusBarControl.TITLEBAR_BUTTONS_WIDTH)
                    _statusBarControl.HoveredButtonIndex = (mouse.X - (Width - TopStatusBarControl.TITLEBAR_BUTTONS_WIDTH)) / TopStatusBarControl.TITLEBAR_BUTTON_SIZE;
                else
                    _statusBarControl.HoveredButtonIndex = -1;
            }
            else
                _statusBarControl.HoveredButtonIndex = -1;
        }
    }
}
