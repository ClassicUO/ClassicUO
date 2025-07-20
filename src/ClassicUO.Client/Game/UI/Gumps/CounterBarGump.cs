// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace ClassicUO.Game.UI.Gumps
{
    internal class CounterBarGump : ResizableGump
    {
        private AlphaBlendControl _background;

        private DataBox _dataBox;
        private int _rectSize;
        private ScissorControl _scissor;

        public CounterBarGump(World world) : base(world, 0, 0, 50, 50, 0, 0, 0)
        {
            ContextMenu = new ContextMenuControl(this);
            ContextMenu.Add(ResGumps.Add, AddPlaceholder);
        }

        public CounterBarGump(
            World world,
            int x,
            int y,
            int rectSize = 30
        ) : this(world)
        {
            X = x;
            Y = y;

            SetCellSize(rectSize);

            BuildGump();
        }

        private void AddPlaceholder()
        {
            _dataBox.Add(new CounterItem(this, 0, 0));
            SetupLayout();
        }

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
                _rectSize = size;
                SetupLayout();
            }
        }

        public override GumpType GumpType => GumpType.CounterBar;

        private void BuildGump()
        {
            CanMove = true;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;
            CanCloseWithRightClick = false;
            WantUpdateSize = false;
            int borderSize = BoderSize;

            Add(_background = new AlphaBlendControl(0.7f) { X = borderSize, Y = borderSize, Width = Width - borderSize * 2, Height = Height - borderSize * 2 });

            Add(_scissor = new ScissorControl(true, borderSize, borderSize, 0, 0));
            _dataBox = new DataBox(borderSize, borderSize, 0, 0);
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

            int width = Width - this.BoderSize * 2;
            int height = Height - this.BoderSize * 2;
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
                CounterItem c = (CounterItem)_dataBox.Children[i];
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
                if (Client.Game.UO.GameCursor.ItemHold.Enabled && Client.Game.UO.GameCursor.ItemHold.Graphic != 0)
                {
                    CounterItem item = new CounterItem(this, Client.Game.UO.GameCursor.ItemHold.Graphic, Client.Game.UO.GameCursor.ItemHold.Hue);
                    _dataBox.Add(item);
                    GameActions.DropItem(Client.Game.UO.GameCursor.ItemHold.Serial, Client.Game.UO.GameCursor.ItemHold.X, Client.Game.UO.GameCursor.ItemHold.Y, 0, Client.Game.UO.GameCursor.ItemHold.Container);

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

            IEnumerable<CounterItem> controls = FindControls<CounterItem>();

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
                        CounterItem c = new CounterItem(this, graphic, ushort.Parse(controlXml.GetAttribute("hue")));
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
            private readonly ImageWithText _image;
            private uint _time;
            private readonly CounterBarGump _gump;

            public CounterItem(CounterBarGump gump, ushort graphic, ushort hue)
            {
                _gump = gump;
                
                AcceptMouseInput = true;
                WantUpdateSize = false;
                CanMove = true;
                CanCloseWithRightClick = false;

                _image = new ImageWithText();
                Add(_image);

                SetGraphic(graphic, hue);
            }

            public ushort Graphic { get; private set; }

            public ushort Hue { get; private set; }

            public void SetGraphic(ushort graphic, ushort hue)
            {
                _image.ChangeGraphic(graphic, hue);

                Graphic = graphic;
                Hue = hue;

                ContextMenu = new ContextMenuControl(_gump);
                if (graphic != 0)
                {
                    ContextMenu.Add(ResGumps.UseObject, Use);
                }
                ContextMenu.Add(ResGumps.Remove, RemoveItem);
            }

            public void RemoveItem()
            {
                _image?.ChangeGraphic(0, 0);
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

                Item backpack = _gump.World.Player.FindItemByLayer(Layer.Backpack);

                if (backpack == null)
                {
                    return;
                }

                Item item = backpack.FindItem(Graphic, Hue);

                if (item != null)
                {
                    GameActions.DoubleClick(_gump.World, item);
                }
            }

            protected override void OnMouseOver(int x, int y)
            {
                base.OnMouseOver(x, y);

                if (_gump.World.Player.FindItemByLayer(Layer.Backpack)?.FindItem(Graphic, Hue) is {} item)
                    SetTooltip(item);
            }

            protected override void OnMouseExit(int x, int y)
            {
                base.OnMouseExit(x, y);
                ClearTooltip();
            }

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left)
                {
                    if (Client.Game.UO.GameCursor.ItemHold.Enabled)
                    {
                        SetGraphic(
                            Client.Game.UO.GameCursor.ItemHold.Graphic,
                            Client.Game.UO.GameCursor.ItemHold.Hue
                        );

                        GameActions.DropItem(
                            Client.Game.UO.GameCursor.ItemHold.Serial,
                            Client.Game.UO.GameCursor.ItemHold.X,
                            Client.Game.UO.GameCursor.ItemHold.Y,
                            0,
                            Client.Game.UO.GameCursor.ItemHold.Container
                        );
                    }
                    else if (ProfileManager.CurrentProfile.CastSpellsByOneClick)
                    {
                        Use();
                    }
                }
                else if (button == MouseButtonType.Right && Keyboard.Alt )
                {
                    RemoveItem();
                }
                else if (button == MouseButtonType.Right)
                {
                    base.OnMouseUp(x, y, button);
                }
                else if (Graphic != 0)
                {
                    base.OnMouseUp(x, y, button);
                }
            }

            protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
            {
                if (
                    button == MouseButtonType.Left
                    && !ProfileManager.CurrentProfile.CastSpellsByOneClick
                )
                {
                    Use();
                }

                return true;
            }

            public override void Update()
            {
                base.Update();

                if (!IsDisposed)
                {
                    _image.Width = Width;
                    _image.Height = Height;
                }

                if (Parent != null && Parent.IsEnabled && _time < Time.Ticks)
                {
                    _time = Time.Ticks + 100;

                    if (Graphic == 0)
                    {
                        _image.SetAmount(string.Empty);
                    }
                    else
                    {
                        _amount = 0;

                        for (
                            Item item = (Item)_gump.World.Player.Items;
                            item != null;
                            item = (Item)item.Next
                        )
                        {
                            if (
                                item.ItemData.IsContainer
                                && !item.IsEmpty
                                && item.Layer >= Layer.OneHanded
                                && item.Layer <= Layer.Legs
                            )
                            {
                                GetAmount(item, Graphic, Hue, ref _amount);
                            }
                        }

                        if (ProfileManager.CurrentProfile.CounterBarDisplayAbbreviatedAmount)
                        {
                            if (
                                _amount >= ProfileManager.CurrentProfile.CounterBarAbbreviatedAmount
                            )
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
                    Item item = (Item)i;

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

                Texture2D color = SolidColorTextureCache.GetTexture(
                    MouseIsOver
                        ? Color.Yellow
                        : ProfileManager.CurrentProfile.CounterBarHighlightOnAmount
                        && _amount < ProfileManager.CurrentProfile.CounterBarHighlightAmount
                        && Graphic != 0
                            ? Color.Red
                            : Color.Gray
                );

                Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

                batcher.DrawRectangle(color, x, y, Width, Height, hueVector);

                return true;
            }

            private class ImageWithText : Control
            {
                private readonly Label _label;
                private ushort _graphic;
                private ushort _hue;
                private bool _partial;

                public ImageWithText()
                {
                    CanMove = true;
                    WantUpdateSize = true;
                    AcceptMouseInput = false;

                    _label = new Label("", true, 0x35, 0, 1, FontStyle.BlackBorder)
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
                        _graphic = graphic;
                        _hue = hue;
                        _partial = Client.Game.UO.FileManager.TileData.StaticData[graphic].IsPartialHue;
                    }
                    else
                    {
                        _graphic = 0;
                    }
                }

                public override void Update()
                {
                    base.Update();

                    if (Parent != null)
                    {
                        Width = Parent.Width;
                        Height = Parent.Height;
                        _label.Y = Parent.Height - 15;
                    }
                }

                public override bool Draw(UltimaBatcher2D batcher, int x, int y)
                {
                    if (_graphic != 0)
                    {
                        ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(_graphic);
                        var rect = Client.Game.UO.Arts.GetRealArtBounds(_graphic);

                        Vector3 hueVector = ShaderHueTranslator.GetHueVector(_hue, _partial, 1f);

                        Point originalSize = new Point(Width, Height);
                        Point point = new Point();

                        if (rect.Width < Width)
                        {
                            originalSize.X = rect.Width;
                            point.X = (Width >> 1) - (originalSize.X >> 1);
                        }

                        if (rect.Height < Height)
                        {
                            originalSize.Y = rect.Height;
                            point.Y = (Height >> 1) - (originalSize.Y >> 1);
                        }

                        batcher.Draw(
                            artInfo.Texture,
                            new Rectangle(x + point.X, y + point.Y, originalSize.X, originalSize.Y),
                            new Rectangle(
                                artInfo.UV.X + rect.X,
                                artInfo.UV.Y + rect.Y,
                                rect.Width,
                                rect.Height
                            ),
                            hueVector
                        );
                    }

                    return base.Draw(batcher, x, y);
                }

                public void SetAmount(string amount)
                {
                    _label.Text = amount;
                }
            }
        }
    }
}
