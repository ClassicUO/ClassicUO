#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
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
            while (Engine.UI.GetByLocalSerial<UseSpellButtonGump>((uint) spell.ID) != null) Engine.UI.GetByLocalSerial<UseSpellButtonGump>((uint) spell.ID).Dispose();
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