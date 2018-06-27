using ClassicUO.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClassicUO.UI
{
    //http://www.sdltutorials.com/sdl-events

    public sealed class UIEngine
    {
        private Control _lastFocused;
        private Game _game;

        public UIEngine(Game game)
        {
            _game = game;
            Controls = new List<Control>();
        }

        public List<Control> Controls { get; }


        public void Initialize()
        {

        }


        public void Draw(GameTime time, SpriteBatch spriteBatch)
        {
            Controls.ForEach(s => s.Draw(time, spriteBatch));
        }

        public void Update(GameTime time)
        {
            Controls.ForEach(s => s.Update(time));
        }

        private void DoMouseButtonEvents(MouseEventArgs arg)
        {
            Control control = null;

            bool func(Control s)
            {
                if (arg.Button == MouseButton.Left && arg.ButtonState == ButtonState.Released && s.CanDragNow)
                    s.CanDragNow = false;

                if (s.IsEnabled && s.IsVisible && s.Rectangle.Contains(arg.Location.X, arg.Location.Y))
                {
                    if (s.IsMovable && !s.CanDragNow && arg.Button == MouseButton.Left
                        && arg.ButtonState == ButtonState.Pressed)
                        s.CanDragNow = true;
                    return true;
                }

                return false;
            }
            foreach (Control c in Controls)
                GetControl(c, func, ref control);

            if (control != null)
            {
                if (arg.ButtonState == ButtonState.Pressed)
                {
                    if (_lastFocused != null && control != _lastFocused)
                        _lastFocused.RemoveFocus();

                    control.SetFocused();

                    _lastFocused = control;
                }

                control?.OnMouseButton(arg);
            }

        }

        private void DoMouseMoveEvents(MouseEventArgs arg)
        {
            Control control = null;

            bool func(Control s)
            {
                if (s.IsEnabled && s.IsVisible)
                {
                    if (s.Rectangle.Contains(arg.Location.X, arg.Location.Y) || s.CanDragNow)
                    {
                        if (!s.MouseIsOver)
                            s.OnMouseEnter(arg);
                        return true;
                    }
                    else if (s.MouseIsOver)
                        s.OnMouseLeft(arg);
                }
                return false;
            }

            foreach (Control c in Controls)
                GetControl(c, func, ref control);

            if (control != null)
            {
                
                
                if (control.IsMovable && control.CanDragNow)
                {
                    control.MoveTo(

                         arg.Offset.X, 
                         arg.Offset.Y

                        );
                }

                control.OnMouseMove(arg);
            }
        }

        private void DoMouseWheelEvents(MouseWheelEventArgs arg)
        {
            Control control = null;

            bool func(Control s)
            {
                return s.IsEnabled && s.IsVisible && s.Rectangle.Contains(arg.Location.X, arg.Location.Y);
            }

            foreach (Control c in Controls)
                GetControl(c, func, ref control);

            control?.OnMouseWheel(arg);
        }


        private void DoKeyboardEvents(KeyboardEventArgs arg)
        {
            _lastFocused?.OnKeyboard(arg);
        }

        private void GetControl(Control parent, Func<Control, bool> condition, ref Control founded)
        {
            //for (int i = parent.Children.Count - 1; i >= 0; i--)
            for (int i = 0; i < parent.Children.Count; i++)
            {
                Control c = parent.Children[i];
                if (condition(c))
                {
                    founded = c;
                    GetControl(c, condition, ref founded);
                }
            }

            if (founded == null && condition(parent))
                founded = parent;
        }


    }
}
