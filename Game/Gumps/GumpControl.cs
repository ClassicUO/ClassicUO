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
using ClassicUO.Input;
using ClassicUO.Interfaces;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
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
            //IsEnabled = true;
            //IsInitialized = true;
            //IsVisible = true;
            AllowedToDraw = true;

            AcceptMouseInput = true;

            Page = 0;
            UIManager = Service.Get<UIManager>();
        }


        //internal event Action<GumpControl, int, int, MouseButton> MouseDoubleClickEvent;
        public event EventHandler<MouseEventArgs> MouseDown, MouseUp, MouseMove, MouseEnter, MouseLeft, MouseClick, MouseDoubleClick;
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
        public bool IsInitialized { get; set; }
        public bool IsFocused { get; protected set; }
        public bool MouseIsOver { get; protected set; }
        public virtual bool CanMove { get; set; }
        public bool CanCloseWithRightClick { get; set; } = true;
        public bool CanCloseWithEsc { get; set; }
        public bool IsEditable { get; set; }
        public bool IsTransparent { get; set; }
        public IReadOnlyList<GumpControl> Children => _children;

        public UIManager UIManager { get; private set; }



        public virtual bool AcceptKeyboardInput
        {
            get
            {
                if (!IsEnabled || IsDisposed || !IsVisible)
                    return false;

                if (_acceptKeyboardInput)
                    return true;

                foreach (var c in _children)
                    if (c.AcceptKeyboardInput)
                        return true;

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
            set
            {
                if (_bounds.X != value)
                {
                    _bounds.X = value;
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
                    OnMove();
                }
            }
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

            foreach (var c in _children)
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
            if (IsDisposed || !IsInitialized)
            {
                return;
            }

            if (Children.Count > 0)
            {
                int w = 0, h = 0;

                foreach (GumpControl c in Children)
                {
                    c.Update(totalMS, frameMS);

                    if (c.Page == 0 || c.Page == ActivePage)
                    {
                        if (w < c.Bounds.Right)
                            w = c.Bounds.Right;
                        if (h < c.Bounds.Bottom)
                            h = c.Bounds.Bottom;
                    }
                }

                if (w != Width)
                    Width = w;
                if (h != Height)
                    Height = h;
            }
        }

        public virtual bool Draw(SpriteBatchUI spriteBatch, Vector3 position)
        {
            if (IsDisposed)
            {
                return false;
            }

            if (Texture != null && !Texture.IsDisposed)
                Texture.Ticks = World.Ticks;


            foreach (GumpControl c in Children)
            {
                if (c.Page == 0 || c.Page == ActivePage)
                {
                    if (c.IsVisible && c.IsInitialized)
                    {
                        Vector3 offset = new Vector3(c.X + position.X, c.Y + position.Y, position.Z);
                        c.Draw(spriteBatch, offset);
                    }
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
                if (AcceptMouseInput)
                    results.Insert(0, this);

                foreach (var c in Children)
                {
                    if (c.Page == 0 || c.Page == ActivePage)
                    {
                        var cl = c.HitTest(position);
                        if (cl != null)
                        {
                            for (int i = cl.Length - 1; i >= 0; i--)
                                results.Insert(0, cl[i]);
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
            foreach (var c in _children)
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
        }

        public void RemoveChildren(GumpControl c) => c.Parent = null;

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
            MouseDown.Raise(new MouseEventArgs(x, y, button, Microsoft.Xna.Framework.Input.ButtonState.Pressed));
        }

        public void InvokeMouseUp(Point position, MouseButton button)
        {
            _lastClickPosition = position;
            int x = position.X - X - ParentX;
            int y = position.Y - Y - ParentY;
            OnMouseUp(x, y, button);
            MouseUp.Raise(new MouseEventArgs(x, y, button, Microsoft.Xna.Framework.Input.ButtonState.Released));
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
                MouseClick.Raise(new MouseEventArgs(x, y, button, Microsoft.Xna.Framework.Input.ButtonState.Pressed));

                if (CanCloseWithRightClick)
                    CloseWithRightClick();
            }
            else
            {
                if (doubleClick)
                {
                    OnMouseDoubleClick(x, y, button);
                    MouseDoubleClick.Raise(new MouseEventArgs(x, y, button, Microsoft.Xna.Framework.Input.ButtonState.Pressed));
                }
                else
                {
                    OnMouseClick(x, y, button);
                    MouseClick.Raise(new MouseEventArgs(x, y, button, Microsoft.Xna.Framework.Input.ButtonState.Pressed));
                }
            }
        }

        public void InvokeTextInput(string c)
        {
            OnTextInput(c);
        }

        public void InvokeKeyDown(SDL2.SDL.SDL_Keycode key, SDL2.SDL.SDL_Keymod mod)
        {
            OnKeyDown(key, mod);
        }

        public void InvokeKeyUp(SDL2.SDL.SDL_Keycode key, SDL2.SDL.SDL_Keymod mod)
        {
            OnKeyUp(key, mod);
        }



        protected virtual void OnMouseDown(int x, int y, MouseButton button)
        {
            if (Parent != null)
                Parent.OnMouseDown(x, y, button);
        }

        protected virtual void OnMouseUp(int x, int y, MouseButton button)
        {
            if (Parent != null)
                Parent.OnMouseUp(x, y, button);
        }

        protected virtual void OnMouseEnter(int x, int y)
        {

        }

        protected virtual void OnMouseLeft(int x, int y)
        {

        }

        protected virtual void OnMouseClick(int x, int y, MouseButton button)
        {
            if (Parent != null)
                Parent.OnMouseClick(x, y, button);
        }

        protected virtual void OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            if (Parent != null)
                Parent.OnMouseDoubleClick(x, y, button);
        }

        protected virtual void OnTextInput(string c)
        {

        }

        protected virtual void OnKeyDown(SDL2.SDL.SDL_Keycode key, SDL2.SDL.SDL_Keymod mod)
        {

        }

        protected virtual void OnKeyUp(SDL2.SDL.SDL_Keycode key, SDL2.SDL.SDL_Keymod mod)
        {

        }

        protected virtual bool Contains(int x, int y)
        {
            return true;
        }

        protected virtual void OnMove()
        {

        }

        protected virtual void OnInitialize()
        {

        }

        protected virtual void OnClosing()
        {

        }

        protected virtual void CloseWithRightClick()
        {
            if (!CanCloseWithRightClick)
                return;

            var parent = Parent;

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
                    if (c.HandlesKeyboardFocus)
                        return true;

                return false;
            }
            set
            {
                _handlesKeyboardFocus = value;
            }
        }

        public int ActivePage
        {

            get { return _activePage; }
            set
            {
                var _uiManager = Service.Get<UIManager>();
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
                        if (c.HandlesKeyboardFocus && (c.Page == _activePage))
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
                var c = Children[i];
                c.Dispose();
            }

            IsDisposed = true;
        }

    }
}