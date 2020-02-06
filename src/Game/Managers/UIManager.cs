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
using System.Linq;
using System.Text;

using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Collections;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Managers
{
    internal static class UIManager
    {
        private static readonly TextFileParser _parser = new TextFileParser(string.Empty, new[] { ' ' }, new char[] { }, new[] { '{', '}' });
        private static readonly TextFileParser _cmdparser = new TextFileParser(string.Empty, new[] { ' ' }, new char[] { }, new char[] { });
        private static readonly Dictionary<uint, Point> _gumpPositionCache = new Dictionary<uint, Point>();
        private static readonly Control[] _mouseDownControls = new Control[5];


        private static readonly Dictionary<uint, TargetLineGump> _targetLineGumps = new Dictionary<uint, TargetLineGump>();
        private static int _dragOriginX, _dragOriginY;
        private static bool _isDraggingControl;
        private static Control _keyboardFocusControl, _validForDClick, _lastFocus;
        private static bool _needSort;



        public static float ContainerScale { get; set; } = 1f;

        public static AnchorManager AnchorManager { get; } = new AnchorManager();

        public static Deque<Control> Gumps { get; } = new Deque<Control>();

        public static Control MouseOverControl { get; private set; }

        public static bool IsMouseOverAControl => MouseOverControl != null;

        public static bool IsMouseOverWorld => MouseOverControl is WorldViewport;

        public static Control DraggingControl { get; private set; }

        public static GameCursor GameCursor { get; private set; }

        public static SystemChatControl SystemChat { get; set; }

        public static Control KeyboardFocusControl
        {
            get
            {
                //if (_keyboardFocusControl == null || _keyboardFocusControl.IsDisposed || !_keyboardFocusControl.IsVisible || !_keyboardFocusControl.IsEnabled)
                //{
                //    _keyboardFocusControl = null;

                //    foreach (Control c in Gumps)
                //    {
                //        if (!c.IsDisposed && c.IsVisible && c.IsEnabled)
                //        {
                //            _keyboardFocusControl = c.GetFirstControlAcceptKeyboardInput();

                //            if (_keyboardFocusControl != null)
                //            {
                //                _keyboardFocusControl.OnFocusEnter();
                //                break;
                //            }
                //        }
                //    }
                //}

                return _keyboardFocusControl;
            }
            set
            {
                if (_keyboardFocusControl != value)
                {
                    _keyboardFocusControl?.OnFocusLeft();
                    _keyboardFocusControl = value;

                    if (value != null && value.AcceptKeyboardInput)
                    {
                        value.OnFocusEnter();
                    }
                }
            }
        }


        public static bool IsModalControlOpen()
        {
            foreach (var g in Gumps)
            {
                if (g.ControlInfo.IsModal)
                    return true;
            }
            return false;
        }

        public static bool IsDragging => _isDraggingControl && DraggingControl != null;

        public static TargetLineGump TargetLine { get; set; }


        public static bool ValidForDClick() => !(_validForDClick is WorldViewport);



        public static void OnMouseDragging()
        {
            HandleMouseInput();

            //if (_mouseDownControls[0] == MouseOverControl && MouseOverControl != null)
            if (_mouseDownControls[0] != null)
            {
                if (ProfileManager.Current == null || !ProfileManager.Current.HoldAltToMoveGumps || Keyboard.Alt)
                {
                    AttemptDragControl(_mouseDownControls[0], Mouse.Position, true);
                }
            }

            if (_isDraggingControl)
            {
                DoDragControl(Mouse.Position);
            }
        }

        public static void OnLeftMouseButtonDown()
        {
            HandleMouseInput();
            _validForDClick = null;
            if (MouseOverControl != null)
            {
                MakeTopMostGump(MouseOverControl);
                MouseOverControl.InvokeMouseDown(Mouse.Position, MouseButtonType.Left);

                if (MouseOverControl.AcceptKeyboardInput)
                    _keyboardFocusControl = MouseOverControl;

                if (MouseOverControl.IsEnabled && MouseOverControl.IsVisible)
                {
                    if (_lastFocus != MouseOverControl)
                    {
                        _lastFocus?.OnFocusLeft();
                        MouseOverControl.OnFocusEnter();
                        _lastFocus = MouseOverControl;
                    }
                }

                _mouseDownControls[(int) MouseButtonType.Left] = MouseOverControl;
            }
            else
            {
                if (IsModalControlOpen())
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
        }

        public static void OnLeftMouseButtonUp()
        {
            HandleMouseInput();

            const int btn = (int) MouseButtonType.Left;
            EndDragControl(Mouse.Position);

            if (MouseOverControl != null)
            {
                if (_mouseDownControls[btn] != null && MouseOverControl == _mouseDownControls[btn])
                    MouseOverControl.InvokeMouseUp(Mouse.Position, MouseButtonType.Left);

                if (_mouseDownControls[btn] != null && MouseOverControl != _mouseDownControls[btn])
                    _mouseDownControls[btn].InvokeMouseUp(Mouse.Position, MouseButtonType.Left);
            }
            else
                _mouseDownControls[btn]?.InvokeMouseUp(Mouse.Position, MouseButtonType.Left);

            CloseIfClickOutGumps();
            _mouseDownControls[btn] = null;
            _validForDClick = MouseOverControl;
        }

        public static bool OnLeftMouseDoubleClick()
        {
            HandleMouseInput();

            if (MouseOverControl != null && IsMouseOverAControl)
            {
                if (MouseOverControl.InvokeMouseDoubleClick(Mouse.Position, MouseButtonType.Left))
                {
                    DelayedObjectClickManager.Clear();
                    return true;
                }
            }

            return false;
        }

        public static void OnRightMouseButtonDown()
        {
            HandleMouseInput();

            if (MouseOverControl != null)
            {
                MakeTopMostGump(MouseOverControl);
                MouseOverControl.InvokeMouseDown(Mouse.Position, MouseButtonType.Right);

                if (MouseOverControl.AcceptKeyboardInput)
                    _keyboardFocusControl = MouseOverControl;
                _mouseDownControls[(int) MouseButtonType.Right] = MouseOverControl;
            }
            else
            {
                if (IsModalControlOpen())
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
        }

        public static void OnRightMouseButtonUp()
        {
            HandleMouseInput();

            const int btn = (int) MouseButtonType.Right;
            EndDragControl(Mouse.Position);

            if (MouseOverControl != null)
            {
                if (_mouseDownControls[btn] != null && MouseOverControl == _mouseDownControls[btn])
                {
                    MouseOverControl.InvokeMouseCloseGumpWithRClick();
                }

                MouseOverControl.InvokeMouseUp(Mouse.Position, MouseButtonType.Right);

                if (_mouseDownControls[btn] != null && MouseOverControl != _mouseDownControls[btn])
                {
                    _mouseDownControls[btn].InvokeMouseUp(Mouse.Position, MouseButtonType.Right);
                    _mouseDownControls[btn].InvokeMouseCloseGumpWithRClick();
                }
            }
            else if (_mouseDownControls[btn] != null)
            {
                _mouseDownControls[btn].InvokeMouseUp(Mouse.Position, MouseButtonType.Right);
                _mouseDownControls[btn].InvokeMouseCloseGumpWithRClick();
            }

            CloseIfClickOutGumps();
            _mouseDownControls[btn] = null;
        }

        public static bool OnRightMouseDoubleClick()
        {
            if (MouseOverControl != null && IsMouseOverAControl)
                return MouseOverControl.InvokeMouseDoubleClick(Mouse.Position, MouseButtonType.Right);

            return false;
        }

        public static void OnMouseWheel(bool isup)
        {
            if (MouseOverControl != null && MouseOverControl.AcceptMouseInput)
                MouseOverControl.InvokeMouseWheel(isup ? MouseEventType.WheelScrollUp : MouseEventType.WheelScrollDown);
        }


        public static bool HadMouseDownOnGump(MouseButtonType button)
        {
            var c = LastControlMouseDown(button);
            return c != null && !(c is WorldViewport) && !(c is ItemGump);
        }

        public static Control LastControlMouseDown(MouseButtonType button)
        {
            return _mouseDownControls[(int) button];
        }


        public static void InitializeGameCursor()
        {
            GameCursor = new GameCursor();
        }

        public static void CloseIfClickOutGumps()
        {
            foreach (Gump gump in Gumps.OfType<Gump>().Where(s => s.CloseIfClickOutside))
                gump.Dispose();
        }

        public static void SavePosition(uint serverSerial, Point point)
        {
            _gumpPositionCache[serverSerial] = point;
        }

        public static bool GetGumpCachePosition(uint id, out Point pos)
        {
            return _gumpPositionCache.TryGetValue(id, out pos);
        }

        public static Control Create(uint sender, uint gumpID, int x, int y, string layout, string[] lines)
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
            bool textBoxFocused = false;

            for (int cnt = 0; cnt < cmdlen; cnt++)
            {
                List<string> gparams = _cmdparser.GetTokens(cmdlist[cnt], false);

                if (gparams.Count == 0)
                    continue;

                switch (gparams[0].ToLower())
                {
                    case "button":
                        gump.Add(new Button(gparams), page);

                        break;

                    case "buttontileart":

                        gump.Add(new ButtonTileArt(gparams), page);

                        break;

                    case "checkertrans":
                        applyCheckerTrans = true;
                        gump.Add(new CheckerTrans(gparams), page);

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

                            string s, lvl;

                            switch (pic.Hue)
                            {
                                case 2403:
                                    lvl = "";
                                    break;
                                case 1154:
                                case 1547:
                                case 2213:
                                case 235:
                                case 18:
                                case 2210:
                                case 1348:
                                    lvl = "Seeker of ";
                                    break;
                                case 2404:
                                case 1552:
                                case 2216:
                                case 2302:
                                case 2118:
                                case 618:
                                case 2212:
                                case 1352:
                                    lvl = "Follower of ";
                                    break;
                                case 43:
                                case 53:
                                case 1153:
                                case 33:
                                case 318:
                                case 67:
                                case 98:
                                    lvl = "Knight of ";
                                    break;
                                case 2406:
                                    if (pic.Graphic == 0x6F)
                                        lvl = "Seeker of ";
                                    else
                                        lvl = "Knight of ";
                                    break;
                                default:
                                    lvl = "";
                                    break;
                            }

                            switch (pic.Graphic)
                            {
                                case 0x69:
                                    s = ClilocLoader.Instance.GetString(1051000 + 2);
                                    break;
                                case 0x6A:
                                    s = ClilocLoader.Instance.GetString(1051000 + 7);
                                    break;
                                case 0x6B:
                                    s = ClilocLoader.Instance.GetString(1051000 + 5);
                                    break;
                                case 0x6D:
                                    s = ClilocLoader.Instance.GetString(1051000 + 6);
                                    break;
                                case 0x6E:
                                    s = ClilocLoader.Instance.GetString(1051000 + 1);
                                    break;
                                case 0x6F:
                                    s = ClilocLoader.Instance.GetString(1051000 + 3);
                                    break;
                                case 0x70:
                                    s = ClilocLoader.Instance.GetString(1051000 + 4);
                                    break;

                                case 0x6C:
                                default:
                                    s = ClilocLoader.Instance.GetString(1051000);
                                    break;
                            }

                            if (string.IsNullOrEmpty(s))
                                s = "Unknown virtue";

                            pic.SetTooltip(lvl + s, 100);
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
                        gump.Add(new HtmlControl(int.Parse(gparams[1]), int.Parse(gparams[2]), int.Parse(gparams[3]), int.Parse(gparams[4]), int.Parse(gparams[6]) == 1, int.Parse(gparams[7]) != 0, gparams[6] != "0" && gparams[7] == "2", ClilocLoader.Instance.GetString(int.Parse(gparams[5])), 0, true), page);

                        break;

                    case "xmfhtmlgumpcolor":
                        int color = int.Parse(gparams[8]);

                        if (color == 0x7FFF)
                            color = 0x00FFFFFF;
                        gump.Add(new HtmlControl(int.Parse(gparams[1]), int.Parse(gparams[2]), int.Parse(gparams[3]), int.Parse(gparams[4]), int.Parse(gparams[6]) == 1, int.Parse(gparams[7]) != 0, gparams[6] != "0" && gparams[7] == "2", ClilocLoader.Instance.GetString(int.Parse(gparams[5])), color, true), page);

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

                        gump.Add(new HtmlControl(int.Parse(gparams[1]), int.Parse(gparams[2]), int.Parse(gparams[3]), int.Parse(gparams[4]), int.Parse(gparams[5]) == 1, int.Parse(gparams[6]) != 0, gparams[5] != "0" && gparams[6] == "2", sb == null ? ClilocLoader.Instance.GetString(int.Parse(gparams[8])) : ClilocLoader.Instance.Translate(ClilocLoader.Instance.GetString(int.Parse(gparams[8])), sb.ToString().Trim('@').Replace('@', '\t')), color, true), page);

                        break;

                    case "page":

                        if (gparams.Count >= 2)
                            page = int.Parse(gparams[1]);

                        break;

                    case "resizepic":
                        gump.Add(new ResizePic(gparams), page);

                        break;

                    case "text":
                        if (gparams.Count >= 5)
                            gump.Add(new Label(gparams, lines), page);

                        break;

                    case "textentrylimited":
                    case "textentry":
                        TextBox textBox = new TextBox(gparams, lines);

                        if (!textBoxFocused)
                        {
                            textBox.SetKeyboardFocus();
                            textBoxFocused = true;
                        }

                        gump.Add(textBox, page);

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
                            string text = ClilocLoader.Instance.GetString(int.Parse(gparams[1]));

                            if (gparams.Count > 2 && gparams[2].Length != 0 && gparams[2][0] == '@')
                            {
                                string args = gparams[2];
                                //Convert tooltip args format to standard cliloc format
                                args = args.Trim('@').Replace('@', '\t');

                                if (args.Length > 1)
                                    text = ClilocLoader.Instance.Translate(text, args);
                                else
                                    Log.Error($"String '{args}' too short, something wrong with gump tooltip: {text}");
                            }

                            gump.Children.LastOrDefault()?.SetTooltip(text);
                        }

                        break;

                    case "itemproperty":

                        if (World.ClientFeatures.TooltipsEnabled)
                        {
                            gump.Children.LastOrDefault()?.SetTooltip(SerialHelper.Parse(gparams[1]));
                        }

                        break;

                    case "noresize":

                        break;

                    case "mastergump":
                        Log.Warn("Gump part 'mastergump' not handled.");

                        break;
                    default:
                        Log.Warn(gparams[0]);
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

            gump.Update(Time.Ticks, 0);
            gump.SetInScreen();

            return gump;
        }

        public static void SetTargetLineGump(uint mob)
        {
            if (TargetLine != null && !TargetLine.IsDisposed && TargetLine.Mobile == mob)
                return;

            TargetLine?.Dispose();
            GetGump<TargetLineGump>()?.Dispose();
            TargetLine = null;

            if (TargetLine == null || TargetLine.IsDisposed)
            {
                Mobile mobile = World.Mobiles.Get(mob);

                if (mobile != null)
                {
                    TargetLine = new TargetLineGump(mobile);
                    Add(TargetLine);
                }
            }

            //if (!_targetLineGumps.TryGetValue(mob, out TargetLineGump gump))
            //{
            //    Mobile m = World.Mobiles.Get(mob);
            //    if (m == null)
            //        return;

            //    gump = new TargetLineGump(m);
            //    _targetLineGumps[mob] = gump;
            //    UIManager.Add(gump);
            //}
        }

        public static void RemoveTargetLineGump(uint serial)
        {
            TargetLine?.Dispose();
            GetGump<TargetLineGump>()?.Dispose();
            TargetLine = null;

            //if (_targetLineGumps.TryGetValue(serial, out TargetLineGump gump))
            //{
            //    gump?.Dispose();
            //    _targetLineGumps.Remove(serial);
            //}
        }



        public static T GetGump<T>(uint? serial = null) where T : Control
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

        public static Gump GetGump(uint serial)
        {
            foreach (Control c in Gumps)
            {
                if (!c.IsDisposed && c.LocalSerial == serial)
                {
                    return c as Gump;
                }
            }

            return null;
        }


        public static void Update(double totalMS, double frameMS)
        {
            SortControlsByInfo();

            for (int i = 0; i < Gumps.Count; i++)
            {
                Control g = Gumps[i];

                g.Update(totalMS, frameMS);

                if (g.IsDisposed)
                    Gumps.RemoveAt(i--);
            }

            GameCursor?.Update(totalMS, frameMS);
            HandleKeyboardInput();
            HandleMouseInput();
        }

        public static void Draw(UltimaBatcher2D batcher)
        {
            SortControlsByInfo();

            batcher.GraphicsDevice.Clear(Color.Transparent);

            batcher.Begin();

            for (int i = Gumps.Count - 1; i >= 0; i--)
            {
                Control g = Gumps[i];
                g.Draw(batcher, g.X, g.Y);
            }

            GameCursor?.Draw(batcher);

            batcher.End();
        }

        public static void Add(Control gump)
        {
            if (!gump.IsDisposed)
            {
                Gumps.AddToFront(gump);
                _needSort = true;
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
                _keyboardFocusControl = null;

            if (_keyboardFocusControl == null)
            {
                if (SystemChat != null && !SystemChat.IsDisposed)
                {
                    _keyboardFocusControl = SystemChat.TextBoxControl;
                    _keyboardFocusControl.OnFocusEnter();
                }
                else
                {
                    foreach (Control c in Gumps)
                    {
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
                {
                    if (ProfileManager.Current == null || !ProfileManager.Current.HoldAltToMoveGumps || Keyboard.Alt)
                    {
                        AttemptDragControl(gump, Mouse.Position);
                    }
                }
            }

            MouseOverControl = gump;

            for (int i = 0; i < 5; i++)
            {
                if (_mouseDownControls[i] != null && _mouseDownControls[i] != gump)
                    _mouseDownControls[i].InvokeMouseOver(Mouse.Position);
            }
        }

        private static Control GetMouseOverControl(Point position)
        {
            if (_isDraggingControl)
                return DraggingControl;

            Control control = null;

            bool ismodal = IsModalControlOpen();

            foreach (Control c in Gumps)
            {
                if ((ismodal && !c.ControlInfo.IsModal) || !c.IsVisible || !c.IsEnabled)
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

        public static void MakeTopMostGumpOverAnother(Control control, Control overed)
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


        private static void SortControlsByInfo()
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

        public static void AttemptDragControl(Control control, Point mousePosition, bool attemptAlwaysSuccessful = false)
        {
            if (_isDraggingControl || ItemHold.Enabled)
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
                    if (_needSort && control == dragTarget)
                    {
                        _dragOriginX = mousePosition.X;
                        _dragOriginY = mousePosition.Y;
                    }
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

        private static void DoDragControl(Point mousePosition)
        {
            if (DraggingControl == null)
                return;

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
                DoDragControl(mousePosition);
            DraggingControl?.InvokeDragEnd(mousePosition);
            DraggingControl = null;
            _isDraggingControl = false;
        }
    }
}