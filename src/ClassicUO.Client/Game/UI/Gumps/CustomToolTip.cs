using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class CustomToolTip : Gump
    {
        private readonly Item item;
        private readonly Control hoverReference;
        private readonly string prepend;
        private readonly string append;
        private RenderedText text;

        public CustomToolTip(Item item, int x, int y, Control hoverReference, string prepend = "", string append = "") : base(0, 0)
        {
            this.item = item;
            this.hoverReference = hoverReference;
            this.prepend = prepend;
            this.append = append;
            X = x;
            Y = y;
            BuildGump();
        }

        private void BuildGump()
        {
            if (item != null)
            {
                if (World.OPL.TryGetNameAndData(item.Serial, out string name, out string data))
                {
                    byte font = 1;
                    ushort hue = 0xFFFF;
                    

                    if (ProfileManager.CurrentProfile != null)
                    {
                        font = ProfileManager.CurrentProfile.TooltipFont;
                        hue = ProfileManager.CurrentProfile.TooltipTextHue;
                    }


                    text = RenderedText.Create
                        (
                            FormatTooltip(name,data),
                            font: font,
                            isunicode: true,
                            style: FontStyle.BlackBorder,
                            cell: 5,
                            isHTML: true,
                            align: TEXT_ALIGN_TYPE.TS_CENTER,
                            recalculateWidthByInfo: true,
                            hue: hue
                        );
                    text.MaxWidth = 600;
                    Width = text.Width;
                    return;
                }
            }
            Dispose();
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
