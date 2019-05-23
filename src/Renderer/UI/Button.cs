using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer.UI
{
    class Button2 : UIControl
    {
        private Rectangle _rect;

        public Button2(string caption)
        {
            Text = caption;
            Width = 100;
            Height = 50;
        }

        public string Text { get; set; }


        public override void Draw(SpriteBatch batcher, int x, int y)
        {
            _rect.X = x;
            _rect.Y = y;
            _rect.Width = Width;
            _rect.Height = Height;

           //batcher.Draw(Texture, x, y, Width, Height, Color.Red);

            batcher.Draw(Texture, _rect, Color.Red);
        }



        public override void OnMouseEnter(int x, int y)
        {
        }

        public override void OnMouseExit(int x, int y)
        {
        }

        public override void OnMouseDown(int x, int y, MouseButton button)
        {
        }

        public override void OnMouseUp(int x, int y, MouseButton button)
        {
        }
    }

    internal class Button : Control
    {
        private bool _adaptSizeToText;

        private StateType _state;
        private string _text;
        private Vector2 _textSize;

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

        public bool AdaptSizeToText
        {
            get => _adaptSizeToText;
            set
            {
                _adaptSizeToText = value;

                if (_textSize != Vector2.Zero)
                {
                    Width = (int) (_textSize.X + 4);
                    Height = (int) (_textSize.Y + 4);
                }
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                _textSize = Fonts.Regular.MeasureString(_text);

                if (AdaptSizeToText)
                {
                    Width = (int) (_textSize.X + 4);
                    Height = (int) (_textSize.Y + 4);
                }
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

        private static Vector3 _zero = Vector3.Zero;

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
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

            batcher.Draw2D(texture, x, y, Width, Height, ref _zero);

            batcher.DrawString(Fonts.Regular, Text, x - (((int) _textSize.X - Width) >> 1), y - (((int) _textSize.Y - Height) >> 1), ref _zero);

            return true; // base.Draw(batcher, position, hue);
        }

        private enum StateType
        {
            Normal,
            Over,
            Pressed
        }
    }
}