// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SDL3;
using System;
using System.Collections.Generic;
using System.Linq;
using Keyboard = ClassicUO.Input.Keyboard;

namespace ClassicUO.Game.UI.Controls
{
    internal abstract class Control
    {
        protected const float CHILD_LAYER_INCREMENT = 0.01f;
        internal static int _StepsDone = 1;
        internal static int _StepChanger = 1;

        private bool _acceptKeyboardInput, _acceptMouseInput;
        private int _activePage;
        private Rectangle _bounds;
        private bool _handlesKeyboardFocus;
        private Point _offset;
        private Control _parent;
        private bool _isVisible = true;
        private int _page;
        private float _alpha = 1.0f;

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

        public int Page
        {
            get => _page;
            set
            {
                if (_page != value)
                {
                    _page = value;
                    NotifyRenderDirty();
                }
            }
        }

        public Point Location
        {
            get => _bounds.Location;
            set
            {
                if (_bounds.Location != value)
                {
                    X = value.X;
                    Y = value.Y;
                    _bounds.Location = value;
                    NotifyRenderDirty();
                }
            }
        }

        public ref Rectangle Bounds => ref _bounds;

        public Point Offset => _offset;

        public bool IsDisposed { get; private set; }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    NotifyRenderDirty();
                }
            }
        }

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
            get => _alpha;
            set
            {
                if (_alpha != value)
                {
                    _alpha = value;
                    NotifyRenderDirty();
                }
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

        // X / Y / Width / Height used to be `ref int` accessors returning a
        // reference into the underlying Rectangle, which let callers mutate the
        // field directly with no setter interception. That meant any layout
        // recompute (e.g. `child.Y = Height - offset;` inside an Update override)
        // silently bypassed NotifyRenderDirty, leaving the retained render
        // cache stale. Converting to full properties funnels every write through
        // a change-detect + notify. A repo-wide grep confirmed there are no
        // callers of `ref control.X` style usages — only the declarations
        // themselves — so dropping ref is safe. Compound-assignment forms
        // (`control.X += 5`, `control.X++`) still compile fine against regular
        // properties.

        public int X
        {
            get => _bounds.X;
            set
            {
                if (_bounds.X != value)
                {
                    _bounds.X = value;
                    NotifyRenderDirty();
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
                    NotifyRenderDirty();
                }
            }
        }

        public int Width
        {
            get => _bounds.Width;
            set
            {
                if (_bounds.Width != value)
                {
                    _bounds.Width = value;
                    NotifyRenderDirty();
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
                    NotifyRenderDirty();
                }
            }
        }

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
                if (_activePage != value)
                {
                    _activePage = value;
                    NotifyRenderDirty();
                    OnPageChanged();
                }
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

        public virtual bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepth)
        {
            if (IsDisposed)
            {
                return false;
            }

            foreach (Control c in Children)
            {
                if (c.Page == 0 || c.Page == ActivePage)
                {
                    if (c.IsVisible)
                    {
                        layerDepth += CHILD_LAYER_INCREMENT;
                        c.AddToRenderLists(renderLists, c.X + x, c.Y + y, ref layerDepth);
                    }
                }
            }

            DrawDebug(renderLists, x, y, layerDepth);

            return true;
        }

        public virtual void Update()
        {
            if (IsDisposed)
            {
                return;
            }

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

        public virtual void OnPageChanged()
        {
            //Update size as pages may vary in size.
            if (ServerSerial != 0)
            {
                WantUpdateSize = true;
            }
        }

        private void DrawDebug(RenderLists renderLists, int x, int y, float layerDepth)
        {
            if (IsVisible && CUOEnviroment.Debug)
            {
                Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);
                Texture2D pixel = SolidColorTextureCache.GetTexture(Color.Green);

                renderLists.AddGumpSprite(pixel, new Rectangle(x, y, Width, 1), hueVector, layerDepth); // top
                renderLists.AddGumpSprite(pixel, new Rectangle(x + Width - 1, y, 1, Height), hueVector, layerDepth); // right
                renderLists.AddGumpSprite(pixel, new Rectangle(x, y + Height - 1, Width, 1), hueVector, layerDepth); // bottom
                renderLists.AddGumpSprite(pixel, new Rectangle(x, y, 1, Height), hueVector, layerDepth); // left
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

        public virtual T Add<T>(T c, int page = 0) where T : Control
        {
            c.Page = page;
            c.Parent = this;
            OnChildAdded();

            return c;
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
            // Controls that render a "pressed" state (Button, NiceButton, checkboxes
            // whose click flips IsChecked in OnMouseUp, etc.) need the cache to
            // rebuild after the state change. Invalidating unconditionally is cheap
            // and keeps opt-in simple — any interactive control just works.
            NotifyRenderDirty();
        }

        public void InvokeMouseUp(Point position, MouseButtonType button)
        {
            int x = position.X - X - ParentX;
            int y = position.Y - Y - ParentY;
            OnMouseUp(x, y, button);
            MouseUp.Raise(new MouseEventArgs(x, y, button), this);
            NotifyRenderDirty();
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
            // Many controls render differently on hover (HitBox highlight, Button
            // _entered sprite, HoveredLabel, etc.). Invalidate the owning gump's
            // cache so the next frame rebuilds with the new hover state instead of
            // replaying the non-hover snapshot.
            NotifyRenderDirty();
        }

        public void InvokeMouseExit(Point position)
        {
            int x = position.X - X - ParentX;
            int y = position.Y - Y - ParentY;
            OnMouseExit(x, y);
            MouseExit.Raise(new MouseEventArgs(x, y), this);
            NotifyRenderDirty();
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
            // Typing into a text box changes what the control renders. Invalidate
            // so the cache rebuilds with the new text / caret position next frame.
            NotifyRenderDirty();
        }

        public void InvokeKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            OnKeyDown(key, mod);
            KeyboardEventArgs arg = new KeyboardEventArgs(key, mod, KeyboardEventType.Down);
            KeyDown?.Raise(arg);
            // Editing keys (Backspace, Delete, arrows for selection, etc.) also
            // change render state even when they don't insert a character.
            NotifyRenderDirty();
        }

        public void InvokeKeyUp(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            OnKeyUp(key, mod);
            KeyboardEventArgs arg = new KeyboardEventArgs(key, mod, KeyboardEventType.Up);
            KeyUp?.Raise(arg);
        }

        public void InvokeMouseWheel(MouseEventType delta)
        {
            OnMouseWheel(delta);
            MouseWheel.Raise(new MouseWheelEventArgs(delta), this);
            // Scroll changes what the control shows (scroll bar position, clipped
            // region contents).
            NotifyRenderDirty();
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
            NotifyRenderDirty();
        }

        protected virtual void OnChildRemoved()
        {
            NotifyRenderDirty();
        }

        /// <summary>
        /// Walks up the parent chain to find the owning <see cref="Gumps.Gump"/> and
        /// bumps its render version so the gump's retained command cache will be
        /// rebuilt on the next frame. Called automatically from render-affecting
        /// property setters on this class; subclasses that add their own render-
        /// affecting properties should call it from their setters too.
        /// </summary>
        protected internal virtual void NotifyRenderDirty()
        {
            Control p = this;
            while (p != null)
            {
                if (p is Gumps.Gump gump)
                {
                    gump.InvalidateRenderCache();
                    return;
                }
                p = p._parent;
            }
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
        }
    }
}