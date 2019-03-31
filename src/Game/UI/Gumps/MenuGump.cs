using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    class MenuGump : Gump
    {
        private readonly ContainerHorizontal _container;
        private bool _isDown, _isLeft;

        public MenuGump(Serial serial, Serial serv, string name) : base(serial, serv)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;

            Add(new GumpPic(0, 0, 0x0910, 0));
            Add(new ColorBox(217, 49, 0, 0xFF000001)
            {
                X = 40,
                Y = 42,
            });

            Label label = new Label(name, false, 0x0386, 200, 1, FontStyle.Fixed)
            {
                X = 39,
                Y = 18
            };

            Add(label);

            _container = new ContainerHorizontal()
            {
                X = 40,
                Y = 42,
                Width = 217,
                Height = 49,
                WantUpdateSize = false
            };

            Add(_container);




            HitBox left = new HitBox(25, 60, 10, 15)
            {
                IsTransparent = true,
                Alpha = 1,
            };
            left.MouseDown += (sender, e) =>
            {
                _isDown = true;
                _isLeft = true;
            };

            left.MouseUp += (sender, e) =>
            {
                _isDown = false;
            };
            Add(left);


            HitBox right = new HitBox(260, 60, 10, 15)
            {
                IsTransparent = true,
                Alpha = 1,
            };
            right.MouseDown += (sender, e) =>
            {
                _isDown = true;
                _isLeft = false;
            };

            right.MouseUp += (sender, e) =>
            {
                _isDown = false;
            };
            Add(right);
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (_isDown)
            {
                _container.Value += (_isLeft ? -1 : 1);
            }
        }


        public void AddItem(Graphic graphic, Hue hue, string name, int x, int y, int index)
        {
            StaticPic pic = new StaticPic(graphic, hue)
            {
                X = x,
                Y = y,
                //LocalSerial = (uint) index,
                AcceptMouseInput = true,
            };
            pic.MouseDoubleClick += (sender, e) =>
            {
                NetClient.Socket.Send(new PMenuResponse(LocalSerial, (Graphic) ServerSerial.Value, index, graphic, hue));
                Dispose();
            };
            pic.SetTooltip(name);


            _container.Add(pic);
        }

        class ContainerHorizontal : Control
        {
            private int _maxWidth;
            private bool _update = true;
            private int _value;

            public ContainerHorizontal()
            {

            }

            public int Value
            {
                get => _value;
                set
                {
                    if (value < 0)
                        value = 0;
                    else if (value > _maxWidth)
                        value = _maxWidth;

                    _value = value;
                }
            }

            protected override void OnInitialize()
            {
                _update = true;
                base.OnInitialize();
            }

            public override void Update(double totalMS, double frameMS)
            {
                base.Update(totalMS, frameMS);

                if (_update)
                {
                    _update = false;

                    CalculateWidth();
                }
            }
            
            public override bool Draw(Batcher2D batcher, int x, int y)
            {
                Rectangle scissor = ScissorStack.CalculateScissors(batcher.TransformMatrix, x, y, Width, Height);

                if (ScissorStack.PushScissors(scissor))
                {
                    batcher.EnableScissorTest(true);

                    int width = 0;
                    int maxWidth = Value + Width;
                    bool drawOnly1 = true;

                    for (int i = 0; i < Children.Count; i++)
                    {
                        Control child = Children[i];

                        if (!child.IsVisible)
                            continue;

                        child.X = width - Value;

                        if (width + child.Width <= Value)
                        {

                        }
                        else if (width + child.Width <= maxWidth)
                        {
                            child.Draw(batcher, x, y);
                        }
                        else
                        {
                            if (drawOnly1)
                            {
                                child.Draw(batcher, x, y);
                                drawOnly1 = false;
                            }
                        }

                        width += child.Width;
                    }


                    batcher.EnableScissorTest(false);
                    ScissorStack.PopScissors();
                }

                return true; // base.Draw(batcher,position, hue);
            }

            protected override void OnChildAdded()
            {
                _update = true;
            }

            protected override void OnChildRemoved()
            {
                _update = true;
            }

            private void CalculateWidth()
            {
                _maxWidth = Children.Sum(s => s.Width) - Width;

                if (_maxWidth < 0)
                    _maxWidth = 0;
            }
        }


    }

    class GrayMenuGump : Gump
    {
        private readonly ResizePic _resizePic;
        public GrayMenuGump(Serial local, Serial serv, string name) : base(local, serv)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = false;

            Add(_resizePic = new ResizePic(0x13EC)
            {
                Width = 400,
                Height = 111111,
            });

            Label l;
            Add(l = new Label(name, false, 0x0386, 370, 1)
            {
                X = 20,
                Y = 16
            });

            Width = _resizePic.Width;
            Height = l.Height;
        }

        public void SetHeight(int h)
        {
            _resizePic.Height = h;
            Width = _resizePic.Width;
            Height = _resizePic.Height;
        }


        public int AddItem(string name, int y)
        {
            RadioButton radio = new RadioButton(0, 0x138A, 0x138B, name, 1, 0x0386, false, 330)
            {
                X = 50,
                Y = y
            };

            Add(radio);

            return radio.Height;
        }

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 0: // cancel
                    NetClient.Socket.Send(new PGrayMenuResponse(LocalSerial, (Graphic) ServerSerial.Value, 0));

                    Dispose();
                    break;

                case 1: // continue

                    ushort index = 1;
                    foreach (RadioButton radioButton in Children.OfType<RadioButton>())
                    {
                        if (radioButton.IsChecked)
                        {
                            NetClient.Socket.Send(new PGrayMenuResponse(LocalSerial, (Graphic)ServerSerial.Value, index));
                            break;
                        }

                        index++;
                    }
                    Dispose();
                    break;
            }
        }
    }
}
