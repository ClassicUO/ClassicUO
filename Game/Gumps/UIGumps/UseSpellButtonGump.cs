using ClassicUO.Game.Data;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Input;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class UseSpellButtonGump : Gump
    {
        private readonly GumpPic _button;
        private readonly SpellDefinition _spell;

        public UseSpellButtonGump(SpellDefinition spell) : base((uint) spell.ID, 0)
        {
            while (UIManager.GetByLocalSerial<UseSpellButtonGump>((uint) spell.ID) != null) UIManager.GetByLocalSerial<UseSpellButtonGump>((uint) spell.ID).Dispose();
            _spell = spell;
            CanMove = true;
            AcceptMouseInput = true;

            _button = new GumpPic(0, 0, (ushort) spell.GumpIconSmallID, 0)
            {
                AcceptMouseInput = true
            };
            _button.MouseDoubleClick += ButtonOnMouseDoubleClick;
            AddChildren(_button);
        }

        private void ButtonOnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButton.Left)
                GameActions.CastSpell(_spell.ID);
        }

        public override void Dispose()
        {
            _button.MouseDoubleClick -= ButtonOnMouseDoubleClick;
            base.Dispose();
        }
    }
}