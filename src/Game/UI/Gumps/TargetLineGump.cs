using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.Game.UI.Gumps
{
    class TargetLineGump : Gump
    {
        private readonly GumpPic _background;
        private readonly GumpPicWithWidth _hp;
        private Mobile _mobile;

        public static TargetLineGump TTargetLineGump { get; set; }

        public TargetLineGump() : base(0, 0)
        {
            CanMove = false;
            AcceptMouseInput = false;
            WantUpdateSize = false;

            AddChildren(_background = new GumpPic(0,0, 0x1068, 0));
            AddChildren(_hp = new GumpPicWithWidth(0, 0, 0x1069, 0, 1));
        }

        public Mobile Mobile
        {
            get => _mobile;
            set
            {
                _mobile = value;
                LocalSerial = _mobile.Serial;
            }
        }

        public Hue HpHue
        {
            get => _hp.Hue;
            set
            {
                if (_hp.Hue != value)
                {
                    _hp.Hue = value;
                }
            }
        }

        public Hue BackgroudHue
        {
            get => _background.Hue;
            set
            {
                if (_background.Hue != value)
                {
                    _background.Hue = value;
                }
            }
        }

        public void SetMobile(Mobile mob)
            => Mobile = mob;

        public override void Update(double totalMS, double frameMS)
        {
            if (IsDisposed)
                return;

            if (Mobile == null || Mobile.IsDisposed)
            {
                IsVisible = false;
                return;
            }

            if (!IsVisible)
                IsVisible = true;

            base.Update(totalMS, frameMS);

            int per = Mobile.HitsMax;

            if (per > 0)
            {
                per = (Mobile.Hits * 100) / per;

                if (per > 100)
                    per = 100;

                if (per < 1)
                    per = 0;
                else
                    per = (34 * per) / 100;
            }

            _hp.Percent = per;
        }

        

    }
}
