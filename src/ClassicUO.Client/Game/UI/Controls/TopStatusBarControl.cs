using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;

namespace ClassicUO.Game.UI.Controls
{
    public class TopStatusBarControl : Control
    {
        private const int ROW_HEIGHT = 8;
        private const int BAR_HEIGHT = 8;
        private const int LABEL_BAR_GAP = 2;
        private const int ROW_GAP = 1;
        private const int PADDING_LEFT = 4;
        private const int PADDING_TOP = 2;
        private const int ICON_SIZE = 20;
        private const int TEXT_LEFT = 28;
        public const int TITLEBAR_BUTTONS_WIDTH = 72;
        public const int TITLEBAR_BUTTON_SIZE = 24;

        private static readonly Color ColorStripBg = new Color(35, 35, 40, 250);
        private static readonly Color ColorBtnBg = new Color(60, 60, 65, 250);
        private static readonly Color ColorBtnHover = new Color(80, 80, 90, 250);
        private static readonly Color ColorBtnCloseHover = new Color(180, 50, 50, 250);
        private static readonly Color ColorLifeBg = new Color(25, 45, 25);
        private static readonly Color ColorLifeFill = new Color(50, 180, 50);
        private static readonly Color ColorStaminaBg = new Color(45, 35, 20);
        private static readonly Color ColorStaminaFill = new Color(220, 120, 40);
        private static readonly Color ColorManaBg = new Color(20, 25, 50);
        private static readonly Color ColorManaFill = new Color(60, 80, 220);
        private static readonly Color ColorUosBarFrame = new Color(90, 78, 44, 255);
        private static readonly Color ColorUosBarBackdrop = new Color(12, 12, 12, 235);

        private static readonly Vector3 HueNone = ShaderHueTranslator.GetHueVector(0);


        private RenderedText _titleText;
        private string _lastTitle = "";
        private Texture2D _iconTexture;
        private RenderedText _statsText;
        private string _lastStatsString = "";

        public int HoveredButtonIndex { get; set; } = -1;

        public TopStatusBarControl(int width, int height)
        {
            Width = width;
            Height = height;
            AcceptMouseInput = false;
            CanCloseWithRightClick = false;

            // title will be created/updated in Update() so we can react to connected server
            _titleText = null;
            _lastTitle = string.Empty;

            string iconPath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client", "logodust.png");
            if (File.Exists(iconPath))
                _iconTexture = PNGLoader.Instance.GetImageTexture(iconPath);
        }

