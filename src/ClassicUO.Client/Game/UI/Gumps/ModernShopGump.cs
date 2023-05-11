using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
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
        const int WIDTH = 500;
        const int HEIGHT = 700;

        const int ITEM_DESCPTION_WIDTH = 300;

        private ScrollArea scrollArea;
        private StbTextBox searchBox;

        private List<ShopItem> shopItems = new List<ShopItem>();

        private int itemY = 0;

        public ModernShopGump(uint serial, bool isPurchaseGump) : base(serial, 0)
        {
            #region VARS
            X = 200;
            Y = 200;
            Width = WIDTH;
            Height = HEIGHT;

            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            CanMove = true;
            #endregion

            scrollArea = new ScrollArea(1, 75, Width - 2, Height - 77, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways };

            Add(new AlphaBlendControl(0.75f) { Width = Width, Height = Height, Hue = 999 });

            Add(new AlphaBlendControl(0.75f) { Width = Width, Height = 75});

            TextBox _;
            Add(_ = new TextBox(isPurchaseGump ? "Shop Inventory" : "Your Inventory", ProfileManager.CurrentProfile.EmbeddedFont, 25, Width, Color.Orange, FontStashSharp.RichText.TextHorizontalAlignment.Center, true));
            _.Y = 25 - (_.Height / 2);

            searchBox = new StbTextBox(0xFF, 20, 150, true, FontStyle.None, 0x0481)
            {
                X = 1,
                Y = 55,
                Multiline = false,
                Width = Width - 2,
                Height = 20
            };
            searchBox.TextChanged += (s, e) => {
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

            Add(new SimpleBorder() { Width = Width, Height = Height, Hue = 0, Alpha = 0.3f });
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
            ShopItem _ = new ShopItem(serial, graphic, hue, amount, price, name, scrollArea.Width - scrollArea.ScrollBarWidth(), 50);
            _.Y = itemY;
            scrollArea.Add(_);
            shopItems.Add(_);
            itemY += 50;
        }

        private void SearchContents(string text)
        {
            text = text.ToLower();

            List<ShopItem> remove = new List<ShopItem>();
            foreach(ShopItem i in scrollArea.Children.OfType<ShopItem>()) //Remove current shop items
                remove.Add(i);
            foreach (ShopItem i in remove)
                scrollArea.Children.Remove(i); //Actually remove them since we can't modify enumerators

            itemY = 0; //Reset positioning

            foreach(ShopItem i in shopItems)
            {
                if (i.MatchSearch(text))
                {
                    i.Y = itemY;
                    scrollArea.Add(i);
                    itemY += 50;
                }
            }
        }

        private class ShopItem : Control
        {
            AlphaBlendControl backgound;
            public ShopItem(uint serial, ushort graphic, ushort hue, int count, uint price, string name, int width, int height)
            {
                Serial = serial;
                Graphic = graphic;
                Hue = hue;
                Count = count;
                Price = price;
                Name = name;
                Width = width;
                Height = height;

                CanMove = true;
                CanCloseWithRightClick = true;
                AcceptMouseInput = true;

                SetTooltip(serial);

                Add(backgound = new AlphaBlendControl(0.01f) { Width = Width, Height = Height });

                Add(new ResizableStaticPic(Graphic, Height, Height) { Hue = hue, AcceptMouseInput = false });

                TextBox _;
                Add(_ = new TextBox(Name, ProfileManager.CurrentProfile.EmbeddedFont, 25, ITEM_DESCPTION_WIDTH, Color.White, dropShadow: true) { X = 51, Y = 1 });

                Add(_ = new TextBox(count.ToString(), ProfileManager.CurrentProfile.EmbeddedFont, 25, (Width - ITEM_DESCPTION_WIDTH - 55), Color.WhiteSmoke, FontStashSharp.RichText.TextHorizontalAlignment.Right, true) { X = _.X + _.Width });

                Add(_ = new TextBox($"{price}gp", ProfileManager.CurrentProfile.EmbeddedFont, 25, (Width - ITEM_DESCPTION_WIDTH - 55), Color.Gold, FontStashSharp.RichText.TextHorizontalAlignment.Right,  true) { X = _.X });
                _.Y = height - _.Height;

                //Add(new TextBox($"Offering [{count}] at {price}gp each.", ProfileManager.CurrentProfile.DefaultTTFFont, 20, Width, Color.WhiteSmoke, dropShadow: true) { X = 51, Y = Height / 2 });

                Add(new SimpleBorder() { Width = Width, Height = Height, Hue = 0, Alpha = 0.2f });
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

            public uint Serial { get; }
            public ushort Graphic { get; }
            public ushort Hue { get; }
            public int Count { get; }
            public uint Price { get; }
            public string Name { get; }
        }
    }
}
