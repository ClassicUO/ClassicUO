using System;

using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Scenes;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class LoadingGump : Gump
    {
        private string _labelText;
        private Buttons showButtons;

        public LoadingGump() : base(0, 0)
        {
            //if (g_ConnectionScreen.GetType() != CST_CONLOST)
            AddChildren(new ResizePic(0x0A28)
            {
                X = 142, Y = 134, Width = 356, Height = 212
            });
            // else
            // AddChildren(new ResizePic(0x0A28) { X = 210, Y = 178, Width = 203, Height = 121 });
            DefineLoadingScreen();

            AddChildren(new Label(_labelText, false, 0x0386, 326, 2, align: TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 162, Y = 178
            });

            if (showButtons == Buttons.OK)
                AddChildren(new Button((int) Buttons.OK, 0x0481, 0x0483, 0x0482)
                {
                    X = 306, Y = 304, ButtonAction = ButtonAction.Activate
                });
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
            if (showButtons == Buttons.OK)
            {
                LoginScene loginScene = Service.Get<LoginScene>();
                loginScene.StepBack();
            }

            base.OnButtonClick(buttonID);
        }

        private void DefineLoadingScreen()
        {
            _labelText = "No Text";
            showButtons = Buttons.None;
            LoginScene loginScene = Service.Get<LoginScene>();

            if (!loginScene.LoginRejectionReason.HasValue)
            {
                switch (loginScene.CurrentLoginStep)
                {
                    case LoginScene.LoginStep.Connecting:
                        _labelText = Cliloc.GetString(3000002); // "Connecting..."

                        break;
                    case LoginScene.LoginStep.VerifyingAccount:
                        _labelText = Cliloc.GetString(3000003); // "Verifying Account..."

                        break;
                    case LoginScene.LoginStep.LoginInToServer:
                        _labelText = Cliloc.GetString(3000053); // logging into shard

                        break;
                    case LoginScene.LoginStep.EnteringBritania:
                        _labelText = Cliloc.GetString(3000001); // Entering Britania...

                        break;
                }
            }
            else
            {
                switch (loginScene.LoginRejectionReason.Value)
                {
                    case LoginScene.LoginRejectionReasons.BadPassword:
                    case LoginScene.LoginRejectionReasons.InvalidAccountPassword:
                        _labelText = Cliloc.GetString(3000036); // Incorrect username and/or password.

                        break;
                    case LoginScene.LoginRejectionReasons.AccountInUse:
                        _labelText = Cliloc.GetString(3000034); // Someone is already using this account.

                        break;
                    case LoginScene.LoginRejectionReasons.AccountBlocked:
                        _labelText = Cliloc.GetString(3000035); // Your account has been blocked / banned

                        break;
                    case LoginScene.LoginRejectionReasons.IdleExceeded:
                        _labelText = Cliloc.GetString(3000004); // Login idle period exceeded (I use "Connection lost")

                        break;
                    case LoginScene.LoginRejectionReasons.BadCommuncation:
                        _labelText = Cliloc.GetString(3000037); // Communication problem.

                        break;
                }

                showButtons = Buttons.OK;
            }
        }

        [Flags]
        private enum Buttons
        {
            None = 1,
            OK = 2,
            Cancel = 4
        }
    }
}