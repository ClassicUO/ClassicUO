
﻿using ClassicUO.Configuration;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Gumps.UIGumps.CharCreation;
using ClassicUO.Game.Scenes;
using ClassicUO.IO.Resources;
using static ClassicUO.Game.Scenes.LoginScene;

namespace ClassicUO.Game.Gumps.UIGumps.Login
{
    internal class LoginGump : Gump
    {
        private readonly LoginScene loginScene;
        
        private LoginStep currentStep;
        private GumpControl currentStepGump;

        public LoginGump() : base(0, 0)
        {
            loginScene = Service.Get<LoginScene>();
            CanCloseWithRightClick = false;

            // Background
            AddChildren(new GumpPicTiled(0, 0, 640, 480, 0x0E14));
            // Border
            AddChildren(new GumpPic(0, 0, 0x157C, 0));
            AddChildren(currentStepGump = GetGumpForStep(loginScene.CurrentLoginStep));

            // UO Flag
            AddChildren(new GumpPic(0, 4, 0x15A0, 0));

            // Quit Button
            AddChildren(new Button(0, 0x1589, 0x158B, 0x158A)
            {
                X = 555, Y = 4, ButtonAction = ButtonAction.Activate
            });
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (loginScene.UpdateScreen)
            {
                RemoveChildren(currentStepGump);
                AddChildren(currentStepGump = GetGumpForStep(loginScene.CurrentLoginStep));
                loginScene.UpdateScreen = false;
            }

            base.Update(totalMS, frameMS);
        }

        private GumpControl GetGumpForStep(LoginStep step)
        {
            currentStep = step;

            switch (step)
            {
                case LoginStep.Main:

                    return new MainLoginGump();
                case LoginStep.Connecting:
                case LoginStep.VerifyingAccount:
                case LoginStep.LoginInToServer:
                case LoginStep.EnteringBritania:
                    return GetLoadingScreen();

                case LoginStep.CharacterSelection:
                    return new CharacterSelectionGump();

                case LoginStep.ServerSelection:
                    return new ServerSelectionGump();

                case LoginStep.CharCreation:
                    return new CharCreationGump();
            }

            return null;
        }

        private LoadingGump GetLoadingScreen()
        {
            var labelText = "No Text";
            var showButtons = LoadingGump.Buttons.None;
            
            if (!loginScene.LoginRejectionReason.HasValue)
            {
                switch (loginScene.CurrentLoginStep)
                {
                    case LoginScene.LoginStep.Connecting:
                        labelText = Cliloc.GetString(3000002); // "Connecting..."
                        break;
                    case LoginScene.LoginStep.VerifyingAccount:
                        labelText = Cliloc.GetString(3000003); // "Verifying Account..."
                        break;
                    case LoginScene.LoginStep.LoginInToServer:
                        labelText = Cliloc.GetString(3000053); // logging into shard
                        break;
                    case LoginScene.LoginStep.EnteringBritania:
                        labelText = Cliloc.GetString(3000001); // Entering Britania...
                        break;
                }
            }
            else
            {
                switch (loginScene.LoginRejectionReason.Value)
                {
                    case LoginScene.LoginRejectionReasons.BadPassword:
                    case LoginScene.LoginRejectionReasons.InvalidAccountPassword:
                        labelText = Cliloc.GetString(3000036); // Incorrect username and/or password.
                        break;
                    case LoginScene.LoginRejectionReasons.AccountInUse:
                        labelText = Cliloc.GetString(3000034); // Someone is already using this account.
                        break;
                    case LoginScene.LoginRejectionReasons.AccountBlocked:
                        labelText = Cliloc.GetString(3000035); // Your account has been blocked / banned
                        break;
                    case LoginScene.LoginRejectionReasons.IdleExceeded:
                        labelText = Cliloc.GetString(3000004); // Login idle period exceeded (I use "Connection lost")
                        break;
                    case LoginScene.LoginRejectionReasons.BadCommuncation:
                        labelText = Cliloc.GetString(3000037); // Communication problem.
                        break;
                }

                showButtons = LoadingGump.Buttons.OK;
            }

            return new LoadingGump(labelText, showButtons, OnLoadingGumpButtonClick);
        }

        private void OnLoadingGumpButtonClick(int buttonId)
        {
            if ((LoadingGump.Buttons)buttonId == LoadingGump.Buttons.OK)
            {
                loginScene.StepBack();
            }
        }
    }
}