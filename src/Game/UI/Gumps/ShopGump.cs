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
using System.Linq;

using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ShopGump : Gump
    {
        private static UOTexture[] _shopGumpParts;
        private readonly GumpPicTiled _middleGumpLeft, _middleGumpRight;
        private readonly Dictionary<uint, ShopItem> _shopItems;
        private readonly ScrollArea _shopScrollArea, _transactionScrollArea;
        private readonly Label _totalLabel, _playerGoldLabel;
        private readonly Dictionary<uint, TransactionItem> _transactionItems;

        private bool _isUpDOWN, _isDownDOWN;
        private bool _isUpDOWN_T, _isDownDOWN_T;

        private bool _shiftPressed;
        private bool _updateTotal;

        public ShopGump(uint serial, bool isBuyGump, int x, int y) : base(serial, 0) //60 is the base height, original size
        {
            int height = ProfileManager.Current.VendorGumpHeight;
            if (_shopGumpParts == null) GenerateVirtualTextures();
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

            int add = isBuyGump ? 0 : 6;

            GumpPic pic = new GumpPic(0, 0, _shopGumpParts[0 + add], 0);
            Add(pic);
            pic = new GumpPic(250, 144, _shopGumpParts[3 + add], 0);
            Add(pic);

            Add(_middleGumpLeft = new GumpPicTiled(0, 64, pic.Width, height, _shopGumpParts[1 + add]));
            Add(new GumpPic(0, _middleGumpLeft.Height + _middleGumpLeft.Y, _shopGumpParts[2 + add], 0));
            
            _shopScrollArea = new ScrollArea(30, 60, 225, _middleGumpLeft.Height + _middleGumpLeft.Y + 50, false, _middleGumpLeft.Height + _middleGumpLeft.Y);
            Add(_shopScrollArea);

           
            Add(_middleGumpRight = new GumpPicTiled(250, 144 + 64, pic.Width, _middleGumpLeft.Height >> 1, _shopGumpParts[4 + add]));
            Add(new GumpPic(250, _middleGumpRight.Height + _middleGumpRight.Y, _shopGumpParts[5 + add], 0));


            HitBox boxAccept = new HitBox(280, 306 + _middleGumpRight.Height, 34, 30)
            {
                Alpha = 1
            };

            HitBox boxClear = new HitBox(452, 310 + _middleGumpRight.Height, 24, 24)
            {
                Alpha = 1
            };

            boxAccept.MouseUp += (sender, e) => { OnButtonClick((int) Buttons.Accept); };
            boxClear.MouseUp += (sender, e) => { OnButtonClick((int) Buttons.Clear); };
            Add(boxAccept);
            Add(boxClear);


            if (isBuyGump)
            {
                Add(_totalLabel = new Label("0", true, 0x0386, 0, 1)
                {
                    X = 318,
                    Y = 281 + _middleGumpRight.Height
                });

                Add(_playerGoldLabel = new Label(World.Player.Gold.ToString(), true, 0x0386, 0, 1)
                {
                    X = 436,
                    Y = 281 + _middleGumpRight.Height
                });
            }
            else
            {
                Add(_totalLabel = new Label("0", true, 0x0386, 0, 1)
                {
                    X = 436,
                    Y = 281 + _middleGumpRight.Height
                });
            }

            Add(new Label(World.Player.Name, false, 0x0386, font: 5)
            {
                X = 322,
                Y = 308 + _middleGumpRight.Height
            });

            Add(_transactionScrollArea = new ScrollArea(260, 215, 245, 53 + _middleGumpRight.Height, false));


            HitBox upButton = new HitBox(233, 50, 18, 16)
            {
                Alpha = 1
            };
            upButton.MouseDown += (sender, e) => { _isUpDOWN = true; };
            upButton.MouseUp += (sender, e) => { _isUpDOWN = false; };

            Add(upButton);

            HitBox downButton = new HitBox(233, 130 + _middleGumpLeft.Height, 18, 16)
            {
                Alpha = 1
            };
            downButton.MouseDown += (sender, e) => { _isDownDOWN = true; };
            downButton.MouseUp += (sender, e) => { _isDownDOWN = false; };
            Add(downButton);


            HitBox upButtonT = new HitBox(483, 195, 18, 16)
            {
                Alpha = 1
            };
            upButtonT.MouseDown += (sender, e) => { _isUpDOWN_T = true; };
            upButtonT.MouseUp += (sender, e) => { _isUpDOWN_T = false; };

            Add(upButtonT);

            HitBox downButtonT = new HitBox(483, 270 + _middleGumpRight.Height, 18, 16)
            {
                Alpha = 1
            };
            downButtonT.MouseDown += (sender, e) => { _isDownDOWN_T = true; };
            downButtonT.MouseUp += (sender, e) => { _isDownDOWN_T = false; };
            Add(downButtonT);
        }

        public bool IsBuyGump { get; }

        private void GenerateVirtualTextures()
        {
            _shopGumpParts = new UOTexture[12];
            UOTexture t = GumpsLoader.Instance.GetTexture(0x0870);
            UOTexture[][] splits = new UOTexture[4][];

            splits[0] = Utility.GraphicHelper.SplitTexture16(t,
                                                             new int[3, 4]
                                                             {
                                                                 {0, 0, t.Width, 64},
                                                                 {0, 64, t.Width, 124},
                                                                 {0, 124, t.Width, t.Height - 124}
                                                             });
            t = GumpsLoader.Instance.GetTexture(0x0871);

            splits[1] = Utility.GraphicHelper.SplitTexture16(t,
                                                             new int[3, 4]
                                                             {
                                                                 {0, 0, t.Width, 64},
                                                                 {0, 64, t.Width, 94},
                                                                 {0, 94, t.Width, t.Height - 94}
                                                             });
            t = GumpsLoader.Instance.GetTexture(0x0872);

            splits[2] = Utility.GraphicHelper.SplitTexture16(t,
                                                             new int[3, 4]
                                                             {
                                                                 {0, 0, t.Width, 64},
                                                                 {0, 64, t.Width, 124},
                                                                 {0, 124, t.Width, t.Height - 124}
                                                             });
            t = GumpsLoader.Instance.GetTexture(0x0873);

            splits[3] = Utility.GraphicHelper.SplitTexture16(t,
                                                             new int[3, 4]
                                                             {
                                                                 {0, 0, t.Width, 64},
                                                                 {0, 64, t.Width, 94},
                                                                 {0, 94, t.Width, t.Height - 94}
                                                             });

            for (int i = 0, idx = 0; i < splits.Length; i++)
            {
                for (int ii = 0; ii < splits[i].Length; ii++) _shopGumpParts[idx++] = splits[i][ii];
            }
        }



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

        public void AddItem(uint serial, ushort graphic, ushort hue, ushort amount, uint price, string name, bool fromcliloc)
        {
            ShopItem shopItem;

            _shopScrollArea.Add(shopItem = new ShopItem(serial, graphic, hue, amount, price, name)
            {
                X = 5,
                Y = 5,
                NameFromCliloc = fromcliloc
            });

            _shopScrollArea.Add(new ResizePicLine(0x39)
            {
                X = 10,
                Width = 190
            });
            shopItem.MouseUp += ShopItem_MouseClick;
            shopItem.MouseDoubleClick += ShopItem_MouseDoubleClick;
            _shopItems.Add(serial, shopItem);
           
            //var it = World.Items.Get(serial);
            //Console.WriteLine("ITEM: name: {0}  - tiledata name: {1}  - price: {2}   - X,Y= {3},{4}", name, TileDataLoader.Instance.StaticData[graphic].Name, price, it.X, it.Y);
        }

        public void SetNameTo(Item item, string name)
        {
            if (!string.IsNullOrEmpty(name) && _shopItems.TryGetValue(item, out ShopItem shopItem)) 
                shopItem.SetName(name, false);
        }


        public override void Update(double totalMS, double frameMS)
        {
            if (!World.InGame || IsDisposed)
                return;

            if (_shopItems.Count == 0)
            {
                Dispose();
            }

            _shiftPressed = Keyboard.Shift;

            if (_isUpDOWN || _isDownDOWN || _isDownDOWN_T || _isUpDOWN_T)
            {
                if (_isDownDOWN)
                    _shopScrollArea.Scroll(false);
                else if (_isUpDOWN)
                    _shopScrollArea.Scroll(true);
                else if (_isDownDOWN_T)
                    _transactionScrollArea.Scroll(false);
                else
                    _transactionScrollArea.Scroll(true);
            }

            if (_updateTotal)
            {
                int sum = 0;

                foreach (var t in _transactionItems.Values)
                {
                    sum += t.Amount * t.Price;
                }
                _totalLabel.Text = sum.ToString();
                _updateTotal = false;
            }

            if (_playerGoldLabel != null)
                _playerGoldLabel.Text = World.Player.Gold.ToString();

            base.Update(totalMS, frameMS);
        }

        private void ShopItem_MouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
        {
            var shopItem = (ShopItem) sender;

            if (shopItem.Amount <= 0)
                return;


            int total = _shiftPressed ? shopItem.Amount : 1;

            if (_transactionItems.TryGetValue(shopItem.LocalSerial, out TransactionItem transactionItem))
                transactionItem.Amount += total;
            else
            {
                transactionItem = new TransactionItem(shopItem.LocalSerial, shopItem.Graphic, shopItem.Hue, total, (ushort) shopItem.Price, shopItem.ShopItemName);
                transactionItem.OnIncreaseButtomClicked += TransactionItem_OnIncreaseButtomClicked;
                transactionItem.OnDecreaseButtomClicked += TransactionItem_OnDecreaseButtomClicked;
                _transactionScrollArea.Add(transactionItem);
                _transactionItems.Add(shopItem.LocalSerial, transactionItem);
            }

            shopItem.Amount -= total;
            _updateTotal = true;
        }

        private void TransactionItem_OnDecreaseButtomClicked(object sender, EventArgs e)
        {
            var transactionItem = (TransactionItem) sender;

            int total = _shiftPressed ? transactionItem.Amount : 1;

            if (transactionItem.Amount > 0)
            {
                _shopItems[transactionItem.LocalSerial].Amount += total;
                transactionItem.Amount -= total;
            }

            if (transactionItem.Amount <= 0)
                RemoveTransactionItem(transactionItem);
            _updateTotal = true;
        }

        private void RemoveTransactionItem(TransactionItem transactionItem)
        {
            _shopItems[transactionItem.LocalSerial].Amount += transactionItem.Amount;
            transactionItem.OnIncreaseButtomClicked -= TransactionItem_OnIncreaseButtomClicked;
            transactionItem.OnDecreaseButtomClicked -= TransactionItem_OnDecreaseButtomClicked;
            _transactionItems.Remove(transactionItem.LocalSerial);
            _transactionScrollArea.Remove(transactionItem);
            _updateTotal = true;
        }

        private void TransactionItem_OnIncreaseButtomClicked(object sender, EventArgs e)
        {
            var transactionItem = (TransactionItem) sender;

            if (_shopItems[transactionItem.LocalSerial].Amount > 0)
            {
                _shopItems[transactionItem.LocalSerial].Amount--;
                transactionItem.Amount++;
            }

            _updateTotal = true;
        }

        private void ShopItem_MouseClick(object sender, MouseEventArgs e)
        {
            foreach (var shopItem in _shopScrollArea.Children.SelectMany(o => o.Children).OfType<ShopItem>()) 
                shopItem.IsSelected = shopItem == sender;
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons) buttonID)
            {
                case Buttons.Accept:
                    var items = _transactionItems.Select(t => new Tuple<uint, ushort>(t.Key, (ushort) t.Value.Amount)).ToArray();

                    if (IsBuyGump)
                        NetClient.Socket.Send(new PBuyRequest(LocalSerial, items));
                    else
                        NetClient.Socket.Send(new PSellRequest(LocalSerial, items));

                    Dispose();

                    break;

                case Buttons.Clear:

                    foreach (var t in _transactionItems.Values.ToList())
                        RemoveTransactionItem(t);

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

            private static byte GetAnimGroup(ushort graphic)
            {
                switch (AnimationsLoader.Instance.GetGroupIndex(graphic))
                {
                    case ANIMATION_GROUPS.AG_LOW:
                        return (byte) LOW_ANIMATION_GROUP.LAG_STAND;

                    case ANIMATION_GROUPS.AG_HIGHT:
                        return (byte) HIGHT_ANIMATION_GROUP.HAG_STAND;

                    case ANIMATION_GROUPS.AG_PEOPLE:
                        return (byte) PEOPLE_ANIMATION_GROUP.PAG_STAND;
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
                var index = AnimationsLoader.Instance.DataIndex[graphic];

                AnimationDirection direction = index.Groups[group].Direction[dirIndex];
                AnimationsLoader.Instance.AnimID = graphic;
                AnimationsLoader.Instance.AnimGroup = group;
                AnimationsLoader.Instance.Direction = dirIndex;

                for (int i = 0; i < 2 && direction.FrameCount == 0; i++)
                {
                    if (!AnimationsLoader.Instance.LoadDirectionGroup(ref direction))
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

            public ShopItem(uint serial, ushort graphic, ushort hue, int count, uint price, string name)
            {
                LocalSerial = serial;
                Graphic = graphic;
                Hue = hue;
                Price = price;
                Name = name;

                string itemName = StringHelper.CapitalizeAllWords(Name);

                TextureControl control;

                if (SerialHelper.IsMobile(LocalSerial))
                {
                    ushort hue2 = Hue;
                    AnimationDirection direction = GetMobileAnimationDirection(Graphic, ref hue2, 1);

                    Add(control = new TextureControl
                    {
                        Texture = direction != null ? direction.FrameCount != 0 ? direction.Frames[0] : null : null,
                        X = 5,
                        Y = 5,
                        AcceptMouseInput = false,
                        Hue = Hue == 0 ? hue2 : Hue,
                        IsPartial = TileDataLoader.Instance.StaticData[Graphic].IsPartialHue
                    });

                    if (control.Texture != null)
                    {
                        control.Width = control.Texture.Width;
                        control.Height = control.Texture.Height;
                    }
                    else
                    {
                        control.Width = 35;
                        control.Height = 35;
                    }

                    if (control.Width > 35)
                        control.Width = 35;

                    if (control.Height > 35)
                        control.Height = 35;
                }
                else if (SerialHelper.IsItem(LocalSerial))
                {
                    var texture = ArtLoader.Instance.GetTexture(Graphic);

                    Add(control = new TextureControl
                    {
                        Texture = texture,
                        X = 10 - texture.ImageRectangle.X,
                        Y = 10 + texture.ImageRectangle.Y,
                        Width = texture.ImageRectangle.Width,
                        Height = texture.ImageRectangle.Height,
                        AcceptMouseInput = false,
                        ScaleTexture = false,
                        Hue = Hue,
                        IsPartial = TileDataLoader.Instance.StaticData[Graphic].IsPartialHue
                    });
                }
                else
                    return;

                string subname = $"{itemName} at {Price}gp";

                Add(_name = new Label(subname, true, 0x219, 110, 1, FontStyle.None, TEXT_ALIGN_TYPE.TS_LEFT, true)
                {
                    Y = 0,
                    X = 55
                });

                int height = Math.Max(_name.Height, control.Height) + 10;

                Add(_amountLabel = new Label(count.ToString(), true, 0x0219, 35, 1, FontStyle.None, TEXT_ALIGN_TYPE.TS_RIGHT)
                {
                    X = 168,
                    Y = height >> 2
                });

                Width = 220;


                Height = height;
                WantUpdateSize = false;

                if (World.ClientFeatures.TooltipsEnabled) 
                    SetTooltip(LocalSerial);

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
                    foreach (var label in Children.OfType<Label>())
                        label.Hue = (ushort) (value ? 0x0021 : 0x0219);
                }
            }

            public uint Price { get; set; }
            public ushort Hue { get; set; }
            public ushort Graphic { get; set; }
            public string Name { get; set; }

            public bool NameFromCliloc { get; set; }

            public void SetName(string s, bool new_name)
            {
                _name.Text = new_name ? $"{s}: {Price}" : $"{s} at {Price}gp";
                WantUpdateSize = true;
            }

            protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
            {
                return true;
            }

            public override void Update(double totalMS, double frameMS)
            {
                base.Update(totalMS, frameMS);

                if (SerialHelper.IsMobile(LocalSerial))
                {
                    ushort hue = Hue;
                    var dir = GetMobileAnimationDirection(Graphic, ref hue, 1);
                    if (dir != null)
                        dir.LastAccessTime = Time.Ticks;
                }
            }
        }

        private class TransactionItem : Control
        {
            private readonly Label _amountLabel;

            public TransactionItem(uint serial, ushort graphic, ushort hue, int amount, ushort price, string realname)
            {
                LocalSerial = serial;
                Graphic = graphic;
                Hue = hue;
                Price = price;

                Label l;

                Add(l = new Label(realname, true, 0x021F, 140, 1, FontStyle.None, TEXT_ALIGN_TYPE.TS_LEFT, true)
                {
                    X = 50,
                    Y = 0
                });

                Add(_amountLabel = new Label(amount.ToString(), true, 0x021F, 35, 1, FontStyle.None, TEXT_ALIGN_TYPE.TS_RIGHT)
                {
                    X = 10,
                    Y = 0
                });

                Button buttonAdd;

                Add(buttonAdd = new Button(0, 0x37, 0x37)
                {
                    X = 190,
                    Y = 5,
                    ButtonAction = ButtonAction.Activate,
                    ContainsByBounds = true
                }); // Plus

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
                                _StepChanger+=2;
                        }
                    }
                };

                buttonAdd.MouseDown += (sender, e) =>
                {
                    if (e.Button != MouseButtonType.Left)
                        return;

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

                Add(buttonRemove = new Button(1, 0x38, 0x38)
                {
                    X = 210,
                    Y = 5,
                    ButtonAction = ButtonAction.Activate,
                    ContainsByBounds = true
                }); // Minus

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
                                _StepChanger+=2;
                        }
                    }
                };

                buttonRemove.MouseDown += (sender, e) =>
                {
                    if (e.Button != MouseButtonType.Left)
                        return;

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
            private readonly UOTexture[] _gumpTexture = new UOTexture[3];

            public ResizePicLine(ushort graphic)
            {
                _graphic = graphic;
                CanMove = true;
                CanCloseWithRightClick = true;

                for (int i = 0; i < _gumpTexture.Length; i++)
                {
                    if (_gumpTexture[i] == null)
                        _gumpTexture[i] = GumpsLoader.Instance.GetTexture((ushort) (_graphic + i));
                }

                Height = _gumpTexture.Max(o => o.Height);
            }

            public override void Update(double totalMS, double frameMS)
            {
                foreach (UOTexture t in _gumpTexture)
                {
                    if (t != null)
                        t.Ticks = (long) totalMS;
                }

                base.Update(totalMS, frameMS);
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                ResetHueVector();

                ShaderHuesTraslator.GetHueVector(ref _hueVector, 0, false, Alpha, true);

                int middleWidth = Width - _gumpTexture[0].Width - _gumpTexture[2].Width;
                batcher.Draw2D(_gumpTexture[0], x, y, ref _hueVector);
                batcher.Draw2DTiled(_gumpTexture[1], x + _gumpTexture[0].Width, y, middleWidth, _gumpTexture[1].Height, ref _hueVector);
                batcher.Draw2D(_gumpTexture[2], x + Width - _gumpTexture[2].Width, y, ref _hueVector);

                return base.Draw(batcher, x, y);
            }
        }
    }
}