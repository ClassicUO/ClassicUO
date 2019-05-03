﻿using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer.UI
{
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

        public override bool Draw(Batcher2D batcher, int x, int y)
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

            batcher.Draw2D(texture, x, y, Width, Height, Vector3.Zero);

            batcher.DrawString(Fonts.Regular, Text, x - (((int) _textSize.X - Width) >> 1), y - (((int) _textSize.Y - Height) >> 1), Vector3.Zero);

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