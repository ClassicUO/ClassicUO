using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static ClassicUO.Game.UI.Gumps.OptionsGump;

namespace ClassicUO.Game.UI.Gumps
{
    internal class GridHightlightMenu : Gump
    {
        private const int WIDTH = 350, HEIGHT = 500;
        private AlphaBlendControl background;
        private SettingsSection highlightSection;
        private ScrollArea highlightSectionScroll;

        public GridHightlightMenu(int x = 100, int y = 100) : base(0, 0)
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
                section.Add(_ = new NiceButton(0, 0, 40, 20, ButtonAction.Activate, "Add +") { IsSelectable = false });
                _.MouseUp += (s, e) =>
                {
                    if (e.Button == Input.MouseButtonType.Left)
                    {
                        highlightSectionScroll?.Add(NewAreaSection(ProfileManager.CurrentProfile.GridHighlight_Name.Count, y));
                        y += 21;
                    }
                };

                section.AddRight(_ = new NiceButton(0, 0, 60, 20, ButtonAction.Activate, "Export") { IsSelectable = false });
                _.MouseUp += (s, e) =>
                {
                    if (e.Button == Input.MouseButtonType.Left)
                    {
                        ExportGridHightlightSettings();
                    }
                };

                section.AddRight(_ = new NiceButton(0, 0, 60, 20, ButtonAction.Activate, "Import") { IsSelectable = false });
                _.MouseUp += (s, e) =>
                {
                    if (e.Button == Input.MouseButtonType.Left)
                    {
                        ImportGridHighlightSettings();
                    }
                };

                Add(section);
                y = section.Y + section.Height;
            }//Top section

            highlightSection = new SettingsSection("", WIDTH) { Y = y };
            highlightSection.Add(highlightSectionScroll = new ScrollArea(0, 0, WIDTH - 20, Height - y - 10, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways }); ;

            y = 0;
            for (int i = 0; i < ProfileManager.CurrentProfile.GridHighlight_Name.Count; i++)
            {
                highlightSectionScroll.Add(NewAreaSection(i, y));
                y += 21;
            }

