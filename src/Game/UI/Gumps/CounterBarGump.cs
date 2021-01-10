#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System.Collections.Generic;
using System.IO;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
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
            {
                rectSize = 30;
            }
            else if (rectSize > 80)
            {
                rectSize = 80;
            }

            if (rows < 1)
            {
                rows = 1;
            }

            if (columns < 1)
            {
                columns = 1;
            }

            _rows = rows;
            _columns = columns;
            _rectSize = rectSize;
            //_isVertical = vertical;

            BuildGump();
        }

        public override GumpType GumpType => GumpType.CounterBar;

        private void BuildGump()
        {
            CanMove = true;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;
            CanCloseWithRightClick = false;
            WantUpdateSize = false;

            Width = _rectSize * _columns + 1;
            Height = _rectSize * _rows + 1;

            Add(_background = new AlphaBlendControl(0.3f) { Width = Width, Height = Height });

            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    Add(new CounterItem(col * _rectSize + 2, row * _rectSize + 2, _rectSize - 4, _rectSize - 4));
                }
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
            {
                rows = 30;
            }

            if (columns > 30)
            {
                columns = 30;
            }

            if (size < 30)
            {
                size = 30;
            }
            else if (size > 80)
            {
                size = 80;
            }

            if (_rectSize != size)
            {
                ok = true;
                _rectSize = size;
            }

            if (rows < 1)
            {
                rows = 1;
            }

            if (_rows != rows)
            {
                ok = true;
                _rows = rows;
            }

            if (columns < 1)
            {
                columns = 1;
            }

            if (_columns != columns)
            {
                ok = true;
                _columns = columns;
            }

            if (ok)
            {
                ApplyLayout();
            }
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

                        c.SetGraphic(c.Graphic, c.Hue);

                        indices[index] = -1;
                    }
                    else
                    {
                        Add(new CounterItem(col * _rectSize + 2, row * _rectSize + 2, _rectSize - 4, _rectSize - 4));
                    }
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
            {
                items[i].SetGraphic(reader.ReadUInt16(), version > 1 ? reader.ReadUInt16() : (ushort) 0);
            }

            IsEnabled = IsVisible = ProfileManager.CurrentProfile.CounterBarEnabled;
        }


        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);

            writer.WriteAttributeString("rows", _rows.ToString());
            writer.WriteAttributeString("columns", _columns.ToString());
            writer.WriteAttributeString("rectsize", _rectSize.ToString());

            IEnumerable<CounterItem> controls = FindControls<CounterItem>();

            writer.WriteStartElement("controls");

            foreach (CounterItem control in controls)
            {
                writer.WriteStartElement("control");
                writer.WriteAttributeString("graphic", control.Graphic.ToString());
                writer.WriteAttributeString("hue", control.Hue.ToString());
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            _rows = int.Parse(xml.GetAttribute("rows"));
            _columns = int.Parse(xml.GetAttribute("columns"));
            _rectSize = int.Parse(xml.GetAttribute("rectsize"));

            BuildGump();

            XmlElement controlsXml = xml["controls"];

            if (controlsXml != null)
            {
                CounterItem[] items = GetControls<CounterItem>();
                int index = 0;

                foreach (XmlElement controlXml in controlsXml.GetElementsByTagName("control"))
                {
                    if (index < items.Length)
                    {
                        items[index++]?.SetGraphic(ushort.Parse(controlXml.GetAttribute("graphic")), ushort.Parse(controlXml.GetAttribute("hue")));
                    }
                    else
                    {
                        Log.Error(ResGumps.IndexOutOfbounds);
                    }
                }
            }

            IsEnabled = IsVisible = ProfileManager.CurrentProfile.CounterBarEnabled;
        }


        private class CounterItem : Control
        {
            private int _amount;

            private readonly ImageWithText _image;
            private uint _time;

            public CounterItem(int x, int y, int w, int h)
            {
                AcceptMouseInput = true;
                WantUpdateSize = false;
                CanMove = true;
                CanCloseWithRightClick = false;

                X = x;
                Y = y;
                Width = w;
                Height = h;

                _image = new ImageWithText();
                Add(_image);

                ContextMenu = new ContextMenuControl();
                ContextMenu.Add(ResGumps.UseObject, Use);
                ContextMenu.Add(ResGumps.Remove, RemoveItem);
            }

            public ushort Graphic { get; private set; }

            public ushort Hue { get; private set; }

            public void SetGraphic(ushort graphic, ushort hue)
            {
                _image.ChangeGraphic(graphic, hue);

                if (graphic == 0)
                {
                    return;
                }

                Graphic = graphic;
                Hue = hue;
            }

            public void RemoveItem()
            {
                _image?.ChangeGraphic(0, 0);
                _amount = 0;
                Graphic = 0;
            }

            public void Use()
            {
                if (Graphic == 0)
                {
                    return;
                }

                Item backpack = World.Player.FindItemByLayer(Layer.Backpack);

                if (backpack == null)
                {
                    return;
                }

                Item item = backpack.FindItem(Graphic, Hue);

                if (item != null)
                {
                    GameActions.DoubleClick(item);
                }
            }

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left)
                {
                    if (ItemHold.Enabled)
                    {
                        SetGraphic(ItemHold.Graphic, ItemHold.Hue);

                        GameActions.DropItem
                        (
                            ItemHold.Serial,
                            ItemHold.X,
                            ItemHold.Y,
                            0,
                            ItemHold.Container
                        );
                    }
                }
                else if (button == MouseButtonType.Right && Keyboard.Alt && Graphic != 0)
                {
                    RemoveItem();
                }
                else if (Graphic != 0)
                {
                    base.OnMouseUp(x, y, button);
                }
            }

            protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left)
                {
                    Use();
                }

                return true;
            }

            public override void Update(double totalTime, double frameTime)
            {
                base.Update(totalTime, frameTime);

                if (_time < Time.Ticks)
                {
                    _time = Time.Ticks + 100;

                    if (Graphic == 0)
                    {
                        _image.SetAmount(string.Empty);
                    }
                    else
                    {
                        _amount = 0;

                        for (Item item = (Item) World.Player.Items; item != null; item = (Item) item.Next)
                        {
                            if (item.ItemData.IsContainer && !item.IsEmpty && item.Layer >= Layer.OneHanded && item.Layer <= Layer.Legs)
                            {
                                GetAmount(item, Graphic, Hue, ref _amount);
                            }
                        }

                        if (ProfileManager.CurrentProfile.CounterBarDisplayAbbreviatedAmount)
                        {
                            if (_amount >= ProfileManager.CurrentProfile.CounterBarAbbreviatedAmount)
                            {
                                _image.SetAmount(StringHelper.IntToAbbreviatedString(_amount));

                                return;
                            }
                        }

                        _image.SetAmount(_amount.ToString());
                    }
                }
            }

            private static void GetAmount(Item parent, ushort graphic, ushort hue, ref int amount)
            {
                if (parent == null)
                {
                    return;
                }

                for (LinkedObject i = parent.Items; i != null; i = i.Next)
                {
                    Item item = (Item) i;

                    GetAmount(item, graphic, hue, ref amount);

                    if (item.Graphic == graphic && item.Hue == hue && item.Exists)
                    {
                        amount += item.Amount;
                    }
                }
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                base.Draw(batcher, x, y);


                Texture2D color = SolidColorTextureCache.GetTexture(MouseIsOver ? Color.Yellow : ProfileManager.CurrentProfile.CounterBarHighlightOnAmount && _amount < ProfileManager.CurrentProfile.CounterBarHighlightAmount && Graphic != 0 ? Color.Red : Color.Gray);

                ResetHueVector();

                batcher.DrawRectangle
                (
                    color,
                    x,
                    y,
                    Width,
                    Height,
                    ref HueVector
                );

                return true;
            }


            private class ImageWithText : Control
            {
                private readonly Label _label;
                private readonly TextureControl _textureControl;

                public ImageWithText()
                {
                    CanMove = true;
                    WantUpdateSize = true;
                    AcceptMouseInput = false;

                    _textureControl = new TextureControl
                    {
                        ScaleTexture = true,
                        AcceptMouseInput = false
                    };

                    Add(_textureControl);


                    _label = new Label
                    (
                        "",
                        true,
                        0x35,
                        0,
                        1,
                        FontStyle.BlackBorder
                    )
                    {
                        X = 2,
                        Y = Height - 15
                    };

                    Add(_label);
                }


                public void ChangeGraphic(ushort graphic, ushort hue)
                {
                    if (graphic != 0)
                    {
                        _textureControl.Texture = ArtLoader.Instance.GetTexture(graphic);
                        _textureControl.Hue = hue;

                        _textureControl.IsPartial = TileDataLoader.Instance.StaticData[graphic].IsPartialHue;

                        _textureControl.Width = Parent.Width;
                        _textureControl.Height = Parent.Height;
                        _label.Y = Parent.Height - 15;
                    }
                    else
                    {
                        _textureControl.Texture = null;
                    }
                }


                public void SetAmount(string amount)
                {
                    _label.Text = amount;
                }
            }
        }
    }
}