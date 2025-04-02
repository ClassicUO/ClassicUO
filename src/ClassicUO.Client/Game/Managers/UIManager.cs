// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using ClassicUO.Game.Services;

namespace ClassicUO.Game.Managers
{
    internal static class UIManager
    {
        private static readonly Dictionary<uint, Point> _gumpPositionCache = new Dictionary<uint, Point>();
        private static readonly Control?[] _mouseDownControls = new Control[0xFF];
        private static readonly UOService _uoService = ServiceProvider.Get<UOService>();
        private static readonly SceneService _sceneService = ServiceProvider.Get<SceneService>();

        //private static readonly Dictionary<uint, TargetLineGump> _targetLineGumps = new Dictionary<uint, TargetLineGump>();
        private static Point _dragOrigin;
        private static bool _isDraggingControl;
        private static Control? _keyboardFocusControl, _lastFocus;
        private static bool _needSort;


        public static float ContainerScale { get; set; } = 1f;

        public static AnchorManager AnchorManager { get; } = new AnchorManager();

        public static LinkedList<Gump> Gumps { get; } = new LinkedList<Gump>();

        public static Control? MouseOverControl { get; private set; }

        public static bool IsModalOpen { get; private set; }

        public static bool IsMouseOverWorld
        {
            get
            {
                Point mouse = Mouse.Position;
                Profile profile = ProfileManager.CurrentProfile;

                return profile != null &&
                    _uoService.GameCursor.AllowDrawSDLCursor &&
                    DraggingControl == null &&
                    MouseOverControl == null &&
                    !IsModalOpen &&
                    (_sceneService.Scene?.Camera.Bounds.Contains(mouse) ?? false);
            }
        }

        public static Control? DraggingControl { get; private set; }

        public static SystemChatControl? SystemChat { get; set; }

        public static PopupMenuGump? PopupMenu { get; private set; }

        public static Control? KeyboardFocusControl
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

        public static ContextMenuShowMenu? ContextMenu { get; private set; }

        public static void ShowGamePopup(PopupMenuGump? popup)
        {
            PopupMenu?.Dispose();
            PopupMenu = popup;

            if (popup == null || popup.IsDisposed)
            {
                return;
            }

            if (PopupMenu != null)
                Add(PopupMenu);
        }


        public static bool IsModalControlOpen()
        {
            foreach (Gump control in Gumps)
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

            var mouseDownControl = _mouseDownControls[(int)MouseButtonType.Left];
            if (mouseDownControl != null)
            {
                if (ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.HoldAltToMoveGumps || Keyboard.Alt)
                {
                    AttemptDragControl(mouseDownControl, true);
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

                _mouseDownControls[(int) button] = MouseOverControl;
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

            var index = (int) button;
            ref var mouseOverControl = ref _mouseDownControls[index];

            if (MouseOverControl != null)
            {
                if (mouseOverControl != null && MouseOverControl == mouseOverControl || _uoService.GameCursor.ItemHold.Enabled)
                {
                    MouseOverControl.InvokeMouseUp(Mouse.Position, button);
                }
                else if (mouseOverControl != null && MouseOverControl != mouseOverControl)
                {
                    if (!mouseOverControl.IsDisposed)
                    {
                        mouseOverControl?.InvokeMouseUp(Mouse.Position, button);
                    }
                }
            }
            else if (mouseOverControl != null && !mouseOverControl.IsDisposed)
            {
                mouseOverControl?.InvokeMouseUp(Mouse.Position, button);
            }

            if (button == MouseButtonType.Right)
            {
                var mouseDownControl = mouseOverControl;
                // only attempt to close the gump if the mouse is still on the gump when right click mouse up occurs
                if(mouseDownControl != null && MouseOverControl == mouseDownControl)
                {
                    mouseDownControl.InvokeMouseCloseGumpWithRClick();
                }
            }

            mouseOverControl = null;
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
                        _uoService.World.DelayedObjectClickManager.Clear();
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

        public static Control? LastControlMouseDown(MouseButtonType button)
        {
            return _mouseDownControls[(int) button];
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

        public static void ShowContextMenu(ContextMenuShowMenu? menu)
        {
            ContextMenu?.Dispose();

            ContextMenu = menu;

            if (ContextMenu == null || menu == null || menu.IsDisposed)
            {
                return;
            }

            Add(ContextMenu);
        }

        public static T? GetGump<T>(uint? serial = null) where T : Control
        {
            if (serial.HasValue)
            {
                for (var last = Gumps.Last; last != null; last = last.Previous)
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
                for (var first = Gumps.First; first != null; first = first.Next)
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

        public static Gump? GetGump(uint serial)
        {
            for (var last = Gumps.Last; last != null; last = last.Previous)
            {
                var c = last.Value;

                if (!c.IsDisposed && c.LocalSerial == serial)
                {
                    return c;
                }
            }

            return null;
        }

        public static TradingGump? GetTradingGump(uint serial)
        {
            for (var g = Gumps.Last; g != null; g = g.Previous)
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

            var first = Gumps.First;

            while (first != null)
            {
                var next = first.Next;
                var g = first.Value;

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

        public static void Draw(UltimaBatcher2D batcher)
        {
            SortControlsByInfo();

            batcher.Begin();

            for (var last = Gumps.Last; last != null; last = last.Previous)
            {
                var g = last.Value;
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
                    for (var first = Gumps.First; first != null; first = first.Next)
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
            var gump = GetMouseOverControl(Mouse.Position);

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

        private static Control? GetMouseOverControl(Point position)
        {
            if (_isDraggingControl)
            {
                return DraggingControl;
            }

            Control? control = null;

            IsModalOpen = IsModalControlOpen();

            for (var first = Gumps.First; first != null; first = first.Next)
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
            var gump = control as Gump;
            if (gump == null && control?.RootParent is Gump)
            {
                gump = control.RootParent as Gump;
            }

            if (gump != null)
            {
                for (var start = Gumps.First; start != null; start = start.Next)
                {
                    if (start.Value == gump)
                    {
                        if (gump.LayerOrder == UILayer.Under)
                        {
                            if (start != Gumps.Last)
                            {
                                Gumps.Remove(gump);
                                if (Gumps.Last != null)
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
                for (var el = Gumps.First; el != null; el = el.Next)
                {
                    Gump c = el.Value;

                    if (c.LayerOrder == UILayer.Default)
                    {
                        continue;
                    }

                    if (c.LayerOrder == UILayer.Under)
                    {
                        for (var first = Gumps.First; first != null; first = first.Next)
                        {
                            if (first.Value == c)
                            {
                                if (c != Gumps.Last?.Value)
                                {
                                    Gumps.Remove(first);
                                    if (Gumps.Last!= null)
                                        Gumps.AddBefore(Gumps.Last, first);
                                }
                            }
                        }
                    }
                    else if (c.LayerOrder == UILayer.Over)
                    {
                        for (var first = Gumps.First; first != null; first = first.Next)
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
            if ((_isDraggingControl && !attemptAlwaysSuccessful) || _uoService.GameCursor.ItemHold.Enabled && !_uoService.GameCursor.ItemHold.IsFixedPosition)
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

                     for (int i = 0; i < (int) MouseButtonType.Size; i++)
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