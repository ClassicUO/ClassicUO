#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using SDL2;

using IUpdateable = ClassicUO.Interfaces.IUpdateable;
using Mouse = ClassicUO.Input.Mouse;

namespace ClassicUO.Game.Gumps.Controls
{
    public enum ClickPriority
    {
        High,
        Default,
        Low
    }

    public abstract class GumpControl : IDrawableUI, IUpdateable, IColorable, IDebuggable
    {
        private static SpriteTexture _debugTexture;
        private readonly List<GumpControl> _children;
        private bool _acceptKeyboardInput, _acceptMouseInput, _mouseIsDown;
        private int _activePage;
        private bool _attempToDrag;
        private Rectangle _bounds;
        private GumpControlInfo _controlInfo;
        private bool _handlesKeyboardFocus;
        private Point _lastClickPosition;
        private GumpControl _parent;

        protected GumpControl(GumpControl parent = null)
        {
            Parent = parent;
            _children = new List<GumpControl>();
            AllowedToDraw = true;
            AcceptMouseInput = true;
            Page = 0;
            UIManager = Service.Get<UIManager>();
            Debug = false;
        }

        protected virtual ClickPriority Priority => ClickPriority.Default;

        public Serial ServerSerial { get; set; }

        public Serial LocalSerial { get; set; }

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

        public bool IsVisible { get; set; } = true;

        public bool IsEnabled { get; set; }

        public bool IsInitialized { get; set; }

        public bool IsFocused { get; protected set; }

        public bool MouseIsOver => UIManager?.MouseOverControl == this;

        public virtual bool CanMove { get; set; }

        public bool CanCloseWithRightClick { get; set; } = true;

        public bool CanCloseWithEsc { get; set; }

        public bool IsEditable { get; set; }

        public bool IsTransparent { get; set; }

        public float Alpha { get; set; } = .5f;

        public IReadOnlyList<GumpControl> Children => _children;

        public UIManager UIManager { get; }

        public object Tag { get; set; }

        public string Tooltip { get; private set; }

