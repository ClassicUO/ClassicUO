using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Input;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer.UI
{
    class UISystemManager : DrawableGameComponent
    {
        private readonly SpriteBatch _spriteBatch;

        private readonly List<UIControl> _controls = new List<UIControl>();
        private UIControl _currentControl;

        public UISystemManager(Microsoft.Xna.Framework.Game game) : base(game)
        {
            _spriteBatch = new SpriteBatch(game.GraphicsDevice);


            Add(new Button2("I'm a button")
            {
                X = 200,
                Y = 200,
                Width = 200,
                Height = 200,
                Texture = Textures.GetTexture(Color.White)
            });
        }


        public override void Initialize()
        {
            Engine.Input.LeftMouseButtonDown += InputOnLeftMouseButtonDown;
            Engine.Input.LeftMouseButtonUp += InputOnLeftMouseButtonUp;

            Engine.Input.RightMouseButtonDown += InputOnRightMouseButtonDown;
            Engine.Input.RightMouseButtonUp += InputOnRightMouseButtonUp;

            Engine.Input.MouseWheel += InputOnMouseWheel;

            Engine.Input.MouseDragging += InputOnMouseDragging;


            base.Initialize();
        }



        public void Add(UIControl control)
        {
            _controls.Insert(0, control);
        }

        public void Remove(UIControl control)
        {
            _controls.Remove(control);
        }


        private void InputOnMouseDragging(object sender, EventArgs e)
        {
        }

        private void InputOnMouseWheel(object sender, bool e)
        {
        }

        private void InputOnRightMouseButtonUp(object sender, EventArgs e)
        {
        }

        private void InputOnRightMouseButtonDown(object sender, EventArgs e)
        {
        }

        private void InputOnLeftMouseButtonUp(object sender, EventArgs e)
        {
            if (_currentControl == null)
                return;

            (int x, int y) = GetContolCoordinates(_currentControl);
            _currentControl.OnMouseUp(x, y, MouseButton.Left);
        }

        private void InputOnLeftMouseButtonDown(object sender, EventArgs e)
        {
            if (_currentControl == null)
                return;

            (int x, int y) = GetContolCoordinates(_currentControl);
            _currentControl.OnMouseDown(x, y, MouseButton.Left);
        }



        public override void Update(GameTime gameTime)
        {
            CheckMouseOverUIControl();

            for (int i = 0; i < _controls.Count; i++)
            {
                _controls[i].Update(gameTime);
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            _spriteBatch.Begin();

            for (int i = _controls.Count - 1; i >= 0 ; i--)
            {
                var c = _controls[i];

                if (c.IsVisible)
                {
                    c.Draw(_spriteBatch, c.X, c.Y);
                }
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }


        private (int, int) GetContolCoordinates(UIControl control)
        {
            Point mp = Mouse.Position;

            int x = mp.X - control.X - control.GetParentX();
            int y = mp.Y - control.Y - control.GetParentY();

            return (x, y);
        }

        private void CheckMouseOverUIControl()
        {
            Point mousePosition = Mouse.Position;

            _currentControl = null;

            for (int i = 0; i < _controls.Count; i++)
            {
                var control = _controls[i];

                UIControl[] list = control.HitTest(mousePosition.X, mousePosition.Y);

                if (list != null && list.Length != 0)
                {
                    _currentControl = list[0];

                    break;
                }
            }
        }

    }
}
