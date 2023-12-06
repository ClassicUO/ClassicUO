using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClassicUO.Game.UI.Gumps
{
    internal class SkillProgressBar : Gump
    {
        public SkillProgressBar(int skillIndex) : base((uint)skillIndex + 764544, 0)
        {
            UIManager.GetGump<SkillProgressBar>((uint)skillIndex + 764544)?.Dispose();

            Height = 40;
            Width = 300;

            if (ProfileManager.CurrentProfile.SkillProgressBarPosition == Point.Zero)
            {
                WorldViewportGump vp = UIManager.GetGump<WorldViewportGump>();

                Y = vp.Location.Y + 80;
                X = (vp.Location.X + (vp.Width / 2)) - (Width / 2);
            }
            else
            {
                Location = ProfileManager.CurrentProfile.SkillProgressBarPosition;
            }

            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            CanMove = true;

            this.skillIndex = skillIndex;

            BuildGump();
        }

        private int skillIndex { get; }

        protected override void OnMove(int x, int y)
        {
            base.OnMove(x, y);
            ProfileManager.CurrentProfile.SkillProgressBarPosition = Location;
        }

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

                tb.X = (Width / 2) - (tb.MeasuredSize.X / 2);

                Rectangle barBounds = Client.Game.Gumps.GetGump(0x0805).UV;

                int widthPercent = (int)(barBounds.Width * (s.Value / s.Cap));
                Add(new GumpPic(0, Height - barBounds.Height, 0x0805, 0) { X = (Width / 2) - (barBounds.Width / 2) }); //Background

                if (widthPercent > 0)
                    Add(new GumpPicTiled(0, Height - barBounds.Height, widthPercent, barBounds.Height, 0x0806) { X = (Width / 2) - (barBounds.Width / 2) });//Foreground
            }
        }

        public static class QueManager
        {
            private static TimeSpan duration = TimeSpan.FromSeconds(5);

            private static ConcurrentQueue<SkillProgressBar> skillProgressBars = new ConcurrentQueue<SkillProgressBar>();

            private static bool threadRunning = false;

            public static void AddSkill(int skillIndex)
            {
                skillProgressBars.Enqueue(new SkillProgressBar(skillIndex));
                StartProcessing();
            }

            private static void StartProcessing()
            {
                if (threadRunning)
                {
                    return;
                }
                threadRunning = true;

                Task.Factory.StartNew(() =>
                {
                    while (skillProgressBars.TryDequeue(out SkillProgressBar bar))
                    {
                        UIManager.Add(bar);
                        Task.Delay(duration).Wait();
                        bar?.Dispose();
                    }
                    threadRunning = false;
                });
            }
        }
    }
}
