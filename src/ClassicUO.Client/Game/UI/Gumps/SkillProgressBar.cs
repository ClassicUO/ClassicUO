using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;
using System.Collections.Concurrent;

namespace ClassicUO.Game.UI.Gumps
{
    internal class SkillProgressBar : Gump
    {
        private long expireAt = long.MaxValue;
        public SkillProgressBar(int skillIndex) : base(0, 0)
        {
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

        public void SetDuration(long ms)
        {
            expireAt = Time.Ticks + ms;
        }

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
                if(widthPercent > barBounds.Width)
                    widthPercent = barBounds.Width;

                Add(new GumpPic(0, Height - barBounds.Height, 0x0805, 0) { X = (Width / 2) - (barBounds.Width / 2) }); //Background

                if (widthPercent > 0)
                    Add(new GumpPicTiled(0, Height - barBounds.Height, widthPercent, barBounds.Height, 0x0806) { X = (Width / 2) - (barBounds.Width / 2) });//Foreground
            }
        }

        public override void Update()
        {
            base.Update();

            if (Time.Ticks >= expireAt)
            {
                Dispose();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            QueManager.ShowNext();
        }

        public static class QueManager
        {
            private static ConcurrentQueue<SkillProgressBar> skillProgressBars = new ConcurrentQueue<SkillProgressBar>();
            public static SkillProgressBar CurrentProgressBar;


            public static void AddSkill(int skillIndex)
            {
                skillProgressBars.Enqueue(new SkillProgressBar(skillIndex));

                if (CurrentProgressBar == null || CurrentProgressBar.IsDisposed)
                {
                    ShowNext();
                }
            }

            public static void ShowNext()
            {
                if (World.InGame)
                    if (skillProgressBars.TryDequeue(out var skillProgressBar))
                    {
                        CurrentProgressBar = skillProgressBar;
                        skillProgressBar.SetDuration(4000); //Expire in 4 seconds
                        UIManager.Add(skillProgressBar);
                    }
                    else
                    {
                        //Not in game anymore, clear the que
                        Reset();
                    }
            }

            public static void Reset()
            {
                while(skillProgressBars.TryDequeue( out var skillProgressBar))
                    skillProgressBar?.Dispose();
                skillProgressBars = new ConcurrentQueue<SkillProgressBar>();
            }
        }
    }
}
