#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
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

using System;
using System.IO;

using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Gumps
{
    internal class UseSpellButtonGump : AnchorableGump
    {
        private SpellDefinition _spell;

        public UseSpellButtonGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanBeSaved = true;
        }

        public UseSpellButtonGump(SpellDefinition spell) : this()
        {
            Engine.UI.GetControl<UseSpellButtonGump>((uint) spell.ID)?.Dispose();
            _spell = spell;
            BuildGump();
        }

        private void BuildGump()
        {
            LocalSerial = (uint) _spell.ID;

            Add(new GumpPic(0, 0, (ushort) _spell.GumpIconSmallID, 0) {AcceptMouseInput = false});

            WantUpdateSize = true;
            AcceptMouseInput = true;
            AnchorGroupName = "spell";
            GroupMatrixWidth = 44;
            GroupMatrixHeight = 44;
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            if (Engine.Profile.Current.CastSpellsByOneClick && button == MouseButton.Left)
            {
                if (Math.Max(Math.Abs(Mouse.LDroppedOffset.X), Math.Abs(Mouse.LDroppedOffset.Y)) <= 1)
                    GameActions.CastSpell(_spell.ID);
            }
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            if (!Engine.Profile.Current.CastSpellsByOneClick && button == MouseButton.Left)
                GameActions.CastSpell(_spell.ID);

            return true;
        }

        public override void Save(BinaryWriter writer)
        {
            base.Save(writer);
            writer.Write(0); //version - 4
            writer.Write(_spell.ID); // 4
        }

        public override void Restore(BinaryReader reader)
        {
            base.Restore(reader);
            int version = reader.ReadInt32();
            int id;

            if (version > 0)
            {
                string name = reader.ReadUTF8String(version);
                id = reader.ReadInt32();
                int gumpID = reader.ReadInt32();
                int smallGumpID = reader.ReadInt32();
                int reagsCount = reader.ReadInt32();

                Reagents[] reagents = new Reagents[reagsCount];

                for (int i = 0; i < reagsCount; i++)
                    reagents[i] = (Reagents) reader.ReadInt32();

                int manaCost = reader.ReadInt32();
                int minSkill = reader.ReadInt32();
                string powerWord = reader.ReadUTF8String(reader.ReadInt32());
                int tithingCost = reader.ReadInt32();
            }
            else
                id = reader.ReadInt32();

            _spell = SpellDefinition.FullIndexGetSpell(id);
            BuildGump();
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}