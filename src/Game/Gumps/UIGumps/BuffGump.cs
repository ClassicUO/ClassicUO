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
using System.Collections.Generic;
using System.IO;
using System.Linq;

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.IO;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class BuffGump : Gump
    {
        private static BuffGump _gump;
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
          
            _graphic = 0x7580;
            BuildGump();
        }

        private void BuildGump()
        {
            AddChildren(_background = new GumpPic(0, 0, _graphic, 0)
            {
                LocalSerial = 1
            });

            AddChildren(_button = new Button(0, 0x7585, 0x7589, 0x7589)
            {
                X = -2,
                Y = 36,
                ButtonAction = ButtonAction.Activate
            });
            _direction = GumpDirection.LEFT_HORIZONTAL;

            foreach (KeyValuePair<Graphic, BuffIcon> k in World.Player.BuffIcons)
                AddChildren(new BuffControlEntry(World.Player.BuffIcons[k.Key]));
            UpdateElements();
        }


        public override void Save(BinaryWriter writer)
        {
            base.Save(writer);
            writer.Write(_graphic);
            writer.Write((byte)_direction);
        }

        public override void Restore(BinaryReader reader)
        {
            base.Restore(reader);

            _graphic = reader.ReadUInt16();
            _direction = (GumpDirection) reader.ReadByte();
            BuildGump();
        }

        protected override bool Contains(int x, int y)
        {
            return Bounds.Contains(X + x, Y + y);
        }

        public static void Toggle()
        {
            if (Engine.UI.GetByLocalSerial<BuffGump>() == null)
                Engine.UI.Add(_gump = new BuffGump(100, 100));
            else
                _gump.Dispose();
        }

        public void AddBuff(Graphic graphic)
        {
            AddChildren(new BuffControlEntry(World.Player.BuffIcons[graphic]));
            UpdateElements();
        }

        public void RemoveBuff(Graphic graphic)
        {
            RemoveChildren(Children.OfType<BuffControlEntry>().FirstOrDefault(s => s.Icon.Graphic == graphic));
            UpdateElements();
        }

        private void UpdateElements()
        {
            var list = FindControls<BuffControlEntry>();
            int offset = 0;

            foreach (BuffControlEntry e in list)
            {
                switch (_direction)
                {
                    case GumpDirection.LEFT_VERTICAL:
                        e.X = 26;
                        e.Y = 25 + offset;
                        offset += 31;

                        break;
                    case GumpDirection.LEFT_HORIZONTAL:
                        e.X = 26 + offset;
                        e.Y = 5;
                        offset += 31;

                        break;
                    case GumpDirection.RIGHT_VERTICAL:
                        e.X = 5;
                        e.Y = 48 + offset;
                        offset -= 31;

                        break;
                    case GumpDirection.RIGHT_HORIZONTAL:
                        e.X = 48 + offset;
                        e.Y = 5;
                        offset -= 31;

                        break;
                }
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == 0)
            {
                _graphic++;

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
                _background.Texture = FileManager.Gumps.GetTexture(_graphic);
                Width = _background.Texture.Width;
                Height = _background.Texture.Height;

                UpdateElements();
            }
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

            public BuffControlEntry(BuffIcon icon) : base(0, 0, icon.Graphic, 0)
            {
                Icon = icon;
                Texture = FileManager.Gumps.GetTexture(icon.Graphic);
                Width = Texture.Width;
                Height = Texture.Height;
                _alpha = 0xFF;
                _decreaseAlpha = true;
                _timer = (uint) (icon.Timer <= 0 ? 0xFFFF_FFFF : Engine.Ticks + icon.Timer * 1000);

                SetTooltip(icon.Text);
            }

            public BuffIcon Icon { get; }

            public override void Update(double totalMS, double frameMS)
            {
                Texture.Ticks = (long) totalMS;
                int delta = (int) (_timer - totalMS);

                if (_timer != 0xFFFF_FFFF && delta < 10000)
                {
                    if (delta <= 0)
                        Dispose();
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

                base.Update(totalMS, frameMS);
            }

            public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
            {
                return batcher.Draw2D(Texture, position, ShaderHuesTraslator.GetHueVector(0, false, 1.0f - _alpha / 255f, false));
            }
        }
    }
}