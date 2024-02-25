#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using ClassicUO.Game.Managers;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using Keyboard = ClassicUO.Input.Keyboard;

namespace ClassicUO.Game.UI.Controls
{
    public abstract class Control
    {
        internal static int _StepsDone = 1;
        internal static int _StepChanger = 1;

        private bool _acceptKeyboardInput, _acceptMouseInput;
        private int _activePage;
        private Rectangle _bounds;
        private bool _handlesKeyboardFocus;
        private Point _offset;
        private Control _parent;
        private float alpha = 1.0f;

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

        public bool IsFromServer { get; set; }

        /// <summary>
        /// This is not implemented in all controls, this is for use in custom control's that want to have a scale setting
        /// </summary>
        public double Scale { get; set; } = 1.0f;

        /// <summary>
        /// This is intended for scaling mouse positions and other non-visual uses
        /// </summary>
        public double InternalScale { get; set; } = 1.0f;

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

        public Point Offset => _offset;

        public bool IsDisposed { get; private set; }

        public bool IsVisible { get; set; } = true;

        public bool IsEnabled { get; set; }

        public bool HasKeyboardFocus => UIManager.KeyboardFocusControl == this;

        public bool MouseIsOver => UIManager.MouseOverControl == this;

        public bool CanMove { get; set; }

        public bool CanCloseWithRightClick { get; set; } = true;

        public bool CanCloseWithEsc { get; set; }

        public bool IsEditable { get; set; }

        public bool IsFocused { get; set; }

        public float Alpha
        {
            get => alpha; set
            {
                float old = alpha;
                alpha = value;
                AlphaChanged(old, value);
            }
        }

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

        public ref int X => ref _bounds.X;

        public ref int Y => ref _bounds.Y;

        public ref int Width => ref _bounds.Width;

        public ref int Height => ref _bounds.Height;

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
                {
                    _parent?.Children.Remove(this);
                }
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
                {
                    return null;
                }

                Control p = Parent;

                while (p.Parent != null)
                {
                    p = p.Parent;
                }

