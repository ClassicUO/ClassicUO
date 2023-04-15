using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System.Threading.Tasks;

namespace ClassicUO.Game.UI.Gumps
{
    internal class CustomToolTip : Gump
    {
        private readonly Item item;
        private readonly Control hoverReference;
        private readonly string prepend;
        private readonly string append;
        private RenderedText text;
        private readonly byte font = 1;
        private readonly ushort hue = 0xFFFF;

        public CustomToolTip(Item item, int x, int y, Control hoverReference, string prepend = "", string append = "") : base(0, 0)
        {
            this.item = item;
            this.hoverReference = hoverReference;
            this.prepend = prepend;
            this.append = append;
            X = x;
            Y = y;
            Width = 175;
            if (ProfileManager.CurrentProfile != null)
            {
                font = ProfileManager.CurrentProfile.TooltipFont;
                hue = ProfileManager.CurrentProfile.TooltipTextHue;
            }
            BuildGump();
        }

        private void BuildGump()
        {
            LoadOPLData(0);
        }

        private void LoadOPLData(int attempt)
        {
            if (attempt > 4 || IsDisposed)
                return;
            if (item == null)
            {
                Dispose();
                return;
            }

            Task.Factory.StartNew(() =>
            {
                if (World.OPL.TryGetNameAndData(item.Serial, out string name, out string data))
                {
                    text = RenderedText.Create
                        (
                            FormatTooltip(name, data),
                            maxWidth: Width,
                            font: font,
                            isunicode: true,
                            style: FontStyle.BlackBorder,
                            cell: 5,
                            isHTML: true,
                            align: ProfileManager.CurrentProfile.LeftAlignToolTips ? TEXT_ALIGN_TYPE.TS_LEFT : TEXT_ALIGN_TYPE.TS_CENTER,
                            recalculateWidthByInfo: true,
                            hue: hue
                        );
                }
                else
                {
                    World.OPL.Contains(item.Serial);
                    Task.Delay(1000);
                    LoadOPLData(attempt++);
                }
            });
        }

        private string FormatTooltip(string name, string data)
        {
            string text =
                prepend +
                "<basefont color=\"yellow\">" +
                name +
                "<br><basefont color=\"#FFFFFFFF\">" +
                data +
                append;

            return text;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);
            if (IsDisposed)
                return false;
            if (!hoverReference.MouseIsOver)
            {
                Dispose();
                return false;
            }
            if (text == null) //Waiting for opl data to load the text tooltip
                return true;

            float zoom = 1;
            float alpha = 0.7f;

            if (ProfileManager.CurrentProfile != null)
            {
                alpha = ProfileManager.CurrentProfile.TooltipBackgroundOpacity / 100f;
                if (float.IsNaN(alpha))
                {
                    alpha = 0f;
                }
                zoom = ProfileManager.CurrentProfile.TooltipDisplayZoom / 100f;
            }

            Vector3 hue_vec = ShaderHueTranslator.GetHueVector(0, false, alpha);

            batcher.Draw
            (
                SolidColorTextureCache.GetTexture(Color.Black),
                new Rectangle
                (
                    x - 4,
                    y - 2,
                    (int)(text.Width + 8 * zoom),
                    (int)(text.Height + 8 * zoom)
                ),
                hue_vec
            );


            batcher.DrawRectangle
            (
                SolidColorTextureCache.GetTexture(Color.Gray),
                x - 4,
                y - 2,
                (int)(text.Width + 8 * zoom),
                (int)(text.Height + 8 * zoom),
                hue_vec
            );

            batcher.Draw
            (
                text.Texture,
                new Rectangle
                (
                    x + 3,
                    y + 3,
                    (int)(text.Texture.Width * zoom),
                    (int)(text.Texture.Height * zoom)
                ),
                null,
                Vector3.UnitZ
            );
            return true;
        }
    }
}
