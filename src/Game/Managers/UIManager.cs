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
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Managers
{
    internal static class UIManager
    {
        private static readonly Dictionary<uint, Point> _gumpPositionCache = new Dictionary<uint, Point>();
        private static readonly Control[] _mouseDownControls = new Control[(int)MouseButtonType.Size];


        //private static readonly Dictionary<uint, TargetLineGump> _targetLineGumps = new Dictionary<uint, TargetLineGump>();
        private static int _dragOriginX, _dragOriginY;
        private static bool _isDraggingControl;
        private static Control _keyboardFocusControl, _lastFocus;
        private static bool _needSort;


        public static float ContainerScale { get; set; } = 1f;

        public static AnchorManager AnchorManager { get; } = new AnchorManager();

        public static LinkedList<Control> Gumps { get; } = new LinkedList<Control>();

        public static Control MouseOverControl { get; private set; }

        public static bool IsMouseOverAControl => MouseOverControl != null;

        public static bool IsModalOpen { get; private set; }

        public static bool IsMouseOverWorld
        {
            get
            {
                Point mouse = Mouse.Position;
                Profile profile = ProfileManager.Current;

                return profile != null &&
                       GameCursor.AllowDrawSDLCursor &&
                       DraggingControl == null &&
                       MouseOverControl == null &&
                       !IsModalOpen &&
                       mouse.X >= profile.GameWindowPosition.X + 5 &&
                       mouse.X < profile.GameWindowPosition.X + 5 + profile.GameWindowSize.X &&
                       mouse.Y >= profile.GameWindowPosition.Y + 5 &&
                       mouse.Y < profile.GameWindowPosition.Y + 5 + profile.GameWindowSize.Y;
            }
        }

        public static Control DraggingControl { get; private set; }

        public static GameCursor GameCursor { get; private set; }

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
            foreach (Control control in Gumps)
            {
                if (control.IsModal)
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
                if (ProfileManager.Current == null || !ProfileManager.Current.HoldAltToMoveGumps || Keyboard.Alt)
                {
                    AttemptDragControl(_mouseDownControls[(int)MouseButtonType.Left], Mouse.Position, true);
                }
            }

            if (_isDraggingControl)
            {
                DoDragControl(Mouse.Position);
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
                foreach (Control s in Gumps)
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

            _mouseDownControls[(int)button]?.InvokeMouseUp(Mouse.Position, button);
            if (button == MouseButtonType.Right)
            {
                _mouseDownControls[(int)button]?.InvokeMouseCloseGumpWithRClick();
            }

            _mouseDownControls[(int)button] = null;
        }

        public static bool OnMouseDoubleClick(MouseButtonType button)
        {
            HandleMouseInput();

            if (MouseOverControl != null)
            {
                if (MouseOverControl.InvokeMouseDoubleClick(Mouse.Position, button))
                {
                    if (button == MouseButtonType.Left)
                        DelayedObjectClickManager.Clear();

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
            return _mouseDownControls[(int) button];
        }


        public static void InitializeGameCursor()
        {
            GameCursor = new GameCursor();
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
                for (LinkedListNode<Control> last = Gumps.Last; last != null; last = last.Previous)
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
                for (LinkedListNode<Control> first = Gumps.First; first != null; first = first.Next)
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
            for (LinkedListNode<Control> last = Gumps.Last; last != null; last = last.Previous)
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
            for (LinkedListNode<Control> g = Gumps.Last; g != null; g = g.Previous)
            {
                if (g.Value != null && !g.Value.IsDisposed && g.Value is TradingGump trading && (trading.ID1 == serial || trading.ID2 == serial || trading.LocalSerial == serial))
                {
                    return trading;
                }
            }

            return null;
        }

        public static void Update(double totalMS, double frameMS)
        {
            SortControlsByInfo();

            LinkedListNode<Control> first = Gumps.First;

            while (first != null)
            {
                LinkedListNode<Control> next = first.Next;

                Control g = first.Value;

                g.Update(totalMS, frameMS);

                if (g.IsDisposed)
                {
                    Gumps.Remove(first);
                }

                first = next;
            }

            GameCursor?.Update(totalMS, frameMS);
            HandleKeyboardInput();
            HandleMouseInput();
        }

        public static void Draw(UltimaBatcher2D batcher)
        {
            SortControlsByInfo();

            batcher.Begin();

            for (LinkedListNode<Control> last = Gumps.Last; last != null; last = last.Previous)
            {
                Control g = last.Value;
                g.Draw(batcher, g.X, g.Y);
            }

            GameCursor?.Draw(batcher);

            batcher.End();
        }

        public static void Add(Control gump)
        {
            if (!gump.IsDisposed)
            {
                Gumps.AddFirst(gump);
                _needSort = Gumps.Count > 1;
            }
        }

        public static void Clear()
        {
            foreach (Control s in Gumps)
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
                    for (LinkedListNode<Control> first = Gumps.First; first != null; first = first.Next)
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

            for (int i = 0; i < (int)MouseButtonType.Size; i++)
            {
                if (_mouseDownControls[i] != null && _mouseDownControls[i] != gump)
                {
                    _mouseDownControls[i]
                        .InvokeMouseOver(Mouse.Position);
                }
            }
        }

        private static Control GetMouseOverControl(Point position)
        {
            if (_isDraggingControl)
            {
                return DraggingControl;
            }

            Control control = null;

            IsModalOpen = IsModalControlOpen();

            for (LinkedListNode<Control> first = Gumps.First; first != null; first = first.Next)
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
            Control c = control;

            while (c.Parent != null)
            {
                c = c.Parent;
            }

            LinkedListNode<Control> first = Gumps.First?.Next; // skip game window

            for (; first != null; first = first.Next)
            {
                if (first.Value == c)
                {
                    Gumps.Remove(first);
                    Gumps.AddFirst(first);
                    _needSort = Gumps.Count > 1;
                }
            }
        }

        private static void SortControlsByInfo()
        {
            if (_needSort)
            {
                for (LinkedListNode<Control> el = Gumps.First; el != null; el = el.Next)
                {
                    Control c = el.Value;

                    if (c.LayerOrder == UILayer.Default)
                    {
                        continue;
                    }

                    if (c.LayerOrder == UILayer.Under)
                    {
                        for (LinkedListNode<Control> first = Gumps.First; first != null; first = first.Next)
                        {
                            if (first.Value == c)
                            {
                                if (Gumps.Last != null)
                                {
                                    Gumps.Remove(first);
                                    Gumps.AddAfter(Gumps.Last, c);
                                }
                            }
                        }
                    }
                    else if (c.LayerOrder == UILayer.Over)
                    {
                        for (LinkedListNode<Control> first = Gumps.First; first != null; first = first.Next)
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

        public static void AttemptDragControl(Control control, Point mousePosition, bool attemptAlwaysSuccessful = false)
        {
            if (_isDraggingControl || ItemHold.Enabled && !ItemHold.IsFixedPosition)
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
                    _dragOriginX = Mouse.LDropPosition.X;
                    _dragOriginY = Mouse.LDropPosition.Y;

                    for (int i = 0; i < (int)MouseButtonType.Size; i++)
                    {
                        _mouseDownControls[i] = null;
                    }
                }

                int deltaX = mousePosition.X - _dragOriginX;
                int deltaY = mousePosition.Y - _dragOriginY;

                int delta = Math.Abs(deltaX) + Math.Abs(deltaY);

                if (attemptAlwaysSuccessful || delta > Constants.MIN_GUMP_DRAG_DISTANCE)
                {
                    _isDraggingControl = true;
                    dragTarget.InvokeDragBegin(new Point(deltaX, deltaY));
                }
            }
        }

        private static void DoDragControl(Point mousePosition)
        {
            if (DraggingControl == null)
            {
                return;
            }

            int deltaX = mousePosition.X - _dragOriginX;
            int deltaY = mousePosition.Y - _dragOriginY;

            DraggingControl.X = DraggingControl.X + deltaX;
            DraggingControl.Y = DraggingControl.Y + deltaY;
            DraggingControl.InvokeMove(deltaX, deltaY);
            _dragOriginX = mousePosition.X;
            _dragOriginY = mousePosition.Y;
        }

        private static void EndDragControl(Point mousePosition)
        {
            if (_isDraggingControl)
            {
                DoDragControl(mousePosition);
            }

            DraggingControl?.InvokeDragEnd(mousePosition);
            DraggingControl = null;
            _isDraggingControl = false;
        }
    }
}