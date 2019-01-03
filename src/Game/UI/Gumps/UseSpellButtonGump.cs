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

using System.IO;

using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Gumps
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
            Engine.UI.GetByLocalSerial<UseSpellButtonGump>((uint) spell.ID)?.Dispose();
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


        public override void Save(BinaryWriter writer)
        {
            base.Save(writer);
          
            // spell data
            writer.Write(_spell.Name.Length); // 4
            writer.WriteUTF8String(_spell.Name); // variable
            writer.Write(_spell.ID); // 4
            writer.Write(_spell.GumpIconID); // 4
            writer.Write(_spell.GumpIconSmallID); // 4

            writer.Write(_spell.Regs.Length); // 4
            for (int i = 0; i < _spell.Regs.Length; i++)
            {
                writer.Write((int)_spell.Regs[i]); // 4
            }
            writer.Write(_spell.ManaCost); // 4
            writer.Write(_spell.MinSkill); // 4
            writer.Write(_spell.PowerWords.Length); // 4
            writer.WriteUTF8String(_spell.PowerWords); // variable
            writer.Write(_spell.TithingCost); // 4
        }

        public override void Restore(BinaryReader reader)
        {
            base.Restore(reader);

         
            string name = reader.ReadUTF8String(reader.ReadInt32());
            int id = reader.ReadInt32();
            int gumpID = reader.ReadInt32();
            int smallGumpID = reader.ReadInt32();
            int reagsCount = reader.ReadInt32();
            
            Reagents[] reagents = new Reagents[reagsCount];

            for (int i = 0; i < reagsCount; i++)
            {
                reagents[i] = (Reagents) reader.ReadInt32();
            }

            int manaCost = reader.ReadInt32();
            int minSkill = reader.ReadInt32();
            string powerWord = reader.ReadUTF8String(reader.ReadInt32());
            int tithingCost = reader.ReadInt32();

            _spell = new SpellDefinition(name, id, gumpID, smallGumpID, powerWord, manaCost, minSkill, tithingCost, reagents);

            BuildGump();
        }

        public override void Dispose()
        {
            _button.MouseDoubleClick -= ButtonOnMouseDoubleClick;
            base.Dispose();
        }
    }
}