using System.Collections.Generic;

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Input;

using Newtonsoft.Json;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class UseSpellButtonGump : Gump
    {
        private GumpPic _button;
        private SpellDefinition _spell;

        public UseSpellButtonGump() : base(0 ,0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanBeSaved = true;
        }

        public UseSpellButtonGump(SpellDefinition spell) : this()
        {
            while (UIManager.GetByLocalSerial<UseSpellButtonGump>((uint) spell.ID) != null) UIManager.GetByLocalSerial<UseSpellButtonGump>((uint) spell.ID).Dispose();
            _spell = spell;
            BuildGump();
        }

        private void BuildGump()
        {
            LocalSerial = (uint)_spell.ID;

            _button = new GumpPic(0, 0, (ushort)_spell.GumpIconSmallID, 0)
            {
                AcceptMouseInput = true
            };
            _button.MouseDoubleClick += ButtonOnMouseDoubleClick;
            AddChildren(_button);

            WantUpdateSize = true;
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            return true;
        }

        private void ButtonOnMouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
        {
            if (e.Button == MouseButton.Left)
                GameActions.CastSpell(_spell.ID);
        }

        public override bool Save(out Dictionary<string, object> data)
        {
            if (base.Save(out data))
            {
                data["spell"] = _spell;
                return true;
            }

            return false;
        }

        public override bool Restore(Dictionary<string, object> data)
        {
            //if (base.Restore(data) && Service.Get<Settings>().GetGumpValue(typeof(UseSpellButtonGump), "spell", out _spell))
            //{
            //    BuildGump();

            //    return true;
            //}

            return false;
        }

        public override void Dispose()
        {
            _button.MouseDoubleClick -= ButtonOnMouseDoubleClick;
            base.Dispose();
        }
    }
}