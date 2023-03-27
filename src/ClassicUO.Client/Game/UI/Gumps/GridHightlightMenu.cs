using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using static ClassicUO.Game.UI.Gumps.OptionsGump;

namespace ClassicUO.Game.UI.Gumps
{
    internal class GridHightlightMenu : Gump
    {
        private const int WIDTH = 350, HEIGHT = 600;
        private AlphaBlendControl background;
        private SettingsSection highlightSection;

        public GridHightlightMenu() : base(0, 0)
        {
            #region SET VARS
            Width = WIDTH;
            Height = HEIGHT;
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            #endregion

            BuildGump();
        }

        private void BuildGump()
        {
            {
                background = new AlphaBlendControl(0.85f);
                background.Width = WIDTH;
                background.Height = HEIGHT;
                Add(background);
            }//Background
            int y = 0;
            {
                SettingsSection section = new SettingsSection("Grid highlighting settings", WIDTH);
                section.Add(new Label("You can add object properties that you would like the grid to be highlighted for here.", true, 0xffff, WIDTH));

                NiceButton _;
                section.Add(_ = new NiceButton(0, 0, 40, 20, ButtonAction.Activate, "Add") { IsSelectable = false });
                _.MouseUp += (s, e) =>
                {
                    if (e.Button == Input.MouseButtonType.Left)
                    {
                        highlightSection?.Add(NewAreaSection());
                    }
                };

                Add(section);
                y = section.Y + section.Height;
            }//Top section

            highlightSection = new SettingsSection("", WIDTH) { Y = y };

            Add(highlightSection);
        }

        private Area NewAreaSection()
        {
            Area area = new Area();
            area.Width = WIDTH - 30;
            area.Height = 150;
            int y = 0;
            area.Add(new NiceButton(0, y, 120, 20, ButtonAction.Activate, "Add property"));

            ModernColorPicker.HueDisplay hueDisplay;
            area.Add(hueDisplay = new ModernColorPicker.HueDisplay(0, null, true) { X = 150, Y = y });
            hueDisplay.SetTooltip("Select grid highlight hue");

            y += 20;

            area.Add(new Label("Property name", true, 0xffff, 120) { X = 0, Y = y });
            area.Add(new Label("Min value", true, 0xffff, 120) { X = 200, Y = y });

            y += 20;

            area.Add(new InputField(0x0BB8, 0xFF, 0xFFFF, true, 150, 20) { Y = y });

            return area;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            batcher.DrawRectangle(
                SolidColorTextureCache.GetTexture(Color.LightGray),
                x - 1, y - 1,
                WIDTH + 2, HEIGHT + 2,
                new Vector3(0, 0, 1)
                );

            return true;
        }
    }
}
