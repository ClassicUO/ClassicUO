using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;
using System.Globalization;
using static ClassicUO.Game.Managers.AutoLootManager;
using static ClassicUO.Game.UI.Gumps.OptionsGump;

namespace ClassicUO.Game.UI.Gumps
{
    internal class AutoLootOptions : Gump
    {
        public AutoLootOptions() : base(0, 0)
        {
            if (!AutoLootManager.Instance.IsLoaded)
            {
                Dispose();
                return;
            }

            Width = 400;
            Height = 600;
            X = 100;
            Y = 100;

            CanMove = true;
            CanCloseWithRightClick = true;

            BuildGump();
        }

        private void BuildGump()
        {
            Add(new AlphaBlendControl(0.85f) { Width = Width, Height = Height });

            SettingsSection topSecion = new SettingsSection("Simple Auto Loot", Width);

            topSecion.Add(new TextBox("Enable auto loot", TrueTypeLoader.EMBEDDED_FONT, 18, null, Color.White, strokeEffect: false) { AcceptMouseInput = true });

            Checkbox enable;
            topSecion.AddRight(enable = new Checkbox(0x00D2, 0x00D3, "", 0xff, 0xffff) { IsChecked = ProfileManager.CurrentProfile.EnableAutoLoot });
            enable.ValueChanged += (e, v) => { ProfileManager.CurrentProfile.EnableAutoLoot = enable.IsChecked; };

            NiceButton addEntry;
            topSecion.AddRight(addEntry = new NiceButton(0, 0, 100, 25, ButtonAction.Activate, "Add entry") { IsSelectable = false });
            addEntry.MouseUp += (e, v) =>
            {
                AutoLootManager.Instance.AddLootItem();
                AddToUI();
            };

            Add(topSecion);

            ScrollArea entries = new ScrollArea(0, topSecion.Y + topSecion.Height, Width, Height - topSecion.Y + topSecion.Height + 15, true);
            SettingsSection entriesSection = new SettingsSection("Loot entries", Width) { Y = topSecion.Y + topSecion.Height + 15 };
            entries.Add(entriesSection);

            BuildEntries(entriesSection);

            Add(entries);
        }

        private void BuildEntries(SettingsSection parent)
        {
            for (int i = 0; i < AutoLootManager.Instance.AutoLootList.Count; i++)
            {
                AutoLootItem autoLootItem = AutoLootManager.Instance.AutoLootList[i];
                Area area = new Area() { Width = Width - 18, Height = 50 };

                int x = 0;
                if (autoLootItem.Graphic > 0)
                {
                    ResizableStaticPic rsp;
                    area.Add(rsp = new ResizableStaticPic(autoLootItem.Graphic, 50, 50) { Hue = (ushort)(autoLootItem.Hue == ushort.MaxValue ? 0 : autoLootItem.Hue) });
                    rsp.SetTooltip(autoLootItem.Name);
                    x += 55;
                }

                InputField graphicInput = new InputField(0x0BB8, 0xFF, 0xFFF, true, 100, 50) { X = x };
                graphicInput.SetText(autoLootItem.Graphic.ToString());
                graphicInput.TextChanged += (s, e) =>
                {
                    if (graphicInput.Text.StartsWith("0x") && ushort.TryParse(graphicInput.Text.Substring(2), NumberStyles.AllowHexSpecifier, null, out var ngh))
                    {
                        autoLootItem.Graphic = ngh;
                    }
                    else if (ushort.TryParse(graphicInput.Text, out var ng))
                    {
                        autoLootItem.Graphic = ng;
                    }
                };
                area.Add(graphicInput);
                x += graphicInput.Width + 5;

                InputField hueInput = new InputField(0x0BB8, 0xFF, 0xFFF, true, 100, 50) { X = x };
                hueInput.SetText(autoLootItem.Hue == ushort.MaxValue ? "-1" : autoLootItem.Hue.ToString());
                hueInput.TextChanged += (s, e) =>
                {
                    if (hueInput.Text == "-1")
                    {
                        autoLootItem.Hue = ushort.MaxValue;
                    }
                    else if (ushort.TryParse(hueInput.Text, out var ng))
                    {
                        autoLootItem.Hue = ng;
                    }
                };
                area.Add(hueInput);
                x += hueInput.Width + 5;

                NiceButton delete;
                area.Add(delete = new NiceButton(x, 0, 100, 49, ButtonAction.Activate, "Delete") { IsSelectable = false, DisplayBorder = true });
                delete.MouseUp += (s, e) =>
                {
                    if (e.Button == Input.MouseButtonType.Left)
                    {
                        AutoLootManager.Instance.TryRemoveLootItem(autoLootItem.UID);
                        AddToUI();
                    }
                };
                x += delete.Width + 5;

                parent.Add(area);
            }
        }

        public static void AddToUI()
        {
            UIManager.GetGump<AutoLootOptions>()?.Dispose(); //Make sure only one is open at a time.
            UIManager.Add(new AutoLootOptions());
        }
    }
}
