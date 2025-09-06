using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class VersionHistory : Gump
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

            BorderControl bc = new BorderControl(0, 0, Width, Height, 36);
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

            TextBox _;
            Add(_ = new TextBox(Language.Instance.TazuoVersionHistory, TrueTypeLoader.EMBEDDED_FONT, 30, Width, Color.White, FontStashSharp.RichText.TextHorizontalAlignment.Center, false) { Y = 10 });
            Add(_ = new TextBox(Language.Instance.CurrentVersion + CUOEnviroment.Version.ToString(), TrueTypeLoader.EMBEDDED_FONT, 20, Width, Color.DarkRed, FontStashSharp.RichText.TextHorizontalAlignment.Center, false) { Y = _.Y + _.Height + 5 });

            ScrollArea scroll = new ScrollArea(10, _.Y + _.Height, Width - 20, Height - (_.Y + _.Height) - 20, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways };

            Add(new AlphaBlendControl(0.45f) { Width = scroll.Width, Height = scroll.Height, X = scroll.X, Y = scroll.Y });

            int y = 0;
            foreach (string s in updateTexts)
            {
                scroll.Add(_ = new TextBox(s, TrueTypeLoader.EMBEDDED_FONT, 15, scroll.Width - scroll.ScrollBarWidth(), Color.DarkRed, FontStashSharp.RichText.TextHorizontalAlignment.Left, false) { Y = y });
                y += _.Height + 10;
            }

            Add(scroll);


            HitBox _hit;
            Add(_ = new TextBox(Language.Instance.TazUOWiki, TrueTypeLoader.EMBEDDED_FONT, 15, 200, Color.DarkRed, strokeEffect: false) { X = 25, Y = Height - 20 });
            Add(_hit = new HitBox(_.X, _.Y, _.MeasuredSize.X, _.MeasuredSize.Y));
            _hit.MouseUp += (s, e) =>
            {
                Utility.Platforms.PlatformHelper.LaunchBrowser("https://github.com/bittiez/ClassicUO/wiki");
            };

            Add(_ = new TextBox(Language.Instance.TazUOWiki, TrueTypeLoader.EMBEDDED_FONT, 15, 200, Color.DarkRed, strokeEffect: false) { X = 280, Y = Height - 20 });
            Add(_hit = new HitBox(_.X, _.Y, _.MeasuredSize.X, _.MeasuredSize.Y));
            _hit.MouseUp += (s, e) =>
            {
                Utility.Platforms.PlatformHelper.LaunchBrowser("https://discord.gg/SqwtB5g95H");
            };
        }
    }
}
