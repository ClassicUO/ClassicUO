using ClassicUO.Game.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class SkillButtonGump : Gump
    {
        private Skill _skill;
        private ResizePic _buttonBackgroundNormal;
        private ResizePic _buttonBackgroundOver;
        public SkillButtonGump(Skill skill, int x, int y)
            : base(0, 0)
        {
            X = x;
            Y = y;
            CanMove = true;
            AcceptMouseInput = false;
            CanCloseWithRightClick = true;
            _skill = skill;
            AddChildren(_buttonBackgroundNormal = new ResizePic(0x24B8) { Width = 120, Height = 40 });
            AddChildren(_buttonBackgroundOver = new ResizePic(0x24EA) { Width = 120, Height = 40 });
            AddChildren(new HoveredLabel(skill.Name, true, 0, 1151, 105, 1, FontStyle.None, TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 7,
                Y = 5,
                Height = 35,
                AcceptMouseInput = true,
                CanMove = true
                
            });

        }

        protected override void OnMouseEnter(int x, int y)
        {
            _buttonBackgroundNormal.IsVisible = false;
            _buttonBackgroundOver.IsVisible = true;
            

        }

        protected override void OnMouseLeft(int x, int y)
        {
            _buttonBackgroundNormal.IsVisible = true;
            _buttonBackgroundOver.IsVisible = false;
            
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                GameActions.UseSkill(_skill.Index);
            }
           
        }

    }
}
