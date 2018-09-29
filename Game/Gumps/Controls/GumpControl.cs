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
using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Input;
using ClassicUO.Interfaces;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SDL2;
using IUpdateable = ClassicUO.Interfaces.IUpdateable;

namespace ClassicUO.Game.Gumps
{
    public abstract class GumpControl : IDrawableUI, IUpdateable, IColorable
    {
        private readonly List<GumpControl> _children;
        private GumpControl _parent;
        private Rectangle _bounds;
        private Point _lastClickPosition;
        private float _maxTimeForDClick;
        private bool _acceptKeyboardInput, _acceptMouseInput;
        private int _activePage;
        private bool _handlesKeyboardFocus;



        protected GumpControl(GumpControl parent = null)
        {
            Parent = parent;
            _children = new List<GumpControl>();
            AllowedToDraw = true;
            AcceptMouseInput = true;

            Page = 0;
            UIManager = Service.Get<UIManager>();
        }


        //internal event Action<GumpControl, int, int, MouseButton> MouseDoubleClickEvent;
        public event EventHandler<MouseEventArgs> MouseDown,
            MouseUp,
            MouseMove,
            MouseEnter,
            MouseLeft,
            MouseClick,
            MouseDoubleClick;

        public event EventHandler<MouseWheelEventArgs> MouseWheel;
        public event EventHandler<KeyboardEventArgs> Keyboard;

        public Serial ServerSerial { get; set; }
        public Serial LocalSerial { get; set; }
        public bool AllowedToDraw { get; set; }
        public SpriteTexture Texture { get; set; }
        public Vector3 HueVector { get; set; }
        public int Page { get; set; }

