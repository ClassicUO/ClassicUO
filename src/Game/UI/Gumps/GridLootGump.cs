using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.IO;

namespace ClassicUO.Game.UI.Gumps
{
    class GridLootGump : Gump
    {
        public GridLootGump(Serial local) : base(local, 0)
        {
            Item corpse = World.Items.Get(local);

            if (corpse == null)
            {
                Dispose();
                return;
            }

            AlphaBlendControl background = new AlphaBlendControl();
            background.Width = 300;
            background.Height = 400;


            int x = 10;
            int y = 10;

            foreach (Item item in corpse.Items)
            {
                GridLootItem gridItem = new GridLootItem(item);

                gridItem.X = x;
                gridItem.Y = y;

                x += gridItem.Width + 2;

                if (x >= background.Width)
                {
                    x = 10;
                    y += gridItem.Height + 2;
                }

            }
        }



        class GridLootItem : Control
        {
            private Serial _serial;

            public GridLootItem(Serial serial)
            {
                _serial = serial;

                Item item = World.Items.Get(serial);
                if (item == null)
                {
                    Dispose();
                    return;
                }


                HSliderBar amount = new HSliderBar(0, 0, 100, 1, item.Amount, 1, HSliderBarStyle.MetalWidgetRecessedBar, true);
                Add(amount);


                TextureControl img = new TextureControl();
                img.ScaleTexture = false;
                img.Texture = FileManager.Art.GetTexture(item.DisplayedGraphic);
                img.Hue = item.Hue;
                img.Width = 100;
                img.Height = 100;
                img.WantUpdateSize = false;
                img.AcceptMouseInput = true;
                img.IsPartial = item.ItemData.IsPartialHue;
                img.X = (amount.Width >> 1) + (img.Width >> 1);
                img.Y = amount.Y + amount.Height + 3;

                Add(img);


             
                img.MouseUp += (sender, e) => { GameActions.PickUp(_serial, amount.Value); };

                Width = img.Width;
                Height = img.Height;
            }


        }
    }
}
