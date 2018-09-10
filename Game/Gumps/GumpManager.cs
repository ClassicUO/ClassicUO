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
using ClassicUO.Game.Renderer;
using ClassicUO.Input;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ClassicUO.Game.Gumps
{
    public static class GumpManager
    {
        private static readonly List<GumpControl> _gumps = new List<GumpControl>(); 
        private static GumpControl _mouseOverControl;
        private static readonly GumpControl[] _mouseDownControls = new GumpControl[5];


        public static GumpControl Create(Serial sender,  Serial gumpID,  int x,  int y,  string layout,  string[] lines)
        {
            List<string> pieces = new List<string>();
            int index = 0;
            GumpControl gump = new GumpControl()
            {
                LocalSerial = sender,
                ServerSerial = gumpID,
                X = x,
                Y = y,
                CanMove = true,
                CanCloseWithRightClick = true,
                CanCloseWithEsc = true,
            };

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
                        case "page":
                            break;
                        case "resizepic":
                            gump.AddChildren(new ResizePic(gparams));
                            break;
                        case "text":
                            break;
                        case "textentry":
                            break;
                        case "textentrylimited":
                            break;
                        case "tilepic":
                            break;
                        case "tilepichue":
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
                            break;
                        case "endgroup":
                            break;
                        case "radio":
                            break;
                        case "checkbox":
                            break;
                        case "xmfhtmlgump":
                            break;
                        case "xmfhtmlgumpcolor":
                            break;
                        case "xmfhtmltok":
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

        public static void Update(double ms)
        {
            for (int i = 0; i < _gumps.Count; i++)
            {
                _gumps[i].Update(ms);

                if (_gumps[i].IsDisposed)
                    _gumps.RemoveAt(i--);
            }

            HandleMouseInput();
        }

        public static void Render(SpriteBatchUI spriteBatch)
        {
            for (int i = _gumps.Count - 1; i >= 0; i--)
            {
                var g = _gumps[i];
                g.Draw(spriteBatch,
                    new Vector3(g.X, g.Y, 0));
            }
        }


        private static void HandleMouseInput()
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
                
                if (_mouseOverControl.Parent != null && (gump == null || gump.RootParent != _mouseOverControl.RootParent))
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

            for ( int i = 0; i < 5; i++)
            {
                if (_mouseDownControls[i] != null && _mouseDownControls[i] != gump)
                    _mouseDownControls[i].InvokeMouseEnter(position);
            }


            var events = inputManager.GetMouseEvents();

            foreach (var e in events)
            {
                switch(e.EventType)
                {
                    case MouseEvent.Down:
                        if (gump != null)
                        {
                            MakeTopMostGump(gump);
                            gump.InvokeMouseDown(e.Position, e.Button);

                            _mouseDownControls[(int)e.Button] = gump;
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



            //if (gump != null)
            //{
            //    bool isdown = false;

            //    while (_typeQueue.Count > 0)
            //    {
            //        var t = _typeQueue.Dequeue();


            //        switch (t)
            //        {
            //            case InputMouseType.MouseWheel:
            //                var evw = _mouseEventsWheelTriggered.Dequeue();
            //                gump.OnMouseWheel(evw);
            //                break;
            //            case InputMouseType.MouseDown:
            //            case InputMouseType.MouseUp:
            //            case InputMouseType.MousePressed:
            //                var ev = _mouseEventsTriggered[(int)t].Dequeue();
            //                gump.OnMouseButton(ev);

            //                isdown = ev.Button == MouseButtons.Left && ev.ButtonState == Microsoft.Xna.Framework.Input.ButtonState.Pressed;

            //                if (isdown)
            //                {
            //                    _lastClickedPosition = new Point(ev.X, ev.Y);
            //                }

            //                if (ev.Button == MouseButtons.Right && t == InputMouseType.MouseUp && gump.RootParent.CanCloseWithRightClick)
            //                {
            //                    gump.RootParent.Dispose();
            //                    _gumps.Remove(gump.RootParent);
            //                }

            //                break;
            //            case InputMouseType.MouseMove:
            //                ev = _mouseEventsTriggered[(int)t].Dequeue();



            //                if (isdown && gump.CanMove)
            //                {
            //                    // TODO: add check to viewport

            //                    gump.RootParent.X += ev.X - _lastClickedPosition.X;
            //                    gump.RootParent.Y += ev.Y - _lastClickedPosition.Y;

            //                    _lastClickedPosition = new Point(ev.X, ev.Y);
            //                }
            //                else
            //                {
            //                    gump.OnMouseMove(ev);
            //                }

            //                break;
            //            default:
            //                Service.Get<Log>().Message(LogTypes.Error, "WRONG MOUSE INPUT");
            //                break;
            //        }
            //    }

            //}
        }

        private static GumpControl HitTest(GumpControl parent,  Point position)
        {
            var p = parent?.HitTest(position);
            if (p != null && p.Length > 0)
                return p[0];
            return null;
        }

        private static void MakeTopMostGump(GumpControl control)
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

        private static GumpControl _draggingControl;
        private static bool _isDraggingControl;
        private static int _dragOriginX, _dragOriginY;

        public static void AttemptDragControl(GumpControl control, Point mousePosition, bool attemptAlwaysSuccessful = false)
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

        private static void DoDragControl(Point mousePosition)
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

        private static void EndDragControl(Point mousePosition)
        {
            if (_isDraggingControl)
                DoDragControl(mousePosition);
            _draggingControl = null;
            _isDraggingControl = false;
        }

    }
}
