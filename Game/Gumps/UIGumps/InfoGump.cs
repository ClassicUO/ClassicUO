using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Renderer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class InfoGump : Gump
    {
        private const int WIDTH = 300;
        private const int HEIGHT = 600;
        private readonly ScrollArea _scrollArea;
        public InfoGump(object obj) : base(0, 0)
        {
            X = 200;
            Y = 200;
            CanMove = true;
            AcceptMouseInput = false;
            AddChildren(new GameBorder(0, 0, WIDTH, HEIGHT, 4));

            AddChildren(new GumpPicTiled(4, 4, WIDTH - 8, HEIGHT - 8, 0x0A40)
            {
                IsTransparent = true
            });

            AddChildren(new GumpPicTiled(4, 4, WIDTH - 8, HEIGHT - 8, 0x0A40)
            {
                IsTransparent = true
            });
            AddChildren(new Label("Object Information", true, 1153, font: 3) { X = 20, Y = 20 });
            AddChildren(new Line(20, 50, 250, 1, 0xFFFFFFFF));
            _scrollArea = new ScrollArea(20, 60, WIDTH - 40, 510, true)
            {
                AcceptMouseInput = true
            };
            AddChildren(_scrollArea);

            foreach (var item in ReflectionHolder.GameObjectDictionary(obj))
            {
                if (item.Value != typeof(object))
                {
                    _scrollArea.AddChildren(new Label(item.Key + " : " + item.Value, true, 1153, font: 3));
                }
                

            }

        }
    }

    public class InfoGumpEntry : GumpControl
    {
        public readonly Label Entry;

        public InfoGumpEntry(Label entry)
        {
            Entry = entry;
            Entry.X = 20;
            AddChildren(Entry);

        }


        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
        }

    }
}