        public Point Location
        {
            get => _bounds.Location;
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        public Rectangle Bounds
        {
            get => _bounds;
            set => _bounds = value;
        }

        public bool IsDisposed { get; private set; }
        public bool IsVisible { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsInitialized { get; set; }
        public bool IsFocused { get; protected set; }
        public bool MouseIsOver { get; protected set; }
        public virtual bool CanMove { get; set; }
        public bool CanCloseWithRightClick { get; set; } = true;
        public bool CanCloseWithEsc { get; set; }
        public bool IsEditable { get; set; }
        public bool IsTransparent { get; set; }
        public IReadOnlyList<GumpControl> Children => _children;

        public UIManager UIManager { get; }


        public virtual bool AcceptKeyboardInput
        {
            get
            {
                if (!IsEnabled || IsDisposed || !IsVisible)
                    return false;

                if (_acceptKeyboardInput)
                    return true;

                foreach (GumpControl c in _children)
                {
                    if (c.AcceptKeyboardInput)
                        return true;
                }

                return false;
            }
            set => _acceptKeyboardInput = value;
        }

        public virtual bool AcceptMouseInput
        {
            get => IsEnabled && !IsDisposed && _acceptMouseInput;
            set => _acceptMouseInput = value;
        }

        public int Width
        {
            get => _bounds.Width;
            set
            {
                if (_bounds.Width != value)
                {
                    _bounds.Width = value;
                    if (IsInitialized)
                        OnResize();
                }
            }
        }

        public int Height
        {
            get => _bounds.Height;
            set
            {
                if (_bounds.Height != value)
                {
                    _bounds.Height = value;
                    if (IsInitialized)
                        OnResize();
                }
            } 
        }

        public int X
        {
            get => _bounds.X;
            set
            {
                if (_bounds.X != value)
                {
                    _bounds.X = value;
                    if (IsInitialized)
                        OnMove();
                }
            }
        }

        public int Y
        {
            get => _bounds.Y;
            set
            {
                if (_bounds.Y != value)
                {
                    _bounds.Y = value;
                    if (IsInitialized)
                        OnMove();
                }
            }
        }

        public int ParentX => Parent != null ? Parent.X + Parent.ParentX : 0;
        public int ParentY => Parent != null ? Parent.Y + Parent.ParentY : 0;
        public int ScreenCoordinateX => ParentX + X;
        public int ScreenCoordinateY => ParentY + Y;

        public GumpControl Parent
        {
            get => _parent;
            private set
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

        private GumpControlInfo _controlInfo;

        public GumpControlInfo ControlInfo
        {
            get
            {
                if (_controlInfo == null)
                    _controlInfo = new GumpControlInfo(this);
                return _controlInfo;
            }
        }


        public void Initialize()
        {
            IsDisposed = false;
            IsEnabled = true;
            IsInitialized = true;
            IsVisible = true;
            InitializeControls();
            OnInitialize();
        }

        private void InitializeControls()
        {
            bool initializedKeyboardFocusedControl = false;

            foreach (GumpControl c in _children)
            {
                if (!c.IsInitialized)
                {
                    c.Initialize();

                    if (!initializedKeyboardFocusedControl && c.AcceptKeyboardInput)
                    {
                        Service.Get<UIManager>().KeyboardFocusControl = c;
                        initializedKeyboardFocusedControl = true;
                    }
                }
            }
        }

        public virtual void Update(double totalMS, double frameMS)
        {
            if (IsDisposed || !IsInitialized) return;

            if (Children.Count > 0)
            {
                InitializeControls();

                int w = 0, h = 0;

                List<GumpControl> toremove = new List<GumpControl>();

                foreach (GumpControl c in Children)
                {
                    c.Update(totalMS, frameMS);

                    if (c.IsDisposed)
                        toremove.Add(c);
                    else
                    {
                        if (c.Page == 0 || c.Page == ActivePage)
                        {
                            if (w < c.Bounds.Right)
                                w = c.Bounds.Right;
                            if (h < c.Bounds.Bottom)
                                h = c.Bounds.Bottom;
                        }
                    }
                }

                if (!IgnoreParentFill)
                {
                    if (w != Width)
                        Width = w;
                    if (h != Height)
                        Height = h;
                }


                if (toremove.Count > 0)
                    toremove.ForEach(s => _children.Remove(s));
            }
        }

        public bool IgnoreParentFill { get; set; }


        public virtual bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            if (IsDisposed) return false;

            if (Texture != null && !Texture.IsDisposed)
                Texture.Ticks = World.Ticks;


            foreach (GumpControl c in Children)
            {
                if (c.Page == 0 || c.Page == ActivePage)
                {
                    if (c.IsVisible && c.IsInitialized)
                    {
                        Vector3 offset = new Vector3(c.X + position.X, c.Y + position.Y, position.Z);
                        c.Draw(spriteBatch, offset, hue);
                    }
                }
            }

            return true;
        }

        //TODO: Future implementation

        //public virtual bool Draw(SpriteBatchUI spriteBatch, Rectangle dst, int offsetX, int offsetY, Vector3? hue = null)
        //{
        //    Rectangle src = new Rectangle();

        //    if (offsetX > Width || offsetX < -Width || offsetY > Height || offsetY < -Height)
        //        return false;

        //    src.X = offsetX;
        //    src.Y = offsetY;

        //    int maxX = src.X + dst.Width;
        //    if (maxX <= Width)
        //        src.Width = dst.Width;
        //    else
        //    {
        //        src.Width = Width - src.X;
        //        dst.Width = src.Width;
        //    }

        //    int maxY = src.Y + dst.Height;
        //    if (maxY <= Height)
        //        src.Height = dst.Height;
        //    else
        //    {
        //        src.Height = Height - src.Y;
        //        dst.Height = src.Height;
        //    }

        //    return true; /*spriteBatch.Draw2D(Texture, dst, src, hue ?? Vector3.Zero);*/
        //}



        internal void SetFocused()
        {
            IsFocused = true;
            OnFocusEnter();
        }

        internal void RemoveFocus()
        {
            IsFocused = false;
            OnFocusLeft();
        }


        public GumpControl[] HitTest(Point position)
        {
            List<GumpControl> results = new List<GumpControl>();

            bool inbouds = Bounds.Contains(position.X - ParentX, position.Y - ParentY);

            if (inbouds)
            {
                if (Contains(position.X - X - ParentX, position.Y - Y - ParentY))
                {
                    if (AcceptMouseInput)
                        results.Insert(0, this);

                    foreach (GumpControl c in Children)
                    {
                        if (c.Page == 0 || c.Page == ActivePage)
                        {
                            GumpControl[] cl = c.HitTest(position);
                            if (cl != null)
                            {
                                for (int i = cl.Length - 1; i >= 0; i--)
                                    results.Insert(0, cl[i]);
                            }
                        }
                    }
                }
            }

            return results.Count == 0 ? null : results.ToArray();
        }

        public GumpControl GetFirstControlAcceptKeyboardInput()
        {
            if (_acceptKeyboardInput)
                return this;
            if (_children == null || _children.Count == 0)
                return null;
            foreach (GumpControl c in _children)
            {
                if (c.AcceptKeyboardInput)
                    return c.GetFirstControlAcceptKeyboardInput();
            }

            return null;
        }


        public void AddChildren(GumpControl c, int page = 0)
        {
            c.Page = page;
            c.Parent = this;
            OnChildAdded();
        }

        public void RemoveChildren(GumpControl c)
        {
            c.Parent = null;
            OnChildRemoved();
        }

        public void Clear()
        {
            _children.ForEach(s => s.Dispose());
        }

        public T[] GetControls<T>() where T : GumpControl => Children.OfType<T>().ToArray();


        public void InvokeMouseDown(Point position, MouseButton button)
        {
            _lastClickPosition = position;
            int x = position.X - X - ParentX;
            int y = position.Y - Y - ParentY;
            OnMouseDown(x, y, button);
            MouseDown.Raise(new MouseEventArgs(x, y, button, ButtonState.Pressed));
        }

        public void InvokeMouseUp(Point position, MouseButton button)
        {
            _lastClickPosition = position;
            int x = position.X - X - ParentX;
            int y = position.Y - Y - ParentY;
            OnMouseUp(x, y, button);
            MouseUp.Raise(new MouseEventArgs(x, y, button, ButtonState.Released));
        }

        public void InvokeMouseEnter(Point position)
        {
            MouseIsOver = true;
            if (Math.Abs(_lastClickPosition.X - position.X) + Math.Abs(_lastClickPosition.Y - position.Y) > 3)
                _maxTimeForDClick = 0.0f;
            int x = position.X - X - ParentX;
            int y = position.Y - Y - ParentY;
            OnMouseEnter(x, y);
            MouseEnter.Raise(new MouseEventArgs(x, y));
        }

        public void InvokeMouseLeft(Point position)
        {
            MouseIsOver = false;
            int x = position.X - X - ParentX;
            int y = position.Y - Y - ParentY;
            OnMouseLeft(x, y);
            MouseLeft.Raise(new MouseEventArgs(x, y));
        }

        public void InvokeMouseClick(Point position, MouseButton button)
        {
            int x = position.X - X - ParentX;
            int y = position.Y - Y - ParentY;
            float ms = World.Ticks;

            bool doubleClick = false;

            if (_maxTimeForDClick != 0f)
            {
                if (ms <= _maxTimeForDClick)
                {
                    _maxTimeForDClick = 0;
                    doubleClick = true;
                }
            }
            else
                _maxTimeForDClick = ms + 200;

            if (button == MouseButton.Right)
            {
                OnMouseClick(x, y, button);
                MouseClick.Raise(new MouseEventArgs(x, y, button, ButtonState.Pressed));

                if (CanCloseWithRightClick)
                    CloseWithRightClick();
            }
            else
            {
                if (doubleClick)
                {
                    OnMouseDoubleClick(x, y, button);
                    MouseDoubleClick.Raise(new MouseEventArgs(x, y, button, ButtonState.Pressed));
                }
                else
                {
                    OnMouseClick(x, y, button);
                    MouseClick.Raise(new MouseEventArgs(x, y, button, ButtonState.Pressed));
                }
            }
        }

        public void InvokeTextInput(string c)
        {
            OnTextInput(c);
        }

        public void InvokeKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            OnKeyDown(key, mod);
        }

        public void InvokeKeyUp(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            OnKeyUp(key, mod);
        }

        public void InvokeMouseWheel(MouseEvent delta)
        {
            OnMouseWheel(delta);
            MouseWheel.Raise(new MouseWheelEventArgs(delta));
        }


        protected virtual void OnMouseDown(int x, int y, MouseButton button)
        {
            Parent?.OnMouseDown(x, y, button);
        }

        protected virtual void OnMouseUp(int x, int y, MouseButton button)
        {
            Parent?.OnMouseUp(x, y, button);
        }

        protected virtual void OnMouseWheel(MouseEvent delta)
        {
            Parent?.OnMouseWheel(delta);
        }

        protected virtual void OnMouseEnter(int x, int y)
        {
        }

        protected virtual void OnMouseLeft(int x, int y)
        {
        }

        protected virtual void OnMouseClick(int x, int y, MouseButton button)
        {
            Parent?.OnMouseClick(x, y, button);
        }

        protected virtual void OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            Parent?.OnMouseDoubleClick(x, y, button);
        }

