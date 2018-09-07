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
        enum InputMouseType
        {
            MouseDown,
            MouseUp,
            MousePressed,
            MouseMove,

            MouseWheel
        }


        private static readonly List<GumpControl> _gumps = new List<GumpControl>();


        private static readonly Queue<InputMouseType> _typeQueue = new Queue<InputMouseType>();
        private static readonly Queue<MouseEventArgs>[] _mouseEventsTriggered = new Queue<MouseEventArgs>[4]
        {
            new Queue<MouseEventArgs>(), new Queue<MouseEventArgs>(), new Queue<MouseEventArgs>(), new Queue<MouseEventArgs>()
        };
        private static readonly Queue<MouseWheelEventArgs> _mouseEventsWheelTriggered = new Queue<MouseWheelEventArgs>();

        private static GumpControl _mouseOverControl;

        static GumpManager()
        {
            MouseManager.MouseDown += (sender, e) => { _typeQueue.Enqueue(InputMouseType.MouseDown); _mouseEventsTriggered[(int)InputMouseType.MouseDown].Enqueue(e); };
            MouseManager.MouseUp += (sender, e) =>
            {
                _typeQueue.Enqueue(InputMouseType.MouseUp); _mouseEventsTriggered[(int)InputMouseType.MouseUp].Enqueue(e);
            };
            MouseManager.MousePressed += (sender, e) => { _typeQueue.Enqueue(InputMouseType.MousePressed); _mouseEventsTriggered[(int)InputMouseType.MousePressed].Enqueue(e); };
            MouseManager.MouseMove += (sender, e) => { _typeQueue.Enqueue(InputMouseType.MouseMove); _mouseEventsTriggered[(int)InputMouseType.MouseMove].Enqueue(e); };
            MouseManager.MouseWheel += (sender, e) => { _typeQueue.Enqueue(InputMouseType.MouseWheel); _mouseEventsWheelTriggered.Enqueue(e); };
        }



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
            }

            HandleMouseInput();

            // cleaning
            _typeQueue.Clear();
            for (InputMouseType t = InputMouseType.MouseDown; t <= InputMouseType.MouseMove; t++)
                _mouseEventsTriggered[(int)t].Clear();
            _mouseEventsWheelTriggered.Clear();
        }

        public static void Render(SpriteBatchUI spriteBatch)
        {
            for (int i = 0; i < _gumps.Count; i++)
            {
                var g = _gumps[i];
                g.Draw(spriteBatch,
                    new Vector3(g.X, g.Y, 0));
            }
        }


        private static void HandleMouseInput()
        {
            GumpControl gump = null;

            for (int i = 0; i < _gumps.Count; i++)
            {
                gump = HitTest(_gumps[i], MouseManager.ScreenPosition);
                if (gump != null)
                    break;
            }

            if (_mouseOverControl != null && gump != _mouseOverControl)
            {
                var arg = new MouseEventArgs(MouseManager.ScreenPosition.X, MouseManager.ScreenPosition.Y, 0, 0);
                _mouseOverControl.OnMouseLeft(arg);

                
                if (_mouseOverControl.RootParent != null && (gump == null || gump.RootParent != _mouseOverControl.RootParent))
                    _mouseOverControl.OnMouseLeft(arg);
                
                
                if (gump != null)
                    gump.OnMouseEnter(arg);
            }

            _mouseOverControl = gump;

            if (gump != null)
            {
                bool isdown = false;

                while (_typeQueue.Count > 0)
                {
                    var t = _typeQueue.Dequeue();

                    switch (t)
                    {
                        case InputMouseType.MouseWheel:
                            var evw = _mouseEventsWheelTriggered.Dequeue();
                            gump.OnMouseWheel(evw);
                            break;
                        case InputMouseType.MouseDown:                          
                        case InputMouseType.MouseUp:
                        case InputMouseType.MousePressed:
                            var ev = _mouseEventsTriggered[(int)t].Dequeue();
                            gump.OnMouseButton(ev);

                            isdown = ev.Button == MouseButton.Left && ev.ButtonState == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
    
                            if (ev.Button == MouseButton.Right && t == InputMouseType.MouseUp && gump.RootParent.CanCloseWithRightClick)
                            {
                                gump.RootParent.Dispose();
                                _gumps.Remove(gump.RootParent);
                            }

                            break;
                        case InputMouseType.MouseMove:
                            ev = _mouseEventsTriggered[(int)t].Dequeue();

                            if (isdown && gump.CanMove)
                            {
                                // TODO: add check to viewport

                                gump.RootParent.X += ev.Offset.X;
                                gump.RootParent.Y += ev.Offset.Y;                               
                            }
                            else
                            {
                                gump.OnMouseMove(ev);
                            }

                            break;
                        default:
                            Log.Message(LogTypes.Error, "WRONG MOUSE INPUT");
                            break;
                    }
                }

            }
        }

        private static GumpControl HitTest(GumpControl parent,  Point position)
        {
            var p = parent?.HitTest(position);
            if (p != null && p.Length > 0)
                return p[0];
            return null;
        }


    }
}
