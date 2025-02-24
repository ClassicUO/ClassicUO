// SPDX-License-Identifier: BSD-2-Clause

using System.IO;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    internal class SkillButtonGump : AnchorableGump
    {
        private Skill _skill;

        public SkillButtonGump(World world, Skill skill, int x, int y) : this(world)
        {
            X = x;
            Y = y;
            _skill = skill;

            BuildGump();
        }

        public SkillButtonGump(World world) : base(world, 0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            WantUpdateSize = false;
            WidthMultiplier = 2;
            HeightMultiplier = 1;
            GroupMatrixWidth = 44;
            GroupMatrixHeight = 44;
            AnchorType = ANCHOR_TYPE.SPELL;
        }


        public override GumpType GumpType => GumpType.SkillButton;

        public int SkillID => _skill.Index;

        private void BuildGump()
        {
            Width = 88;
            Height = 44;

            Add
            (
                new ResizePic(0x24B8)
                {
                    Width = Width,
                    Height = Height,
                    AcceptMouseInput = true,
                    CanMove = true
                }
            );

            Label label;

            Add
            (
                label = new Label
                (
                    _skill.Name,
                    true,
                    0,
                    Width - 8,
                    1,
                    FontStyle.None,
                    TEXT_ALIGN_TYPE.TS_CENTER
                )
                {
                    X = 4,
                    Y = 0,
                    Width = Width - 8,
                    AcceptMouseInput = true,
                    CanMove = true
                }
            );

            label.Y = (Height >> 1) - (label.Height >> 1);
        }


        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);

            if (ProfileManager.CurrentProfile.CastSpellsByOneClick && button == MouseButtonType.Left && !Keyboard.Alt)
            {
                GameActions.UseSkill(_skill.Index);
            }
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (!ProfileManager.CurrentProfile.CastSpellsByOneClick && button == MouseButtonType.Left && !Keyboard.Alt)
            {
                GameActions.UseSkill(_skill.Index);

                return true;
            }

            return false;
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
            {
                Dispose();
            }
        }
    }
}