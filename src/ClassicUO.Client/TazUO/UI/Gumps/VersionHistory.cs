using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Renderer;
using ClassicUO.Utility.Platforms;
using Microsoft.Xna.Framework;

namespace ClassicUO.TazUO.UI.Gumps
{
    internal class VersionHistory : Gump
    {
        private const string WIKI_URL = "https://github.com/dust765/ClassicUO/wiki";
        private const string DISCORD_URL = "https://discord.gg/RG9kAkmW";
        private const int WIDTH = 480;
        private const int HEIGHT = 520;
        private const ushort HUE_TITLE = 0x0022;
        private const ushort HUE_TEXT = 0xFFFF;
        private const ushort HUE_CONTENT = 0xFFFF;

        public VersionHistory() : base(0, 0)
        {
            X = (Client.Game.Window.ClientBounds.Width - WIDTH) >> 1;
            Y = (Client.Game.Window.ClientBounds.Height - HEIGHT) >> 1;
            Width = WIDTH;
            Height = HEIGHT;
            CanCloseWithRightClick = true;
            CanMove = true;

            Add(new AlphaBlendControl(0.95f)
            {
                X = 1,
                Y = 1,
                Width = WIDTH - 2,
                Height = HEIGHT - 2,
                Hue = 999
            });

            int startY = 25;
            Add(new Label("Dust765", true, HUE_TITLE, WIDTH - 30, 1, FontStyle.None)
            {
                X = 20,
                Y = startY
            });
            startY += 22;

            Add(new Label($"Version: {CUOEnviroment.Version}", false, HUE_TEXT, WIDTH - 30, 1, FontStyle.None)
            {
                X = 20,
                Y = startY
            });
            startY += 30;

            int scrollX = 20;
            int scrollY = startY;
            int scrollW = WIDTH - 40;
            int scrollH = HEIGHT - startY - 55;

            Add(new BorderControl(scrollX - 3, scrollY - 3, scrollW + 6, scrollH + 6, 3));
            Add(new AlphaBlendControl(1f) { X = scrollX, Y = scrollY, Width = scrollW, Height = scrollH });

            ScrollArea scroll = new ScrollArea(scrollX, scrollY, scrollW, scrollH, true)
            {
                ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways
            };

            int contentY = 0;
            string[] sections =
            {
                "/c[yellow]v2.0.11/cd",
                "",
                "/c[yellow]Macros/cd",
                "- OpenJournal2: second journal",
                "- OpenBackpack2: second backpack (normal view)",
                "",
                "/c[yellow]Options (Dust)/cd",
                "- Show target range indicator (checkbox left, label right); gray text removed",
                "- Dust765 panel layout aligned with Options",
                "",
                "/c[yellow]Version History/cd",
                "- Options style layout; Dust765 red, version white; Discord link updated",
                "- Text colors via HTML/ConvertUoColorCodesToHtml",
                "",
                "/c[yellow]PvP/Infobar/cd",
                "- 2-min timer for Grey/Criminal; Murderer excluded from timer",
                "",
                "/c[yellow]Character Selection/cd",
                "- Outline/shader removed on hover; alpha 0.6 when not selected; 1.0 when selected/hovering",
                "",
                "/c[yellow]Build/cd",
                "- Release: WinExe; Debug: Exe",
                "- loginbg.png, logodust.png in Data\\Client",
                "- Removed characterbg.png and serverbg.png",
                "",
                "/c[yellow]Login/ServerSelection/cd",
                "- Logo limited size (600x220 login, 450x140 server), centered",
                "",
                "/c[yellow]Corrections/cd",
                "- Math in LoginGump; warnings (Argument.GetHashCode, CustomGumpPic, ImageButton)"
            };

            foreach (string line in sections)
            {
                if (string.IsNullOrEmpty(line))
                {
                    contentY += 8;
                    continue;
                }
                string displayText = HtmlTextHelper.ConvertUoColorCodesToHtml(line).Trim();
                Label lbl = new Label(displayText, true, HUE_CONTENT, scroll.Width - 30, 1, FontStyle.None, align: TEXT_ALIGN_TYPE.TS_LEFT, ishtml: true) { Y = contentY };
                scroll.Add(lbl);
                contentY += lbl.Height + 2;
            }

            Add(scroll);

            int btnY = HEIGHT - 40;
            int btnWidth = 160;

            NiceButton wikiBtn = new NiceButton(20, btnY, btnWidth, 25, ButtonAction.Activate, "Dust765 Wiki") { IsSelectable = false };
            wikiBtn.MouseUp += (s, e) => PlatformHelper.LaunchBrowser(WIKI_URL);
            Add(wikiBtn);

            NiceButton discordBtn = new NiceButton(WIDTH - btnWidth - 20, btnY, btnWidth, 25, ButtonAction.Activate, "Discord") { IsSelectable = false };
            discordBtn.MouseUp += (s, e) => PlatformHelper.LaunchBrowser(DISCORD_URL);
            Add(discordBtn);
        }
    }
}
