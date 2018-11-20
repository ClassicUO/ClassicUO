using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class ShopGump : Gump
    {
        private readonly Label _playerGoldLabel;
        private readonly Dictionary<Item, ShopItem> _shopItems;
        private readonly ScrollArea _shopScrollArea;
        private readonly Label _totalLabel;
        private readonly Dictionary<Item, TransactionItem> _transactionItems;
        private readonly ScrollArea _transactionScrollArea;
        private bool updateTotal;

        public ShopGump(Mobile shopMobile, bool isBuyGump, int x, int y) : base(shopMobile.Serial, 0)
        {
            _transactionItems = new Dictionary<Item, TransactionItem>();
            _shopItems = new Dictionary<Item, ShopItem>();
            updateTotal = false;
            X = x;
            Y = y;

            if (isBuyGump)
                AddChildren(new GumpPic(0, 0, 0x0870, 0));
            else
                AddChildren(new GumpPic(0, 0, 0x0872, 0));

            if (isBuyGump)
                AddChildren(new GumpPic(170, 214, 0x0871, 0));
            else
                AddChildren(new GumpPic(170, 214, 0x0873, 0));

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
            AddChildren(boxAccept);
            AddChildren(boxClear);

            if (isBuyGump)
            {
                AddChildren(_totalLabel = new Label("0", false, 0x0386, font: 9)
                {
                    X = 240, Y = 385
                });

                AddChildren(_playerGoldLabel = new Label(World.Player.Gold.ToString(), false, 0x0386, font: 9)
                {
                    X = 358, Y = 385
                });
            }
            else
                AddChildren(_totalLabel = new Label("0", false, 0x0386, font: 9)
                {
                    X = 358, Y = 386
                });

            AddChildren(new Label(World.Player.Name, false, 0x0386, font: 5)
            {
                X = 242, Y = 408
            });
            AcceptMouseInput = true;
            CanMove = true;
            _shopScrollArea = new ScrollArea(20, 60, 235, 150, false);
            var itemsToShow = shopMobile.Items.Where(o => o.Layer == Layer.ShopResale || o.Layer == Layer.ShopBuy).SelectMany(o => o.Items).OrderBy(o => o.Serial.Value);

            foreach (var item in itemsToShow)
            {
                ShopItem shopItem;

                _shopScrollArea.AddChildren(shopItem = new ShopItem(item)
                {
                    X = 5, Y = 5
                });

                _shopScrollArea.AddChildren(new ResizePicLine(0x39)
                {
                    X = 10, Width = 210
                });
                shopItem.MouseClick += ShopItem_MouseClick;
                shopItem.MouseDoubleClick += ShopItem_MouseDoubleClick;
                _shopItems.Add(item, shopItem);
            }

            AddChildren(_shopScrollArea);
            AddChildren(_transactionScrollArea = new ScrollArea(200, 280, 225, 80, false));
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (updateTotal)
            {
                _totalLabel.Text = _transactionItems.Sum(o => o.Value.Amount * o.Key.Price).ToString();
                updateTotal = false;
            }

            _playerGoldLabel.Text = World.Player.Gold.ToString();
            base.Update(totalMS, frameMS);
        }

        private void ShopItem_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var shopItem = (ShopItem) sender;
            TransactionItem transactionItem;

            if (shopItem.Amount <= 0)
                return;

            if (_transactionItems.TryGetValue(shopItem.Item, out transactionItem))
                transactionItem.Amount++;
            else
            {
                transactionItem = new TransactionItem(shopItem.Item);
                transactionItem.OnIncreaseButtomClicked += TransactionItem_OnIncreaseButtomClicked;
                transactionItem.OnDecreaseButtomClicked += TransactionItem_OnDecreaseButtomClicked;
                _transactionScrollArea.AddChildren(transactionItem);
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
            _transactionScrollArea.RemoveChildren(transactionItem);
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
                    NetClient.Socket.Send(new PBuyRequest(LocalSerial, items));
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

        private class ShopItem : GumpControl
        {
            private readonly Label _amountLabel;

            public ShopItem(Item item)
            {
                Item = item;
                var itemName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(item.Name);

                AddChildren(new ItemGump(item)
                {
                    X = 5, Y = 5, Height = 50, AcceptMouseInput = false
                });

                AddChildren(new Label($"{itemName} at {item.Price}gp", false, 0x021F, 110, 9)
                {
                    Y = 5, X = 65
                });

                AddChildren(_amountLabel = new Label(item.Amount.ToString(), false, 0x021F, font: 9)
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

        private class TransactionItem : GumpControl
        {
            private readonly Label _amountLabel;

            public TransactionItem(Item item)
            {
                Item = item;
                var itemName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(item.ItemData.Name);

                AddChildren(_amountLabel = new Label("1", false, 0x021F, font: 9)
                {
                    X = 5, Y = 5
                });

                AddChildren(new Label($"{itemName} at {item.Price}gp", false, 0x021F, 150, 9)
                {
                    X = 30, Y = 5
                });

                AddChildren(new Button(0, 0x37, 0x37)
                {
                    X = 170, Y = 5, ButtonAction = ButtonAction.Activate
                }); // Plus

                AddChildren(new Button(1, 0x38, 0x38)
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

        private class ResizePicLine : GumpControl
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
                        _gumpTexture[i] = IO.Resources.Gumps.GetGumpTexture((Graphic) (_graphic + i));
                }

                Height = _gumpTexture.Max(o => o.Height);
            }

            public override void Update(double totalMS, double frameMS)
            {
                for (int i = 0; i < _gumpTexture.Length; i++)
                    _gumpTexture[i].Ticks = (long) totalMS;
                base.Update(totalMS, frameMS);
            }

            public override bool Draw(SpriteBatchUI spriteBatch, Point position, Vector3? hue = null)
            {
                Vector3 color = IsTransparent ? ShaderHuesTraslator.GetHueVector(0, false, .5f, true) : Vector3.Zero;
                int middleWidth = Width - _gumpTexture[0].Width - _gumpTexture[2].Width;
                spriteBatch.Draw2D(_gumpTexture[0], position, color);
                spriteBatch.Draw2DTiled(_gumpTexture[1], new Rectangle(position.X + _gumpTexture[0].Width, position.Y, middleWidth, _gumpTexture[1].Height), color);
                spriteBatch.Draw2D(_gumpTexture[2], new Point(position.X + Width - _gumpTexture[2].Width, position.Y), color);

                return base.Draw(spriteBatch, position, hue);
            }
        }
    }
}