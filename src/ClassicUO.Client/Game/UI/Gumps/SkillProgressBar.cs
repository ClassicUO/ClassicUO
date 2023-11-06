using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;

namespace ClassicUO.Game.UI.Gumps
{
    internal class SkillProgressBar : Gump
    {
        public SkillProgressBar(int skillIndex) : base((uint)skillIndex + 764544, 0)
        {
            UIManager.GetGump<SkillProgressBar>((uint)skillIndex + 764544)?.Dispose();

            Height = 40;
            Width = 300;

            WorldViewportGump vp = UIManager.GetGump<WorldViewportGump>();

            Y = vp.Location.Y + 80;
            X = (vp.Location.X + (vp.Width / 2)) - (Width / 2);

            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            CanMove = true;

            this.skillIndex = skillIndex;

            BuildGump();
            createdAt = DateTime.Now;
        }

        private int skillIndex { get; }
        private DateTime createdAt { get; }
        private TimeSpan displayDuration = TimeSpan.FromSeconds(10);

        private void BuildGump()
        {
            if (World.Player.Skills.Length > skillIndex)
            {
                Skill s = World.Player.Skills[skillIndex];
                TextBox tb;
                Add(tb = new TextBox(
                    string.Format(ProfileManager.CurrentProfile.SkillBarFormat, s.Name, s.Value, s.Cap),
                    ProfileManager.CurrentProfile.GameWindowSideChatFont,
                    ProfileManager.CurrentProfile.GameWindowSideChatFontSize,
                    null,
                    Color.White));

                tb.X = (Width/2) - (tb.MeasuredSize.X / 2);

                Rectangle barBounds = Client.Game.Gumps.GetGump(0x0805).UV;

                int widthPercent = (int)(barBounds.Width * (s.Value / s.Cap));
                Add(new GumpPic(0, Height - barBounds.Height, 0x0805, 0) { X = (Width / 2) - (barBounds.Width / 2) }); //Background

                if (widthPercent > 0)
                    Add(new GumpPicTiled(0, Height - barBounds.Height, widthPercent, barBounds.Height, 0x0806) { X = (Width / 2) - (barBounds.Width / 2) });//Foreground
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (createdAt + displayDuration < DateTime.Now) { Dispose(); }

            return base.Draw(batcher, x, y);
        }
    }
}
