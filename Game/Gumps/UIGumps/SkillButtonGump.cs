using ClassicUO.Game.Data;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class SkillButtonGump : Gump
    {
        private readonly ResizePic _buttonBackgroundNormal;
        private readonly ResizePic _buttonBackgroundOver;
        private readonly Skill _skill;

        public SkillButtonGump(Skill skill, int x, int y) : base(0, 0)
        {
            X = x;
            Y = y;
            CanMove = true;
            AcceptMouseInput = false;
            CanCloseWithRightClick = true;
            _skill = skill;

            AddChildren(_buttonBackgroundNormal = new ResizePic(0x24B8)
            {
                Width = 120, Height = 40
            });

            AddChildren(_buttonBackgroundOver = new ResizePic(0x24EA)
            {
                Width = 120, Height = 40
            });

            AddChildren(new HoveredLabel(skill.Name, true, 0, 1151, 105, 1, FontStyle.None, TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 7,
                Y = 5,
                Height = 35,
                AcceptMouseInput = true,
                CanMove = true
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
    }
}