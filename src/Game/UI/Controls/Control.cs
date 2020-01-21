#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Interfaces;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using SDL2;

using IUpdateable = ClassicUO.Interfaces.IUpdateable;
using Keyboard = ClassicUO.Input.Keyboard;
using Mouse = ClassicUO.Input.Mouse;

namespace ClassicUO.Game.UI.Controls
{
    enum ClickPriority
    {
        High,
        Default,
        Low
    }

    internal abstract class Control : IDrawableUI, IUpdateable
    {
        internal static int _StepsDone = 1;
        internal static int _StepChanger = 1;
        private bool _acceptKeyboardInput, _acceptMouseInput, _mouseIsDown;
        private int _activePage;
        private bool _attempToDrag;
        private Rectangle _bounds;
        private GumpControlInfo _controlInfo;
        private bool _handlesKeyboardFocus;
        private Control _parent;

        protected static Vector3 _hueVector = Vector3.Zero;

        protected static void ResetHueVector()
        {
            _hueVector.X = 0;
            _hueVector.Y = 0;
            _hueVector.Z = 0;
        }

        protected Control(Control parent = null)
        {
            Parent = parent;
            Children = new List<Control>();
            AllowedToDraw = true;
            AcceptMouseInput = true;
            Page = 0;

            IsDisposed = false;
            IsEnabled = true;
        }

        public virtual ClickPriority Priority { get; set; } = ClickPriority.Default;

        public uint ServerSerial { get; set; }

        public uint LocalSerial { get; set; }

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

        public ref Rectangle Bounds => ref _bounds;
        
        public bool IsDisposed { get; private set; }

        public bool IsVisible { get; set; } = true;

        public bool IsEnabled { get; set; }

        public bool HasKeyboardFocus => UIManager.KeyboardFocusControl == this;

        public bool MouseIsOver => UIManager.MouseOverControl == this;

        public virtual bool CanMove { get; set; }

        public bool CanCloseWithRightClick { get; set; } = true;

        public bool CanCloseWithEsc { get; set; }

        public bool IsEditable { get; set; }

        public bool IsFocused { get; set; }

        public float Alpha { get; set; }

        public List<Control> Children { get; }

        public object Tag { get; set; }

        public object Tooltip { get; private set; }

        public bool HasTooltip => /*World.ClientFlags.TooltipsEnabled &&*/ Tooltip != null;

        public virtual bool AcceptKeyboardInput
        {
            get => IsEnabled && !IsDisposed && IsVisible && _acceptKeyboardInput;
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

        public ref int X => ref _bounds.X;

        public ref int Y => ref _bounds.Y;

        public int ParentX => Parent != null ? Parent.X + Parent.ParentX : 0;

        public int ParentY => Parent != null ? Parent.Y + Parent.ParentY : 0;

        public int ScreenCoordinateX => ParentX + X;

        public int ScreenCoordinateY => ParentY + Y;

        public ContextMenuControl ContextMenu { get; set; }

        public Control Parent
        {
            get => _parent;
            internal set
            {
                if (value == null)
                    _parent?.Children.Remove(this);
                else
                {
                    _parent?.Children.Remove(this);
                    value.Children.Add(this);
                }
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
                if (!IsEnabled || IsDisposed || !IsVisible)
                    return false;

                if (_handlesKeyboardFocus)
                    return true;

                if (Children == null)
                    return false;

                foreach (Control c in Children)
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
                _activePage = value;

                OnPageChanged();

                //if (UIManager.KeyboardFocusControl != null)
                //{
                //    if (Children.Contains(UIManager.KeyboardFocusControl))
                //    {
                //        if (UIManager.KeyboardFocusControl.Page != 0)
                //            UIManager.KeyboardFocusControl = null;
                //    }
                //}

                // When ActivePage changes, check to see if there are new text input boxes
                // that we should redirect text input to.
                
                //if (UIManager.KeyboardFocusControl == null)
                //{
                //    foreach (Control c in Children)
                //    {
                //        if (c.HandlesKeyboardFocus && c.Page == _activePage)
                //        {
                //            UIManager.KeyboardFocusControl = c;

                //            break;
                //        }
                //    }
                //}

            }
        }

        public bool WantUpdateSize { get; set; } = true;

        public bool AllowedToDraw { get; set; }

        public int TooltipMaxLength { get; private set; }

        public UOTexture Texture { get; set; }

        public virtual bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed) return false;