        public override void Update()
        {
            base.Update();
            if (Parent != null && Parent.Width > 0 && Width != Parent.Width)
                Width = Parent.Width;

            // recalc title text if server name or version changed
            string serverName = World.ServerName;
            string title = $"Dust765 {CUOEnviroment.Version}";
            if (!string.IsNullOrEmpty(serverName) && serverName != "_")
                title += " | " + serverName;

            // include player name once logged in
            if (World.Player != null && !string.IsNullOrEmpty(World.Player.Name))
                title += " - " + World.Player.Name;

            if (title != _lastTitle)
            {
                _titleText?.Destroy();
                _titleText = RenderedText.Create(title, 0x0034, 1, true, FontStyle.BlackBorder, TEXT_ALIGN_TYPE.TS_LEFT);
                _lastTitle = title;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
                return false;

            batcher.Draw(
                SolidColorTextureCache.GetTexture(ColorStripBg),
                new Rectangle(x, y, Width, Height),
                HueNone,
                0f);

            int cx = x + PADDING_LEFT;
            int cy = y + PADDING_TOP;

            if (_iconTexture != null && !_iconTexture.IsDisposed)
            {
                int iconW = System.Math.Min(ICON_SIZE, _iconTexture.Width);
                int iconH = System.Math.Min(ICON_SIZE, _iconTexture.Height);
                batcher.Draw(_iconTexture, new Rectangle(cx, cy, iconW, iconH), null, HueNone);
            }
            else
            {
                batcher.Draw(
                    SolidColorTextureCache.GetTexture(Color.Gray),
                    new Rectangle(cx, cy, ICON_SIZE, ICON_SIZE),
                    HueNone,
                    0f);
            }

            cx = x + TEXT_LEFT;
            int titleWidth = _titleText?.Width ?? 0;
            if (_titleText != null)
                _titleText.Draw(batcher, cx, cy + (ICON_SIZE - _titleText.Height) / 2, Alpha);

            int statsLeft = cx + titleWidth + 12;
            int statsRight = Width - TITLEBAR_BUTTONS_WIDTH;
            int statsWidth = statsRight - statsLeft;
            bool showStats = ProfileManager.CurrentProfile?.EnableTitleBarStats == true && World.Player != null;

            if (showStats && statsWidth > 60)
            {
                TitleBarStatsMode mode = ProfileManager.CurrentProfile.TitleBarStatsMode;
                if (mode == TitleBarStatsMode.ProgressBar)
                {
                    int barAreaWidth = 65; // fixed width for bars, since they are more compact than text
                    if (barAreaWidth > 40)
                    {
                        bool useUosBarBackdrop = ProfileManager.CurrentProfile?.GetEffectiveWindowTitleStyle() == WindowTitleBarStyle.UOS;
                        int rowY = cy;
                        Mobile player = World.Player;
                        ushort hits = player?.Hits ?? 0;
                        ushort hitsMax = player?.HitsMax ?? 1;
                        ushort mana = player?.Mana ?? 0;
                        ushort manaMax = player?.ManaMax ?? 1;
                        ushort stamina = player?.Stamina ?? 0;
                        ushort staminaMax = player?.StaminaMax ?? 1;

                        // HP, then Mana, then Stamina – keep ordering consistent with other UI elements
                        // HP bar uses dynamic colour based on percent
                        DrawBar(batcher, statsLeft + LABEL_BAR_GAP, rowY, barAreaWidth, BAR_HEIGHT, hits, hitsMax, ColorLifeBg, TitleBarStatsManager.GetHealthColor(hits, hitsMax), useUosBarBackdrop);
                        rowY += ROW_HEIGHT + ROW_GAP;
                        DrawBar(batcher, statsLeft + LABEL_BAR_GAP, rowY, barAreaWidth, BAR_HEIGHT, mana, manaMax, ColorManaBg, ColorManaFill, useUosBarBackdrop);
                        rowY += ROW_HEIGHT + ROW_GAP;
                        DrawBar(batcher, statsLeft + LABEL_BAR_GAP, rowY, barAreaWidth, BAR_HEIGHT, stamina, staminaMax, ColorStaminaBg, ColorStaminaFill, useUosBarBackdrop);
                    }
                }
                else
                {
                    string statsString = BuildStatsString(mode);
                    if (statsString != _lastStatsString)
                    {
                        _statsText?.Destroy();
                        _statsText = RenderedText.Create(statsString, 0x0034, 1, true, FontStyle.BlackBorder, TEXT_ALIGN_TYPE.TS_LEFT);
                        _lastStatsString = statsString;
                    }
                    if (_statsText != null)
                        _statsText.Draw(batcher, statsLeft, cy + (ICON_SIZE - _statsText.Height) / 2, Alpha);
                }
            }
            else if (_lastStatsString != "")
            {
                _statsText?.Destroy();
                _statsText = null;
                _lastStatsString = "";
            }

            int btnLeft = x + Width - TITLEBAR_BUTTONS_WIDTH;
            int btnY = y + (Height - TITLEBAR_BUTTON_SIZE) / 2;
            DrawWindowButton(batcher, btnLeft, btnY, 0, "\u2212");
            DrawWindowButton(batcher, btnLeft + TITLEBAR_BUTTON_SIZE, btnY, 1, "\u25A1");
            DrawWindowButton(batcher, btnLeft + TITLEBAR_BUTTON_SIZE * 2, btnY, 2, "\u00D7");

            return base.Draw(batcher, x, y);
        }

        private static readonly RenderedText _btnMin = RenderedText.Create("\u2212", 0x0034, 1, true, FontStyle.BlackBorder, TEXT_ALIGN_TYPE.TS_CENTER);
        private static readonly RenderedText _btnMax = RenderedText.Create("\u25A1", 0x0034, 1, true, FontStyle.BlackBorder, TEXT_ALIGN_TYPE.TS_CENTER);
        private static readonly RenderedText _btnClose = RenderedText.Create("\u00D7", 0x0034, 1, true, FontStyle.BlackBorder, TEXT_ALIGN_TYPE.TS_CENTER);

        private void DrawWindowButton(UltimaBatcher2D batcher, int x, int y, int buttonIndex, string symbol)
        {
            bool hover = HoveredButtonIndex == buttonIndex;
            Color bg = hover ? (buttonIndex == 2 ? ColorBtnCloseHover : ColorBtnHover) : ColorBtnBg;
            batcher.Draw(
                SolidColorTextureCache.GetTexture(bg),
                new Rectangle(x, y, TITLEBAR_BUTTON_SIZE, TITLEBAR_BUTTON_SIZE),
                HueNone,
                0f);
            RenderedText t = symbol == "\u2212" ? _btnMin : (symbol == "\u25A1" ? _btnMax : _btnClose);
            int tx = x + (TITLEBAR_BUTTON_SIZE - t.Width) / 2;
            int ty = y + (TITLEBAR_BUTTON_SIZE - t.Height) / 2;
            t.Draw(batcher, tx, ty, 1f);
        }

        private static string BuildStatsString(TitleBarStatsMode mode)
        {
            Mobile player = World.Player;
            if (player == null)
                return "";
            if (mode == TitleBarStatsMode.Text)
                return $"HP {player.Hits}/{player.HitsMax} MP {player.Mana}/{player.ManaMax} SP {player.Stamina}/{player.StaminaMax}";
            if (mode == TitleBarStatsMode.Percent)
            {
                int hpPct = player.HitsMax > 0 ? (player.Hits * 100) / player.HitsMax : 100;
                int mpPct = player.ManaMax > 0 ? (player.Mana * 100) / player.ManaMax : 100;
                int spPct = player.StaminaMax > 0 ? (player.Stamina * 100) / player.StaminaMax : 100;
                return $"HP {hpPct}% MP {mpPct}% SP {spPct}%";
            }
            return "";
        }

        private static void DrawBar(UltimaBatcher2D batcher, int x, int y, int w, int h,
            ushort current, ushort max, Color colorBg, Color colorFill, bool useUosBarBackdrop)
        {
            if (useUosBarBackdrop)
            {
                batcher.Draw(
                    SolidColorTextureCache.GetTexture(ColorUosBarFrame),
                    new Rectangle(x - 1, y - 1, w + 2, h + 2),
                    HueNone,
                    0f);

                batcher.Draw(
                    SolidColorTextureCache.GetTexture(ColorUosBarBackdrop),
                    new Rectangle(x, y, w, h),
                    HueNone,
                    0f);
            }

            batcher.Draw(
                SolidColorTextureCache.GetTexture(colorBg),
                new Rectangle(x, y, w, h),
                HueNone,
                0f);

            if (max > 0 && current > 0)
            {
                int fillW = (int)((float)current / max * w);
                if (fillW > 0)
                {
                    batcher.Draw(
                        SolidColorTextureCache.GetTexture(colorFill),
                        new Rectangle(x, y, fillW, h),
                        HueNone,
                        0f);
                }
            }
        }

        public override void Dispose()
        {
            _titleText?.Destroy();
            _statsText?.Destroy();
            base.Dispose();
        }
    }
}
