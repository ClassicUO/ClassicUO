
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.Game.UI.Gumps
{
    internal sealed class MacroGump : Gump
    {
        public MacroGump(string name) : base(0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;

            AlphaBlendControl macroGumpBackground = new AlphaBlendControl
            {
                Width = 260,
                Height = 200,
                X = ProfileManager.CurrentProfile.GameWindowSize.X / 2 - 125,
                Y = 150,
                Alpha = 0.8f
            };

            Label text = new Label($"Edit macro: {name}", true, 15)
            {
                X = ProfileManager.CurrentProfile.GameWindowSize.X / 2 - 105,
                Y = macroGumpBackground.Y + 2
            };

            Add(macroGumpBackground);
            Add(text);

            Add
            (
                new MacroControl(name, true)
                {
                    X = macroGumpBackground.X + 20,
                    Y = macroGumpBackground.Y + 20,
                }
            );

            SetInScreen();
        }
    }
}
