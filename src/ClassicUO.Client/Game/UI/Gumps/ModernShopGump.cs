using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ModernShopGump : Gump
    {
        private int WIDTH = 450;

        const int ITEM_DESCPTION_WIDTH = 300;
        private readonly bool isPurchaseGump;
        private ScrollArea scrollArea;
        private StbTextBox searchBox;
        private AlphaBlendControl background;
        private SimpleBorder border;

        private HitBox resizeDrag;
        private bool dragging = false;
        private int dragStartH = 0;

        private List<ShopItem> shopItems = new List<ShopItem>();

        private int itemY = 0;

        public ModernShopGump(uint serial, bool isPurchaseGump) : base(serial, 0)
        {
            #region VARS
            X = 200;
            Y = 200;
            Width = WIDTH;
            Height = ProfileManager.CurrentProfile.VendorGumpHeight;
            if (Height < 200)
                Height = 200;

            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            CanMove = true;

            this.isPurchaseGump = isPurchaseGump;
            #endregion

            scrollArea = new ScrollArea(1, 75, Width - 2, Height - 77, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways };

            Add(background = new AlphaBlendControl(0.75f) { Width = Width, Height = Height, Hue = 997 });

            Add(new AlphaBlendControl(0.65f) { Width = Width, Height = 75, Hue = 997 });

            TextBox _;
            Add(_ = new TextBox(isPurchaseGump ? "Shop Inventory" : "Your Inventory", TrueTypeLoader.EMBEDDED_FONT, 30, Width, Color.LightBlue, FontStashSharp.RichText.TextHorizontalAlignment.Center, true));
            _.Y = (50 - _.MeasuredSize.Y) / 2;

            Add(_ = new TextBox("Item", TrueTypeLoader.EMBEDDED_FONT, 20, 75, Color.White, dropShadow: true) { X = 5 });
            _.Y = 55 - _.Height;

            Add(_ = new TextBox("/c[white]Avail.\n/cdPrice per item", TrueTypeLoader.EMBEDDED_FONT, 20, 150, Color.Gold, FontStashSharp.RichText.TextHorizontalAlignment.Right, true));
            _.Y = 55 - _.Height;
            _.X = Width - 1 - _.Width - 5;

            searchBox = new StbTextBox(0xFF, 20, 150, true, FontStyle.None, 0x0481)
            {
                X = 1,
                Y = 55,
                Multiline = false,
                Width = Width - 1,
                Height = 20
            };
            searchBox.TextChanged += (s, e) =>
            {
                SearchContents(searchBox.Text);
            };
            searchBox.Add(new AlphaBlendControl(0.5f)
            {
                Hue = 0x0481,
                Width = searchBox.Width,
                Height = searchBox.Height
            });

            Add(searchBox);

            Add(scrollArea);

            Add(resizeDrag = new HitBox(Width / 2 - 10, Height - 10, 20, 10, "Drag to resize", 0.50f));
            resizeDrag.Add(new AlphaBlendControl(0.5f) { Width = 20, Height = 10, Hue = 997 });
            resizeDrag.MouseDown += ResizeDrag_MouseDown;
            resizeDrag.MouseUp += ResizeDrag_MouseUp;

            Add(border = new SimpleBorder() { Width = Width, Height = Height, Hue = 0, Alpha = 0.3f });
        }

        private void ResizeDrag_MouseUp(object sender, Input.MouseEventArgs e)
        {
            dragging = false;
        }

        private void ResizeDrag_MouseDown(object sender, Input.MouseEventArgs e)
        {
            dragStartH = Height;
            dragging = true;
        }

        public override void Update()
        {
            base.Update();

            int steps = Mouse.LDragOffset.Y;

            if (dragging && steps != 0)
            {
                Height = dragStartH + steps;
                if (Height < 200)
                    Height = 200;
                ProfileManager.CurrentProfile.VendorGumpHeight = Height;

                background.Height = Height;
                scrollArea.Height = Height - 77;
                resizeDrag.Y = Height - 10;
                border.Height = Height;
            }
        }

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
            if (IsDisposed)
                return;
            ShopItem _ = new ShopItem(serial, graphic, hue, amount, price, name, scrollArea.Width - scrollArea.ScrollBarWidth(), 50, isPurchaseGump, LocalSerial);
            _.Y = itemY;
            scrollArea.Add(_);
            shopItems.Add(_);
            itemY += _.Height;
        }

        private void SearchContents(string text)
        {
            text = text.ToLower();

            List<ShopItem> remove = new List<ShopItem>();
            foreach (ShopItem i in scrollArea.Children.OfType<ShopItem>()) //Remove current shop items
                remove.Add(i);
            foreach (ShopItem i in remove)
                scrollArea.Children.Remove(i); //Actually remove them since we can't modify enumerators

            itemY = 0; //Reset positioning

            foreach (ShopItem i in shopItems)
            {
                if (i.MatchSearch(text))
                {
                    i.Y = itemY;
                    scrollArea.Add(i);
                    itemY += i.Height;
                }
            }
        }

        private class ShopItem : Control
        {
            AlphaBlendControl backgound;
            Area itemInfo, purchaseSell;
            BuySellButton buySellButton;
            private readonly uint gumpSerial;

            public ShopItem(uint serial, ushort graphic, ushort hue, int count, uint price, string name, int width, int height, bool isPurchase, uint gumpSerial)
            {
                Serial = serial;
                Graphic = graphic;
                Hue = hue;
                Count = count;
                Price = price;
                Name = name;
                this.gumpSerial = gumpSerial;
                Width = width;
                Height = height;

                CanMove = true;
                CanCloseWithRightClick = true;
                AcceptMouseInput = true;

                SetTooltip(serial);



                Add(backgound = new AlphaBlendControl(0.01f) { Width = Width, Height = Height });

                Add(new ResizableStaticPic(Graphic, Height, Height) { Hue = hue, AcceptMouseInput = false });

                itemInfo = new Area(false) { Width = Width - Height, Height = Height, X = Height, AcceptMouseInput = false };
                purchaseSell = new Area(false) { Width = Width - Height, Height = Height, X = Height, AcceptMouseInput = false, IsVisible = false };

                #region ITEM INFO
                TextBox _;
                itemInfo.Add(_ = new TextBox(Name, TrueTypeLoader.EMBEDDED_FONT, 25, ITEM_DESCPTION_WIDTH - Height, Color.White, dropShadow: true));
                _.Y = (itemInfo.Height - _.MeasuredSize.Y) / 2;

                TextBox countTB;
                itemInfo.Add(countTB = new TextBox($"x{count}", TrueTypeLoader.EMBEDDED_FONT, 20, ITEM_DESCPTION_WIDTH - Height, Color.WhiteSmoke, FontStashSharp.RichText.TextHorizontalAlignment.Right, true) { Y = 3 });
                countTB.X = itemInfo.Width - countTB.Width - 3;

                itemInfo.Add(_ = new TextBox($"{price}gp", TrueTypeLoader.EMBEDDED_FONT, 25, ITEM_DESCPTION_WIDTH - Height, Color.Gold, FontStashSharp.RichText.TextHorizontalAlignment.Right, true));
                _.Y = itemInfo.Height - _.Height - 3;
                _.X = itemInfo.Width - _.Width - 3;
                #endregion

                #region PURCHASE OR SELL
                purchaseSell.Add(new TextBox($"How many would you like to {(isPurchase ? "buy" : "sell")} at /c[gold]{price}gp /cdeach?", TrueTypeLoader.EMBEDDED_FONT, 18, purchaseSell.Width - 76, Color.White, dropShadow: true));

                AlphaBlendControl sliderBG;
                purchaseSell.Add(sliderBG = new AlphaBlendControl(0.5f) { Width = purchaseSell.Width - 78, Height = 5, Hue = 148, BaseColor = Color.White });

                HSliderBar quantity;
                purchaseSell.Add(quantity = new HSliderBar(3, 0, purchaseSell.Width - 78, 1, Count, 1, HSliderBarStyle.BlueWidgetNoBar));
                quantity.Y = purchaseSell.Height - quantity.Height;
                sliderBG.Y = quantity.Y + (quantity.Height / 2) - 2;

                quantity.ValueChanged += (sender, e) =>
                {
                    buySellButton.UpdateQuantity(quantity.Value, (int)price);
                };


                purchaseSell.Add(buySellButton = new BuySellButton(isPurchase, 1, (int)price) { X = purchaseSell.Width - 75 });

                buySellButton.MouseUp += (sender, e) =>
                {
                    Dictionary<uint, ushort> theItem = new Dictionary<uint, ushort>();
                    theItem.Add(serial, (ushort)quantity.Value);

                    Tuple<uint, ushort>[] item = theItem.Select(t => new Tuple<uint, ushort>(t.Key, (ushort)t.Value)).ToArray();
                  
                    if (isPurchase)
                    {
                        NetClient.Socket.Send_BuyRequest(gumpSerial, item);
                        count -= quantity.Value;
                    }
                    else
                    {
                        NetClient.Socket.Send_SellRequest(gumpSerial, item);
                        count -= quantity.Value;
                    }

                    if (count < 1)
                        Dispose();
                    else
                    {
                        quantity.MaxValue = count;
                        quantity.Value = 1;
                        countTB.Text = $"x{count}";
                    }
                };

                #endregion

                Add(itemInfo);
                Add(purchaseSell);

                Add(new SimpleBorder() { Width = Width, Height = Height, Hue = 0, Alpha = 0.2f });
            }

            protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
            {
                base.OnMouseDoubleClick(x, y, button);

                if (button == MouseButtonType.Left)
                {
                    itemInfo.IsVisible ^= true;
                    purchaseSell.IsVisible ^= true;
                }

                return true;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (MouseIsOver)
                    backgound.Alpha = 0.6f;
                else
                    backgound.Alpha = 0.01f;
                return base.Draw(batcher, x, y);
            }

            public bool MatchSearch(string text)
            {
                if (Name.ToLower().Contains(text))
                    return true;
                if (World.OPL.TryGetNameAndData(Serial, out string name, out string data))
                {
                    if (data.ToLower().Contains(text))
                        return true;
                }
                return false;
            }

            private class BuySellButton : Control
            {
                private readonly bool isPurchase;
                TextBox text;

                public BuySellButton(bool isPurchase, int quantity, int price)
                {
                    Width = 75;
                    Height = 50;
                    AcceptMouseInput = true;
                    CanCloseWithRightClick = true;
                    this.isPurchase = isPurchase;

                    Add(new AlphaBlendControl() { Width = Width, Height = Height });

                    UpdateQuantity(quantity, price);

                    Add(new SimpleBorder() { Width = Width, Height = Height, Hue = 997, Alpha = 0.2f });

                }

                public void UpdateQuantity(int quantity, int price)
                {
                    if (text == null)
                    {
                        text = new TextBox($"{(isPurchase ? "Buy" : "Sell")}\n{quantity}\n/c[Gold]{price * quantity}", TrueTypeLoader.EMBEDDED_FONT, 17, Width, Color.White, FontStashSharp.RichText.TextHorizontalAlignment.Center, true);
                        Add(text);
                    }
                    else
                    {
                        text.Text = $"{(isPurchase ? "Buy" : "Sell")}\n{quantity}\n/c[Gold]{price * quantity}";
                    }
                }

                public override bool Draw(UltimaBatcher2D batcher, int x, int y)
                {
                    base.Draw(batcher, x, y);

                    if (MouseIsOver)
                    {
                        batcher.Draw(
                            SolidColorTextureCache.GetTexture(Color.White),
                            new Rectangle(x, y, Width, Height),
                            new Vector3(0, 0, 0.2f)
                            );
                    }

                    return true;
                }
            }

            public uint Serial { get; }
            public ushort Graphic { get; }
            public ushort Hue { get; }
            public int Count { get; }
            public uint Price { get; }
            public string Name { get; }
        }
    }
}
