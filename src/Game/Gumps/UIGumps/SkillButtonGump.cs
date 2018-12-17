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
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class SkillButtonGump : Gump
    {
        private ResizePic _buttonBackgroundNormal;
        private ResizePic _buttonBackgroundOver;
        private Skill _skill;

        public SkillButtonGump(Skill skill, int x, int y) : this()
        {
            X = x;
            Y = y;
            _skill = skill;

            BuildGump();
            LocalSerial = (uint)(World.Player.Serial + _skill.Index + 1);
        }

        public SkillButtonGump() : base(0 ,0)
        {
            CanMove = true;
            AcceptMouseInput = false;
            CanCloseWithRightClick = true;
            CanBeSaved = true;
        }

        private void BuildGump()
        {
            AddChildren(_buttonBackgroundNormal = new ResizePic(0x24B8)
            {
                Width = 120,
                Height = 40
            });

            AddChildren(_buttonBackgroundOver = new ResizePic(0x24EA)
            {
                Width = 120,
                Height = 40
            });

            AddChildren(new HoveredLabel(_skill.Name, true, 0, 1151, 105, 1, FontStyle.None, TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 7,
                Y = 5,
                Height = 35,
                AcceptMouseInput = true,
            });
        }

        protected override void OnMouseOver(int x, int y)
        {
            _buttonBackgroundNormal.IsVisible = false;
            _buttonBackgroundOver.IsVisible = true;
        }

        protected override void OnMouseExit(int x, int y)
        {
            _buttonBackgroundNormal.IsVisible = true;
            _buttonBackgroundOver.IsVisible = false;
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left) GameActions.UseSkill(_skill.Index);
        }

        public override void Save(BinaryWriter writer)
        {
            base.Save(writer);
            writer.Write(LocalSerial);
            writer.Write(_skill.Index);
        }

        public override void Restore(BinaryReader reader)
        {
            base.Restore(reader);


            LocalSerial = reader.ReadUInt32();
            int skillIndex = reader.ReadInt32();

            _skill = World.Player.Skills[skillIndex];

            BuildGump();
        }
    }
}