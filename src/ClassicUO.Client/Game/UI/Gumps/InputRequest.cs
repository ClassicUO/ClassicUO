using ClassicUO.Assets;
using ClassicUO.Game.UI.Controls;
using System;
using static ClassicUO.Game.UI.Gumps.OptionsGump;

namespace ClassicUO.Game.UI.Gumps
{
    internal class InputRequest : Gump
    {
        public InputRequest(string message, string buttonText, string button2Text, Action<Result, string> result, string defaultInputValue = "") : base(0, 0)
        {
            Width = 400;
            Height = 0;

            AlphaBlendControl bg;
            Add(bg = new AlphaBlendControl(0.75f));

            Control _;
            Add(_ = new TextBox(message, TrueTypeLoader.EMBEDDED_FONT, 25, Width, Microsoft.Xna.Framework.Color.White, FontStashSharp.RichText.TextHorizontalAlignment.Center, false));
            Height += _.Height;

            InputField input = new InputField
            (
                0x0BB8,
                0xFF,
                0xFFF,
                true,
                Width,
                30
            )
            { X = 0, Y = _.Y + _.Height + 15 };
            Height += input.Height;
            input.SetText(defaultInputValue);
            Add(input);

            NiceButton button1, button2;
            Add(button1 = new NiceButton(0, input.Y + input.Height + 20, Width / 2, 40, ButtonAction.Activate, buttonText) { IsSelectable = false });
            button1.MouseUp += (s, e) =>
            {
                result.Invoke(Result.BUTTON1, input.Text); Dispose();
            };

            Add(button2 = new NiceButton(Width / 2, input.Y + input.Height + 20, Width / 2, 40, ButtonAction.Activate, button2Text) { IsSelectable = false });
            button2.MouseUp += (s, e) =>
            {
                result.Invoke(Result.BUTTON2, input.Text); Dispose();
            };
            Height += button1.Height + button2.Height;

            bg.Width = Width;
            bg.Height = Height;
        }

        public enum Result
        {
            BUTTON1,
            BUTTON2
        }
    }
}
