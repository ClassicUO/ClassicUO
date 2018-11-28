using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.IO;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class TradingGump : Gump
    {
        private Checkbox _myCheckbox;
        private GumpPic _hisPic;

        private bool _imAccepting, _heIsAccepting;

        private Entity _entity1, _entity2;

        public TradingGump(Serial local, string name, Serial id1, Serial id2) : base(local, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;

            AddChildren(new GumpPic(0,0, 0x0866, 0));

            if (FileManager.ClientVersion < ClientVersions.CV_500A)
            {

            }

            AddChildren(new Label(World.Player.Name, false, 0x0386, font: 1)
                            { X = 84, Y = 40 });

            int fontWidth = Fonts.GetWidthASCII(1, name);

            AddChildren(new Label(name, false, 0x0386, font: 1)
            {X = fontWidth, Y = 170 });

            _entity1 = World.Get(id1);
            _entity2 = World.Get(id2);

            _entity1.Items.Added += ItemsOnAdded1;
            _entity2.Items.Added += ItemsOnAdded2;
        }

      
        public bool ImAccepting
        {
            get => _imAccepting;
            set
            {
                if (_imAccepting != value)
                {
                    _imAccepting = value;
                    UpdateState();
                }
            }
        }

        public bool HeIsAccepting
        {
            get => _heIsAccepting;
            set
            {
                if (_heIsAccepting != value)
                {
                    _heIsAccepting = value;
                    UpdateState();
                }
            }
        }

        //public List<Item> Items { get; } = new List<Item>();


        private void ItemsOnAdded1(object sender, CollectionChangedEventArgs<Item> e)
        {
            foreach (Item item in e)
            {
                AddChildren(new ItemGump(item)
                {
                    HighlightOnMouseOver = true,

                });
            }
        }

        private void ItemsOnAdded2(object sender, CollectionChangedEventArgs<Item> e)
        {
        }

        public override void Dispose()
        {
            base.Dispose();

            _entity1.Items.Added -= ItemsOnAdded1;
            _entity2.Items.Added -= ItemsOnAdded2;
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);


        }


        private void UpdateState()
        {
            _myCheckbox?.Dispose();
            _hisPic?.Dispose();

            if (ImAccepting)
            {
                _myCheckbox = new Checkbox(0x0869, 0x086A)
                {
                    X = 52,
                    Y = 29
                };
            }
            else
            {
                _myCheckbox = new Checkbox(0x0867, 0x0868)
                {
                    X = 52,
                    Y = 29
                };
            }
            AddChildren(_myCheckbox);



            if (HeIsAccepting)
            {
                _hisPic = new GumpPic(266, 160, 0x0869, 0);
            }
            else
            {
                _hisPic = new GumpPic(266, 160, 0x0867, 0);
            }

            AddChildren(_hisPic);
        }

    }
}