        protected virtual void OnTextInput(string c)
        {
        }

        protected virtual void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
        }

        protected virtual void OnKeyUp(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
        }

        protected virtual bool Contains(int x, int y) => true;


        protected virtual void OnMove()
        {

        }

        protected virtual void OnResize()
        {
            
        }

        protected virtual void OnInitialize()
        {

        }

        protected virtual void OnClosing()
        {

        }

        protected virtual void OnFocusEnter()
        {

        }

        protected virtual void OnFocusLeft()
        {

        }

        protected virtual void OnChildAdded()
        {

        }

        protected virtual void OnChildRemoved()
        {

        }

        protected virtual void CloseWithRightClick()
        {
            if (!CanCloseWithRightClick)
                return;

            GumpControl parent = Parent;

            while (parent != null)
            {
                if (!parent.CanCloseWithRightClick)
                    return;
                parent = parent.Parent;
            }

            if (Parent == null)
                Dispose();
            else
                Parent.CloseWithRightClick();
        }

        public virtual void OnButtonClick(int buttonID)
        {
            if (Parent != null)
                Parent.OnButtonClick(buttonID);
        }

        public virtual void OnKeybaordReturn(int textID, string text)
        {
            if (Parent != null)
                Parent.OnKeybaordReturn(textID, text);
        }

