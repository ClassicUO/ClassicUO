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
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps
{
    public class UIManager
    {
        private readonly List<Gump> _gumps = new List<Gump>();
        private GumpControl _mouseOverControl, _keyboardFocusControl;
        private readonly GumpControl[] _mouseDownControls = new GumpControl[5];
        private readonly CursorRenderer _cursor;
        private readonly List<object> _inputBlockingObjects = new List<object>();

       
        public UIManager()
        {
            _cursor = new CursorRenderer(this);
        }


        public IReadOnlyList<Gump> Gumps => _gumps;

        public GumpControl MouseOverControl => _mouseOverControl;
        public bool IsMouseOverUI => MouseOverControl != null;
        public bool IsMouseOverWorld => IsMouseOverUI && MouseOverControl is WorldViewport;

        public GumpControl KeyboardFocusControl
        {
            get
            {
                if (_keyboardFocusControl == null)
                {
                    foreach (var c in _gumps)
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
                foreach (var c in _gumps)
                {
                    if (c.ControlInfo.IsModal)
                    {
                        return true;
                    }
                }
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
            List<string> pieces = new List<string>();
            int index = 0;
            Gump gump = new Gump(sender, gumpID)
            {
                X = x,
                Y = y,
                CanMove = true,
                CanCloseWithRightClick = true,
                CanCloseWithEsc = true,
            };

            int group = 0;

            while (index < layout.Length)
            {
                if (layout.Substring(index) == "\0")
                {
                    break;
                }

                int begin = layout.IndexOf("{", index);
                int end = layout.IndexOf("}", index + 1);

                if (begin != -1 && end != -1)
                {
                    string sub = layout.Substring(begin + 1, end - begin - 1).Trim();
                    pieces.Add(sub);
                    index = end;

                    string[] gparams = Regex.Split(sub, @"\s+");

                    switch (gparams[0].ToLower())
                    {
                        case "button":
                            gump.AddChildren(new Button(gparams));
                            break;
                        case "buttontileart":
                            gump.AddChildren(new Button(gparams));
                            gump.AddChildren(new StaticPic(Graphic.Parse(gparams[8]), Hue.Parse(gparams[9]))
                            {
                                X = int.Parse(gparams[1]) + int.Parse(gparams[10]),
                                Y = int.Parse(gparams[2]) + int.Parse(gparams[11])
                            });
                            break;
                        case "checkertrans":
                            gump.AddChildren(new CheckerTrans(gparams));
                            break;
                        case "croppedtext":
                            gump.AddChildren(new CroppedText(gparams, lines));
                            break;
                        case "gumppic":
                            gump.AddChildren(new GumpPic(gparams));
                            break;
                        case "gumppictiled":
                            gump.AddChildren(new GumpPicTiled(gparams));
                            break;
                        case "htmlgump":
                            gump.AddChildren(new HtmlGump(gparams, lines));
                            break;
                        case "xmfhtmlgump":
                            gump.AddChildren(new HtmlGump(int.Parse(gparams[1]), int.Parse(gparams[2]), int.Parse(gparams[3]), int.Parse(gparams[4]),
                                IO.Resources.Cliloc.GetString(int.Parse(gparams[5])), int.Parse(gparams[6]), int.Parse(gparams[7]), 0, true));
                            break;
                        case "xmfhtmlgumpcolor":
                            int color = int.Parse(gparams[8]);
                            if (color == 0x7FFF)
                                color = 0x00FFFFFF;
                            gump.AddChildren(new HtmlGump(int.Parse(gparams[1]), int.Parse(gparams[2]), int.Parse(gparams[3]), int.Parse(gparams[4]),
                               IO.Resources.Cliloc.GetString(int.Parse(gparams[5])), int.Parse(gparams[6]), int.Parse(gparams[7]), color, true));
                            break;
                        case "xmfhtmltok":
                            color = int.Parse(gparams[7]);
                            if (color == 0x7FFF)
                                color = 0x00FFFFFF;

                            gump.AddChildren(new HtmlGump(int.Parse(gparams[1]), int.Parse(gparams[2]), int.Parse(gparams[3]), int.Parse(gparams[4]),
                              IO.Resources.Cliloc.GetString(int.Parse(gparams[8])), int.Parse(gparams[5]), int.Parse(gparams[6]), color, true));
                            break;
                        case "page":
                            break;
                        case "resizepic":
                            gump.AddChildren(new ResizePic(gparams));
                            break;
                        case "text":
                            gump.AddChildren(new Label(gparams, lines));
                            break;
                        case "textentrylimited":
                        case "textentry":
                            gump.AddChildren(new TextBox(gparams, lines));
                            break;
                        case "tilepichue":
                        case "tilepic":
                            gump.AddChildren(new StaticPic(gparams));
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
                            gump.AddChildren(new RadioButton(group, gparams, lines));
                            break;
                        case "checkbox":
                            gump.AddChildren(new Checkbox(gparams, lines));
                            break;
                        case "tooltip":
                            break;
                        case "noresize":
                            break;
                        default:
                            break;
                    }

                }
                else
                {
                    break;
                }
            }


            _gumps.Add(gump);
            return gump;
        }

        public T Get<T>() where T : Gump
            => _gumps.OfType<T>().FirstOrDefault();


        public void Update(double totalMS, double frameMS)
        {
            SortControlsByInfo();

            for (int i = 0; i < _gumps.Count; i++)
            {
                var g = _gumps[i];

                if (!g.IsInitialize && !g.IsDisposed)
                    g.Initialize();

                g.Update(totalMS, frameMS);

                if (g.IsDisposed)
                    _gumps.RemoveAt(i--);
            }

            HandleKeyboardInput();
            HandleMouseInput();

            _cursor.Update(totalMS, frameMS);
        }

        public void Draw(SpriteBatchUI spriteBatch)
        {
            SortControlsByInfo();

            for (int i = _gumps.Count - 1; i >= 0; i--)
            {
                var g = _gumps[i];

                g.Draw(spriteBatch, new Vector3(g.X, g.Y, 0));
            }

            _cursor.Draw(spriteBatch);
        }


        public void Add(Gump gump) => _gumps.Add(gump);

        private void HandleKeyboardInput()
        {
            if (KeyboardFocusControl != null)
            {
                if (_keyboardFocusControl.IsDisposed)
                    _keyboardFocusControl = null;
                else
                {
                    var events = Service.Get<InputManager>().GetKeyboardEvents();

                    foreach (var e in events)
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
            GumpControl gump = null;
            var inputManager = Service.Get<InputManager>();
            var position = inputManager.MousePosition;

            if (_isDraggingControl)
                gump = _draggingControl;
            else
            {
                for (int i = 0; i < _gumps.Count; i++)
                {
                    gump = HitTest(_gumps[i], position);
                    if (gump != null)
                        break;
                }
            }

            if (_mouseOverControl != null && gump != _mouseOverControl)
            {
                _mouseOverControl.InvokeMouseLeft(position);

                if (_mouseOverControl.Parent != null && ( gump == null || gump.RootParent != _mouseOverControl.RootParent ))
                    _mouseOverControl.InvokeMouseLeft(position);
            }


            if (gump != null)
            {
                gump.InvokeMouseEnter(position);
                if (_mouseDownControls[0] == gump)
                    AttemptDragControl(gump, position);

                if (_isDraggingControl)
                    DoDragControl(position);
            }

            _mouseOverControl = gump;

            for (int i = 0; i < 5; i++)
            {
                if (_mouseDownControls[i] != null && _mouseDownControls[i] != gump)
                    _mouseDownControls[i].InvokeMouseEnter(position);
            }

            if (!!IsModalControlOpen && ObjectsBlockingInputExists)
                return;

            var events = inputManager.GetMouseEvents();

            foreach (var e in events)
            {
                switch (e.EventType)
                {
                    case MouseEvent.Down:
                        if (gump != null)
                        {
                            MakeTopMostGump(gump);
                            gump.InvokeMouseDown(e.Position, e.Button);

                            _mouseDownControls[(int)e.Button] = gump;
                        }
                        else
                        {
                            if (IsModalControlOpen)
                            {
                                _gumps.ForEach(s => { if (s.ControlInfo.IsModal && s.ControlInfo.ModalClickOutsideAreaClosesThisControl) s.Dispose(); });
                            }
                        }
                        break;
                    case MouseEvent.Up:

                        int btn = (int)e.Button;

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

        private GumpControl HitTest(GumpControl parent, Point position)
        {
            var p = parent?.HitTest(position);
            if (p != null && p.Length > 0)
                return p[0];
            return null;
        }

        private void MakeTopMostGump(GumpControl control)
        {
            var c = control.RootParent;

            for (int i = 0; i < _gumps.Count; i++)
            {
                if (_gumps[i] == c)
                {
                    var cm = _gumps[i];
                    _gumps.RemoveAt(i);
                    _gumps.Insert(0, cm);
                }
            }
        }

        private void SortControlsByInfo()
        {
            List<Gump> gumps = _gumps.Where(s => s.ControlInfo.Layer != UILayer.Default).ToList();

            foreach (var c in gumps)
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
                        _mouseDownControls[i].InvokeMouseUp(mousePosition, (MouseButton)i);
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
