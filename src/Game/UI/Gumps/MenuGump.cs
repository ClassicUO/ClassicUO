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

using System.Linq;

using ClassicUO.Game.UI.Controls;
using ClassicUO.Network;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class MenuGump : Gump
    {
        private readonly ContainerHorizontal _container;
        private readonly HSliderBar _slider;
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
                Y = 42
            });

            Label label = new Label(name, false, 0x0386, 200, 1, FontStyle.Fixed)
            {
                X = 39,
                Y = 18
            };

            Add(label);

            _container = new ContainerHorizontal
            {
                X = 40,
                Y = 42,
                Width = 217,
                Height = 49,
                WantUpdateSize = false
            };

            Add(_container);

            Add(_slider = new HSliderBar(40, _container.Y + _container.Height + 12, 217, 0, 1, 0, HSliderBarStyle.MetalWidgetRecessedBar));
            _slider.ValueChanged += (sender, e) => { _container.Value = _slider.Value; };

            HitBox left = new HitBox(25, 60, 10, 15)
            {
                Alpha = 1
            };

            left.MouseDown += (sender, e) =>
            {
                _isDown = true;
                _isLeft = true;
            };

            left.MouseUp += (sender, e) => { _isDown = false; };
            Add(left);


            HitBox right = new HitBox(260, 60, 10, 15)
            {
                Alpha = 1
            };

            right.MouseDown += (sender, e) =>
            {
                _isDown = true;
                _isLeft = false;
            };

            right.MouseUp += (sender, e) => { _isDown = false; };
            Add(right);
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (_isDown) _container.Value += _isLeft ? -1 : 1;
        }


        public void AddItem(Graphic graphic, Hue hue, string name, int x, int y, int index)
        {
            StaticPic pic = new StaticPic(graphic, hue)
            {
                X = x,
                Y = y,
                //LocalSerial = (uint) index,
                AcceptMouseInput = true
            };

            pic.MouseDoubleClick += (sender, e) =>
            {
                NetClient.Socket.Send(new PMenuResponse(LocalSerial, (Graphic) ServerSerial.Value, index, graphic, hue));
                Dispose();
            };
            pic.SetTooltip(name);


            _container.Add(pic);

            _container.CalculateWidth();
            _slider.MaxValue = _container.MaxValue;
        }

        private class ContainerHorizontal : Control
        {
            private bool _update = true;
            private int _value;

            public int Value
            {
                get => _value;
                set
                {
                    if (value < 0)
                        value = 0;
                    else if (value > MaxValue)
                        value = MaxValue;

                    _value = value;
                }
            }

            public int MaxValue { get; private set; }

            protected override void OnInitialize()
            {
                _update = true;
                base.OnInitialize();
            }

            public override void Update(double totalMS, double frameMS)
            {
                base.Update(totalMS, frameMS);

                //if (_update)
                //{
                //    _update = false;

                //    CalculateWidth();
                //}
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                Rectangle scissor = ScissorStack.CalculateScissors(Matrix.Identity, x, y, Width, Height);

                if (ScissorStack.PushScissors(scissor))
                {
                    batcher.EnableScissorTest(true);

                    int width = 0;
                    int maxWidth = Value + Width;
                    bool drawOnly1 = true;

                    foreach (Control child in Children)
                    {
                        if (!child.IsVisible)
                            continue;

                        child.X = width - Value;

                        if (width + child.Width <= Value)
                        {
                        }
                        else if (width + child.Width <= maxWidth)
                            child.Draw(batcher, child.X + x, y);
                        else
                        {
                            if (drawOnly1)
                            {
                                child.Draw(batcher, child.X + x, y);
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

            public void CalculateWidth()
            {
                MaxValue = Children.Sum(s => s.Width) - Width;

                if (MaxValue < 0)
                    MaxValue = 0;
            }
        }
    }

    internal class GrayMenuGump : Gump
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
                Height = 111111
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
                            NetClient.Socket.Send(new PGrayMenuResponse(LocalSerial, (Graphic) ServerSerial.Value, index));

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