        public bool HasTooltip => World.ClientFeatures.TooltipsEnabled && !string.IsNullOrEmpty(Tooltip);

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
                if (_bounds.Width != value) _bounds.Width = value;
            }
        }

        public int Height
        {
            get => _bounds.Height;
            set
            {
                if (_bounds.Height != value) _bounds.Height = value;
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
                if (Parent == null)
                    return null;
                GumpControl p = Parent;

                while (p.Parent != null)
                    p = p.Parent;

                return p;
            }
        }

        public GumpControlInfo ControlInfo => _controlInfo ?? (_controlInfo = new GumpControlInfo(this));

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

        public bool WantUpdateSize { get; set; } = true;

        public Vector3 HueVector { get; set; }

        public bool Debug { get; set; }

        public bool AllowedToDraw { get; set; }

        public SpriteTexture Texture { get; set; }

        public virtual bool Draw(SpriteBatchUI spriteBatch, Point position, Vector3? hue = null)
        {
            if (IsDisposed) return false;

            if (Texture != null && !Texture.IsDisposed)
                Texture.Ticks = CoreGame.Ticks;

            foreach (GumpControl c in Children)
            {
                if (c.Page == 0 || c.Page == ActivePage)
                {
                    if (c.IsVisible && c.IsInitialized)
                    {
                        Point offset = new Point(c.X + position.X, c.Y + position.Y);
                        c.Draw(spriteBatch, offset, hue);
                    }
                }
            }

            if (IsVisible && Debug)
            {
                if (_debugTexture == null)
                {
                    _debugTexture = new SpriteTexture(1, 1);

                    _debugTexture.SetData(new Color[1]
                    {
                        Color.Green
                    });
                }

                spriteBatch.DrawRectangle(_debugTexture, new Rectangle(position.X, position.Y, Width, Height), Vector3.Zero);
            }

            return true;
        }

        public virtual void Update(double totalMS, double frameMS)
        {
            if (IsDisposed || !IsInitialized) return;

            if (Children.Count > 0)
            {
                InitializeControls();
                int w = 0, h = 0;
                //List<GumpControl> toremove = new List<GumpControl>();

                for (int i = 0; i < _children.Count; i++)
                {
                    GumpControl c = _children[i];
                    c.Update(totalMS, frameMS);

                    if (c.IsDisposed)
                    {
                        //toremove.Add(c);
                        _children.RemoveAt(i--);
                    }
                    else
                    {
                        if (WantUpdateSize)
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
                }

                if (WantUpdateSize)
                {
                    if (w != Width)
                        Width = w;

                    if (h != Height)
                        Height = h;
                    WantUpdateSize = false;
                }

                //if (toremove.Count > 0)
                //    toremove.ForEach(s => _children.Remove(s));
            }
        }

        public void SetTooltip(string c)
        {
            if (string.IsNullOrEmpty(c))
                ClearTooltip();
            else
                Tooltip = c;
        }

        public void ClearTooltip()
        {
            Tooltip = null;
        }

        public event EventHandler<MouseEventArgs> MouseDown, MouseUp, MouseMove, MouseOver, MouseEnter, MouseExit, MouseClick, DragBegin, DragEnd;

        public event EventHandler<MouseWheelEventArgs> MouseWheel;

        public event EventHandler<KeyboardEventArgs> Keyboard;

        public event EventHandler<MouseDoubleClickEventArgs> MouseDoubleClick;

        public void Initialize()
        {
            IsDisposed = false;
            IsEnabled = true;
            IsInitialized = true;
            InitializeControls();
            OnInitialize();
        }

        private void InitializeControls()
        {
            bool initializedKeyboardFocusedControl = false;

            for (int i = 0; i < _children.Count; i++)
            {
                GumpControl c = _children[i];

                if (!c.IsInitialized)
                {
                    c.Initialize();

                    if (!initializedKeyboardFocusedControl && c.AcceptKeyboardInput)
                    {
                        UIManager.KeyboardFocusControl = c;
                        initializedKeyboardFocusedControl = true;
                    }
                }
            }
        }

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

                    for (int j = 0; j < Children.Count; j++)
                    {
                        GumpControl c = Children[j];

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

            return results.Count == 0 ? null : results.OrderBy(s => s.Priority).ToArray();
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

        public virtual void AddChildren(GumpControl c, int page = 0)
        {
            c.Page = page;
            c.Parent = this;
            OnChildAdded();
        }

        public virtual void RemoveChildren(GumpControl c)
        {
            if (c == null)
                return;
            c.Parent = null;
            _children.Remove(c);
            OnChildRemoved();
        }

        public virtual void Clear()
        {
            for (int i = 0; i < Children.Count; i++)
                Children[i].Dispose();
        }

        public T[] GetControls<T>() where T : GumpControl
        {
            return Children.OfType<T>().ToArray();
        }

        public IEnumerable<T> FindControls<T>() where T : GumpControl
        {
            return Children.OfType<T>();
        }

        public void InvokeMouseDown(Point position, MouseButton button)
        {
            _lastClickPosition = position;
            int x = position.X - X - ParentX;
            int y = position.Y - Y - ParentY;
            OnMouseDown(x, y, button);
            MouseDown.Raise(new MouseEventArgs(x, y, button, ButtonState.Pressed), this);
        }

        public void InvokeMouseUp(Point position, MouseButton button)
        {
            _lastClickPosition = position;
            int x = position.X - X - ParentX;
            int y = position.Y - Y - ParentY;
            OnMouseUp(x, y, button);
            MouseUp.Raise(new MouseEventArgs(x, y, button, ButtonState.Released), this);
        }

        public void InvokeMouseOver(Point position)
        {
            int x = position.X - X - ParentX;
            int y = position.Y - Y - ParentY;
            OnMouseOver(x, y);
            MouseOver.Raise(new MouseEventArgs(x, y), this);
        }

        public void InvokeMouseEnter(Point position)
        {
            int x = position.X - X - ParentX;
            int y = position.Y - Y - ParentY;
            OnMouseEnter(x, y);
            MouseEnter.Raise(new MouseEventArgs(x, y), this);
        }

        public void InvokeMouseExit(Point position)
        {
            int x = position.X - X - ParentX;
            int y = position.Y - Y - ParentY;
            OnMouseExit(x, y);
            MouseExit.Raise(new MouseEventArgs(x, y), this);
        }

        public void InvokeMouseClick(Point position, MouseButton button)
        {
            int x = position.X - X - ParentX;
            int y = position.Y - Y - ParentY;
            OnMouseClick(x, y, button);

            if (button == MouseButton.Right)
            {
                MouseClick.Raise(new MouseEventArgs(x, y, button, ButtonState.Pressed), this);

                if (CanCloseWithRightClick)
                    CloseWithRightClick();
            }
            else
                MouseClick.Raise(new MouseEventArgs(x, y, button, ButtonState.Pressed), this);
        }

        public bool InvokeMouseDoubleClick(Point position, MouseButton button)
        {
            int x = position.X - X - ParentX;
            int y = position.Y - Y - ParentY;
            bool result = OnMouseDoubleClick(x, y, button);

            var arg = new MouseDoubleClickEventArgs(x, y, button);
            MouseDoubleClick.Raise(arg, this);
            result |= arg.Result;

            return result;
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
            MouseWheel.Raise(new MouseWheelEventArgs(delta), this);
        }

        public void InvokeDragBegin(Point position)
        {
            int x = position.X - X - ParentX;
            int y = position.Y - Y - ParentY;
            OnDragBegin(x, y);
            DragBegin.Raise(new MouseEventArgs(x, y, MouseButton.Left, ButtonState.Pressed), this);
        }

        public void InvokeDragEnd(Point position)
        {
            int x = position.X - X - ParentX;
            int y = position.Y - Y - ParentY;
            OnDragEnd(x, y);
            DragBegin.Raise(new MouseEventArgs(x, y, MouseButton.Left), this);
        }

        protected virtual void OnMouseDown(int x, int y, MouseButton button)
        {
            _mouseIsDown = true;
            Parent?.OnMouseDown(x, y, button);
        }

        protected virtual void OnMouseUp(int x, int y, MouseButton button)
        {
            _mouseIsDown = false;

            if (_attempToDrag)
            {
                _attempToDrag = false;
                InvokeDragEnd(new Point(x, y));
            }

            Parent?.OnMouseUp(x, y, button);
        }

        protected virtual void OnMouseWheel(MouseEvent delta)
        {
            Parent?.OnMouseWheel(delta);
        }

        protected virtual void OnMouseOver(int x, int y)
        {
            if (_mouseIsDown && !_attempToDrag && Mouse.LDropPosition != Point.Zero)
            {
                InvokeDragBegin(new Point(x, y));
                _attempToDrag = true;
            }
        }

        protected virtual void OnMouseEnter(int x, int y)
        {

        }

        protected virtual void OnMouseExit(int x, int y)
        {
            _attempToDrag = false;
        }

        protected virtual void OnMouseClick(int x, int y, MouseButton button)
        {
            Parent?.OnMouseClick(x, y, button);
        }

        protected virtual bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            return Parent?.OnMouseDoubleClick(x, y, button) ?? false;
        }

        protected virtual void OnDragBegin(int x, int y)
        {
        }

        protected virtual void OnDragEnd(int x, int y)
        {
        }

        protected virtual void OnTextInput(string c)
        {
        }

        protected virtual void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            Parent?.OnKeyDown(key, mod);
        }

        protected virtual void OnKeyUp(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            Parent?.OnKeyUp(key, mod);
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
            Parent?.OnButtonClick(buttonID);
        }

        public virtual void OnKeybaordReturn(int textID, string text)
        {
            Parent?.OnKeybaordReturn(textID, text);
        }

        public virtual void ChangePage(int pageIndex)
        {
            Parent?.ChangePage(pageIndex);
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