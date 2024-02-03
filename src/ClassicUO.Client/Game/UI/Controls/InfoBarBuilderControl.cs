#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

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