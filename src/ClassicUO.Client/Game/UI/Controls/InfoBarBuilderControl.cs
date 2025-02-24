// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Managers;
using ClassicUO.Resources;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.UI.Controls
{
    internal class InfoBarBuilderControl : Control
    {
        private readonly StbTextBox infoLabel;
        private readonly ClickableColorBox labelColor;
        private readonly Combobox varStat;
        private readonly Gump _gump;

        public InfoBarBuilderControl(Gump gump, InfoBarItem item)
        {
            _gump = gump;
            infoLabel = new StbTextBox(0xFF, 10, 80) { X = 5, Y = 0, Width = 130, Height = 26 };
            infoLabel.SetText(item.label);

            string[] dataVars = InfoBarManager.GetVars();

            varStat = new Combobox
            (
                200,
                0,
                170,
                dataVars,
                (int) item.var
            );


            labelColor = new ClickableColorBox
            (
                _gump.World,
                150,
                0,
                13,
                14,
                item.hue
            );

            NiceButton deleteButton = new NiceButton
            (
                390,
                0,
                60,
                25,
                ButtonAction.Activate,
                ResGumps.Delete
            ) { ButtonParameter = 999 };

            deleteButton.MouseUp += (sender, e) =>
            {
                Dispose();
                ((DataBox) Parent)?.ReArrangeChildren();
            };

            Add(new ResizePic(0x0BB8) { X = infoLabel.X - 5, Y = 0, Width = infoLabel.Width + 10, Height = infoLabel.Height });

            Add(infoLabel);
            Add(varStat);
            Add(labelColor);
            Add(deleteButton);

            Width = infoLabel.Width + 10;
            Height = infoLabel.Height;
        }

        public string LabelText => infoLabel.Text;
        public InfoBarVars Var => (InfoBarVars) varStat.SelectedIndex;
        public ushort Hue => labelColor.Hue;
    }
}