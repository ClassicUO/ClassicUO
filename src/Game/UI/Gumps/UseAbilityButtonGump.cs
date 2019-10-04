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

using System.IO;

using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Gumps
{
    internal class UseAbilityButtonGump : AnchorableGump
    {
        private GumpPic _button;
        private AbilityDefinition _definition;
        private bool _isPrimary;

        public UseAbilityButtonGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanBeSaved = true;
        }

        public UseAbilityButtonGump(AbilityDefinition def, bool primary) : this()
        {
            _isPrimary = primary;
            Engine.UI.GetGump<UseAbilityButtonGump>((uint) def.Index)?.Dispose();
            _definition = def;
            BuildGump();
        }

        private void BuildGump()
        {
            LocalSerial = (uint) _definition.Index;

            _button = new GumpPic(0, 0, _definition.Icon, 0)
            {
                AcceptMouseInput = false
            };
            Add(_button);

            WantUpdateSize = true;
            AcceptMouseInput = true;
            AnchorGroupName = "spell";
            GroupMatrixWidth = 44;
            GroupMatrixHeight = 44;
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                if (_isPrimary)
                    GameActions.UsePrimaryAbility();
                else
                    GameActions.UseSecondaryAbility();
            }

            return true;
        }


        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (IsDisposed)
                return;

            int index = ((byte) World.Player.Abilities[_isPrimary ? 0 : 1] & 0x7F) - 1;

            ref readonly AbilityDefinition def = ref AbilityData.Abilities[index];

            if (_definition.Index != def.Index)
            {
                _definition = def;
                _button.Graphic = def.Icon;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
                return false;

            byte index = (byte) World.Player.Abilities[_isPrimary ? 0 : 1];

            if ((index & 0x80) != 0)
                _button.Hue = 38;
            else if (_button.Hue != 0)
                _button.Hue = 0;


            return base.Draw(batcher, x, y);
        }

        public override void Save(BinaryWriter writer)
        {
            base.Save(writer);

            writer.Write(_definition.Index);
            writer.Write(_definition.Name.Length);
            writer.WriteUTF8String(_definition.Name);
            writer.Write((int) _definition.Icon);
            writer.Write(_isPrimary);
        }

        public override void Restore(BinaryReader reader)
        {
            base.Restore(reader);

            int index = reader.ReadInt32();
            string name = reader.ReadUTF8String(reader.ReadInt32());
            int graphic = reader.ReadInt32();

            _definition = new AbilityDefinition(index, name, (ushort) graphic);
            _isPrimary = reader.ReadBoolean();

            BuildGump();
        }
    }
}