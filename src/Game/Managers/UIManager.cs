#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
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

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Collections;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Managers
{
    internal sealed class UIManager
    {
        private static readonly TextFileParser _parser = new TextFileParser(string.Empty, new[] {' '}, new char[] { }, new[] {'{', '}'});
        private static readonly TextFileParser _cmdparser = new TextFileParser(string.Empty, new[] {' '}, new char[] { }, new char[] { });
        private readonly Dictionary<Serial, Point> _gumpPositionCache = new Dictionary<Serial, Point>();
        private readonly Control[] _mouseDownControls = new Control[5];


        private readonly Dictionary<Serial, TargetLineGump> _targetLineGumps = new Dictionary<Serial, TargetLineGump>();
        private int _dragOriginX, _dragOriginY;
        private bool _isDraggingControl;
        private Control _keyboardFocusControl;
        private bool _needSort;

        public UIManager()
        {
            AnchorManager = new AnchorManager();

            Engine.Input.MouseDragging += (sender, e) =>
            {
                HandleMouseInput();

                if (_mouseDownControls[0] == MouseOverControl && MouseOverControl != null)
                    AttemptDragControl(MouseOverControl, Mouse.Position, true);

                if (_isDraggingControl)
                {
                    DoDragControl(Mouse.Position);
                }
            };

            Engine.Input.LeftMouseButtonDown += (sender, e) =>
            {
                //if (!IsModalControlOpen /*&& ObjectsBlockingInputExists*/)
                //    return;

                HandleMouseInput();

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
                        foreach (Control s in Gumps)
                        {
                            if (s.ControlInfo.IsModal && s.ControlInfo.ModalClickOutsideAreaClosesThisControl)
                            {
                                s.Dispose();
                                Mouse.CancelDoubleClick = true;
                            }
                        }
                    }
                }
            };

            Control lastLeftUp = null, lastRightUp = null;

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
                    //if (_mouseDownControls[btn] != null && MouseOverControl == _mouseDownControls[btn])
                    //    MouseOverControl.InvokeMouseClick(Mouse.Position, MouseButton.Left);

                    //if (_mouseDownControls[btn] != null && MouseOverControl == _mouseDownControls[btn])
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
                if (MouseOverControl != null && IsMouseOverAControl && MouseOverControl == lastLeftUp)
                    e.Result = MouseOverControl.InvokeMouseDoubleClick(Mouse.Position, MouseButton.Left);
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
                        foreach (Control s in Gumps)
                        {
                            if (s.ControlInfo.IsModal && s.ControlInfo.ModalClickOutsideAreaClosesThisControl)
                            {
                                s.Dispose();
                                Mouse.CancelDoubleClick = true;
                            }
                        }
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
                    {
                        //MouseOverControl.InvokeMouseClick(Mouse.Position, MouseButton.Right);
                        MouseOverControl.InvokeMouseCloseGumpWithRClick();
                    }

                    MouseOverControl.InvokeMouseUp(Mouse.Position, MouseButton.Right);

                    if (_mouseDownControls[btn] != null && MouseOverControl != _mouseDownControls[btn])
                    {
                        _mouseDownControls[btn].InvokeMouseUp(Mouse.Position, MouseButton.Right);
                        _mouseDownControls[btn].InvokeMouseCloseGumpWithRClick();
                    }


                    lastRightUp = MouseOverControl;
                }
                else if (_mouseDownControls[btn] != null)
                {
                    _mouseDownControls[btn].InvokeMouseUp(Mouse.Position, MouseButton.Right);
                    _mouseDownControls[btn].InvokeMouseCloseGumpWithRClick();
                }

                CloseIfClickOutGumps();
                _mouseDownControls[btn] = null;
            };

            Engine.Input.RightMouseDoubleClick += (sender, e) =>
            {
                if (MouseOverControl != null && IsMouseOverAControl && MouseOverControl == lastRightUp) e.Result = MouseOverControl.InvokeMouseDoubleClick(Mouse.Position, MouseButton.Right);
            };

            Engine.Input.MouseWheel += (sender, isup) =>
            {
                //if (!IsModalControlOpen /*&& ObjectsBlockingInputExists*/)
                //    return;

                if (MouseOverControl != null && MouseOverControl.AcceptMouseInput)
                    MouseOverControl.InvokeMouseWheel(isup ? MouseEvent.WheelScrollUp : MouseEvent.WheelScrollDown);
            };
            Engine.Input.KeyDown += (sender, e) => { _keyboardFocusControl?.InvokeKeyDown(e.keysym.sym, e.keysym.mod); };
            Engine.Input.KeyUp += (sender, e) => { _keyboardFocusControl?.InvokeKeyUp(e.keysym.sym, e.keysym.mod); };
            Engine.Input.TextInput += (sender, e) => { _keyboardFocusControl?.InvokeTextInput(e); };
        }


        public AnchorManager AnchorManager { get; }

        public Deque<Control> Gumps { get; } = new Deque<Control>();

        public Control MouseOverControl { get; private set; }

        public bool IsMouseOverAControl => MouseOverControl != null;

        public bool IsMouseOverWorld => MouseOverControl is WorldViewport;

        public Control DraggingControl { get; private set; }

        public GameCursor GameCursor { get; private set; }

        public SystemChatControl SystemChat { get; set; }

        public Control KeyboardFocusControl
        {
            get
            {
                if (_keyboardFocusControl == null)
                {
                    foreach (Control c in Gumps)
                    {
                        if (!c.IsDisposed && c.IsVisible && c.IsEnabled)
                        {
                            _keyboardFocusControl = c.GetFirstControlAcceptKeyboardInput();

                            if (_keyboardFocusControl != null) break;
                        }
                    }
                }

                return _keyboardFocusControl;
            }
            set
            {
                if (value != null && value.AcceptKeyboardInput)
                {
                    _keyboardFocusControl?.OnFocusLeft();
                    _keyboardFocusControl = value;
                    value.OnFocusEnter();
                }
            }
        }

        public bool IsModalControlOpen => Gumps.Any(s => s.ControlInfo.IsModal);

        public bool IsDragging => _isDraggingControl && DraggingControl != null;

        public TargetLineGump TargetLine { get; set; }



        public void InitializeGameCursor()
        {
            GameCursor = new GameCursor();
        }

        private void CloseIfClickOutGumps()
        {
            foreach (Gump gump in Gumps.OfType<Gump>().Where(s => s.CloseIfClickOutside)) gump.Dispose();
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

            List<string> cmdlist = _parser.GetTokens(layout);
            int cmdlen = cmdlist.Count;
            bool applyCheckerTrans = false;

            for (int cnt = 0; cnt < cmdlen; cnt++)
            {
                List<string> gparams = _cmdparser.GetTokens(cmdlist[cnt], false);

                if (gparams.Count == 0) continue;

                switch (gparams[0].ToLower())
                {
                    case "button":
                        gump.Add(new Button(gparams), page);

                        break;

                    case "buttontileart":

                        gump.Add(new Button(gparams)
                        {
                            ContainsByBounds = true
                        }, page);

                        gump.Add(new StaticPic(Graphic.Parse(gparams[8]), Hue.Parse(gparams[9]))
                        {
                            X = int.Parse(gparams[1]) + int.Parse(gparams[10]),
                            Y = int.Parse(gparams[2]) + int.Parse(gparams[11]),

                            AcceptMouseInput = true
                        }, page);

                        break;

                    case "checkertrans":
                        CheckerTrans t = new CheckerTrans(gparams);

                        applyCheckerTrans = true;
                        //bool applyTrans(int ii, int current_page)
                        //{
                        //    bool transparent = false;
                        //    for (; ii < gump.Children.Count; ii++)
                        //    {
                        //        var child = gump.Children[ii];

                        //        bool canDraw = /*current_page == 0 || child.Page == 0 ||*/
                        //                       current_page == child.Page;

                        //        if (canDraw && child.IsVisible && child is CheckerTrans)
                        //        {
                        //            transparent = true;
                        //        }
                        //    }

                        //    return transparent;
                        //}

                        //void checkerContains(int ii, float tr)
                        //{
                        //    var master = gump.Children[ii];

                        //    for (int i = 0; i < ii; i++)
                        //    {
                        //        var cc = gump.Children[i];

                        //        if (master.Bounds.Contains(cc.Bounds))
                        //        {
                        //            cc.Alpha = 1f;
                        //        }
                        //    }
                        //}

                        //Rectangle bounds = t.Bounds;
                        //bool trans = false;
                        //for (int i = gump.Children.Count - 1; i >= 0; i--)
                        //{
                        //    var cc = gump.Children[i];

                        //    if (cc is CheckerTrans)
                        //    {
                        //        trans = applyTrans(i, cc.Page);
                        //        bounds = cc.Bounds;
                        //        continue;
                        //    }

                        //    if (bounds.Contains(cc.Bounds))
                        //    {
                        //        cc.Alpha = 1f;
                        //    }
                        //    else
                        //        cc.Alpha = trans ? 1 : 0.5f;
                        //}

                        gump.Add(t, page);

                        //  int j = 0;
                        //  bool trans = applyTrans(j, page, null);
                        //  float alpha = trans ? 1 : 0.5f;
                        ////  checkerContains(j, alpha);
                        //  for (; j < gump.Children.Count; j++)
                        //  {
                        //      var child = gump.Children[j];

                        //      if (child is CheckerTrans tt)
                        //      {
                        //          trans = applyTrans(j, child.Page, tt);
                        //          alpha = trans ? 1 : .5f;
                        //          checkerContains(j, alpha);
                        //      }
                        //      else
                        //      {
                        //          child.Alpha = alpha != 1 ? 0.5f : alpha;
                        //      }
                        //  }


                        //float[] alpha = { 1f, 0.5f };

                        //bool checkTransparent(Control c, int start)
                        //{
                        //    bool transparent = false;
                        //    for (int i = start; i < c.Children.Count; i++)
                        //    //for (int i = start; i >= 0; i--)
                        //    {
                        //        var control = c.Children[i];

                        //        bool canDraw = /*c.Page == 0 || control.Page == 0 || c.Page == control.Page ||*/ control.Page == page;

                        //        if (canDraw && control is CheckerTrans)
                        //        {
                        //            transparent = true;
                        //        }
                        //    }

                        //    return transparent;
                        //}


                        //bool trans = checkTransparent(gump, 0);

                        //for (int i = gump.Children.Count - 1; i >= 0; i--)
                        //{
                        //    Control g = gump.Children[i];
                        //    g.Initialize();
                        //    g.IsTransparent = true;

                        //    if (g is CheckerTrans)
                        //    {
                        //        trans = checkTransparent(gump, i + 1);

                        //        continue;
                        //    }

                        //    g.Alpha = alpha[trans ? 0 : 1];
                        //}



                        break;

                    case "croppedtext":
                        gump.Add(new CroppedText(gparams, lines), page);

                        break;

                    case "gumppic":

                        GumpPic pic = new GumpPic(gparams);

                        if (gparams.Count >= 6 && gparams[5].ToLower().Contains("virtuegumpitem"))
                        {
                            pic.ContainsByBounds = true;
                            pic.IsVirtue = true;
                        }

                        gump.Add(pic, page);

                        break;

                    case "gumppictiled":
                        gump.Add(new GumpPicTiled(gparams), page);

                        break;

                    case "htmlgump":
                        gump.Add(new HtmlControl(gparams, lines), page);

                        break;

                    case "xmfhtmlgump":
                        gump.Add(new HtmlControl(int.Parse(gparams[1]), int.Parse(gparams[2]), int.Parse(gparams[3]), int.Parse(gparams[4]), int.Parse(gparams[6]) == 1, int.Parse(gparams[7]) != 0, gparams[6] != "0" && gparams[7] == "2", FileManager.Cliloc.GetString(int.Parse(gparams[5])), 0, true), page);

                        break;

                    case "xmfhtmlgumpcolor":
                        int color = int.Parse(gparams[8]);

                        if (color == 0x7FFF)
                            color = 0x00FFFFFF;
                        gump.Add(new HtmlControl(int.Parse(gparams[1]), int.Parse(gparams[2]), int.Parse(gparams[3]), int.Parse(gparams[4]), int.Parse(gparams[6]) == 1, int.Parse(gparams[7]) != 0, gparams[6] != "0" && gparams[7] == "2", FileManager.Cliloc.GetString(int.Parse(gparams[5])), color, true), page);

                        break;

                    case "xmfhtmltok":
                        color = int.Parse(gparams[7]);

                        if (color == 0x7FFF)
                            color = 0x00FFFFFF;
                        StringBuilder sb = null;

                        if (gparams.Count > 9)
                        {
                            sb = new StringBuilder();
                            sb.Append(gparams[9]);

                            for (int i = 10; i < gparams.Count; i++)
                            {
                                sb.Append(' ');
                                sb.Append(gparams[i]);
                            }
                        }

                        gump.Add(new HtmlControl(int.Parse(gparams[1]), int.Parse(gparams[2]), int.Parse(gparams[3]), int.Parse(gparams[4]), int.Parse(gparams[5]) == 1, int.Parse(gparams[6]) != 0, gparams[5] != "0" && gparams[6] == "2", sb == null ? FileManager.Cliloc.GetString(int.Parse(gparams[8])) : FileManager.Cliloc.Translate(FileManager.Cliloc.GetString(int.Parse(gparams[8])), sb.ToString().Trim('@')), color, true), page);

                        break;

                    case "page":
                        page = int.Parse(gparams[1]);

                        break;

                    case "resizepic":
                        gump.Add(new ResizePic(gparams), page);

                        break;

                    case "text":
                        gump.Add(new Label(gparams, lines), page);

                        break;

                    case "textentrylimited":
                    case "textentry":
                        gump.Add(new TextBox(gparams, lines), page);

                        break;

                    case "tilepichue":
                    case "tilepic":
                        gump.Add(new StaticPic(gparams), page);

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
                        gump.Add(new RadioButton(group, gparams, lines), page);

                        break;

                    case "checkbox":
                        gump.Add(new Checkbox(gparams, lines), page);

                        break;

                    case "tooltip":

                        if (World.ClientFeatures.TooltipsEnabled)
                        {
                            string cliloc = FileManager.Cliloc.GetString(int.Parse(gparams[1]));

                            if (gparams.Count > 2 && gparams[2][0] == '@')
                            {
                                string args = gparams[2];
                                //Convert tooltip args format to standard cliloc format
                                args = args.Trim('@').Replace('@', '\t');

                                if (args.Length > 1)
                                    cliloc = FileManager.Cliloc.Translate(cliloc, args);
                                else
                                    Log.Message(LogTypes.Error, $"String '{args}' too short, something wrong with gump tooltip: {cliloc}");
                            }

                            gump.Children.Last()?.SetTooltip(cliloc);
                        }

                        break;

                    case "itemproperty":

                        if (World.ClientFeatures.TooltipsEnabled)
                        {
                            var entity = World.Get(Serial.Parse(gparams[1]));
                            var lastControl = gump.Children.LastOrDefault();

                            if (lastControl != default(Control) && entity != default(Entity))
                                lastControl.SetTooltip(entity);
                        }

                        break;

                    case "noresize":

                        break;

                    case "mastergump":
                        Log.Message(LogTypes.Warning, "Gump part 'mastergump' not handled.");

                        break;
                }
            }

            if (applyCheckerTrans)
            {
                bool applyTrans(int ii, int current_page)
                {
                    bool transparent = false;
                    for (; ii < gump.Children.Count; ii++)
                    {
                        var child = gump.Children[ii];

                        if (current_page == 0)
                            current_page = child.Page;

                        bool canDraw = /*current_page == 0 || child.Page == 0 ||*/
                            current_page == child.Page;

                        if (canDraw && child.IsVisible && child is CheckerTrans)
                        {
                            transparent = true;
                            continue;
                        }

                        child.Alpha = transparent ? 0.5f : 0;
                    }

                    return transparent;
                }


                bool trans = applyTrans(0, 0);
                float alpha = trans ? 0.5f : 0;
                for (int i = 0; i < gump.Children.Count; i++)
                {
                    var cc = gump.Children[i];

                    if (cc is CheckerTrans)
                    {
                        trans = applyTrans(i + 1, cc.Page);
                        alpha = trans ? 0.5f : 0;
                    }
                    else
                    {
                        cc.Alpha = alpha;
                    }
                }
            }

            Add(gump);

            return gump;
        }

        public void SetTargetLineGump(Serial mob)
        {
            if (TargetLine != null && !TargetLine.IsDisposed && TargetLine.Mobile == mob)
                return;

            TargetLine?.Dispose();
            Remove<TargetLineGump>();
            TargetLine = null;

            if (TargetLine == null || TargetLine.IsDisposed)
            {
                Mobile mobile = World.Mobiles.Get(mob);

                if (mobile != null)
                {
                    TargetLine = new TargetLineGump(mobile);
                    Engine.UI.Add(TargetLine);
                }
            }

            //if (!_targetLineGumps.TryGetValue(mob, out TargetLineGump gump))
            //{
            //    Mobile m = World.Mobiles.Get(mob);
            //    if (m == null)
            //        return;

            //    gump = new TargetLineGump(m);
            //    _targetLineGumps[mob] = gump;
            //    Engine.UI.Add(gump);
            //}
        }

        public void RemoveTargetLineGump(Serial serial)
        {
            //if (_targetLineGumps.TryGetValue(serial, out TargetLineGump gump))
            //{
            //    gump?.Dispose();
            //    _targetLineGumps.Remove(serial);
            //}
        }


        public ChildType GetChildByLocalSerial<ParentType, ChildType>(Serial parentSerial, Serial childSerial)
            where ParentType : Control
            where ChildType : Control
        {
            ParentType parent = GetGump<ParentType>(parentSerial);

            return parent?.Children.OfType<ChildType>().FirstOrDefault(s => !s.IsDisposed && s.LocalSerial == childSerial);
        }

        public T GetGump<T>(Serial? serial = null) where T : Control
        {
            foreach (Control c in Gumps)
            {
                if (!c.IsDisposed && (!serial.HasValue || c.LocalSerial == serial) && c is T t)
                {
                    return t;
                }
            }

            return null;
        }


        public void Update(double totalMS, double frameMS)
        {
            SortControlsByInfo();

            for (int i = 0; i < Gumps.Count; i++)
            {
                Control g = Gumps[i];

                if (!g.IsInitialized && !g.IsDisposed)
                    g.Initialize();
                g.Update(totalMS, frameMS);

                if (g.IsDisposed)
                    Gumps.RemoveAt(i--);
            }

            GameCursor?.Update(totalMS, frameMS);
            HandleKeyboardInput();
            HandleMouseInput();
        }

        public void Draw(UltimaBatcher2D batcher)
        {
            SortControlsByInfo();

            batcher.GraphicsDevice.Clear(Color.Transparent);

            batcher.Begin();

            for (int i = Gumps.Count - 1; i >= 0; i--)
            {
                Control g = Gumps[i];

                if (g.IsInitialized)
                    g.Draw(batcher, g.X, g.Y);
            }

            GameCursor?.Draw(batcher);

            batcher.End();
        }

        public void Add(Control gump)
        {
            if (!gump.IsDisposed)
            {
                Gumps.AddToFront(gump);
                _needSort = true;
            }
        }

        public void Remove<T>(Serial? local = null) where T : Control
        {
            Gumps.OfType<T>().FirstOrDefault(s => (!local.HasValue || s.LocalSerial == local) && !s.IsDisposed)?.Dispose();
        }

        public void Clear()
        {
            foreach (Control s in Gumps)
            {
                s.Dispose();
            }
        }


        private void HandleKeyboardInput()
        {
            if (KeyboardFocusControl != null && _keyboardFocusControl.IsDisposed)
                _keyboardFocusControl = null;
        }

        private void HandleMouseInput()
        {
            Control gump = GetMouseOverControl(Mouse.Position);

            if (MouseOverControl != null && gump != MouseOverControl)
            {
                MouseOverControl.InvokeMouseExit(Mouse.Position);

                if (MouseOverControl.RootParent != null)
                {
                    if (gump == null || gump.RootParent != MouseOverControl.RootParent)
                        MouseOverControl.RootParent.InvokeMouseExit(Mouse.Position);
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
                            gump.RootParent.InvokeMouseEnter(Mouse.Position);
                    }
                }

                gump.InvokeMouseOver(Mouse.Position);

                if (_mouseDownControls[0] == gump)
                    AttemptDragControl(gump, Mouse.Position);
            }

            MouseOverControl = gump;

            for (int i = 0; i < 5; i++)
            {
                if (_mouseDownControls[i] != null && _mouseDownControls[i] != gump)
                    _mouseDownControls[i].InvokeMouseOver(Mouse.Position);
            }
        }

        public Control[] GetMouseOverControls(Point position)
        {
            return Gumps.Where(o => o.HitTest(position) != null).ToArray();
        }

        private Control GetMouseOverControl(Point position)
        {
            if (_isDraggingControl)
                return DraggingControl;

            Control[] mouseOverControls = null;

            bool ismodal = IsModalControlOpen;

            foreach (Control c in Gumps)
            {
                if (ismodal && !c.ControlInfo.IsModal)
                {
                    continue;
                }

                Control[] ctrls = c.HitTest(position);

                if (ctrls != null)
                {
                    mouseOverControls = ctrls;

                    break;
                }
            }

            if (mouseOverControls != null)
            {
                foreach (Control t in mouseOverControls)
                {
                    if (t.AcceptMouseInput)
                        return t;
                }
            }

            return null;
        }

        public void MakeTopMostGump(Control control)
        {
            Control c = control;

            while (c.Parent != null)
                c = c.Parent;

            for (int i = 1; i < Gumps.Count; i++)
            {
                if (Gumps[i] == c)
                {
                    Control cm = Gumps[i];
                    Gumps.RemoveAt(i);
                    Gumps.AddToFront(cm);
                    _needSort = true;
                }
            }
        }

        public void MakeTopMostGumpOverAnother(Control control, Control overed)
        {
            Control c = control;

            while (c.Parent != null)
                c = c.Parent;

            Control c1 = overed;

            while (c1.Parent != null)
                c1 = c1.Parent;

            int index = 0;

            for (int i = Gumps.Count - 1; i >= 1; i--)
            {
                if (Gumps[i] == c)
                {
                    Control cm = Gumps[i];
                    Gumps.RemoveAt(i);

                    if (index == 0)
                        index = i;

                    Gumps.Insert(index - 1, cm);
                    _needSort = true;
                }
                else if (Gumps[i] == c1)
                    index = i;
            }
        }


        private void SortControlsByInfo()
        {
            if (_needSort)
            {
                var gumps = Gumps.Where(s => s.ControlInfo.Layer != UILayer.Default).ToArray();

                int over = 0;
                int under = Gumps.Count - 1;

                foreach (Control c in gumps)
                {
                    if (c.ControlInfo.Layer == UILayer.Under)
                    {
                        for (int i = 0; i < Gumps.Count; i++)
                        {
                            if (Gumps[i] == c)
                            {
                                Gumps.RemoveAt(i);
                                Gumps.Insert(under, c);
                            }
                        }
                    }
                    else if (c.ControlInfo.Layer == UILayer.Over)
                    {
                        for (int i = 0; i < Gumps.Count; i++)
                        {
                            if (Gumps[i] == c)
                            {
                                Gumps.RemoveAt(i);
                                Gumps.Insert(over++, c);
                            }
                        }
                    }
                }

                //_gumps.Sort((a, b) => a.ControlInfo.Layer.CompareTo(b.ControlInfo.Layer));
                _needSort = false;
            }
        }

        public void AttemptDragControl(Control control, Point mousePosition, bool attemptAlwaysSuccessful = false)
        {
            if (_isDraggingControl || (Engine.SceneManager.CurrentScene is GameScene gs && gs.IsHoldingItem))
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
                    DraggingControl = dragTarget;
                    _dragOriginX = mousePosition.X;
                    _dragOriginY = mousePosition.Y;
                }

                if (DraggingControl == dragTarget)
                {
                    //var p = Mouse.LDroppedOffset;
                    int deltaX = mousePosition.X - _dragOriginX;
                    int deltaY = mousePosition.Y - _dragOriginY;

                    if (attemptAlwaysSuccessful || Math.Abs(deltaX) + Math.Abs(deltaY) > Constants.MIN_GUMP_DRAG_DISTANCE)
                    {
                        _isDraggingControl = true;
                        dragTarget.InvokeDragBegin(new Point(deltaX, deltaY));
                    }
                }
                else
                {
                    DraggingControl = dragTarget;
                    _dragOriginX = mousePosition.X;
                    _dragOriginY = mousePosition.Y;
                }
            }

            //if (_isDraggingControl)
            //{
            //    for (int i = 0; i < 5; i++)
            //    {
            //        if (_mouseDownControls[i] != null && _mouseDownControls[i] != DraggingControl)
            //        {
            //            //_mouseDownControls[i].InvokeMouseUp(mousePosition, (MouseButton) i);
            //            _mouseDownControls[i] = null;
            //        }
            //    }
            //}
        }

        private void DoDragControl(Point mousePosition)
        {
            if (DraggingControl == null)
                return;

            int deltaX = mousePosition.X - _dragOriginX;
            int deltaY = mousePosition.Y - _dragOriginY;
            DraggingControl.X = DraggingControl.X + deltaX;
            DraggingControl.Y = DraggingControl.Y + deltaY;
            _dragOriginX = mousePosition.X;
            _dragOriginY = mousePosition.Y;
        }

        private void EndDragControl(Point mousePosition)
        {
            if (_isDraggingControl)
                DoDragControl(mousePosition);
            DraggingControl?.InvokeDragEnd(mousePosition);
            DraggingControl = null;
            _isDraggingControl = false;
        }
    }
}