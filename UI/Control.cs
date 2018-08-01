using System;
using System.Collections.Generic;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.UI
{
    public abstract class Control
    {
        private readonly List<Control> _children;
        private Control _parent;
        private Rectangle _rectangle;

        protected Control(Control parent = null)
        {
            Parent = parent;
            _children = new List<Control>();
            IsEnabled = true;
            IsVisible = true;
        }

        protected Control(Control parent, int x, int y) : this(parent)
        {
            X = x;
            Y = y;
        }

        protected Control(Control parent, int x, int y, int width, int height) : this(parent, x, y)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public Point Location
        {
            get => _rectangle.Location;
            set => _rectangle.Location = value;
        }

        public Rectangle Rectangle
        {
            get => _rectangle;
            set => _rectangle = value;
        }

        public bool IsVisible { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsFocused { get; protected set; }
        public bool MouseIsOver { get; protected set; }
        public bool IsMovable { get; set; }
        public IReadOnlyList<Control> Children => _children;

        internal bool CanDragNow { get; set; }

        public int Width
        {
            get => _rectangle.Width;
            set => _rectangle.Width = value;
        }

        public int Height
        {
            get => _rectangle.Height;
            set => _rectangle.Height = value;
        }

        public int X
        {
            get => _rectangle.X;
            set => _rectangle.X = value;
        }

        public int Y
        {
            get => _rectangle.Y;
            set => _rectangle.Y = value;
        }

        public Control Parent
        {
            get => _parent;
            set
            {
                if (value == null)
                    _parent?.RemoveChildren(this);
                else
                    value?.AddChildren(this);

                _parent = value;
            }
        }


        internal void SetFocused()
        {
            IsFocused = true;
        }

        internal void RemoveFocus()
        {
            IsFocused = false;
        }


        public event EventHandler<MouseEventArgs> MouseButton, MouseMove, MouseEnter, MouseLeft;
        public event EventHandler<MouseWheelEventArgs> MouseWheel;
        public event EventHandler<KeyboardEventArgs> Keyboard;


        public void AddChildren(Control c)
        {
            c.Parent = this;
            _children.Add(c);
        }

        public void RemoveChildren(Control c)
        {
            c.Parent = null;
            _children.Remove(c);
        }

        public void Clear()
        {
            _children.ForEach(s => s.Parent = null);
            _children.Clear();
        }

        internal virtual void Draw(GameTime time, SpriteBatch spriteBatch)
        {
            _children.ForEach(s => s.Draw(time, spriteBatch));
        }

        internal virtual void Update(GameTime time)
        {
            _children.ForEach(s => s.Update(time));
        }

        public void MoveTo(int offsetX, int offsetY)
        {
            if (IsMovable)
            {
                Console.WriteLine("OFFSET: {0},{1}", offsetX, offsetY);

                if (Parent != null)
                    if (X + offsetX > Parent.Width || Y + offsetY > Parent.Height || X + offsetX < Parent.X || Y + offsetY < Parent.Y)
                        return;


                X += offsetX;
                Y += offsetY;

                foreach (Control c in Children)
                    c.MoveTo(offsetX, offsetY);
            }
        }


        public virtual void OnMouseButton(MouseEventArgs e)
        {
            MouseButton?.Invoke(this, e);
        }

        public virtual void OnMouseEnter(MouseEventArgs e)
        {
            MouseIsOver = true;
            MouseEnter?.Invoke(this, e);
        }

        public virtual void OnMouseLeft(MouseEventArgs e)
        {
            MouseIsOver = false;
            MouseLeft?.Invoke(this, e);
        }

        public virtual void OnMouseMove(MouseEventArgs e)
        {
            MouseMove?.Invoke(this, e);
        }

        public virtual void OnMouseWheel(MouseWheelEventArgs e)
        {
            MouseWheel?.Invoke(this, e);
        }

        public virtual void OnKeyboard(KeyboardEventArgs e)
        {
            Keyboard?.Invoke(this, e);
        }
    }
}