            if (Texture != null && !Texture.IsDisposed)
                Texture.Ticks = Time.Ticks;

            foreach (Control c in Children)
            {
                if (c.Page == 0 || c.Page == ActivePage)
                {
                    if (c.IsVisible)
                    {
                        c.Draw(batcher, c.X + x, c.Y + y);
                    }
                }
            }

            DrawDebug(batcher, x, y);

            return true;
        }

        public virtual void Update(double totalMS, double frameMS)
        {
            if (IsDisposed) return;

            if (Children.Count != 0)
            {
                //InitializeControls();
                int w = 0, h = 0;

                for (int i = 0; i < Children.Count; i++)
                {
                    Control c = Children[i];

                    if (c.IsDisposed)
                    {
                        OnChildRemoved();
                        Children.RemoveAt(i--);

                        continue;
                    }

                    c.Update(totalMS, frameMS);

                    if (WantUpdateSize)
                    {
                        if ((c.Page == 0 || c.Page == ActivePage) && c.IsVisible)
                        {
                            if (w < c.Bounds.Right)
                                w = c.Bounds.Right;

                            if (h < c.Bounds.Bottom)
                                h = c.Bounds.Bottom;
                        }
                    }
                }

                if (WantUpdateSize && IsVisible)
                {
                    if (w != Width)
                        Width = w;

                    if (h != Height)
                        Height = h;
                    WantUpdateSize = false;
                }
            }
        }

        public virtual void OnPageChanged()
        {
            //Update size as pages may vary in size.
            if (ServerSerial != 0)
                WantUpdateSize = true;
        }

