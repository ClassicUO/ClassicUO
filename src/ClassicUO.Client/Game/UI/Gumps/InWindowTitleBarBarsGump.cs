using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;

namespace ClassicUO.Game.UI.Gumps
{
    internal class InWindowTitleBarBarsGump : Gump
    {
        private const int BAR_HEIGHT = 6;
        private const int BAR_GAP = 1;
        private const int PADDING_H = 8;
        private const int PADDING_V = 2;
        private const int TOTAL_HEIGHT = PADDING_V * 2 + BAR_HEIGHT * 3 + BAR_GAP * 2;
        private const int MAX_BAR_WIDTH = 65; // limit width when drawing bars

        private const ushort HueLife = 0x0021;
        private const ushort HueMana = 0x0059;
        private const ushort HueStamina = 0x002A;

        private static readonly Color ColorLifeFill = new Color(180, 40, 40);
        private static readonly Color ColorLifeBg = new Color(60, 20, 20);
        private static readonly Color ColorManaFill = new Color(40, 60, 180);
        private static readonly Color ColorManaBg = new Color(20, 20, 60);
        private static readonly Color ColorStaminaFill = new Color(200, 120, 40);
        private static readonly Color ColorStaminaBg = new Color(60, 40, 20);
        private static readonly Color ColorStripBg = new Color(30, 30, 30, 240);
        private static readonly Color ColorUosBarFrame = new Color(90, 78, 44, 255);
        private static readonly Color ColorUosBarBackdrop = new Color(12, 12, 12, 235);

        private static readonly Vector3 HueNone = ShaderHueTranslator.GetHueVector(0);

        private static Texture2D _texBarBg;
        private static Texture2D _texBarFill;

        private static void EnsureBarTextures()
        {
            if (_texBarBg != null && _texBarFill != null)
                return;
            string dataPath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client");
            string bgPath = Path.Combine(dataPath, "titlebar_bar_bg.png");
            string fillPath = Path.Combine(dataPath, "titlebar_bar_fill.png");
            if (File.Exists(bgPath))
                _texBarBg = PNGLoader.Instance.GetImageTexture(bgPath);
            if (File.Exists(fillPath))
                _texBarFill = PNGLoader.Instance.GetImageTexture(fillPath);
        }

        public InWindowTitleBarBarsGump() : base(0, 0)
        {
            X = 0;
            Y = 0;
            Width = 400;
            Height = TOTAL_HEIGHT;
            CanMove = false;
            CanCloseWithRightClick = false;
            AcceptMouseInput = false;
            LayerOrder = UILayer.Over;
        }

        public static void UpdateVisibility()
        {
            if (ProfileManager.CurrentProfile == null)
                return;

            // Colored bar strip only for UOS/Orion custom title + ProgressBar mode.
            // CUO native shows stats in the OS window title text instead.
            bool shouldShow = ProfileManager.CurrentProfile.UsesCustomWindowTitleBar()
                              && ProfileManager.CurrentProfile.EnableTitleBarStats
                              && ProfileManager.CurrentProfile.TitleBarStatsMode == TitleBarStatsMode.ProgressBar;

            InWindowTitleBarBarsGump existing = UIManager.GetGump<InWindowTitleBarBarsGump>();

            if (shouldShow && existing == null)
                UIManager.Add(new InWindowTitleBarBarsGump());
            else if (!shouldShow && existing != null)
                existing.Dispose();
        }

        public override void Update()
        {
            base.Update();
            int w = Client.Game.Window.ClientBounds.Width;
            if (w > 0 && Width != w)
                Width = w;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed || World.Player == null)
                return false;

            EnsureBarTextures();

            batcher.Draw(
                SolidColorTextureCache.GetTexture(ColorStripBg),
                new Rectangle(x, y, Width, Height),
                HueNone,
                0f);

            int barWidth = Width - PADDING_H * 2;
            if (barWidth > MAX_BAR_WIDTH)
                barWidth = MAX_BAR_WIDTH;
            int ox = x + PADDING_H;
            int oy = y + PADDING_V;
            bool useUosBarBackdrop = ProfileManager.CurrentProfile?.GetEffectiveWindowTitleStyle() == WindowTitleBarStyle.UOS;

            DrawBar(batcher, ox, oy, barWidth, BAR_HEIGHT, World.Player.Hits, World.Player.HitsMax, ColorLifeBg, TitleBarStatsManager.GetHealthColor(World.Player.Hits, World.Player.HitsMax), ShaderHueTranslator.GetHueVector(HueLife, false, 1f), useUosBarBackdrop);
            oy += BAR_HEIGHT + BAR_GAP;
            DrawBar(batcher, ox, oy, barWidth, BAR_HEIGHT, World.Player.Mana, World.Player.ManaMax, ColorManaBg, ColorManaFill, ShaderHueTranslator.GetHueVector(HueMana, false, 1f), useUosBarBackdrop);
            oy += BAR_HEIGHT + BAR_GAP;
            DrawBar(batcher, ox, oy, barWidth, BAR_HEIGHT, World.Player.Stamina, World.Player.StaminaMax, ColorStaminaBg, ColorStaminaFill, ShaderHueTranslator.GetHueVector(HueStamina, false, 1f), useUosBarBackdrop);

            return true;
        }

        private static void DrawBar(UltimaBatcher2D batcher, int x, int y, int w, int h,
            ushort current, ushort max, Color colorBg, Color colorFill, Vector3 fillHue, bool useUosBarBackdrop)
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

            if (_texBarBg != null && !_texBarBg.IsDisposed)
            {
                batcher.Draw(_texBarBg, new Rectangle(x, y, w, h), null, HueNone);
            }
            else
            {
                batcher.Draw(
                    SolidColorTextureCache.GetTexture(colorBg),
                    new Rectangle(x, y, w, h),
                    HueNone,
                    0f);
            }

            if (max > 0 && current > 0)
            {
                int fillW = (int)((float)current / max * w);
                if (fillW > 0)
                {
                    if (_texBarFill != null && !_texBarFill.IsDisposed)
                    {
                        float pct = (float)current / max;
                        int srcW = (int)(_texBarFill.Width * pct);
                        if (srcW > 0)
                        {
                            var srcRect = new Rectangle(0, 0, srcW, _texBarFill.Height);
                            batcher.Draw(_texBarFill, new Rectangle(x, y, fillW, h), srcRect, fillHue);
                        }
                    }
                    else
                    {
                        batcher.Draw(
                            SolidColorTextureCache.GetTexture(colorFill),
                            new Rectangle(x, y, fillW, h),
                            HueNone,
                            0f);
                    }
                }
            }
        }
    }
}
