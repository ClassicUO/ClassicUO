﻿#region license

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

using System.IO;
using System.Linq;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    internal class CounterBarGump : Gump
    {
        private AlphaBlendControl _background;

        private int _rows, _columns, _rectSize;
        //private bool _isVertical;

        public CounterBarGump() : base(0, 0)
        {
        }

        public CounterBarGump(int x, int y, int rectSize = 30, int rows = 1, int columns = 1 /*, bool vertical = false*/) : base(0, 0)
        {
            X = x;
            Y = y;

            if (rectSize < 30)
                rectSize = 30;
            else if (rectSize > 80)
                rectSize = 80;

            if (rows < 1)
                rows = 1;

            if (columns < 1)
                columns = 1;

            _rows = rows;
            _columns = columns;
            _rectSize = rectSize;
            //_isVertical = vertical;

            BuildGump();
        }

        private void BuildGump()
        {
            CanMove = true;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;
            CanCloseWithRightClick = false;
            WantUpdateSize = false;
            CanBeSaved = true;

            Width = _rectSize * _columns + 1;
            Height = _rectSize * _rows + 1;

            Add(_background = new AlphaBlendControl(0.3f) {Width = Width, Height = Height});

            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++) Add(new CounterItem(col * _rectSize + 2, row * _rectSize + 2, _rectSize - 4, _rectSize - 4));
            }
        }

        public void SetLayout(int size, int rows, int columns)
        {
            bool ok = false;

            //if (_isVertical != isvertical)
            //{
            //    _isVertical = isvertical;
            //    int temp = _rows;
            //    _rows = _columns;
            //    _columns = temp;
            //    ok = true;
            //}

            if (rows > 30)
                rows = 30;

            if (columns > 30)
                columns = 30;

            if (size < 30)
                size = 30;
            else if (size > 80)
                size = 80;

            if (_rectSize != size)
            {
                ok = true;
                _rectSize = size;
            }

            if (rows < 1)
                rows = 1;

            if (_rows != rows)
            {
                ok = true;
                _rows = rows;
            }

            if (columns < 1)
                columns = 1;

            if (_columns != columns)
            {
                ok = true;
                _columns = columns;
            }

            if (ok) ApplyLayout();
        }

        private void ApplyLayout()
        {
            Width = _rectSize * _columns + 1;
            Height = _rectSize * _rows + 1;

            _background.Width = Width;
            _background.Height = Height;

            CounterItem[] items = GetControls<CounterItem>();

            int[] indices = new int[items.Length];

            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    int index = /*_isVertical ? col * _rows + row :*/ row * _columns + col;

                    if (index < items.Length)
                    {
                        CounterItem c = items[index];

                        c.X = col * _rectSize + 2;
                        c.Y = row * _rectSize + 2;
                        c.Width = _rectSize - 4;
                        c.Height = _rectSize - 4;

                        TextureControl textControl = c.Children.OfType<TextureControl>().FirstOrDefault();

                        if (textControl != null)
                        {
                            textControl.Width = c.Width;
                            textControl.Height = c.Height;
                        }

                        indices[index] = -1;
                    }
                    else
                        Add(new CounterItem(col * _rectSize + 2, row * _rectSize + 2, _rectSize - 4, _rectSize - 4));
                }
            }

            for (int i = 0; i < indices.Length; i++)
            {
                int index = indices[i];

                if (index >= 0 && index < items.Length)
                {
                    items[i].Parent = null;
                    items[i].Dispose();
                }
            }

            SetInScreen();
        }

        public override void Save(BinaryWriter writer)
        {
            base.Save(writer);

            writer.Write((byte) 2);
            writer.Write(_rows);
            writer.Write(_columns);
            writer.Write(_rectSize);

            CounterItem[] controls = GetControls<CounterItem>();

            writer.Write(controls.Length);

            foreach (CounterItem c in controls)
            {
                writer.Write(c.Graphic);
                writer.Write(c.Hue);
            }
        }

        public override void Restore(BinaryReader reader)
        {
            base.Restore(reader);

            byte version = reader.ReadByte();
            _rows = reader.ReadInt32();
            _columns = reader.ReadInt32();
            _rectSize = reader.ReadInt32();

            int count = reader.ReadInt32();

            BuildGump();

            CounterItem[] items = GetControls<CounterItem>();

            for (int i = 0; i < count; i++)
                items[i].SetGraphic(reader.ReadUInt16(), version > 1 ? reader.ReadUInt16() : (ushort) 0);

            IsEnabled = IsVisible = Engine.Profile.Current.CounterBarEnabled;
        }



        private class CounterItem : Control
        {
            private int _amount;
            private TextureControl _controlPic;
            private Graphic _graphic;
            private Hue _hue;
            private uint _time;


            public CounterItem(int x, int y, int w, int h)
            {
                AcceptMouseInput = true;
                WantUpdateSize = false;
                CanMove = true;

                X = x;
                Y = y;
                Width = w;
                Height = h;
            }

            public ushort Graphic => _graphic;
            public ushort Hue => _hue;

            public void SetGraphic(ushort graphic, ushort hue)
            {
                if (graphic == 0)
                    return;

                _graphic = graphic;
                _hue = hue;

                _controlPic?.Dispose();

                _controlPic = new TextureControl
                {
                    ScaleTexture = true,
                    Texture = FileManager.Art.GetTexture(_graphic),
                    Hue = hue,
                    //Hue = gs.HeldItem.Hue,
                    //IsPartial = gs.HeldItem.IsPartialHue,
                    Width = Width,
                    Height = Height,
                    AcceptMouseInput = false
                };
                Add(_controlPic);
            }

            protected override void OnMouseUp(int x, int y, MouseButton button)
            {
                if (button == MouseButton.Left)
                {
                    GameScene gs = Engine.SceneManager.GetScene<GameScene>();

                    if (!gs.IsHoldingItem || !gs.IsMouseOverUI)
                        return;

                    Item item = World.Items.Get(gs.HeldItem.Container);

                    if (item == null)
                        return;

                    SetGraphic(gs.HeldItem.Graphic, gs.HeldItem.Hue);

                    gs.DropHeldItemToContainer(item, gs.HeldItem.Position.X, gs.HeldItem.Position.Y);
                }
                else if (button == MouseButton.Right && Keyboard.Alt && _graphic != 0)
                {
                    _controlPic?.Dispose();
                    _amount = 0;
                    _graphic = 0;
                }
            }

            protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
            {
                if (button == MouseButton.Left)
                {
                    Item backpack = World.Player.Equipment[(int) Layer.Backpack];
                    Item item = backpack.FindItem(_graphic, _hue);

                    if (item != null)
                        GameActions.DoubleClick(item);
                }

                return true;
            }

            public override void Update(double totalMS, double frameMS)
            {
                base.Update(totalMS, frameMS);


                if (_time < Engine.Ticks)
                {
                    _time = Engine.Ticks + 100;

                    _amount = 0;
                    GetAmount(World.Player.Equipment[(int) Layer.Backpack], _graphic, _hue, ref _amount);
                }
            }

            private static void GetAmount(Item parent, Graphic graphic, Hue hue, ref int amount)
            {
                if (parent == null)
                    return;

                foreach (Item item in parent.Items)
                {
                    GetAmount(item, graphic, hue, ref amount);

                    if (item.Graphic == graphic && item.Hue == hue)
                        amount += item.Amount;
                }
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                base.Draw(batcher, x, y);

                string text = _amount.ToString();

                if (_amount >= 1000)
                {
                    if (text.Length > 4)
                    {
                        if (text.Length > 5) // >= 100.000
                            text = $"{text.Substring(0, 3)}K+";
                        else // <= 10.000
                            text = $"{text.Substring(0, 2)}K+";
                    }
                    else // 1.000
                        text = $"{text[0]}K+";
                }

                Texture2D color = Textures.GetTexture(Color.Gray);

                if (Engine.Profile.Current.CounterBarHighlightOnAmount &&
                    _amount < Engine.Profile.Current.CounterBarHighlightAmount && _graphic != 0)
                    color = Textures.GetTexture(Color.Red);

                ResetHueVector();
                batcher.DrawRectangle(color, x, y, Width, Height, ref _hueVector);

                if (_graphic != 0)
                {
                    _hueVector.X = 59;
                    _hueVector.Y = 1;
                    _hueVector.Z = 0;

                    batcher.DrawString(Fonts.Bold, text, x + 2, y + Height - 15, ref _hueVector);
                }


                return true;
            }
        }
    }
}