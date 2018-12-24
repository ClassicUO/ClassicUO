#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Gumps.UIGumps.CharCreation;
using ClassicUO.Game.Scenes;
using ClassicUO.IO;
using ClassicUO.IO.Resources;

using static ClassicUO.Game.Scenes.LoginScene;

namespace ClassicUO.Game.Gumps.UIGumps.Login
{
    internal class LoginGump : Gump
    {
        private readonly TextBox _textboxAccount;
        private readonly TextBox _textboxPassword;
        private Checkbox _checkboxSaveAccount;


        public override void OnKeyboardReturn(int textID, string text)
        {
            Engine.SceneManager.GetScene<LoginScene>().Connect(_textboxAccount.Text, _textboxPassword.Text);
        }

        public LoginGump() : base(0, 0)
        {
            CanCloseWithRightClick = false;

            AcceptKeyboardInput = false;


            if (FileManager.ClientVersion >= ClientVersions.CV_500A)
                // Full background
                AddChildren(new GumpPic(0, 0, 0x2329, 0));


            //// Quit Button
            AddChildren(new Button((int)Buttons.Quit, 0x1589, 0x158B, 0x158A)
            {
                X = 555,
                Y = 4,
                ButtonAction = ButtonAction.Activate
            });


            // Login Panel
            AddChildren(new ResizePic(0x13BE)
            {
                X = 128,
                Y = 288,
                Width = 451,
                Height = 157
            });

            if (FileManager.ClientVersion < ClientVersions.CV_500A)
                AddChildren(new GumpPic(286, 45, 0x058A, 0));

            // Arrow Button
            AddChildren(new Button((int)Buttons.NextArrow, 0x15A4, 0x15A6, 0x15A5)
            {
                X = 610,
                Y = 445,
                ButtonAction = ButtonAction.Activate
            });

            // Account Text Input Background
            AddChildren(new ResizePic(0x0BB8)
            {
                X = 328,
                Y = 343,
                Width = 210,
                Height = 30
            });

            // Password Text Input Background
            AddChildren(new ResizePic(0x0BB8)
            {
                X = 328,
                Y = 383,
                Width = 210,
                Height = 30
            });

            AddChildren(_checkboxSaveAccount = new Checkbox(0x00D2, 0x00D3)
            {
                X = 328,
                Y = 417
            });
            //g_MainScreen.m_SavePassword->SetTextParameters(9, "Save Password", 0x0386, STP_RIGHT_CENTER);

            //g_MainScreen.m_AutoLogin =
            //    (CGUICheckbox*)AddChildren(new CGUICheckbox(ID_MS_AUTOLOGIN, 0x00D2, 0x00D3, 0x00D2, 183, 417));
            //g_MainScreen.m_AutoLogin->SetTextParameters(9, "Auto Login", 0x0386, STP_RIGHT_CENTER);
            AddChildren(new Label("Log in to Ultima Online", false, 0x0386, font: 2)
            {
                X = 253,
                Y = 305
            });

            AddChildren(new Label("Account Name", false, 0x0386, font: 2)
            {
                X = 183,
                Y = 345
            });

            AddChildren(new Label("Password", false, 0x0386, font: 2)
            {
                X = 183,
                Y = 385
            });

            AddChildren(new Label($"UO Version {Engine.GlobalSettings.ClientVersion}.", false, 0x034E, font: 9)
            {
                X = 286,
                Y = 453
            });

            AddChildren(new Label($"ClassicUO Version {Engine.Assembly.GetName().Version}", false, 0x034E, font: 9)
            {
                X = 286,
                Y = 465
            });

            // Text Inputs
            AddChildren(_textboxAccount = new TextBox(5, 32, 190, 190, false)
            {
                X = 335,
                Y = 343,
                Width = 190,
                Height = 25
            });

            AddChildren(_textboxPassword = new TextBox(5, 32, 190, 190, false)
            {
                X = 335,
                Y = 385,
                Width = 190,
                Height = 25,
                IsPassword = true
            });
            _textboxAccount.SetText(Engine.GlobalSettings.Username);
            _textboxPassword.SetText(Engine.GlobalSettings.Password);
        }


        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons)buttonID)
            {
                case Buttons.NextArrow:
                    Engine.SceneManager.GetScene<LoginScene>().Connect(_textboxAccount.Text, _textboxPassword.Text);
                    
                    break;
                case Buttons.Quit:
                    Engine.Quit();

                    break;
            }
        }


        private enum Buttons
        {
            NextArrow,
            Quit,
        }
    }
}