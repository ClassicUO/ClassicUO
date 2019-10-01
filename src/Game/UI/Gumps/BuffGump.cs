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
using System.IO;
using System.Linq;

using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using ClassicUO.IO;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class BuffGump : Gump
    {
        private GumpPic _background;
        private Button _button;
        private GumpDirection _direction;
        private ushort _graphic;

        public BuffGump() : base(0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;
            CanBeSaved = true;
        }

        public BuffGump(int x, int y) : this()
        {
            X = x;
            Y = y;

            SetInScreen();

            _graphic = 0x7580;
            BuildGump();
        }

        private void BuildGump()
        {
            WantUpdateSize = false;

            Add(_background = new GumpPic(0, 0, _graphic, 0)
            {
                LocalSerial = 1
            });

            Add(_button = new Button(0, 0x7585, 0x7589, 0x7589)
            {
                X = -2,
                Y = 36,
                ButtonAction = ButtonAction.Activate
            });
            _direction = GumpDirection.LEFT_HORIZONTAL;


            foreach (KeyValuePair<Graphic, BuffIcon> k in World.Player.BuffIcons)
                Add(new BuffControlEntry(World.Player.BuffIcons[k.Key]));

            Change();
        }

        public override void Save(BinaryWriter writer)
        {
            base.Save(writer);
            writer.Write(_graphic);
            writer.Write((byte) _direction);
        }

        public override void Restore(BinaryReader reader)
        {
            base.Restore(reader);

            _graphic = reader.ReadUInt16();
            _direction = (GumpDirection) reader.ReadByte();
            BuildGump();
        }

        //protected override bool Contains(int x, int y)
        //{
        //    return Bounds.Contains(X + x, Y + y);
        //}

        public void AddBuff(Graphic graphic)
        {
            Add(new BuffControlEntry(World.Player.BuffIcons[graphic]));
            UpdateElements();
        }

        public void RemoveBuff(Graphic graphic)
        {
            foreach (BuffControlEntry entry in Children.OfType<BuffControlEntry>().Where(s => s.Icon.Graphic == graphic))
            {
                if (Height > _background.Texture.Height)
                {
                    int delta = Height - _background.Texture.Height;

                    if (_direction == GumpDirection.RIGHT_VERTICAL)
                    {
                        Y += delta;
                        Height -= delta;
                        _background.Y -= delta;
                        _button.Y -= delta;
                    }
                    else if (_direction == GumpDirection.LEFT_VERTICAL) Height -= delta;
                }

                if (Width > _background.Texture.Width)
                {
                    int delta = Width - _background.Texture.Width;

                    if (_direction == GumpDirection.RIGHT_HORIZONTAL)
                    {
                        X += delta;
                        Width -= delta;
                        _background.X -= delta;
                        _button.X -= delta;
                    }
                    else if (_direction == GumpDirection.LEFT_HORIZONTAL) Width -= delta;
                }

                entry.Dispose();
            }

            UpdateElements();
        }


        private void UpdateElements()
        {
            var list = FindControls<BuffControlEntry>();
            int offset = 0;

            int maxWidth = 0;
            int maxHeight = 0;

            int i = 0;
            foreach (var e in list)
            {
                maxWidth += e.Width;
                maxHeight += e.Height;

                switch (_direction)
                {
                    case GumpDirection.LEFT_VERTICAL:
                        e.X = 26;
                        e.Y = 25 + offset;
                        offset += 31;

                        if (Height < 25 + offset)
                            Height = 25 + offset;

                        break;

                    case GumpDirection.LEFT_HORIZONTAL:
                        e.X = 26 + offset;
                        e.Y = 5;
                        offset += 31;

                        if (Width < 26 + offset)
                            Width = 26 + offset;

                        break;

                    case GumpDirection.RIGHT_VERTICAL:
                        e.X = 5;
                        e.Y = Height - 48 - offset;

                        if (e.Y < 0)
                        {
                            Y += e.Y;
                            Height -= e.Y;
                            _background.Y -= e.Y;
                            _button.Y -= e.Y;

                            int j = 0;
                            foreach (var ee in list)
                            {
                                if (j >= i)
                                    break;
                                ee.Y -= e.Y;
                                j++;
                            }

                            e.Y = Height - 48 - offset;
                        }

                        offset += 31;

                        break;

                    case GumpDirection.RIGHT_HORIZONTAL:
                        e.X = Width - 48 - offset;
                        e.Y = 5;

                        if (e.X < 0)
                        {
                            X += e.X;
                            Width -= e.X;
                            _background.X -= e.X;
                            _button.X -= e.X;

                            int j = 0;
                            foreach (var ee in list)
                            {
                                if (j >= i)
                                    break;
                                ee.X -= e.X;
                                j++;
                            }

                            e.X = Width - 48 - offset;
                        }

                        offset += 31;

                        break;
                }

                i++;
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == 0)
            {
                _graphic++;

                Change();
            }
        }

        private void Change()
        {
            if (_graphic > 0x7582)
                _graphic = 0x757F;

            switch (_graphic)
            {
                case 0x7580:
                    _button.X = -2;
                    _button.Y = 36;
                    _direction = GumpDirection.LEFT_HORIZONTAL;

                    break;

                case 0x7581:
                    _button.X = 34;
                    _button.Y = 78;
                    _direction = GumpDirection.RIGHT_VERTICAL;

                    break;

                case 0x7582:
                    _button.X = 76;
                    _button.Y = 36;
                    _direction = GumpDirection.RIGHT_HORIZONTAL;

                    break;

                case 0x757F:
                default:
                    _button.X = 0;
                    _button.Y = 0;
                    _direction = GumpDirection.LEFT_VERTICAL;

                    break;
            }

            _background.Graphic = _graphic;
            _background.X = 0;
            _background.Y = 0;

            Width = _background.Texture.Width;
            Height = _background.Texture.Height;

            UpdateElements();
        }

        private enum GumpDirection
        {
            LEFT_VERTICAL,
            LEFT_HORIZONTAL,
            RIGHT_VERTICAL,
            RIGHT_HORIZONTAL
        }

        private class BuffControlEntry : GumpPic
        {
            private readonly uint _timer;
            private byte _alpha;
            private bool _decreaseAlpha;

            private float _updateTooltipTime;

            public BuffControlEntry(BuffIcon icon) : base(0, 0, icon.Graphic, 0)
            {
                Icon = icon;
                Width = Texture.Width;
                Height = Texture.Height;
                _alpha = 0xFF;
                _decreaseAlpha = true;
                _timer = (uint) (icon.Timer <= 0 ? 0xFFFF_FFFF : Engine.Ticks + icon.Timer * 1000);

                SetTooltip(icon.Text);
            }

            public BuffIcon Icon { get; }

            protected override void OnInitialize()
            {
                base.OnInitialize();
                AcceptMouseInput = true;
                WantUpdateSize = false;
                CanMove = true;
            }

            public override void Update(double totalMS, double frameMS)
            {
                base.Update(totalMS, frameMS);

                Texture.Ticks = (long) totalMS;
                int delta = (int) (_timer - totalMS);


                if (_updateTooltipTime < totalMS && delta > 0)
                {
                    TimeSpan span = TimeSpan.FromMilliseconds(delta);
                    SetTooltip($"{Icon.Text}\nTime left: {span.Hours:00}:{span.Minutes:00}:{span.Seconds:00}");
                    _updateTooltipTime = (float) totalMS + 1000;
                }

                if (_timer != 0xFFFF_FFFF && delta < 10000)
                {
                    if (delta <= 0)
                        ((BuffGump) Parent).RemoveBuff(Icon.Graphic);
                    else
                    {
                        int alpha = _alpha;
                        int addVal = (10000 - delta) / 600;

                        if (_decreaseAlpha)
                        {
                            alpha -= addVal;

                            if (alpha <= 60)
                            {
                                _decreaseAlpha = false;
                                alpha = 60;
                            }
                        }
                        else
                        {
                            alpha += addVal;

                            if (alpha >= 255)
                            {
                                _decreaseAlpha = true;
                                alpha = 255;
                            }
                        }

                        _alpha = (byte) alpha;
                    }
                }
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                ResetHueVector();
                ShaderHuesTraslator.GetHueVector(ref _hueVector, 0, false, 1.0f - _alpha / 255f, true);

                return batcher.Draw2D(Texture, x, y, ref _hueVector);
            }
        }
    }
}