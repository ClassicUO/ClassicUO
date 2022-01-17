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

using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
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

        private ButtonScroll _buttonScroll = ButtonScroll.None;
        private readonly Dictionary<uint, ShopItem> _shopItems;
        private readonly ScrollArea _shopScrollArea, _transactionScrollArea;
        private readonly Label _totalLabel, _playerGoldLabel;
        private readonly DataBox _transactionDataBox;
        private readonly Dictionary<uint, TransactionItem> _transactionItems;
        private bool _updateTotal;

        public ShopGump(uint serial, bool isBuyGump, int x, int y) : base(serial, 0) //60 is the base height, original size
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

            _ = GumpsLoader.Instance.GetGumpTexture(graphicLeft, out var boundsLeft);
            _ = GumpsLoader.Instance.GetGumpTexture(graphicRight, out var boundsRight);



            const int LEFT_TOP_HEIGHT = 64;
            const int LEFT_BOTTOM_HEIGHT = 116;

            Rectangle offset = new Rectangle(0, 0, boundsLeft.Width, LEFT_TOP_HEIGHT);
            GumpPicTexture leftTop = new GumpPicTexture(graphicLeft, 0, 0, offset, false);
            Add(leftTop);


            

            offset.Y += LEFT_TOP_HEIGHT;
            offset.Height = boundsLeft.Height - (LEFT_BOTTOM_HEIGHT + LEFT_TOP_HEIGHT);
            GumpPicTexture leftmiddle = new GumpPicTexture(graphicLeft, 0, LEFT_TOP_HEIGHT, offset, true);
            int diff = height - leftmiddle.Height;
            leftmiddle.Height = height;
            Add(leftmiddle);


            offset.Y += offset.Height;
            offset.Height = LEFT_BOTTOM_HEIGHT;
            GumpPicTexture leftBottom = new GumpPicTexture(graphicLeft, 0, leftmiddle.Y + leftmiddle.Height, offset, false);
            Add(leftBottom);



            const int RIGHT_OFFSET = 32;
            const int RIGHT_BOTTOM_HEIGHT = 93;

            int rightX = boundsLeft.Width - RIGHT_OFFSET;
            int rightY = boundsLeft.Height / 2 - RIGHT_OFFSET;
            offset = new Rectangle(0, 0, boundsRight.Width, LEFT_TOP_HEIGHT);
            GumpPicTexture rightTop = new GumpPicTexture(graphicRight, rightX, rightY, offset, false);
            Add(rightTop);


            offset.Y += LEFT_TOP_HEIGHT;
            offset.Height = boundsRight.Height - (RIGHT_BOTTOM_HEIGHT + LEFT_TOP_HEIGHT);
            GumpPicTexture rightMiddle = new GumpPicTexture(graphicRight, rightX, rightY + LEFT_TOP_HEIGHT, offset, true);
            rightMiddle.Height += diff;
            Add(rightMiddle);


            offset.Y += offset.Height;
            offset.Height = RIGHT_BOTTOM_HEIGHT;
            GumpPicTexture rightBottom = new GumpPicTexture(graphicRight, rightX, rightMiddle.Y + rightMiddle.Height, offset, false);
            Add(rightBottom);



            _shopScrollArea = new ScrollArea
            (
                RIGHT_OFFSET,
                leftmiddle.Y,
                boundsLeft.Width - RIGHT_OFFSET * 2 + 5,
                leftmiddle.Height + 50,
                false,
                leftmiddle.Height
            );

            Add(_shopScrollArea);


            _transactionScrollArea = new ScrollArea
            (
                RIGHT_OFFSET / 2 + rightTop.X,
                LEFT_TOP_HEIGHT + rightTop.Y,
                boundsRight.Width - RIGHT_OFFSET * 2 + RIGHT_OFFSET / 2 + 5, 
                rightMiddle.Height,
                false
            );

            Add(_transactionScrollArea);

            _transactionDataBox = new DataBox(0, 0, 1, 1);
            _transactionDataBox.WantUpdateSize = true;
            _transactionScrollArea.Add(_transactionDataBox);


            _totalLabel = new Label("0", true, 0x0386, 0, 1)
            {
                X = RIGHT_OFFSET + rightTop.X + 32 + 4,
                Y = rightBottom.Y + rightBottom.Height - 32 * 3 + 15,
            };

            Add(_totalLabel);

            if (isBuyGump)
            {
                _playerGoldLabel = new Label
                (
                    World.Player.Gold.ToString(),
                    true,
                    0x0386,
                    0,
                    1
                )
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




            Button expander = new Button(2, 0x082E, 0x82F)
            {
                ButtonAction = ButtonAction.Activate,
                X = boundsLeft.Width / 2 - 10,
                Y = leftBottom.Y + leftBottom.Height - 5
            };

            Add(expander);


            const float ALPHA_HIT_BUTTON = 0f;

            HitBox accept = new HitBox(RIGHT_OFFSET + rightTop.X, (rightBottom.Y + rightBottom.Height) - 50, 34, 30, "Accept", ALPHA_HIT_BUTTON);
            HitBox clear = new HitBox(accept.X + 175, accept.Y, 20, 20, "Clear", ALPHA_HIT_BUTTON);
            accept.MouseUp += (sender, e) => { OnButtonClick((int)Buttons.Accept); };
            clear.MouseUp += (sender, e) => { OnButtonClick((int)Buttons.Clear); };
            Add(accept);
            Add(clear);



            HitBox leftUp = new HitBox((leftTop.X + leftTop.Width) - 50, (leftTop.Y + leftTop.Height) - 18, 18, 16, "Scroll Up", ALPHA_HIT_BUTTON);
            HitBox leftDown = new HitBox(leftUp.X, leftBottom.Y, 18, 16, "Scroll Down", ALPHA_HIT_BUTTON);

            HitBox rightUp = new HitBox((rightTop.X + rightTop.Width - 50), (rightTop.Y + rightTop.Height) - 18, 18, 16, "Scroll Up", ALPHA_HIT_BUTTON);
            HitBox rightDown = new HitBox(rightUp.X, rightBottom.Y, 18, 16, "Scroll Down", ALPHA_HIT_BUTTON);

            leftUp.MouseUp += ButtonMouseUp;
            leftDown.MouseUp += ButtonMouseUp;
            rightUp.MouseUp += ButtonMouseUp;
            rightDown.MouseUp += ButtonMouseUp;
            leftUp.MouseDown += (sender, e) => { _buttonScroll = ButtonScroll.LeftScrollUp; };
            leftDown.MouseDown += (sender, e) => { _buttonScroll = ButtonScroll.LeftScrollDown; };
            rightUp.MouseDown += (sender, e) => { _buttonScroll = ButtonScroll.RightScrollUp; };
            rightDown.MouseDown += (sender, e) => { _buttonScroll = ButtonScroll.RightScrollUp; };
            Add(leftUp);
            Add(leftDown);
            Add(rightUp);
            Add(rightDown);



            bool is_pressing = false;
            int initial_height = 0, initialHeightRight = 0;
            int minHeight = boundsLeft.Height - (LEFT_BOTTOM_HEIGHT + LEFT_TOP_HEIGHT);
            int minHeightRight = boundsRight.Height - (RIGHT_BOTTOM_HEIGHT + LEFT_TOP_HEIGHT);

            expander.MouseDown += (sender, args) =>
            {
                is_pressing = true;
                initial_height = leftmiddle.Height;
                initialHeightRight = rightMiddle.Height;
            };

            expander.MouseUp += (sender, args) => { is_pressing = false; };

            expander.MouseOver += (sender, args) =>
            {
                int steps = Mouse.LDragOffset.Y;

                if (is_pressing && steps != 0)
                {
                    leftmiddle.Height = initial_height + steps;

                    if (leftmiddle.Height < minHeight)
                    {
                        leftmiddle.Height = minHeight;
                    }
                    else if (leftmiddle.Height > 640)
                    {
                        leftmiddle.Height = 640;
                    }

                    rightMiddle.Height = initialHeightRight + steps;

                    if (rightMiddle.Height < minHeightRight)
                    {
                        rightMiddle.Height = minHeightRight;
                    }
                    else if (rightMiddle.Height > 640 - LEFT_TOP_HEIGHT)
                    {
                        rightMiddle.Height = 640 - LEFT_TOP_HEIGHT;
                    }

                    ProfileManager.CurrentProfile.VendorGumpHeight = leftmiddle.Height;

                    leftBottom.Y = leftmiddle.Y + leftmiddle.Height;
                    expander.Y = leftBottom.Y + leftBottom.Height - 5; 
                    rightBottom.Y = rightMiddle.Y + rightMiddle.Height;

                    _shopScrollArea.Height = leftmiddle.Height + 50;
                    _shopScrollArea.ScrollMaxHeight = leftmiddle.Height;

                    _transactionDataBox.Height = _transactionScrollArea.Height = rightMiddle.Height;
                    _totalLabel.Y = rightBottom.Y + rightBottom.Height - RIGHT_OFFSET * 3 + 15;
                    accept.Y = clear.Y = (rightBottom.Y + rightBottom.Height) - 50;
                    leftDown.Y = leftBottom.Y;
                    rightDown.Y = rightBottom.Y;

                    if (_playerGoldLabel != null)
                    {
                        _playerGoldLabel.Y = _totalLabel.Y;
                    }

                    _transactionDataBox.ReArrangeChildren();
                    WantUpdateSize = true;
                }
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

        private void ButtonMouseUp(object sender, MouseEventArgs e) { _buttonScroll = ButtonScroll.None; }


        public void AddItem
        (
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

            ShopItem shopItem = new ShopItem
            (
                serial,
                graphic,
                hue,
                amount,
                price,
                name
            )
            {
                X = 5,
                Y = y + 2,
                NameFromCliloc = fromcliloc
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


        public override void Update(double totalTime, double frameTime)
        {
            if (!World.InGame || IsDisposed)
            {
                return;
            }

            if (_shopItems.Count == 0)
            {
                Dispose();
            }

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

            if (_updateTotal)
            {
                int sum = 0;

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

            base.Update(totalTime, frameTime);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            return base.Draw(batcher, x, y);
        }

        private void ShopItem_MouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
        {
            ShopItem shopItem = (ShopItem) sender;

            if (shopItem.Amount <= 0)
            {
                return;
            }


            int total = Keyboard.Shift ? shopItem.Amount : 1;

            if (_transactionItems.TryGetValue(shopItem.LocalSerial, out TransactionItem transactionItem))
            {
                transactionItem.Amount += total;
            }
            else
            {
                transactionItem = new TransactionItem
                (
                    shopItem.LocalSerial,
                    shopItem.Graphic,
                    shopItem.Hue,
                    total,
                    (ushort) shopItem.Price,
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
            TransactionItem transactionItem = (TransactionItem) sender;

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
            TransactionItem transactionItem = (TransactionItem) sender;

            if (_shopItems[transactionItem.LocalSerial].Amount > 0)
            {
                _shopItems[transactionItem.LocalSerial].Amount--;

                transactionItem.Amount++;
            }

            _updateTotal = true;
        }

        private void ShopItem_MouseClick(object sender, MouseEventArgs e)
        {
            foreach (ShopItem shopItem in _shopScrollArea.Children.SelectMany(o => o.Children).OfType<ShopItem>())
            {
                shopItem.IsSelected = shopItem == sender;
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons) buttonID)
            {
                case Buttons.Accept:
                    Tuple<uint, ushort>[] items = _transactionItems.Select(t => new Tuple<uint, ushort>(t.Key, (ushort) t.Value.Amount)).ToArray();

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
            private readonly Label _amountLabel, _name;

            public ShopItem
            (
                uint serial,
                ushort graphic,
                ushort hue,
                int count,
                uint price,
                string name
            )
            {
                LocalSerial = serial;
                Graphic = graphic;
                Hue = hue;
                Price = price;
                Name = name;

                ResizePicLine line = new ResizePicLine(0x39)
                {
                    X = 10,
                    Width = 190
                };
                Add(line);

                int offY = 15;

                string itemName = StringHelper.CapitalizeAllWords(Name);

                if (!SerialHelper.IsValid(serial))
                {
                    return;
                }

               
                string subname = string.Format(ResGumps.Item0Price1, itemName, Price);

                Add
                (
                    _name = new Label
                    (
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

                Add
                (
                    _amountLabel = new Label
                    (
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

                if (World.ClientFeatures.TooltipsEnabled)
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
                        label.Hue = (ushort) (value ? 0x0021 : 0x0219);
                    }
                }
            }

            public uint Price { get; }
            public ushort Hue { get; }
            public ushort Graphic { get; }
            public string Name { get; }

            public bool NameFromCliloc { get; set; }

            private static byte GetAnimGroup(ushort graphic)
            {
                switch (AnimationsLoader.Instance.GetGroupIndex(graphic))
                {
                    case ANIMATION_GROUPS.AG_LOW: return (byte) LOW_ANIMATION_GROUP.LAG_STAND;

                    case ANIMATION_GROUPS.AG_HIGHT: return (byte) HIGHT_ANIMATION_GROUP.HAG_STAND;

                    case ANIMATION_GROUPS.AG_PEOPLE: return (byte) PEOPLE_ANIMATION_GROUP.PAG_STAND;
                }

                return 0;
            }

            private static AnimationDirection GetMobileAnimationDirection(ushort graphic, ref ushort hue, byte dirIndex)
            {
                if (graphic >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                {
                    return null;
                }

                byte group = GetAnimGroup(graphic);
                IndexAnimation index = AnimationsLoader.Instance.DataIndex[graphic];

                AnimationDirection direction = index.Groups[group].Direction[dirIndex];

                for (int i = 0; i < 2 && direction.FrameCount == 0; i++)
                {
                    if (!AnimationsLoader.Instance.LoadAnimationFrames(graphic, group, dirIndex, ref direction))
                    {
                        //direction = AnimationsLoader.Instance.GetCorpseAnimationGroup(ref graphic, ref group, ref hue2).Direction[dirIndex];
                        //graphic = item.ItemData.AnimID;
                        //group = GetAnimGroup(graphic);
                        //index = AnimationsLoader.Instance.DataIndex[graphic];
                        //direction = AnimationsLoader.Instance.GetBodyAnimationGroup(ref graphic, ref group, ref hue2, true).Direction[dirIndex];
                        ////direction = index.Groups[group].Direction[1];
                        //AnimationsLoader.Instance.AnimID = graphic;
                        //AnimationsLoader.Instance.AnimGroup = group;
                        //AnimationsLoader.Instance.Direction = dirIndex;
                    }
                }

                return direction;
            }

            public void SetName(string s, bool new_name)
            {
                _name.Text = new_name ? $"{s}: {Price}" : string.Format(ResGumps.Item0Price1, s, Price);
                WantUpdateSize = false;
            }

            protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
            {
                return true;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                Vector3 hueVector;

                if (SerialHelper.IsMobile(LocalSerial))
                {
                    ushort hue2 = Hue;
                    AnimationDirection direction = GetMobileAnimationDirection(Graphic, ref hue2, 1);

                    if (direction != null && direction.SpriteInfos != null && direction.FrameCount != 0)
                    {
                        hueVector = ShaderHueTranslator.GetHueVector(hue2, TileDataLoader.Instance.StaticData[Graphic].IsPartialHue, 1f);

                        batcher.Draw
                        (
                            direction.SpriteInfos[0].Texture,
                            new Rectangle
                            (
                                x - 3, 
                                y + 5 + 15,
                                Math.Min(direction.SpriteInfos[0].UV.Width, 45),
                                Math.Min(direction.SpriteInfos[0].UV.Height, 45)
                            ),
                            direction.SpriteInfos[0].UV,
                            hueVector
                        );
                    }
                }
                else if (SerialHelper.IsItem(LocalSerial))
                {
                    var texture = ArtLoader.Instance.GetStaticTexture(Graphic, out var bounds);

                    hueVector = ShaderHueTranslator.GetHueVector(Hue, TileDataLoader.Instance.StaticData[Graphic].IsPartialHue, 1f);

                    var rect = ArtLoader.Instance.GetRealArtBounds(Graphic);

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

                    batcher.Draw
                    (
                        texture,
                        new Rectangle
                        (
                            x + point.X - 5,
                            y + point.Y + 10,
                            originalSize.X,
                            originalSize.Y
                        ),
                        new Rectangle
                        (
                            bounds.X + rect.X,
                            bounds.Y + rect.Y,
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

            public TransactionItem
            (
                uint serial,
                ushort graphic,
                ushort hue,
                int amount,
                ushort price,
                string realname
            )
            {
                LocalSerial = serial;
                Graphic = graphic;
                Hue = hue;
                Price = price;

                Label l;

                Add
                (
                    l = new Label
                    (
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

                Add
                (
                    _amountLabel = new Label
                    (
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

                Add
                (
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

                Add
                (
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

            public ushort Price { get; }

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

                _ = GumpsLoader.Instance.GetGumpTexture((ushort)(_graphic + 0), out var bounds0);
                _ = GumpsLoader.Instance.GetGumpTexture((ushort)(_graphic + 1), out var bounds1);
                _ = GumpsLoader.Instance.GetGumpTexture((ushort)(_graphic + 2), out var bounds2);

                Height = Math.Max(bounds0.Height, Math.Max(bounds1.Height, bounds2.Height));
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                var texture0 = GumpsLoader.Instance.GetGumpTexture((ushort)(_graphic + 0), out var bounds0);
                var texture1 = GumpsLoader.Instance.GetGumpTexture((ushort)(_graphic + 1), out var bounds1);
                var texture2 = GumpsLoader.Instance.GetGumpTexture((ushort)(_graphic + 2), out var bounds2);

                Vector3 hueVector = ShaderHueTranslator.GetHueVector
                                    (
                                        0,
                                        false,
                                        Alpha,
                                        true
                                    );

                int middleWidth = Width - bounds0.Width - bounds2.Width;

                batcher.Draw
                (
                    texture0,
                    new Vector2(x, y),
                    bounds0,
                    hueVector
                );

                batcher.DrawTiled
                (
                    texture1,
                    new Rectangle
                    (
                        x + bounds0.Width,
                        y,
                        middleWidth,
                        bounds1.Height
                    ),
                    bounds1,
                    hueVector
                );

                batcher.Draw
                (
                    texture2,
                    new Vector2(x + Width - bounds2.Width, y),
                    bounds2,
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
                var texture = GumpsLoader.Instance.GetGumpTexture(_graphic, out var bounds);

                Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

                if (_tiled)
                {
                    batcher.DrawTiled
                    (
                        texture,
                        new Rectangle
                        (
                            x,
                            y,
                            Width,
                            Height
                        ),
                        new Rectangle
                        (
                            bounds.X + _rect.X,
                            bounds.Y + _rect.Y,
                            _rect.Width,
                            _rect.Height
                        ),
                        hueVector
                    );
                }
                else
                {
                    batcher.Draw
                    (
                        texture,
                        new Rectangle
                        (
                            x,
                            y,
                            Width,
                            Height
                        ),
                        new Rectangle
                        (
                            bounds.X + _rect.X,
                            bounds.Y + _rect.Y,
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