#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
//    
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using ClassicUO.Game.GameObjects.Interfaces;
using ClassicUO.Game.Renderer;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using IUpdateable = ClassicUO.Game.GameObjects.Interfaces.IUpdateable;

namespace ClassicUO.Game.Gumps
{
    public class GumpControl : IDrawableUI, IUpdateable
    {
        private readonly List<GumpControl> _children;
        private GumpControl _parent;
        private Rectangle _bounds;

        public GumpControl(GumpControl parent = null)
        {
            Parent = parent;
            _children = new List<GumpControl>();
            IsEnabled = true;
            IsVisible = true;
            AllowedToDraw = true;
        }


        public event EventHandler<MouseEventArgs> MouseButton, MouseMove, MouseEnter, MouseLeft;
        public event EventHandler<MouseWheelEventArgs> MouseWheel;
        public event EventHandler<KeyboardEventArgs> Keyboard;


        public bool AllowedToDraw { get; set; }
        public SpriteTexture Texture { get; set; }
        public Vector3 HueVector { get; set; }
        public Serial ServerSerial { get; set; }
        public Serial LocalSerial { get; set; }
        public Point Location
        {
            get => _bounds.Location;
            set { X = value.X; Y = value.Y; }
        }

        public Rectangle Bounds
        {
            get => _bounds;
            set => _bounds = value;
        }

        public bool IsDisposed { get; private set; }
        public bool IsVisible { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsFocused { get; protected set; }
        public bool MouseIsOver { get; protected set; }
        public bool CanMove { get; set; }
        public bool CanCloseWithRightClick { get; set; }
        public bool CanCloseWithEsc { get; set; }
        public bool IsEditable { get; set; }
        public IReadOnlyList<GumpControl> Children => _children;
        internal bool CanDragNow { get; set; }

        public int Width
        {
            get => _bounds.Width;
            set => _bounds.Width = value;
        }

        public int Height
        {
            get => _bounds.Height;
            set => _bounds.Height = value;
        }

        public int X
        {
            get => _bounds.X;
            set => _bounds.X = value;
        }

        public int Y
        {
            get => _bounds.Y;
            set => _bounds.Y = value;
        }

        public int ParentX => Parent != null ? Parent.X + Parent.ParentX : 0;
        public int ParentY => Parent != null ? Parent.Y + Parent.ParentY : 0;

        public GumpControl Parent
        {
            get => _parent;
            set
            {
                if (value == null)
                    _parent?._children.Remove(this);
                else
                    value._children.Add(this);

                _parent = value;

            }
        }

        public GumpControl RootParent
        {
            get
            {
                GumpControl p = this;
                while (p.Parent != null)
                    p = p.Parent;
                return p;
            }
        }

        public virtual void Update(double frameMS)
        {
            if (IsDisposed)
            {
                return;
            }

            if (Children.Count > 0)
            {
                int w = 0, h = 0;

                foreach (GumpControl c in Children)
                {
                    c.Update(frameMS);

                    if (w < c.Bounds.Right)
                        w = c.Bounds.Right;
                    if (h < c.Bounds.Bottom)
                        h = c.Bounds.Bottom;
                }

                if (w != Width)
                    Width = w;
                if (h != Height)
                    Height = h;
            }
        }

        public virtual bool Draw(SpriteBatchUI spriteBatch,  Vector3 position)
        {
            if (IsDisposed || ((Texture == null || Texture.IsDisposed) && Children.Count <= 0))
            {
                return false;
            }

            if (Texture != null)
                Texture.Ticks = World.Ticks;


            foreach (GumpControl c in Children)
            {
                if (c.IsVisible)
                {
                    Vector3 offset = new Vector3(c.X + position.X, c.Y + position.Y, position.Z);
                    c.Draw(spriteBatch, offset);
                }
            }

            return true;
        }



        internal void SetFocused()
        {
            IsFocused = true;
        }

        internal void RemoveFocus()
        {
            IsFocused = false;
        }



        public GumpControl[] HitTest(Point position)
        {
            List<GumpControl> results = new List<GumpControl>();

            bool inbouds = Bounds.Contains(position.X - ParentX, position.Y - ParentY);

            if (inbouds)
            {
                results.Insert(0, this);
                foreach (var c in Children)
                {
                    var cl = c.HitTest(position);
                    if (cl != null)
                    {
                        for (int i = cl.Length - 1; i >= 0; i--)
                            results.Insert(0, cl[i]);
                    }
                }
            }

            return results.Count == 0 ? null : results.ToArray();
        }


        public void AddChildren(GumpControl c)
        {
            c.Parent = this;
        }

        public void RemoveChildren(GumpControl c)
        {
            c.Parent = null;
        }

        public void Clear()
        {
            _children.ForEach(s => s.Parent = null);
            _children.Clear();
        }

        public T[] GetControls<T>() where T : GumpControl => Children.OfType<T>().ToArray();


        public virtual void OnMouseButton(MouseEventArgs e)
        {        
            if (e.Button == MouseButtons.Right && e.ButtonState == Microsoft.Xna.Framework.Input.ButtonState.Released && RootParent.CanCloseWithRightClick)
            {
                RootParent.Dispose();
            }
            else
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

        public virtual void Dispose()
        {
            if (IsDisposed)
                return;

            for (int i = 0; i < Children.Count; i++)
            {
                var c = Children[i];
                c.Dispose();
            }

            IsDisposed = true;
        }

    }
}