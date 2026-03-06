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
        private const string WIKI_URL = "https://github.com/dust765/ClassicUO/wiki/Dust765-%E2%80%90-ClassicUO-Features";
        private const string DISCORD_URL = "https://discord.gg/kjzFEEyD";

        public VersionHistory() : base(0, 0)
        {
            X = (Client.Game.Window.ClientBounds.Width - 420) >> 1;
            Y = (Client.Game.Window.ClientBounds.Height - 500) >> 1;
            Width = 420;
            Height = 500;
            CanCloseWithRightClick = true;
            CanMove = true;

            Add(new AlphaBlendControl(0.85f) { Width = Width, Height = Height });

            Add(new ResizePic(0x0A28) { Width = Width, Height = Height });

            int yPos = 15;

            Label title = new Label("Dust765", false, 0x0481, Width - 30, 2, FontStyle.None)
            {
                X = 15,
                Y = yPos
            };
            Add(title);
            yPos += title.Height + 5;

            Label version = new Label($"Version: {CUOEnviroment.Version}", false, 0x0386, Width - 30, 1, FontStyle.None)
            {
                X = 15,
                Y = yPos
            };
            Add(version);
            yPos += version.Height + 15;

            ScrollArea scroll = new ScrollArea(15, yPos, Width - 30, Height - yPos - 60, true)
            {
                ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways
            };

            int scrollY = 0;

            string[] sections = {
                "/c[yellow]v2.0.9/cd",
                "- Welcome",
                "- Auto Avoid Obstacles",
                "- Defend Party",
                "",
                "/c[yellow]Features/cd",
                "- Dust765 visual helpers and combat tools",
                "- Legion Script Studio",
                "- UOScript support",
                "- Grid containers",
                "- Cooldown bars",
                "- Custom tooltip override",
                "- Name overhead filters",
                "- Save settings as default"
            };

            foreach (string line in sections)
            {
                if (string.IsNullOrEmpty(line))
                {
                    scrollY += 8;
                    continue;
                }

                Label lbl = new Label(line, true, 0xFFFF, scroll.Width - 20, 1, FontStyle.None)
                {
                    Y = scrollY
                };
                scroll.Add(lbl);
                scrollY += lbl.Height + 2;
            }

            Add(scroll);

            int btnY = Height - 40;
            int btnWidth = 160;

            NiceButton wikiBtn = new NiceButton(15, btnY, btnWidth, 25, ButtonAction.Activate, "Dust765 Wiki")
            {
                IsSelectable = false
            };
            wikiBtn.MouseUp += (s, e) => PlatformHelper.LaunchBrowser(WIKI_URL);
            Add(wikiBtn);

            NiceButton discordBtn = new NiceButton(Width - btnWidth - 15, btnY, btnWidth, 25, ButtonAction.Activate, "Discord")
            {
                IsSelectable = false
            };
            discordBtn.MouseUp += (s, e) => PlatformHelper.LaunchBrowser(DISCORD_URL);
            Add(discordBtn);
        }
    }
}
