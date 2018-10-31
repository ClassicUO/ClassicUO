
﻿using ClassicUO.Configuration;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Gumps.UIGumps.CharCreation;
using ClassicUO.Game.Scenes;

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

                    return new LoadingGump();
                case LoginStep.CharacterSelection:

                    return new CharacterSelectionGump();
                case LoginStep.ServerSelection:

                    return new ServerSelectionGump();
                case LoginStep.CharCreation:
                    return new CharCreationGump();
            }

            return null;
        }
    }
}