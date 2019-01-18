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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ShopGump : Gump
    {
        private readonly Label _playerGoldLabel;
        private readonly Dictionary<Item, ShopItem> _shopItems;
        private readonly ScrollArea _shopScrollArea;
        private readonly Label _totalLabel;
        private readonly Dictionary<Item, TransactionItem> _transactionItems;
        private readonly ScrollArea _transactionScrollArea;
        private readonly bool _isBuyGump;
        private bool updateTotal;
        
        public ShopGump(Serial serial, Item[] itemList, bool isBuyGump, int x, int y) : base(serial, 0)
        {
            _transactionItems = new Dictionary<Item, TransactionItem>();
            _shopItems = new Dictionary<Item, ShopItem>();
            _isBuyGump = isBuyGump;
            updateTotal = false;
            X = x;
            Y = y;

            if (isBuyGump)
                Add(new GumpPic(0, 0, 0x0870, 0));
            else
                Add(new GumpPic(0, 0, 0x0872, 0));

            if (isBuyGump)
                Add(new GumpPic(170, 214, 0x0871, 0));
            else
                Add(new GumpPic(170, 214, 0x0873, 0));

            HitBox boxAccept = new HitBox(200, 406, 34, 30)
            {
                Alpha = 1
            };

            HitBox boxClear = new HitBox(372, 410, 24, 24)
            {
                Alpha = 1
            };

            boxAccept.MouseClick += (sender, e) => { OnButtonClick((int) Buttons.Accept); };
            boxClear.MouseClick += (sender, e) => { OnButtonClick((int) Buttons.Clear); };
            Add(boxAccept);
            Add(boxClear);

            if (isBuyGump)
            {
                Add(_totalLabel = new Label("0", false, 0x0386, font: 9)
                {
                    X = 240, Y = 385
                });

                Add(_playerGoldLabel = new Label(World.Player.Gold.ToString(), false, 0x0386, font: 9)
                {
                    X = 358, Y = 385
                });
            }
            else
                Add(_totalLabel = new Label("0", false, 0x0386, font: 9)
                {
                    X = 358, Y = 386
                });

            Add(new Label(World.Player.Name, false, 0x0386, font: 5)
            {
                X = 242, Y = 408
            });
            
            _shopScrollArea = new ScrollArea(20, 60, 235, 150, false);
            
            foreach (var item in itemList)
            {
                ShopItem shopItem;

                _shopScrollArea.Add(shopItem = new ShopItem(item)
                {
                    X = 5, Y = 5
                });

                _shopScrollArea.Add(new ResizePicLine(0x39)
                {
                    X = 10, Width = 210
                });
                shopItem.MouseClick += ShopItem_MouseClick;
                shopItem.MouseDoubleClick += ShopItem_MouseDoubleClick;
                _shopItems.Add(item, shopItem);
            }

            Add(_shopScrollArea);
            Add(_transactionScrollArea = new ScrollArea(200, 280, 225, 80, false));

            AcceptMouseInput = true;
            CanMove = true;
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (updateTotal)
            {
                _totalLabel.Text = _transactionItems.Sum(o => o.Value.Amount * o.Key.Price).ToString();
                updateTotal = false;
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

            if (_transactionItems.TryGetValue(shopItem.Item, out TransactionItem transactionItem))
                transactionItem.Amount++;
            else
            {
                transactionItem = new TransactionItem(shopItem.Item);
                transactionItem.OnIncreaseButtomClicked += TransactionItem_OnIncreaseButtomClicked;
                transactionItem.OnDecreaseButtomClicked += TransactionItem_OnDecreaseButtomClicked;
                _transactionScrollArea.Add(transactionItem);
                _transactionItems.Add(shopItem.Item, transactionItem);
            }

            shopItem.Amount--;
            updateTotal = true;
        }

        private void TransactionItem_OnDecreaseButtomClicked(object sender, EventArgs e)
        {
            var transactionItem = (TransactionItem) sender;

            if (transactionItem.Amount > 0)
            {
                _shopItems[transactionItem.Item].Amount++;
                transactionItem.Amount--;
            }

            if (transactionItem.Amount <= 0) RemoveTransactionItem(transactionItem);
            updateTotal = true;
        }

        private void RemoveTransactionItem(TransactionItem transactionItem)
        {
            _shopItems[transactionItem.Item].Amount += transactionItem.Amount;
            transactionItem.OnIncreaseButtomClicked -= TransactionItem_OnIncreaseButtomClicked;
            transactionItem.OnDecreaseButtomClicked -= TransactionItem_OnDecreaseButtomClicked;
            _transactionItems.Remove(transactionItem.Item);
            _transactionScrollArea.Remove(transactionItem);
            updateTotal = true;
        }

        private void TransactionItem_OnIncreaseButtomClicked(object sender, EventArgs e)
        {
            var transactionItem = (TransactionItem) sender;

            if (_shopItems[transactionItem.Item].Amount > 0)
            {
                _shopItems[transactionItem.Item].Amount--;
                transactionItem.Amount++;
            }

            updateTotal = true;
        }

        private void ShopItem_MouseClick(object sender, MouseEventArgs e)
        {
            foreach (var shopItem in _shopScrollArea.Children.SelectMany(o => o.Children).OfType<ShopItem>()) shopItem.IsSelected = shopItem == sender;
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons) buttonID)
            {
                case Buttons.Accept:
                    var items = _transactionItems.Select(t => new Tuple<uint, ushort>(t.Key.Serial, (ushort) t.Value.Amount)).ToArray();
                    if (_isBuyGump)
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
            private readonly Label _amountLabel;

            public ShopItem(Item item)
            {
                Item = item;
                var itemName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(item.Name);

                Add(new ItemGump(item)
                {
                    X = 5, Y = 5, Height = 50, AcceptMouseInput = false
                });

                Add(new Label($"{itemName} at {item.Price}gp", false, 0x021F, 110, 9)
                {
                    Y = 5, X = 65
                });

                Add(_amountLabel = new Label(item.Amount.ToString(), false, 0x021F, font: 9)
                {
                    X = 180, Y = 20
                });
                Width = 220;
                Height = 30;
            }

            public Item Item { get; }

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
                        label.Hue = (Hue) (value ? 0x0021 : 0x021F);
                }
            }

            protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
            {
                return true;
            }
        }

        private class TransactionItem : Control
        {
            private readonly Label _amountLabel;

            public TransactionItem(Item item)
            {
                Item = item;
                var itemName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(item.ItemData.Name);

                Add(_amountLabel = new Label("1", false, 0x021F, font: 9)
                {
                    X = 5, Y = 5
                });

                Add(new Label($"{itemName} at {item.Price}gp", false, 0x021F, 140, 9)
                {
                    X = 30, Y = 5
                });

                Add(new Button(0, 0x37, 0x37)
                {
                    X = 170, Y = 5, ButtonAction = ButtonAction.Activate
                }); // Plus

                Add(new Button(1, 0x38, 0x38)
                {
                    X = 190, Y = 5, ButtonAction = ButtonAction.Activate
                }); // Minus
                Width = 220;
                Height = 30;
            }

            public Item Item { get; }

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
                        OnIncreaseButtomClicked?.Invoke(this, new EventArgs());

                        break;
                    case 1:
                        OnDecreaseButtomClicked?.Invoke(this, new EventArgs());

                        break;
                }
            }
        }

        private class ResizePicLine : Control
        {
            private readonly Graphic _graphic;
            private readonly SpriteTexture[] _gumpTexture = new SpriteTexture[3];

            public ResizePicLine(Graphic graphic)
            {
                _graphic = graphic;
                CanMove = true;
                CanCloseWithRightClick = true;

                for (int i = 0; i < _gumpTexture.Length; i++)
                {
                    if (_gumpTexture[i] == null)
                        _gumpTexture[i] = FileManager.Gumps.GetTexture((Graphic) (_graphic + i));
                }

                Height = _gumpTexture.Max(o => o.Height);
            }

            public override void Update(double totalMS, double frameMS)
            {
                for (int i = 0; i < _gumpTexture.Length; i++)
                    _gumpTexture[i].Ticks = (long) totalMS;
                base.Update(totalMS, frameMS);
            }

            public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
            {
                Vector3 color = IsTransparent ? ShaderHuesTraslator.GetHueVector(0, false, Alpha, true) : Vector3.Zero;
                int middleWidth = Width - _gumpTexture[0].Width - _gumpTexture[2].Width;
                batcher.Draw2D(_gumpTexture[0], position, color);
                batcher.Draw2DTiled(_gumpTexture[1], new Rectangle(position.X + _gumpTexture[0].Width, position.Y, middleWidth, _gumpTexture[1].Height), color);
                batcher.Draw2D(_gumpTexture[2], new Point(position.X + Width - _gumpTexture[2].Width, position.Y), color);

                return base.Draw(batcher, position, hue);
            }
        }
    }
}