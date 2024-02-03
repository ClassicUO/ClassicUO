#region license

// Copyright (c) 2024, andreakarasho
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

using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ShopGump : Gump
    {
        enum ButtonScroll
        {
            None = -1,
            LeftScrollUp,
            LeftScrollDown,
            RightScrollUp,
            RightScrollDown
        }

        // Scroll Delay (in ms)
        private const int SCROLL_DELAY = 60;
        private uint _lastMouseEventTime = Time.Ticks;

        private ButtonScroll _buttonScroll = ButtonScroll.None;
        private readonly Dictionary<uint, ShopItem> _shopItems;
        private readonly ScrollArea _shopScrollArea,
            _transactionScrollArea;
        private readonly Label _totalLabel,
            _playerGoldLabel;
        private readonly DataBox _transactionDataBox;
        private readonly Dictionary<uint, TransactionItem> _transactionItems;
        private bool _updateTotal;
        private bool _isPressing = false;
        private GumpPicTexture _leftMiddle;
        private int _initialHeight = 0;
        private int _initialHeightRight = 0;
        private int _minHeight = 0;
        private int _minHeightRight = 0;
        private GumpPicTexture _rightMiddle;
        private GumpPicTexture _leftBottom;
        private Button _expander;
        private GumpPicTexture _rightBottom;
        private HitBox _accept,
            _clear,
            _leftDown,
            _rightDown;

        private const int LEFT_TOP_HEIGHT = 64;
        private const int LEFT_BOTTOM_HEIGHT = 116;

        private const int RIGHT_OFFSET = 32;
        private const int RIGHT_BOTTOM_HEIGHT = 93;

        public ShopGump(World world, uint serial, bool isBuyGump, int x, int y) : base(world, serial, 0) //60 is the base height, original size
        {
            int height = ProfileManager.CurrentProfile.VendorGumpHeight;

            X = x;
            Y = y;
            AcceptMouseInput = false;
            AcceptKeyboardInput = true;
            CanMove = true;
            CanCloseWithRightClick = true;
            IsBuyGump = isBuyGump;

            _transactionItems = new Dictionary<uint, TransactionItem>();
            _shopItems = new Dictionary<uint, ShopItem>();
            _updateTotal = false;

            WantUpdateSize = true;

            const ushort BUY_GRAPHIC_LEFT = 0x0870;
            const ushort BUY_GRAPHIC_RIGHT = 0x0871;
            const ushort SELL_GRAPHIC_LEFT = 0x0872;
            const ushort SELL_GRAPHIC_RIGHT = 0x0873;

            ushort graphicLeft = isBuyGump ? BUY_GRAPHIC_LEFT : SELL_GRAPHIC_LEFT;
            ushort graphicRight = isBuyGump ? BUY_GRAPHIC_RIGHT : SELL_GRAPHIC_RIGHT;

            ref readonly var artInfoLeft = ref Client.Game.UO.Gumps.GetGump(graphicLeft);
            ref readonly var artInfoRight = ref Client.Game.UO.Gumps.GetGump(graphicRight);

            Rectangle offset = new Rectangle(0, 0, artInfoLeft.UV.Width, LEFT_TOP_HEIGHT);
            GumpPicTexture leftTop = new GumpPicTexture(graphicLeft, 0, 0, offset, false);
            Add(leftTop);

            offset.Y += LEFT_TOP_HEIGHT;
            offset.Height = artInfoLeft.UV.Height - (LEFT_BOTTOM_HEIGHT + LEFT_TOP_HEIGHT);
            _leftMiddle = new GumpPicTexture(graphicLeft, 0, LEFT_TOP_HEIGHT, offset, true);
            int diff = height - _leftMiddle.Height;
            _leftMiddle.Height = height;
            Add(_leftMiddle);

            offset.Y += offset.Height;
            offset.Height = LEFT_BOTTOM_HEIGHT;
            _leftBottom = new GumpPicTexture(
                graphicLeft,
                0,
                _leftMiddle.Y + _leftMiddle.Height,
                offset,
                false
            );
            Add(_leftBottom);

            int rightX = artInfoLeft.UV.Width - RIGHT_OFFSET;
            int rightY = artInfoLeft.UV.Height / 2 - RIGHT_OFFSET;
            offset = new Rectangle(0, 0, artInfoRight.UV.Width, LEFT_TOP_HEIGHT);
            GumpPicTexture rightTop = new GumpPicTexture(
                graphicRight,
                rightX,
                rightY,
                offset,
                false
            );
            Add(rightTop);

            offset.Y += LEFT_TOP_HEIGHT;
            offset.Height = artInfoRight.UV.Height - (RIGHT_BOTTOM_HEIGHT + LEFT_TOP_HEIGHT);
            _rightMiddle = new GumpPicTexture(
                graphicRight,
                rightX,
                rightY + LEFT_TOP_HEIGHT,
                offset,
                true
            );
            _rightMiddle.Height += diff;
            Add(_rightMiddle);

            offset.Y += offset.Height;
            offset.Height = RIGHT_BOTTOM_HEIGHT;
            _rightBottom = new GumpPicTexture(
                graphicRight,
                rightX,
                _rightMiddle.Y + _rightMiddle.Height,
                offset,
                false
            );
            Add(_rightBottom);

            _shopScrollArea = new ScrollArea(
                RIGHT_OFFSET,
                _leftMiddle.Y,
                artInfoLeft.UV.Width - RIGHT_OFFSET * 2 + 5,
                _leftMiddle.Height + 50,
                false,
                _leftMiddle.Height
            );

            Add(_shopScrollArea);

            _transactionScrollArea = new ScrollArea(
                RIGHT_OFFSET / 2 + rightTop.X,
                LEFT_TOP_HEIGHT + rightTop.Y,
                artInfoRight.UV.Width - RIGHT_OFFSET * 2 + RIGHT_OFFSET / 2 + 5,
                _rightMiddle.Height,
                false
            );

            Add(_transactionScrollArea);

            _transactionDataBox = new DataBox(0, 0, 1, 1);
            _transactionDataBox.WantUpdateSize = true;
            _transactionScrollArea.Add(_transactionDataBox);

            _totalLabel = new Label("0", true, 0x0386, 0, 1)
            {
                X = RIGHT_OFFSET + rightTop.X + 32 + 4,
                Y = _rightBottom.Y + _rightBottom.Height - 32 * 3 + 15,
            };

            Add(_totalLabel);

            if (isBuyGump)
            {
                _playerGoldLabel = new Label(World.Player.Gold.ToString(), true, 0x0386, 0, 1)
                {
                    X = _totalLabel.X + 120,
                    Y = _totalLabel.Y
                };

                Add(_playerGoldLabel);
            }
            else
            {
                _totalLabel.X = (rightTop.X + rightTop.Width) - RIGHT_OFFSET * 3;
            }

            _expander = new Button(2, 0x082E, 0x82F)
            {
                ButtonAction = ButtonAction.Activate,
                X = artInfoLeft.UV.Width / 2 - 10,
                Y = _leftBottom.Y + _leftBottom.Height - 5
            };

            Add(_expander);

            const float ALPHA_HIT_BUTTON = 0f;

            _accept = new HitBox(
                RIGHT_OFFSET + rightTop.X,
                (_rightBottom.Y + _rightBottom.Height) - 50,
                34,
                30,
                "Accept",
                ALPHA_HIT_BUTTON
            );
            _clear = new HitBox(_accept.X + 175, _accept.Y, 20, 20, "Clear", ALPHA_HIT_BUTTON);
            _accept.MouseUp += (sender, e) =>
            {
                OnButtonClick((int)Buttons.Accept);
            };
            _clear.MouseUp += (sender, e) =>
            {
                OnButtonClick((int)Buttons.Clear);
            };
            Add(_accept);
            Add(_clear);

            HitBox leftUp = new HitBox(
                (leftTop.X + leftTop.Width) - 50,
                (leftTop.Y + leftTop.Height) - 18,
                18,
                16,
                "Scroll Up",
                ALPHA_HIT_BUTTON
            );
            _leftDown = new HitBox(
                leftUp.X,
                _leftBottom.Y,
                18,
                16,
                "Scroll Down",
                ALPHA_HIT_BUTTON
            );

            HitBox rightUp = new HitBox(
                (rightTop.X + rightTop.Width - 50),
                (rightTop.Y + rightTop.Height) - 18,
                18,
                16,
                "Scroll Up",
                ALPHA_HIT_BUTTON
            );
            _rightDown = new HitBox(
                rightUp.X,
                _rightBottom.Y,
                18,
                16,
                "Scroll Down",
                ALPHA_HIT_BUTTON
            );

            leftUp.MouseUp += ButtonMouseUp;
            _leftDown.MouseUp += ButtonMouseUp;
            rightUp.MouseUp += ButtonMouseUp;
            _rightDown.MouseUp += ButtonMouseUp;
            leftUp.MouseDown += (sender, e) =>
            {
                _buttonScroll = ButtonScroll.LeftScrollUp;
            };
            _leftDown.MouseDown += (sender, e) =>
            {
                _buttonScroll = ButtonScroll.LeftScrollDown;
            };
            rightUp.MouseDown += (sender, e) =>
            {
                _buttonScroll = ButtonScroll.RightScrollUp;
            };
            _rightDown.MouseDown += (sender, e) =>
            {
                _buttonScroll = ButtonScroll.RightScrollDown;
            };
            Add(leftUp);
            Add(_leftDown);
            Add(rightUp);
            Add(_rightDown);

            _minHeight = artInfoLeft.UV.Height - (LEFT_BOTTOM_HEIGHT + LEFT_TOP_HEIGHT);
            _minHeightRight = artInfoRight.UV.Height - (RIGHT_BOTTOM_HEIGHT + LEFT_TOP_HEIGHT);

            _expander.MouseDown += (sender, args) =>
            {
                _isPressing = true;
                _initialHeight = _leftMiddle.Height;
                _initialHeightRight = _rightMiddle.Height;
            };

            _expander.MouseUp += (sender, args) =>
            {
                _isPressing = false;
            };

            //Label name = new Label(World.Player.Name, false, 0x0386, font: 5)
            //{
            //    X = 322,
            //    Y = 308 + _middleGumpRight.Height
            //};

            //Add(name);
        }

        public bool IsBuyGump { get; }

        //public void SetIfNameIsFromCliloc(Item it, bool fromcliloc)
        //{
        //    if (_shopItems.TryGetValue(it, out var shopItem))
        //    {
        //        shopItem.NameFromCliloc = fromcliloc;

        //        if (fromcliloc)
        //        {
        //            shopItem.SetName(ClilocLoader.Instance.Translate(it.Name, $"\t{it.Amount}\t{it.ItemData.Name}", true));
        //        }
        //    }
        //}

        private void ButtonMouseUp(object sender, MouseEventArgs e)
        {
            _buttonScroll = ButtonScroll.None;
        }

        public void AddItem(
            uint serial,
            ushort graphic,
            ushort hue,
            ushort amount,
            uint price,
            string name,
            bool fromcliloc
        )
        {
            int count = _shopScrollArea.Children.Count - 1;

            int y = count > 0 ? _shopScrollArea.Children[count].Bounds.Bottom : 0;

            ShopItem shopItem = new ShopItem(this, serial, graphic, hue, amount, price, name)
            {
                X = 5,
                Y = y + 2,
                NameFromCliloc = fromcliloc,
                InBuyGump = IsBuyGump
            };

            _shopScrollArea.Add(shopItem);

            shopItem.MouseUp += ShopItem_MouseClick;
            shopItem.MouseDoubleClick += ShopItem_MouseDoubleClick;
            _shopItems.Add(serial, shopItem);
        }

        public void SetNameTo(Item item, string name)
        {
            if (!string.IsNullOrEmpty(name) && _shopItems.TryGetValue(item, out ShopItem shopItem))
            {
                shopItem.SetName(name, false);
            }
        }

        public override void Update()
        {
            if (!World.InGame || IsDisposed)
            {
                return;
            }

            int steps = Mouse.LDragOffset.Y;

            if (_isPressing && steps != 0)
            {
                _leftMiddle.Height = _initialHeight + steps;

                if (_leftMiddle.Height < _minHeight)
                {
                    _leftMiddle.Height = _minHeight;
                }
                else if (_leftMiddle.Height > 640)
                {
                    _leftMiddle.Height = 640;
                }

                _rightMiddle.Height = _initialHeightRight + steps;

                if (_rightMiddle.Height < _minHeightRight)
                {
                    _rightMiddle.Height = _minHeightRight;
                }
                else if (_rightMiddle.Height > 640 - LEFT_TOP_HEIGHT)
                {
                    _rightMiddle.Height = 640 - LEFT_TOP_HEIGHT;
                }

                ProfileManager.CurrentProfile.VendorGumpHeight = _leftMiddle.Height;

                _leftBottom.Y = _leftMiddle.Y + _leftMiddle.Height;
                _expander.Y = _leftBottom.Y + _leftBottom.Height - 5;
                _rightBottom.Y = _rightMiddle.Y + _rightMiddle.Height;

                _shopScrollArea.Height = _leftMiddle.Height + 50;
                _shopScrollArea.ScrollMaxHeight = _leftMiddle.Height;

                _transactionDataBox.Height = _transactionScrollArea.Height = _rightMiddle.Height;
                _totalLabel.Y = _rightBottom.Y + _rightBottom.Height - RIGHT_OFFSET * 3 + 15;
                _accept.Y = _clear.Y = (_rightBottom.Y + _rightBottom.Height) - 50;
                _leftDown.Y = _leftBottom.Y;
                _rightDown.Y = _rightBottom.Y;

                if (_playerGoldLabel != null)
                {
                    _playerGoldLabel.Y = _totalLabel.Y;
                }

                _transactionDataBox.ReArrangeChildren();
                WantUpdateSize = true;
            }

            if (_shopItems.Count == 0)
            {
                Dispose();
            }

            if (_buttonScroll != ButtonScroll.None)
            {
                ProcessListScroll();
            }

            if (_updateTotal)
            {
                long sum = 0;

                foreach (TransactionItem t in _transactionItems.Values)
                {
                    sum += t.Amount * t.Price;
                }

                _totalLabel.Text = sum.ToString();
                _updateTotal = false;
            }

            if (_playerGoldLabel != null)
            {
                _playerGoldLabel.Text = World.Player.Gold.ToString();
            }

            base.Update();
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            return base.Draw(batcher, x, y);
        }

        private void ProcessListScroll()
        {
            if (Time.Ticks - _lastMouseEventTime >= SCROLL_DELAY)
            {
                switch (_buttonScroll)
                {
                    case ButtonScroll.LeftScrollUp:
                        _shopScrollArea.Scroll(true);
                        break;
                    case ButtonScroll.LeftScrollDown:
                        _shopScrollArea.Scroll(false);
                        break;
                    case ButtonScroll.RightScrollUp:
                        _transactionScrollArea.Scroll(true);
                        break;
                    case ButtonScroll.RightScrollDown:
                        _transactionScrollArea.Scroll(false);
                        break;
                }
                _lastMouseEventTime = Time.Ticks;
            }
        }

        private void ShopItem_MouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
        {
            ShopItem shopItem = (ShopItem)sender;

            if (shopItem.Amount <= 0)
            {
                return;
            }

            int total = Keyboard.Shift ? shopItem.Amount : 1;

            if (
                _transactionItems.TryGetValue(
                    shopItem.LocalSerial,
                    out TransactionItem transactionItem
                )
            )
            {
                transactionItem.Amount += total;
            }
            else
            {
                transactionItem = new TransactionItem(
                    shopItem.LocalSerial,
                    shopItem.Graphic,
                    shopItem.Hue,
                    total,
                    shopItem.Price,
                    shopItem.ShopItemName
                );

                transactionItem.OnIncreaseButtomClicked += TransactionItem_OnIncreaseButtomClicked;
                transactionItem.OnDecreaseButtomClicked += TransactionItem_OnDecreaseButtomClicked;
                _transactionDataBox.Add(transactionItem);
                _transactionItems.Add(shopItem.LocalSerial, transactionItem);
                _transactionDataBox.WantUpdateSize = true;
                _transactionDataBox.ReArrangeChildren();
            }

            shopItem.Amount -= total;
            _updateTotal = true;
        }

        private void TransactionItem_OnDecreaseButtomClicked(object sender, EventArgs e)
        {
            TransactionItem transactionItem = (TransactionItem)sender;

            int total = Keyboard.Shift ? transactionItem.Amount : 1;

            if (transactionItem.Amount > 0)
            {
                _shopItems[transactionItem.LocalSerial].Amount += total;

                transactionItem.Amount -= total;
            }

            if (transactionItem.Amount <= 0)
            {
                RemoveTransactionItem(transactionItem);
            }

            _updateTotal = true;
        }

        private void RemoveTransactionItem(TransactionItem transactionItem)
        {
            _shopItems[transactionItem.LocalSerial].Amount += transactionItem.Amount;

            transactionItem.OnIncreaseButtomClicked -= TransactionItem_OnIncreaseButtomClicked;
            transactionItem.OnDecreaseButtomClicked -= TransactionItem_OnDecreaseButtomClicked;
            _transactionItems.Remove(transactionItem.LocalSerial);
            transactionItem.Dispose();
            _transactionDataBox.WantUpdateSize = true;
            _transactionDataBox.ReArrangeChildren();
            _updateTotal = true;
        }

        private void TransactionItem_OnIncreaseButtomClicked(object sender, EventArgs e)
        {
            TransactionItem transactionItem = (TransactionItem)sender;

            if (_shopItems[transactionItem.LocalSerial].Amount > 0)
            {
                _shopItems[transactionItem.LocalSerial].Amount--;

                transactionItem.Amount++;
            }

            _updateTotal = true;
        }

        private void ShopItem_MouseClick(object sender, MouseEventArgs e)
        {
            foreach (
                ShopItem shopItem in _shopScrollArea.Children
                    .SelectMany(o => o.Children)
                    .OfType<ShopItem>()
            )
            {
                shopItem.IsSelected = shopItem == sender;
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons)buttonID)
            {
                case Buttons.Accept:
                    Tuple<uint, ushort>[] items = _transactionItems
                        .Select(t => new Tuple<uint, ushort>(t.Key, (ushort)t.Value.Amount))
                        .ToArray();

                    if (IsBuyGump)
                    {
                        NetClient.Socket.Send_BuyRequest(LocalSerial, items);
                    }
                    else
                    {
                        NetClient.Socket.Send_SellRequest(LocalSerial, items);
                    }

                    Dispose();

                    break;

                case Buttons.Clear:

                    foreach (TransactionItem t in _transactionItems.Values.ToList())
                    {
                        RemoveTransactionItem(t);
                    }

                    break;
            }
        }

        private enum Buttons
        {
            Accept,
            Clear
        }

        private class ShopItem : Control
        {
            private readonly ShopGump _gump;
            private readonly Label _amountLabel,
                _name;

            public ShopItem(
                ShopGump gump,
                uint serial,
                ushort graphic,
                ushort hue,
                int count,
                uint price,
                string name
            )
            {
                _gump = gump;
                LocalSerial = serial;
                Graphic = graphic;
                Hue = hue;
                Price = price;
                Name = name;

                ResizePicLine line = new ResizePicLine(0x39) { X = 10, Width = 190 };
                Add(line);

                int offY = 15;

                string itemName = StringHelper.CapitalizeAllWords(Name);

                if (!SerialHelper.IsValid(serial))
                {
                    return;
                }

                string subname = string.Format(ResGumps.Item0Price1, itemName, Price);

                Add(
                    _name = new Label(
                        subname,
                        true,
                        0x219,
                        110,
                        1,
                        FontStyle.None,
                        TEXT_ALIGN_TYPE.TS_LEFT,
                        true
                    )
                    {
                        X = 55,
                        Y = offY
                    }
                );

                int height = Math.Max(_name.Height, 35) + 10;

                if (SerialHelper.IsItem(serial))
                {
                    height = Math.Max(TileDataLoader.Instance.StaticData[graphic].Height, height);
                }

                Add(
                    _amountLabel = new Label(
                        count.ToString(),
                        true,
                        0x0219,
                        35,
                        1,
                        FontStyle.None,
                        TEXT_ALIGN_TYPE.TS_RIGHT
                    )
                    {
                        X = 168,
                        Y = offY + (height >> 2)
                    }
                );

                Width = 220;
                Height = Math.Max(50, height) + line.Height;

                WantUpdateSize = false;

                if (_gump.World.ClientFeatures.TooltipsEnabled)
                {
                    SetTooltip(LocalSerial);
                }

                Amount = count;
            }

            internal string ShopItemName => _name.Text;

            public int Amount
            {
                get => int.Parse(_amountLabel.Text);
                set => _amountLabel.Text = value.ToString();
            }

            public bool IsSelected
            {
                set
                {
                    foreach (Label label in Children.OfType<Label>())
                    {
                        label.Hue = (ushort)(value ? 0x0021 : 0x0219);
                    }
                }
            }

            public uint Price { get; }
            public ushort Hue { get; }
            public ushort Graphic { get; }
            public string Name { get; }

            public bool NameFromCliloc { get; set; }

            public bool InBuyGump { get; set; }

            private static byte GetAnimGroup(ushort graphic)
            {
                var groupType = Client.Game.UO.Animations.GetAnimType(graphic);
                switch (AnimationsLoader.Instance.GetGroupIndex(graphic, groupType))
                {
                    case AnimationGroups.Low:
                        return (byte)LowAnimationGroup.Stand;

                    case AnimationGroups.High:
                        return (byte)HighAnimationGroup.Stand;

                    case AnimationGroups.People:
                        return (byte)PeopleAnimationGroup.Stand;
                }

                return 0;
            }

            public void SetName(string s, bool new_name)
            {
                _name.Text = new_name
                    ? $"{s}: {Price}"
                    : string.Format(ResGumps.Item0Price1, s, Price);
                WantUpdateSize = false;
            }

            protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
            {
                return true;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                Vector3 hueVector;

                if (InBuyGump && SerialHelper.IsMobile(LocalSerial))
                {
                    ushort graphic = Graphic;

                    if (graphic >= Client.Game.UO.Animations.MaxAnimationCount)
                    {
                        graphic = 0;
                    }

                    byte group = GetAnimGroup(graphic);

                    var frames = Client.Game.UO.Animations.GetAnimationFrames(
                        graphic,
                        group,
                        1,
                        out var hue2,
                        out _,
                        true
                    );

                    if (frames.Length != 0)
                    {
                        hueVector = ShaderHueTranslator.GetHueVector(
                            hue2,
                            TileDataLoader.Instance.StaticData[Graphic].IsPartialHue,
                            1f
                        );

                        ref var spriteInfo = ref frames[0];

                        if (spriteInfo.Texture != null)
                        {
                            batcher.Draw(
                                spriteInfo.Texture,
                                new Rectangle(
                                    x - 3,
                                    y + 5 + 15,
                                    Math.Min(spriteInfo.UV.Width, 45),
                                    Math.Min(spriteInfo.UV.Height, 45)
                                ),
                                spriteInfo.UV,
                                hueVector
                            );
                        }
                    }
                }
                else
                {
                    ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(Graphic);
                    hueVector = ShaderHueTranslator.GetHueVector(
                        Hue,
                        TileDataLoader.Instance.StaticData[Graphic].IsPartialHue,
                        1f
                    );

                    var rect = Client.Game.UO.Arts.GetRealArtBounds(Graphic);

                    const int RECT_SIZE = 50;

                    Point originalSize = new Point(RECT_SIZE, Height);
                    Point point = new Point();

                    if (rect.Width < RECT_SIZE)
                    {
                        originalSize.X = rect.Width;
                        point.X = (RECT_SIZE >> 1) - (originalSize.X >> 1);
                    }

                    if (rect.Height < Height)
                    {
                        originalSize.Y = rect.Height;
                        point.Y = (Height >> 1) - (originalSize.Y >> 1);
                    }

                    batcher.Draw(
                        artInfo.Texture,
                        new Rectangle(
                            x + point.X - 5,
                            y + point.Y + 10,
                            originalSize.X,
                            originalSize.Y
                        ),
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
        }

        private class TransactionItem : Control
        {
            private readonly Label _amountLabel;

            public TransactionItem(
                uint serial,
                ushort graphic,
                ushort hue,
                int amount,
                uint price,
                string realname
            )
            {
                LocalSerial = serial;
                Graphic = graphic;
                Hue = hue;
                Price = price;

                Label l;

                Add(
                    l = new Label(
                        realname,
                        true,
                        0x021F,
                        140,
                        1,
                        FontStyle.None,
                        TEXT_ALIGN_TYPE.TS_LEFT,
                        true
                    )
                    {
                        X = 50,
                        Y = 0
                    }
                );

                Add(
                    _amountLabel = new Label(
                        amount.ToString(),
                        true,
                        0x021F,
                        35,
                        1,
                        FontStyle.None,
                        TEXT_ALIGN_TYPE.TS_RIGHT
                    )
                    {
                        X = 10,
                        Y = 0
                    }
                );

                Button buttonAdd;

                Add(
                    buttonAdd = new Button(0, 0x37, 0x37)
                    {
                        X = 190,
                        Y = 5,
                        ButtonAction = ButtonAction.Activate,
                        ContainsByBounds = true
                    }
                ); // Plus

                int status = 0;
                const int increm = 45;

                float t0 = Time.Ticks;
                bool pressedAdd = false;

                buttonAdd.MouseOver += (sender, e) =>
                {
                    if (status == 2)
                    {
                        if (pressedAdd && Time.Ticks > t0)
                        {
                            t0 = Time.Ticks + (increm - _StepChanger);
                            OnButtonClick(0);
                            _StepsDone++;

                            if (_StepChanger < increm && _StepsDone % 3 == 0)
                            {
                                _StepChanger += 2;
                            }
                        }
                    }
                };

                buttonAdd.MouseDown += (sender, e) =>
                {
                    if (e.Button != MouseButtonType.Left)
                    {
                        return;
                    }

                    pressedAdd = true;
                    _StepChanger = 0;
                    status = 2;
                    t0 = Time.Ticks + 500;
                };

                buttonAdd.MouseUp += (sender, e) =>
                {
                    pressedAdd = false;
                    status = 0;
                    _StepsDone = _StepChanger = 1;
                };

                Button buttonRemove;

                Add(
                    buttonRemove = new Button(1, 0x38, 0x38)
                    {
                        X = 210,
                        Y = 5,
                        ButtonAction = ButtonAction.Activate,
                        ContainsByBounds = true
                    }
                ); // Minus

                //float t1 = Time.Ticks;
                bool pressedRemove = false;

                buttonRemove.MouseOver += (sender, e) =>
                {
                    if (status == 2)
                    {
                        if (pressedRemove && Time.Ticks > t0)
                        {
                            t0 = Time.Ticks + (increm - _StepChanger);
                            OnButtonClick(1);
                            _StepsDone++;

                            if (_StepChanger < increm && _StepsDone % 3 == 0)
                            {
                                _StepChanger += 2;
                            }
                        }
                    }
                };

                buttonRemove.MouseDown += (sender, e) =>
                {
                    if (e.Button != MouseButtonType.Left)
                    {
                        return;
                    }

                    pressedRemove = true;
                    _StepChanger = 0;
                    status = 2;
                    t0 = Time.Ticks + 500;
                };

                buttonRemove.MouseUp += (sender, e) =>
                {
                    pressedRemove = false;
                    status = 0;
                    _StepsDone = _StepChanger = 1;
                };

                Width = 245;
                Height = l.Height;
                WantUpdateSize = false;
                Amount = amount;
            }

            public ushort Graphic { get; }
            public ushort Hue { get; }

            public uint Price { get; }

            public int Amount
            {
                get => int.Parse(_amountLabel.Text);
                set => _amountLabel.Text = value.ToString();
            }

            public event EventHandler OnIncreaseButtomClicked;

            public event EventHandler OnDecreaseButtomClicked;

            public override void OnButtonClick(int buttonID)
            {
                switch (buttonID)
                {
                    case 0:
                        OnIncreaseButtomClicked?.Invoke(this, EventArgs.Empty);

                        break;

                    case 1:
                        OnDecreaseButtomClicked?.Invoke(this, EventArgs.Empty);

                        break;
                }
            }
        }

        private class ResizePicLine : Control
        {
            private readonly ushort _graphic;

            public ResizePicLine(ushort graphic)
            {
                _graphic = graphic;
                CanMove = true;
                CanCloseWithRightClick = true;

                ref readonly var gumpInfo0 = ref Client.Game.UO.Gumps.GetGump(_graphic);
                ref readonly var gumpInfo1 = ref Client.Game.UO.Gumps.GetGump((uint)(_graphic + 1));
                ref readonly var gumpInfo2 = ref Client.Game.UO.Gumps.GetGump((uint)(_graphic + 2));

                Height = Math.Max(
                    gumpInfo0.UV.Height,
                    Math.Max(gumpInfo1.UV.Height, gumpInfo2.UV.Height)
                );
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                ref readonly var gumpInfo0 = ref Client.Game.UO.Gumps.GetGump(_graphic);
                ref readonly var gumpInfo1 = ref Client.Game.UO.Gumps.GetGump((uint)(_graphic + 1));
                ref readonly var gumpInfo2 = ref Client.Game.UO.Gumps.GetGump((uint)(_graphic + 2));

                var hueVector = ShaderHueTranslator.GetHueVector(0, false, Alpha, true);

                int middleWidth = Width - gumpInfo0.UV.Width - gumpInfo2.UV.Width;

                batcher.Draw(gumpInfo0.Texture, new Vector2(x, y), gumpInfo0.UV, hueVector);

                batcher.DrawTiled(
                    gumpInfo1.Texture,
                    new Rectangle(x + gumpInfo0.UV.Width, y, middleWidth, gumpInfo1.UV.Height),
                    gumpInfo1.UV,
                    hueVector
                );

                batcher.Draw(
                    gumpInfo2.Texture,
                    new Vector2(x + Width - gumpInfo2.UV.Width, y),
                    gumpInfo2.UV,
                    hueVector
                );

                return base.Draw(batcher, x, y);
            }
        }

        private class GumpPicTexture : Control
        {
            private readonly bool _tiled;
            private readonly ushort _graphic;
            private readonly Rectangle _rect;

            public GumpPicTexture(ushort graphic, int x, int y, Rectangle bounds, bool tiled)
            {
                CanMove = true;
                AcceptMouseInput = true;

                _graphic = graphic;
                _rect = bounds;
                X = x;
                Y = y;
                Width = bounds.Width;
                Height = bounds.Height;
                WantUpdateSize = false;
                _tiled = tiled;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                var gumpInfo = Client.Game.UO.Gumps.GetGump(_graphic);
                var hueVector = ShaderHueTranslator.GetHueVector(0);

                if (_tiled)
                {
                    batcher.DrawTiled(
                        gumpInfo.Texture,
                        new Rectangle(x, y, Width, Height),
                        new Rectangle(
                            gumpInfo.UV.X + _rect.X,
                            gumpInfo.UV.Y + _rect.Y,
                            _rect.Width,
                            _rect.Height
                        ),
                        hueVector
                    );
                }
                else
                {
                    batcher.Draw(
                        gumpInfo.Texture,
                        new Rectangle(x, y, Width, Height),
                        new Rectangle(
                            gumpInfo.UV.X + _rect.X,
                            gumpInfo.UV.Y + _rect.Y,
                            _rect.Width,
                            _rect.Height
                        ),
                        hueVector
                    );
                }

                return base.Draw(batcher, x, y);
            }
        }
    }
}
