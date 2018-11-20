using System;

using ClassicUO.Game.Gumps.Controls;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class LoadingGump : Gump
    {
        [Flags]
        public enum Buttons
        {
            None = 1,
            OK = 2,
            Cancel = 4
        }

        private readonly Action<int> _buttonClick;
        private Buttons _showButtons;

        public LoadingGump(string labelText, Buttons showButtons, Action<int> buttonClick = null) : base(0, 0)
        {
            _showButtons = showButtons;
            _buttonClick = buttonClick;

            AddChildren(new ResizePic(0x0A28)
            {
                X = 142, Y = 134, Width = 356, Height = 212
            });

            AddChildren(new Label(labelText, false, 0x0386, 326, 2, align: TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 162, Y = 178
            });

            if (showButtons == Buttons.OK)
            {
                AddChildren(new Button((int) Buttons.OK, 0x0481, 0x0483, 0x0482)
                {
                    X = 306, Y = 304, ButtonAction = ButtonAction.Activate
                });
            }
            else if (showButtons == (Buttons.OK | Buttons.Cancel))
            {
                AddChildren(new Button((int) Buttons.OK, 0x0481, 0x0483, 0x0482)
                {
                    X = 264, Y = 304, ButtonAction = ButtonAction.Activate
                });

                AddChildren(new Button((int) Buttons.Cancel, 0x047E, 0x0480, 0x047F)
                {
                    X = 348, Y = 304, ButtonAction = ButtonAction.Activate
                });
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            _buttonClick?.Invoke(buttonID);
            base.OnButtonClick(buttonID);
        }
    }
}