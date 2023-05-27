using ClassicUO.Assets;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class VersionHistory : Gump
    {
        private static string[] updateTexts = {
            "/c[white][3.5.0]/cd\n" +
                "- Bug fix for EA egg event\n" +
                "- Added tooltip header formatting(change item name color)\n" +
                "- Damage hues fixed",

            "/c[white][3.4.0]/cd\n" +
                "- Added this version history gump\n" +
                "- Added /c[green]-version/cd command to open this gump\n" +
                "- Made advanced skill gump more compact, height resizable and can grab skill buttons by dragging skills\n" +
                "- Added tooltip override feature (See wiki for more details)\n" +
                "- Better rain\n" +
                "- Fixed tooltips in vendor search\n" +
                "- Fixed modern shop gump displaying wrong items at animal trainers\n" +
                "- Added hide border and timestamps to journal options\n" +
                "- Added hide border option for grid containers",

            "/c[white][3.3.0]/cd\n" +
                "-Last attack automatic healthbar gump will remember its position\n" +
                "-Nameplate gump now has a search option (Ctrl + Shift)\n"+
                "-Fix number(gold) entry for trading gump\n"+
                "-Fixed red warmode outline for custom health gumps\n"+
                "-Graphics in info bar -> See wiki\n"+
                "-Tooltip background colors adjustable\n"+
                "-Tmap and SOS right click menu moved to menu icon on gump\n"+
                "- \"/c[green]-skill /c[white]skillname/cd\" command added to use skills\n",
            "\n\n/c[white]For further history please visit our discord."
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
            Add(_ = new TextBox("TazUO Version History", TrueTypeLoader.EMBEDDED_FONT, 30, Width, Color.White, FontStashSharp.RichText.TextHorizontalAlignment.Center, false) { Y = 10 });
            Add(_ = new TextBox("Current Version: " + CUOEnviroment.Version.ToString(), TrueTypeLoader.EMBEDDED_FONT, 20, Width, Color.Orange, FontStashSharp.RichText.TextHorizontalAlignment.Center, false) { Y = _.Y + _.Height + 5 });

            ScrollArea scroll = new ScrollArea(10, _.Y + _.Height, Width - 20, Height - (_.Y + _.Height) - 20, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways };

            Add(new AlphaBlendControl(0.45f) { Width = scroll.Width, Height = scroll.Height, X = scroll.X, Y = scroll.Y });

            int y = 0;
            foreach (string s in updateTexts)
            {
                scroll.Add(_ = new TextBox(s, TrueTypeLoader.EMBEDDED_FONT, 15, scroll.Width - scroll.ScrollBarWidth(), Color.Orange, FontStashSharp.RichText.TextHorizontalAlignment.Left, false) { Y = y });
                y += _.Height + 10;
            }

            Add(scroll);


            HitBox _hit;
            Add(_ = new TextBox("TazUO Wiki", TrueTypeLoader.EMBEDDED_FONT, 15, 200, Color.Orange, strokeEffect: false) { X = 25, Y = Height - 20 });
            Add(_hit = new HitBox(_.X, _.Y, _.MeasuredSize.X, _.MeasuredSize.Y));
            _hit.MouseUp += (s, e) =>
            {
                Utility.Platforms.PlatformHelper.LaunchBrowser("https://github.com/bittiez/ClassicUO/wiki");
            };

            Add(_ = new TextBox("TazUO Discord", TrueTypeLoader.EMBEDDED_FONT, 15, 200, Color.Orange, strokeEffect: false) { X = 280, Y = Height - 20 });
            Add(_hit = new HitBox(_.X, _.Y, _.MeasuredSize.X, _.MeasuredSize.Y));
            _hit.MouseUp += (s, e) =>
            {
                Utility.Platforms.PlatformHelper.LaunchBrowser("https://discord.gg/SqwtB5g95H");
            };
        }
    }
}
