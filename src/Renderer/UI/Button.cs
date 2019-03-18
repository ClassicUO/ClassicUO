using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer.UI
{
    class Button : Control
    {
        private enum StateType
        {
            Normal,
            Over,
            Pressed
        }

        private StateType _state;
        private Vector2 _textSize;
        private string _text;

        public Button(int x, int y, int w, int h, string text) : this(text)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
        }

        public Button(string text)
        {
            Text = text;

            WantUpdateSize = false;
            CanCloseWithRightClick = false;
        }

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                _textSize = Fonts.Regular.MeasureString(_text);
            }
        }


        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            _state = StateType.Normal;
            base.OnMouseUp(x, y, button);
        }

        protected override void OnMouseDown(int x, int y, MouseButton button)
        {
            _state = StateType.Pressed;
            base.OnMouseDown(x, y, button);
        }

        protected override void OnMouseOver(int x, int y)
        {
            if (_state != StateType.Pressed)
                _state = StateType.Over;
            base.OnMouseOver(x, y);
        }

        protected override void OnMouseExit(int x, int y)
        {
            if (_state != StateType.Pressed)
                _state = StateType.Normal;
            base.OnMouseExit(x, y);
        }

        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {          
            Texture2D texture;
            switch (_state)
            {
                default:
                case StateType.Normal:
                    Color color = new Color(49, 49, 49);
                    texture = Textures.GetTexture(color);
                    break;
                case StateType.Over:
                    color = new Color(104, 44, 44);
                    texture = Textures.GetTexture(color);
                    break;
                case StateType.Pressed:
                    color = new Color(89, 59, 59);
                    texture = Textures.GetTexture(color);
                    break;
            }

            Rectangle rect = new Rectangle(position.X, position.Y, Width, Height);
            batcher.Draw2D(texture, rect, Vector3.Zero);

            batcher.DrawString(Fonts.Regular, Text, position.X - ((int)_textSize.X - Width) / 2, position.Y - ((int)_textSize.Y - Height) / 2, Vector3.Zero);

            return true; // base.Draw(batcher, position, hue);
        }
    }
}
