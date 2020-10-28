using System;
using ClassicUO.Game.UI.Controls;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    internal class QuestionGump : Gump
    {
        private readonly Action<bool> _result;

        public QuestionGump(string message, Action<bool> result) : base(0, 0)
        {
            CanCloseWithRightClick = true;
            Add(new GumpPic(0, 0, 0x0816, 0));

            UOTexture t = GumpsLoader.Instance.GetTexture(0x0816);

            Width = t.Width;
            Height = t.Height;


            Add
            (
                new Label(message, false, 0x0386, 165)
                {
                    X = 33, Y = 30
                }
            );

            Add
            (
                new Button((int) Buttons.Cancel, 0x817, 0x818, 0x0819)
                {
                    X = 37, Y = 75, ButtonAction = ButtonAction.Activate
                }
            );

            Add
            (
                new Button((int) Buttons.Ok, 0x81A, 0x81B, 0x081C)
                {
                    X = 100, Y = 75, ButtonAction = ButtonAction.Activate
                }
            );

            CanMove = false;
            IsModal = true;

            X = (Client.Game.Window.ClientBounds.Width - Width) >> 1;
            Y = (Client.Game.Window.ClientBounds.Height - Height) >> 1;

            WantUpdateSize = false;
            _result = result;
        }


        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 0:
                    _result(false);
                    Dispose();

                    break;

                case 1:
                    _result(true);
                    Dispose();

                    break;
            }
        }

        private enum Buttons
        {
            Cancel,
            Ok
        }
    }
}