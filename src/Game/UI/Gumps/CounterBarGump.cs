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
using System.IO;
using System.Linq;
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
    internal class CounterBarGump : ResizableGump
    {
        private AlphaBlendControl _background;
        private DataBox _dataBox;
        private int _rectSize;
        private ScissorControl _scissor;

        public CounterBarGump() : base(0, 0, 50, 50, 0, 0, 0)
        {
        }

        public CounterBarGump
            (int x, int y, int rectSize = 30) : base(0, 0, 50, 50, 0, 0, 0)
        {
            X = x;
            Y = y;

            SetCellSize(rectSize);
            BuildGump();
        }

        public override GumpType GumpType => GumpType.CounterBar;


        public void SetCellSize(int size)
        {
            if (size != _rectSize)
            {
                if (size < 30)
                {
                    size = 30;
                }
                else if (size > 80)
                {
                    size = 80;
                }

                SetMinSize(new Point(size + 8, size + 8));

                _rectSize = size;

                SetupLayout();
            }
        }

        private void BuildGump()
        {
            CanMove = true;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;
            CanCloseWithRightClick = false;
            WantUpdateSize = false;

            Add(_background = new AlphaBlendControl(0.3f) { X = BorderSize, Y = BorderSize, Width = Width - BorderSize * 2, Height = Height - BorderSize * 2 });

            Add(_scissor = new ScissorControl(true, BorderSize, BorderSize, 0, 0));
            _dataBox = new DataBox(BorderSize, BorderSize, 0, 0);
            Add(_dataBox);
            Add(new ScissorControl(false));
            _dataBox.WantUpdateSize = true;

            ResizeWindow(new Point(Width, Height));
            OnResize();
        }

        public override void OnResize()
        {
            base.OnResize();

            if (_background != null)
            {
                SetupLayout();
            }
        }

        private void SetupLayout()
        {
            if (_background == null)
            {
                return;
            }

            int width = Width - BorderSize * 2;
            int height = Height - BorderSize * 2;
            _background.Width = width;
            _background.Height = height;
            _dataBox.Width = 0;
            _dataBox.Height = 0;
            _dataBox.WantUpdateSize = true;

            _scissor.Width = width;
            _scissor.Height = height;

            int x = 2;
            int y = 2;

            for (int i = 0; i < _dataBox.Children.Count; i++)
            {
                CounterItem c = (CounterItem) _dataBox.Children[i];

                if (!c.IsDisposed)
                {
                    c.X = x;
                    c.Y = y;
                    c.Width = _rectSize - 4;
                    c.Height = _rectSize - 4;

                    x += _rectSize + 2;

                    if (x + _rectSize > width)
                    {
                        x = 2;
                        y += _rectSize + 2;
                    }
                }
            }
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                ShowBorder = !ShowBorder;
                return true;
            }

            return false;
        }


        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                if (ItemHold.Enabled && ItemHold.Graphic != 0)
                {
                    CounterItem item = new CounterItem(ItemHold.Graphic, ItemHold.Hue);
                    _dataBox.Add(item);
                    GameActions.DropItem(ItemHold.Serial, ItemHold.X, ItemHold.Y, 0, ItemHold.Container);

                    SetupLayout();

                    return;
                }
            }

            base.OnMouseUp(x, y, button);
        }


        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);

            writer.WriteAttributeString("rectsize", _rectSize.ToString());
            writer.WriteAttributeString("width", Width.ToString());
            writer.WriteAttributeString("height", Height.ToString());

            writer.WriteStartElement("controls");

            foreach (CounterItem control in _dataBox.Children.Cast<CounterItem>())
            {
                if (control.Graphic != 0)
                {
                    writer.WriteStartElement("control");
                    writer.WriteAttributeString("graphic", control.Graphic.ToString());
                    writer.WriteAttributeString("hue", control.Hue.ToString());
                    writer.WriteEndElement();
                }
            }

            writer.WriteEndElement();
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            SetCellSize(int.Parse(xml.GetAttribute("rectsize")));

            if (!int.TryParse(xml.GetAttribute("width"), out int width))
            {
                width = 200;
            }

            if (!int.TryParse(xml.GetAttribute("height"), out int height))
            {
                height = 80;
            }

            Width = width;
            Height = height;

            BuildGump();

            XmlElement controlsXml = xml["controls"];

            if (controlsXml != null)
            {
                foreach (XmlElement controlXml in controlsXml.GetElementsByTagName("control"))
                {
                    ushort graphic = ushort.Parse(controlXml.GetAttribute("graphic"));

                    if (graphic != 0)
                    {
                        CounterItem c = new CounterItem(graphic, ushort.Parse(controlXml.GetAttribute("hue")));
                        _dataBox.Add(c);
                    }
                }
            }

            IsEnabled = IsVisible = ProfileManager.CurrentProfile.CounterBarEnabled;

            SetupLayout();
        }


        private class CounterItem : Control
        {
            private int _amount;

            private readonly TextureControl _image;
            private uint _time;
            private string _amountText = "0";

            public CounterItem(ushort graphic, ushort hue) : this()
            {
                SetGraphic(graphic, hue);
            }

            public CounterItem()
            {
                AcceptMouseInput = true;
                WantUpdateSize = false;
                CanMove = true;
                CanCloseWithRightClick = false;


                _image = new TextureControl
                {
                    ScaleTexture = true,
                    AcceptMouseInput = false,
                };
                Add(_image);

                ContextMenu = new ContextMenuControl();
                ContextMenu.Add(ResGumps.UseObject, Use);
                ContextMenu.Add(ResGumps.Remove, RemoveItem);
            }


            public ushort Graphic { get; private set; }
            public ushort Hue { get; private set; }


            public void SetGraphic(ushort graphic, ushort hue)
            {
                ChangeGraphic(graphic, hue);

                if (graphic == 0)
                {
                    return;
                }

                Graphic = graphic;
                Hue = hue;
            }

            public void RemoveItem()
            {
                ChangeGraphic(0, 0);
                _amount = 0;
                Graphic = 0;
                Dispose();

                if (RootParent is CounterBarGump g)
                {
                    g.SetupLayout();
                }
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
                        GameActions.DropItem(ItemHold.Serial, ItemHold.X, ItemHold.Y, 0, ItemHold.Container);
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

                if (!IsDisposed)
                {
                    _image.Width = Width;
                    _image.Height = Height;
                }

                if (_time < Time.Ticks)
                {
                    _time = Time.Ticks + 100;

                    if (Graphic == 0)
                    {
                        if (!string.IsNullOrEmpty(_amountText))
                        {
                            _amountText = string.Empty;
                        }
                    }
                    else
                    {
                        int newAmount = 0;

                        for (Item item = (Item) World.Player.Items; item != null; item = (Item) item.Next)
                        {
                            if (item.ItemData.IsContainer && !item.IsEmpty && item.Layer >= Layer.OneHanded && item.Layer <= Layer.Legs)
                            {
                                GetAmount(item, Graphic, Hue, ref newAmount);
                            }
                        }

                        if (ProfileManager.CurrentProfile.CounterBarDisplayAbbreviatedAmount)
                        {
                            if (newAmount >= ProfileManager.CurrentProfile.CounterBarAbbreviatedAmount)
                            {
                                _amount = newAmount;
                                _amountText = StringHelper.IntToAbbreviatedString(newAmount);
                            }
                        }

                        if (newAmount != _amount)
                        {
                            _amount = newAmount;
                            _amountText = _amount.ToString();
                        }
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
                ResetHueVector();

                if (IsVisible && IsEnabled && base.Draw(batcher, x, y))
                {
                    Texture2D color = SolidColorTextureCache.GetTexture
                    (
                        MouseIsOver ? Color.Yellow :
                        ProfileManager.CurrentProfile.CounterBarHighlightOnAmount &&
                        _amount < ProfileManager.CurrentProfile.CounterBarHighlightAmount && Graphic != 0 ? Color.Red : Color.Gray
                    );

                    ResetHueVector();
                    batcher.DrawRectangle(color, x, y, Width, Height, ref HueVector);

                    Vector2 size = Fonts.Bold.MeasureString(_amountText);
                    batcher.DrawString(Fonts.Bold, _amountText, x + 2, y + Height - (int) size.Y, ref HueVector);

                    return true;
                }
                
                return false;
            }


            private void ChangeGraphic(ushort graphic, ushort hue)
            {
                if (graphic != 0)
                {
                    _image.Texture = ArtLoader.Instance.GetTexture(graphic);
                    _image.Hue = hue;
                    _image.IsPartial = TileDataLoader.Instance.StaticData[graphic].IsPartialHue;
                    _image.Width = Width;
                    _image.Height = Height;
                }
                else
                {
                    _image.Texture = null;
                }
            }
        }
    }
}