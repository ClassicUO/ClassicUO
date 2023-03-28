using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using static ClassicUO.Game.UI.Gumps.OptionsGump;

namespace ClassicUO.Game.UI.Gumps
{
    internal class GridHightlightMenu : Gump
    {
        private const int WIDTH = 350, HEIGHT = 500;
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
            X = 100;
            Y = 100;
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
                        highlightSection?.Add(NewAreaSection(ProfileManager.CurrentProfile.GridHighlight_Name.Count));
                    }
                };

                Add(section);
                y = section.Y + section.Height;
            }//Top section

            highlightSection = new SettingsSection("", WIDTH) { Y = y };

            for (int i = 0; i < ProfileManager.CurrentProfile.GridHighlight_Name.Count; i++)
            {
                highlightSection.Add(NewAreaSection(i));
            }

            Add(highlightSection);
        }

        private Area NewAreaSection(int keyLoc)
        {
            GridHighlightData data = GridHighlightData.GetGridHighlightData(keyLoc);
            Area area = new Area();
            area.Width = WIDTH - 30;
            area.Height = 150;
            int y = 0;

            NiceButton _button;
            area.Add(_button = new NiceButton(WIDTH - 170, y, 120, 20, ButtonAction.Activate, "Open property menu") { IsSelectable = false });
            _button.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    UIManager.GetGump<GridHightlightProperties>()?.Dispose();
                    UIManager.Add(new GridHightlightProperties(keyLoc, 100, 100));
                }
            };

            ModernColorPicker.HueDisplay hueDisplay;
            area.Add(hueDisplay = new ModernColorPicker.HueDisplay(data.Hue, null, true) { X = 150, Y = y });
            hueDisplay.SetTooltip("Select grid highlight hue");

            InputField _name;
            area.Add(_name = new InputField(0x0BB8, 0xFF, 0xFFFF, true, 140, 20) { X = 0, Y = y });
            _name.SetText(data.Name);

            y += 20;

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

        private class GridHighlightData
        {

            public readonly string Name;
            public readonly ushort Hue;
            public readonly List<string> Properties;
            public readonly List<int> PropMinVal;

            private GridHighlightData(int keyLoc)
            {
                if(ProfileManager.CurrentProfile.GridHighlight_Name.Count > keyLoc) //Key exists?
                {
                    Name = ProfileManager.CurrentProfile.GridHighlight_Name[keyLoc];
                    Hue = ProfileManager.CurrentProfile.GridHighlight_Hue[keyLoc];
                    Properties = ProfileManager.CurrentProfile.GridHighlight_PropNames[keyLoc];
                    PropMinVal = ProfileManager.CurrentProfile.GridHighlight_PropMinVal[keyLoc];
                } else
                {
                    Name = "Name";
                    ProfileManager.CurrentProfile.GridHighlight_Name.Add(Name);
                    Hue = 1;
                    ProfileManager.CurrentProfile.GridHighlight_Hue.Add(Hue);
                    Properties = new List<string>();
                    ProfileManager.CurrentProfile.GridHighlight_PropNames.Add(Properties);
                    PropMinVal = new List<int>();
                    ProfileManager.CurrentProfile.GridHighlight_PropMinVal.Add(PropMinVal);
                }
            }

            public static GridHighlightData GetGridHighlightData(int keyLoc)
            {
                return new GridHighlightData(keyLoc);
            }
        }

        private class GridHightlightProperties : Gump
        {
            private int lastYitem = 0;
            private ScrollArea scrollArea;
            GridHighlightData data;

            public GridHightlightProperties(int keyLoc, int x, int y) : base(0, 0)
            {
                data = GridHighlightData.GetGridHighlightData(keyLoc);
                X = x;
                Y = y;
                Width = WIDTH;
                Height = HEIGHT;
                CanMove = true;
                AcceptMouseInput = true;
                CanCloseWithRightClick = true;

                Add(new AlphaBlendControl(0.85f) { Width = WIDTH, Height = HEIGHT });

                NiceButton _addPropertyButton, _save;
                Add(_addPropertyButton = new NiceButton(0, 0, 120, 20, ButtonAction.Activate, "Add property") { IsSelectable = false });
                _addPropertyButton.MouseUp += (s, e) =>
                {
                    if (e.Button == Input.MouseButtonType.Left)
                    {
                        AddProperty();

                        lastYitem += 20;
                    }
                };

                Add(_save = new NiceButton(WIDTH - 60, 0, 60, 20, ButtonAction.Activate, "Save") { IsSelectable = false });
                _save.MouseUp += (o, e) => {
                    if (e.Button == Input.MouseButtonType.Left)
                    {
                        Add(new FadingLabel(5, "Saved", true, 0xff) { X = _save.X, Y = _save.Y + 20 });
                    }
                };

                Add(scrollArea = new ScrollArea(0, 20, WIDTH, HEIGHT - 20, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways });

                scrollArea.Add(new Label("Property name", true, 0xffff, 120) { X = 0, Y = lastYitem });
                scrollArea.Add(new Label("Min value", true, 0xffff, 120) { X = 200, Y = lastYitem });

                lastYitem += 20;



            }

            private void AddProperty()
            {
                scrollArea.Add(new InputField(0x0BB8, 0xFF, 0xFFFF, true, 150, 20) { Y = lastYitem });
                scrollArea.Add(new InputField(0x0BB8, 0xFF, 0xFFFF, true, 100, 20) { X = 200, Y = lastYitem, NumbersOnly = true });
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

        private class FadingLabel : Label
        {
            private readonly int tickSpeed;
            private int c = 0;
            public FadingLabel(int tickSpeed, string text, bool isunicode, ushort hue, int maxwidth = 0, byte font = 255, FontStyle style = FontStyle.None, TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT, bool ishtml = false) : base(text, isunicode, hue, maxwidth, font, style, align, ishtml)
            {
                this.tickSpeed = tickSpeed;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if(c>=tickSpeed)
                    Alpha -= 0.01f;
                if (Alpha <= 0f)
                    Dispose();
                c++;
                return base.Draw(batcher, x, y);
            }
        }
    }
}
