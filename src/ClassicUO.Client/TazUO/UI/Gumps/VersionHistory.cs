using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;

namespace ClassicUO.TazUO.UI.Gumps
{
    internal class VersionHistory : ClassicUO.Game.UI.Gumps.Gump
    {
        private static string[] updateTexts = {
            "/c[white][2.0.9]/cd\n" +
                "- Welcome\n" +
                "- Auto Avoid Obstacules\n"  +
                "- Defende Party\n"
        };

        public VersionHistory() : base(0, 0)
        {
            X = 300;
            Y = 200;
            Width = 400;
            Height = 500;
            CanCloseWithRightClick = true;
            CanMove = true;

            ClassicUO.Game.UI.Gumps.BorderControl bc = new ClassicUO.Game.UI.Gumps.BorderControl(0, 0, Width, Height, 36);
            bc.T_Left = 39925;
            bc.H_Border = 39926;
            bc.T_Right = 39927;
            bc.V_Border = 39928;
            bc.V_Right_Border = 39930;
            bc.B_Left = 39931;
            bc.B_Right = 39933;
            bc.H_Bottom_Border = 39932;

            Add(new GumpPicTiled(39929) { X = bc.BorderSize, Y = bc.BorderSize, Width = Width - (bc.BorderSize * 2), Height = Height - (bc.BorderSize * 2) });

            Add(bc);

            UOLabel _;
            Add(_ = new UOLabel(Language.Instance.TazuoVersionHistory, 1, UOLabelHue.Text, Assets.TEXT_ALIGN_TYPE.TS_CENTER, Width) { Y = 10 });
            Add(_ = new UOLabel(Language.Instance.CurrentVersion + CUOEnviroment.Version.ToString(), 1, UOLabelHue.Accent, Assets.TEXT_ALIGN_TYPE.TS_CENTER, Width) { Y = _.Y + _.Height + 5 });

            ScrollArea scroll = new ScrollArea(10, _.Y + _.Height, Width - 20, Height - (_.Y + _.Height) - 20, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways };

            Add(new AlphaBlendControl(0.45f) { Width = scroll.Width, Height = scroll.Height, X = scroll.X, Y = scroll.Y });

            int y = 0;
            foreach (string s in updateTexts)
            {
                scroll.Add(_ = new UOLabel(s, 1, UOLabelHue.Accent, Assets.TEXT_ALIGN_TYPE.TS_LEFT, scroll.Width - scroll.ScrollBarWidth()) { Y = y });
                y += _.Height + 10;
            }

            Add(scroll);

            HitBox _hit;
            Add(_ = new UOLabel(Language.Instance.TazUOWiki, 1, UOLabelHue.Accent, Assets.TEXT_ALIGN_TYPE.TS_LEFT, 200) { X = 25, Y = Height - 20 });
            Add(_hit = new HitBox(_.X, _.Y, _.Width, _.Height));
            _hit.MouseUp += (s, e) =>
            {
                Utility.Platforms.PlatformHelper.LaunchBrowser("https://github.com/bittiez/ClassicUO/wiki");
            };

            Add(_ = new UOLabel(Language.Instance.TazUOWiki, 1, UOLabelHue.Accent, Assets.TEXT_ALIGN_TYPE.TS_LEFT, 200) { X = 280, Y = Height - 20 });
            Add(_hit = new HitBox(_.X, _.Y, _.Width, _.Height));
            _hit.MouseUp += (s, e) =>
            {
                Utility.Platforms.PlatformHelper.LaunchBrowser("https://discord.gg/SqwtB5g95H");
            };
        }
    }
}
