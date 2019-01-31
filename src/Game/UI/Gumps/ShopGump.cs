#region license
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;

using SDL2;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ShopGump : Gump
    {
        private readonly Label _totalLabel, _playerGoldLabel;
        private readonly Dictionary<Item, ShopItem> _shopItems;
        private readonly Dictionary<Item, TransactionItem> _transactionItems;
        private readonly ScrollArea _shopScrollArea, _transactionScrollArea;
        private bool _updateTotal;
        
        public ShopGump(Serial serial, bool isBuyGump, int x, int y) : base(serial, 0)
        {
            X = x;
            Y = y;
            AcceptMouseInput = false;
            AcceptKeyboardInput = true;
            CanMove = true;

            IsBuyGump = isBuyGump;

            _transactionItems = new Dictionary<Item, TransactionItem>();
            _shopItems = new Dictionary<Item, ShopItem>();
            _updateTotal = false;


            if (isBuyGump)
                Add(new GumpPic(0, 0, 0x0870, 0));
            else
                Add(new GumpPic(0, 0, 0x0872, 0));


           
               
            Add(_shopScrollArea = new ScrollArea(30, 60, 225, 180, false, 130));





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

            boxAccept.MouseClick += (sender, e) => { OnButtonClick((int)Buttons.Accept); };
            boxClear.MouseClick += (sender, e) => { OnButtonClick((int)Buttons.Clear); };
            Add(boxAccept);
            Add(boxClear);


            if (isBuyGump)
            {
                Add(_totalLabel = new Label("0", false, 0x0386, font: 9)
                {
                    X = 240,
                    Y = 385
                });

                Add(_playerGoldLabel = new Label(World.Player.Gold.ToString(), false, 0x0386, font: 9)
                {
                    X = 358,
                    Y = 385
                });
            }
            else
                Add(_totalLabel = new Label("0", false, 0x0386, font: 9)
                {
                    X = 358,
                    Y = 386
                });

            Add(new Label(World.Player.Name, false, 0x0386, font: 5)
            {
                X = 242,
                Y = 408
            });


            Add(_transactionScrollArea = new ScrollArea(200, 280, 225, 80, false));


            HitBox upButton = new HitBox(233, 50, 18, 16)
            {
                Alpha = 1
            };
            upButton.MouseDown += (sender, e) =>
            {
                _isUpDOWN = true;
            };
            upButton.MouseUp += (sender, e) => { _isUpDOWN = false; };

            Add(upButton);

            HitBox downButton = new HitBox(233, 190, 18, 16)
            {
                Alpha = 1
            };
            downButton.MouseDown += (sender, e) =>
            {
                _isDownDOWN = true;
            };
            downButton.MouseUp += (sender, e) => { _isDownDOWN = false; };
            Add(downButton);






            HitBox upButtonT = new HitBox(403, 265, 18, 16)
            {
                Alpha = 1
            };
            upButtonT.MouseDown += (sender, e) =>
            {
                _isUpDOWN_T = true;
            };
            upButtonT.MouseUp += (sender, e) => { _isUpDOWN_T = false; };

            Add(upButtonT);

            HitBox downButtonT = new HitBox(403, 370, 18, 16)
            {
                Alpha = 1
            };
            downButtonT.MouseDown += (sender, e) =>
            {
                _isDownDOWN_T = true;
            };
            downButtonT.MouseUp += (sender, e) => { _isDownDOWN_T = false; };
            Add(downButtonT);

            Engine.Input.KeyDown += InputOnKeyDown;
            Engine.Input.KeyUp += InputOnKeyUp;
        }

        private bool _isUpDOWN, _isDownDOWN;
        private bool _isUpDOWN_T, _isDownDOWN_T;

        private bool _shiftPressed;

        private void InputOnKeyUp(object sender, SDL.SDL_KeyboardEvent e)
        {
            if (e.keysym.sym == SDL.SDL_Keycode.SDLK_LSHIFT)
                _shiftPressed = false;
        }

        private void InputOnKeyDown(object sender, SDL.SDL_KeyboardEvent e)
        {
            if (e.keysym.mod == SDL.SDL_Keymod.KMOD_LSHIFT)
                _shiftPressed = true;
        }

        public override void Dispose()
        {
            Engine.Input.KeyDown -= InputOnKeyDown;
            Engine.Input.KeyUp -= InputOnKeyUp;
            base.Dispose();
        }


        public bool IsBuyGump { get; }

        public void SetIfNameIsFromCliloc(Item it, bool fromcliloc)
        {
            if (_shopItems.TryGetValue(it, out var shopItem))
            {
                shopItem.NameFromCliloc = fromcliloc;
            }
        }

        public void AddItem(Item item, bool fromcliloc)
        {
            ShopItem shopItem;

            _shopScrollArea.Add(shopItem = new ShopItem(item)
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
            shopItem.MouseClick += ShopItem_MouseClick;
            shopItem.MouseDoubleClick += ShopItem_MouseDoubleClick;
            _shopItems.Add(item, shopItem);
        }

        public void SetNameTo(Item item, string name)
        {
            if (_shopItems.TryGetValue(item, out ShopItem shopItem))
            {
                shopItem.SetName(name);
            }
        }


        public override void Update(double totalMS, double frameMS)
        {
            if (!World.InGame || IsDisposed)
                return;

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
                _totalLabel.Text = _transactionItems.Sum(o => o.Value.Amount * o.Key.Price).ToString();
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

            if (_transactionItems.TryGetValue(shopItem.Item, out TransactionItem transactionItem))
                transactionItem.Amount += total;
            else
            {
                transactionItem = new TransactionItem(shopItem.Item, total);
                transactionItem.OnIncreaseButtomClicked += TransactionItem_OnIncreaseButtomClicked;
                transactionItem.OnDecreaseButtomClicked += TransactionItem_OnDecreaseButtomClicked;
                _transactionScrollArea.Add(transactionItem);
                _transactionItems.Add(shopItem.Item, transactionItem);
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
                _shopItems[transactionItem.Item].Amount += total;
                transactionItem.Amount -= total;
            }

            if (transactionItem.Amount <= 0)
                RemoveTransactionItem(transactionItem);
            _updateTotal = true;
        }

        private void RemoveTransactionItem(TransactionItem transactionItem)
        {
            _shopItems[transactionItem.Item].Amount += transactionItem.Amount;
            transactionItem.OnIncreaseButtomClicked -= TransactionItem_OnIncreaseButtomClicked;
            transactionItem.OnDecreaseButtomClicked -= TransactionItem_OnDecreaseButtomClicked;
            _transactionItems.Remove(transactionItem.Item);
            _transactionScrollArea.Remove(transactionItem);
            _updateTotal = true;
        }

        private void TransactionItem_OnIncreaseButtomClicked(object sender, EventArgs e)
        {
            var transactionItem = (TransactionItem) sender;

            if (_shopItems[transactionItem.Item].Amount > 0)
            {
                _shopItems[transactionItem.Item].Amount--;
                transactionItem.Amount++;
            }

            _updateTotal = true;
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

        class ShopItem : Control
        {
            private readonly Label _amountLabel, _name;

           
            public ShopItem(Item item)
            {
                Item = item;
                string itemName = StringHelper.CapitalizeAllWords(item.Name);

                TextureControl control;

                if (item.Serial.IsMobile)
                {
                    byte group = 0;

                    switch (FileManager.Animations.GetGroupIndex(item.Graphic))
                    {
                        case ANIMATION_GROUPS.AG_LOW:
                            group = (byte)LOW_ANIMATION_GROUP.LAG_STAND;

                            break;
                        case ANIMATION_GROUPS.AG_HIGHT:
                            group = (byte) HIGHT_ANIMATION_GROUP.HAG_STAND;
                            break;
                        case ANIMATION_GROUPS.AG_PEOPLE:
                            group = (byte)PEOPLE_ANIMATION_GROUP.PAG_STAND;

                            break;
                    }

                    ref AnimationDirection direction = ref FileManager.Animations.DataIndex[item.Graphic].Groups[group].Direction[1];
                    FileManager.Animations.AnimID = item.Graphic;
                    FileManager.Animations.AnimGroup = group;
                    FileManager.Animations.Direction = 1;

                    if (direction.FrameCount == 0)
                        FileManager.Animations.LoadDirectionGroup(ref direction);

                    Add(control = new TextureControl()
                    {
                        Texture = FileManager.Animations.GetTexture(direction.FramesHashes[0]),
                        X = 5, Y = 5,
                        AcceptMouseInput = false,
                    });

                    control.Width = control.Texture.Width;
                    control.Height = control.Texture.Height;

                    if (control.Width > 35)
                        control.Width = 35;

                    if (control.Height > 35)
                        control.Height = 35;
                }
                else if (item.Serial.IsItem)
                {
                    var texture = FileManager.Art.GetTexture(item.Graphic);
                    Add(control = new TextureControl()
                    {
                        Texture = texture,
                        X = 5,
                        Y = 5,
                        Width = texture.ImageRectangle.Width,
                        Height = texture.ImageRectangle.Height,
                        AcceptMouseInput = false,
                        ScaleTexture = false,
                    });
                }
                else 
                    return;

             

                Add(_name = new Label($"{itemName} at {item.Price}gp", false, 0x021F, 110, 9)
                {
                    Y = 5, X = 65
                });

                Add(_amountLabel = new Label(item.Amount.ToString(), false, 0x021F, font: 9)
                {
                    X = 180, Y = 20
                });

                Width = 220;


                Height = Math.Max(_name.Height, control.Height) + 10;
                WantUpdateSize = false;
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

            public bool NameFromCliloc { get; set; }

            public void SetName(string s)
            {
                if (NameFromCliloc)
                    _name.Text = $"{s} at {Item.Price}gp";
            }

            protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
            {
                return true;
            }

            public override void Update(double totalMS, double frameMS)
            {
                base.Update(totalMS, frameMS);

                if (Item != null)
                {
                    if (Item.Serial.IsMobile)
                    {
                        byte group = 0;

                        switch (FileManager.Animations.GetGroupIndex(Item.Graphic))
                        {
                            case ANIMATION_GROUPS.AG_LOW:
                                group = (byte) LOW_ANIMATION_GROUP.LAG_STAND;

                                break;
                            case ANIMATION_GROUPS.AG_HIGHT:
                                group = (byte) HIGHT_ANIMATION_GROUP.HAG_STAND;

                                break;
                            case ANIMATION_GROUPS.AG_PEOPLE:
                                group = (byte) PEOPLE_ANIMATION_GROUP.PAG_STAND;

                                break;
                        }

                        ref AnimationDirection direction = ref FileManager.Animations.DataIndex[Item.Graphic].Groups[group].Direction[1];
                        direction.LastAccessTime = Engine.Ticks;
                    }
                }
            }

        }

        private class TransactionItem : Control
        {
            private readonly Label _amountLabel;

            public TransactionItem(Item item, int amount)
            {
                Item = item;
                var itemName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(item.Name);

                Add(_amountLabel = new Label(amount.ToString(), false, 0x021F, font: 9)
                {
                    X = 5, Y = 5
                });

                Add(new Label($"{itemName} at {item.Price}gp", false, 0x021F, 140, 9)
                {
                    X = 30, Y = 5
                });

                Button buttonAdd;
                Add(buttonAdd = new Button(0, 0x37, 0x37)
                {
                    X = 170, Y = 5, ButtonAction = ButtonAction.Activate, ContainsByBounds = true
                }); // Plus

                int status = 0;

                float t0 = Engine.Ticks;

                buttonAdd.MouseOver += (sender, e) =>
                {
                    if (status == 2)
                    {
                        if (Mouse.LButtonPressed && Engine.Ticks > t0)
                        {
                            t0 = Engine.Ticks + 50;
                            OnButtonClick(0);
                        }
                    }
                    else if (Engine.Ticks > t0)
                    {
                        status = 2;
                        t0 = 0;
                    }
                };

                buttonAdd.MouseDown += (sender, e) =>
                {
                    status = 1;
                    t0 = Engine.Ticks + 200;
                };
                buttonAdd.MouseUp += (sender, e) => { status = 0; };


                Button buttonRemove;
                Add(buttonRemove = new Button(1, 0x38, 0x38)
                {
                    X = 190, Y = 5, ButtonAction = ButtonAction.Activate,
                    ContainsByBounds = true
                }); // Minus

                float t1 = Engine.Ticks;

                buttonRemove.MouseOver += (sender, e) =>
                {
                    if (status == 2)
                    {
                        if (Mouse.LButtonPressed && Engine.Ticks > t1)
                        {
                            t1 = Engine.Ticks + 50;
                            OnButtonClick(1);
                        }
                    }
                    else if (Engine.Ticks > t1)
                    {
                        status = 2;
                        t1 = 0;
                    }
                };

                buttonRemove.MouseDown += (sender, e) =>
                {
                    status = 1;
                    t1 = Engine.Ticks + 500;
                };
                buttonRemove.MouseUp += (sender, e) => { status = 0; };


                Width = 220;
                Height = 30;
                WantUpdateSize = false;
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