using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.Game.UI.Gumps
{
    class NameOverHeadHandlerGump : Gump
    {
        public NameOverHeadHandlerGump() : base(0, 0)
        {
            CanMove = false;
            AcceptMouseInput = true;

            X = 100;
            Y = 100;
            WantUpdateSize = false;

            ControlInfo.Layer = UILayer.Over;

            RadioButton all, mobiles, items;
            AlphaBlendControl alpha;
            Add(alpha = new AlphaBlendControl());

            Add(all = new RadioButton(0, 0x00D0, 0x00D1, "All", color: 0xFFFF)
            {
                IsChecked = NameOverHeadManager.TypeAllowed == NameOverheadTypeAllowed.All
            });
            Add(mobiles = new RadioButton(0, 0x00D0, 0x00D1, "Mobiles only", color: 0xFFFF)
            {
                Y = all.Y + all.Height,
                IsChecked = NameOverHeadManager.TypeAllowed == NameOverheadTypeAllowed.Mobiles
            });
            Add(items = new RadioButton(0, 0x00D0, 0x00D1, "Items only", color: 0xFFFF)
            {
                Y = mobiles.Y + mobiles.Height,
                IsChecked = NameOverHeadManager.TypeAllowed == NameOverheadTypeAllowed.Items
            });

            alpha.Width = Math.Max(items.Width, Math.Max(all.Width, mobiles.Width));
            alpha.Height = all.Height + mobiles.Height + items.Height;

            Width = alpha.Width;
            Height = alpha.Height;

            all.ValueChanged += (sender, e) => { if (all.IsChecked) NameOverHeadManager.TypeAllowed = NameOverheadTypeAllowed.All; };
            mobiles.ValueChanged += (sender, e) => { if (mobiles.IsChecked) NameOverHeadManager.TypeAllowed = NameOverheadTypeAllowed.Mobiles; };
            items.ValueChanged += (sender, e) => { if (items.IsChecked) NameOverHeadManager.TypeAllowed = NameOverheadTypeAllowed.Items; };
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (!Input.Keyboard.Ctrl || !Input.Keyboard.Shift)
                Dispose();
        }


    }
}
