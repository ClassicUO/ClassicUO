using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Network;

namespace ClassicUO.Game.UI.Gumps
{
    class RacialAbilityButton : Gump
    {
        public RacialAbilityButton(ushort graphic) : this()
        {
            Graphic = graphic;
            BuildGump();
        }

        public RacialAbilityButton() : base(0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;
        }

        public ushort Graphic;

        public override GUMP_TYPE GumpType => GUMP_TYPE.GT_RACIALBUTTON;


        private void BuildGump()
        {
            var pic = new GumpPic(0, 0, Graphic, 0);
            Add(pic);
            pic.SetTooltip(UOFileManager.Cliloc.GetString(1112198 + (Graphic - 0x5DD0)), 200);
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            if (Graphic == 0x5DDA && World.Player.Race == RaceType.GARGOYLE)
            {
                NetClient.Socket.Send(new PToggleGargoyleFlying());

                return true;
            }
            return base.OnMouseDoubleClick(x, y, button);
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            writer.WriteAttributeString("graphic", Graphic.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);
            Graphic = ushort.Parse(xml.GetAttribute("graphic"));
            BuildGump();
        }

    }
}
