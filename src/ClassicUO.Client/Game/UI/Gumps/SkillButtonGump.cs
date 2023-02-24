#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

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
        }

        public SkillButtonGump() : base(0, 0)
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