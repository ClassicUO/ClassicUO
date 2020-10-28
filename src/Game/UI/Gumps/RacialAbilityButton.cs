using System.Xml;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Network;

namespace ClassicUO.Game.UI.Gumps
{
    internal class RacialAbilityButton : Gump
    {
        public RacialAbilityButton(ushort graphic) : this()
        {
            LocalSerial = (uint) (7000 + graphic);

            UIManager.GetGump<RacialAbilityButton>(LocalSerial)?.Dispose();

            Graphic = graphic;
            BuildGump();
        }

        public RacialAbilityButton() : base(0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;
        }

        public override GumpType GumpType => GumpType.RacialButton;
        public ushort Graphic;


        private void BuildGump()
        {
            GumpPic pic = new GumpPic(0, 0, Graphic, 0);
            Add(pic);
            pic.SetTooltip(ClilocLoader.Instance.GetString(1112198 + (Graphic - 0x5DD0)), 200);
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
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