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
        const int WIDTH = 400;
        const int HEIGHT = 700;

        private ScrollArea scrollArea;
        private BorderControl borderControl;
        private GumpPicTiled _backgroundTexture;

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

            int borderSize = 38;

            Add(borderControl = new BorderControl(0, 0, Width, Height, borderSize));


            int graphic = 2520;
            borderControl.T_Left = (ushort)graphic;
            borderControl.H_Border = (ushort)(graphic + 1);
            borderControl.T_Right = (ushort)(graphic + 2);
            borderControl.V_Border = (ushort)(graphic + 3);

            Add(_backgroundTexture = new GumpPicTiled(borderSize, borderSize, Width - (borderSize * 2), Height - (borderSize * 2), (ushort)(graphic + 4)));

            borderControl.V_Right_Border = (ushort)(graphic + 5);
            borderControl.B_Left = (ushort)(graphic + 6);
            borderControl.H_Bottom_Border = (ushort)(graphic + 7);
            borderControl.B_Right = (ushort)(graphic + 8);
            borderControl.BorderSize = borderSize;

            //Add(new AlphaBlendControl(0.3f) { Width = Width, Height = Height, Hue = 999 });

            //Add(new AlphaBlendControl(0.3f) { Width = Width, Height = 50});

            TextBox _;
            Add(_ = new TextBox(isPurchaseGump ? "Shop Inventory" : "Your Inventory", ProfileManager.CurrentProfile.DefaultTTFFont, 25, Width, 148, FontStashSharp.RichText.TextHorizontalAlignment.Center, true));
            _.Y = 25 - (_.Height / 2);

            Add(scrollArea = new ScrollArea(1, 51, Width - 2, Height - 52, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways });

            //Add(new SimpleBorder() { Width = Width, Height = Height, Hue = 0, Alpha = 0.3f });
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
            scrollArea.Add(new ShopItem(serial, graphic, hue, amount, price, name, scrollArea.Width - scrollArea.ScrollBarWidth(), 50) { Y = itemY });
            itemY += 50;
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

                Add(new ResizableStaticPic(Graphic, Height, Height) { Hue = hue });

                Add(new TextBox(Name, ProfileManager.CurrentProfile.DefaultTTFFont, 25, Width, 52, dropShadow: true) { X = 51, Y = 1 });

                Add(new TextBox($"Offering [{count}] at {price}gp each.", ProfileManager.CurrentProfile.DefaultTTFFont, 20, Width, 52, dropShadow: true) { X = 51, Y = Height / 2 });

                //Add(new SimpleBorder() { Width = Width, Height = Height, Hue = 0, Alpha = 0.2f });
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (MouseIsOver)
                    backgound.Alpha = 0.6f;
                else
                    backgound.Alpha = 0.01f;
                return base.Draw(batcher, x, y);
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
