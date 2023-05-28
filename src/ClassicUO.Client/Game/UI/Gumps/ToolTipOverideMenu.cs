using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ClassicUO.Game.UI.Gumps.OptionsGump;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ToolTipOverideMenu : Gump
    {
        private const int WIDTH = 500, HEIGHT = 500;
        private AlphaBlendControl background;
        private SettingsSection highlightSection;
        private ScrollArea highlightSectionScroll;

        public static bool Reopen = false;

        public ToolTipOverideMenu(int x = 200, int y = 200) : base(0, 0)
        {
            #region SET VARS
            Width = WIDTH;
            Height = HEIGHT;
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            X = x;
            Y = y;
            #endregion

            BuildGump();
        }

        private void BuildGump()
        {
            {
                background = new AlphaBlendControl(0.85f) { Hue = 997 };
                background.Width = WIDTH;
                background.Height = HEIGHT;
                Add(background);
            }//Background
            int y = 0;
            {
                SettingsSection section = new SettingsSection("Tooltip override settings", WIDTH);
                section.Add(new Label("You can add tooltip items you would like to override formatting for here.", true, 0xffff, WIDTH));

                NiceButton _;
                section.Add(_ = new NiceButton(0, 0, 40, 20, ButtonAction.Activate, "Add +") { IsSelectable = false, DisplayBorder = true });
                _.MouseUp += (s, e) =>
                {
                    if (e.Button == Input.MouseButtonType.Left)
                    {
                        Area _a;
                        highlightSectionScroll.Add(_a = NewAreaSection(ProfileManager.CurrentProfile.ToolTipOverride_SearchText.Count, y));
                        y += _a.Height + 5;
                    }
                };

                section.AddRight(_ = new NiceButton(0, 0, 50, 20, ButtonAction.Activate, "Export") { IsSelectable = false, DisplayBorder = true });
                _.MouseUp += (s, e) =>
                {
                    if (e.Button == Input.MouseButtonType.Left)
                    {
                        ToolTipOverrideData.ExportOverrideSettings();
                    }
                };

                section.AddRight(_ = new NiceButton(0, 0, 50, 20, ButtonAction.Activate, "Import") { IsSelectable = false, DisplayBorder = true });
                _.MouseUp += (s, e) =>
                {
                    if (e.Button == Input.MouseButtonType.Left)
                    {
                        ToolTipOverrideData.ImportOverrideSettings();
                    }
                };

                section.AddRight(_ = new NiceButton(0, 0, 100, 20, ButtonAction.Activate, "Delete All") { IsSelectable = false, DisplayBorder = true });
                _.SetTooltip("/c[red]This will remove ALL tooltip override settings.\nThis is not reversible.");
                _.MouseUp += (s, e) =>
                {
                    if (e.Button == Input.MouseButtonType.Left)
                    {
                        UIManager.Add(new QuestionGump("Are you sure?", (a) =>
                        {
                            if (a)
                            {
                                ProfileManager.CurrentProfile.ToolTipOverride_SearchText = new List<string>();
                                ProfileManager.CurrentProfile.ToolTipOverride_NewFormat = new List<string>();
                                ProfileManager.CurrentProfile.ToolTipOverride_MinVal1 = new List<int>();
                                ProfileManager.CurrentProfile.ToolTipOverride_MinVal2 = new List<int>();
                                ProfileManager.CurrentProfile.ToolTipOverride_MaxVal1 = new List<int>();
                                ProfileManager.CurrentProfile.ToolTipOverride_MaxVal2 = new List<int>();
                                ProfileManager.CurrentProfile.ToolTipOverride_Layer = new List<byte>();
                                Reopen = true;
                            }
                        }));
                    }
                };

                Add(section);
                y = section.Y + section.Height;
            }//Top section

            highlightSection = new SettingsSection("", WIDTH) { Y = y };
            highlightSection.Add(highlightSectionScroll = new ScrollArea(0, 0, WIDTH - 20, Height - y - 10, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways }); ;

            y = 0;
            for (int i = 0; i < ProfileManager.CurrentProfile.ToolTipOverride_SearchText.Count; i++)
            {
                Area _a;
                highlightSectionScroll.Add(_a = NewAreaSection(i, y));
                y += _a.Height + 5;
            }

            Add(highlightSection);
        }

        private Area NewAreaSection(int keyLoc, int y)
        {
            ToolTipOverrideData data = ToolTipOverrideData.Get(keyLoc);
            Area area = new Area() { Y = y };
            area.Width = WIDTH - 35;
            area.Height = 45;
            area.WantUpdateSize = false;
            area.CanMove = true;
            y = 0;

            NiceButton _del;

            Combobox _itemLater;
            InputField _searchText, _formatText, _min1, _min2, _max1, _max2;
            area.Add(_searchText = new InputField(0x0BB8, 0xFF, 0xFFFF, true, 200, 20) { X = 25, Y = y, AcceptKeyboardInput = true });
            _searchText.SetText(data.SearchText);
            _searchText.TextChanged += (s, e) =>
            {
                Task.Factory.StartNew(() =>
                {
                    var tVal = _searchText.Text;
                    System.Threading.Thread.Sleep(1500);
                    if (_searchText.Text == tVal)
                    {
                        if (String.IsNullOrEmpty(_searchText.Text))
                            return;
                        data.SearchText = _searchText.Text;
                        data.Save();
                        UIManager.Add(new SimpleTimedTextGump("Saved", Microsoft.Xna.Framework.Color.LightGreen, TimeSpan.FromSeconds(1)) { X = _searchText.ScreenCoordinateX, Y = _searchText.ScreenCoordinateY - 20 });
                    }
                });
            };


            area.Add(_formatText = new InputField(0x0BB8, 0xFF, 0xFFFF, true, 230, 20) { X = _searchText.X + _searchText.Width + 5, Y = y, AcceptKeyboardInput = true });
            _formatText.SetText(data.FormattedText);
            _formatText.TextChanged += (s, e) =>
            {
                Task.Factory.StartNew(() =>
                {
                    var tVal = _formatText.Text;
                    System.Threading.Thread.Sleep(1500);
                    if (_formatText.Text == tVal)
                    {
                        data.FormattedText = _formatText.Text;
                        data.Save();
                        UIManager.Add(new SimpleTimedTextGump("Saved", Microsoft.Xna.Framework.Color.LightGreen, TimeSpan.FromSeconds(1)) { X = _formatText.ScreenCoordinateX, Y = _formatText.ScreenCoordinateY - 20 });
                    }
                });
            };

            Label label;
            area.Add(label = new Label("Min/Max first", true, 0xFFFF) { X = 5, Y = y + 20 });
            area.Add(_min1 = new InputField(0x0BB8, 0xFF, 0xFFFF, true, 30, 20) { X = label.X + label.Width + 5, Y = y + 20, AcceptKeyboardInput = true, NumbersOnly = true });
            _min1.SetText(data.Min1.ToString());
            _min1.TextChanged += (s, e) =>
            {
                Task.Factory.StartNew(() =>
                {
                    var tVal = _min1.Text;
                    System.Threading.Thread.Sleep(1500);
                    if (_min1.Text == tVal)
                    {
                        if (int.TryParse(_min1.Text, out int val))
                        {
                            data.Min1 = val;
                            data.Save();
                            UIManager.Add(new SimpleTimedTextGump("Saved", Microsoft.Xna.Framework.Color.LightGreen, TimeSpan.FromSeconds(1)) { X = _min1.ScreenCoordinateX, Y = _min1.ScreenCoordinateY - 20 });
                        }
                    }
                });
            };

            area.Add(_max1 = new InputField(0x0BB8, 0xFF, 0xFFFF, true, 30, 20) { X = _min1.X + _min1.Width + 5, Y = y + 20, AcceptKeyboardInput = true, NumbersOnly = true });
            _max1.SetText(data.Max1.ToString());
            _max1.TextChanged += (s, e) =>
            {
                Task.Factory.StartNew(() =>
                {
                    var tVal = _max1.Text;
                    System.Threading.Thread.Sleep(1500);
                    if (_max1.Text == tVal)
                    {
                        if (int.TryParse(_max1.Text, out int val))
                        {
                            data.Max1 = val;
                            data.Save();
                            UIManager.Add(new SimpleTimedTextGump("Saved", Microsoft.Xna.Framework.Color.LightGreen, TimeSpan.FromSeconds(1)) { X = _max1.ScreenCoordinateX, Y = _max1.ScreenCoordinateY - 20 });
                        }
                    }
                });
            };



            area.Add(label = new Label("Min/Max second", true, 0xFFFF) { X = _max1.X + _max1.Width + 15, Y = y + 20 });
            area.Add(_min2 = new InputField(0x0BB8, 0xFF, 0xFFFF, true, 30, 20) { X = label.X + label.Width + 5, Y = y + 20, AcceptKeyboardInput = true, NumbersOnly = true });
            _min2.SetText(data.Min2.ToString());
            _min2.TextChanged += (s, e) =>
            {
                Task.Factory.StartNew(() =>
                {
                    var tVal = _min2.Text;
                    System.Threading.Thread.Sleep(1500);
                    if (_min2.Text == tVal)
                    {
                        if (int.TryParse(_min2.Text, out int val))
                        {
                            data.Min2 = val;
                            data.Save();
                            UIManager.Add(new SimpleTimedTextGump("Saved", Microsoft.Xna.Framework.Color.LightGreen, TimeSpan.FromSeconds(1)) { X = _min2.ScreenCoordinateX, Y = _min2.ScreenCoordinateY - 20 });
                        }
                    }
                });
            };

            area.Add(_max2 = new InputField(0x0BB8, 0xFF, 0xFFFF, true, 30, 20) { X = _min2.X + _min2.Width + 5, Y = y + 20, AcceptKeyboardInput = true, NumbersOnly = true });
            _max2.SetText(data.Max2.ToString());
            _max2.TextChanged += (s, e) =>
            {
                Task.Factory.StartNew(() =>
                {
                    var tVal = _max2.Text;
                    System.Threading.Thread.Sleep(1500);
                    if (_max2.Text == tVal)
                    {
                        if (int.TryParse(_max2.Text, out int val))
                        {
                            data.Max2 = val;
                            data.Save();
                            UIManager.Add(new SimpleTimedTextGump("Saved", Microsoft.Xna.Framework.Color.LightGreen, TimeSpan.FromSeconds(1)) { X = _max2.ScreenCoordinateX, Y = _max2.ScreenCoordinateY - 20 });
                        }
                    }
                });
            };

            area.Add(_itemLater = new Combobox(_max2.X + _max2.Width + 5, _max2.Y, 110, Enum.GetNames(typeof(TooltipLayers)), Array.IndexOf(Enum.GetValues(typeof(TooltipLayers)), data.ItemLayer)));
            _itemLater.OnOptionSelected += (s, e) =>
            {
                data.ItemLayer = (TooltipLayers)(Enum.GetValues(typeof(TooltipLayers))).GetValue(_itemLater.SelectedIndex);
                data.Save();
                UIManager.Add(new SimpleTimedTextGump("Saved", Microsoft.Xna.Framework.Color.LightGreen, TimeSpan.FromSeconds(1)) { X = _itemLater.ScreenCoordinateX, Y = _itemLater.ScreenCoordinateY - 20 });
            };

            area.Add(_del = new NiceButton(0, y, 20, 20, ButtonAction.Activate, "X") { IsSelectable = false });
            _del.SetTooltip("Delete this override");
            _del.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    data.Delete();
                    UIManager.GetGump<ToolTipOverideMenu>()?.Dispose();
                    UIManager.Add(new ToolTipOverideMenu(X, Y));
                }
            };
            return area;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (Reopen)
            {
                Reopen = false;
                Dispose();
                UIManager.Add(new ToolTipOverideMenu(X, Y));
            }

            return base.Draw(batcher, x, y);
        }
    }
}
