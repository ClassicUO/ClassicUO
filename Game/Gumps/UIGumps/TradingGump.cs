using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class TradingGump : Gump
    {
        private Checkbox _myCheckbox;
        private GumpPic _hisPic;

        private bool _imAccepting, _heIsAccepting;

        public TradingGump(Serial local, string name) : base(local, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;

            AddChildren(new GumpPic(0,0, 0x0866, 0));
            AddChildren(new Label(World.Player.Name, false, 0x0386, font: 1)
                            { X = 84, Y = 40 });

            int fontWidth = Fonts.GetWidthASCII(1, name);

            AddChildren(new Label(name, false, 0x0386, font: 1)
            {X = fontWidth, Y = 170 });
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

        public List<Item> Items { get; } = new List<Item>();

 

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
