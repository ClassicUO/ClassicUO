using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using FontStyle = ClassicUO.Game.FontStyle;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System.Threading.Tasks;

namespace ClassicUO.Game.UI.Gumps
{
    internal class CustomToolTip : Gump
    {
        private readonly Item item;
        private Control hoverReference;
        private readonly string prepend;
        private readonly string append;
        private readonly Item compareTo;
        private UOLabel text;
        private readonly uint hue = 0xFFFF;

        public event FinishedLoadingEvent OnOPLLoaded;

        public CustomToolTip(Item item, int x, int y, Control hoverReference, string prepend = "", string append = "", Item compareTo = null) : base(0, 0)
        {
            this.item = item;
            this.hoverReference = hoverReference;
            this.prepend = prepend;
            this.append = append;
            this.compareTo = compareTo;
            X = x;
            Y = y;
            if (ProfileManager.CurrentProfile != null)
            {
                hue = ProfileManager.CurrentProfile.TooltipTextHue;
            }
            BuildGump();
        }

        public void RemoveHoverReference()
        {
            hoverReference = null;
        }

        private void BuildGump()
        {
            var profile = ProfileManager.CurrentProfile;
            byte font = profile?.SelectedToolTipFont ?? 1;
            TEXT_ALIGN_TYPE align = profile?.LeftAlignToolTips == true ? TEXT_ALIGN_TYPE.TS_LEFT : TEXT_ALIGN_TYPE.TS_CENTER;
            FontStyle textStyle = (profile != null && profile.TooltipTextHue != 0xFFFF) ? FontStyle.None : FontStyle.BlackBorder;
            text = new UOLabel("Loading item data...", font, (ushort)hue, align, 150, textStyle, true, true);

            Height = text.Height;
            Width = text.Width;

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

            if (World.OPL.Contains(item.Serial))
            {
                if (World.OPL.TryGetNameAndData(item.Serial, out string name, out string data))
                {
                    string finalString = FormatTooltip(name, data);
                    if (SerialHelper.IsItem(item.Serial) && ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.EnableTooltipOverride)
                    {
                        finalString = Managers.ToolTipOverrideData.ProcessTooltipText(item.Serial, compareTo == null ? uint.MinValue : compareTo.Serial);
                        if (finalString == null)
                            finalString = FormatTooltip(name, data);
                    }

                    string displayText = HtmlTextHelper.ConvertUoColorCodesToHtml(finalString ?? string.Empty).Trim();

                    text?.Dispose();
                    var p = ProfileManager.CurrentProfile;
                    byte font = p?.SelectedToolTipFont ?? 1;
                    TEXT_ALIGN_TYPE align = p?.LeftAlignToolTips == true ? TEXT_ALIGN_TYPE.TS_LEFT : TEXT_ALIGN_TYPE.TS_CENTER;
                    FontStyle textStyle = (p != null && p.TooltipTextHue != 0xFFFF) ? FontStyle.None : FontStyle.BlackBorder;
                    text = new UOLabel(displayText, font, (ushort)hue, align, 600, textStyle, true, true);

                    if (text.Width + 10 < 600)
                        text.Width = text.Width + 10;

                    Height = text.Height;
                    Width = text.Width;
                    OnOPLLoaded?.Invoke();
                }
            }
            else
            {
                Task.Run(async () =>
                {
                    await Task.Delay(1500);
                    LoadOPLData(attempt + 1);
                });
            }



        }

        private string FormatTooltip(string name, string data)
        {
            string text =
                prepend +
                "<basefont color=\"yellow\">" +
                name +
                "\n<basefont color=\"#FFFFFF\">" +
                data +
                append;

            return text;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);
            if (IsDisposed)
                return false;
            if (ProfileManager.CurrentProfile != null && !ProfileManager.CurrentProfile.UseTooltip)
            {
                Dispose();
                return false;
            }
            if (hoverReference != null && !hoverReference.MouseIsOver)
            {
                Dispose();
                return false;
            }
            //if (text == null) //Waiting for opl data to load the text tooltip
            //    return true;

            float alpha = 0.7f;

            if (ProfileManager.CurrentProfile != null)
            {
                alpha = ProfileManager.CurrentProfile.TooltipBackgroundOpacity / 100f;
                if (float.IsNaN(alpha))
                {
                    alpha = 0f;
                }
            }

            Vector3 hue_vec = ShaderHueTranslator.GetHueVector(1, false, alpha);

            if (ProfileManager.CurrentProfile != null)
                hue_vec.X = ProfileManager.CurrentProfile.ToolTipBGHue - 1;

            batcher.Draw
            (
                SolidColorTextureCache.GetTexture(Color.White),
                new Rectangle
                (
                    x - 4,
                    y - 2,
                    (int)(Width + 8),
                    (int)(Height + 8)
                ),
                hue_vec
            );

            hue_vec = ShaderHueTranslator.GetHueVector(0, false, alpha);

            batcher.DrawRectangle
            (
                SolidColorTextureCache.GetTexture(Color.Gray),
                x - 4,
                y - 2,
                (int)(Width + 8),
                (int)(Height + 8),
                hue_vec
            );

            text.Draw(batcher, x, y);

            return true;
        }
    }

    public delegate void FinishedLoadingEvent();
}
