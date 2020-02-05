#region license
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

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    internal class SkillButtonGump : AnchorableGump
    {
        private Skill _skill;

        public SkillButtonGump(Skill skill, int x, int y) : this()
        {
            X = x;
            Y = y;
            _skill = skill;

            BuildGump();
            LocalSerial = (uint) (World.Player.Serial + _skill.Index + 1);
        }

        public SkillButtonGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            WantUpdateSize = false;
            AnchorGroupName = "spell";
            WidthMultiplier = 2;
            HeightMultiplier = 1;
            GroupMatrixWidth = 44;
            GroupMatrixHeight = 44;
        }


        public override GUMP_TYPE GumpType => GUMP_TYPE.GT_SKILLBUTTON;

        private void BuildGump()
        {
            Width = 88;
            Height = 44;

            Add(new ResizePic(0x24B8)
            {
                Width = Width,
                Height = Height,
                AcceptMouseInput = true,
                CanMove = true
            });

            Label label;

            Add(label = new Label(_skill.Name, true, 0, Width - 8, 1, FontStyle.None, TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 4,
                Y = 0,
                Width = Width - 8,
                AcceptMouseInput = true,
                CanMove = true
            });
            label.Y = (Height >> 1) - (label.Height >> 1);
        }


        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (ProfileManager.Current.CastSpellsByOneClick && button == MouseButtonType.Left && !Keyboard.Alt)
                GameActions.UseSkill(_skill.Index);
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (!ProfileManager.Current.CastSpellsByOneClick && button == MouseButtonType.Left && !Keyboard.Alt)
                GameActions.UseSkill(_skill.Index);

            return true;
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

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            writer.WriteAttributeString("id", _skill.Index.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);
            int index = int.Parse(xml.GetAttribute("id"));

            if (index >= 0 && index < World.Player.Skills.Length)
            {
                _skill = World.Player.Skills[index];
                BuildGump();
            }
            else 
                Dispose();
        }
    }
}