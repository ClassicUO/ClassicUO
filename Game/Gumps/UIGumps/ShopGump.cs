using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class ShopGump: Gump
    {
        private ScrollArea _shopScrollArea;

        public ShopGump(Mobile shopMobile, bool isBuyGump, int x, int y) : base(shopMobile.Serial, 0)
        {
            X = x;
            Y = y;

            if (isBuyGump)
                AddChildren(new GumpPic(0, 0, 0x0870, 0));
            else
                AddChildren(new GumpPic(0, 0, 0x0872, 0));

            // AddChildren(new CGUIShader(&g_ColorizerShader, true));
            //m_ItemList[0] =
            //    (CGUIHTMLGump*)AddChildren(new CGUIHTMLGump(ID_GB_SHOP_LIST, 0, 30, 60, 215, 176, false, true));
            //AddChildren(new CGUIShader(&g_ColorizerShader, false));

            if (isBuyGump)
                AddChildren(new GumpPic(170, 214, 0x0871, 0));
            else
                AddChildren(new GumpPic(170, 214, 0x0873, 0));

            //m_ItemList[1] =
            //    (CGUIHTMLGump*)AddChildren(new CGUIHTMLGump(ID_GB_SHOP_RESULT, 0, 200, 280, 215, 92, false, true));

            AddChildren(new HitBox((int)Buttons.Accept, 200, 406, 34, 30));
            AddChildren(new HitBox((int)Buttons.Clear, 372, 410, 24, 24));

            if (isBuyGump)
            {
                AddChildren(new Label("0", false, 0x0386, font: 9) { X = 240, Y = 385 });
                AddChildren(new Label(World.Player.Gold.ToString(), false, 0x0386, font: 9) { X = 358, Y = 385 });
            }
            else
            {
                AddChildren(new Label("0", false, 0x0386, font: 9) { X = 358, Y = 386 });
            }

            AddChildren(new Label(World.Player.Name, false, 0x0386, font: 5) { X = 242, Y = 408 });

            AcceptMouseInput = true;
            CanMove = true;

            _shopScrollArea = new ScrollArea(20, 60, 240, 150, false);
            
            foreach (var layerItems in shopMobile.Items.Where(o => o.Layer == Layer.ShopResale || o.Layer == Layer.ShopBuy))
            {
                foreach(var item in layerItems.Items)
                {
                    ShopItem shopItem;
                    _shopScrollArea.AddChildren(shopItem = new ShopItem(item) { X = 5, Y = 5 });
                    _shopScrollArea.AddChildren(new ResizePicLine(0x39) { X = 10, Width = 210 });

                    shopItem.MouseClick += ShopItem_MouseClick;
                    shopItem.MouseDoubleClick += ShopItem_MouseDoubleClick;
                }
            }
            // 0x34

            AddChildren(_shopScrollArea);
        }

        private void ShopItem_MouseDoubleClick(object sender, Input.MouseEventArgs e)
        {
            
        }

        private void ShopItem_MouseClick(object sender, Input.MouseEventArgs e)
        {
            foreach (var shopItem in _shopScrollArea.Children.SelectMany(o => o.Children).OfType<ShopItem>())
            {
                shopItem.IsSelected = shopItem == sender;
            }
        }

        private enum Buttons
        {
            Accept, Clear
        }

        private class ShopItem : GumpControl
        {
            private readonly Item _item;

            public bool IsSelected
            {
                set
                {
                    foreach(var label in Children.OfType<Label>())
                        label.Hue = (Hue)(value ? 0x0021 : 0x021F);
                }
            }

            public ShopItem(Item item)
            {
                _item = item;
                var itemName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(item.ItemData.Name);
                AddChildren(new ItemGumpling(item) { X = 5, Y = 5, Height = 50, AcceptMouseInput = false });
                AddChildren(new Label($"{itemName} at {item.Price}gp", false, 0x021F, 110, 9) { Y = 5, X = 65 });
                AddChildren(new Label(item.Amount.ToString(), false, 0x021F, font: 9) { X = 180, Y = 20 });

                Width = 220;
                Height = 30;
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
                        _gumpTexture[i] = IO.Resources.Gumps.GetGumpTexture((Graphic)(_graphic + i));
                }

                Height = _gumpTexture.Max(o => o.Height);
            }

            public override void Update(double totalMS, double frameMS)
            {
                for (int i = 0; i < _gumpTexture.Length; i++)
                    _gumpTexture[i].Ticks = (long)totalMS;
                base.Update(totalMS, frameMS);
            }

            public override bool Draw(SpriteBatchUI spriteBatch, Point position, Vector3? hue = null)
            {
                Vector3 color = IsTransparent ? RenderExtentions.GetHueVector(0, false, .5f, true) : Vector3.Zero;

                int middleWidth = Width - _gumpTexture[0].Width - _gumpTexture[2].Width;

                spriteBatch.Draw2D(_gumpTexture[0], position, color);
                spriteBatch.Draw2DTiled(_gumpTexture[1], new Rectangle(position.X + _gumpTexture[0].Width, position.Y, middleWidth, _gumpTexture[1].Height), color);
                spriteBatch.Draw2D(_gumpTexture[2], new Point(position.X + Width - _gumpTexture[2].Width, position.Y), color);

                return base.Draw(spriteBatch, position, hue);
            }
        }
    }
}