        private void DrawDebug(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsVisible && CUOEnviroment.Debug)
            {
                ResetHueVector();
                batcher.DrawRectangle(Texture2DCache.GetTexture(Color.Green), x, y, Width, Height, ref _hueVector);
            }
        }

        public void BringOnTop()
        {
            UIManager.MakeTopMostGump(this);
        }

        public void SetTooltip(string text, int maxWidth = 0)
        {
            ClearTooltip();

            if (!string.IsNullOrEmpty(text))
            {
                Tooltip = text;
                TooltipMaxLength = maxWidth;
            }
        }

        public void SetTooltip(uint entity)
        {
            ClearTooltip();
            Tooltip = entity;
        }

        public void ClearTooltip()
        {
            Tooltip = null;
        }

        public void SetKeyboardFocus()
        {
            if (AcceptKeyboardInput && !HasKeyboardFocus) 
                UIManager.KeyboardFocusControl = this;
        }

        internal event EventHandler<MouseEventArgs> MouseDown, MouseUp, MouseOver, MouseEnter, MouseExit, DragBegin, DragEnd;

        internal event EventHandler<MouseWheelEventArgs> MouseWheel;

        internal event EventHandler<MouseDoubleClickEventArgs> MouseDoubleClick;

        internal event EventHandler FocusEnter, FocusLost;


        public void HitTest(int x, int y, ref Control res)
        {
            if (!IsVisible || !IsEnabled)
                return;

            int parentX = ParentX;
            int parentY = ParentY;

            if (Bounds.Contains(x - parentX, y - parentY))
            {
                if (Contains(x - X - parentX, y - Y - parentY))
                {
                    if (AcceptMouseInput)
                    {
                        if (res == null || res.Priority >= this.Priority)
                            res = this;
                    }

                    foreach (Control c in Children)
                    {
                        if (c.Page == 0 || c.Page == ActivePage)
                        {
                            c.HitTest(x, y, ref res);
                        }
                    }
                }
            }
        }

        public void HitTest(Point position, ref Control res)
        {
            HitTest(position.X, position.Y, ref res);
        }

       
        public Control GetFirstControlAcceptKeyboardInput()
        {
            if (_acceptKeyboardInput)
                return this;

            if (Children == null || Children.Count == 0)
                return null;

            foreach (Control c in Children)
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

        public void Insert(int index, Control c, int page = 0)
        {
            c.Page = 0;

            c._parent?.Children.Remove(c);

            c._parent = this;

            Children.Insert(index, c);
            
            OnChildAdded();
        }

        public virtual void Remove(Control c)
        {
            if (c == null)
                return;

            c.Parent = null;
            Children.Remove(c);
            OnChildRemoved();
        }

        public virtual void Clear()
        {
            Children.ForEach(s => s.Dispose());
        }

        public T[] GetControls<T>() where T : Control
        {
            return Children.OfType<T>().Where(s => !s.IsDisposed).ToArray();
        }

        public IEnumerable<T> FindControls<T>() where T : Control
        {
            return Children.OfType<T>().Where(s => !s.IsDisposed);
        }


        public void InvokeMouseDown(Point position, MouseButtonType button)
        {
            int x = position.X - X - ParentX;
            int y = position.Y - Y - ParentY;
            OnMouseDown(x, y, button);
            MouseDown.Raise(new MouseEventArgs(x, y, button, ButtonState.Pressed), this);
        }

        public void InvokeMouseUp(Point position, MouseButtonType button)
        {
            int x = position.X - X - ParentX;
            int y = position.Y - Y - ParentY;
            OnMouseUp(x, y, button);
            MouseUp.Raise(new MouseEventArgs(x, y, button), this);
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

        public bool InvokeMouseDoubleClick(Point position, MouseButtonType button)
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

        public void InvokeMouseWheel(MouseEventType delta)
        {
            OnMouseWheel(delta);
            MouseWheel.Raise(new MouseWheelEventArgs(delta), this);
        }

        public void InvokeDragBegin(Point position)
        {
            int x = position.X - X - ParentX;
            int y = position.Y - Y - ParentY;
            OnDragBegin(x, y);
            DragBegin.Raise(new MouseEventArgs(x, y, MouseButtonType.Left, ButtonState.Pressed), this);
        }

        public void InvokeDragEnd(Point position)
        {
            int x = position.X - X - ParentX;
            int y = position.Y - Y - ParentY;
            OnDragEnd(x, y);
            DragEnd.Raise(new MouseEventArgs(x, y, MouseButtonType.Left), this);
        }

        public void InvokeMove(int x, int y)
        {
            x = x - X - ParentX;
            y = y - Y - ParentY;
            OnMove(x, y);
        }

        protected virtual void OnMouseDown(int x, int y, MouseButtonType button)
        {
            _mouseIsDown = true;
            Parent?.OnMouseDown(X + x, Y + y, button);
        }

        protected virtual void OnMouseUp(int x, int y, MouseButtonType button)
        {
            _mouseIsDown = false;

            if (_attempToDrag)
            {
                _attempToDrag = false;
                InvokeDragEnd(new Point(x, y));
            }

            Parent?.OnMouseUp(X + x, Y + y, button);

            if (button == MouseButtonType.Right && !IsDisposed && !CanCloseWithRightClick && !Keyboard.Alt && !Keyboard.Shift && !Keyboard.Ctrl && ContextMenu != null && !ContextMenu.IsDisposed)
            {
                ContextMenu.Show();
            }
        }

        protected virtual void OnMouseWheel(MouseEventType delta)
        {
            Parent?.OnMouseWheel(delta);
        }

        protected virtual void OnMouseOver(int x, int y)
        {
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
            else 
                Parent?.OnMouseOver(X + x, Y + y);
        }

        protected virtual void OnMouseEnter(int x, int y)
        {
        }

        protected virtual void OnMouseExit(int x, int y)
        {
            _attempToDrag = false;
        }

        protected virtual bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            return Parent?.OnMouseDoubleClick(X + x, Y + y, button) ?? false;
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

        public virtual bool Contains(int x, int y)
        {
            return !IsDisposed;
        }

        protected virtual void OnMove(int x, int y)
        {
        }

        protected virtual void OnClosing()
        {
        }

        internal virtual void OnFocusEnter()
        {
            IsFocused = true;
            FocusEnter.Raise(this);
            //Parent?.OnFocusEnter();
        }

        internal virtual void OnFocusLeft()
        {
            IsFocused = false;
            FocusLost.Raise(this);
            //Parent?.OnFocusLeft();
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
                Dispose();
            else
                Parent.CloseWithRightClick();
        }

        public void KeyboardTabToNextFocus(Control c)
        {
            int startIndex = Children.IndexOf(c);

            for (int i = startIndex + 1; i < Children.Count; i++)
            {
                if (Children[i].AcceptKeyboardInput)
                {
                    Children[i].SetKeyboardFocus();

                    return;
                }
            }

            for (int i = 0; i < startIndex; i++)
            {
                if (Children[i].AcceptKeyboardInput)
                {
                    Children[i].SetKeyboardFocus();

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

            foreach (Control c in Children)
            {
                c.Dispose();
            }

            Children.Clear();

            ContextMenu?.Dispose();

            IsDisposed = true;
        }
    }
}