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
using ClassicUO.Configuration;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Scenes;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class LogoutGump : Gump
    {
        private Settings _settings;

        public LogoutGump() : base(0, 0)
        {
            AddChildren(new GumpPic(0, 0, 0x0816, 0));

            AddChildren(new Label("Quit\nUltima Online?", false, 0x0386, 165)
            {
                X = 33, Y = 30
            });

            AddChildren(new Button((int) Buttons.Cancel, 0x817, 0x818)
            {
                X = 37, Y = 75, ButtonAction = ButtonAction.Activate
            });

            AddChildren(new Button((int) Buttons.Ok, 0x81A, 0x81B)
            {
                X = 100, Y = 75, ButtonAction = ButtonAction.Activate
            });
            _settings = Service.Get<Settings>();
            CanMove = false;
            ControlInfo.IsModal = true;
        }

        public override void Update(double totalMS, double frameMS)
        {
            X = (UIManager.Width - Width) / 2;
            Y = (UIManager.Height - Height) / 2;
            base.Update(totalMS, frameMS);
        }

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 0:
                    Dispose();

                    break;
                case 1:
                    SceneManager.ChangeScene(ScenesType.Login);
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