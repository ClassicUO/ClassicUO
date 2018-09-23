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
using System.Text.RegularExpressions;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps
{
    public class UIManager
    {
        private readonly List<GumpControl> _gumps = new List<GumpControl>();
        private GumpControl _keyboardFocusControl;
        private readonly GumpControl[] _mouseDownControls = new GumpControl[5];
        private readonly CursorRenderer _cursor;
        private readonly List<object> _inputBlockingObjects = new List<object>();


        public UIManager() => _cursor = new CursorRenderer(this);


        public IReadOnlyList<GumpControl> Gumps => _gumps;

        public GumpControl MouseOverControl { get; private set; }

        public bool IsMouseOverUI => MouseOverControl != null;
        public bool IsMouseOverWorld => IsMouseOverUI && MouseOverControl is WorldViewport;

        public GumpControl KeyboardFocusControl
        {
            get
            {
                if (_keyboardFocusControl == null)
                {
                    foreach (GumpControl c in _gumps)
                    {
                        if (!c.IsDisposed && c.IsVisible && c.IsEnabled && c.AcceptKeyboardInput)
                        {
                            _keyboardFocusControl = c.GetFirstControlAcceptKeyboardInput();
                            if (_keyboardFocusControl != null)
                                break;
                        }
                    }
                }

                return _keyboardFocusControl;
            }
            set => _keyboardFocusControl = value;
        }

        public bool IsModalControlOpen
        {
            get
            {
                foreach (GumpControl c in _gumps)
                    if (c.ControlInfo.IsModal) return true;

                return false;
            }
        }


        private bool ObjectsBlockingInputExists => _inputBlockingObjects.Count > 0;

        public void AddInputBlocker(object obj)
        {
            if (!_inputBlockingObjects.Contains(obj))
                _inputBlockingObjects.Add(obj);
        }

        public void RemoveInputBlocker(object obj)
        {
            if (_inputBlockingObjects.Contains(obj))
                _inputBlockingObjects.Remove(obj);
        }


        public GumpControl Create(Serial sender, Serial gumpID, int x, int y, string layout, string[] lines)
        {
            Gump gump = new Gump(sender, gumpID)
            {
                X = x,
                Y = y,
                CanMove = true,
                CanCloseWithRightClick = true,
                CanCloseWithEsc = true
            };

            int group = 0;
            int page = 0;
            int index = 0;

            while (index < layout.Length)
            {
                if (layout.Substring(index) == "\0") break;

                int begin = layout.IndexOf("{", index, StringComparison.Ordinal);
                int end = layout.IndexOf("}", index + 1, StringComparison.Ordinal);

                if (begin != -1 && end != -1)
                {
                    string sub = layout.Substring(begin + 1, end - begin - 1).Trim();
                    index = end;

                    string[] gparams = Regex.Split(sub, @"\s+");

                    switch (gparams[0].ToLower())
                    {
                        case "button":
                            gump.AddChildren(new Button(gparams), page);
                            break;
                        case "buttontileart":
                            gump.AddChildren(new Button(gparams), page);
                            gump.AddChildren(new StaticPic(Graphic.Parse(gparams[8]), Hue.Parse(gparams[9]))
                            {
                                X = int.Parse(gparams[1]) + int.Parse(gparams[10]),
                                Y = int.Parse(gparams[2]) + int.Parse(gparams[11])
                            }, page);
                            break;
                        case "checkertrans":

                            if (gump.Children.Count > 0)
                                gump.Children[gump.Children.Count - 1].IsTransparent = true;

                            //gump.AddChildren(new CheckerTrans(gparams));
                            break;
                        case "croppedtext":
                            gump.AddChildren(new CroppedText(gparams, lines), page);
                            break;
                        case "gumppic":
                            gump.AddChildren(new GumpPic(gparams), page);
                            break;
                        case "gumppictiled":
                            gump.AddChildren(new GumpPicTiled(gparams), page);
                            break;
                        case "htmlgump":
                            gump.AddChildren(new HtmlGump(gparams, lines), page);
                            break;
                        case "xmfhtmlgump":
                            gump.AddChildren(new HtmlGump(int.Parse(gparams[1]), int.Parse(gparams[2]),
                                int.Parse(gparams[3]), int.Parse(gparams[4]),
                                Cliloc.GetString(int.Parse(gparams[5])), int.Parse(gparams[6]), int.Parse(gparams[7]),
                                0, true), page);
                            break;
                        case "xmfhtmlgumpcolor":
                            int color = int.Parse(gparams[8]);
                            if (color == 0x7FFF)
                                color = 0x00FFFFFF;
                            gump.AddChildren(new HtmlGump(int.Parse(gparams[1]), int.Parse(gparams[2]),
                                int.Parse(gparams[3]), int.Parse(gparams[4]),
                                Cliloc.GetString(int.Parse(gparams[5])), int.Parse(gparams[6]), int.Parse(gparams[7]),
                                color, true), page);
                            break;
                        case "xmfhtmltok":
                            color = int.Parse(gparams[7]);
                            if (color == 0x7FFF)
                                color = 0x00FFFFFF;

                            gump.AddChildren(new HtmlGump(int.Parse(gparams[1]), int.Parse(gparams[2]),
                                int.Parse(gparams[3]), int.Parse(gparams[4]),
                                Cliloc.GetString(int.Parse(gparams[8])), int.Parse(gparams[5]), int.Parse(gparams[6]),
                                color, true), page);
                            break;
                        case "page":
                            page = int.Parse(gparams[1]);
                            break;
                        case "resizepic":
                            gump.AddChildren(new ResizePic(gparams), page);
                            break;
                        case "text":
                            gump.AddChildren(new Label(gparams, lines), page);
                            break;
                        case "textentrylimited":
                        case "textentry":
                            gump.AddChildren(new TextBox(gparams, lines), page);
                            break;
                        case "tilepichue":
                        case "tilepic":
                            gump.AddChildren(new StaticPic(gparams), page);
                            break;
                        case "noclose":
                            gump.CanCloseWithRightClick = false;
                            break;
                        case "nodispose":
                            gump.CanCloseWithEsc = false;
                            break;
                        case "nomove":
                            gump.CanMove = false;
                            break;
                        case "group":
                        case "endgroup":
                            group++;
                            break;
                        case "radio":
                            gump.AddChildren(new RadioButton(group, gparams, lines), page);
                            break;
                        case "checkbox":
                            gump.AddChildren(new Checkbox(gparams, lines), page);
                            break;
                        case "tooltip":
                            break;
                        case "noresize":
                            break;
                    }
                }
                else
                    break;
            }


            Add(gump);
            return gump;
        }

        public T Get<T>(Serial? serial = null) where T : GumpControl
            => _gumps.OfType<T>()
                .FirstOrDefault(s => !s.IsDisposed && (!serial.HasValue || s.LocalSerial == serial));

        public Gump Get(Serial serial)
            => _gumps.OfType<Gump>().FirstOrDefault(s => !s.IsDisposed && s.ServerSerial == serial);


        public void Update(double totalMS, double frameMS)
        {
            SortControlsByInfo();

            for (int i = 0; i < _gumps.Count; i++)
            {
                GumpControl g = _gumps[i];

                if (!g.IsInitialized && !g.IsDisposed)
                    g.Initialize();

                g.Update(totalMS, frameMS);
            }

            for (int i = 0; i < _gumps.Count; i++)
                if (_gumps[i].IsDisposed) _gumps.RemoveAt(i--);

            _cursor.Update(totalMS, frameMS);


            HandleKeyboardInput();
            HandleMouseInput();
        }

        public void Draw(SpriteBatchUI spriteBatch)
        {
            SortControlsByInfo();

            for (int i = _gumps.Count - 1; i >= 0; i--)
            {
                GumpControl g = _gumps[i];
                if (g.IsInitialized)
                    g.Draw(spriteBatch, new Vector3(g.X, g.Y, 0));
            }

            _cursor.Draw(spriteBatch);
        }


        public void Add(GumpControl gump)
        {
            if (gump.IsDisposed)
                return;

            _gumps.Insert(0, gump);
        }

        public void Remove<T>(Serial? local = null) where T : GumpControl
        {
            foreach (GumpControl c in _gumps)
            {
                if (typeof(T).IsAssignableFrom(c.GetType()))
                {
                    if (!local.HasValue || c.LocalSerial == local)
                    {
                        if (!c.IsDisposed)
                            c.Dispose();
                    }
                }
            }
        }

        private void HandleKeyboardInput()
        {
            if (KeyboardFocusControl != null)
            {
                if (_keyboardFocusControl.IsDisposed)
                    _keyboardFocusControl = null;
                else
                {
                    IEnumerable<InputKeyboardEvent> events = Service.Get<InputManager>().GetKeyboardEvents();

                    foreach (InputKeyboardEvent e in events)
                    {
                        switch (e.EventType)
                        {
                            case KeyboardEvent.Press:
                            case KeyboardEvent.Down:
                                _keyboardFocusControl.InvokeKeyDown(e.KeyCode, e.Mod);
                                break;
                            case KeyboardEvent.Up:
                                _keyboardFocusControl.InvokeKeyUp(e.KeyCode, e.Mod);
                                break;
                            case KeyboardEvent.TextInput:
                                _keyboardFocusControl.InvokeTextInput(e.KeyChar);
                                break;
                        }
                    }
                }
            }
        }

        private void HandleMouseInput()
        {
            InputManager inputManager = Service.Get<InputManager>();
            Point position = inputManager.MousePosition;

            GumpControl gump = GetMouseOverControl(position);

            if (MouseOverControl != null && gump != MouseOverControl)
            {
                MouseOverControl.InvokeMouseLeft(position);

                if (MouseOverControl.Parent != null && (gump == null || gump.RootParent != MouseOverControl.RootParent))
                    MouseOverControl.InvokeMouseLeft(position);
            }


            if (gump != null)
            {
                gump.InvokeMouseEnter(position);
                if (_mouseDownControls[0] == gump)
                    AttemptDragControl(gump, position);

                if (_isDraggingControl)
                    DoDragControl(position);
            }

            MouseOverControl = gump;

            for (int i = 0; i < 5; i++)
            {
                if (_mouseDownControls[i] != null && _mouseDownControls[i] != gump)
                    _mouseDownControls[i].InvokeMouseEnter(position);
            }

            if (!IsModalControlOpen && ObjectsBlockingInputExists)
                return;

            IEnumerable<InputMouseEvent> events = inputManager.GetMouseEvents();

            foreach (InputMouseEvent e in events)
            {
                switch (e.EventType)
                {
                    case MouseEvent.WheelScrollDown:
                    case MouseEvent.WheelScrollUp:
                        if (gump != null)
                        {
                            if (gump.AcceptMouseInput)
                                gump.InvokeMouseWheel(e.EventType);
                        }
                        break;
                    case MouseEvent.Down:
                        if (gump != null)
                        {
                            MakeTopMostGump(gump);
                            gump.InvokeMouseDown(e.Position, e.Button);
                            if (gump.AcceptKeyboardInput)
                                _keyboardFocusControl = gump;
                            _mouseDownControls[(int) e.Button] = gump;
                        }
                        else
                        {
                            if (IsModalControlOpen)
                            {
                                _gumps.ForEach(s =>
                                {
                                    if (s.ControlInfo.IsModal && s.ControlInfo.ModalClickOutsideAreaClosesThisControl)
                                        s.Dispose();
                                });
                            }
                        }

                        break;
                    case MouseEvent.Up:

                        int btn = (int) e.Button;

                        EndDragControl(e.Position);

                        if (gump != null)
                        {
                            if (_mouseDownControls[btn] != null && gump == _mouseDownControls[btn])
                                gump.InvokeMouseClick(position, e.Button);

                            gump.InvokeMouseUp(position, e.Button);

                            if (_mouseDownControls[btn] != null && gump != _mouseDownControls[btn])
                                _mouseDownControls[btn].InvokeMouseUp(position, e.Button);
                        }
                        else
                        {
                            if (_mouseDownControls[btn] != null)
                                _mouseDownControls[btn].InvokeMouseUp(position, e.Button);
                        }

                        _mouseDownControls[btn] = null;
                        break;
                }
            }
        }

        private GumpControl GetMouseOverControl(Point position)
        {
            if (_isDraggingControl)
                return _draggingControl;

            List<GumpControl> controls;

            if (IsModalControlOpen)
            {
                controls = new List<GumpControl>();
                controls = _gumps.Where(s => s.ControlInfo.IsModal).ToList();
            }
            else
                controls = _gumps;

            GumpControl[] mouseoverControls = null;

            foreach (GumpControl c in controls)
            {
                GumpControl[] ctrls = c.HitTest(position);
                if (ctrls != null)
                {
                    mouseoverControls = ctrls;
                    break;
                }
            }

            if (mouseoverControls == null)
                return null;

            for (int i = 0; i < mouseoverControls.Length; i++)
            {
                if (mouseoverControls[i].AcceptMouseInput)
                    return mouseoverControls[i];
            }

            return null;
        }

        private void MakeTopMostGump(GumpControl control)
        {
            GumpControl c = control.RootParent;

            for (int i = 0; i < _gumps.Count; i++)
            {
                if (_gumps[i] == c)
                {
                    GumpControl cm = _gumps[i];
                    _gumps.RemoveAt(i);
                    _gumps.Insert(0, cm);
                }
            }
        }

        private void SortControlsByInfo()
        {
            List<GumpControl> gumps = _gumps.Where(s => s.ControlInfo.Layer != UILayer.Default).ToList();

            foreach (GumpControl c in gumps)
            {
                if (c.ControlInfo.Layer == UILayer.Under)
                {
                    for (int i = 0; i < _gumps.Count; i++)
                    {
                        if (_gumps[i] == c)
                        {
                            _gumps.RemoveAt(i);
                            _gumps.Insert(_gumps.Count, c);
                        }
                    }
                }
                else if (c.ControlInfo.Layer == UILayer.Over)
                {
                    for (int i = 0; i < _gumps.Count; i++)
                    {
                        if (_gumps[i] == c)
                        {
                            _gumps.RemoveAt(i);
                            _gumps.Insert(0, c);
                        }
                    }
                }
            }
        }

        private GumpControl _draggingControl;
        private bool _isDraggingControl;
        private int _dragOriginX, _dragOriginY;

        public void AttemptDragControl(GumpControl control, Point mousePosition, bool attemptAlwaysSuccessful = false)
        {
            if (_isDraggingControl)
                return;

            GumpControl dragTarget = control;
            if (!dragTarget.CanMove)
                return;

            dragTarget = dragTarget.RootParent;

            if (dragTarget.CanMove)
            {
                if (attemptAlwaysSuccessful)
                {
                    _draggingControl = dragTarget;
                    _dragOriginX = mousePosition.X;
                    _dragOriginY = mousePosition.Y;
                }

                if (_draggingControl == dragTarget)
                {
                    int deltaX = mousePosition.X - _dragOriginX;
                    int deltaY = mousePosition.Y - _dragOriginY;

                    if (attemptAlwaysSuccessful || Math.Abs(deltaX) + Math.Abs(deltaY) > 2)
                        _isDraggingControl = true;
                }
                else
                {
                    _draggingControl = dragTarget;
                    _dragOriginX = mousePosition.X;
                    _dragOriginY = mousePosition.Y;
                }
            }

            if (_isDraggingControl)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (_mouseDownControls[i] != null && _mouseDownControls[i] != _draggingControl)
                    {
                        _mouseDownControls[i].InvokeMouseUp(mousePosition, (MouseButton) i);
                        _mouseDownControls[i] = null;
                    }
                }
            }
        }

        private void DoDragControl(Point mousePosition)
        {
            if (_draggingControl == null)
                return;

            int deltaX = mousePosition.X - _dragOriginX;
            int deltaY = mousePosition.Y - _dragOriginY;
            _draggingControl.X = _draggingControl.X + deltaX;
            _draggingControl.Y = _draggingControl.Y + deltaY;
            _dragOriginX = mousePosition.X;
            _dragOriginY = mousePosition.Y;
        }

        private void EndDragControl(Point mousePosition)
        {
            if (_isDraggingControl)
                DoDragControl(mousePosition);
            _draggingControl = null;
            _isDraggingControl = false;
        }
    }
}