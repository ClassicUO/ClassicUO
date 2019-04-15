#region license
//  Copyright (C) 2019 ClassicUO Development Community on Github
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
using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.Interfaces;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using SDL2;

using IUpdateable = ClassicUO.Interfaces.IUpdateable;
using Mouse = ClassicUO.Input.Mouse;

namespace ClassicUO.Game.UI.Controls
{
    public enum ClickPriority
    {
        High,
        Default,
        Low
    }

    internal abstract class Control : IDrawableUI, IUpdateable, IColorable
    {
        internal static int _StepsDone = 1;
        internal static int _StepChanger = 1;
        private static SpriteTexture _debugTexture;
        private readonly List<Control> _children;
        private bool _acceptKeyboardInput, _acceptMouseInput, _mouseIsDown;
        private int _activePage;
        private bool _attempToDrag;
        private Rectangle _bounds;
        private GumpControlInfo _controlInfo;
        private bool _handlesKeyboardFocus;
        private Point _lastClickPosition;
        private Control _parent;

        protected Control(Control parent = null)
        {
            Parent = parent;
            _children = new List<Control>();
            AllowedToDraw = true;
            AcceptMouseInput = true;
            Page = 0;
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
                _bounds.Location = value;
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

        public bool HasKeyboardFocus => Engine.UI.KeyboardFocusControl == this;

        public bool MouseIsOver => Engine.UI.MouseOverControl == this;

        public virtual bool CanMove { get; set; }

        public bool CanCloseWithRightClick { get; set; } = true;

        public bool CanCloseWithEsc { get; set; }

        public bool IsEditable { get; set; }

        public bool IsTransparent { get; set; }

        public float Alpha { get; set; }

        public IReadOnlyList<Control> Children => _children;

        public object Tag { get; set; }

        public object Tooltip { get; private set; }

        public bool HasTooltip => /*World.ClientFlags.TooltipsEnabled &&*/ Tooltip != null;

        public virtual bool AcceptKeyboardInput
        {
            get => IsEnabled && !IsDisposed && IsVisible && IsInitialized && _acceptKeyboardInput;
            set => _acceptKeyboardInput = value;
        }

        public virtual bool AcceptMouseInput
        {
            get => IsEnabled && !IsDisposed && _acceptMouseInput && IsVisible;
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

        public Control Parent
        {
            get => _parent;
            internal set
            {
                if (value == null)
                    _parent?._children.Remove(this);
                else
                    value._children.Add(this);
                _parent = value;
            }
        }

        public Control RootParent
        {
            get
            {
                if (Parent == null)
                    return null;
                Control p = Parent;

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

                foreach (Control c in _children)
                {
                    if (c.HandlesKeyboardFocus)
                        return true;
                }

                return false;
            }
            set => _handlesKeyboardFocus = value;
        }

        public virtual void OnPageChanged()
        {

        }

        public int ActivePage
        {
            get => _activePage;
            set
            {              
                _activePage = value;

                OnPageChanged();

                if (Engine.UI.KeyboardFocusControl != null)
                {
                    if (Children.Contains(Engine.UI.KeyboardFocusControl))
                    {
                        if (Engine.UI.KeyboardFocusControl.Page != 0)
                            Engine.UI.KeyboardFocusControl = null;
                    }
                }

                // When ActivePage changes, check to see if there are new text input boxes
                // that we should redirect text input to.
                if (Engine.UI.KeyboardFocusControl == null)
                {
                    foreach (Control c in Children)
                    {
                        if (c.HandlesKeyboardFocus && c.Page == _activePage)
                        {
                            Engine.UI.KeyboardFocusControl = c;

                            break;
                        }
                    }
                }
            }
        }

        public bool WantUpdateSize { get; set; } = true;

        public Vector3 HueVector { get; set; }

        public bool AllowedToDraw { get; set; }

        public SpriteTexture Texture { get; set; }

        public virtual bool Draw(Batcher2D batcher, int x, int y)
        {
            if (IsDisposed) return false;

            if (Texture != null && !Texture.IsDisposed)
                Texture.Ticks = Engine.Ticks;

            foreach (Control c in Children)
            {
                if (c.Page == 0 || c.Page == ActivePage)
                {
                    if (c.IsVisible && c.IsInitialized)
                    {
                        c.Draw(batcher, c.X + x, c.Y + y);

                        //DrawDebug(batcher, position);
                    }
                }
            }

            DrawDebug(batcher, x, y);

            return true;
        }

        private void DrawDebug(Batcher2D batcher, int x, int y)
        {
            if (IsVisible && Engine.GlobalSettings.Debug)
            {
                if (_debugTexture == null)
                {
                    _debugTexture = new SpriteTexture(1, 1);

                    _debugTexture.SetData(new Color[1]
                    {
                        Color.Green
                    });
                }

                //int w, h;

                //if (Texture == null)
                //{
                //    w = Width;
                //    h = Height;
                //}
                //else
                //{
                //    w = Texture.Width;
                //    h = Texture.Height;
                //}

                batcher.DrawRectangle(_debugTexture, x, y, Width, Height, Vector3.Zero);
            }
        }

        public virtual void Update(double totalMS, double frameMS)
        {
            if (IsDisposed || !IsInitialized) return;

            if (Children.Count > 0)
            {
                InitializeControls();
                int w = 0, h = 0;

                for (int i = 0; i < _children.Count; i++)
                {
                    Control c = _children[i];

                    if (c.IsDisposed)
                    {
                        OnChildRemoved();
                        _children.RemoveAt(i--);
                        continue;  
                    }

                    c.Update(totalMS, frameMS);

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

                if (WantUpdateSize)
                {
                    if (w != Width)
                        Width = w;

                    if (h != Height)
                        Height = h;
                    WantUpdateSize = false;
                }
            }
        }

        public void BringOnTop()
        {
            Engine.UI.MakeTopMostGump(this);
        }
      
        public void SetTooltip(string text, int maxWidth = 0)
        {
	        ClearTooltip();

			if (!String.IsNullOrEmpty(text))
            {
                Tooltip = text;
                TooltipMaxLength = maxWidth;
            }
        }

	    public void SetTooltip(Entity entity)
	    {
		    ClearTooltip();

		    if (entity != null & !entity.IsDestroyed)
			    Tooltip = entity;
	    }

		public int TooltipMaxLength { get; private set; }

        public void ClearTooltip()
        {
            Tooltip = null;
        }

        public void SetKeyboardFocus()
        {
            if (AcceptKeyboardInput && !HasKeyboardFocus)
            {
                Engine.UI.KeyboardFocusControl = this;
            }
        }

        public event EventHandler Disposed;

        internal event EventHandler<MouseEventArgs> MouseDown, MouseUp, MouseMove, MouseOver, MouseEnter, MouseExit, MouseClick, DragBegin, DragEnd;

        internal event EventHandler<MouseWheelEventArgs> MouseWheel;

        internal event EventHandler<KeyboardEventArgs> Keyboard;

        internal event EventHandler<MouseDoubleClickEventArgs> MouseDoubleClick;

        public void Initialize()
        {
            if (IsDisposed)
            {
                return;
            }

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
                Control c = _children[i];

                if (!c.IsInitialized && !IsDisposed)
                {
                    c.Initialize();

                    if (!initializedKeyboardFocusedControl && c.AcceptKeyboardInput)
                    {
                        Engine.UI.KeyboardFocusControl = c;
                        initializedKeyboardFocusedControl = true;
                    }
                }
            }
        }

     
        public Control[] HitTest(Point position)
        {
            //List<Control> results = new List<Control>();
            Stack<Control> results = new Stack<Control>();

            if (Bounds.Contains(position.X - ParentX, position.Y - ParentY))
            {
                if (Contains(position.X - X - ParentX, position.Y - Y - ParentY))
                {
                    if (AcceptMouseInput)
                        results.Push(this);
                    //results.Add(this);
                    //results.Insert(0, this);  //results.Push(this);

                    foreach (Control c in Children)
                    {
                        if (c.Page == 0 || c.Page == ActivePage)
                        {
                            var cl = c.HitTest(position);

                            if (cl != null)
                            {
                                for (int j = cl.Length - 1; j >= 0; j--)
                                    results.Push(cl[j]);
                                //results.Insert(0, cl[j]);
                                //results.AddRange(cl);
                            }
                        }
                    }
                }
            }

            if (results.Count != 0)
            {
                var res = results.ToArray();
                Array.Sort(res, (a, b) => a.Priority.CompareTo(b.Priority));

                return res;
            }

            return null;

            //return results.Count == 0 ? null : results/*.OrderBy(s => s.Priority)*/;
        }

        public Control GetFirstControlAcceptKeyboardInput()
        {
            if (_acceptKeyboardInput)
                return this;

            if (_children == null || _children.Count == 0)
                return null;

            foreach (Control c in _children)
            {
                Control a = c.GetFirstControlAcceptKeyboardInput();

                if (a != null)
                    return a;
            }

            return null;
        }

        public virtual void Add(Control c, int page = 0)
        {
            c.Page = page;
            c.Parent = this;
            OnChildAdded();
        }

        public virtual void Remove(Control c)
        {
            if (c == null)
                return;
            c.Parent = null;
            _children.Remove(c);
            OnChildRemoved();
        }

        public virtual void Clear()
        {
            _children.ForEach(s => s.Dispose());
        }

        public T[] GetControls<T>() where T : Control
        {
            return Children.OfType<T>().Where(s => !s.IsDisposed).ToArray();
        }

        public IEnumerable<T> FindControls<T>() where T : Control
        {
            return Children.OfType<T>().Where(s => !s.IsDisposed);
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

        public void InvokeMouseCloseGumpWithRClick()
        {
            if (CanCloseWithRightClick)
                CloseWithRightClick();
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
            //if (_mouseIsDown && !_attempToDrag 
            //                 && (Math.Abs(Mouse.LDroppedOffset.X) > _minimumDistanceForDrag 
            //                     || Math.Abs(Mouse.LDroppedOffset.Y) > _minimumDistanceForDrag))
            //{
            //    InvokeDragBegin(new Point(x, y));
            //    _attempToDrag = true;
            //}

            if (_mouseIsDown && !_attempToDrag)
            {
                Point offset = Mouse.LDroppedOffset;
                if (Math.Abs(offset.X) > Constants.MIN_GUMP_DRAG_DISTANCE
                    || Math.Abs(offset.Y) > Constants.MIN_GUMP_DRAG_DISTANCE)

                {
                    InvokeDragBegin(new Point(x, y));
                    _attempToDrag = true;
                }
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
            _mouseIsDown = false;
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
            return !IsDisposed;
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

        internal virtual void OnFocusEnter()
        {
        }

        internal virtual void OnFocusLeft()
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
            Control parent = Parent;

            while (parent != null)
            {
                if (!parent.CanCloseWithRightClick)
                    return;
                parent = parent.Parent;
            }

            if (Parent == null)
            {
                Dispose();
            }
            else
                Parent.CloseWithRightClick();
        }

        public void KeyboardTabToNextFocus(Control c)
        {
            int startIndex = _children.IndexOf(c);
            for (int i = startIndex + 1; i < _children.Count; i++)
            {
                if (_children[i].AcceptKeyboardInput)
                {
                    _children[i].SetKeyboardFocus();
                    return;
                }
            }
            for (int i = 0; i < startIndex; i++)
            {
                if (_children[i].AcceptKeyboardInput)
                {
                    _children[i].SetKeyboardFocus();
                    return;
                }
            }
        }

        public virtual void OnButtonClick(int buttonID)
        {
            Parent?.OnButtonClick(buttonID);
        }

        public virtual void OnKeyboardReturn(int textID, string text)
        {
            Parent?.OnKeyboardReturn(textID, text);
        }

        public virtual void ChangePage(int pageIndex)
        {
            Parent?.ChangePage(pageIndex);
        }

        public virtual void Dispose()
        {
            if (IsDisposed)
                return;

            for (int i = 0; i < _children.Count; i++)
            {
                Control c = _children[i];
                c.Dispose();

                _children.RemoveAt(i--);
            }

            IsDisposed = true;

            Disposed.Raise();
        }
    }
}