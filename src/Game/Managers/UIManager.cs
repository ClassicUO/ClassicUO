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
using System.Text;
using System.Text.RegularExpressions;

using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Gumps.UIGumps;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Managers
{
    public sealed class UIManager
    {
        private readonly Dictionary<Serial, Point> _gumpPositionCache = new Dictionary<Serial, Point>();
        private readonly List<Control> _gumps = new List<Control>();
        //private readonly List<object> _inputBlockingObjects = new List<object>();
        private readonly Control[] _mouseDownControls = new Control[5];
        private Control _draggingControl;
        private int _dragOriginX, _dragOriginY;
        private bool _isDraggingControl;
        private Control _keyboardFocusControl;
        private bool _needSort;

        public UIManager()
        {
            GameCursor = new GameCursor();

            Engine.Input.MouseDragging += (sender, e) =>
            {
                if (_isDraggingControl)
                    DoDragControl(Mouse.Position);
            };

            Engine.Input.LeftMouseButtonDown += (sender, e) =>
            {
                //if (!IsModalControlOpen /*&& ObjectsBlockingInputExists*/)
                //    return;

                if (MouseOverControl != null)
                {
                    MakeTopMostGump(MouseOverControl);
                    MouseOverControl.InvokeMouseDown(Mouse.Position, MouseButton.Left);

                    if (MouseOverControl.AcceptKeyboardInput)
                        _keyboardFocusControl = MouseOverControl;
                    _mouseDownControls[(int) MouseButton.Left] = MouseOverControl;
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
            };

            Control lastLeftUp = null, lastRight = null;

            Engine.Input.LeftMouseButtonUp += (sender, e) =>
            {
                //if (!IsModalControlOpen && ObjectsBlockingInputExists)
                //    return;

                //if (MouseOverControl == null)
                //    return;
                const int btn = (int) MouseButton.Left;
                EndDragControl(Mouse.Position);

                if (MouseOverControl != null)
                {
                    if (_mouseDownControls[btn] != null && MouseOverControl == _mouseDownControls[btn])
                        MouseOverControl.InvokeMouseClick(Mouse.Position, MouseButton.Left);
                    MouseOverControl.InvokeMouseUp(Mouse.Position, MouseButton.Left);

                    if (_mouseDownControls[btn] != null && MouseOverControl != _mouseDownControls[btn])
                        _mouseDownControls[btn].InvokeMouseUp(Mouse.Position, MouseButton.Left);

                    lastLeftUp = MouseOverControl;
                }
                else
                    _mouseDownControls[btn]?.InvokeMouseUp(Mouse.Position, MouseButton.Left);

                CloseIfClickOutGumps();
                _mouseDownControls[btn] = null;
            };

            Engine.Input.LeftMouseDoubleClick += (sender, e) =>
            {
                if (MouseOverControl != null && IsMouseOverUI && MouseOverControl == lastLeftUp)
                {
                    e.Result |= MouseOverControl.InvokeMouseDoubleClick(Mouse.Position, MouseButton.Left);
                }
                
            };

            Engine.Input.RightMouseButtonDown += (sender, e) =>
            {
                //if (!IsModalControlOpen && ObjectsBlockingInputExists)
                //    return;

                if (MouseOverControl != null)
                {
                    MakeTopMostGump(MouseOverControl);
                    MouseOverControl.InvokeMouseDown(Mouse.Position, MouseButton.Right);

                    if (MouseOverControl.AcceptKeyboardInput)
                        _keyboardFocusControl = MouseOverControl;
                    _mouseDownControls[(int) MouseButton.Right] = MouseOverControl;
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

                CloseIfClickOutGumps();
            };

            Engine.Input.RightMouseButtonUp += (sender, e) =>
            {
                //if (!IsModalControlOpen /*&& ObjectsBlockingInputExists*/)
                //    return;

                //if (MouseOverControl == null)
                //    return;
                const int btn = (int) MouseButton.Right;
                EndDragControl(Mouse.Position);

                if (MouseOverControl != null)
                {
                    if (_mouseDownControls[btn] != null && MouseOverControl == _mouseDownControls[btn])
                        MouseOverControl.InvokeMouseClick(Mouse.Position, MouseButton.Right);
                    MouseOverControl.InvokeMouseUp(Mouse.Position, MouseButton.Right);

                    if (_mouseDownControls[btn] != null && MouseOverControl != _mouseDownControls[btn])
                        _mouseDownControls[btn].InvokeMouseUp(Mouse.Position, MouseButton.Right);

                    lastRight = MouseOverControl;
                }
                else
                    _mouseDownControls[btn]?.InvokeMouseUp(Mouse.Position, MouseButton.Right);

                CloseIfClickOutGumps();
                _mouseDownControls[btn] = null;
            };

            Engine.Input.MouseWheel += (sender, isup) =>
            {
                if (!IsModalControlOpen /*&& ObjectsBlockingInputExists*/)
                    return;

                if (MouseOverControl != null && MouseOverControl.AcceptMouseInput)
                    MouseOverControl.InvokeMouseWheel(isup ? MouseEvent.WheelScrollUp : MouseEvent.WheelScrollDown);
            };
            Engine.Input.KeyDown += (sender, e) => { _keyboardFocusControl?.InvokeKeyDown(e.keysym.sym, e.keysym.mod); };
            Engine.Input.KeyUp += (sender, e) => { _keyboardFocusControl?.InvokeKeyUp(e.keysym.sym, e.keysym.mod); };
            Engine.Input.TextInput += (sender, e) => { _keyboardFocusControl?.InvokeTextInput(e); };
        }

        public IReadOnlyList<Control> Gumps => _gumps;

        public Control MouseOverControl { get; private set; }

        public bool IsMouseOverUI => MouseOverControl != null;

        public bool IsMouseOverWorld => IsMouseOverUI && MouseOverControl is WorldViewport;

        public GameCursor GameCursor { get; }

        public Control KeyboardFocusControl
        {
            get
            {
                if (_keyboardFocusControl == null)
                {
                    for (int i = 0; i < _gumps.Count; i++)
                    {
                        Control c = _gumps[i];

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

        public bool IsModalControlOpen => _gumps.Any(s => s.ControlInfo.IsModal);

        //private bool ObjectsBlockingInputExists => _inputBlockingObjects.Count > 0;

        //public void AddInputBlocker(object obj)
        //{
        //    if (!_inputBlockingObjects.Contains(obj))
        //        _inputBlockingObjects.Add(obj);
        //}

        //public void RemoveInputBlocker(object obj)
        //{
        //    if (_inputBlockingObjects.Contains(obj))
        //        _inputBlockingObjects.Remove(obj);
        //}

        private void CloseIfClickOutGumps()
        {
            foreach (Gump gump in _gumps.OfType<Gump>().Where(s => s.CloseIfClickOutside)) gump.Dispose();
        }

        public void SavePosition(Serial serverSerial, Point point)
        {
            _gumpPositionCache[serverSerial] = point;
        }

        public bool GetGumpCachePosition(Serial id, out Point pos)
        {
            return _gumpPositionCache.TryGetValue(id, out pos);
        }

        public Control Create(Serial sender, Serial gumpID, int x, int y, string layout, string[] lines)
        {
            if (GetGumpCachePosition(gumpID, out Point pos))
            {
                x = pos.X;
                y = pos.Y;
            }
            else
                SavePosition(gumpID, new Point(x, y));

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
                                X = int.Parse(gparams[1]) + int.Parse(gparams[10]), Y = int.Parse(gparams[2]) + int.Parse(gparams[11])
                            }, page);

                            break;
                        case "checkertrans":
                            CheckerTrans t = new CheckerTrans(gparams);

                            for (int i = 0; i < gump.Children.Count; i++)
                            {
                                Control g = gump.Children[i];
                                g.IsTransparent = true;
                                if (g.Bounds.Contains(t.Bounds) && (g is Button || g is Checkbox)) g.IsVisible = false;
                            }

                            gump.AddChildren(t, page);

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
                            gump.AddChildren(new HtmlGump(int.Parse(gparams[1]), int.Parse(gparams[2]), int.Parse(gparams[3]), int.Parse(gparams[4]), int.Parse(gparams[6]) == 1, int.Parse(gparams[7]) != 0, gparams[6] != "0" && gparams[7] == "2", FileManager.Cliloc.GetString(int.Parse(gparams[5])), 0, true), page);

                            break;
                        case "xmfhtmlgumpcolor":
                            int color = int.Parse(gparams[8]);

                            if (color == 0x7FFF)
                                color = 0x00FFFFFF;
                            gump.AddChildren(new HtmlGump(int.Parse(gparams[1]), int.Parse(gparams[2]), int.Parse(gparams[3]), int.Parse(gparams[4]), int.Parse(gparams[6]) == 1, int.Parse(gparams[7]) != 0, gparams[6] != "0" && gparams[7] == "2", FileManager.Cliloc.GetString(int.Parse(gparams[5])), color, true), page);

                            break;
                        case "xmfhtmltok":
                            color = int.Parse(gparams[7]);

                            if (color == 0x7FFF)
                                color = 0x00FFFFFF;
                            StringBuilder sb = null;

                            if (gparams.Length > 9)
                            {
                                sb = new StringBuilder();
                                sb.Append(gparams[9]);

                                for (int i = 10; i < gparams.Length; i++)
                                {
                                    sb.Append('\t');
                                    sb.Append(gparams[i]);
                                }
                            }

                            gump.AddChildren(new HtmlGump(int.Parse(gparams[1]), int.Parse(gparams[2]), int.Parse(gparams[3]), int.Parse(gparams[4]), int.Parse(gparams[5]) == 1, int.Parse(gparams[6]) != 0, gparams[5] != "0" && gparams[6] == "2", sb == null ? FileManager.Cliloc.GetString(int.Parse(gparams[8])) : FileManager.Cliloc.Translate(FileManager.Cliloc.GetString(int.Parse(gparams[8])), sb.ToString().Trim('@')), color, true), page);

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
                            gump.BlockMovement = true;

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

                            if (World.ClientFlags.TooltipsEnabled)
                            {
                                string cliloc = FileManager.Cliloc.GetString(int.Parse(gparams[1]));
                                Control last = gump.Children.Count > 0 ? gump.Children.Last() : null;
                                last?.SetTooltip(cliloc);
                            }

                            break;
                        case "noresize":

                            break;
                        case "mastergump":
                            Log.Message(LogTypes.Warning, "Gump part 'mastergump' not handled.");
                            break;
                    }
                }
                else
                    break;
            }

            Add(gump);

            return gump;
        }

        public T GetByLocalSerial<T>(Serial? serial = null) where T : Control
        {
            return _gumps.OfType<T>().FirstOrDefault(s => !s.IsDisposed && (!serial.HasValue || s.LocalSerial == serial));
        }

        public Gump GetByLocalSerial(Serial serial)
        {
            return _gumps.OfType<Gump>().FirstOrDefault(s => !s.IsDisposed && s.LocalSerial == serial);
        }

        public Gump GetByServerSerial(Serial serial)
        {
            return _gumps.OfType<Gump>().FirstOrDefault(s => !s.IsDisposed && s.ServerSerial == serial);
        }

        public void Update(double totalMS, double frameMS)
        {
            SortControlsByInfo();

            for (int i = 0; i < _gumps.Count; i++)
            {
                Control g = _gumps[i];

                if (!g.IsInitialized && !g.IsDisposed)
                    g.Initialize();
                g.Update(totalMS, frameMS);

                if (g.IsDisposed)
                    _gumps.RemoveAt(i--);

            }

            GameCursor.Update(totalMS, frameMS);
            HandleKeyboardInput();
            HandleMouseInput();
        }

        public void Draw(Batcher2D batcher)
        {
            SortControlsByInfo();

            for (int i = _gumps.Count - 1; i >= 0; i--)
            {
                Control g = _gumps[i];

                if (g.IsInitialized)
                    g.Draw(batcher, g.Location);
            }

            GameCursor.Draw(batcher);
        }

        public void Add(Control gump)
        {
            if (!gump.IsDisposed)
            {
                _gumps.Insert(0, gump);
                _needSort = true;
            }
        }

        public void Remove<T>(Serial? local = null) where T : Control
        {
            foreach (Control c in _gumps)
            {
                if (c is T)
                {
                    if (!local.HasValue || c.LocalSerial == local)
                    {
                        if (!c.IsDisposed)
                            c.Dispose();
                    }
                }
            }
        }

        public void Clear()
        {
            GameCursor?.ClearDraggedItem();
            _gumps.ForEach(s => s.Dispose());
        }


        private void HandleKeyboardInput()
        {
            if (KeyboardFocusControl != null && _keyboardFocusControl.IsDisposed) _keyboardFocusControl = null;
        }

        private void HandleMouseInput()
        {
            Point position = Mouse.Position;
            Control gump = GetMouseOverControl(position);

            if (MouseOverControl != null && gump != MouseOverControl)
            {
                MouseOverControl.InvokeMouseExit(position);

                if (MouseOverControl.RootParent != null)
                {
                    if (gump == null || gump.RootParent != MouseOverControl.RootParent)
                        MouseOverControl.RootParent.InvokeMouseExit(position);
                }
            }

            if (gump != null)
            {

                if (gump != MouseOverControl)
                {
                    gump.InvokeMouseEnter(position);

                    if (gump?.RootParent != null)
                    {
                        if (MouseOverControl== null || gump.RootParent != MouseOverControl.RootParent)
                            gump.RootParent.InvokeMouseEnter(position);
                    }
                }

                gump.InvokeMouseOver(position);

               
                if (_mouseDownControls[0] == gump)
                    AttemptDragControl(gump, position);

                //if (_isDraggingControl)
                //    DoDragControl(position);
            }

            MouseOverControl = gump;

            for (int i = 0; i < 5; i++)
            {
                if (_mouseDownControls[i] != null && _mouseDownControls[i] != gump)
                    _mouseDownControls[i].InvokeMouseOver(position);
            }
        }

        private Control GetMouseOverControl(Point position)
        {
            if (_isDraggingControl)
                return _draggingControl;
            var controls = IsModalControlOpen ? _gumps.Where(s => s.ControlInfo.IsModal) : _gumps;
            IReadOnlyList<Control> mouseoverControls = null;

            foreach (Control c in controls)
            {
                var ctrls = c.HitTest(position);

                if (ctrls != null)
                {
                    mouseoverControls = ctrls;

                    break;
                }
            }

            return mouseoverControls?.LastOrDefault(s => s.AcceptMouseInput);
        }

        private void MakeTopMostGump(Control control)
        {
            Control c = control;

            while (c.Parent != null)
                c = c.Parent;

            for (int i = 1; i < _gumps.Count; i++)
            {
                if (_gumps[i] == c)
                {
                    Control cm = _gumps[i];
                    _gumps.RemoveAt(i);
                    _gumps.Insert(0, cm);
                    _needSort = true;
                }
            }
        }

        private void SortControlsByInfo()
        {
            if (_needSort)
            {
                _gumps.Sort((a, b) => a.ControlInfo.Layer.CompareTo(b.ControlInfo.Layer));
                _needSort = false;
            }
        }

        public void AttemptDragControl(Control control, Point mousePosition, bool attemptAlwaysSuccessful = false)
        {
            if (_isDraggingControl)
                return;
            Control dragTarget = control;

            if (!dragTarget.CanMove)
                return;

            while (dragTarget.Parent != null)
                dragTarget = dragTarget.Parent;

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

                    if (attemptAlwaysSuccessful || Math.Abs(deltaX) + Math.Abs(deltaY) > 4)
                    {
                        _isDraggingControl = true;
                        dragTarget.InvokeDragBegin(new Point(deltaX, deltaY));
                    }
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
            _draggingControl?.InvokeDragEnd(mousePosition);
            _draggingControl = null;
            _isDraggingControl = false;
        }
    }
}