            Add(highlightSection);
        }

        private Area NewAreaSection(int keyLoc, int y)
        {
            GridHighlightData data = GridHighlightData.GetGridHighlightData(keyLoc);
            Area area = new Area() { Y = y };
            area.Width = WIDTH - 40;
            area.Height = 150;
            y = 0;

            NiceButton _button, _del;
            area.Add(_button = new NiceButton(WIDTH - 170, y, 130, 20, ButtonAction.Activate, "Open property menu") { IsSelectable = false });
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
            hueDisplay.HueChanged += (s, e) =>
            {
                data.Hue = hueDisplay.Hue;
                data.Save();
                area.Add(new FadingLabel(10, "Saved", true, 0xff) { X = hueDisplay.X - 40, Y = hueDisplay.Y });
            };

            InputField _name;
            area.Add(_name = new InputField(0x0BB8, 0xFF, 0xFFFF, true, 120, 20) { X = 25, Y = y, AcceptKeyboardInput = true });
            _name.SetText(data.Name);

            _name.TextChanged += (s, e) =>
            {
                Task.Factory.StartNew(() =>
                {
                    var tVal = _name.Text;
                    System.Threading.Thread.Sleep(2500);
                    if (_name.Text == tVal)
                    {
                        data.Name = _name.Text;
                        data.Save();
                        area.Add(new FadingLabel(10, "Saved", true, 0xff) { X = _name.X, Y = _name.Y - 20 });
                    }
                });
            };

            area.Add(_del = new NiceButton(0, y, 20, 20, ButtonAction.Activate, "X") { IsSelectable = false });
            _del.SetTooltip("Delete this highlight configuration");
            _del.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    data.Delete();
                    Dispose();
                    UIManager.Add(new GridHightlightMenu(X, Y));
                }
            };

            y += 20;

            return area;
        }

        public static GridHighlightData[] GetAllGridHighlightData()
        {
            List<GridHighlightData> data = new List<GridHighlightData>();
            for (int i = 0; i < ProfileManager.CurrentProfile.GridHighlight_Name.Count; i++)
            {
                data.Add(GridHighlightData.GetGridHighlightData(i));
            }
            return data.ToArray();
        }

        public static void ExportGridHightlightSettings()
        {
            GridHighlightData[] allData = GetAllGridHighlightData();

            if (!CUOEnviroment.IsUnix)
            {
                Thread t = new Thread(() =>
                {
                    System.Windows.Forms.SaveFileDialog saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
                    saveFileDialog1.Filter = "Json|*.json";
                    saveFileDialog1.Title = "Save grid highlight settings";
                    saveFileDialog1.ShowDialog();

                    string result = JsonSerializer.Serialize(allData);

                    // If the file name is not an empty string open it for saving.
                    if (saveFileDialog1.FileName != "")
                    {
                        System.IO.FileStream fs =
                            (System.IO.FileStream)saveFileDialog1.OpenFile();
                        // NOTE that the FilterIndex property is one-based.
                        switch (saveFileDialog1.FilterIndex)
                        {
                            default:
                                byte[] data = Encoding.UTF8.GetBytes(result);
                                fs.Write(data, 0, data.Length);
                                break;
                        }

                        fs.Close();
                    }
                });
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
            }
        }

        public static void ImportGridHighlightSettings()
        {
            if (!CUOEnviroment.IsUnix)
            {
                Thread t = new Thread(() =>
                {
                    System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
                    openFileDialog.Filter = "Json|*.json";
                    openFileDialog.Title = "Import grid highlight settings";
                    openFileDialog.ShowDialog();

                    // If the file name is not an empty string open it for saving.
                    if (openFileDialog.FileName != "")
                    {
                        // NOTE that the FilterIndex property is one-based.
                        switch (openFileDialog.FilterIndex)
                        {
                            default:
                                try
                                {
                                    string result = File.ReadAllText(openFileDialog.FileName);

                                    GridHighlightData[] imported = JsonSerializer.Deserialize<GridHighlightData[]>(result);

                                    foreach (GridHighlightData importedData in imported)
                                        importedData.Save();

                                    UIManager.GetGump<GridHightlightMenu>()?.Dispose();
                                    UIManager.Add(new GridHightlightMenu());

                                }
                                catch (System.Exception e)
                                {
                                    GameActions.Print("It looks like there was an error trying to import your grid highlight settings.", 32);
                                    Console.WriteLine(e.ToString());
                                }
                                break;
                        }
                    }
                });
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            batcher.DrawRectangle(
                SolidColorTextureCache.GetTexture(Color.LightGray),
                x - 1, y - 1,
                WIDTH + 1, HEIGHT + 1,
                new Vector3(0, 0, 1)
                );

            return true;
        }

        public class GridHighlightData
        {
            private int keyLoc = -1;

            public string Name { get; set; } = "Name";
            public ushort Hue { get; set; } = 1;
            public List<string> Properties { get; set; } = new List<string>();
            public List<int> PropMinVal { get; set; } = new List<int>();

            public GridHighlightData()
            {

            }

            private GridHighlightData(int keyLoc)
            {
                this.keyLoc = keyLoc;
                if (ProfileManager.CurrentProfile.GridHighlight_Name.Count > keyLoc) //Key exists?
                {
                    Name = ProfileManager.CurrentProfile.GridHighlight_Name[keyLoc];
                    Hue = ProfileManager.CurrentProfile.GridHighlight_Hue[keyLoc];
                    Properties = ProfileManager.CurrentProfile.GridHighlight_PropNames[keyLoc];
                    PropMinVal = ProfileManager.CurrentProfile.GridHighlight_PropMinVal[keyLoc];
                }
                else
                {
                    ProfileManager.CurrentProfile.GridHighlight_Name.Add(Name);
                    ProfileManager.CurrentProfile.GridHighlight_Hue.Add(Hue);
                    Properties = new List<string>();
                    ProfileManager.CurrentProfile.GridHighlight_PropNames.Add(Properties);
                    PropMinVal = new List<int>();
                    ProfileManager.CurrentProfile.GridHighlight_PropMinVal.Add(PropMinVal);
                }

                this.keyLoc = keyLoc;
            }

            public void Save()
            {
                if (keyLoc == -1)
                    SaveAsNew();
                else
                {
                    SaveName();
                    SaveHue();
                    SaveProps();
                    SaveMinVals();
                }
            }

            private void SaveName()
            {
                ProfileManager.CurrentProfile.GridHighlight_Name[keyLoc] = Name;
            }

            private void SaveHue()
            {
                ProfileManager.CurrentProfile.GridHighlight_Hue[keyLoc] = Hue;
            }

            private void SaveProps()
            {
                ProfileManager.CurrentProfile.GridHighlight_PropNames[keyLoc] = Properties;
            }

            private void SaveMinVals()
            {
                ProfileManager.CurrentProfile.GridHighlight_PropMinVal[keyLoc] = PropMinVal;
            }

            private void SaveAsNew()
            {
                ProfileManager.CurrentProfile.GridHighlight_Name.Add(Name);
                ProfileManager.CurrentProfile.GridHighlight_Hue.Add(Hue);
                ProfileManager.CurrentProfile.GridHighlight_PropNames.Add(Properties);
                ProfileManager.CurrentProfile.GridHighlight_PropMinVal.Add(PropMinVal);
                keyLoc = ProfileManager.CurrentProfile.GridHighlight_Name.Count;
            }

            public void Delete()
            {
                ProfileManager.CurrentProfile.GridHighlight_Name.RemoveAt(keyLoc);
                ProfileManager.CurrentProfile.GridHighlight_Hue.RemoveAt(keyLoc);
                ProfileManager.CurrentProfile.GridHighlight_PropNames.RemoveAt(keyLoc);
                ProfileManager.CurrentProfile.GridHighlight_PropMinVal.RemoveAt(keyLoc);
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
            private readonly int keyLoc;

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

                NiceButton _addPropertyButton;
                Add(_addPropertyButton = new NiceButton(0, 0, 120, 20, ButtonAction.Activate, "Add property") { IsSelectable = false });
                _addPropertyButton.MouseUp += (s, e) =>
                {
                    if (e.Button == Input.MouseButtonType.Left)
                    {
                        AddProperty(data.Properties.Count);

                        lastYitem += 20;
                    }
                };

                Add(scrollArea = new ScrollArea(0, 20, WIDTH, HEIGHT - 20, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways });

                scrollArea.Add(new Label("Property name", true, 0xffff, 120) { X = 0, Y = lastYitem });
                scrollArea.Add(new Label("Min value", true, 0xffff, 120) { X = 180, Y = lastYitem });

                lastYitem += 20;

                for (int i = 0; i < data.Properties.Count; i++)
                {
                    AddProperty(i);
                    lastYitem += 20;
                }

                this.keyLoc = keyLoc;
            }

            private void AddProperty(int subKeyLoc)
            {
                while (data.Properties.Count <= subKeyLoc)
                {
                    data.Properties.Add("");
                    data.PropMinVal.Add(-1);

                }
                data.Save();
                InputField propInput, valInput;
                scrollArea.Add(propInput = new InputField(0x0BB8, 0xFF, 0xFFFF, true, 150, 20) { Y = lastYitem });
                propInput.SetText(data.Properties[subKeyLoc]);
                propInput.TextChanged += (s, e) =>
                {
                    Task.Factory.StartNew(() =>
                    {
                        var tVal = propInput.Text;
                        System.Threading.Thread.Sleep(2500);
                        if (propInput.Text == tVal)
                        {
                            data.Properties[subKeyLoc] = propInput.Text;
                            data.Save();
                            propInput.Add(new FadingLabel(10, "Saved", true, 0xff) { X = 0, Y = -20 });
                        }
                    });
                };

                scrollArea.Add(valInput = new InputField(0x0BB8, 0xFF, 0xFFFF, true, 100, 20) { X = 180, Y = lastYitem, NumbersOnly = true });
                valInput.SetText(data.PropMinVal[subKeyLoc].ToString());
                valInput.TextChanged += (s, e) =>
                {
                    Task.Factory.StartNew(() =>
                    {
                        var tVal = valInput.Text;
                        System.Threading.Thread.Sleep(2500);
                        if (valInput.Text == tVal)
                        {
                            if (int.TryParse(valInput.Text, out int val))
                            {
                                data.PropMinVal[subKeyLoc] = val;
                                data.Save();
                                valInput.Add(new FadingLabel(10, "Saved", true, 0xff) { X = 0, Y = -20 });
                            }
                            else
                            {
                                valInput.Add(new FadingLabel(20, "Couldn't parse number", true, 0xff) { X = 0, Y = -20 });
                            }
                        }
                    });
                };

                NiceButton _del;
                scrollArea.Add(_del = new NiceButton(285, lastYitem, 20, 20, ButtonAction.Activate, "X") { IsSelectable = false });
                _del.SetTooltip("Delete this property");
                _del.MouseUp += (s, e) =>
                {
                    if (e.Button == Input.MouseButtonType.Left)
                    {
                        Dispose();
                        data.Properties.RemoveAt(subKeyLoc);
                        data.PropMinVal.RemoveAt(subKeyLoc);
                        data.Save();
                        UIManager.Add(new GridHightlightProperties(keyLoc, X, Y));
                    }
                };
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
}
