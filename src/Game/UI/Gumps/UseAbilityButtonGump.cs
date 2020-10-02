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

using System.Xml;
using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    internal class UseAbilityButtonGump : AnchorableGump
    {
        private GumpPic _button;

        public UseAbilityButtonGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
        }

        public UseAbilityButtonGump(int index, bool primary) : this()
        {
            IsPrimary = primary;
            Index = index;
            BuildGump();
        }

        public override GumpType GumpType => GumpType.AbilityButton;

        public int Index { get; }
        public bool IsPrimary { get; private set; }

        private void BuildGump()
        {
            Clear();

            int index = ((byte) World.Player.Abilities[IsPrimary ? 0 : 1] & 0x7F) - 1;

            ref readonly AbilityDefinition def = ref AbilityData.Abilities[index];

            _button = new GumpPic(0, 0, def.Icon, 0)
            {
                AcceptMouseInput = false
            };

            Add(_button);

            SetTooltip(ClilocLoader.Instance.GetString(1028838 + index), 80);

            WantUpdateSize = true;
            AcceptMouseInput = true;
            GroupMatrixWidth = 44;
            GroupMatrixHeight = 44;
            AnchorType = ANCHOR_TYPE.SPELL;
        }


        protected override void UpdateContents()
        {
            BuildGump();
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                if (IsPrimary)
                {
                    GameActions.UsePrimaryAbility();
                }
                else
                {
                    GameActions.UseSecondaryAbility();
                }

                return true;
            }

            return false;
        }


        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            byte index = (byte) World.Player.Abilities[IsPrimary ? 0 : 1];

            if ((index & 0x80) != 0)
            {
                _button.Hue = 38;
            }
            else if (_button.Hue != 0)
            {
                _button.Hue = 0;
            }


            return base.Draw(batcher, x, y);
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            writer.WriteAttributeString("isprimary", IsPrimary.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);
            IsPrimary = bool.Parse(xml.GetAttribute("isprimary"));
            BuildGump();
        }
    }
}