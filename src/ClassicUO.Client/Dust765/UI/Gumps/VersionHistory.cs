using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Renderer;
using ClassicUO.Utility.Platforms;
using Microsoft.Xna.Framework;

namespace ClassicUO.Dust765.UI.Gumps
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
                "/c[yellow]v3.0.2/cd",
                "",
                "/c[yellow]Features/cd",
                "- Add flag to override gargoyle fly state",
                "- Glowing Weapons: restricted to weapons only",
                "- Highlight Friends and Guild members",
                "- Highlight Last Target when stunned",
                "- Highlight Last Target when mortalled (yellow hits)",
                "- Show active spell icon on cursor",
                "- Swing line for ranged weapons",
                "- Sync position on attack (rubberband correction)",
                "- Disarm line indicator",
                "- Custom gargoyle walk animation while flying",
                "- Status bars (HP/Mana/Stamina) in custom window title bar (UOS/Orion style)",
                "- Last Target gump in Dust options panel",
                "- Fast rotation support",
                "- New crash log with extended info",
                "- Frame rendering improvements",
                "- Custom house rendering adjustments",
                "",
                "/c[yellow]Fixes/cd",
                "- Fixed options not applying correctly in Dust menu",
                "- Removed party chat display overhead",
                "- Fixed UCC self-cast not working",
                "- Fixed RazorEnhanced plugin crash (exception guard on hotkey handler)",
                "- Fixed OnCasting gump not triggering correctly",
                "- Fixed Bandage gump target tracking",
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
