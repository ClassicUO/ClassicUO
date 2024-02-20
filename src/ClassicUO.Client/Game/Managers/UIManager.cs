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

using ClassicUO.Configuration;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace ClassicUO.Game.Managers
{
    public static class UIManager
    {
        private static readonly Dictionary<uint, Point> _gumpPositionCache = new Dictionary<uint, Point>();
        private static readonly Control[] _mouseDownControls = new Control[0xFF];


        //private static readonly Dictionary<uint, TargetLineGump> _targetLineGumps = new Dictionary<uint, TargetLineGump>();
        private static Point _dragOrigin;
        private static bool _isDraggingControl;
        private static Control _keyboardFocusControl, _lastFocus;
        private static bool _needSort;

        public static float ContainerScale { get; set; } = 1f;

        public static AnchorManager AnchorManager { get; } = new AnchorManager();

        public static LinkedList<Gump> Gumps { get; } = new LinkedList<Gump>();

        public static Control MouseOverControl { get; private set; }

        public static bool IsModalOpen { get; private set; }

        public static bool IsMouseOverWorld
        {
            get
            {
                Point mouse = Mouse.Position;
                Profile profile = ProfileManager.CurrentProfile;

                return profile != null &&
                    Client.Game.GameCursor.AllowDrawSDLCursor &&
                    DraggingControl == null &&
                    MouseOverControl == null &&
                    !IsModalOpen &&
                    Client.Game.Scene.Camera.Bounds.Contains(mouse);
            }
        }

        public static Control DraggingControl { get; private set; }

        public static SystemChatControl SystemChat { get; set; }

        public static PopupMenuGump PopupMenu { get; private set; }

        public static Control KeyboardFocusControl
        {
            get => _keyboardFocusControl;
            set
            {
                if (_keyboardFocusControl != value)
                {
                    _keyboardFocusControl?.OnFocusLost();
                    _keyboardFocusControl = value;

                    if (value != null && value.AcceptKeyboardInput)
                    {
                        if (!value.IsFocused)
                        {
                            value.OnFocusEnter();
                        }
                    }
                }
            }
        }

        public static bool IsDragging => _isDraggingControl && DraggingControl != null;

        public static ContextMenuShowMenu ContextMenu { get; private set; }

        public static void ShowGamePopup(PopupMenuGump popup)
        {
            PopupMenu?.Dispose();
            PopupMenu = popup;

            if (popup == null || popup.IsDisposed)
            {
                return;
            }

            Add(PopupMenu);
        }


        public static bool IsModalControlOpen()
        {
            for (LinkedListNode<Gump> last = Gumps.Last; last != null; last = last.Previous)
            {
                if (last.Value.IsModal)
                {
                    return true;
                }
            }

            return false;
        }


        public static void OnMouseDragging()
        {
            HandleMouseInput();

            if (_mouseDownControls[(int)MouseButtonType.Left] != null)
            {
                if (ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.HoldAltToMoveGumps || Keyboard.Alt)
                {
                    AttemptDragControl(_mouseDownControls[(int)MouseButtonType.Left], true);
                }
            }

            if (_isDraggingControl)
            {
                DoDragControl();
            }
        }

        public static void OnMouseButtonDown(MouseButtonType button)
        {
            HandleMouseInput();

            if (MouseOverControl != null)
            {
                if (MouseOverControl.IsEnabled && MouseOverControl.IsVisible)
                {
                    if (_lastFocus != MouseOverControl)
                    {
                        _lastFocus?.OnFocusLost();
                        MouseOverControl.OnFocusEnter();
                        _lastFocus = MouseOverControl;
                    }
                }

                MakeTopMostGump(MouseOverControl);
                MouseOverControl.InvokeMouseDown(Mouse.Position, button);

                if (MouseOverControl.AcceptKeyboardInput)
                {
                    _keyboardFocusControl = MouseOverControl;
                }

                _mouseDownControls[(int)button] = MouseOverControl;
            }
            else
            {
                foreach (Gump s in Gumps)
                {
                    if (s.IsModal && s.ModalClickOutsideAreaClosesThisControl)
                    {
                        s.Dispose();
                        Mouse.CancelDoubleClick = true;
                    }
                }
            }

            if (PopupMenu != null && !PopupMenu.Bounds.Contains(Mouse.Position.X, Mouse.Position.Y))
            {
                ShowGamePopup(null);
            }
        }

        public static void OnMouseButtonUp(MouseButtonType button)
        {
            EndDragControl(Mouse.Position);
            HandleMouseInput();

            int index = (int)button;

            if (MouseOverControl != null)
            {
                if (_mouseDownControls[index] != null && MouseOverControl == _mouseDownControls[index] || Client.Game.GameCursor.ItemHold.Enabled)
                {
                    MouseOverControl.InvokeMouseUp(Mouse.Position, button);
                }
                else if (_mouseDownControls[index] != null && MouseOverControl != _mouseDownControls[index])
                {
                    if (!_mouseDownControls[index].IsDisposed)
                    {
                        _mouseDownControls[index].InvokeMouseUp(Mouse.Position, button);
                    }
                }
            }
            else if (_mouseDownControls[index] != null && !_mouseDownControls[index].IsDisposed)
            {
                _mouseDownControls[index].InvokeMouseUp(Mouse.Position, button);
            }

            if (button == MouseButtonType.Right)
            {
                var mouseDownControl = _mouseDownControls[index];
                // only attempt to close the gump if the mouse is still on the gump when right click mouse up occurs
                if (mouseDownControl != null && MouseOverControl == mouseDownControl)
                {
                    mouseDownControl.InvokeMouseCloseGumpWithRClick();
                }
            }

            _mouseDownControls[index] = null;
        }

        public static bool OnMouseDoubleClick(MouseButtonType button)
        {
            HandleMouseInput();

            if (MouseOverControl != null)
            {
                if (MouseOverControl.InvokeMouseDoubleClick(Mouse.Position, button))
                {
                    if (button == MouseButtonType.Left)
                    {
                        DelayedObjectClickManager.Clear();
                    }

                    return true;
                }
            }

            return false;
        }

        public static void OnMouseWheel(bool isup)
        {
            if (MouseOverControl != null && MouseOverControl.AcceptMouseInput)
            {
                MouseOverControl.InvokeMouseWheel(isup ? MouseEventType.WheelScrollUp : MouseEventType.WheelScrollDown);
            }
        }

        public static Control LastControlMouseDown(MouseButtonType button)
        {
            return _mouseDownControls[(int)button];
        }

        public static void SavePosition(uint serverSerial, Point point)
        {
            _gumpPositionCache[serverSerial] = point;
        }

        public static bool RemovePosition(uint serverSerial)
        {
            return _gumpPositionCache.Remove(serverSerial);
        }

        public static bool GetGumpCachePosition(uint id, out Point pos)
        {
            return _gumpPositionCache.TryGetValue(id, out pos);
        }

        public static void ShowContextMenu(ContextMenuShowMenu menu)
        {
            ContextMenu?.Dispose();

            ContextMenu = menu;

            if (ContextMenu == null || menu.IsDisposed)
            {
                return;
            }

            Add(ContextMenu);
        }

        public static T GetGump<T>(uint? serial = null) where T : Control
        {
            if (serial.HasValue)
            {
                for (LinkedListNode<Gump> last = Gumps.Last; last != null; last = last.Previous)
                {
                    Control c = last.Value;

                    if (!c.IsDisposed && c.LocalSerial == serial.Value && c is T t)
                    {
                        return t;
                    }
                }
            }
            else
            {
                for (LinkedListNode<Gump> first = Gumps.First; first != null; first = first.Next)
                {
                    Control c = first.Value;

                    if (!c.IsDisposed && c is T t)
                    {
                        return t;
                    }
                }
            }

            return null;
        }

        public static Gump GetGump(uint serial)
        {
            for (LinkedListNode<Gump> last = Gumps.Last; last != null; last = last.Previous)
            {
                Control c = last.Value;

                if (!c.IsDisposed && c.LocalSerial == serial)
                {
                    return c as Gump;
                }
            }

            return null;
        }

        public static TradingGump GetTradingGump(uint serial)
        {
            for (LinkedListNode<Gump> g = Gumps.Last; g != null; g = g.Previous)
            {
                if (g.Value != null && !g.Value.IsDisposed && g.Value is TradingGump trading && (trading.ID1 == serial || trading.ID2 == serial || trading.LocalSerial == serial))
                {
                    return trading;
                }
            }

            return null;
        }

        public static void Update()
        {
            SortControlsByInfo();

            LinkedListNode<Gump> first = Gumps.First;

            while (first != null)
            {
                LinkedListNode<Gump> next = first.Next;

                Control g = first.Value;

                g.Update();

                if (g.IsDisposed)
                {
                    Gumps.Remove(first);
                }

                first = next;
            }

            HandleKeyboardInput();
            HandleMouseInput();
        }

        public static void SlowUpdate()
        {
            SortControlsByInfo();

            LinkedListNode<Gump> first = Gumps.First;

            while (first != null)
            {
                LinkedListNode<Gump> next = first.Next;

                Control g = first.Value;

                g.SlowUpdate();

                if (g.IsDisposed)
                {
                    Gumps.Remove(first);
                }

                first = next;
            }
        }

        public static void Draw(UltimaBatcher2D batcher)
        {
            SortControlsByInfo();

            batcher.Begin();

            for (LinkedListNode<Gump> last = Gumps.Last; last != null; last = last.Previous)
            {
                Control g = last.Value;
                g.Draw(batcher, g.X, g.Y);
            }

            batcher.End();
        }

        public static void Add(Gump gump, bool front = true)
        {
            if (!gump.IsDisposed)
            {
                if (front)
                {
                    Gumps.AddFirst(gump);
                }
                else
                {
                    Gumps.AddLast(gump);
                }

                _needSort = Gumps.Count > 1;
            }
        }

        public static void Clear()
        {
            foreach (Gump s in Gumps)
            {
                s.Dispose();
            }
        }


        private static void HandleKeyboardInput()
        {
            if (_keyboardFocusControl != null && _keyboardFocusControl.IsDisposed)
            {
                _keyboardFocusControl = null;
            }

            if (_keyboardFocusControl == null)
            {
                if (SystemChat != null && !SystemChat.IsDisposed)
                {
                    _keyboardFocusControl = SystemChat.TextBoxControl;
                    _keyboardFocusControl.OnFocusEnter();
                }
                else
                {
                    for (LinkedListNode<Gump> first = Gumps.First; first != null; first = first.Next)
                    {
                        Control c = first.Value;

                        if (!c.IsDisposed && c.IsVisible && c.IsEnabled)
                        {
                            _keyboardFocusControl = c.GetFirstControlAcceptKeyboardInput();

                            if (_keyboardFocusControl != null)
                            {
                                _keyboardFocusControl.OnFocusEnter();

                                break;
                            }
                        }
                    }
                }
            }
        }

        private static void HandleMouseInput()
        {
            Control gump = GetMouseOverControl(Mouse.Position);

            if (MouseOverControl != null && gump != MouseOverControl)
            {
                MouseOverControl.InvokeMouseExit(Mouse.Position);

                if (MouseOverControl.RootParent != null)
                {
                    if (gump == null || gump.RootParent != MouseOverControl.RootParent)
                    {
                        MouseOverControl.RootParent.InvokeMouseExit(Mouse.Position);
                    }
                }
            }

            if (gump != null)
            {
                if (gump != MouseOverControl)
                {
                    gump.InvokeMouseEnter(Mouse.Position);

                    if (gump.RootParent != null)
                    {
                        if (MouseOverControl == null || gump.RootParent != MouseOverControl.RootParent)
                        {
                            gump.RootParent.InvokeMouseEnter(Mouse.Position);
                        }
                    }
                }

                gump.InvokeMouseOver(Mouse.Position);
            }

            MouseOverControl = gump;

            //for (int i = 0; i < (int) MouseButtonType.Size; i++)
            //{
            //    if (_mouseDownControls[i] != null && _mouseDownControls[i] != gump)
            //    {
            //        _mouseDownControls[i].InvokeMouseOver(Mouse.Position);
            //    }
            //}
        }

        private static Control GetMouseOverControl(Point position)
        {
            if (_isDraggingControl)
            {
                return DraggingControl;
            }

            Control control = null;

            IsModalOpen = IsModalControlOpen();

            for (LinkedListNode<Gump> first = Gumps.First; first != null; first = first.Next)
            {
                Control c = first.Value;

                if (IsModalOpen && !c.IsModal || !c.IsVisible || !c.IsEnabled)
                {
                    continue;
                }

                c.HitTest(position, ref control);

                if (control != null)
                {
                    return control;
                }
            }

            return null;
        }

        public static void MakeTopMostGump(Control control)
        {
            Gump gump = control as Gump;
            if (gump == null && control?.RootParent is Gump)
            {
                gump = control.RootParent as Gump;
            }

            if (gump != null)
            {
                for (LinkedListNode<Gump> start = Gumps.First; start != null; start = start.Next)
                {
                    if (start.Value == gump)
                    {
                        if (gump.LayerOrder == UILayer.Under)
                        {
                            if (start != Gumps.Last)
                            {
                                Gumps.Remove(gump);
                                Gumps.AddBefore(Gumps.Last, start);
                            }
                        }
                        else
                        {
                            Gumps.Remove(gump);
                            Gumps.AddFirst(start);
                        }

                        break;
                    }
                }

                _needSort = Gumps.Count > 1;
            }
        }

        private static void SortControlsByInfo()
        {
            if (_needSort)
            {
                for (LinkedListNode<Gump> el = Gumps.First; el != null; el = el.Next)
                {
                    Gump c = el.Value;

                    if (c.LayerOrder == UILayer.Default)
                    {
                        continue;
                    }

                    if (c.LayerOrder == UILayer.Under)
                    {
                        for (LinkedListNode<Gump> first = Gumps.First; first != null; first = first.Next)
                        {
                            if (first.Value == c)
                            {
                                if (c != Gumps.Last.Value)
                                {
                                    Gumps.Remove(first);
                                    Gumps.AddBefore(Gumps.Last, first);
                                }
                            }
                        }
                    }
                    else if (c.LayerOrder == UILayer.Over)
                    {
                        for (LinkedListNode<Gump> first = Gumps.First; first != null; first = first.Next)
                        {
                            if (first.Value == c)
                            {
                                Gumps.Remove(first);
                                Gumps.AddFirst(c);
                            }
                        }
                    }
                }

                _needSort = false;
            }
        }

        public static void AttemptDragControl(Control control, bool attemptAlwaysSuccessful = false)
        {
            if ((_isDraggingControl && !attemptAlwaysSuccessful) || Client.Game.GameCursor.ItemHold.Enabled && !Client.Game.GameCursor.ItemHold.IsFixedPosition)
            {
                return;
            }

            Control dragTarget = control;

            if (!dragTarget.CanMove)
            {
                return;
            }

            while (dragTarget.Parent != null)
            {
                dragTarget = dragTarget.Parent;
            }

            if (dragTarget.CanMove)
            {
                if (attemptAlwaysSuccessful || !_isDraggingControl)
                {
                    DraggingControl = dragTarget;
                    _dragOrigin = Mouse.LClickPosition;

                    for (int i = 0; i < (int)MouseButtonType.Size; i++)
                    {
                        _mouseDownControls[i] = null;
                    }
                }

                Point delta = Mouse.Position - _dragOrigin;

                if (attemptAlwaysSuccessful || delta != Point.Zero)
                {
                    _isDraggingControl = true;
                    dragTarget.InvokeDragBegin(delta);
                }
            }
        }

        private static void DoDragControl()
        {
            if (DraggingControl == null)
            {
                return;
            }

            Point delta = Mouse.Position - _dragOrigin;

            DraggingControl.X += delta.X;
            DraggingControl.Y += delta.Y;
            DraggingControl.InvokeMove(delta.X, delta.Y);
            _dragOrigin = Mouse.Position;
        }

        private static void EndDragControl(Point mousePosition)
        {
            if (_isDraggingControl)
            {
                DoDragControl();
            }

            DraggingControl?.InvokeDragEnd(mousePosition);
            DraggingControl = null;
            _isDraggingControl = false;
        }
    }
}