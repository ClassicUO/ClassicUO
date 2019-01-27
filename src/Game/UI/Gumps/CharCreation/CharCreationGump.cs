#region license
//  Copyright (C) 2019 ClassicUO Development Community on Github
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

using System.Linq;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps.Login;

namespace ClassicUO.Game.UI.Gumps.CharCreation
{
    internal class CharCreationGump : Gump
    {
        public enum CharCreationStep
        {
			Appearence = 0,
			ChooseTrade = 1,
			ChooseCity = 2,
		}

        private PlayerMobile _character;
        private CharCreationStep _currentStep;
        private LoadingGump _loadingGump;
        private readonly LoginScene loginScene;

	    private CityInfo _startingCity;

        public CharCreationGump() : base(0, 0)
        {
            loginScene = Engine.SceneManager.GetScene<LoginScene>();
            Add(new CreateCharAppearanceGump(), 1);
            SetStep(CharCreationStep.Appearence);
            CanCloseWithRightClick = false;
        }

        public void SetCharacter(PlayerMobile character)
        {
            _character = character;
            SetStep(CharCreationStep.ChooseTrade);
		}

	    public void SetAttributes()
	    {
		    SetStep(CharCreationStep.ChooseCity);
		}

	    public void SetCity(CityInfo city)
	    {
		    _startingCity = city;
	    }

		public void CreateCharacter()
        {
            loginScene.CreateCharacter(_character, _startingCity);
        }

        public void StepBack()
        {
            if (_currentStep == CharCreationStep.Appearence)
            {
                loginScene.StepBack();
            }
            else
                SetStep(_currentStep - 1);
        }

        public void ShowMessage(string message)
        {
            var currentPage = ActivePage;

            if (_loadingGump != null)
                Remove(_loadingGump);
            Add(_loadingGump = new LoadingGump(message, LoadingGump.Buttons.OK, a => ChangePage(currentPage)), 4);
            ChangePage(4);
        }

        private void SetStep(CharCreationStep step)
        {
            _currentStep = step;

            switch (step)
            {
                case CharCreationStep.Appearence:
                    ChangePage(1);

                    break;
                case CharCreationStep.ChooseTrade:
					var existing = Children.FirstOrDefault(page => page.Page == 2);

	                if (existing != null)
		                Remove(existing);

					Add(new CreateCharTradeGump(_character), 2);

                    ChangePage(2);
	                break;
                case CharCreationStep.ChooseCity:
                    if (Engine.GlobalSettings.ShardType == 2)
                    {
                        // FIXME: skip city selection on Outlands for now
                        SetCity(loginScene.Cities[0]);
                        CreateCharacter();
                    }
                    else
                    {
                        existing = Children.FirstOrDefault(page => page.Page == 3);

                        if (existing != null)
                            Remove(existing);

                        Add(new CreateCharCityGump(), 3);

                        ChangePage(3);
                    }
                    break;
            }
        }
    }
}