#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using System;

using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.IO;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.UI.Controls
{
    internal class InfoBarBuilderControl : Control
    {
        public string LabelText { get { return infoLabel.Text; } }
        public InfoBarVars Var { get { return (InfoBarVars) varStat.SelectedIndex; } }
        public ushort Hue { get { return labelColor.Hue; } }

        private TextBox infoLabel;
        private Combobox varStat;
        private ClickableColorBox labelColor;

        public InfoBarBuilderControl(InfoBarItem item)
        {
            infoLabel = new TextBox(0xFF, 10, 80, 80) { X = 5, Y = 0, Width = 130, Height = 30, Text = item.label };

            string[] dataVars = InfoBarManager.GetVars(Settings.GlobalSettings.ShardType);
            varStat = new Combobox(200, 0, 170, dataVars, (int) item.var);

            uint color = 0xFF7F7F7F;

            if (item.hue != 0xFFFF)
                color = HuesLoader.Instance.GetPolygoneColor(12, item.hue);

            labelColor = new ClickableColorBox(150, 0, 13, 14, item.hue, color);

            NiceButton deleteButton = new NiceButton(390, 0, 60, 25, ButtonAction.Activate, "Delete") { ButtonParameter = 999 };
            deleteButton.MouseUp += (sender, e) =>
            {
                Dispose();
            };

            Add(new ResizePic(0x0BB8) { X = infoLabel.X - 5, Y = 0, Width = infoLabel.Width + 10, Height = infoLabel.Height - 6 });
            Add(infoLabel);
            Add(varStat);
            Add(labelColor);
            Add(deleteButton);
        }

    }

}