                return p;
            }
        }

        public UILayer LayerOrder { get; set; } = UILayer.Default;
        public bool IsModal { get; set; }
        public bool ModalClickOutsideAreaClosesThisControl { get; set; }


        public virtual bool HandlesKeyboardFocus
        {
            get
            {
                if (!IsEnabled || IsDisposed || !IsVisible)
                {
                    return false;
                }

                if (_handlesKeyboardFocus)
                {
                    return true;
                }

                if (Children == null)
                {
                    return false;
                }

                foreach (Control c in Children)
                {
                    if (c.HandlesKeyboardFocus)
                    {
                        return true;
                    }
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
            }
        }

        public bool WantUpdateSize { get; set; } = true;

        public bool AllowedToDraw { get; set; }

        public int TooltipMaxLength { get; private set; }

        public void UpdateOffset(int x, int y)
        {
            if (_offset.X != x || _offset.Y != y)
            {
                _offset.X = x;
                _offset.Y = y;

                foreach (Control c in Children)
                {
                    c.UpdateOffset(x, y);
                }
            }
        }

        public virtual bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            for (int i = 0; i < Children.Count; i++)
            {
                if (Children.Count <= i)
                {
                    break;
                }
                Control c = Children.ElementAt(i);

                if (c != null && (c.Page == 0 || c.Page == ActivePage))
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

        /// <summary>
        /// Update is called as often as possible.
        /// </summary>
        public virtual void Update()
        {
            if (IsDisposed)
            {
                return;
            }


            if (Children.Count != 0)
            {
                List<Control> removalList = new List<Control>(); ;
                int w = 0, h = 0;

                for (int i = 0; i < Children.Count; i++)
                {
                    if (i < 0 || i >= Children.Count)
                    {
                        continue;
                    }

                    Control c = Children.ElementAt(i);

                    if (c == null)
                    {
                        continue;
                    }

                    if (c.IsDisposed)
                    {
                        removalList.Add(c);
                        continue;
                    }

                    c.Update();

                    if (WantUpdateSize)
                    {
                        if ((c.Page == 0 || c.Page == ActivePage) && c.IsVisible)
                        {
                            if (w < c.Bounds.Right)
                            {
                                w = c.Bounds.Right;
                            }

                            if (h < c.Bounds.Bottom)
                            {
                                h = c.Bounds.Bottom;
                            }
                        }
                    }
                }

                if (removalList.Count > 0)
                {
                    foreach (Control c in removalList)
                    {
                        OnChildRemoved();
                        Children.Remove(c);
                    }
                }

                if (WantUpdateSize && IsVisible)
                {
                    if (w != Width)
                    {
                        Width = w;
                    }

                    if (h != Height)
                    {
                        Height = h;
                    }

                    WantUpdateSize = false;
                }
            }
        }

        /// <summary>
        /// Intended for updates that don't need to occur as frequently as Update() does.
        /// SlowUpdate is called twice per second.
        /// </summary>
        public virtual void SlowUpdate()
        {
            if (IsDisposed)
            {
                return;
            }

            if (Children.Count != 0)
            {
                List<Control> removalList = new List<Control>(); ;

                for (int i = 0; i < Children.Count; i++)
                {
                    if (i < 0 || i >= Children.Count)
                    {
                        continue;
                    }

                    Control c = Children.ElementAt(i);

                    if (c == null)
                    {
                        continue;
                    }

                    if (c.IsDisposed)
                    {
                        removalList.Add(c);
                        continue;
                    }

                    c.SlowUpdate();

                }

                if (removalList.Count > 0)
                {
                    foreach (Control c in removalList)
                    {
                        OnChildRemoved();
                        Children.Remove(c);
                    }
                }

            }
        }

        /// <summary>
        /// Scale the width and height of this control. Width/Height * Scale
        /// </summary>
        /// <param name="scale"></param>
        /// <returns>This control</returns>
        public virtual Control ScaleWidthAndHeight(double scale)
        {
            if (scale != 1f)
            {
                Width = (int)(Width * scale);
                Height = (int)(Height * scale);
            }
            return this;
        }

        /// <summary>
        /// Scale the x/y position of this control. x/y * Scale
        /// </summary>
        /// <param name="scale"></param>
        /// <returns>This control</returns>
        public virtual Control ScaleXAndY(double scale)
        {
            if (scale != 1f)
            {
                X = (int)(X * scale);
                Y = (int)(Y * scale);
            }
            return this;
        }

        /// <summary>
        /// Set the internal scale used for mouse interactions or other non visual scaling
        /// </summary>
        /// <param name="scale"></param>
        /// <returns>This control</returns>
        public virtual Control SetInternalScale(double scale)
        {
            InternalScale = scale;
            return this;
        }

        public void ForceSizeUpdate()
        {
            int h = Height, w = Width;
            for (int i = 0; i < Children.Count; i++)
            {
                Control c = Children[i];
                if ((c.Page == 0 || c.Page == ActivePage) && c.IsVisible && !c.IsDisposed)
                {
                    if (w < c.Bounds.Right)
                    {
                        w = c.Bounds.Right;
                    }

                    if (h < c.Bounds.Bottom)
                    {
                        h = c.Bounds.Bottom;
                    }
                }
            }

            if (w != Width)
            {
                Width = w;
            }

            if (h != Height)
            {
                Height = h;
            }

            WantUpdateSize = false;
        }

        public virtual void OnPageChanged()
        {
            //Update size as pages may vary in size.
            if (ServerSerial != 0)
            {
                WantUpdateSize = true;
            }
        }

        private void DrawDebug(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsVisible && CUOEnviroment.Debug)
            {
                Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

                batcher.DrawRectangle
                (
                    SolidColorTextureCache.GetTexture(Color.Green),
                    x,
                    y,
                    Width,
                    Height,
                    hueVector
                );
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
            {
                UIManager.KeyboardFocusControl = this;
            }
        }

        internal event EventHandler<MouseEventArgs> MouseDown, MouseUp, MouseOver, MouseEnter, MouseExit, DragBegin, DragEnd;

        internal event EventHandler<MouseWheelEventArgs> MouseWheel;

        internal event EventHandler<MouseDoubleClickEventArgs> MouseDoubleClick;

        internal event EventHandler FocusEnter, FocusLost;

        internal event EventHandler<KeyboardEventArgs> KeyDown, KeyUp;

        internal event EventHandler<SDL.SDL_GameControllerButton> ControllerButtonUp, ControllerButtonDown;


        public void HitTest(int x, int y, ref Control res)
        {
            if (!IsVisible || !IsEnabled || IsDisposed)
            {
                return;
            }

            int parentX = ParentX;
            int parentY = ParentY;

            if (Bounds.Contains(x - parentX - _offset.X, y - parentY - _offset.Y))
            {
                if (Contains(x - X - parentX, y - Y - parentY))
                {
                    if (AcceptMouseInput)
                    {
                        if (res == null || res.Priority >= Priority)
                        {
                            res = this;
                            OnHitTestSuccess(x, y, ref res);
                        }
                    }

                    for (int i = 0; i < Children.Count; ++i)
                    {
                        Control c = Children[i];

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

        public virtual void OnHitTestSuccess(int x, int y, ref Control res)
        {
        }

        /// <summary>
        /// Invoked when alpha is changed on a control
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        public virtual void AlphaChanged(float oldValue, float newValue) { }

        public Control GetFirstControlAcceptKeyboardInput()
        {
            if (_acceptKeyboardInput)
            {
                return this;
            }

            if (Children == null || Children.Count == 0)
            {
                return null;
            }

            foreach (Control c in Children)
            {
                Control a = c.GetFirstControlAcceptKeyboardInput();

                if (a != null)
                {
                    return a;
                }
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
            {
                return;
            }

            c.Parent = null;
            Children.Remove(c);
            OnChildRemoved();
        }

        public virtual void Clear()
        {
            foreach (Control c in Children)
            {
                c.Dispose();
            }
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
            {
                CloseWithRightClick();
            }
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

            MouseDoubleClickEventArgs arg = new MouseDoubleClickEventArgs(x, y, button);
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
            KeyboardEventArgs arg = new KeyboardEventArgs(key, mod, KeyboardEventType.Down);
            KeyDown?.Raise(arg);
        }

        public void InvokeKeyUp(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            OnKeyUp(key, mod);
            KeyboardEventArgs arg = new KeyboardEventArgs(key, mod, KeyboardEventType.Up);
            KeyUp?.Raise(arg);
        }

        public void InvokeControllerButtonUp(SDL.SDL_GameControllerButton button) { OnControllerButtonUp(button); ControllerButtonUp?.Raise(button); }

        public void InvokeControllerButtonDown(SDL.SDL_GameControllerButton button) { OnControllerButtonDown(button); ControllerButtonDown?.Raise(button); }

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
            Parent?.OnMouseDown(X + x, Y + y, button);
        }

        protected virtual void OnMouseUp(int x, int y, MouseButtonType button)
        {
            Parent?.OnMouseUp(X + x, Y + y, button);

            if (button == MouseButtonType.Right && !IsDisposed && !CanCloseWithRightClick && !Keyboard.Alt && !Keyboard.Shift && !Keyboard.Ctrl)
            {
                ContextMenu?.Show();
            }
        }

        protected virtual void OnMouseWheel(MouseEventType delta)
        {
            Parent?.OnMouseWheel(delta);
        }

        protected virtual void OnMouseOver(int x, int y)
        {
            Parent?.OnMouseOver(X + x, Y + y);
        }

        protected virtual void OnMouseEnter(int x, int y)
        {
        }

        protected virtual void OnMouseExit(int x, int y)
        {
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

        protected virtual void OnControllerButtonUp(SDL.SDL_GameControllerButton button) { }

        protected virtual void OnControllerButtonDown(SDL.SDL_GameControllerButton button) { }

        public virtual bool Contains(int x, int y)
        {
            return !IsDisposed;
        }

        protected virtual void OnMove(int x, int y)
        {
        }

        internal virtual void OnFocusEnter()
        {
            if (!IsFocused)
            {
                IsFocused = true;
                FocusEnter.Raise(this);
                //Parent?.OnFocusEnter();
            }
        }

        internal virtual void OnFocusLost()
        {
            if (IsFocused)
            {
                IsFocused = false;
                FocusLost.Raise(this);
                //Parent?.OnFocusLeft();
            }
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
            {
                return;
            }

            Control parent = Parent;

            while (parent != null)
            {
                if (!parent.CanCloseWithRightClick)
                {
                    return;
                }

                parent = parent.Parent;
            }

            if (Parent == null)
            {
                Dispose();
            }
            else
            {
                Parent.CloseWithRightClick();
            }
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
            {
                return;
            }

            if (Children != null)
            {
                foreach (Control c in Children)
                {
                    c.Dispose();
                }

                Children.Clear();
            }

            IsDisposed = true;
            AfterDispose();
        }

        /// <summary>
        /// Called after the control has been disposed.
        /// </summary>
        public virtual void AfterDispose() { }
    }
}