using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Game.Data;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Input;
using ClassicUO.IO;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class UseSpellButtonGump : Gump
    {
        private SpellDefinition _spell;
        private GumpPic _button;


        public UseSpellButtonGump(SpellDefinition spell) : base((uint)spell.ID, 0)
        {
            while (UIManager.GetByLocalSerial<UseSpellButtonGump>((uint)spell.ID) != null)
            {
                UIManager.GetByLocalSerial<UseSpellButtonGump>((uint)spell.ID).Dispose();
            }

            _spell = spell;
            CanMove = true;
            AcceptMouseInput = true;

            _button = new GumpPic(0,0 , (ushort)spell.GumpIconSmallID, 0)
            {
                AcceptMouseInput = true,
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
