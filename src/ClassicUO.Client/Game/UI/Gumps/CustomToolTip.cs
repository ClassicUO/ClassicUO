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
        private Control hoverReference;
        private readonly string prepend;
        private readonly string append;
        private readonly Item compareTo;
        private TextBox text;
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
            text = new TextBox(
                "Loading item data...",
                ProfileManager.CurrentProfile.SelectedToolTipFont,
                ProfileManager.CurrentProfile.SelectedToolTipFontSize,
                150,
                (int)hue,
                align: ProfileManager.CurrentProfile.LeftAlignToolTips ? FontStashSharp.RichText.TextHorizontalAlignment.Left : FontStashSharp.RichText.TextHorizontalAlignment.Center
                );

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
                    if (SerialHelper.IsItem(item.Serial))
                    {
                        finalString = Managers.ToolTipOverrideData.ProcessTooltipText(item.Serial, compareTo == null ? uint.MinValue : compareTo.Serial);
                        if (finalString == null)
                            finalString = FormatTooltip(name, data);
                    }

                    text?.Dispose();
                    text = new TextBox(
                        TextBox.ConvertHtmlToFontStashSharpCommand(finalString).Trim(),
                        ProfileManager.CurrentProfile.SelectedToolTipFont,
                        ProfileManager.CurrentProfile.SelectedToolTipFontSize,
                        600,
                        (int)hue,
                        align: ProfileManager.CurrentProfile.LeftAlignToolTips ? FontStashSharp.RichText.TextHorizontalAlignment.Left : FontStashSharp.RichText.TextHorizontalAlignment.Center
                        );

                    if (text.MeasuredSize.X + 10 < 600)
                        text.Width = text.MeasuredSize.X + 10;

                    Height = text.Height;
                    Width = text.Width;
                    OnOPLLoaded?.Invoke();
                }
            }
            else
            {
                Task.Factory.StartNew(() =>
                {
                    Task.Delay(1500).Wait();
                    LoadOPLData(attempt++);
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