        public virtual void ChangePage(int pageIndex)
        {
            if (Parent != null)
                Parent.ChangePage(pageIndex);
        }

        public virtual bool HandlesKeyboardFocus
        {
            get
            {
                if (!IsEnabled || !IsInitialized || IsDisposed || !IsVisible)
                    return false;

                if (_handlesKeyboardFocus)
                    return true;

                if (_children == null)
                    return false;

                foreach (GumpControl c in _children)
                {
                    if (c.HandlesKeyboardFocus)
                        return true;
                }

                return false;
            }
            set => _handlesKeyboardFocus = value;
        }

        public int ActivePage
        {
            get => _activePage;
            set
            {
                UIManager _uiManager = Service.Get<UIManager>();
                _activePage = value;

                if (_uiManager.KeyboardFocusControl != null)
                {
                    if (Children.Contains(_uiManager.KeyboardFocusControl))
                    {
                        if (_uiManager.KeyboardFocusControl.Page != 0)
                            _uiManager.KeyboardFocusControl = null;
                    }
                }

                // When ActivePage changes, check to see if there are new text input boxes
                // that we should redirect text input to.
                if (_uiManager.KeyboardFocusControl == null)
                {
                    foreach (GumpControl c in Children)
                    {
                        if (c.HandlesKeyboardFocus && c.Page == _activePage)
                        {
                            _uiManager.KeyboardFocusControl = c;
                            break;
                        }
                    }
                }
            }
        }

        public virtual void Dispose()
        {
            if (IsDisposed)
                return;

            for (int i = 0; i < Children.Count; i++)
            {
                GumpControl c = Children[i];
                c.Dispose();
            }

            IsDisposed = true;
        }
    }
}