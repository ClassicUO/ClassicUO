using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    internal class TitleBarStatsBarsGump : Gump
    {
        private const int BAR_WIDTH = 140;
        private const int BAR_HEIGHT = 10;
        private const int BAR_GAP = 2;
        private const int PADDING = 2;
        private const int LABEL_WIDTH = 28;

        private static readonly Color ColorLifeFill = new Color(180, 40, 40);
        private static readonly Color ColorLifeBg = new Color(60, 20, 20);
        private static readonly Color ColorManaFill = new Color(40, 60, 180);
        private static readonly Color ColorManaBg = new Color(20, 20, 60);
        private static readonly Color ColorStaminaFill = new Color(200, 120, 40);
        private static readonly Color ColorStaminaBg = new Color(60, 40, 20);

        private static readonly Vector3 HueNone = ShaderHueTranslator.GetHueVector(0);

        private readonly Label _labelHp;
        private readonly Label _labelMp;
        private readonly Label _labelSp;

        public TitleBarStatsBarsGump() : base(0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;
            Width = LABEL_WIDTH + PADDING * 2 + BAR_WIDTH + PADDING;
            Height = PADDING * 2 + (BAR_HEIGHT + BAR_GAP) * 3 - BAR_GAP;
            X = 10;
            Y = 42;

            _labelHp = new Label("HP", true, 0x21, 0, 1, FontStyle.BlackBorder) { X = PADDING, Y = PADDING };
            _labelMp = new Label("MP", true, 0x59, 0, 1, FontStyle.BlackBorder) { X = PADDING, Y = PADDING + BAR_HEIGHT + BAR_GAP };
            _labelSp = new Label("SP", true, 0x2A, 0, 1, FontStyle.BlackBorder) { X = PADDING, Y = PADDING + (BAR_HEIGHT + BAR_GAP) * 2 };
            Add(_labelHp);
            Add(_labelMp);
            Add(_labelSp);
        }

        public static void UpdateVisibility()
        {
            if (ProfileManager.CurrentProfile == null)
                return;

            bool shouldShow = false;

            TitleBarStatsBarsGump existing = UIManager.GetGump<TitleBarStatsBarsGump>();

            if (shouldShow && existing == null)
                UIManager.Add(new TitleBarStatsBarsGump());
            else if (!shouldShow && existing != null)
                existing.Dispose();
        }

        public static void Create()
        {
            if (World.Player == null)
                return;
            UpdateVisibility();
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed || World.Player == null)
                return false;

            ushort hits = World.Player.Hits;
            ushort hitsMax = World.Player.HitsMax;
            ushort mana = World.Player.Mana;
            ushort manaMax = World.Player.ManaMax;
            ushort stamina = World.Player.Stamina;
            ushort staminaMax = World.Player.StaminaMax;

            int ox = x + PADDING + LABEL_WIDTH;
            int oy = y + PADDING;

            DrawBar(batcher, ox, oy, BAR_WIDTH, BAR_HEIGHT, hits, hitsMax, ColorLifeBg, TitleBarStatsManager.GetHealthColor(hits, hitsMax));
            oy += BAR_HEIGHT + BAR_GAP;
            DrawBar(batcher, ox, oy, BAR_WIDTH, BAR_HEIGHT, mana, manaMax, ColorManaBg, ColorManaFill);
            oy += BAR_HEIGHT + BAR_GAP;
            DrawBar(batcher, ox, oy, BAR_WIDTH, BAR_HEIGHT, stamina, staminaMax, ColorStaminaBg, ColorStaminaFill);

            batcher.DrawRectangle(
                SolidColorTextureCache.GetTexture(Color.Gray),
                x, y, Width, Height,
                HueNone);

            return base.Draw(batcher, x, y);
        }

        private static void DrawBar(UltimaBatcher2D batcher, int x, int y, int w, int h,
            ushort current, ushort max, Color colorBg, Color colorFill)
        {
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
    }
}
