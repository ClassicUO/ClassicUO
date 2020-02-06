﻿#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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
using System.Xml;

using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
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
            CanCloseWithRightClick = true;
        }

        public UseAbilityButtonGump(AbilityDefinition def, bool primary) : this()
        {
            _isPrimary = primary;
            UIManager.GetGump<UseAbilityButtonGump>(2000 + (uint) def.Index)?.Dispose();
            _definition = def;
            BuildGump();
        }

        public override GUMP_TYPE GumpType => GUMP_TYPE.GT_ABILITYBUTTON;

        private void BuildGump()
        {
            LocalSerial = 2000 + (uint) _definition.Index;

            _button = new GumpPic(0, 0, _definition.Icon, 0)
            {
                AcceptMouseInput = false
            };
            Add(_button);

            SetTooltip();

            WantUpdateSize = true;
            AcceptMouseInput = true;
            AnchorGroupName = "spell";
            GroupMatrixWidth = 44;
            GroupMatrixHeight = 44;
        }

        private void SetTooltip()
        {
            SetTooltip(ClilocLoader.Instance.GetString(1028838 + (byte) (((byte) (_isPrimary ? World.Player.PrimaryAbility : World.Player.SecondaryAbility) & 0x7F) - 1)), 80);
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
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

                SetTooltip();
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

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            writer.WriteAttributeString("id", _definition.Index.ToString());
            writer.WriteAttributeString("name", _definition.Name);
            writer.WriteAttributeString("graphic", _definition.Icon.ToString());
            writer.WriteAttributeString("isprimary", _isPrimary.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            _definition = new AbilityDefinition(int.Parse(xml.GetAttribute("id")),
                                                xml.GetAttribute("name"),
                                                ushort.Parse(xml.GetAttribute("graphic")));
            _isPrimary = bool.Parse(xml.GetAttribute("isprimary"));
            BuildGump();
        }
    }
}